using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks;
using AccountingImmersiveAnalytics.UI.Charts.Scales;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Types
{
    public sealed class BoxPlotChart : ChartType
    {
        private const double Reach = 1.5d;

        private sealed class Summary
        {
            public double Low, Q1, Median, Q3, High;
            public readonly List<double> Outliers = new List<double>();
        }

        public override void Draw(ChartDrawing d)
        {
            CsvField category = Field(d, Role.Category);
            CsvField value = Field(d, Role.Value);
            List<int> all = Rows(d, false);
            List<int> rows = Rows(d, true);

            (double min, double max) = Range(value, all);
            NumberAxis axis = NumberAxis.For(min, max);
            List<int> categories = Codes(category, rows);
            var summaries = new Dictionary<int, Summary>();
            foreach (int code in categories)
            {
                var values = new List<double>();
                foreach (int r in rows)
                    if (category.Codes[r] == code) values.Add(value.Numbers[r]);
                summaries[code] = Summarise(values);
            }
            SortBy(categories, d.Shaping.Sort, code => summaries[code].Median);

            string valueTitle = d.Spec.TitleFor(Role.Value);
            d.Axes(AxisSpec.Of(d.Spec.TitleFor(Role.Category), Names(category, categories)), AxisSpec.Of(valueTitle, axis));
            if (categories.Count == 0) d.Notice = NothingShown;

            Color color = ChartStyle.Group(0);
            for (int i = 0; i < categories.Count; i++)
            {
                Summary s = summaries[categories[i]];
                float cx = d.XCategory(i), w = d.XBand * ChartStyle.BarShare;
                float yLow = d.YAt(s.Low), yQ1 = d.YAt(s.Q1), yMed = d.YAt(s.Median), yQ3 = d.YAt(s.Q3), yHigh = d.YAt(s.High);
                d.Marks.Line(new Vector2(cx, yLow), new Vector2(cx, yQ1), Style.Stroke, Style.Ink);
                d.Marks.Line(new Vector2(cx, yQ3), new Vector2(cx, yHigh), Style.Stroke, Style.Ink);
                d.Marks.Line(new Vector2(cx - w * 0.25f, yLow), new Vector2(cx + w * 0.25f, yLow), Style.Stroke, Style.Ink);
                d.Marks.Line(new Vector2(cx - w * 0.25f, yHigh), new Vector2(cx + w * 0.25f, yHigh), Style.Stroke, Style.Ink);
                int top;
                if (yQ1 - yQ3 < Style.Stroke)
                    top = d.Marks.Line(new Vector2(cx - w * 0.5f, yMed), new Vector2(cx + w * 0.5f, yMed), Style.Stroke, color);
                else
                {
                    var upper = Rect.MinMaxRect(cx - w * 0.5f, yQ3, cx + w * 0.5f, yMed);
                    var lower = Rect.MinMaxRect(cx - w * 0.5f, yMed, cx + w * 0.5f, yQ1);
                    top = d.Marks.Cell(upper, color, new Sides(Side.Free, Side.Free, Side.Free, Side.Gap));
                    d.Marks.Cell(lower, color, new Sides(Side.Free, Side.Gap, Side.Free, Side.Free));
                }
                d.HitBox(top, Rect.MinMaxRect(cx - w * 0.5f, yHigh, cx + w * 0.5f, yLow), category.Categories[categories[i]])
                    .With("Median", Value(s.Median)).With("Middle half", $"{Value(s.Q1)} to {Value(s.Q3)}")
                    .With("Range", $"{Value(s.Low)} to {Value(s.High)}");
                foreach (double outlier in s.Outliers)
                {
                    var centre = new Vector2(cx, d.YAt(outlier));
                    int dot = d.Marks.Circle(centre, ChartStyle.DotRadius, color);
                    d.HitCircle(dot, centre, ChartStyle.DotRadius, category.Categories[categories[i]]).With(valueTitle, Value(outlier));
                }
            }
            d.Finish();
        }

        private static Summary Summarise(List<double> values)
        {
            values.Sort();
            var s = new Summary();
            if (values.Count == 0) return s;
            s.Q1 = Quantile(values, 0.25d);
            s.Median = Quantile(values, 0.5d);
            s.Q3 = Quantile(values, 0.75d);
            double spread = (s.Q3 - s.Q1) * Reach;
            s.Low = s.Q1;
            s.High = s.Q3;
            foreach (double v in values)
            {
                if (v < s.Q1 - spread || v > s.Q3 + spread) { s.Outliers.Add(v); continue; }
                s.Low = Math.Min(s.Low, v);
                s.High = Math.Max(s.High, v);
            }
            return s;
        }

        private static double Quantile(List<double> sorted, double q)
        {
            double at = (sorted.Count - 1) * q;
            int below = (int)Math.Floor(at);
            int above = Math.Min(sorted.Count - 1, below + 1);
            return sorted[below] + (sorted[above] - sorted[below]) * (at - below);
        }
    }
}
