using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public sealed class DataColumn
{
    public string Name;
    public bool IsNumber;
    public double[] Numbers;
    public int[] Codes;
    public List<string> Categories;
    public double Min = double.NaN;
    public double Max = double.NaN;

    public string TextAt(int row) =>
        IsNumber
            ? (double.IsNaN(Numbers[row]) ? null : Numbers[row].ToString(CultureInfo.InvariantCulture))
            : (Codes[row] < 0 ? null : Categories[Codes[row]]);
}

public sealed class DataTable
{
    private readonly Dictionary<string, DataColumn> _byName =
        new Dictionary<string, DataColumn>(StringComparer.OrdinalIgnoreCase);

    public readonly List<DataColumn> Columns = new List<DataColumn>();
    public int RowCount { get; private set; }

    public DataColumn Column(string name) =>
        name != null && _byName.TryGetValue(name.Trim(), out DataColumn column) ? column : null;

    public static DataTable ParseCsv(string text)
    {
        List<List<string>> rows = SplitRows(text);
        if (rows.Count < 2) throw new FormatException("The dataset has no data rows.");

        List<string> header = rows[0];
        var table = new DataTable { RowCount = rows.Count - 1 };
        for (int c = 0; c < header.Count; c++)
        {
            DataColumn column = ReadColumn(header[c].Trim(), rows, c);
            table.Columns.Add(column);
            table._byName[column.Name] = column;
        }
        return table;
    }

    private static DataColumn ReadColumn(string name, List<List<string>> rows, int at)
    {
        int count = rows.Count - 1;
        var numbers = new double[count];
        bool numeric = true;
        bool any = false;
        for (int r = 0; r < count && numeric; r++)
        {
            string field = FieldAt(rows[r + 1], at);
            if (field.Length == 0) { numbers[r] = double.NaN; continue; }
            numeric = double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[r]);
            any |= numeric;
        }

        var column = new DataColumn { Name = name };
        if (numeric && any)
        {
            column.IsNumber = true;
            column.Numbers = numbers;
            for (int r = 0; r < count; r++)
            {
                if (double.IsNaN(numbers[r])) continue;
                if (double.IsNaN(column.Min) || numbers[r] < column.Min) column.Min = numbers[r];
                if (double.IsNaN(column.Max) || numbers[r] > column.Max) column.Max = numbers[r];
            }
            return column;
        }

        column.Codes = new int[count];
        column.Categories = new List<string>();
        var codes = new Dictionary<string, int>();
        for (int r = 0; r < count; r++)
        {
            string field = FieldAt(rows[r + 1], at);
            if (field.Length == 0) { column.Codes[r] = -1; continue; }
            if (!codes.TryGetValue(field, out int code))
            {
                code = column.Categories.Count;
                codes[field] = code;
                column.Categories.Add(field);
            }
            column.Codes[r] = code;
        }
        return column;
    }

    private static string FieldAt(List<string> row, int at) => at < row.Count ? row[at].Trim() : "";

    private static List<List<string>> SplitRows(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder(32);
        bool quoted = false;
        int start = text.Length > 0 && text[0] == '﻿' ? 1 : 0;

        void EndRow()
        {
            row.Add(field.ToString());
            field.Clear();
            bool blank = row.Count == 1 && row[0].Trim().Length == 0;
            if (!blank) rows.Add(row);
            row = new List<string>(rows.Count > 0 ? rows[0].Count : 8);
        }

        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (quoted)
            {
                if (c != '"') field.Append(c);
                else if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = false;
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (c == '\n') EndRow();
            else if (c != '\r') field.Append(c);
        }
        if (field.Length > 0 || row.Count > 0) EndRow();
        return rows;
    }
}
