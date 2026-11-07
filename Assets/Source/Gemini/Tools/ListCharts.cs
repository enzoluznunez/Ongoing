using System.Collections.Generic;
using GeminiLive;

public sealed class ListCharts : AgenticTool {

    public override FunctionDeclaration Declaration => new FunctionDeclaration {
        Name = "ListCharts",
        Description = "List the charts that can be opened, with what each one shows and which is open now."
    };

    protected override void Run(Dictionary<string, object> args, Dictionary<string, object> result) {
        ManageCharts charts = Scene.Charts;
        if (charts == null) { result["error"] = "The chart manager was not found in the scene."; return; }

        var list = new List<object>();
        foreach (ManageCharts.Entry entry in charts.Entries)
            list.Add(new Dictionary<string, object> {
                { "title", entry.Spec.Title },
                { "kind", entry.Spec.BlueprintKind },
                { "description", entry.Spec.Description },
                { "open", entry == charts.Active }
            });
        result["charts"] = list;
    }
}
