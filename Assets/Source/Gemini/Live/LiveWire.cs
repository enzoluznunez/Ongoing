using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeminiLive {

    public static class LiveWire {

        public static string ToJson(object value) => ToToken(value).ToString(Formatting.None);

        public static string Text(object value) {
            switch (value) {
                case null: return null;
                case string s: return s;
                case bool b: return b ? "true" : "false";
                case IFormattable f: return f.ToString(null, CultureInfo.InvariantCulture);
                default: return value.ToString();
            }
        }

        public static JToken ToToken(object value) {
            switch (value) {
                case null: return JValue.CreateNull();
                case JToken token: return token;
                case string s: return new JValue(s);
                case bool b: return new JValue(b);
                case byte n: return new JValue((long)n);
                case int n: return new JValue((long)n);
                case long n: return new JValue(n);
                case float n: return new JValue((double)n);
                case double n: return new JValue(n);
                case decimal n: return new JValue(n);
                case IDictionary map: {
                    var result = new JObject();
                    foreach (DictionaryEntry entry in map) result[Text(entry.Key)] = ToToken(entry.Value);
                    return result;
                }
                case IEnumerable list: {
                    var result = new JArray();
                    foreach (object item in list) result.Add(ToToken(item));
                    return result;
                }
                default: return new JValue(Text(value));
            }
        }

        public static JObject Parse(string json) {
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                return JObject.Load(reader);
        }

        public static object ToPlain(JToken token) {
            switch (token) {
                case null: return null;
                case JObject map: {
                    var result = new Dictionary<string, object>(map.Count);
                    foreach (JProperty property in map.Properties()) result[property.Name] = ToPlain(property.Value);
                    return result;
                }
                case JArray list: {
                    var result = new List<object>(list.Count);
                    foreach (JToken item in list) result.Add(ToPlain(item));
                    return result;
                }
                case JValue value: return value.Value;
                default: return token.ToString();
            }
        }

        public static string Setup(string model, LiveConnectConfig config) {
            var generation = new JObject();
            if (config.ResponseModalities != null)
                generation["responseModalities"] = new JArray(config.ResponseModalities.Select(m => (object)m.ToString().ToUpperInvariant()));
            if (config.SpeechConfig != null) generation["speechConfig"] = Speech(config.SpeechConfig);

            var setup = new JObject {
                ["model"] = model.StartsWith("models/", StringComparison.Ordinal) ? model : "models/" + model,
                ["generationConfig"] = generation
            };
            if (config.SystemInstruction != null) setup["systemInstruction"] = ContentToken(config.SystemInstruction);
            if (config.Tools != null) setup["tools"] = new JArray(config.Tools.Select(Tool));
            if (config.RealtimeInputConfig != null) setup["realtimeInputConfig"] = RealtimeInput(config.RealtimeInputConfig);
            if (config.SessionResumption != null) {
                var resumption = new JObject();
                if (config.SessionResumption.Handle != null) resumption["handle"] = config.SessionResumption.Handle;
                setup["sessionResumption"] = resumption;
            }
            if (config.ContextWindowCompression != null) {
                var compression = new JObject { ["triggerTokens"] = config.ContextWindowCompression.TriggerTokens };
                if (config.ContextWindowCompression.SlidingWindow != null)
                    compression["slidingWindow"] = new JObject { ["targetTokens"] = config.ContextWindowCompression.SlidingWindow.TargetTokens };
                setup["contextWindowCompression"] = compression;
            }
            if (config.InputAudioTranscription != null) setup["inputAudioTranscription"] = new JObject();
            if (config.OutputAudioTranscription != null) setup["outputAudioTranscription"] = new JObject();

            return new JObject { ["setup"] = setup }.ToString(Formatting.None);
        }

        public static string RealtimeAudio(LiveSendRealtimeInputParameters input) =>
            new JObject {
                ["realtimeInput"] = new JObject { ["audio"] = BlobToken(input.Audio) }
            }.ToString(Formatting.None);

        public static string ClientContent(LiveSendClientContentParameters content) {
            var body = new JObject { ["turns"] = new JArray(content.Turns.Select(ContentToken)) };
            if (content.TurnComplete.HasValue) body["turnComplete"] = content.TurnComplete.Value;
            return new JObject { ["clientContent"] = body }.ToString(Formatting.None);
        }

        public static string ToolResponse(LiveSendToolResponseParameters response) {
            var list = new JArray();
            foreach (FunctionResponse item in response.FunctionResponses) {
                var token = new JObject {
                    ["id"] = item.Id,
                    ["name"] = item.Name,
                    ["response"] = ToToken(item.Response)
                };
                if (item.Scheduling.HasValue) token["scheduling"] = Scheduling(item.Scheduling.Value);
                list.Add(token);
            }
            return new JObject { ["toolResponse"] = new JObject { ["functionResponses"] = list } }.ToString(Formatting.None);
        }

        public static LiveServerMessage Message(JObject root) {
            var message = new LiveServerMessage();
            if (root["setupComplete"] != null) message.SetupComplete = new LiveServerSetupComplete();
            if (root["serverContent"] is JObject content) message.ServerContent = ServerContent(content);
            if (root["toolCall"] is JObject call) {
                message.ToolCall = new LiveServerToolCall { FunctionCalls = new List<FunctionCall>() };
                if (call["functionCalls"] is JArray calls)
                    foreach (JToken item in calls)
                        message.ToolCall.FunctionCalls.Add(new FunctionCall {
                            Id = (string)item["id"],
                            Name = (string)item["name"],
                            Args = ToPlain(item["args"]) as Dictionary<string, object> ?? new Dictionary<string, object>()
                        });
            }
            if (root["toolCallCancellation"] is JObject cancellation)
                message.ToolCallCancellation = new LiveServerToolCallCancellation { Ids = Strings(cancellation["ids"]) };
            if (root["goAway"] is JObject goAway)
                message.GoAway = new LiveServerGoAway { TimeLeft = (string)goAway["timeLeft"] };
            if (root["sessionResumptionUpdate"] is JObject resume)
                message.SessionResumptionUpdate = new LiveServerSessionResumptionUpdate {
                    NewHandle = (string)resume["newHandle"],
                    Resumable = (bool?)resume["resumable"]
                };
            if (root["usageMetadata"] is JObject usage) message.UsageMetadata = Usage(usage);
            return message;
        }

        private static LiveServerContent ServerContent(JObject content) {
            var result = new LiveServerContent {
                TurnComplete = (bool?)content["turnComplete"],
                Interrupted = (bool?)content["interrupted"],
                TurnCompleteReason = (string)content["turnCompleteReason"],
                InputTranscription = TranscriptionOf(content["inputTranscription"]),
                OutputTranscription = TranscriptionOf(content["outputTranscription"])
            };
            if (content["groundingMetadata"] is JObject grounding)
                result.GroundingMetadata = new GroundingMetadata { WebSearchQueries = Strings(grounding["webSearchQueries"]) };
            if (content["modelTurn"] is JObject turn) {
                result.ModelTurn = new Content { Role = (string)turn["role"], Parts = new List<Part>() };
                if (turn["parts"] is JArray parts)
                    foreach (JToken part in parts) result.ModelTurn.Parts.Add(PartOf(part));
            }
            return result;
        }

        private static Part PartOf(JToken part) {
            var result = new Part { Text = (string)part["text"] };
            if (part["inlineData"] is JObject inline)
                result.InlineData = new Blob {
                    MimeType = (string)inline["mimeType"],
                    Data = inline["data"] != null ? Convert.FromBase64String((string)inline["data"]) : null
                };
            return result;
        }

        private static Transcription TranscriptionOf(JToken token) =>
            token is JObject map ? new Transcription { Text = (string)map["text"], Finished = (bool?)map["finished"] } : null;

        private static UsageMetadata Usage(JObject usage) {
            var result = new UsageMetadata {
                PromptTokenCount = (int?)usage["promptTokenCount"],
                CachedContentTokenCount = (int?)usage["cachedContentTokenCount"],
                ResponseTokenCount = (int?)usage["responseTokenCount"],
                ThoughtsTokenCount = (int?)usage["thoughtsTokenCount"],
                TotalTokenCount = (int?)usage["totalTokenCount"]
            };
            if (usage["promptTokensDetails"] is JArray details) {
                result.PromptTokensDetails = new List<ModalityTokenCount>(details.Count);
                foreach (JToken item in details)
                    result.PromptTokensDetails.Add(new ModalityTokenCount {
                        Modality = (string)item["modality"],
                        TokenCount = (int?)item["tokenCount"]
                    });
            }
            return result;
        }

        private static List<string> Strings(JToken token) {
            var result = new List<string>();
            if (token is JArray list)
                foreach (JToken item in list) result.Add((string)item);
            return result;
        }

        private static JToken Speech(SpeechConfig speech) {
            var result = new JObject();
            if (speech.LanguageCode != null) result["languageCode"] = speech.LanguageCode;
            if (speech.VoiceConfig?.PrebuiltVoiceConfig != null)
                result["voiceConfig"] = new JObject {
                    ["prebuiltVoiceConfig"] = new JObject { ["voiceName"] = speech.VoiceConfig.PrebuiltVoiceConfig.VoiceName }
                };
            return result;
        }

        private static JToken RealtimeInput(RealtimeInputConfig input) {
            var result = new JObject();
            if (input.TurnCoverage.HasValue)
                result["turnCoverage"] = input.TurnCoverage.Value == TurnCoverage.TurnIncludesOnlyActivity
                    ? "TURN_INCLUDES_ONLY_ACTIVITY"
                    : "TURN_INCLUDES_ALL_INPUT";
            AutomaticActivityDetection detection = input.AutomaticActivityDetection;
            if (detection != null) {
                var token = new JObject();
                if (detection.StartOfSpeechSensitivity.HasValue)
                    token["startOfSpeechSensitivity"] = detection.StartOfSpeechSensitivity.Value == StartSensitivity.StartSensitivityLow
                        ? "START_SENSITIVITY_LOW"
                        : "START_SENSITIVITY_HIGH";
                if (detection.EndOfSpeechSensitivity.HasValue)
                    token["endOfSpeechSensitivity"] = detection.EndOfSpeechSensitivity.Value == EndSensitivity.EndSensitivityLow
                        ? "END_SENSITIVITY_LOW"
                        : "END_SENSITIVITY_HIGH";
                if (detection.SilenceDurationMs.HasValue) token["silenceDurationMs"] = detection.SilenceDurationMs.Value;
                result["automaticActivityDetection"] = token;
            }
            return result;
        }

        private static JToken Tool(Tool tool) {
            var result = new JObject();
            if (tool.GoogleSearch != null) result["googleSearch"] = new JObject();
            if (tool.FunctionDeclarations != null)
                result["functionDeclarations"] = new JArray(tool.FunctionDeclarations.Select(Declaration));
            return result;
        }

        private static JToken Declaration(FunctionDeclaration declaration) {
            var result = new JObject { ["name"] = declaration.Name };
            if (declaration.Description != null) result["description"] = declaration.Description;
            if (declaration.Parameters != null) result["parameters"] = SchemaToken(declaration.Parameters);
            if (declaration.Response != null) result["response"] = SchemaToken(declaration.Response);
            if (declaration.Behavior.HasValue)
                result["behavior"] = declaration.Behavior.Value == Behavior.NonBlocking ? "NON_BLOCKING" : "BLOCKING";
            return result;
        }

        private static JToken SchemaToken(Schema schema) {
            var result = new JObject { ["type"] = schema.Type.ToString().ToUpperInvariant() };
            if (schema.Description != null) result["description"] = schema.Description;
            if (schema.Enum != null) result["enum"] = new JArray(schema.Enum.Cast<object>());
            if (schema.Properties != null) {
                var properties = new JObject();
                foreach (KeyValuePair<string, Schema> pair in schema.Properties) properties[pair.Key] = SchemaToken(pair.Value);
                result["properties"] = properties;
            }
            if (schema.Required != null) result["required"] = new JArray(schema.Required.Cast<object>());
            if (schema.Items != null) result["items"] = SchemaToken(schema.Items);
            if (schema.Minimum.HasValue) result["minimum"] = schema.Minimum.Value;
            if (schema.Maximum.HasValue) result["maximum"] = schema.Maximum.Value;
            if (schema.Default != null) result["default"] = ToToken(schema.Default);
            return result;
        }

        private static JToken ContentToken(Content content) => ToToken(new Dictionary<string, object> {
            { "role", content.Role },
            { "parts", content.Parts.Select(PartToken).ToList() }
        });

        private static JToken PartToken(Part part) {
            var result = new JObject();
            if (part.Text != null) result["text"] = part.Text;
            if (part.InlineData != null) result["inlineData"] = BlobToken(part.InlineData);
            return result;
        }

        private static JToken BlobToken(Blob blob) => new JObject {
            ["mimeType"] = blob.MimeType,
            ["data"] = Convert.ToBase64String(blob.Data)
        };

        private static string Scheduling(FunctionResponseScheduling scheduling) {
            switch (scheduling) {
                case FunctionResponseScheduling.Silent: return "SILENT";
                case FunctionResponseScheduling.Interrupt: return "INTERRUPT";
                default: return "WHEN_IDLE";
            }
        }
    }
}
