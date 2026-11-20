using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AccountingImmersiveAnalytics.UI.Charts.Scales;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Text
{
    public static class Numbers
    {
        private static readonly Regex Token = new Regex(
            @"(?<![\p{L}\p{N}_.,])(-?)(\d{1,3}(?:,\d{3})+|\d+)(\.\d+)?([kMBT])?(%)?(?![\p{L}\p{N}_])", RegexOptions.CultureInvariant);

        private static readonly Regex OnlyNumber = new Regex(
            @"^\s*-?(\d{1,3}(?:,\d{3})+|\d+)(\.\d+)?[kMBT]?%?\s*$", RegexOptions.CultureInvariant);

        private static readonly Dictionary<string, (string template, string[] tokens)> Templates =
            new Dictionary<string, (string, string[])>();

        public static NumberStyle Style =>
            NumberFormats.ByLanguage.TryGetValue(Language.Current, out NumberStyle style) ? style
            : NumberFormats.ByLanguage.TryGetValue(Language.English, out NumberStyle english) ? english : NumberStyle.Invariant;

        public static string Shown(double value, double step) => AxisFormat.Number(value, step, Style);

        public static string Shown(double value) => AxisFormat.Value(value, Style);

        public static string Count(long count) => AxisFormat.Plain(count, 1d, Style);

        public static string Whole(double value) => AxisFormat.Whole(value, Style);

        public static bool IsNumber(string text) => !string.IsNullOrEmpty(text) && OnlyNumber.IsMatch(text);

        public static string Text(string english)
        {
            if (string.IsNullOrEmpty(english)) return "";
            if (IsNumber(english)) return Token.Replace(english, Localize);

            (string template, string[] tokens) = TemplateOf(english);
            if (tokens.Length == 0) return Translator.Get(english);

            string translated = Translator.Get(template);
            for (int i = 0; i < tokens.Length; i++)
                if (translated.IndexOf("{" + i + "}", StringComparison.Ordinal) < 0) { translated = template; break; }
            var shown = new StringBuilder(translated);
            for (int i = 0; i < tokens.Length; i++) shown.Replace("{" + i + "}", Localize(Token.Match(tokens[i])));
            return shown.ToString();
        }

        private static (string, string[]) TemplateOf(string english)
        {
            if (Templates.TryGetValue(english, out (string, string[]) known)) return known;
            var tokens = new List<string>();
            string template = english.IndexOf('{') >= 0 || english.IndexOf('}') >= 0
                ? english
                : Token.Replace(english, m =>
                {
                    tokens.Add(m.Value);
                    return "{" + (tokens.Count - 1) + "}";
                });
            var made = (template, tokens.ToArray());
            if (Templates.Count > 4096) Templates.Clear();
            Templates[english] = made;
            return made;
        }

        private static string Localize(Match m)
        {
            NumberStyle style = Style;
            string sign = m.Groups[1].Value, digits = m.Groups[2].Value, fraction = m.Groups[3].Value, suffix = m.Groups[4].Value;
            bool percent = m.Groups[5].Success && m.Groups[5].Value.Length > 0;
            string shown;
            if (digits.IndexOf(',') < 0 && fraction.Length == 0 && suffix.Length == 0)
            {
                shown = (sign.Length > 0 ? style.Minus : "") + style.Native(digits);
            }
            else
            {
                if (!double.TryParse(digits.Replace(",", "") + fraction, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                    return m.Value;
                double size = suffix == "k" ? 1e3 : suffix == "M" ? 1e6 : suffix == "B" ? 1e9 : suffix == "T" ? 1e12 : 1d;
                int decimals = fraction.Length > 0 ? fraction.Length - 1 : 0;
                double value = (sign.Length > 0 ? -number : number) * size;
                double step = Math.Pow(10d, -decimals) * size;
                shown = suffix.Length > 0 ? AxisFormat.Number(value, step, style) : AxisFormat.Plain(value, step, style);
            }
            return percent ? style.Percent(shown) : shown;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Templates.Clear();
    }
}
