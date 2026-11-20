using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.Scales;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Types
{
    public sealed class ScatterChart : ChartType
    {
        private const int MostDots = 4000;
        private static readonly double[] SizeSteps = { 0.1d, 0.5d, 1d };

        public override void Draw(ChartDrawing d)
        {
            CsvField x = Field(d, Role.X);
            CsvField y = Field(d, Role.Y);
            CsvField size = d.Spec.Kind == ChartKind.Bubble ? Field(d, Role.Size) : null;
            CsvField group = Field(d, Role.Group);
            List<int> all = Rows(d, false);
            List<int> rows = Sample(Rows(d, true), MostDots);

            (double xMin, double xMax) = Range(x, all);
            (double yMin, double yMax) = Range(y, all);
            AxisSpec xSpec = NumberAxes(d, x, (xMin, xMax), (yMin, yMax), rows.Count);

            double sizeTop = 1d;
            if (size != null)
            {
                (_, double most) = Range(size, all);
                sizeTop = NumberAxis.For(0d, Math.Max(most, 1e-9), includeZero: true).High;
            }
            float Radius(double v) => size == null ? ChartStyle.DotRadius
                : Mathf.Sqrt((float)Math.Max(0d, v / sizeTop)) * ChartStyle.LargestBubble;

            List<int> allGroups = group != null ? Codes(group, all) : new List<int> { -1 };
            var order = new List<int>(rows);
            if (size != null) order.Sort((a, b) => size.Numbers[b].CompareTo(size.Numbers[a]));
            foreach (int r in order)
            {
                var centre = new Vector2(d.XAt(x.Numbers[r]), d.YAt(y.Numbers[r]));
                float radius = Radius(size != null ? size.Numbers[r] : 0d);
                Color color = Style.Faded(ChartStyle.Group(group != null ? allGroups.IndexOf(group.Codes[r]) : 0), ChartStyle.DotAlpha);
                int shape = d.Marks.Circle(centre, radius, color);
                MarkInfo info = d.HitCircle(shape, centre, radius, group != null ? group.Categories[group.Codes[r]] : $"Row {r + 1}");
                info.With(d.Spec.TitleFor(Role.X), Value(x.Numbers[r])).With(d.Spec.TitleFor(Role.Y), Value(y.Numbers[r]));
                if (size != null) info.With(d.Spec.TitleFor(Role.Size), Value(size.Numbers[r]));
            }

            if (group != null)
            {
                List<int> shown = Codes(group, rows);
                d.Key.Add(KeySection.Groups(d.Spec.TitleFor(Role.Group), Names(group, shown), KeyShape.Dot,
                    i => Style.Faded(ChartStyle.Group(allGroups.IndexOf(shown[i])), ChartStyle.DotAlpha)));
            }
            if (size != null)
            {
                var sizes = new KeySection { Title = d.Spec.TitleFor(Role.Size), Shape = KeyShape.Size, LargestRadius = ChartStyle.LargestBubble };
                foreach (double step in SizeSteps)
                {
                    double v = sizeTop * step;
                    sizes.Entries.Add(new KeyEntry { Name = AxisFormat.Number(v, sizeTop * 0.1d), Color = Style.Faded(ChartStyle.SizeGrey, ChartStyle.DotAlpha), Radius = Radius(v) });
                }
                d.Key.Add(sizes);
            }
            d.Finish();
        }
    }
}
