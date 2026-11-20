using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Elements;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Types
{
    public sealed class DonutChart : ChartType
    {
        public override void Draw(ChartDrawing d)
        {
            List<int> all = Rows(d, false);
            List<int> rows = Rows(d, true);
            CsvField category = Fold(d, Field(d, Role.Category), rows, Field(d, Role.Value));
            Dictionary<(int, int), double> totals = Totals(d, rows, category, null);
            List<int> allCategories = Codes(Field(d, Role.Category), all);
            List<int> categories = Codes(category, rows);
            foreach (int code in categories)
                if (totals.TryGetValue((code, -1), out double v) && v <= 0d) d.LeftOut.Add(category.Categories[code]);
            categories.RemoveAll(code => !totals.TryGetValue((code, -1), out double v) || v <= 0d);
            SortBy(categories, d.Shaping.Sort, code => totals[(code, -1)]);
            OthersLast(category, categories);

            d.Open(false);
            if (categories.Count == 0) d.Notice = NothingShown;
            double total = 0d;
            foreach (int code in categories) total += totals[(code, -1)];

            Rect b = d.Plot;
            Vector2 centre = b.center;
            float outer = Mathf.Min(b.width, b.height) * 0.5f - ChartStyle.DonutInset;
            float hole = outer * ChartStyle.DonutHole;
            float angle = -Mathf.PI * 0.5f;
            string valueTitle = d.Spec.TitleFor(Role.Value);
            foreach (int code in categories)
            {
                double v = totals[(code, -1)];
                float next = angle + (float)(v / total) * Mathf.PI * 2f;
                int shape = d.Marks.Ring(centre, outer, hole, angle, next, ChartStyle.Group(ColourOf(category, allCategories, categories, code)));
                MarkInfo info = d.Hit(shape, category.Categories[code]).With(valueTitle, Value(v)).With("Share", $"{v / total:P0}");
                NoteOthers(info, category, code);
                info.Area = HitShape.Ring;
                info.Centre = centre;
                info.Outer = outer;
                info.Inner = hole;
                info.From = angle;
                info.To = next;
                angle = next;
            }

            if (categories.Count > 0)
            {
                float big = Style.InkHeight(Style.MediumText), small = Style.InkHeight(Style.SmallText);
                float block = big + Style.SmallPad + small;
                float top = centre.y - block * 0.5f;
                float room = hole * 2f - Style.SmallPad * 2f;
                d.Text.Put(ChartText.Cut(AccountingImmersiveAnalytics.Text.Numbers.Shown(total), Style.MediumText, room, Style.BoldWeight), Style.MediumText, Style.BoldWeight, centre.x, top + Style.Ascent(Style.MediumText), Align.Middle);
                string label = ChartText.Shown("Total " + valueTitle.ToLowerInvariant());
                d.Text.Put(ChartText.Cut(label, Style.SmallText, room), Style.SmallText, Style.NormalWeight,
                    centre.x, top + big + Style.SmallPad + Style.Ascent(Style.SmallText), Align.Middle);
            }

            d.Key.Add(KeySection.Groups(d.Spec.TitleFor(Role.Category), Names(category, categories), KeyShape.Box,
                i => ChartStyle.Group(ColourOf(category, allCategories, categories, categories[i]))));
            d.Finish();
        }
    }
}
