using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Charts.View;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolTargets;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Chart
{
    public sealed class CallFilter : AssistantTool<CallFilter.Args>
    {

        public class Args
        {
            [Doc("The chart to filter, by its title. Leave it out for the pinned chart, or when only one chart is shown."), Optional]
            public string chart;
            [Doc("The column to filter, named exactly as ReadChart lists the chart's fields. Leave it out to only open the Filter panel."), Optional]
            public string column;
            [Doc("For a number column: the lowest value to keep. Leave out min and max (and categories) to clear this column's filter."), Optional]
            public double? min;
            [Doc("For a number column: the highest value to keep."), Optional]
            public double? max;
            [Doc("For a text column: the categories to keep showing, named exactly as ReadChart lists them. At least one must stay."), Optional]
            public List<string> categories;
            [Doc("false switches the button off, as pressing it again does. Defaults to true."), DefaultsTo(true), Optional]
            public bool on;
        }

        protected override bool EditsAreOutcome => true;

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "CallFilter",
            Description = "Press the pinned chart's Filter button, which opens its Filter panel, and filter the rows behind a chart by one of its columns, then the chart recalculates (bar heights, counts and boxes change). A chart in the column that is not pinned changes on its card, and no panel opens. " +
                          "Axis ranges stay at the full data. Hidden categories close up on their axis, and hidden groups leave the key. " +
                          "Each column has its own filter; the others stay as they are.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            if (SwitchedOff(ToolType.Filter, args.on, result)) return;
            if (!TryShownChart(args.chart, result, out ChartEntry entry)) return;
            if (!EnsureToolFor(ToolType.Filter, entry, result)) return;
            if (string.IsNullOrWhiteSpace(args.column))
            {
                result["chart"] = entry.Title;
                result["filters"] = entry.Shaping.Summary(entry.Spec);
                result["fields"] = new List<object>(entry.Spec.UsedFields());
                result["ok"] = true;
                return;
            }
            CsvField field = entry.Table.Field(args.column);
            var used = new List<string>(entry.Spec.UsedFields());
            if (field == null || !used.Exists(name => string.Equals(name, field.Name, StringComparison.OrdinalIgnoreCase)))
            {
                result["error"] = $"'{args.column}' is not one of this chart's fields.";
                result["fields"] = new List<object>(used);
                return;
            }

            ChartShaping next = entry.Shaping.Copy();
            string what;
            string label;
            if (field.IsNumber)
            {
                if (!args.min.HasValue && !args.max.HasValue)
                {
                    next.Ranges.Remove(field.Name);
                    what = $"cleared the '{field.Name}' filter on \"{entry.Title}\"";
                    label = $"Showed all {field.Name}";
                }
                else
                {
                    double low = args.min ?? field.Min, high = args.max ?? field.Max;
                    if (high < low) (low, high) = (high, low);
                    next.Ranges[field.Name] = (low, high);
                    what = $"filtered \"{entry.Title}\" to '{field.Name}' from {low:G4} to {high:G4}";
                    label = $"Kept {field.Name} from {UI.Charts.Scales.AxisFormat.Value(low)} to {UI.Charts.Scales.AxisFormat.Value(high)}";
                }
                result["columnMin"] = Round(field.Min);
                result["columnMax"] = Round(field.Max);
            }
            else
            {
                if (args.categories == null || args.categories.Count == 0)
                {
                    next.Hidden.Remove(field.Name);
                    what = $"showed every '{field.Name}' on \"{entry.Title}\" again";
                    label = $"Showed every {field.Name}";
                }
                else
                {
                    var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var unknown = new List<object>();
                    foreach (string name in args.categories)
                    {
                        string match = field.Categories.Find(c => string.Equals(c, name?.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (match != null) keep.Add(match);
                        else unknown.Add(name);
                    }
                    if (keep.Count == 0)
                    {
                        result["error"] = "None of those categories are in this column; at least one must stay shown.";
                        result["categories"] = new List<object>(field.Categories);
                        return;
                    }
                    var hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string category in field.Categories)
                        if (!keep.Contains(category)) hidden.Add(category);
                    if (hidden.Count == 0) next.Hidden.Remove(field.Name);
                    else next.Hidden[field.Name] = hidden;
                    if (unknown.Count > 0) result["unknownCategories"] = unknown;
                    what = $"filtered \"{entry.Title}\" to {keep.Count} of {field.Categories.Count} '{field.Name}' values";
                    label = $"Kept {keep.Count} of {field.Categories.Count} {field.Name}";
                }
            }

            Scene.Charts.Reshape(entry, next, EditKind.Filter, what, label);
            result["chart"] = entry.Title;
            result["rowsShown"] = entry.Shaping.KeptCount(entry.Table);
            result["rowsTotal"] = entry.Table.RowCount;
            result["filters"] = entry.Shaping.Summary(entry.Spec);
        }
    }
}
