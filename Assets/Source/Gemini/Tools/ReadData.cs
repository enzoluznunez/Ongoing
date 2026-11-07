using System;
using System.Collections.Generic;
using GeminiLive;

public sealed class ReadData : AgenticTool<ReadData.Args> {

    private const int MostGroups = 60;
    private const int MostRows = 40;

    public class Args {
        [Doc("The column to read, named as DescribeChart lists it."), Optional]
        public string column;
        [Doc("'sum', 'average', 'min', 'max' and 'count' summarize the column; 'rows' returns whole rows, up to 40."),
         Values("sum", "average", "min", "max", "count", "rows")]
        public string read;
        [Doc("A column to split the summary by, giving one answer per category, sorted largest first."), Optional]
        public string groupBy;
        [Doc("Only use rows whose 'whereColumn' holds this exact value."), Optional]
        public string whereValue;
        [Doc("The column 'whereValue' is compared with."), Optional]
        public string whereColumn;
    }

    public override FunctionDeclaration Declaration => new FunctionDeclaration {
        Name = "ReadData",
        Description = "Read the open chart's data: a total, average, minimum, maximum or count of a column, " +
                      "optionally split by a category and limited to rows matching one value, or the matching rows " +
                      "themselves. This is your only source of numbers.",
        Parameters = ParametersFor(typeof(Args))
    };

    protected override void Run(Args args, Dictionary<string, object> result) {
        ManageCharts charts = Scene.Charts;
        ManageCharts.Entry entry = charts != null ? charts.Active : null;
        if (entry == null || !entry.Loaded) { result["error"] = "No chart is open yet."; return; }
        DataTable table = entry.Table;

        DataColumn where = null;
        if (args.whereColumn != null || args.whereValue != null) {
            where = table.Column(args.whereColumn);
            if (where == null || args.whereValue == null) {
                result["error"] = "'whereColumn' and 'whereValue' go together, and the column must exist.";
                ListColumns(table, result);
                return;
            }
        }

        if (args.read == "rows") {
            ReadRows(table, where, args.whereValue, result);
            return;
        }

        DataColumn column = table.Column(args.column);
        bool needsNumbers = args.read != "count";
        if (column == null || (needsNumbers && !column.IsNumber)) {
            result["error"] = column == null
                ? $"There is no column called '{args.column}'."
                : $"'{args.column}' holds text, so it can only be counted.";
            ListColumns(table, result);
            return;
        }

        DataColumn group = null;
        if (args.groupBy != null) {
            group = table.Column(args.groupBy);
            if (group == null) {
                result["error"] = $"There is no column called '{args.groupBy}'.";
                ListColumns(table, result);
                return;
            }
        }

        var tallies = new Dictionary<string, Tally>();
        var order = new List<string>();
        for (int r = 0; r < table.RowCount; r++) {
            if (!Matches(where, args.whereValue, r)) continue;
            string key = group != null ? group.TextAt(r) ?? "(blank)" : "";
            if (!tallies.TryGetValue(key, out Tally tally)) {
                tally = new Tally();
                tallies[key] = tally;
                order.Add(key);
            }
            if (column.IsNumber) {
                if (!double.IsNaN(column.Numbers[r])) tally.Add(column.Numbers[r]);
            }
            else if (column.Codes[r] >= 0) tally.Add(0d);
        }

        if (group == null) {
            result["value"] = tallies.TryGetValue("", out Tally only) ? Round(only.Read(args.read)) : Round(0d);
            result["count"] = only != null ? only.Count : 0;
            return;
        }

        order.Sort((a, b) => tallies[b].Read(args.read).CompareTo(tallies[a].Read(args.read)));
        var groups = new List<object>();
        for (int i = 0; i < order.Count && i < MostGroups; i++)
            groups.Add(new Dictionary<string, object> {
                { group.Name, order[i] },
                { "value", Round(tallies[order[i]].Read(args.read)) },
                { "count", tallies[order[i]].Count }
            });
        result["groups"] = groups;
        if (order.Count > MostGroups) result["note"] = $"Showing the first {MostGroups} of {order.Count} groups.";
    }

    private static void ReadRows(DataTable table, DataColumn where, string value, Dictionary<string, object> result) {
        var rows = new List<object>();
        int matched = 0;
        for (int r = 0; r < table.RowCount; r++) {
            if (!Matches(where, value, r)) continue;
            matched++;
            if (rows.Count >= MostRows) continue;

            var row = new Dictionary<string, object>();
            foreach (DataColumn column in table.Columns)
                row[column.Name] = column.IsNumber && !double.IsNaN(column.Numbers[r])
                    ? Round(column.Numbers[r])
                    : column.TextAt(r);
            rows.Add(row);
        }
        result["rows"] = rows;
        result["matched"] = matched;
        if (matched > MostRows) result["note"] = $"Only the first {MostRows} of {matched} rows are listed; summarize for the rest.";
    }

    private static bool Matches(DataColumn where, string value, int row) =>
        where == null || string.Equals(where.TextAt(row), value.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void ListColumns(DataTable table, Dictionary<string, object> result) {
        var names = new List<object>();
        foreach (DataColumn column in table.Columns) names.Add(column.Name);
        result["columns"] = names;
    }

    private sealed class Tally {
        public int Count;
        private double _sum, _min, _max;

        public void Add(double value) {
            if (Count == 0) { _min = value; _max = value; }
            else {
                if (value < _min) _min = value;
                if (value > _max) _max = value;
            }
            _sum += value;
            Count++;
        }

        public double Read(string read) {
            switch (read) {
                case "sum": return _sum;
                case "average": return Count > 0 ? _sum / Count : 0d;
                case "min": return _min;
                case "max": return _max;
                default: return Count;
            }
        }
    }
}
