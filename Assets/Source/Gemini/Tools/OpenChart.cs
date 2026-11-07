using System.Collections.Generic;
using System.Threading.Tasks;
using GeminiLive;

public sealed class OpenChart : AgenticTool<OpenChart.Args> {

    public class Args {
        [Doc("The chart's title, or a part of it that fits only one chart.")]
        public string chart;
    }

    public override FunctionDeclaration Declaration => new FunctionDeclaration {
        Name = "OpenChart",
        Description = "Open one of the listed charts in place of the one showing. Each chart keeps its own position " +
                      "and undo history, so switching is always safe.",
        Parameters = ParametersFor(typeof(Args))
    };

    protected override async Task<string> Prepare(Args args) {
        Task<string> loading = null;
        await MainThread.Run(() => {
            ManageCharts charts = Scene.Charts;
            ManageCharts.Entry entry = charts != null ? charts.Find(args.chart) : null;
            if (entry != null) loading = charts.Load(entry);
        }).ConfigureAwait(false);
        return loading != null ? await loading.ConfigureAwait(false) : null;
    }

    protected override void Run(Args args, Dictionary<string, object> result) {
        ManageCharts charts = Scene.Charts;
        if (charts == null) { result["error"] = "The chart manager was not found in the scene."; return; }

        ManageCharts.Entry entry = charts.Find(args.chart);
        if (entry == null) {
            result["error"] = $"No single listed chart matches '{args.chart}'; if several match, ask the user which.";
            var titles = new List<object>();
            foreach (ManageCharts.Entry listed in charts.Entries) titles.Add(listed.Spec.Title);
            result["available"] = titles;
            return;
        }

        if (entry == charts.Active) result["alreadyOpen"] = true;
        else charts.Show(entry);
        result["opened"] = entry.Spec.Title;
    }
}
