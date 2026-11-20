using System.Collections.Generic;
using System.Text;

namespace AccountingImmersiveAnalytics.Text
{
    public static class Markers
    {
        private const char Open = '⟦';
        private const char Close = '⟧';

        public static string Signature(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf(Open) < 0) return "";
            var styles = new List<string>();
            var sealedOnes = new List<string>();
            int closes = 0;
            foreach ((string inside, int _, int _) in Find(text))
            {
                if (inside == "/") closes++;
                else if (inside.StartsWith("#")) sealedOnes.Add(inside);
                else styles.Add(inside);
            }
            styles.Sort(System.StringComparer.Ordinal);
            sealedOnes.Sort(System.StringComparer.Ordinal);
            return string.Join(",", styles) + "|" + closes + "|" + string.Join(",", sealedOnes);
        }

        public static string KeepSealed(string text)
        {
            if (string.IsNullOrEmpty(text) || (text.IndexOf(Open) < 0 && text.IndexOf(Close) < 0)) return text;
            var kept = new StringBuilder(text.Length);
            int at = 0;
            foreach ((string inside, int start, int end) in Find(text))
            {
                kept.Append(StripChars(text.Substring(at, start - at)));
                if (inside.StartsWith("#")) kept.Append(text, start, end - start);
                at = end;
            }
            kept.Append(StripChars(text.Substring(at)));
            return kept.ToString();
        }

        private static string StripChars(string text) => text.Replace(Open.ToString(), "").Replace(Close.ToString(), "");

        private static IEnumerable<(string inside, int start, int end)> Find(string text)
        {
            int at = 0;
            while (at < text.Length)
            {
                int open = text.IndexOf(Open, at);
                if (open < 0) yield break;
                int close = text.IndexOf(Close, open + 1);
                if (close < 0) yield break;
                int nextOpen = text.IndexOf(Open, open + 1);
                if (nextOpen >= 0 && nextOpen < close)
                {
                    at = nextOpen;
                    continue;
                }
                yield return (text.Substring(open + 1, close - open - 1).Trim(), open, close + 1);
                at = close + 1;
            }
        }
    }
}
