using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolTargets;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Chart
{
    public sealed class CallEdits : AssistantTool<CallEdits.Args>
    {

        public class Args
        {
            [Doc("false switches the button off, closing the Edits panel, as pressing it again does. Defaults to true."), DefaultsTo(true), Optional]
            public bool on;
        }

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "CallEdits",
            Description = "Press the pinned chart's Edits button, which opens its Edits panel below the chart: the chart's edits, " +
                          "newest first, each with who made it, and an Undo button. Returns the list. Undo takes back the newest edit.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            if (SwitchedOff(ToolType.Edits, args.on, result)) return;
            if (!TryPinnedChart(result, "The Edits button is beside a pinned chart, and none is pinned.", out ChartManager charts)) return;
            if (!EnsureToolSelected(ToolType.Edits, result)) return;
            result["chart"] = charts.Pinned.Title;

            var edits = new List<object>();
            foreach (Edit edit in ChartManager.ActiveEdits.For(charts.Pinned.Id))
                edits.Add(edit.Describe());
            result["edits"] = edits;
        }
    }
}
