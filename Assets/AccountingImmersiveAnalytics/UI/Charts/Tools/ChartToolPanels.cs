using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Elements;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.View;
using AccountingImmersiveAnalytics.UI.Charts.View;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using AccountingImmersiveAnalytics.UI.Panels;
using OneText.UGUI;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.Tools
{
    public static class ChartToolPanels
    {
        private const float KeySwatch = Style.MediumText;

        private static readonly SortOrder[] SortCycle = { SortOrder.None, SortOrder.Ascending, SortOrder.Descending };

        public static bool Build(PanelCard card, ToolType tool, ChartEntry entry, ChartCard view, ChartManager charts)
        {
            switch (tool)
            {
                case ToolType.Sort: Sort(card, entry, charts); return true;
                case ToolType.Filter: Filter(card, entry, charts); return true;
                case ToolType.Trendline: Trend(card, entry, charts); return true;
                case ToolType.Key: Key(card, view); return true;
                case ToolType.Edits: Edits(card, entry, charts); return true;
                default: return false;
            }
        }

        private static string Data(string text) => Numbers.Text(text ?? "");

        private static Role SortRole(ChartSpec spec)
        {
            switch (spec.Kind)
            {
                case ChartKind.Line:
                case ChartKind.Scatter:
                case ChartKind.Bubble: return Role.X;
                case ChartKind.Heatmap: return Role.Y;
                default: return Role.Category;
            }
        }

        private static bool SortsByValue(ChartSpec spec) =>
            spec.Kind == ChartKind.Bar || spec.Kind == ChartKind.Pie || spec.Kind == ChartKind.BoxPlot || spec.Kind == ChartKind.Heatmap;

        private static void Sort(PanelCard card, ChartEntry entry, ChartManager charts)
        {
            ChartSpec spec = entry.Spec;
            string field = spec.TitleFor(SortRole(spec));
            SortOrder now = entry.Shaping.Sort;
            card.Wide(() => Data(field), () => Strings.Get("sort." + now.ToString().ToLowerInvariant()), true, false, true, () =>
            {
                if (ActivityFeed.UserDriven) AssistantTurn.UserTookControl();
                SortOrder next = SortCycle[(Array.IndexOf(SortCycle, entry.Shaping.Sort) + 1) % SortCycle.Length];
                ChartShaping shaping = entry.Shaping.Copy();
                shaping.Sort = next;
                string label = next == SortOrder.None
                    ? $"Put {field} back in data order"
                    : $"Sorted {field}, {(next == SortOrder.Ascending ? "lowest" : "highest")} first";
                charts.Reshape(entry, shaping, EditKind.Sort, $"sorted \"{entry.Title}\" {next.ToString().ToLowerInvariant()}", label);
            });
            if (SortsByValue(spec))
            {
                string by = spec.TitleFor(Role.Value);
                card.Line(() => Strings.Format("sort.by", Data(by)));
            }
        }

        private static bool Aggregates(ChartKind kind) =>
            kind == ChartKind.Bar || kind == ChartKind.Pie || kind == ChartKind.BoxPlot || kind == ChartKind.Heatmap || kind == ChartKind.Treemap;

        private static void Filter(PanelCard card, ChartEntry entry, ChartManager charts)
        {
            CsvTable table = entry.Table;
            var used = new List<string>(entry.Spec.UsedFields());
            OneTextLabel count = null;

            foreach (string name in used)
            {
                CsvField field = table.Field(name);
                if (field == null || field.IsNumber) continue;
                var chips = new List<(Func<string>, bool, Action)>();
                var described = new List<string>();
                foreach (string category in field.Categories)
                {
                    string c = category;
                    bool hidden = entry.Shaping.Hidden.TryGetValue(field.Name, out HashSet<string> set) && set.Contains(c);
                    chips.Add((() => Data(c), !hidden, () => ToggleCategory(entry, field, c, charts)));
                    described.Add($"'{c}' ({(hidden ? "hidden" : "shown")})");
                }
                card.Chips(() => Data(field.Name), chips, $"filter {field.Name}", i => described[i]);
            }

            bool aggregates = Aggregates(entry.Spec.Kind);
            foreach (CsvField field in table.Fields)
            {
                if (!field.IsNumber || double.IsNaN(field.Min)) continue;
                bool plotted = used.Exists(n => string.Equals(n, field.Name, StringComparison.OrdinalIgnoreCase));
                if (aggregates == plotted) continue;
                (double low, double high) = entry.Shaping.Ranges.TryGetValue(field.Name, out var range) ? range : (field.Min, field.Max);
                RangeSlider slider = card.Range(() => Data(field.Name), field.Min, field.Max, low, high, Whole(field));
                ChartShaping before = null;
                slider.Changed += (a, b) =>
                {
                    if (before == null) before = entry.Shaping.Copy();
                    charts.Preview(entry, WithRange(entry.Shaping, field, a, b));
                    ShowCount(count, entry);
                };
                slider.Released += (a, b) =>
                {
                    if (before == null) return;
                    if (ActivityFeed.UserDriven) AssistantTurn.UserTookControl();
                    bool full = a <= field.Min && b >= field.Max;
                    string label = full ? $"Showed all {field.Name}" : $"Kept {field.Name} from {Scales.AxisFormat.Value(a)} to {Scales.AxisFormat.Value(b)}";
                    charts.Commit(entry, before, EditKind.Filter, $"filtered \"{entry.Title}\" by '{field.Name}'", label);
                    before = null;
                };
            }

            if (entry.LeftOut.Count > 0)
            {
                string names = string.Join(", ", entry.LeftOut);
                card.Line(() => Strings.Format("filter.leftOut", Data(names)));
            }
            count = card.Line(CountText(entry));
        }

        private static void ShowCount(OneTextLabel label, ChartEntry entry)
        {
            if (label == null) return;
            Texts.Bind(label, CountText(entry));
        }

        private static Func<string> CountText(ChartEntry entry)
        {
            int kept = entry.Shaping.KeptCount(entry.Table);
            int total = entry.Table.RowCount;
            return () => Strings.Format("filter.showing", kept, total);
        }

        private static bool Whole(CsvField field)
        {
            foreach (double v in field.Numbers)
                if (!double.IsNaN(v) && Math.Abs(v - Math.Round(v)) > 1e-9) return false;
            return true;
        }

        private static ChartShaping WithRange(ChartShaping shaping, CsvField field, double low, double high)
        {
            ChartShaping next = shaping.Copy();
            if (low <= field.Min && high >= field.Max) next.Ranges.Remove(field.Name);
            else next.Ranges[field.Name] = (low, high);
            return next;
        }

        private static void ToggleCategory(ChartEntry entry, CsvField field, string category, ChartManager charts)
        {
            ChartShaping next = entry.Shaping.Copy();
            if (!next.Hidden.TryGetValue(field.Name, out HashSet<string> hidden))
                next.Hidden[field.Name] = hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hide = !hidden.Contains(category);
            if (hide && hidden.Count >= field.Categories.Count - 1) return;
            if (ActivityFeed.UserDriven) AssistantTurn.UserTookControl();
            if (hide) hidden.Add(category);
            else hidden.Remove(category);
            if (hidden.Count == 0) next.Hidden.Remove(field.Name);
            charts.Reshape(entry, next, EditKind.Filter,
                $"{(hide ? "hid" : "showed")} '{category}' on \"{entry.Title}\"", $"{(hide ? "Hid" : "Showed")} {category}");
        }

        private static void Trend(PanelCard card, ChartEntry entry, ChartManager charts)
        {
            TrendKind now = entry.Shaping.Trend;
            bool fits = TrendFitter.Fits(entry.Spec, entry.Table, entry.Shaping, now);
            card.Wide(() => Strings.Get("trend.name"), () => Strings.Get("trend." + now), true, !fits, true, () =>
            {
                if (ActivityFeed.UserDriven) AssistantTurn.UserTookControl();
                TrendKind next = TrendFitter.Order[(Array.IndexOf(TrendFitter.Order, entry.Shaping.Trend) + 1) % TrendFitter.Order.Length];
                charts.SetTrend(entry, next);
            });
            if (!fits)
            {
                string why = TrendFitter.WhyNot(entry.Spec, entry.Table, entry.Shaping, now);
                if (why != null) card.Line(() => why);
                return;
            }
            TrendFit fit = TrendFitter.Fit(entry.Spec, entry.Table, entry.Shaping, now);
            if (fit != null) card.Line(() => Numbers.Text(fit.Meaning));
        }

        private static void Key(PanelCard card, ChartCard view)
        {
            if (view == null) return;
            foreach (KeySection section in view.Key)
            {
                KeySection s = section;
                if (s.IsRamp)
                {
                    var ramp = s.RampLabels.ConvertAll(Numbers.Text);
                    float labels = Style.InkHeight(Style.MediumText);
                    RectTransform rampRow = card.Row(card.NameRow(() => Data(s.Title)));
                    card.Name(rampRow, () => Data(s.Title));
                    float barTop = (PanelCard.RowHeight - ChartStyle.RampHeight - Style.SmallPad - labels) * 0.5f;
                    RectTransform area = card.PlaceIn(LayoutTools.EnsureChild(card.Centred(rampRow, PanelCard.RowHeight), "Ramp"), card.Half - PanelCard.Inset, 0f, card.ControlWidth, PanelCard.RowHeight);
                    MarkSurface marks = LayoutTools.Ensure<MarkSurface>(area.gameObject);
                    marks.Clear();
                    marks.Gradient(new Rect(0f, barTop, card.ControlWidth, ChartStyle.RampHeight), ChartStyle.Ramp, Style.SmallRadius);
                    marks.Commit();
                    var text = new ChartText(area);
                    text.Begin();
                    int last = ramp.Count - 1;
                    float baseline = barTop + ChartStyle.RampHeight + Style.SmallPad + Style.Ascent(Style.MediumText);
                    bool crowded = RampCrowded(ramp, card.ControlWidth);
                    for (int i = 0; i <= last; i++)
                        if (!crowded || i == 0 || i == last || i == last / 2)
                            text.Put(ramp[i], Style.MediumText, Style.NormalWeight, card.ControlWidth * i / Mathf.Max(1, last), baseline,
                                i == 0 ? Align.Start : i == last ? Align.End : Align.Middle);
                    text.End();
                    continue;
                }

                float tall = s.Shape == KeyShape.Size ? Mathf.Max(PanelCard.RowHeight, s.LargestRadius * 2f) : PanelCard.RowHeight;
                RectTransform row = card.Row(Mathf.Max(tall, card.NameRow(() => Data(s.Title))));
                card.Name(row, () => Data(s.Title));
                var items = new List<(float, Action<RectTransform>)>();
                var names = new List<string>();
                foreach (KeyEntry entry in s.Entries)
                {
                    KeyEntry e = entry;
                    float swatch = s.Shape == KeyShape.Size ? e.Radius * 2f : KeySwatch;
                    string name = Data(e.Name);
                    names.Add(e.Name);
                    float wide = swatch + Style.SmallPad + Mathf.Ceil(ChartText.Width(name, Style.MediumText));
                    items.Add((wide, holder => Swatch(holder, s.Shape, e, swatch, name, tall, card.Mirrored)));
                }
                card.Slide(card.Centred(row, tall), items, PanelCard.KeyGap,
                    string.IsNullOrEmpty(s.Title) ? "key" : $"key {s.Title}", "entry", i => $"'{names[i]}'");
            }
        }

        private static bool RampCrowded(IReadOnlyList<string> labels, float width)
        {
            int last = labels.Count - 1;
            if (last < 1) return false;
            float Left(int i)
            {
                float w = ChartText.Width(labels[i], Style.MediumText), at = width * i / last;
                return i == 0 ? at : i == last ? at - w : at - w * 0.5f;
            }
            float Right(int i) => Left(i) + ChartText.Width(labels[i], Style.MediumText);
            for (int i = 0; i < last; i++)
                if (Left(i + 1) - Right(i) < Style.SmallPad) return true;
            return false;
        }

        private static void Swatch(RectTransform holder, KeyShape shape, KeyEntry entry, float size, string name, float tall, bool mirrored)
        {
            float from = mirrored ? holder.rect.width - size : 0f;
            MarkSurface marks = LayoutTools.Ensure<MarkSurface>(holder.gameObject);
            marks.Clear();
            float middle = tall * 0.5f;
            switch (shape)
            {
                case KeyShape.Line:
                    marks.Line(new Vector2(from, middle), new Vector2(from + size, middle), Style.Stroke, entry.Color);
                    break;
                case KeyShape.Dot:
                case KeyShape.Size:
                    marks.Circle(new Vector2(from + size * 0.5f, middle), size * 0.5f, entry.Color);
                    break;
                default:
                    marks.Box(new Rect(from, middle - size * 0.5f, size, size), entry.Color, Style.SmallRadius);
                    break;
            }
            marks.Commit();
            RectTransform words = PanelCard.Place(LayoutTools.EnsureChild(holder, "Text"), 0f, 0f, holder.rect.width, tall);
            var text = new ChartText(words);
            text.Begin();
            float textAt = mirrored ? holder.rect.width - size - Style.SmallPad : size + Style.SmallPad;
            text.Put(name, Style.MediumText, Style.NormalWeight, textAt, middle + Style.Ascent(Style.MediumText) * 0.5f, mirrored ? Align.End : Align.Start);
            text.End();
        }

        private static void Edits(PanelCard card, ChartEntry entry, ChartManager charts)
        {
            List<Edit> edits = ChartManager.ActiveEdits.For(entry.Id);
            card.BeginList();
            if (edits.Count == 0) card.Line(() => Strings.Get("edits.empty"));
            foreach (Edit edit in edits)
            {
                Edit e = edit;
                card.Pair(() => Numbers.Text(e.What ?? Edit.KindName(e.Kind)),
                    () => Strings.Get(e.ByAssistant ? "assistant" : "edits.you"), true);
            }
            card.EndList("edits", "edit", i => $"{edits[i].What ?? Edit.KindName(edits[i].Kind)} ({(edits[i].ByAssistant ? "Assistant" : "You")})");
            card.Wide(null, () => Strings.Get("edits.undo"), false, false, edits.Count > 0, () =>
            {
                if (ActivityFeed.UserDriven) AssistantTurn.UserTookControl();
                charts.UndoNewest(entry);
            }, PanelCard.ListGap);
        }
    }
}
