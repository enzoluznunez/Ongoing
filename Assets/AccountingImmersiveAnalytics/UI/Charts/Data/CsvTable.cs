using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AccountingImmersiveAnalytics.UI.Charts.Data
{
    public sealed class CsvTable
    {
        private readonly Dictionary<string, CsvField> _byName = new Dictionary<string, CsvField>(StringComparer.OrdinalIgnoreCase);

        public readonly List<CsvField> Fields = new List<CsvField>();
        public int RowCount { get; private set; }

        public CsvField Field(string name) =>
            name != null && _byName.TryGetValue(name.Trim(), out CsvField field) ? field : null;

        public static bool LooksLikeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string head = text.TrimStart();
            if (head.StartsWith("<") || head.StartsWith("{") || head.StartsWith("[") || head.StartsWith("%PDF")) return false;
            int line = head.IndexOf('\n');
            return line > 0 && head.Substring(0, line).Contains(",") && head.Substring(line + 1).Trim().Length > 0;
        }

        public static CsvTable Parse(string text) => FromRows(SplitRows(text));

        public static CsvTable FromRows(List<List<string>> rows)
        {
            if (rows.Count < 2) throw new FormatException("The file has no data rows.");
            List<string> header = rows[0];
            for (int r = 1; r < rows.Count; r++)
                for (int c = 0; c < header.Count; c++)
                    if (Cell(rows[r], c).Length == 0)
                        throw new FormatException($"Row {r + 1} has a blank cell in column {c + 1}.");
            var table = new CsvTable { RowCount = rows.Count - 1 };
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < header.Count; c++)
            {
                string name = header[c].Trim();
                if (name.Length == 0) name = $"Column {c + 1}";
                while (!seen.Add(name)) name += " (2)";
                CsvField field = Read(name, rows, c);
                table.Fields.Add(field);
                table._byName[field.Name] = field;
            }
            return table;
        }

        private static CsvField Read(string name, List<List<string>> rows, int at)
        {
            int count = rows.Count - 1;
            var numbers = new double[count];
            bool numeric = true, any = false;
            for (int r = 0; r < count && numeric; r++)
            {
                string cell = Cell(rows[r + 1], at);
                if (cell.Length == 0) { numbers[r] = double.NaN; continue; }
                numeric = TryNumber(cell, out numbers[r]);
                any |= numeric;
            }

            var field = new CsvField { Name = name };
            if (numeric && any)
            {
                field.IsNumber = true;
                field.Numbers = numbers;
                foreach (double v in numbers)
                {
                    if (double.IsNaN(v)) continue;
                    if (double.IsNaN(field.Min) || v < field.Min) field.Min = v;
                    if (double.IsNaN(field.Max) || v > field.Max) field.Max = v;
                }
                return field;
            }

            field.Codes = new int[count];
            field.Categories = new List<string>();
            var codes = new Dictionary<string, int>();
            for (int r = 0; r < count; r++)
            {
                string cell = Cell(rows[r + 1], at);
                if (cell.Length == 0) { field.Codes[r] = -1; continue; }
                if (!codes.TryGetValue(cell, out int code))
                {
                    code = field.Categories.Count;
                    codes[cell] = code;
                    field.Categories.Add(cell);
                }
                field.Codes[r] = code;
            }
            return field;
        }

        private static bool TryNumber(string cell, out double value)
        {
            string clean = cell.Replace("$", "").Replace("%", "").Replace(" ", "");
            if (clean.Length > 1 && clean[0] == '(' && clean[clean.Length - 1] == ')') clean = "-" + clean.Substring(1, clean.Length - 2);
            clean = clean.Replace(",", "");
            return double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static string Cell(List<string> row, int at) => at < row.Count ? row[at].Trim() : "";

        public static List<List<string>> SplitRows(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder(32);
            bool quoted = false;
            int start = text.Length > 0 && text[0] == '﻿' ? 1 : 0;

            void EndRow()
            {
                row.Add(cell.ToString());
                cell.Clear();
                bool blank = row.Count == 1 && row[0].Trim().Length == 0;
                if (!blank) rows.Add(row);
                row = new List<string>(rows.Count > 0 ? rows[0].Count : 8);
            }

            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c != '"') cell.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else quoted = false;
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n') EndRow();
                else if (c != '\r') cell.Append(c);
            }
            if (cell.Length > 0 || row.Count > 0) EndRow();
            return rows;
        }
    }
}
