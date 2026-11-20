using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Charts.View;
using Newtonsoft.Json.Linq;

namespace AccountingImmersiveAnalytics.Assistant.FlashLite
{
    public static class MarkExplainer
    {
        private const int MostRowsSent = 200;
        private const int MostCategoriesSent = 30;

        private static readonly Dictionary<string, Explanation> Cache = new Dictionary<string, Explanation>();
        private static readonly Dictionary<string, List<Action<Explanation?>>> Waiting =
            new Dictionary<string, List<Action<Explanation?>>>();

        private static readonly JObject AnswerSchema = FlashLiteClient.Schema(("lines", "STRING_LIST"));

        public static bool TryCached(ChartEntry entry, MarkInfo mark, out Explanation explanation) =>
            Cache.TryGetValue(KeyFor(entry, mark), out explanation);

        public static bool IsWriting(string key) => Waiting.ContainsKey(key);

        public static void Request(ChartEntry entry, MarkInfo mark, Action<Explanation?> done)
        {
            string key = KeyFor(entry, mark);
            if (Cache.TryGetValue(key, out Explanation cached))
            {
                done(cached);
                return;
            }
            if (Waiting.TryGetValue(key, out List<Action<Explanation?>> callbacks))
            {
                callbacks.Add(done);
                return;
            }

            Waiting[key] = new List<Action<Explanation?>> { done };
            Ask(key, entry, mark);
        }

        private static async void Ask(string key, ChartEntry entry, MarkInfo detail)
        {
            Explanation? result = null;
            try
            {
                JObject answer = await FlashLiteClient.AskOnce(Instructions, Context(entry, detail), AnswerSchema);
                result = Read(answer, detail.Rows.Count);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[MarkExplainer] The explanation could not be written: {e.Message}");
            }
            finally
            {
                Finish(key, result);
            }
        }

        public static string KeyFor(ChartEntry entry, MarkInfo mark) =>
            $"{entry.File}|{entry.Spec.Title}|{entry.Spec.Describe()}|{entry.Shaping.Summary(entry.Spec)}|{mark?.Describe()}";

        private static void Finish(string key, Explanation? result)
        {
            if (result.HasValue) Cache[key] = result.Value;
            if (!Waiting.TryGetValue(key, out List<Action<Explanation?>> callbacks)) return;
            Waiting.Remove(key);
            foreach (Action<Explanation?> callback in callbacks) callback(result);
        }

        private static Explanation? Read(JObject answer, int rows)
        {
            if (!(answer?["lines"] is JArray lines)) return null;
            var kept = new List<string>();
            foreach (JToken line in lines)
            {
                if (kept.Count >= rows) break;
                kept.Add(((string)line)?.Trim() ?? "");
            }
            if (kept.TrueForAll(string.IsNullOrEmpty)) return null;
            return new Explanation { Lines = kept };
        }

        private const string Instructions =
            "You are the Assistant in a mixed-reality data app. The user pinched one shape in a chart to ask " +
            "what it means. Its tooltip has rows, each a field name and its value. Write 'lines': exactly one line per row, " +
            "in the same order, for someone who does not read charts well.\n" +
            "For a row that names a thing (a region, a product, a year), say what it is in this file, such as " +
            "\"One of the four sales regions in this file.\" " +
            "For a row with a number, say what the number counts and give the single most useful comparison that makes it " +
            "meaningful, such as its rank, how it differs from its neighbour or the average, or its share of a total, such as " +
            "\"Total sales across the year, about 15% more than North, the next best region.\" Do not repeat the value; the row shows it.\n" +
            "Work any figure out only from the data given, check it, and round it to what is easy to read. " +
            "Never invent a number or a cause the data does not show. Each line is one sentence of at most 18 words and ends with a period.\n" +
            "Write in English; the app translates it for the user.\n" +
            SystemPrompt.ExplanationRules;

        private static string Context(ChartEntry entry, MarkInfo detail)
        {
            ChartSpec spec = entry.Spec;
            CsvTable table = entry.Table;
            var text = new StringBuilder(4096);
            text.Append("Chart: ").Append(spec.Title).Append(" (").Append(spec.Describe()).Append(")\n");
            if (!string.IsNullOrEmpty(spec.Insight)) text.Append("About: ").Append(spec.Insight).Append('\n');
            text.Append("Sort and filters: ").Append(entry.Shaping.Summary(entry.Spec)).Append('\n');

            text.Append("Fields:\n");
            foreach (KeyValuePair<Role, string> field in spec.Fields)
                text.Append("- ").Append(field.Key.ToString().ToLowerInvariant()).Append(" = column '").Append(field.Value).Append("' shown as \"")
                    .Append(spec.TitleFor(field.Key)).Append("\"\n");

            text.Append("\nThe pinched shape's tooltip rows, in order:\n");
            foreach (KeyValuePair<string, string> row in detail.Rows) text.Append("- ").Append(row.Key).Append(": ").Append(row.Value).Append('\n');
            if (detail.Members.Count > 0)
                text.Append("This shape is 'Others': the items past the 9 largest, added together. It holds: ").Append(string.Join(", ", detail.Members)).Append('\n');

            text.Append("\nColumns (").Append(table.RowCount).Append(" rows):\n");
            foreach (CsvField column in table.Fields) AppendSummary(text, column, table.RowCount);

            if (table.RowCount <= MostRowsSent) AppendRows(text, table);
            else text.Append("\nThe dataset is too large to list; use the tooltip and the column summaries.\n");
            return text.ToString();
        }

        private static void AppendSummary(StringBuilder text, CsvField column, int rowCount)
        {
            text.Append("- ").Append(column.Name).Append(": ");
            if (column.IsNumber)
            {
                double sum = 0d;
                int count = 0;
                for (int r = 0; r < rowCount; r++)
                {
                    if (double.IsNaN(column.Numbers[r])) continue;
                    sum += column.Numbers[r];
                    count++;
                }
                text.Append("numbers from ").Append(Number(column.Min)).Append(" to ").Append(Number(column.Max));
                if (count > 0) text.Append(", average ").Append(Number(sum / count)).Append(", total ").Append(Number(sum));
            }
            else
            {
                text.Append(column.Categories.Count).Append(" categories");
                int shown = Math.Min(column.Categories.Count, MostCategoriesSent);
                if (shown > 0) text.Append(": ").Append(string.Join(", ", column.Categories.GetRange(0, shown)));
                if (shown < column.Categories.Count) text.Append(", …");
            }
            text.Append('\n');
        }

        private static void AppendRows(StringBuilder text, CsvTable table)
        {
            text.Append("\nAll rows (CSV):\n");
            for (int c = 0; c < table.Fields.Count; c++)
            {
                if (c > 0) text.Append(',');
                text.Append(table.Fields[c].Name);
            }
            text.Append('\n');
            for (int r = 0; r < table.RowCount; r++)
            {
                for (int c = 0; c < table.Fields.Count; c++)
                {
                    if (c > 0) text.Append(',');
                    text.Append(table.Fields[c].TextAt(r));
                }
                text.Append('\n');
            }
        }

        private static string Number(double value) =>
            Math.Round(value, 3).ToString(CultureInfo.InvariantCulture);
    }
}
