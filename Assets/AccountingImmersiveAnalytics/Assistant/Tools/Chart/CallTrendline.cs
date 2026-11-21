using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Charts.View;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolResult;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolTargets;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Chart
{
    public sealed class CallTrendline : AssistantTool<CallTrendline.Args>
    {

        public class Args
        {
            [Doc("The chart, by its title. Leave it out for the pinned chart, or when only one chart is shown."), Optional]
            public string chart;
            [Doc("The trend to draw, as the Trend button on the pinned chart's Trendline panel offers them, or 'none' to turn it off. " +
                 "Leave it out to only open the Trendline panel."), Optional]
            public TrendKind? trend;
            [Doc("false switches the button off, as pressing it again does. Defaults to true."), DefaultsTo(true), Optional]
            public bool on;
        }

        protected override bool EditsAreOutcome => true;

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "CallTrendline",
            Description = "Press the pinned chart's Trendline button, which opens its Trendline panel, and turn on, change or turn off the trend line a scatter or bubble chart draws. Charts start with no trend line; only the user or you can add one. Other charts have none. A chart in the column that is not pinned changes on its card, and no panel opens. " +
                          "Exponential needs every y above 0, logarithmic every x above 0, power both, polynomial three different x values, " +
                          "and smooth at least five rows. A trend the data cannot take is kept but draws nothing. " +
                          "The result says in plain words what the trend means.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            if (SwitchedOff(ToolType.Trendline, args.on, result)) return;
            if (!TryShownChart(args.chart, result, out ChartEntry entry)) return;
            if (!TrendFitter.Offered(entry.Spec))
            {
                Refuse(result, "chart that takes a trend", $"A {entry.Spec.Describe()} has no trend line. Say so to the user.");
                return;
            }
            if (!EnsureToolFor(ToolType.Trendline, entry, result)) return;
            if (args.trend == null)
            {
                result["chart"] = entry.Title;
                result["trend"] = entry.Shaping.Trend.ToString().ToLowerInvariant();
                result["ok"] = true;
                return;
            }
            Scene.Charts.SetTrend(entry, args.trend.Value);
            result["chart"] = entry.Title;
            result["trend"] = entry.Shaping.Trend.ToString().ToLowerInvariant();
            if (entry.Shaping.Trend == TrendKind.None) return;
            TrendFit fit = TrendFitter.Fit(entry.Spec, entry.Table, entry.Shaping, entry.Shaping.Trend);
            if (fit != null) result["meaning"] = fit.Meaning;
            else result["note"] = "This trend does not fit the data, so nothing is drawn. Tell the user why.";
        }
    }
}
