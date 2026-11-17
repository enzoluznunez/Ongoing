using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Tools;

namespace AccountingImmersiveAnalytics.Assistant.Live {

    public enum Type { String, Number, Integer, Boolean, Array, Object }

    public enum Behavior { Blocking, NonBlocking }

    public enum FunctionResponseScheduling { Silent, WhenIdle, Interrupt }

    public enum Modality { Audio, Text }

    public enum TurnCoverage { TurnIncludesOnlyActivity, TurnIncludesAllInput }

    public enum StartSensitivity { StartSensitivityHigh, StartSensitivityLow }

    public enum EndSensitivity { EndSensitivityHigh, EndSensitivityLow }

    public sealed class Schema {
        public Type Type;
        public string Description;
        public List<string> Enum;
        public Dictionary<string, Schema> Properties;
        public List<string> Required;
        public Schema Items;
        public double? Minimum;
        public double? Maximum;
        public object Default;
    }

    public sealed class FunctionDeclaration {
        public string Name;
        public string Description;
        public Schema Parameters;
        public Schema Response;
        public Behavior? Behavior;
    }

    public sealed class GoogleSearch { }

    public sealed class Tool {
        public GoogleSearch GoogleSearch;
        public List<FunctionDeclaration> FunctionDeclarations;
    }

    public sealed class Blob {
        public string MimeType;
        public byte[] Data;
    }

    public sealed class Part {
        public string Text;
        public Blob InlineData;
    }

    public sealed class Content {
        public string Role;
        public List<Part> Parts;
    }

    public sealed class FunctionCall {
        public string Id;
        public string Name;
        public Dictionary<string, object> Args;
    }

    public sealed class FunctionResponse {
        public string Id;
        public string Name;
        public Dictionary<string, object> Response;
        public FunctionResponseScheduling? Scheduling;
    }

    public sealed class ContextWindowCompressionConfig {
        public long TriggerTokens;
        public SlidingWindow SlidingWindow;
    }

    public sealed class SlidingWindow {
        public long TargetTokens;
    }

    public sealed class PrebuiltVoiceConfig {
        public string VoiceName;
    }

    public sealed class VoiceConfig {
        public PrebuiltVoiceConfig PrebuiltVoiceConfig;
    }

    public sealed class SpeechConfig {
        public string LanguageCode;
        public VoiceConfig VoiceConfig;
    }

    public sealed class AutomaticActivityDetection {
        public StartSensitivity? StartOfSpeechSensitivity;
        public EndSensitivity? EndOfSpeechSensitivity;
        public int? SilenceDurationMs;
    }

    public sealed class RealtimeInputConfig {
        public TurnCoverage? TurnCoverage;
        public AutomaticActivityDetection AutomaticActivityDetection;
    }

    public sealed class AudioTranscriptionConfig {
        public List<string> LanguageCodes;
    }

    public sealed class SessionResumptionConfig {
        public string Handle;
    }

    public sealed class LiveConnectConfig {
        public Content SystemInstruction;
        public ContextWindowCompressionConfig ContextWindowCompression;
        public List<Modality> ResponseModalities;
        public SpeechConfig SpeechConfig;
        public List<Tool> Tools;
        public RealtimeInputConfig RealtimeInputConfig;
        public AudioTranscriptionConfig InputAudioTranscription;
        public AudioTranscriptionConfig OutputAudioTranscription;
        public SessionResumptionConfig SessionResumption;
    }

    public sealed class LiveSendRealtimeInputParameters {
        public Blob Audio;
    }

    public sealed class LiveSendClientContentParameters {
        public List<Content> Turns;
        public bool? TurnComplete;
    }

    public sealed class LiveSendToolResponseParameters {
        public List<FunctionResponse> FunctionResponses;
    }

    public sealed class Transcription {
        public string Text;
        public bool? Finished;
    }

    public sealed class GroundingMetadata {
        public List<string> WebSearchQueries;
    }

    public sealed class LiveServerContent {
        public Content ModelTurn;
        public bool? TurnComplete;
        public bool? Interrupted;
        public string TurnCompleteReason;
        public Transcription InputTranscription;
        public Transcription OutputTranscription;
        public GroundingMetadata GroundingMetadata;
    }

    public sealed class LiveServerSetupComplete { }

    public sealed class LiveServerToolCall {
        public List<FunctionCall> FunctionCalls;
    }

    public sealed class LiveServerToolCallCancellation {
        public List<string> Ids;
    }

    public sealed class LiveServerGoAway {
        public string TimeLeft;
    }

    public sealed class LiveServerSessionResumptionUpdate {
        public string NewHandle;
        public bool? Resumable;
    }

    public sealed class ModalityTokenCount {
        public string Modality;
        public int? TokenCount;
    }

    public sealed class UsageMetadata {
        public int? PromptTokenCount;
        public int? CachedContentTokenCount;
        public int? ResponseTokenCount;
        public int? ThoughtsTokenCount;
        public int? TotalTokenCount;
        public List<ModalityTokenCount> PromptTokensDetails;
    }

    public sealed class LiveServerMessage {
        public LiveServerSetupComplete SetupComplete;
        public LiveServerContent ServerContent;
        public LiveServerToolCall ToolCall;
        public LiveServerToolCallCancellation ToolCallCancellation;
        public LiveServerGoAway GoAway;
        public LiveServerSessionResumptionUpdate SessionResumptionUpdate;
        public UsageMetadata UsageMetadata;
    }
}
