using System.Collections.Generic;
using GeminiLive;
using UnityEngine;

public sealed class DescribeChart : AgenticTool {

    private const int MostCategories = 24;

    public override FunctionDeclaration Declaration => new FunctionDeclaration {
        Name = "DescribeChart",
        Description = "Describe the open chart: what it shows, which data column drives which part of the chart, " +
                      "every column in its data with its range or categories, its row count, and its place and size " +
                      "in the room. It carries no individual values; use ReadData for those."
    };

    protected override void Run(Dictionary<string, object> args, Dictionary<string, object> result) {
        ManageCharts charts = Scene.Charts;
        ManageCharts.Entry entry = charts != null ? charts.Active : null;
        if (entry == null || !entry.Loaded) { result["error"] = "No chart is open yet."; return; }

        result["title"] = entry.Spec.Title;
        result["kind"] = entry.Spec.BlueprintKind;
        result["description"] = entry.Spec.Description;
        result["rows"] = entry.Table.RowCount;

        var encoding = new Dictionary<string, object>();
        foreach (KeyValuePair<string, string> pair in entry.Spec.Encoding) encoding[pair.Key] = pair.Value;
        result["encoding"] = encoding;

        var columns = new List<object>();
        foreach (DataColumn column in entry.Table.Columns) {
            var described = new Dictionary<string, object> { { "name", column.Name } };
            if (column.IsNumber) {
                described["min"] = Round(column.Min);
                described["max"] = Round(column.Max);
            }
            else {
                described["categoryCount"] = column.Categories.Count;
                if (column.Categories.Count <= MostCategories)
                    described["categories"] = new List<object>(column.Categories);
            }
            columns.Add(described);
        }
        result["columns"] = columns;

        ChartView view = charts.View;
        if (view == null) return;
        charts.GetCommittedPose(view, out Vector3 position, out _, out Vector3 scale);
        Vector3 world = charts.transform.TransformPoint(position);
        result["scale"] = Round(scale.x);
        result["worldPosition"] = new List<object> { Round(world.x), Round(world.y), Round(world.z) };
    }
}
