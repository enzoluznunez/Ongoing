using System;
using System.Text;

namespace AccountingImmersiveAnalytics.UI.Charts.Scales
{
    public sealed class NumberStyle
    {
        public readonly string Decimal, Group, Minus, Digits, PercentBefore, PercentAfter;
        public readonly int Primary, Secondary;
        public readonly NumberUnit[] Units;

        public NumberStyle(string point, string group, string minus, int primary, int secondary, string digits,
            string percentBefore, string percentAfter, NumberUnit[] units)
        {
            Decimal = point;
            Group = group;
            Minus = minus;
            Primary = Math.Max(1, primary);
            Secondary = Math.Max(1, secondary);
            Digits = digits;
            PercentBefore = percentBefore;
            PercentAfter = percentAfter;
            Units = (NumberUnit[])units.Clone();
            Array.Sort(Units, (a, b) => b.Threshold.CompareTo(a.Threshold));
        }

        public static readonly NumberStyle Invariant = new NumberStyle(".", ",", "-", 3, 3, null, "", "%", new[]
        {
            new NumberUnit(1e3, 1e3, "", "k"), new NumberUnit(1e6, 1e6, "", "M"),
            new NumberUnit(1e9, 1e9, "", "B"), new NumberUnit(1e12, 1e12, "", "T")
        });

        public string Native(string ascii)
        {
            if (Digits == null) return ascii;
            var shown = new StringBuilder(ascii.Length);
            foreach (char c in ascii) shown.Append(c >= '0' && c <= '9' ? Digits[c - '0'] : c);
            return shown.ToString();
        }

        public string Percent(string number) => PercentBefore + number + PercentAfter;
    }
}
