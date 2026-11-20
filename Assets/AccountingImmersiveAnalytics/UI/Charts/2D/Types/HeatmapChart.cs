using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks;
using AccountingImmersiveAnalytics.UI.Charts.Scales;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Types
{
    public sealed class HeatmapChart : ChartType
    {
        public override void Draw(ChartDrawing d)
        {
            List<int> all = Rows(d, false);
            List<int> rows = Rows(d, true);
            CsvField value = Field(d, Role.Value);
            CsvField x = Fold(d, Field(d, Role.X), rows, value);
            CsvField y = Fold(d, Field(d, Role.Y), rows, value);

            Dictionary<(int, int), double> full = Totals(d, all, x, y);
            Dictionary<(int, int), double> cells = Totals(d, rows, x, y);
            (double min, double max) = Range(full.Values);
            NumberAxis scale = NumberAxis.For(min, max, whole: value == null);

            List<int> columns = Codes(x, rows);
            List<int> lines = Codes(y, rows);
            SortBy(lines, d.Shaping.Sort, code => Mean(cells, code, columns, codeFirst: false));
            OthersLast(x, columns);
            OthersLast(y, lines);

            d.Axes(AxisSpec.Of(d.Spec.TitleFor(Role.X), Names(x, columns)), AxisSpec.Of(d.Spec.TitleFor(Role.Y), Names(y, lines)), grid: false);
            if (rows.Count == 0) d.Notice = NothingShown;

            string valueTitle = d.Spec.TitleFor(Role.Value);
            for (int li = 0; li < lines.Count; li++)
            {
                for (int ci = 0; ci < columns.Count; ci++)
                {
                    if (!cells.TryGetValue((columns[ci], lines[li]), out double v)) continue;
                    var cell = new Rect(d.Plot.xMin + d.XBand * ci, d.Plot.yMin + d.YBand * li, d.XBand, d.YBand);
                    var sides = new Sides(
                        ci == 0 ? Side.Edge : Side.Gap, li == 0 ? Side.Edge : Side.Gap,
                        ci == columns.Count - 1 ? Side.Edge : Side.Gap, li == lines.Count - 1 ? Side.Edge : Side.Gap);
                    int shape = d.Marks.Cell(cell, ChartStyle.RampAt(scale.Fraction(v)), sides);
                    MarkInfo info = d.HitBox(shape, cell, $"{x.Categories[columns[ci]]}, {y.Categories[lines[li]]}").With(valueTitle, Value(v));
                    NoteOthers(info, x, columns[ci]);
                    NoteOthers(info, y, lines[li]);
                }
            }

            var labels = new List<string>();
            for (int i = 0; i < ChartStyle.Ticks; i++) labels.Add(scale.Label(i));
            d.Key.Add(new KeySection { Title = valueTitle, RampLabels = labels });
            d.Finish();
        }
    }
}
