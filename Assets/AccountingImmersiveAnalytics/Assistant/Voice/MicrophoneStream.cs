using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Audio;
using UnityEngine;
using UnityEngine.Android;

namespace AccountingImmersiveAnalytics.Assistant.Voice
{
    public static class MicrophoneStream
    {
        private const int FrameBytes = 640;
        private const int MostQueuedFrames = 25;
        private const int FramesPerLog = 500;
        private const float BytesPerMillisecond = 32f;
        private const string AudioMimeType = "audio/pcm;rate=16000";

        private static readonly ConcurrentQueue<byte[]> _frames = new ConcurrentQueue<byte[]>();
        private static readonly object _bufferGate = new object();
        private static byte[] _buffer = new byte[FrameBytes * 4];
        private static int _bufferLength;
        private static SemaphoreSlim _framesReady;

        private static readonly object _audioGate = new object();
        private static Task _audioTail = Task.CompletedTask;
        private static bool _listening;

        private static int _rawFrames;
        private static int _rawSmallest = int.MaxValue;
        private static int _rawLargest;

        public static bool Granted { get; private set; }

        public static Task<bool> RequestPermission()
        {
            var answer = new TaskCompletionSource<bool>();
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                answer.SetResult(true);
                return answer.Task;
            }
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => answer.TrySetResult(true);
            callbacks.PermissionDenied += _ => answer.TrySetResult(false);
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
#else
            answer.SetResult(true);
#endif
            return answer.Task;
        }

        public static void Start(bool granted)
        {
            Granted = granted;
            Speaker.Init();
            _framesReady = new SemaphoreSlim(0);
            if (!granted) return;
            Voip.Init();
            Voip.FrameCaptured += Capture;
        }

        public static void Stop()
        {
            bool hadMic = Granted;
            if (hadMic) Voip.FrameCaptured -= Capture;
            RunAudio(() =>
            {
                if (hadMic) Voip.Destroy();
                Speaker.Destroy();
            });
            _listening = false;
        }

        public static bool Listen()
        {
            if (!Granted) return false;
            if (_listening) return true;
            _listening = true;
            Drop();
            RunAudio(() =>
            {
                Speaker.Start();
                Voip.Start();
            });
            return true;
        }

        public static void Mute()
        {
            if (!_listening) return;
            _listening = false;
            Drop();
            RunAudio(() =>
            {
                Speaker.Stop();
                Voip.Stop();
            });
        }

        public static void Wake() => _framesReady?.Release();

        public static async Task SendPump(LiveSession session, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try { await _framesReady.WaitAsync(token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
                if (!VoiceSession.IsCurrent(session)) return;
                while (_frames.TryDequeue(out byte[] frame))
                {
                    if (!VoiceSession.IsCurrent(session)) return;
                    if (!VoiceSession.Ready) continue;
                    try
                    {
                        await session.SendRealtimeInputAsync(new LiveSendRealtimeInputParameters
                        {
                            Audio = new Blob { MimeType = AudioMimeType, Data = frame }
                        }).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { return; }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MicrophoneStream] Send failed: {e.Message}");
                        return;
                    }
                }
            }
        }

        private static void RunAudio(Action action)
        {
            lock (_audioGate)
            {
                _audioTail = _audioTail.ContinueWith(_ =>
                {
                    try { action(); }
                    catch (Exception e) { Debug.LogError(e); }
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
        }

        private static void Drop()
        {
            lock (_bufferGate) _bufferLength = 0;
            while (_frames.TryDequeue(out _)) { }
        }

        private static void Capture(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            LogRawSize(data.Length);

            lock (_bufferGate)
            {
                int needed = _bufferLength + data.Length;
                if (needed > _buffer.Length)
                    Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, needed));
                Buffer.BlockCopy(data, 0, _buffer, _bufferLength, data.Length);
                _bufferLength = needed;

                int taken = 0;
                while (_bufferLength - taken >= FrameBytes)
                {
                    var frame = new byte[FrameBytes];
                    Buffer.BlockCopy(_buffer, taken, frame, 0, FrameBytes);
                    taken += FrameBytes;
                    _frames.Enqueue(frame);
                    while (_frames.Count > MostQueuedFrames && _frames.TryDequeue(out _)) { }
                    _framesReady?.Release();
                }

                if (taken == 0) return;
                _bufferLength -= taken;
                if (_bufferLength > 0) Buffer.BlockCopy(_buffer, taken, _buffer, 0, _bufferLength);
            }
        }

        private static void LogRawSize(int bytes)
        {
            _rawFrames++;
            if (bytes < _rawSmallest) _rawSmallest = bytes;
            if (bytes > _rawLargest) _rawLargest = bytes;
            if (_rawFrames % FramesPerLog != 0) return;
            Debug.Log($"[MicrophoneStream] {_rawFrames} frames in, raw {_rawSmallest}-{_rawLargest} B " +
                      $"(~{_rawSmallest / BytesPerMillisecond:0.#}-{_rawLargest / BytesPerMillisecond:0.#} ms); " +
                      $"sending fixed {FrameBytes} B (~{FrameBytes / BytesPerMillisecond:0.#} ms)");
            _rawSmallest = int.MaxValue;
            _rawLargest = 0;
        }
    }
}
