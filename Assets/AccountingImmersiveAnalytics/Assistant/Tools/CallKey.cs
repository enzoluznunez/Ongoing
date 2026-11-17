using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Session;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Layout;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Charts.View;

namespace AccountingImmersiveAnalytics.Assistant.Tools
{
    public sealed class CallKey : AgenticTool<CallKey.Args> {

        public class Args {
            [Doc("false switches the button off, closing the Key panel, as pressing it again does. Defaults to true."), DefaultsTo(true), Optional]
            public bool? on;
        }

        public override FunctionDeclaration Declaration => new FunctionDeclaration {
            Name = "CallKey",
            Description = "Press the pinned chart's Key button, which opens its Key panel below the chart. Returns what the key " +
                          "shows: each section's title and its entries, or a colour scale's labels. Charts with nothing to explain have no key.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result) {
            if (SwitchedOff(ToolType.Key, args.on, result)) return;
            ChartManager charts = Scene.Charts;
            ChartCard card = charts != null ? charts.PinnedCard : null;
            if (card == null) {
                Refuse(result, "pinned chart", "The Key button is beside a pinned chart, and none is pinned. Pin one with CallPin first.");
                return;
            }
            if (card.Key.Count == 0) {
                Refuse(result, "chart with a key", $"\"{charts.Pinned.Title}\" has no key: nothing on it needs explaining. Say so to the user.");
                return;
            }
            if (!EnsureToolSelected(ToolType.Key, result)) return;
            result["on"] = true;
            result["chart"] = charts.Pinned.Title;

            var sections = new List<object>();
            foreach (KeySection section in card.Key) {
                var described = new Dictionary<string, object> { { "title", section.Title ?? "" } };
                if (section.IsRamp) described["scale"] = new List<object>(section.RampLabels);
                else {
                    var entries = new List<object>();
                    foreach (KeyEntry entry in section.Entries) entries.Add(entry.Name);
                    described["entries"] = entries;
                }
                sections.Add(described);
            }
            result["key"] = sections;
        }
    }
}
