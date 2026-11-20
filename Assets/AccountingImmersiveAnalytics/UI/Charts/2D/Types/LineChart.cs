using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Types
{
    public sealed class LineChart : ChartType
    {
        private const float PointReach = 4f;

        public override void Draw(ChartDrawing d)
        {
            CsvField x = Field(d, Role.X);
            CsvField y = Field(d, Role.Y);
            List<int> all = Rows(d, false);
            List<int> rows = Rows(d, true);
            CsvField source = Field(d, Role.Group);
            CsvField group = Fold(d, source, rows, y);

            List<int> allGroups = group != null ? Codes(source, all) : new List<int> { -1 };
            List<int> groups = group != null ? Codes(group, rows) : new List<int> { -1 };
            OthersLast(group, groups);
            var series = new List<List<(double X, double Y)>>(groups.Count);
            foreach (int g in groups) series.Add(Series(rows, x, y, group, source, g));

            (double xMin, double xMax) = Range(x, all);
            (double yMin, double yMax) = Range(y, all);
            foreach (var line in series)
                foreach (var point in line)
                {
                    if (double.IsNaN(yMin) || point.Y < yMin) yMin = point.Y;
                    if (double.IsNaN(yMax) || point.Y > yMax) yMax = point.Y;
                }
            AxisSpec xSpec = NumberAxes(d, x, (xMin, xMax), (yMin, yMax), rows.Count);

            for (int gi = 0; gi < groups.Count; gi++)
            {
                int g = groups[gi];
                List<(double X, double Y)> values = series[gi];
                if (values.Count == 0) continue;
                var points = new List<Vector2>(values.Count);
                foreach (var value in values) points.Add(new Vector2(d.XAt(value.X), d.YAt(value.Y)));
                Color color = ChartStyle.Group(ColourOf(group, allGroups, groups, g));
                int shape = points.Count > 1
                    ? d.Marks.Polyline(points, Style.Stroke, color)
                    : d.Marks.Box(new Rect(points[0].x - Style.Stroke, points[0].y - Style.Stroke * 0.5f, Style.Stroke * 2f, Style.Stroke), color);
                string title = group != null ? group.Categories[g] : d.Spec.TitleFor(Role.Y);
                for (int i = 0; i < points.Count; i++)
                {
                    MarkInfo info = d.HitCircle(shape, points[i], PointReach, title)
                        .With(d.Spec.TitleFor(Role.X), xSpec.WholeNumbers ? values[i].X.ToString("0") : Value(values[i].X))
                        .With(d.Spec.TitleFor(Role.Y), Value(values[i].Y));
                    NoteOthers(info, group, g);
                }
            }

            if (group != null)
                d.Key.Add(KeySection.Groups(d.Spec.TitleFor(Role.Group), Names(group, groups), KeyShape.Line,
                    i => ChartStyle.Group(ColourOf(group, allGroups, groups, groups[i]))));
            d.Finish();
        }

        private static List<(double X, double Y)> Series(List<int> rows, CsvField x, CsvField y, CsvField group, CsvField source, int g)
        {
            var sums = new SortedDictionary<double, Dictionary<int, (double Sum, int Count)>>();
            foreach (int r in rows)
            {
                if (group != null && group.Codes[r] != g) continue;
                double at = x.Numbers[r];
                int from = source != null ? source.Codes[r] : -1;
                if (!sums.TryGetValue(at, out var byGroup)) sums[at] = byGroup = new Dictionary<int, (double, int)>();
                byGroup.TryGetValue(from, out var acc);
                byGroup[from] = (acc.Sum + y.Numbers[r], acc.Count + 1);
            }
            var values = new List<(double X, double Y)>(sums.Count);
            foreach (var pair in sums)
            {
                double total = 0d;
                foreach (var acc in pair.Value.Values) total += acc.Sum / acc.Count;
                values.Add((pair.Key, total));
            }
            return values;
        }
    }
}
