using System.Collections.Generic;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Text
{
    public static class Strings
    {
        private static readonly Dictionary<string, string> Table = new Dictionary<string, string>
        {
            ["loading"] = "Loading",
            ["panel.documents"] = "Documents",
            ["documents.loading"] = "Loading the document",
            ["documents.failed"] = "The document could not be loaded",
            ["documents.notShown"] = "The document could not be displayed",
            ["documents.empty"] = "There is no document to display",
            ["documents.minutes"] = "{0}-minute read",
            ["documents.page"] = "1 page",
            ["documents.pages"] = "{0} pages",
            ["assistant"] = "Assistant",
            ["assistant.connecting"] = "Connecting",
            ["loading.graphics"] = "Preparing the graphics",
            ["sort.none"] = "Default",
            ["sort.ascending"] = "Lowest first",
            ["sort.descending"] = "Highest first",
            ["sort.by"] = "By {0}",
            ["filter.showing"] = "Showing {0} of {1} rows",
            ["filter.leftOut"] = "Not shown because they are 0 or below: {0}",
            ["trend.name"] = "Trend",
            ["trend.needsAbove"] = "{0} needs every {1} above 0",
            ["trend.needsBoth"] = "{0} needs every {1} and {2} above 0",
            ["trend.needsMore"] = "{0} needs at least {1} different {2}",
            ["trend.needsPoints"] = "{0} needs at least {1} points",
            ["trend.None"] = "Off",
            ["trend.Linear"] = "Linear",
            ["trend.Exponential"] = "Exponential",
            ["trend.Logarithmic"] = "Logarithmic",
            ["trend.Polynomial"] = "Polynomial",
            ["trend.Power"] = "Power",
            ["trend.Smooth"] = "Smooth",
            ["edits.empty"] = "No edits yet",
            ["edits.undo"] = "Undo",
            ["edits.you"] = "You",
            ["notice.micDenied"] = "Microphone access is denied, so the assistant cannot listen.",
            ["notice.disconnected"] = "The assistant could not stay connected and turned itself off.",
            ["notice.nearlyFull.body"] = "The assistant is close to the limit of what it can hold in mind. It will reset itself shortly and start over from a blank slate.",
            ["notice.full.body"] = "The assistant ran out of room to hold the conversation and stopped. Tap the Assistant button on your wrist to start over.",
            ["notice.stopped.one"] = "One change was already made. Undo takes it back.",
            ["notice.stopped.many"] = "{0} changes were already made. Undo takes them back one at a time."
        };

        public static string Original(string key)
        {
            if (Table.TryGetValue(key, out string text)) return text;
            Debug.LogWarning($"[Strings] No text for '{key}'.");
            return key;
        }

        public static string Get(string key) => Translator.Get(Original(key));

        public static string Format(string key, params object[] args)
        {
            var shown = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
                shown[i] = args[i] switch
                {
                    int n => Numbers.Count(n),
                    long n => Numbers.Count(n),
                    double v => Numbers.Shown(v),
                    float v => Numbers.Shown(v),
                    _ => args[i]
                };
            return string.Format(Get(key), shown);
        }

        public static string FormatOriginal(string key, params object[] args) => string.Format(Original(key), args);
    }
}
