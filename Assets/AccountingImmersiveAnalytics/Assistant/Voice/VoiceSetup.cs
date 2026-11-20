using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant.Live;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Voice
{
    public static class VoiceSetup
    {
        public const string ModelId = "gemini-3.8-live";

        private const string VoiceName = "Charon";
        private const int SilenceMilliseconds = 800;

        private static readonly object _startGate = new object();
        private static Task _starting;

        public static LiveClient Client { get; private set; }
        public static LiveConnectConfig Config { get; private set; }
        public static int PromptChars { get; private set; }
        public static int ToolCount => Config?.Tools?.Sum(t => t.FunctionDeclarations?.Count ?? 0) ?? 0;

        public static Task EnsureStarted(bool webSearch)
        {
            lock (_startGate)
            {
                if (_starting == null) _starting = Start(webSearch);
                return _starting;
            }
        }

        private static async Task Start(bool webSearch)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Task<string> key = GeminiKey.Load();
                Task<bool> permission = MicrophoneStream.RequestPermission();
                Client = new LiveClient(await key);
                VoiceSession.StartFresh();
                Config = BuildConfig(webSearch);

                bool granted = await permission;
                if (!granted)
                {
                    Debug.LogWarning("[VoiceSetup] Microphone permission denied; voice input disabled.");
                    VoiceSession.Status = VoiceStatus.MicDenied;
                }

                ScenePush.Start();
                MicrophoneStream.Start(granted);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                VoiceSession.Status = VoiceStatus.Failed;
            }
            clock.Stop();
            Debug.Log($"[VoiceSetup] Start-up took {clock.ElapsedMilliseconds} ms.");
        }

        private static LiveConnectConfig BuildConfig(bool webSearch)
        {
            var tools = new List<Tool>();
            if (webSearch) tools.Add(new Tool { GoogleSearch = new GoogleSearch() });
            tools.Add(new Tool { FunctionDeclarations = SystemPrompt.ToolDeclarations() });

            string instruction = SystemPrompt.PromptBody(webSearch) + SystemPrompt.PromptTail();
            PromptChars = instruction.Length;

            return new LiveConnectConfig
            {
                SystemInstruction = new Content
                {
                    Parts = new List<Part> { new Part { Text = instruction } }
                },
                ContextWindowCompression = new ContextWindowCompressionConfig
                {
                    TriggerTokens = ContextBudget.ServerTrimTrigger,
                    SlidingWindow = new SlidingWindow { TargetTokens = ContextBudget.ServerTrimTarget }
                },
                ResponseModalities = new List<Modality> { Modality.Audio },
                SpeechConfig = new SpeechConfig
                {
                    VoiceConfig = new VoiceConfig
                    {
                        PrebuiltVoiceConfig = new PrebuiltVoiceConfig { VoiceName = VoiceName }
                    }
                },
                Tools = tools,
                RealtimeInputConfig = new RealtimeInputConfig
                {
                    TurnCoverage = TurnCoverage.TurnIncludesOnlyActivity,
                    AutomaticActivityDetection = new AutomaticActivityDetection
                    {
                        StartOfSpeechSensitivity = StartSensitivity.StartSensitivityLow,
                        EndOfSpeechSensitivity = EndSensitivity.EndSensitivityLow,
                        SilenceDurationMs = SilenceMilliseconds
                    }
                },
                InputAudioTranscription = new AudioTranscriptionConfig(),
                OutputAudioTranscription = new AudioTranscriptionConfig()
            };
        }
    }
}
