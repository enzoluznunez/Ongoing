using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AccountingImmersiveAnalytics.UI.Charts.Scales
{
    public static class AxisFormat
    {
        public static string Number(double value, double step) => Number(value, step, NumberStyle.Invariant);

        public static string Value(double value) => Value(value, NumberStyle.Invariant);

        public static string Number(double value, double step, NumberStyle style)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "";
            double magnitude = Math.Abs(value);
            foreach (NumberUnit unit in style.Units)
            {
                if (magnitude < unit.Threshold) continue;
                if (unit.Divisor <= 1d) break;
                string digits = Plain(value / unit.Divisor, step / unit.Divisor, style, smallestGrouped: 5);
                bool one = Math.Abs(value / unit.Divisor - 1d) < 1e-9;
                return (one ? unit.OnePrefix : unit.Prefix) + digits + (one ? unit.OneSuffix : unit.Suffix);
            }
            return Plain(value, step, style);
        }

        public static string Value(double value, NumberStyle style)
        {
            if (double.IsNaN(value)) return "";
            double magnitude = Math.Abs(value);
            if (magnitude >= 1e3) return Number(value, Math.Pow(10d, Math.Floor(Math.Log10(magnitude)) - 2), style);
            double step = magnitude >= 100d ? 1d : magnitude >= 1d ? 0.01d : magnitude > 0d ? Math.Pow(10d, Math.Floor(Math.Log10(magnitude)) - 2) : 1d;
            return Plain(value, step, style);
        }

        public static string Whole(double value, NumberStyle style)
        {
            string text = Math.Round(value).ToString("0", CultureInfo.InvariantCulture);
            return Signed(text, style, grouped: false);
        }

        public static string Plain(double value, double step, NumberStyle style, int smallestGrouped = 0)
        {
            int decimals = 0;
            double s = Math.Abs(step);
            while (decimals < 6 && s > 0d && Math.Abs(s * Math.Pow(10d, decimals) - Math.Round(s * Math.Pow(10d, decimals))) > 1e-6) decimals++;
            string text = Math.Round(value, decimals).ToString("F" + decimals, CultureInfo.InvariantCulture);
            if (decimals > 0) text = text.TrimEnd('0').TrimEnd('.');
            return Signed(text, style, grouped: true, smallestGrouped);
        }

        private static string Signed(string ascii, NumberStyle style, bool grouped, int smallestGrouped = 0)
        {
            bool negative = ascii.StartsWith("-", StringComparison.Ordinal);
            if (negative) ascii = ascii.Substring(1);
            int point = ascii.IndexOf('.');
            string whole = point < 0 ? ascii : ascii.Substring(0, point);
            string fraction = point < 0 ? "" : ascii.Substring(point + 1);
            if (negative && whole.TrimStart('0').Length == 0 && fraction.TrimStart('0').Length == 0) negative = false;

            var shown = new StringBuilder();
            if (negative) shown.Append(style.Minus);
            shown.Append(style.Native(grouped && whole.Length >= smallestGrouped ? Grouped(whole, style) : whole));
            if (fraction.Length > 0) shown.Append(style.Decimal).Append(style.Native(fraction));
            return shown.ToString();
        }

        private static string Grouped(string digits, NumberStyle style)
        {
            if (digits.Length <= style.Primary) return digits;
            var groups = new List<string>();
            int end = digits.Length;
            groups.Add(digits.Substring(end - style.Primary));
            end -= style.Primary;
            while (end > 0)
            {
                int size = Math.Min(style.Secondary, end);
                groups.Add(digits.Substring(end - size, size));
                end -= size;
            }
            groups.Reverse();
            return string.Join(style.Group, groups);
        }
    }
}
