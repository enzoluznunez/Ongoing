using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class BreachMatch {

    public int Total { get; private set; }

    private readonly HashSet<string> _labels = new HashSet<string>();

    public static Task<(BreachMatch match, string error)> Look(BreachFilters where) {
        string bad = where.Check() ??
                     "Rows cannot be picked by what their breaches are in this build; name the rows instead.";
        return Task.FromResult<(BreachMatch, string)>((null, bad));
    }

    public List<int> DataRows(DataSource data) {
        var rows = new List<int>();
        if (data == null) return rows;
        foreach (int row in data.FilterLinesInOrder(rows: true))
            if (_labels.Contains(DataSource.RowLabelOfData(data, row))) rows.Add(row);
        return rows;
    }

    public List<int> RowsOn(DataSource data, Dictionary<string, object> result) {
        List<int> rows = DataRows(data);
        Report(data, rows, result);
        return rows;
    }

    public void Report(DataSource data, List<int> rows, Dictionary<string, object> result) {
        int hidden = 0;
        foreach (int row in rows) if (data.IsHidden(true, row)) hidden++;
        result["matchedInDatabase"] = Total;
        result["matchedOnSheet"] = rows.Count;
        if (hidden > 0) result["matchedButFilteredOff"] = hidden;
        if (rows.Count < Total)
            result["matchNote"] = $"{Total - rows.Count} of the {Total} matching breaches are not on this sheet, " +
                                  "which holds only its own place's breaches; the United States sheet holds every one.";
    }
}
