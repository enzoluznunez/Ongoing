using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools;
using AccountingImmersiveAnalytics.Audio;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Voice
{
    public static class ReplyReader
    {
        private const string AudioMimePrefix = "audio/pcm";

        public static async Task Pump(LiveSession session, int generation, int connection, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                LiveServerMessage message;
                try { message = await session.ReceiveAsync().ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
                catch (Exception e)
                {
                    Debug.LogError($"[ReplyReader] Receive failed: {e.Message}");
                    Debug.LogError($"[ReplyReader] setupCompleted={VoiceSession.SetupCompleted}; recent protocol:\n  {ProtocolLog.Dump()}");
                    break;
                }
                if (message == null) break;
                if (!VoiceSession.IsCurrentGeneration(generation))
                {
                    Debug.LogWarning($"[ReplyReader] Dropping a message from superseded generation {generation}.");
                    break;
                }
                if (!VoiceSession.IsCurrentConnection(connection))
                {
                    Debug.LogWarning($"[ReplyReader] Dropping a message from retired connection {connection} (generation {generation}).");
                    break;
                }
                Read(session, connection, message);
            }
        }

        private static void Read(LiveSession session, int connection, LiveServerMessage message)
        {
            ContextBudget.Observe(message.UsageMetadata, connection);
            ReadControl(session, message);
            ReadToolCalls(session, message);
            ReadContent(message.ServerContent);
            Transcripts.FlushHeardIfPending();
        }

        private static void ReadControl(LiveSession session, LiveServerMessage message)
        {
            if (message.SetupComplete != null)
            {
                VoiceSession.SetupCompleted = true;
                Debug.Log("[ReplyReader] Setup complete.");
                ActivityFeed.RequestSnapshot();
            }

            LiveServerSessionResumptionUpdate resume = message.SessionResumptionUpdate;
            if (resume != null && resume.Resumable == true && !string.IsNullOrEmpty(resume.NewHandle))
                VoiceSession.ResumeHandle = resume.NewHandle;

            if (message.GoAway != null)
            {
                Debug.Log($"[ReplyReader] Server GoAway (time left: {message.GoAway.TimeLeft}).");
                if (VoiceSession.KeepAlive) VoiceSession.BeginGoAwayReconnect();
                else _ = session.CloseAsync();
            }
        }

        private static void ReadToolCalls(LiveSession session, LiveServerMessage message)
        {
            if (message.ToolCall != null)
            {
                VoiceSession.Busy = true;
                List<FunctionCall> calls = message.ToolCall.FunctionCalls;
                if (calls != null)
                {
                    if (ToolCallTracker.StartRound() == 1) Transcripts.FlushHeardLater();
                    ProtocolLog.Note($"<- toolCall {string.Join(",", calls.ConvertAll(c => c.Name))}");
                    foreach (FunctionCall call in calls)
                    {
                        ToolCallTracker.Dispatched(call.Id, call.Name);
                        _ = ToolRegistry.Run(session, call);
                    }
                }
            }

            if (message.ToolCallCancellation != null)
            {
                List<string> ids = message.ToolCallCancellation.Ids;
                ToolCallTracker.Cancel(ids);
                string listed = ids != null ? string.Join(",", ids) : "";
                ProtocolLog.Note($"<- toolCallCancellation ids={listed}");
                Debug.Log($"[ReplyReader] Tool calls cancelled: {listed}");
            }
        }

        private static void ReadContent(LiveServerContent content)
        {
            if (content == null) return;

            GroundingMetadata grounding = content.GroundingMetadata;
            if (grounding != null)
            {
                string asked = grounding.WebSearchQueries != null ? string.Join(" | ", grounding.WebSearchQueries) : "";
                ProtocolLog.Note($"<- groundingMetadata queries=[{asked}]");
                Debug.Log($"[ReplyReader] Grounded with web searches: [{asked}]");
            }

            Transcription heard = content.InputTranscription;
            if (heard != null) Transcripts.Heard(heard.Text, heard.Finished == true);

            Transcription said = content.OutputTranscription;
            if (said != null)
            {
                if (!string.IsNullOrEmpty(said.Text)) VoiceSession.Busy = true;
                Transcripts.Said(said.Text, said.Finished == true);
            }

            if (content.Interrupted == true)
            {
                VoiceSession.Busy = false;
                ProtocolLog.Note("<- interrupted");
                Speaker.Flush();
                Transcripts.Interrupted();
                return;
            }

            if (content.TurnComplete == true) EndTurn(content);

            List<Part> parts = content.ModelTurn?.Parts;
            if (parts == null) return;

            VoiceSession.Busy = true;
            foreach (Part part in parts)
            {
                Blob audio = part.InlineData;
                if (audio?.Data != null && audio.Data.Length > 0 &&
                    audio.MimeType != null && audio.MimeType.StartsWith(AudioMimePrefix))
                    Speaker.Write(audio.Data);
            }
        }

        private static void EndTurn(LiveServerContent content)
        {
            VoiceSession.Busy = false;
            ProtocolLog.Note("<- turnComplete");
            if (content.TurnCompleteReason != null) ProtocolLog.Note($"<- turnCompleteReason {content.TurnCompleteReason}");

            Transcripts.EndTurn();
            ToolCallTracker.ResetRounds();
            _ = MainThread.Run(AssistantTurn.Clear);
            if (!VoiceSession.ConsumeShutdownRequest()) return;
            _ = MainThread.Run(() =>
            {
                Menu menu = Scene.Menu;
                if (menu != null) menu.SetAssistantActive(false, AssistantCause.Assistant);
            });
        }
    }
}
