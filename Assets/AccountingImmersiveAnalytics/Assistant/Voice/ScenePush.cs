using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Assistant.Live;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Voice
{
    public static class ScenePush
    {
        private const int MostWaitMilliseconds = 30000;
        private const int WaitStepMilliseconds = 100;

        private static SemaphoreSlim _wanted;

        public static void Start() => _wanted = new SemaphoreSlim(0);

        public static void Request() => _wanted?.Release();

        private static bool Idle() => VoiceSession.KeepAlive && VoiceSession.Ready && !VoiceSession.Busy;

        public static async Task Pump(LiveSession session, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!ActivityFeed.HasPending || !VoiceSession.KeepAlive)
                {
                    try { await _wanted.WaitAsync(token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }
                if (!VoiceSession.IsCurrent(session)) return;
                if (!VoiceSession.KeepAlive) continue;

                int waited = 0;
                while (!Idle() && waited < MostWaitMilliseconds)
                {
                    try { await Task.Delay(WaitStepMilliseconds, token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { return; }
                    if (!VoiceSession.IsCurrent(session)) return;
                    waited += WaitStepMilliseconds;
                }
                if (!Idle()) continue;

                string text = null;
                try
                {
                    await MainThread.Run(() =>
                    {
                        if (ActivityFeed.TryTakeBatch(out string batch)) text = batch;
                    }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ScenePush] Could not gather the update: {e.Message}");
                    continue;
                }
                if (string.IsNullOrEmpty(text)) continue;

                try
                {
                    ProtocolLog.Note($"-> clientContent {text.Length}B (turnComplete=false)");
                    Debug.Log($"[ScenePush] {text.Replace('\n', ' ')}");
                    await session.SendClientContentAsync(new LiveSendClientContentParameters
                    {
                        Turns = new List<Content>
                        {
                            new Content
                            {
                                Role = "user",
                                Parts = new List<Part> { new Part { Text = text } }
                            }
                        },
                        TurnComplete = false
                    }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ScenePush] Sending failed: {e.Message}");
                }
            }
        }
    }
}
