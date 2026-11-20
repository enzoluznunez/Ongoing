using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Charts.View;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Chart
{
    public sealed class ReadChart : AssistantTool<ReadChart.Args>
    {

        private const int MostCategories = 24;

        public class Args
        {
            [Doc("The chart to describe, by its title. Leave it out to list every chart shown, with the pinned one described in full."), Optional]
            public string chart;
        }

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "ReadChart",
            Description = "Read the charts in the column beside the Documents panel and which one is pinned. For one chart: its kind, " +
                          "what it shows, which column drives which part, its sort and filters, the tools it offers, every column in its " +
                          "file with its range or categories, and, when pinned, its place and size in the room. It carries no individual values; use ReadData.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            ChartManager charts = Scene.Charts;
            if (charts == null) { result["error"] = "The chart manager was not found in the scene."; return; }

            var shown = new List<object>();
            foreach (ChartEntry entry in charts.Shown)
                shown.Add(new Dictionary<string, object> {
                    { "title", entry.Title }, { "kind", entry.Spec.Describe() }, { "pinned", entry == charts.Pinned }
                });
            result["charts"] = shown;
            if (charts.Status != null) result["status"] = charts.Status;
            result["pinned"] = charts.Pinned != null ? charts.Pinned.Title : null;

            ChartEntry chosen = string.IsNullOrWhiteSpace(args.chart) ? charts.Active : charts.Find(args.chart);
            if (chosen == null)
            {
                if (!string.IsNullOrWhiteSpace(args.chart)) result["error"] = $"No single chart matches '{args.chart}'.";
                else if (shown.Count == 0) result["note"] = "No charts are shown. Open a document with SetDocument.";
                return;
            }
            Describe(charts, chosen, result);
        }

        private static void Describe(ChartManager charts, ChartEntry entry, Dictionary<string, object> result)
        {
            ChartSpec spec = entry.Spec;
            result["title"] = spec.Title;
            result["kind"] = spec.Describe();
            result["file"] = entry.Source;
            if (!string.IsNullOrEmpty(spec.Insight)) result["insight"] = spec.Insight;
            result["rows"] = entry.Table.RowCount;
            if (spec.FieldFor(Role.Value) != null || spec.Kind == ChartKind.Bar || spec.Kind == ChartKind.Pie) result["aggregate"] = spec.FieldFor(Role.Value) == null ? "count" : spec.Aggregate.ToString().ToLowerInvariant();

            var fields = new Dictionary<string, object>();
            foreach (RoleRule rule in ChartKinds.RulesFor(spec.Kind))
            {
                string name = spec.FieldFor(rule.Role);
                if (name != null) fields[rule.Role.ToString().ToLowerInvariant()] = $"{spec.TitleFor(rule.Role)} (column '{name}')";
            }
            result["fields"] = fields;
            result["shaping"] = entry.Shaping.Summary(spec);
            if (entry.LeftOut.Count > 0) result["notShown"] = $"Left out because their values are 0 or below: {string.Join(", ", entry.LeftOut)}";
            result["others"] = "When more than 10 categories, groups or tiles would show, the 9 largest show and the rest are added together as 'Others'.";

            var tools = new List<object>();
            foreach (ToolType tool in ToolManager.ChartTools)
                if ((tool != ToolType.Sort || ChartKinds.Sorts(spec.Kind)) && (tool != ToolType.Trendline || TrendFitter.Offered(spec)))
                    tools.Add(tool.ToString());
            result["tools"] = tools;

            var edits = new List<object>();
            foreach (Edit edit in ChartManager.ActiveEdits.For(entry.Id))
                edits.Add($"{edit.What} ({(edit.ByAssistant ? "Assistant" : "You")})");
            result["edits"] = edits;
            if (TrendFitter.Offered(spec))
            {
                TrendFit fit = TrendFitter.Fit(spec, entry.Table, entry.Shaping, entry.Shaping.Trend);
                result["trend"] = entry.Shaping.Trend == TrendKind.None ? "no trend line (off)"
                    : fit != null ? fit.Meaning : $"{entry.Shaping.Trend} (does not fit the data, so nothing is drawn)";
            }

            var columns = new List<object>();
            foreach (CsvField field in entry.Table.Fields)
            {
                var described = new Dictionary<string, object> { { "name", field.Name } };
                if (field.IsNumber)
                {
                    described["min"] = Round(field.Min);
                    described["max"] = Round(field.Max);
                }
                else
                {
                    described["categoryCount"] = field.Categories.Count;
                    if (field.Categories.Count <= MostCategories) described["categories"] = new List<object>(field.Categories);
                }
                columns.Add(described);
            }
            result["columns"] = columns;

            if (entry != charts.Pinned || charts.PinnedTransform == null) return;
            Transform pinned = charts.PinnedTransform;
            charts.GetCommittedPose(pinned, out Vector3 position, out _, out Vector3 scale);
            Vector3 world = pinned.parent != null ? pinned.parent.TransformPoint(position) : position;
            result["scale"] = Round(scale.x);
            result["worldPosition"] = new List<object> { Round(world.x), Round(world.y), Round(world.z) };
        }
    }
}
