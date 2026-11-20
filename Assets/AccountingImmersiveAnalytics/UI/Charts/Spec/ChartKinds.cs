using System;
using System.Collections.Generic;

namespace AccountingImmersiveAnalytics.UI.Charts.Spec
{
    public enum ChartKind { Bar, Line, Pie, Scatter, Histogram, Heatmap, BoxPlot, Bubble, Treemap }

    public static class ChartKinds
    {
        private static readonly Dictionary<ChartKind, RoleRule[]> Rules = new Dictionary<ChartKind, RoleRule[]>
        {
            [ChartKind.Bar] = new[] { Need(Role.Category, FieldType.Category), Maybe(Role.Group, FieldType.Category), Maybe(Role.Value, FieldType.Number) },
            [ChartKind.Line] = new[] { Need(Role.X, FieldType.Number), Need(Role.Y, FieldType.Number), Maybe(Role.Group, FieldType.Category) },
            [ChartKind.Pie] = new[] { Need(Role.Category, FieldType.Category), Maybe(Role.Value, FieldType.Number) },
            [ChartKind.Scatter] = new[] { Need(Role.X, FieldType.Number), Need(Role.Y, FieldType.Number), Maybe(Role.Group, FieldType.Category) },
            [ChartKind.Histogram] = new[] { Need(Role.Value, FieldType.Number) },
            [ChartKind.Heatmap] = new[] { Need(Role.X, FieldType.Category), Need(Role.Y, FieldType.Category), Maybe(Role.Value, FieldType.Number) },
            [ChartKind.BoxPlot] = new[] { Need(Role.Category, FieldType.Category), Need(Role.Value, FieldType.Number) },
            [ChartKind.Bubble] = new[] { Need(Role.X, FieldType.Number), Need(Role.Y, FieldType.Number), Need(Role.Size, FieldType.Number), Maybe(Role.Group, FieldType.Category) },
            [ChartKind.Treemap] = new[] { Need(Role.Item, FieldType.Category), Maybe(Role.Group, FieldType.Category), Maybe(Role.Value, FieldType.Number) }
        };

        public static IReadOnlyList<RoleRule> RulesFor(ChartKind kind) => Rules[kind];

        public static bool Sorts(ChartKind kind) => kind != ChartKind.Histogram && kind != ChartKind.Treemap;

        public static string Name(ChartKind kind, bool horizontal)
        {
            switch (kind)
            {
                case ChartKind.Bar: return horizontal ? "horizontal bar chart" : "vertical bar chart";
                case ChartKind.Pie: return "donut chart";
                case ChartKind.BoxPlot: return "box plot";
                case ChartKind.Heatmap: return "heatmap";
                case ChartKind.Treemap: return "treemap";
                case ChartKind.Histogram: return "histogram";
                default: return kind.ToString().ToLowerInvariant() + " chart";
            }
        }

        public static bool TryParse(string text, out ChartKind kind)
        {
            kind = ChartKind.Bar;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string key = text.Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
            switch (key)
            {
                case "donut": case "pie": kind = ChartKind.Pie; return true;
                case "box": case "boxplot": kind = ChartKind.BoxPlot; return true;
                case "column": case "verticalbar": case "horizontalbar": kind = ChartKind.Bar; return true;
            }
            if (key.Length == 0 || !char.IsLetter(key[0])) return false;
            return Enum.TryParse(key, true, out kind) && Enum.IsDefined(typeof(ChartKind), kind);
        }

        private static RoleRule Need(Role role, FieldType type) => new RoleRule(role, type, true);

        private static RoleRule Maybe(Role role, FieldType type) => new RoleRule(role, type, false);
    }
}
