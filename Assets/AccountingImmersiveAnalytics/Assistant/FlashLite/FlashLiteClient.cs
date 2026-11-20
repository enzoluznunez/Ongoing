using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant.Live;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AccountingImmersiveAnalytics.Assistant.FlashLite
{
    public static class FlashLiteClient
    {
        public const string ModelId = "gemini-3.5-flash-lite";

        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";
        private const int DefaultTimeoutSeconds = 15;

        public static bool Reached { get; private set; }

        public static Task<JObject> Ask(string instructions, string prompt, JObject schema, int timeoutSeconds = DefaultTimeoutSeconds) =>
            Post(BuildRequest(instructions, new JArray(new JObject { ["text"] = prompt }), schema), timeoutSeconds);

        public static async Task<JObject> AskOnce(string instructions, string prompt, JObject schema, int timeoutSeconds = DefaultTimeoutSeconds)
        {
            string body = BuildRequest(instructions, new JArray(new JObject { ["text"] = prompt }), schema);
            string key = AnswerCache.KeyFor(ModelId + "\n" + body);
            string saved = await AnswerCache.Find(key);
            JObject remembered = ReadSaved(saved);
            if (remembered != null) return remembered;
            JObject answer = await Post(body, timeoutSeconds);
            if (answer != null) AnswerCache.Store(key, answer.ToString(Newtonsoft.Json.Formatting.None));
            return answer;
        }

        private static JObject ReadSaved(string saved)
        {
            if (string.IsNullOrEmpty(saved)) return null;
            try
            {
                return JObject.Parse(saved);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static async Task<JObject> Post(string body, int timeoutSeconds)
        {
            try
            {
                string apiKey = await GeminiKey.Load();
                if (string.IsNullOrEmpty(apiKey)) return null;
                return await Send(apiKey, body, timeoutSeconds);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FlashLiteClient] The request failed: {e.Message}");
                return null;
            }
        }

        private static Task<JObject> Send(string apiKey, string body, int timeoutSeconds)
        {
            var done = new TaskCompletionSource<JObject>();
            var request = new UnityWebRequest(string.Format(Endpoint, ModelId), "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = timeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-goog-api-key", apiKey);

            request.SendWebRequest().completed += _ =>
            {
                JObject answer = null;
                if (request.result == UnityWebRequest.Result.Success) answer = Parse(request.downloadHandler.text);
                else Debug.LogWarning($"[FlashLiteClient] {request.responseCode} {request.error}: {request.downloadHandler?.text}");
                request.Dispose();
                Reached = answer != null;
                done.SetResult(answer);
            };
            return done.Task;
        }

        private static JObject Parse(string response)
        {
            try
            {
                JObject root = JObject.Parse(response);
                string text = (string)root.SelectToken("candidates[0].content.parts[0].text");
                return string.IsNullOrEmpty(text) ? null : JObject.Parse(text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FlashLiteClient] could not read the answer: {e.Message}");
                return null;
            }
        }

        private static string BuildRequest(string instructions, JArray parts, JObject schema)
        {
            var request = new JObject
            {
                ["systemInstruction"] = new JObject
                {
                    ["parts"] = new JArray(new JObject { ["text"] = instructions })
                },
                ["contents"] = new JArray(new JObject
                {
                    ["role"] = "user",
                    ["parts"] = parts
                }),
                ["generationConfig"] = new JObject
                {
                    ["responseMimeType"] = "application/json",
                    ["responseSchema"] = schema,
                    ["thinkingConfig"] = new JObject { ["thinkingLevel"] = "low" }
                }
            };
            return request.ToString(Newtonsoft.Json.Formatting.None);
        }

        public static JObject Schema(params (string name, string type)[] fields)
        {
            var properties = new JObject();
            var required = new JArray();
            foreach ((string name, string type) in fields)
            {
                properties[name] = type.EndsWith("_LIST")
                    ? new JObject { ["type"] = "ARRAY", ["items"] = new JObject { ["type"] = type.Substring(0, type.Length - 5) } }
                    : new JObject { ["type"] = type };
                required.Add(name);
            }
            return new JObject { ["type"] = "OBJECT", ["properties"] = properties, ["required"] = required };
        }
    }
}
