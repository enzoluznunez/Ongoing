using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Charts.View;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolResult;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolTargets;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Chart
{
    public sealed class CallSort : AssistantTool<CallSort.Args>
    {

        public class Args
        {
            [Doc("The chart to sort, by its title. Leave it out for the pinned chart, or when only one chart is shown."), Optional]
            public string chart;
            [Doc("'ascending' or 'descending' orders the categories by value (bars, donut slices, heatmap rows; box plots by median). " +
                 "On a line, scatter or bubble chart 'descending' flips the x axis. 'none' returns to the data's own order. " +
                 "Leave it out to only open the Sort panel."), Optional]
            public SortOrder? order;
            [Doc("false switches the button off, as pressing it again does. Defaults to true."), DefaultsTo(true), Optional]
            public bool on;
        }

        protected override bool EditsAreOutcome => true;

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "CallSort",
            Description = "Press the pinned chart's Sort button, which opens its Sort panel, and sort the chart or clear its sort. Histograms and treemaps cannot be sorted: their order is fixed by their bins or their layout.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            if (SwitchedOff(ToolType.Sort, args.on, result)) return;
            if (!TryChart(args.chart, result, out ChartEntry entry)) return;
            if (!ChartKinds.Sorts(entry.Spec.Kind))
            {
                Refuse(result, "sortable chart", $"A {entry.Spec.Describe()} has no Sort: its order is fixed. Say so to the user.");
                return;
            }
            if (!EnsureToolSelected(ToolType.Sort, result)) return;
            if (args.order == null)
            {
                result["chart"] = entry.Title;
                result["order"] = entry.Shaping.Sort.ToString().ToLowerInvariant();
                result["ok"] = true;
                return;
            }
            SortOrder order = args.order.Value;
            ChartShaping next = entry.Shaping.Copy();
            next.Sort = order;
            string label = order == SortOrder.None ? "Put the chart back in data order"
                : $"Sorted the chart, {(order == SortOrder.Ascending ? "lowest" : "highest")} first";
            Scene.Charts.Reshape(entry, next, EditKind.Sort, $"sorted \"{entry.Title}\" {order.ToString().ToLowerInvariant()}", label);
            result["chart"] = entry.Title;
            result["order"] = entry.Shaping.Sort.ToString().ToLowerInvariant();
        }
    }
}
