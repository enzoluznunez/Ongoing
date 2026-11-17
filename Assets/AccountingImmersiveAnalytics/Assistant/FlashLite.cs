using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant.Live;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AccountingImmersiveAnalytics.Assistant
{
    public static class FlashLite
    {
        public const string ModelId = "gemini-3.5-flash-lite";

        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";
        private const int DefaultTimeoutSeconds = 15;

        public static bool Reached { get; private set; }

        public static Task<JObject> Ask(string instructions, string prompt, JObject schema, int timeoutSeconds = DefaultTimeoutSeconds) =>
            Post(BuildRequest(instructions, new JArray(new JObject { ["text"] = prompt }), schema), timeoutSeconds);

        private static Task<JObject> Post(string body, int timeoutSeconds)
        {
            var done = new TaskCompletionSource<JObject>();
            WithKey(apiKey =>
            {
                if (string.IsNullOrEmpty(apiKey)) done.SetResult(null);
                else Send(apiKey, body, timeoutSeconds, done);
            });
            return done.Task;
        }

        private static async void WithKey(Action<string> then) => then(await Gemini.ApiKey());

        private static void Send(string apiKey, string body, int timeoutSeconds, TaskCompletionSource<JObject> done)
        {
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
                else Debug.LogWarning($"[FlashLite] {request.responseCode} {request.error}: {request.downloadHandler?.text}");
                request.Dispose();
                Reached = answer != null;
                done.SetResult(answer);
            };
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
                Debug.LogWarning($"[FlashLite] could not read the answer: {e.Message}");
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
