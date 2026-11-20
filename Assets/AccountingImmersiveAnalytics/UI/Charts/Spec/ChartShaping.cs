using System;
using System.Collections.Generic;
using System.Linq;
using AccountingImmersiveAnalytics.UI.Charts.Data;

namespace AccountingImmersiveAnalytics.UI.Charts.Spec
{
    public enum SortOrder { None, Ascending, Descending }

    public enum TrendKind { None, Linear, Exponential, Logarithmic, Polynomial, Power, Smooth }

    public sealed class ChartShaping
    {
        public SortOrder Sort = SortOrder.None;
        public TrendKind Trend = TrendKind.None;
        public readonly Dictionary<string, HashSet<string>> Hidden = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, (double Low, double High)> Ranges = new Dictionary<string, (double, double)>(StringComparer.OrdinalIgnoreCase);


        public ChartShaping Copy()
        {
            var copy = new ChartShaping { Sort = Sort, Trend = Trend };
            foreach (var pair in Hidden) copy.Hidden[pair.Key] = new HashSet<string>(pair.Value);
            foreach (var pair in Ranges) copy.Ranges[pair.Key] = pair.Value;
            return copy;
        }

        public bool SameAs(ChartShaping other)
        {
            if (other == null || Sort != other.Sort || Trend != other.Trend || Hidden.Count != other.Hidden.Count || Ranges.Count != other.Ranges.Count) return false;
            foreach (var pair in Hidden)
                if (!other.Hidden.TryGetValue(pair.Key, out HashSet<string> set) || !set.SetEquals(pair.Value)) return false;
            foreach (var pair in Ranges)
                if (!other.Ranges.TryGetValue(pair.Key, out var range) || range != pair.Value) return false;
            return true;
        }

        public Func<int, bool> Keeper(CsvTable table)
        {
            var hidden = new List<(CsvField Field, bool[] Hides)>();
            foreach (var pair in Hidden)
            {
                CsvField field = table.Field(pair.Key);
                if (field == null || field.IsNumber || pair.Value.Count == 0) continue;
                var hides = new bool[field.Categories.Count];
                for (int code = 0; code < hides.Length; code++) hides[code] = pair.Value.Contains(field.Categories[code]);
                hidden.Add((field, hides));
            }
            var ranges = new List<(CsvField Field, double Low, double High)>();
            foreach (var pair in Ranges)
            {
                CsvField field = table.Field(pair.Key);
                if (field != null && field.IsNumber) ranges.Add((field, pair.Value.Low, pair.Value.High));
            }
            return row =>
            {
                foreach (var (field, hides) in hidden)
                {
                    int code = field.Codes[row];
                    if (code >= 0 && hides[code]) return false;
                }
                foreach (var (field, low, high) in ranges)
                {
                    double v = field.Numbers[row];
                    if (!double.IsNaN(v) && (v < low || v > high)) return false;
                }
                return true;
            };
        }

        public int KeptCount(CsvTable table)
        {
            Func<int, bool> keeps = Keeper(table);
            int kept = 0;
            for (int r = 0; r < table.RowCount; r++)
                if (keeps(r)) kept++;
            return kept;
        }

        public string Summary(ChartSpec spec)
        {
            var parts = new List<string>();
            if (Sort != SortOrder.None) parts.Add($"sorted {Sort.ToString().ToLowerInvariant()}");
            if (TrendFitter.Offered(spec) && Trend != TrendKind.None) parts.Add($"{Trend.ToString().ToLowerInvariant()} trend");
            foreach (var pair in Hidden.Where(p => p.Value.Count > 0))
                parts.Add($"'{pair.Key}' hides {string.Join(", ", pair.Value)}");
            foreach (var pair in Ranges)
                parts.Add($"'{pair.Key}' kept from {pair.Value.Low:G4} to {pair.Value.High:G4}");
            return parts.Count == 0 ? "no sort or filter" : string.Join("; ", parts);
        }
    }
}
