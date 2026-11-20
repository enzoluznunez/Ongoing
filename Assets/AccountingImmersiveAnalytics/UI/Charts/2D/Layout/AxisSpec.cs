using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Scales;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout
{
    public sealed class AxisSpec
    {
        public string Title;
        public List<string> Categories;
        public NumberAxis Numbers;
        public bool WholeNumbers;

        public bool IsCategory => Categories != null;

        public int TickCount => IsCategory ? Categories.Count : ChartStyle.Ticks;

        public string TickText(int i)
        {
            if (IsCategory) return Elements.ChartText.Cap(Elements.ChartText.Shown(Categories[i]));
            double v = Numbers.TickValue(i);
            return WholeNumbers ? AccountingImmersiveAnalytics.Text.Numbers.Whole(v) : Numbers.ShownLabel(i);
        }

        public static AxisSpec Of(string title, List<string> categories) => new AxisSpec { Title = title, Categories = categories };

        public static AxisSpec Of(string title, NumberAxis numbers) => new AxisSpec { Title = title, Numbers = numbers };
    }
}
