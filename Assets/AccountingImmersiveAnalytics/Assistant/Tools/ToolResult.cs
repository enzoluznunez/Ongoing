using System.Collections.Generic;

namespace AccountingImmersiveAnalytics.Assistant.Tools
{
    public static class ToolResult
    {
        private static readonly string[] RefusalKeys = { "error", "preconditionUnmet", "needsChoice" };

        public static bool IsRefusal(Dictionary<string, object> result)
        {
            foreach (string key in RefusalKeys)
                if (result.ContainsKey(key)) return true;
            return false;
        }

        public static void Refuse(Dictionary<string, object> result, string unmet, string message)
        {
            result["preconditionUnmet"] = unmet;
            result["message"] = message;
        }

        public static void NeedChoice(Dictionary<string, object> result, string what,
            IReadOnlyList<string> options, string message)
        {
            result["needsChoice"] = what;
            if (options != null && options.Count > 0) result["options"] = new List<object>(options);
            result["message"] = message;
        }
    }
}
