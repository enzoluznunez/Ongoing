using System;
using System.Threading;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Assistant.Live;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Voice
{
    public static class VoiceSession
    {
        private static readonly TimeSpan IdleReset = TimeSpan.FromHours(2);
        private const int GoAwayGraceMilliseconds = 8000;
        private const int GoAwayCheckMilliseconds = 200;
        private const int PreviousDrainMilliseconds = 5000;
        private const int FirstBackoffMilliseconds = 500;
        private const int LongestBackoffMilliseconds = 5000;
        private const int HealthySessionMilliseconds = 2000;
        private const int MostFailures = 5;

        private static volatile VoiceStatus _status = VoiceStatus.Off;
        private static volatile LiveSession _live;
        private static volatile string _resumeHandle;
        private static volatile bool _setupCompleted;
        private static volatile bool _busy;
        private static volatile bool _shutdownAfterTurn;
        private static volatile bool _keepAlive;
        private static volatile bool _goAwayPending;
        private static DateTime _goAwayDeadlineUtc;
        private static DateTime _lastInactiveUtc;

        private static CancellationTokenSource _sessionCancel;
        private static Task _sessionTask;
        private static int _generation;
        private static int _connectionCounter;
        private static int _activeConnection;

        public static VoiceStatus Status
        {
            get => _status;
            internal set => _status = value;
        }

        public static bool Busy
        {
            get => _busy;
            internal set => _busy = value;
        }

        public static bool KeepAlive => _keepAlive;

        internal static bool SetupCompleted
        {
            get => _setupCompleted;
            set => _setupCompleted = value;
        }

        internal static string ResumeHandle
        {
            set => _resumeHandle = value;
        }

        internal static bool Ready => _setupCompleted && _status == VoiceStatus.Live;

        public static void SetKeepAlive(bool value)
        {
            _keepAlive = value;
            if (value) ScenePush.Request();
        }

        public static void RequestShutdownAfterTurn() => _shutdownAfterTurn = true;

        public static void CancelShutdownRequest() => _shutdownAfterTurn = false;

        internal static bool ConsumeShutdownRequest()
        {
            if (!_shutdownAfterTurn) return false;
            _shutdownAfterTurn = false;
            return true;
        }

        internal static bool IsCurrent(LiveSession session) => session == _live;

        internal static bool IsCurrentGeneration(int generation) => generation == Volatile.Read(ref _generation);

        internal static bool IsCurrentConnection(int connection) => connection == Volatile.Read(ref _activeConnection);

        private static void RetireConnection() => Volatile.Write(ref _activeConnection, 0);

        private static void SetStatus(int generation, VoiceStatus status)
        {
            if (IsCurrentGeneration(generation)) _status = status;
        }

        internal static void StartFresh()
        {
            ContextBudget.Reset();
            ActivityFeed.ClearPending();
        }

        internal static void BeginGoAwayReconnect()
        {
            if (_goAwayPending) return;
            _goAwayDeadlineUtc = DateTime.UtcNow.AddMilliseconds(GoAwayGraceMilliseconds);
            _goAwayPending = true;
        }

        internal static void DropConnection()
        {
            _resumeHandle = null;
            RetireConnection();
            LiveSession session = _live;
            if (session != null) { try { _ = session.CloseAsync(); } catch { } }
        }

        public static void Listen()
        {
            if (!MicrophoneStream.Listen()) _status = VoiceStatus.MicDenied;
        }

        public static void Connect()
        {
            if (VoiceSetup.Client == null) return;

            bool running = _sessionTask != null && !_sessionTask.IsCompleted;
            if (running && (_status == VoiceStatus.Live || _status == VoiceStatus.Connecting)) return;

            if (_lastInactiveUtc != default && DateTime.UtcNow - _lastInactiveUtc > IdleReset)
            {
                _resumeHandle = null;
                StartFresh();
            }

            if (ContextBudget.Exhausted) ContextBudget.ClearExhaustion();

            _status = VoiceStatus.Connecting;
            _shutdownAfterTurn = false;

            Task previous = _sessionTask;
            _sessionCancel?.Cancel();
            int generation = Interlocked.Increment(ref _generation);
            var cancel = new CancellationTokenSource();
            _sessionCancel = cancel;
            _sessionTask = Task.Run(() => RunAfter(previous, generation, cancel.Token));
        }

        public static void Disconnect()
        {
            MicrophoneStream.Mute();
            RetireConnection();
            Interlocked.Increment(ref _generation);
            CancellationTokenSource cancel = _sessionCancel;
            _sessionCancel = null;
            if (cancel != null)
                Task.Run(() =>
                {
                    try { cancel.Cancel(); }
                    catch (ObjectDisposedException) { }
                });
            _lastInactiveUtc = DateTime.UtcNow;
            if (_status != VoiceStatus.MicDenied && _status != VoiceStatus.Failed)
                _status = VoiceStatus.Off;
        }

        public static void Destroy()
        {
            try
            {
                RetireConnection();
                Interlocked.Increment(ref _generation);
                _sessionCancel?.Cancel();
                _sessionCancel = null;
                _sessionTask = null;
                MicrophoneStream.Stop();
                _status = VoiceStatus.Off;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private static async Task RunAfter(Task previous, int generation, CancellationToken token)
        {
            if (previous != null && !previous.IsCompleted)
            {
                Debug.Log("[VoiceSession] Waiting for the previous session loop to drain.");
                Task drained = await Task.WhenAny(previous, Task.Delay(PreviousDrainMilliseconds)).ConfigureAwait(false);
                if (!ReferenceEquals(drained, previous))
                    Debug.LogWarning($"[VoiceSession] The previous loop did not drain within {PreviousDrainMilliseconds} ms; starting anyway.");
            }

            if (!IsCurrentGeneration(generation))
            {
                Debug.Log($"[VoiceSession] Generation {generation} was superseded before it started.");
                return;
            }

            await Run(generation, token).ConfigureAwait(false);
        }

        private static async Task Run(int generation, CancellationToken token)
        {
            int backoffMilliseconds = FirstBackoffMilliseconds;
            int failures = 0;

            while (!token.IsCancellationRequested)
            {
                _goAwayPending = false;
                LiveSession session = null;
                try
                {
                    LiveConnectConfig config = VoiceSetup.Config;
                    config.SessionResumption = new SessionResumptionConfig { Handle = _resumeHandle };
                    ContextBudget.StartConnection(_resumeHandle != null);
                    _setupCompleted = false;
                    _busy = false;
                    ToolCallTracker.ResetRounds();
                    Transcripts.Reset();
                    ToolCallTracker.Clear();
                    Debug.Log($"[VoiceSession] Connecting: model={VoiceSetup.ModelId}, tools={VoiceSetup.ToolCount}, " +
                              $"promptChars={VoiceSetup.PromptChars}, resume={_resumeHandle != null}, " +
                              $"serverTrim={ContextBudget.ServerTrimTrigger}/{ContextBudget.ServerTrimTarget}");
                    session = await VoiceSetup.Client.ConnectAsync(VoiceSetup.ModelId, config).ConfigureAwait(false);
                    if (!IsCurrentGeneration(generation)) break;
                    _status = VoiceStatus.Live;
                    _live = session;
                    int connection = Interlocked.Increment(ref _connectionCounter);
                    Volatile.Write(ref _activeConnection, connection);
                    Debug.Log($"[VoiceSession] Live (generation={generation}, connection={connection}, " +
                              $"resumed={_resumeHandle != null}, contextBefore={ContextBudget.ContextTokens})");

                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    Task send = MicrophoneStream.SendPump(session, token);
                    Task receive = ReplyReader.Pump(session, generation, connection, token);
                    Task push = ScenePush.Pump(session, token);
                    Task goAway = GoAwayPump(session, token);
                    await Task.WhenAny(send, receive).ConfigureAwait(false);
                    if (IsCurrentConnection(connection)) RetireConnection();
                    if (ReferenceEquals(_live, session)) _live = null;
                    MicrophoneStream.Wake();
                    ScenePush.Request();
                    try { await session.CloseAsync().ConfigureAwait(false); } catch { }
                    try { await Task.WhenAll(send, receive, push, goAway).ConfigureAwait(false); } catch { }
                    session = null;
                    clock.Stop();

                    if (clock.ElapsedMilliseconds >= HealthySessionMilliseconds)
                    {
                        failures = 0;
                        backoffMilliseconds = FirstBackoffMilliseconds;
                    }
                    else
                    {
                        BackOff(ref failures, ref backoffMilliseconds);
                        if (_resumeHandle != null)
                        {
                            Debug.LogWarning("[VoiceSession] The session died immediately; dropping the stale resume handle and starting fresh.");
                            _resumeHandle = null;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VoiceSession] Session error: {e}");
                    if (session == null) _resumeHandle = null;
                    BackOff(ref failures, ref backoffMilliseconds);
                }
                finally
                {
                    if (session != null)
                    {
                        if (ReferenceEquals(_live, session)) _live = null;
                        try { await session.CloseAsync().ConfigureAwait(false); } catch { }
                    }
                }

                if (token.IsCancellationRequested) break;
                if (ContextBudget.Exhausted)
                {
                    ContextBudget.MarkEnded();
                    break;
                }
                if (!_keepAlive) break;

                if (failures >= MostFailures)
                {
                    Debug.LogError("[VoiceSession] Giving up after repeated session failures.");
                    SetStatus(generation, VoiceStatus.Failed);
                    break;
                }

                SetStatus(generation, VoiceStatus.Reconnecting);
                try { await Task.Delay(backoffMilliseconds, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }

            if (IsCurrentGeneration(generation) && _status != VoiceStatus.Failed)
                _status = VoiceStatus.Off;
        }

        private static async Task GoAwayPump(LiveSession session, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(GoAwayCheckMilliseconds, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
                if (!IsCurrent(session)) return;
                ToolCallTracker.CheckStalled();
                if (!_goAwayPending) continue;

                bool quiet = !_busy;
                if (!quiet && DateTime.UtcNow < _goAwayDeadlineUtc) continue;

                _goAwayPending = false;
                ProtocolLog.Note($"-> goAway reconnect (quiet={quiet})");
                Debug.Log($"[VoiceSession] Cycling the socket after GoAway (quiet={quiet}).");
                try { await session.CloseAsync().ConfigureAwait(false); } catch { }
                return;
            }
        }

        private static void BackOff(ref int failures, ref int backoffMilliseconds)
        {
            failures++;
            backoffMilliseconds = Mathf.Min(backoffMilliseconds * 2, LongestBackoffMilliseconds);
        }
    }
}
