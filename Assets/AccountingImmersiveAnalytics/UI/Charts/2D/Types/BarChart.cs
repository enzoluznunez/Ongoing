using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks;
using AccountingImmersiveAnalytics.UI.Charts.Scales;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Types
{
    public sealed class BarChart : ChartType
    {
        public override void Draw(ChartDrawing d)
        {
            List<int> all = Rows(d, false);
            List<int> rows = Rows(d, true);
            CsvField value = Field(d, Role.Value);
            CsvField category = Fold(d, Field(d, Role.Category), rows, value);
            CsvField group = Fold(d, Field(d, Role.Group), rows, value);

            Dictionary<(int, int), double> full = Totals(d, all, category, group);
            Dictionary<(int, int), double> totals = Totals(d, rows, category, group);
            (double min, double max) = Range(full.Values);
            NumberAxis axis = NumberAxis.For(min, max, includeZero: true, whole: value == null);

            List<int> allGroups = group != null ? Codes(Field(d, Role.Group), all) : new List<int> { -1 };
            List<int> groups = group != null ? Codes(group, rows) : new List<int> { -1 };
            List<int> categories = Codes(category, rows);
            SortBy(categories, d.Shaping.Sort, code => Mean(totals, code, groups, codeFirst: true));
            OthersLast(category, categories);
            OthersLast(group, groups);

            string categoryTitle = d.Spec.TitleFor(Role.Category);
            string valueTitle = d.Spec.TitleFor(Role.Value);
            var names = Names(category, categories);
            if (d.Spec.Horizontal) d.Axes(AxisSpec.Of(valueTitle, axis), AxisSpec.Of(categoryTitle, names));
            else d.Axes(AxisSpec.Of(categoryTitle, names), AxisSpec.Of(valueTitle, axis));
            if (categories.Count == 0) d.Notice = NothingShown;

            float band = d.Spec.Horizontal ? d.YBand : d.XBand;
            float inner = band * ChartStyle.BarShare;
            float each = inner / Mathf.Max(1, groups.Count);
            for (int ci = 0; ci < categories.Count; ci++)
            {
                for (int gi = 0; gi < groups.Count; gi++)
                {
                    if (!totals.TryGetValue((categories[ci], groups[gi]), out double v)) continue;
                    Color color = ChartStyle.Group(ColourOf(group, allGroups, groups, groups[gi]));
                    bool first = gi == 0, last = gi == groups.Count - 1;
                    Side before = first ? Side.Free : Side.Gap, after = last ? Side.Free : Side.Gap;
                    Rect bar;
                    Sides sides;
                    if (d.Spec.Horizontal)
                    {
                        float top = d.YCategory(ci) - inner * 0.5f + each * gi;
                        float x0 = d.XAt(0d), x1 = d.XAt(v);
                        bar = Rect.MinMaxRect(Mathf.Min(x0, x1), top, Mathf.Max(x0, x1), top + each);
                        sides = v >= 0d ? new Sides(Side.Edge, before, Side.Free, after) : new Sides(Side.Free, before, Side.Edge, after);
                    }
                    else
                    {
                        float left = d.XCategory(ci) - inner * 0.5f + each * gi;
                        float y0 = d.YAt(0d), y1 = d.YAt(v);
                        bar = Rect.MinMaxRect(left, Mathf.Min(y0, y1), left + each, Mathf.Max(y0, y1));
                        sides = v >= 0d ? new Sides(before, Side.Free, after, Side.Edge) : new Sides(before, Side.Edge, after, Side.Free);
                    }
                    int shape = d.Marks.Cell(bar, color, sides);
                    MarkInfo info = d.HitBox(shape, bar, category.Categories[categories[ci]]);
                    if (group != null) info.With(d.Spec.TitleFor(Role.Group), group.Categories[groups[gi]]);
                    info.With(valueTitle, Value(v));
                    NoteOthers(info, category, categories[ci]);
                    NoteOthers(info, group, groups[gi]);
                }
            }

            if (group != null)
                GroupKey(d, Role.Group, group, allGroups, groups, KeyShape.Box);
            d.Finish();
        }
    }
}
