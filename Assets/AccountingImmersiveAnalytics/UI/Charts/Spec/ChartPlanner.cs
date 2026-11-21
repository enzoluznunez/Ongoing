using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant.FlashLite;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.Scales;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.Spec
{
    public static class ChartPlanner
    {
        private const int PlanSeconds = 30;
        private const int SampleRows = 8;
        private const int MostColumnCategories = 12;
        private const int ShortLabel = 10;
        private const int MostBars = 40;
        private const int MostSlices = 12;
        private const int MostHeatCells = 40;
        private const int MostGroups = 10;
        private const int MostTiles = 200;
        private const double LogSpan = 100d;

        private static readonly string[] RoleKeys = { "category", "group", "value", "x", "y", "size", "item" };

        public static async Task<List<ChartSpec>> Plan(string title, CsvTable table)
        {
            List<ChartSpec> planned = await Ask(title, table);
            if (planned == null || planned.Count == 0)
            {
                planned = Guess(table);
                if (planned.Count > 0) Debug.Log($"[ChartPlanner] Flash-Lite gave no usable plan for '{title}'; using {planned.Count} built-in charts.");
            }
            return planned;
        }

        public static async Task<ChartSpec> PlanTable(string title, CsvTable table, string caption)
        {
            ChartKind? named = NamedKind(caption, out bool horizontal);
            List<ChartSpec> planned = await Ask(title, table, caption);
            ChartSpec pick = planned?.FirstOrDefault(spec => named == null || spec.Kind == named.Value);
            if (pick == null && named != null) pick = Shape(named.Value, horizontal, table, CaptionTitle(caption) ?? title);
            if (pick == null) pick = planned?.FirstOrDefault() ?? Guess(table).FirstOrDefault();
            if (pick != null && named == ChartKind.Bar && pick.Kind == ChartKind.Bar)
            {
                pick.Horizontal = horizontal;
                Fits(pick, table);
            }
            return pick;
        }

        private static readonly (string words, ChartKind kind, bool horizontal)[] Names =
        {
            ("horizontal bar", ChartKind.Bar, true), ("vertical bar", ChartKind.Bar, false), ("column chart", ChartKind.Bar, false),
            ("bar chart", ChartKind.Bar, false), ("donut", ChartKind.Pie, false), ("doughnut", ChartKind.Pie, false), ("pie chart", ChartKind.Pie, false),
            ("box plot", ChartKind.BoxPlot, false), ("boxplot", ChartKind.BoxPlot, false), ("heatmap", ChartKind.Heatmap, false),
            ("heat map", ChartKind.Heatmap, false), ("treemap", ChartKind.Treemap, false), ("tree map", ChartKind.Treemap, false),
            ("histogram", ChartKind.Histogram, false), ("bubble", ChartKind.Bubble, false), ("scatter", ChartKind.Scatter, false),
            ("line chart", ChartKind.Line, false), ("line graph", ChartKind.Line, false)
        };

        private static ChartKind? NamedKind(string caption, out bool horizontal)
        {
            horizontal = false;
            if (string.IsNullOrWhiteSpace(caption)) return null;
            string text = caption.ToLowerInvariant();
            foreach ((string words, ChartKind kind, bool across) in Names)
            {
                if (!text.Contains(words)) continue;
                horizontal = across;
                return kind;
            }
            return null;
        }

        private static string CaptionTitle(string caption)
        {
            if (string.IsNullOrWhiteSpace(caption)) return null;
            string text = caption.Trim();
            if (text.StartsWith("Table", StringComparison.OrdinalIgnoreCase))
            {
                int stop = text.IndexOf(". ", StringComparison.Ordinal);
                if (stop > 0) text = text.Substring(stop + 2);
            }
            int end = text.IndexOf(". ", StringComparison.Ordinal);
            if (end > 0) text = text.Substring(0, end);
            text = text.TrimEnd('.').Trim();
            return text.Length > 0 ? text : null;
        }

        private static ChartSpec Shape(ChartKind kind, bool horizontal, CsvTable table, string title)
        {
            ChartSpec spec = New(kind, title);
            spec.Horizontal = horizontal;
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RoleRule rule in ChartKinds.RulesFor(kind))
            {
                bool number = rule.Type == FieldType.Number;
                CsvField field = table.Fields.FirstOrDefault(f => !used.Contains(f.Name) && f.IsNumber == number &&
                    (rule.Required || number || f.Categories.Count <= MostGroups && f.Categories.Count < table.RowCount));
                if (field == null)
                {
                    if (rule.Required) return null;
                    continue;
                }
                spec.Fields[rule.Role] = field.Name;
                used.Add(field.Name);
            }
            return Fits(spec, table) == null ? spec : null;
        }

        public static string Fits(ChartSpec spec, CsvTable table)
        {
            string problem = spec.Validate(table);
            if (problem != null) return problem;
            int Count(Role role) => table.Field(spec.FieldFor(role))?.Distinct ?? 0;
            switch (spec.Kind)
            {
                case ChartKind.Bar when Count(Role.Category) > MostBars:
                    return $"'{spec.Title}' would need {Count(Role.Category)} bars; at most {MostBars} fit.";
                case ChartKind.Pie when Count(Role.Category) > MostSlices:
                    return $"'{spec.Title}' would need {Count(Role.Category)} slices; at most {MostSlices} read well.";
                case ChartKind.Heatmap when Count(Role.X) > MostHeatCells || Count(Role.Y) > MostHeatCells:
                    return $"'{spec.Title}' would need too many heatmap rows or columns.";
                case ChartKind.Treemap when Count(Role.Item) > MostTiles:
                    return $"'{spec.Title}' would need {Count(Role.Item)} tiles.";
                case ChartKind.BoxPlot when Count(Role.Category) > MostBars:
                    return $"'{spec.Title}' would need {Count(Role.Category)} boxes.";
            }
            if (spec.FieldFor(Role.Group) != null && Count(Role.Group) > MostGroups) spec.Fields.Remove(Role.Group);
            if (spec.Kind == ChartKind.Bar && !FitsColumns(table.Field(spec.FieldFor(Role.Category)))) spec.Horizontal = true;
            return null;
        }

        private static bool FitsColumns(CsvField category)
        {
            if (category == null || category.IsNumber) return true;
            if (category.Categories.Count > MostColumnCategories) return false;
            foreach (string name in category.Categories)
                if (name.Length > ShortLabel) return false;
            return true;
        }

        private static async Task<List<ChartSpec>> Ask(string title, CsvTable table, string caption = null)
        {
            JObject answer = await FlashLiteClient.AskOnce(Instructions, Prompt(title, table, caption), Schema, PlanSeconds);
            if (!(answer?["charts"] is JArray charts)) return null;
            var specs = new List<ChartSpec>();
            foreach (JToken chart in charts)
            {
                if (!ChartKinds.TryParse(Clean(chart["kind"]), out ChartKind kind)) continue;
                var spec = new ChartSpec
                {
                    Kind = kind,
                    Horizontal = string.Equals(Clean(chart["orientation"]), "horizontal", StringComparison.OrdinalIgnoreCase),
                    Title = Clean(chart["title"]) ?? title,
                    Insight = Clean(chart["insight"]),
                    LogX = chart["logX"]?.Type == JTokenType.Boolean && (bool)chart["logX"],
                    LogY = chart["logY"]?.Type == JTokenType.Boolean && (bool)chart["logY"]
                };
                if (Enum.TryParse(Clean(chart["aggregate"]) ?? "", true, out Aggregate how)) spec.Aggregate = how;
                foreach (string key in RoleKeys)
                {
                    CsvField field = table.Field(Clean(chart[key]));
                    if (field == null) continue;
                    var role = (Role)Enum.Parse(typeof(Role), key, true);
                    spec.Fields[role] = field.Name;
                    string shown = Clean(chart[key + "Title"]);
                    if (shown != null) spec.Titles[role] = shown;
                }
                bool valueNeeded = ChartKinds.RulesFor(kind).Any(rule => rule.Role == Role.Value && rule.Required);
                if (spec.Aggregate == Aggregate.Count && !valueNeeded) spec.Fields.Remove(Role.Value);
                string problem = Fits(spec, table);
                if (problem != null)
                {
                    Debug.LogWarning($"[ChartPlanner] Skipped a planned chart: {problem}");
                    continue;
                }
                if (!specs.Any(s => Same(s, spec))) specs.Add(spec);
            }
            return specs;
        }

        private static bool Same(ChartSpec a, ChartSpec b) =>
            a.Kind == b.Kind && a.Fields.Count == b.Fields.Count && a.Fields.All(p => b.FieldFor(p.Key) == p.Value);

        private static string Clean(JToken token)
        {
            string text = token?.Type == JTokenType.String ? ((string)token)?.Trim() : null;
            return string.IsNullOrEmpty(text) ? null : text;
        }

        public static List<ChartSpec> Guess(CsvTable table)
        {
            var specs = new List<ChartSpec>();
            var numbers = table.Fields.Where(f => f.IsNumber).ToList();
            var categories = table.Fields.Where(f => !f.IsNumber && f.Categories.Count >= 2).ToList();
            CsvField time = numbers.FirstOrDefault(IsTime);
            var measures = numbers.Where(f => f != time).ToList();
            CsvField label = categories.Where(f => f.Categories.Count <= MostBars).OrderBy(f => f.Categories.Count).LastOrDefault();
            CsvField group = categories.Where(f => f != label && f.Categories.Count <= MostGroups).OrderBy(f => f.Categories.Count).FirstOrDefault();

            void Add(ChartSpec spec)
            {
                if (Fits(spec, table) == null && !specs.Any(s => Same(s, spec))) specs.Add(spec);
            }

            if (time != null && measures.Count > 0)
            {
                var line = New(ChartKind.Line, $"{Short(measures[0].Name)} over {Short(time.Name).ToLowerInvariant()}");
                line.Fields[Role.X] = time.Name;
                line.Fields[Role.Y] = measures[0].Name;
                if (group != null) line.Fields[Role.Group] = group.Name;
                line.Aggregate = Aggregate.Mean;
                Add(line);
            }
            if (label != null && measures.Count > 0)
            {
                var bar = New(ChartKind.Bar, $"{Short(measures[0].Name)} by {Short(label.Name).ToLowerInvariant()}");
                bar.Fields[Role.Category] = label.Name;
                bar.Fields[Role.Value] = measures[0].Name;
                if (group != null && group != label && time == null) bar.Fields[Role.Group] = group.Name;
                Add(bar);
                if (label.Categories.Count <= MostSlices && measures[0].Min >= 0d)
                {
                    var donut = New(ChartKind.Pie, $"Share of {Short(measures[0].Name).ToLowerInvariant()}");
                    donut.Fields[Role.Category] = label.Name;
                    donut.Fields[Role.Value] = measures[0].Name;
                    Add(donut);
                }
                CsvField named = categories.FirstOrDefault(f => f != label && f.Categories.Count <= MostBars && f.Categories.Any(c => c.Length > ShortLabel));
                if (named != null && measures.Count > 1)
                {
                    var ranked = New(ChartKind.Bar, $"{Short(measures[1].Name)} by {Short(named.Name).ToLowerInvariant()}");
                    ranked.Fields[Role.Category] = named.Name;
                    ranked.Fields[Role.Value] = measures[1].Name;
                    ranked.Horizontal = true;
                    Add(ranked);
                }
                var box = New(ChartKind.BoxPlot, $"Spread of {Short(measures[0].Name).ToLowerInvariant()} by {Short(label.Name).ToLowerInvariant()}");
                box.Fields[Role.Category] = label.Name;
                box.Fields[Role.Value] = measures[0].Name;
                if (table.RowCount > label.Categories.Count * 3) Add(box);
            }
            if (measures.Count >= 2)
            {
                var scatter = New(ChartKind.Scatter, $"{Short(measures[1].Name)} against {Short(measures[0].Name).ToLowerInvariant()}");
                scatter.Fields[Role.X] = measures[0].Name;
                scatter.Fields[Role.Y] = measures[1].Name;
                if (group != null) scatter.Fields[Role.Group] = group.Name;
                scatter.LogX = Wide(measures[0]);
                scatter.LogY = Wide(measures[1]);
                Add(scatter);
            }
            if (measures.Count > 0)
            {
                var histogram = New(ChartKind.Histogram, $"How {Short(measures[0].Name).ToLowerInvariant()} is spread");
                histogram.Fields[Role.Value] = measures[0].Name;
                Add(histogram);
            }
            return specs;
        }

        private static ChartSpec New(ChartKind kind, string title) => new ChartSpec { Kind = kind, Title = title };

        private static bool IsTime(CsvField field)
        {
            string name = field.Name.ToLowerInvariant();
            return name == "year" || name == "yr" || name.EndsWith(" year") || name == "date" || name == "time" || name == "month" || name == "period";
        }

        private static bool Wide(CsvField field) => field.Min > 0d && field.Max / field.Min >= LogSpan;

        private static string Short(string name)
        {
            int bracket = name.IndexOf(" (", StringComparison.Ordinal);
            return bracket > 0 ? name.Substring(0, bracket) : name;
        }

        private static JObject Schema
        {
            get
            {
                var properties = new JObject();
                var required = new JArray();
                void Field(string name, string type)
                {
                    properties[name] = new JObject { ["type"] = type };
                    required.Add(name);
                }
                Field("kind", "STRING");
                Field("orientation", "STRING");
                Field("title", "STRING");
                Field("insight", "STRING");
                Field("aggregate", "STRING");
                foreach (string key in RoleKeys)
                {
                    Field(key, "STRING");
                    Field(key + "Title", "STRING");
                }
                Field("logX", "BOOLEAN");
                Field("logY", "BOOLEAN");
                var chart = new JObject { ["type"] = "OBJECT", ["properties"] = properties, ["required"] = required };
                return new JObject
                {
                    ["type"] = "OBJECT",
                    ["properties"] = new JObject { ["charts"] = new JObject { ["type"] = "ARRAY", ["items"] = chart } },
                    ["required"] = new JArray("charts")
                };
            }
        }

        private const string Instructions =
            "You plan flat 2D charts from a CSV file for a mixed-reality app used by non-experts learning accounting. " +
            "Make one chart for each distinct insight the data supports, as many as are genuinely useful, and no duplicates. " +
            "Use column names exactly as listed; leave a field empty when the chart does not use it.\n" +
            "Chart kinds and the fields each uses:\n" +
            "- bar: category (text column), value (number), optional group (text, at most 10 values). orientation 'vertical' for ordered or time categories with at most 12 short labels, otherwise 'horizontal'.\n" +
            "- line: x (number, usually time), y (number), optional group (text). For change over time.\n" +
            "- pie: a donut. category (text, at most 12 values), value (positive number). For parts of a whole.\n" +
            "- scatter: x (number), y (number), optional group (text). For the relationship between two numbers.\n" +
            "- bubble: x, y, size (numbers), optional group. A scatter where area shows a third number.\n" +
            "- histogram: value (number). For how one number is spread.\n" +
            "- heatmap: x (text), y (text), value (number). For a value across two sets of categories.\n" +
            "- boxplot: category (text), value (number). For comparing spreads, with several rows per category.\n" +
            "- treemap: item (text), optional group (text), value (positive number). For parts of a whole with many items.\n" +
            "aggregate: how rows sharing a category combine: 'sum', 'mean', or 'count' (count ignores value).\n" +
            "logX, logY: true only when that number column is all above zero and spans more than 100 times.\n" +
            "title: a short chart title in plain English. insight: one sentence on what the chart shows. " +
            "The ...Title fields are short axis or key titles in plain English with units in brackets, such as \"Revenue ($)\"; leave empty to use the column name.";

        private static string Prompt(string title, CsvTable table, string caption)
        {
            var text = new StringBuilder(4096);
            text.Append("File: ").Append(title).Append('\n');
            if (!string.IsNullOrWhiteSpace(caption))
                text.Append("\nThis is one table from a PDF document. Its caption reads: \"").Append(caption.Trim()).Append("\"\n")
                    .Append("Make exactly one chart for it, of the kind the caption names if it names one, and take the chart title from the caption.\n");
            text.Append("\nColumns (").Append(table.RowCount).Append(" rows):\n");
            foreach (CsvField field in table.Fields)
            {
                text.Append("- ").Append(field.Name).Append(": ");
                if (field.IsNumber)
                    text.Append("numbers from ").Append(AxisFormat.Value(field.Min)).Append(" to ").Append(AxisFormat.Value(field.Max));
                else
                {
                    text.Append(field.Categories.Count).Append(" different texts, such as ");
                    text.Append(string.Join(", ", field.Categories.Take(8)));
                }
                text.Append('\n');
            }
            text.Append("\nFirst rows:\n");
            text.Append(string.Join(",", table.Fields.Select(f => f.Name))).Append('\n');
            for (int r = 0; r < Math.Min(SampleRows, table.RowCount); r++)
                text.Append(string.Join(",", table.Fields.Select(f => f.TextAt(r) ?? ""))).Append('\n');
            return text.ToString();
        }
    }
}
