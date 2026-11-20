using System;
using System.Collections.Generic;
using System.Linq;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.Scales;
using AccountingImmersiveAnalytics.UI.Charts.Spec;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Types
{
    public abstract class ChartType
    {
        protected const string NothingShown = "No rows match the filter.";
        public const string Others = "Others";

        public abstract void Draw(ChartDrawing d);

        public static ChartType For(ChartKind kind)
        {
            switch (kind)
            {
                case ChartKind.Bar: return new BarChart();
                case ChartKind.Line: return new LineChart();
                case ChartKind.Pie: return new DonutChart();
                case ChartKind.Scatter: return new ScatterChart();
                case ChartKind.Bubble: return new ScatterChart();
                case ChartKind.Histogram: return new HistogramChart();
                case ChartKind.Heatmap: return new HeatmapChart();
                case ChartKind.BoxPlot: return new BoxPlotChart();
                case ChartKind.Treemap: return new TreemapChart();
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        protected static CsvField Field(ChartDrawing d, Role role) => d.Table.Field(d.Spec.FieldFor(role));

        protected static List<int> Rows(ChartDrawing d, bool shaped) => d.Spec.Rows(d.Table, shaped ? d.Shaping : null);

        protected static List<int> Codes(CsvField field, List<int> rows)
        {
            var codes = new List<int>();
            if (field == null) return codes;
            var seen = new HashSet<int>();
            foreach (int r in rows)
            {
                int code = field.Codes[r];
                if (code >= 0 && seen.Add(code)) codes.Add(code);
            }
            return codes;
        }

        protected static List<string> Names(CsvField field, List<int> codes)
        {
            var names = new List<string>(codes.Count);
            foreach (int code in codes) names.Add(field.Categories[code]);
            return names;
        }

        protected static Dictionary<(int, int), double> Totals(ChartDrawing d, List<int> rows, CsvField first, CsvField second)
        {
            CsvField value = Field(d, Role.Value);
            Aggregate how = value == null ? Aggregate.Count : d.Spec.Aggregate;
            var sums = new Dictionary<(int, int), double>();
            var counts = new Dictionary<(int, int), int>();
            foreach (int r in rows)
            {
                var key = (first.Codes[r], second != null ? second.Codes[r] : -1);
                sums.TryGetValue(key, out double sum);
                counts.TryGetValue(key, out int count);
                sums[key] = sum + (how == Aggregate.Count ? 1d : value.Numbers[r]);
                counts[key] = count + 1;
            }
            if (how != Aggregate.Mean) return sums;
            var means = new Dictionary<(int, int), double>();
            foreach (var pair in sums) means[pair.Key] = pair.Value / counts[pair.Key];
            return means;
        }

        protected static void SortBy(List<int> codes, SortOrder order, Func<int, double> key)
        {
            if (order == SortOrder.None) return;
            List<int> sorted = (order == SortOrder.Ascending ? codes.OrderBy(key) : codes.OrderByDescending(key)).ToList();
            codes.Clear();
            codes.AddRange(sorted);
        }

        protected static (double Min, double Max) Range(IEnumerable<double> values)
        {
            double min = double.NaN, max = double.NaN;
            foreach (double v in values)
            {
                if (double.IsNaN(v) || double.IsInfinity(v)) continue;
                if (double.IsNaN(min) || v < min) min = v;
                if (double.IsNaN(max) || v > max) max = v;
            }
            return (min, max);
        }

        protected static (double Min, double Max) Range(CsvField field, List<int> rows) =>
            Range(rows.Select(r => field.Numbers[r]));

        protected static AxisSpec NumberAxes(ChartDrawing d, CsvField x,
            (double Min, double Max) xRange, (double Min, double Max) yRange, int shownRows)
        {
            NumberAxis xAxis = NumberAxis.For(xRange.Min, xRange.Max, log: d.Spec.LogX);
            NumberAxis yAxis = NumberAxis.For(yRange.Min, yRange.Max, log: d.Spec.LogY);
            xAxis.Reversed = d.Shaping.Sort == SortOrder.Descending;
            var xSpec = AxisSpec.Of(d.Spec.TitleFor(Role.X), xAxis);
            xSpec.WholeNumbers = YearLike(x) && !d.Spec.LogX;
            d.Axes(xSpec, AxisSpec.Of(d.Spec.TitleFor(Role.Y), yAxis));
            if (shownRows == 0) d.Notice = NothingShown;
            return xSpec;
        }

        protected static double Mean(Dictionary<(int, int), double> totals, int code, List<int> across, bool codeFirst)
        {
            double sum = 0d;
            int count = 0;
            foreach (int other in across)
                if (totals.TryGetValue(codeFirst ? (code, other) : (other, code), out double v)) { sum += v; count++; }
            return count > 0 ? sum / count : 0d;
        }

        protected static bool YearLike(CsvField field)
        {
            if (field == null || !field.IsNumber) return false;
            string name = field.Name.ToLowerInvariant();
            if (name.Contains("year") || name == "yr") return true;
            foreach (double v in field.Numbers)
                if (!double.IsNaN(v) && (v != Math.Floor(v) || v < 1000d || v > 3000d)) return false;
            return true;
        }

        protected static CsvField Fold(ChartDrawing d, CsvField field, List<int> rows, CsvField value)
        {
            if (field == null || field.IsNumber) return field;
            Aggregate how = value == null ? Aggregate.Count : d.Spec.Aggregate;
            var sums = new Dictionary<int, double>();
            var counts = new Dictionary<int, int>();
            foreach (int r in rows)
            {
                int code = field.Codes[r];
                if (code < 0) continue;
                sums.TryGetValue(code, out double sum);
                counts.TryGetValue(code, out int count);
                sums[code] = sum + (how == Aggregate.Count ? 1d : value.Numbers[r]);
                counts[code] = count + 1;
            }
            if (sums.Count <= ChartStyle.MostShown) return field;

            double Size(int code) => how == Aggregate.Mean ? sums[code] / counts[code] : sums[code];
            var kept = new HashSet<int>(sums.Keys.OrderByDescending(Size).Take(ChartStyle.MostShown - 1));
            int others = field.Categories.Count;
            var folded = new CsvField
            {
                Name = field.Name,
                Categories = new List<string>(field.Categories) { Others },
                Codes = new int[field.Codes.Length],
                Folded = sums.Keys.Where(code => !kept.Contains(code)).OrderByDescending(Size).Select(code => field.Categories[code]).ToList()
            };
            for (int r = 0; r < field.Codes.Length; r++)
            {
                int code = field.Codes[r];
                folded.Codes[r] = code < 0 || kept.Contains(code) ? code : others;
            }
            return folded;
        }

        protected static bool IsOthers(CsvField field, int code) =>
            field != null && !field.IsNumber && code == field.Categories.Count - 1 && field.Categories[code] == Others;

        protected static void NoteOthers(MarkInfo info, CsvField field, int code)
        {
            if (IsOthers(field, code) && field.Folded != null) info.Includes(field.Folded);
        }

        protected static void OthersLast(CsvField field, List<int> codes)
        {
            int at = codes.FindIndex(code => IsOthers(field, code));
            if (at < 0 || at == codes.Count - 1) return;
            int code = codes[at];
            codes.RemoveAt(at);
            codes.Add(code);
        }

        protected static int ColourOf(CsvField field, List<int> allCodes, List<int> shownCodes, int code)
        {
            if (field == null) return 0;
            if (IsOthers(field, code)) return ChartStyle.MostShown - 1;
            bool folded = field.Categories.Count > 0 && IsOthers(field, field.Categories.Count - 1);
            if (!folded && allCodes.Count <= ChartStyle.MostShown) return allCodes.IndexOf(code);
            return shownCodes.Where(c => !IsOthers(field, c)).ToList().IndexOf(code);
        }

        protected static string Value(double v) => AxisFormat.Value(v);

        protected static List<int> Sample(List<int> rows, int most)
        {
            if (rows.Count <= most) return rows;
            var kept = new List<int>(most);
            double step = rows.Count / (double)most;
            for (int i = 0; i < most; i++) kept.Add(rows[(int)(i * step)]);
            return kept;
        }
    }
}
