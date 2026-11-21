using System.Collections.Generic;
using System.Linq;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Elements;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Types
{
    public sealed class TreemapChart : ChartType
    {
        private const float Near = 0.5f;
        private const int OthersTile = -2;

        private sealed class Tile
        {
            public int Item;
            public int Group;
            public double Value;
            public float Area;
            public Rect Box;
        }

        public override void Draw(ChartDrawing d)
        {
            CsvField item = Field(d, Role.Item);
            CsvField group = Field(d, Role.Group);
            List<int> all = Rows(d, false);
            List<int> rows = Rows(d, true);
            Dictionary<(int, int), double> totals = Totals(d, rows, item, group);
            List<int> allGroups = group != null ? Codes(group, all) : new List<int>();

            d.Open(true);
            foreach (var p in totals.Where(p => p.Value <= 0d)) d.LeftOut.Add(item.Categories[p.Key.Item1]);
            var tiles = totals.Where(p => p.Value > 0d)
                .Select(p => new Tile { Item = p.Key.Item1, Group = p.Key.Item2, Value = p.Value })
                .OrderByDescending(t => t.Value).ToList();
            List<string> restNames = null;
            if (tiles.Count > ChartStyle.MostShown)
            {
                var rest = tiles.Skip(ChartStyle.MostShown - 1).ToList();
                tiles = tiles.Take(ChartStyle.MostShown - 1).ToList();
                tiles.Add(new Tile { Item = OthersTile, Group = OthersTile, Value = rest.Sum(t => t.Value) });
                restNames = rest.Select(t => item.Categories[t.Item]).Distinct().ToList();
            }
            var tileGroups = tiles.Where(t => t.Group != OthersTile).Select(t => t.Group).Distinct().ToList();
            bool folded = tiles.Any(t => t.Group == OthersTile);
            int ColourFor(Tile t)
            {
                if (t.Group == OthersTile) return ChartStyle.MostShown - 1;
                if (group == null) return 0;
                return !folded && allGroups.Count <= ChartStyle.MostShown ? allGroups.IndexOf(t.Group) : tileGroups.IndexOf(t.Group);
            }
            if (tiles.Count == 0) d.Notice = NothingShown;

            Rect plot = d.Plot;
            double sum = tiles.Sum(t => t.Value);
            foreach (Tile t in tiles) t.Area = (float)(t.Value / sum) * plot.width * plot.height;
            Squarify(tiles, plot);

            float tickBand = Style.InkHeight(Style.SmallText);
            string valueTitle = d.Spec.TitleFor(Role.Value);
            foreach (Tile t in tiles)
            {
                Rect r = t.Box;
                var sides = new Sides(
                    Mathf.Abs(r.xMin - plot.xMin) < Near ? Side.Edge : Side.Gap,
                    Mathf.Abs(r.yMin - plot.yMin) < Near ? Side.Edge : Side.Gap,
                    Mathf.Abs(r.xMax - plot.xMax) < Near ? Side.Edge : Side.Gap,
                    Mathf.Abs(r.yMax - plot.yMax) < Near ? Side.Edge : Side.Gap);
                int colour = ColourFor(t);
                int shape = d.Marks.Cell(r, ChartStyle.Group(colour), sides);
                string name = t.Item == OthersTile ? Others : item.Categories[t.Item];
                MarkInfo info = d.HitBox(shape, r, name).With(valueTitle, Value(t.Value));
                if (group != null && t.Group != OthersTile) info.With(d.Spec.TitleFor(Role.Group), group.Categories[t.Group]);
                if (t.Item == OthersTile && restNames != null) info.Includes(restNames);

                float room = r.width - Style.SmallPad * 2f;
                string shown = ChartText.Shown(name);
                if (shown.Length == 0) continue;
                if (r.height < tickBand + Style.SmallPad * 2f) continue;
                if (room < ChartText.Width(shown.Substring(0, 1) + "…", Style.SmallText)) continue;
                d.Text.Put(ChartText.Cut(shown, Style.SmallText, room), Style.SmallText, Style.NormalWeight,
                    r.xMin + Style.SmallPad, r.yMin + Style.SmallPad + Style.Ascent(Style.SmallText), Align.Start);
            }

            if (group != null)
            {
                var keyed = tiles.Where(t => t.Group != OthersTile).GroupBy(t => t.Group).Select(g => g.First())
                    .Concat(tiles.Where(t => t.Group == OthersTile)).ToList();
                var names = keyed.Select(t => t.Group == OthersTile ? Others : group.Categories[t.Group]).ToList();
                d.Key.Add(KeySection.Groups(d.Spec.TitleFor(Role.Group), names, KeyShape.Box,
                    i => ChartStyle.Group(ColourFor(keyed[i]))));
            }
            d.Finish();
        }

        private static void Squarify(List<Tile> tiles, Rect space)
        {
            var row = new List<Tile>();
            var queue = new Queue<Tile>(tiles);
            while (queue.Count > 0)
            {
                float side = Mathf.Min(space.width, space.height);
                Tile next = queue.Peek();
                if (row.Count == 0 || Worst(Plus(row, next), side) <= Worst(row, side))
                {
                    row.Add(queue.Dequeue());
                    continue;
                }
                space = Place(row, space);
                row.Clear();
            }
            if (row.Count > 0) Place(row, space);
        }

        private static List<Tile> Plus(List<Tile> row, Tile next) => new List<Tile>(row) { next };

        private static float Worst(List<Tile> row, float side)
        {
            float s = row.Sum(t => t.Area), worst = 0f;
            foreach (Tile t in row)
                worst = Mathf.Max(worst, Mathf.Max(side * side * t.Area / (s * s), s * s / (side * side * t.Area)));
            return worst;
        }

        private static Rect Place(List<Tile> row, Rect space)
        {
            float s = row.Sum(t => t.Area);
            if (space.width >= space.height)
            {
                float w = s / space.height, y = space.yMin;
                foreach (Tile t in row)
                {
                    float h = t.Area / w;
                    t.Box = new Rect(space.xMin, y, w, h);
                    y += h;
                }
                return Rect.MinMaxRect(space.xMin + w, space.yMin, space.xMax, space.yMax);
            }
            float rowHeight = s / space.width, x = space.xMin;
            foreach (Tile t in row)
            {
                float w = t.Area / rowHeight;
                t.Box = new Rect(x, space.yMin, w, rowHeight);
                x += w;
            }
            return Rect.MinMaxRect(space.xMin, space.yMin + rowHeight, space.xMax, space.yMax);
        }
    }
}
