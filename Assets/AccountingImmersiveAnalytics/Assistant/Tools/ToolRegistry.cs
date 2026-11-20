using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Voice;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Tools
{
    public static class ToolRegistry
    {
        private const int LoggedResponseBytes = 400;

        private static Dictionary<string, AssistantTool> _tools;

        public static IReadOnlyDictionary<string, AssistantTool> Tools => _tools;

        public static async Task Run(LiveSession session, FunctionCall call)
        {
            Dictionary<string, object> result;
            if (!_tools.TryGetValue(call.Name, out AssistantTool tool))
            {
                Debug.LogWarning($"[ToolRegistry] Unknown tool: {call.Name}");
                result = new Dictionary<string, object>
                {
                    { "error", $"There is no tool called '{call.Name}'." },
                    { "tools", new List<object>(_tools.Keys) }
                };
            }
            else
            {
                try
                {
                    result = await tool.Execute(call.Args).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ToolRegistry] {call.Name} failed: {e}");
                    result = new Dictionary<string, object> { { "error", e.Message } };
                }
            }

            if (ToolCallTracker.ConsumeCancelled(call.Id))
            {
                Debug.LogWarning($"[ToolRegistry] {call.Name} id={call.Id} was cancelled by the server (turn interrupted); suppressing its tool response.");
                ToolCallTracker.Settled(call.Id);
                return;
            }

            try
            {
                await Respond(session, call, result).ConfigureAwait(false);
                ToolCallTracker.Settled(call.Id);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ToolRegistry] Sending the tool response failed: {e}");
                if (VoiceSession.Status != VoiceStatus.Live) return;
                try
                {
                    ToolCallTracker.Settled(call.Id);
                    await Respond(session, call, new Dictionary<string, object>
                    {
                        { "error", "The tool result could not be delivered. The action may still have applied; " +
                                   "check the current state before retrying." }
                    }).ConfigureAwait(false);
                }
                catch (Exception fallback)
                {
                    Debug.LogError($"[ToolRegistry] Sending the fallback tool response failed: {fallback}");
                }
            }
        }

        public static string BriefJson(object value, int maxBytes, out long totalBytes)
        {
            string json = LiveWire.ToJson(value);
            totalBytes = System.Text.Encoding.UTF8.GetByteCount(json);
            return json.Length > maxBytes ? json.Substring(0, maxBytes) : json;
        }

        private static Task Respond(LiveSession session, FunctionCall call, Dictionary<string, object> result)
        {
            try
            {
                string head = BriefJson(result, LoggedResponseBytes, out long totalBytes);
                ProtocolLog.Note($"toolResponse {call.Name} id={call.Id} {totalBytes}B :: {head}");
            }
            catch (Exception e) { ProtocolLog.Note($"toolResponse {call.Name} SERIALIZE-FAIL {e.Message}"); }
            return session.SendToolResponseAsync(new LiveSendToolResponseParameters
            {
                FunctionResponses = new List<FunctionResponse>
                {
                    new FunctionResponse { Id = call.Id, Name = call.Name, Response = result }
                }
            });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            _tools = new Dictionary<string, AssistantTool>();
            IEnumerable<Type> types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(AssistantTool)) && !t.IsAbstract && !t.IsGenericTypeDefinition);
            foreach (Type type in types)
            {
                AssistantTool tool;
                try { tool = (AssistantTool)Activator.CreateInstance(type); }
                catch (Exception e)
                {
                    Debug.LogError($"[ToolRegistry] {type.Name} could not be constructed and will not be registered: {e.Message}");
                    continue;
                }

                string name = tool.Declaration?.Name;
                if (string.IsNullOrEmpty(name))
                {
                    Debug.LogError($"[ToolRegistry] {type.Name} has no Declaration.Name and will not be registered.");
                    continue;
                }
                if (_tools.ContainsKey(name))
                    Debug.LogError($"[ToolRegistry] Duplicate tool name '{name}'; {type.Name} overwrites the earlier registration.");

                tool.Name = name;
                _tools[name] = tool;
            }
            Debug.Log($"[ToolRegistry] Registered {_tools.Count} tools.");
        }
    }
}
