using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.Layout;
using AccountingImmersiveAnalytics.UI.Charts.Marks;
using AccountingImmersiveAnalytics.UI.Charts.Scales;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.Types
{
    public sealed class HistogramChart : ChartType
    {
        private static readonly int[] BinCounts = { 5, 25, 125 };

        public static int BinsFor(int rows)
        {
            double target = Math.Sqrt(Math.Max(1, rows));
            int bins = BinCounts[0];
            foreach (int count in BinCounts)
                if (count <= target) bins = count;
            return bins;
        }

        public override void Draw(ChartDrawing d)
        {
            CsvField value = Field(d, Role.Value);
            List<int> all = Rows(d, false);
            List<int> rows = Rows(d, true);

            (double min, double max) = Range(value, all);
            NumberAxis xAxis = NumberAxis.For(min, max);
            int bins = BinsFor(all.Count);
            double width = (xAxis.High - xAxis.Low) / bins;

            int[] Count(List<int> set)
            {
                var counts = new int[bins];
                foreach (int r in set)
                {
                    int bin = (int)Math.Floor((value.Numbers[r] - xAxis.Low) / width);
                    counts[Mathf.Clamp(bin, 0, bins - 1)]++;
                }
                return counts;
            }

            int[] fullCounts = Count(all);
            int[] counts = Count(rows);
            int most = 0;
            foreach (int c in fullCounts) most = Math.Max(most, c);
            NumberAxis yAxis = NumberAxis.For(0d, Math.Max(most, 4), includeZero: true, whole: true);

            var xSpec = AxisSpec.Of(d.Spec.TitleFor(Role.Value), xAxis);
            xSpec.WholeNumbers = YearLike(value);
            d.Axes(xSpec, AxisSpec.Of(d.Spec.Titles.TryGetValue(Role.Y, out string yTitle) ? yTitle : "Count", yAxis));
            if (rows.Count == 0) d.Notice = NothingShown;

            for (int i = 0; i < bins; i++)
            {
                if (counts[i] == 0) continue;
                double from = xAxis.Low + width * i, to = from + width;
                float x0 = d.XAt(from), x1 = d.XAt(to), y0 = d.YAt(counts[i]);
                var bin = Rect.MinMaxRect(Mathf.Min(x0, x1), y0, Mathf.Max(x0, x1), d.Plot.yMax);
                bool atLeft = Mathf.Min(x0, x1) <= d.Plot.xMin + 0.5f, atRight = Mathf.Max(x0, x1) >= d.Plot.xMax - 0.5f;
                int shape = d.Marks.Cell(bin, ChartStyle.Group(0),
                    new Sides(atLeft ? Side.Edge : Side.Gap, Side.Free, atRight ? Side.Edge : Side.Gap, Side.Edge));
                d.HitBox(shape, bin, $"{AxisFormat.Number(from, width)} to {AxisFormat.Number(to, width)}")
                    .With("Count", counts[i].ToString());
            }
            d.Finish();
        }
    }
}
