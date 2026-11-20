using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.View;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolResult;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolTargets;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Chart
{
    public sealed class CallPin : AssistantTool<CallPin.Args>
    {

        public class Args
        {
            [Doc("The chart to pin, by its title as the chart column lists it. Leave it out with pin false to unpin.")]
            [Optional]
            public string chart;
            [Doc("true pins the chart in front of the Documents panel, replacing any pinned chart; false unpins it."), DefaultsTo(true), Optional]
            public bool pin;
        }

        protected override bool EditsAreOutcome => true;

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "CallPin",
            Description = "Press a chart in the column beside the Documents panel: pressing a card pins it, so a bigger copy " +
                          "of the card stands in front of the panel, and pressing the pinned chart's outlined card again unpins it (pin false). " +
                          "Only one chart is pinned at a time. The other Call tools act on the pinned chart.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            if (!TryCharts(result, out ChartManager charts)) return;
            if (!args.pin)
            {
                charts.Pin(null);
                result["pinned"] = null;
                return;
            }
            if (string.IsNullOrWhiteSpace(args.chart) && charts.Shown.Count != 1)
            {
                NeedChoice(result, "chart", ShownTitles(charts), "Name the chart to pin.");
                return;
            }
            if (!TryChart(args.chart, result, out ChartEntry entry)) return;
            charts.Pin(entry);
            result["pinned"] = charts.Pinned != null ? charts.Pinned.Title : null;
        }
    }
}
