using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolResult;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Chart
{
    public sealed class Undo : AssistantTool<Undo.Args>
    {

        public class Args
        {
            [Doc("How many edits to undo, one at a time. Defaults to 1.")]
            [Limits(1, 1000), DefaultsTo(1), Optional]
            public int count;
            [Doc("The kind of edit you expect to be newest. Only checked for a single undo.")]
            [Values("move", "turn", "resize", "filter", "sort", "pin", "trend"), Optional]
            public string kind;
        }

        public override FunctionDeclaration Declaration
        {
            get
            {
                var response = new Schema
                {
                    Type = SchemaType.Object,
                    Properties = new Dictionary<string, Schema>()
                };
                response.Properties["undone"] = new Schema
                {
                    Type = SchemaType.Integer,
                    Description = "How many edits were reverted."
                };
                response.Properties["remaining"] = new Schema
                {
                    Type = SchemaType.Integer,
                    Description = "How many edits are left on the timeline."
                };
                response.Properties["failed"] = new Schema
                {
                    Type = SchemaType.Integer,
                    Description = "Records that no longer matched the scene and were dropped without reverting anything. Never report these as undone."
                };

                return new FunctionDeclaration
                {
                    Name = "Undo",
                    Description = "Undo recent edits, newest first. The Undo button on the pinned chart's Edits panel does the same for that chart's newest edit. " +
                                  "Sorts, filters, trends, pins, moves, turns and resizes share one timeline; 'count' stops when it runs out. " +
                                  "An undone edit cannot be brought back. " +
                                  "Optionally pass 'kind' to assert what the newest edit is; if it does not match, this refuses and names " +
                                  "what is actually next, so you can ask the user which they meant.",
                    Parameters = ParametersFor(typeof(Args)),
                    Response = response
                };
            }
        }

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            var controller = Scene.Tools;
            if (controller == null) { result["error"] = "The tool manager was not found in the scene."; return; }

            var top = ChartManager.ActiveEdits.Peek();
            if (top == null)
            {
                result["undone"] = 0;
                result["remaining"] = 0;
                result["note"] = "There are no edits to undo.";
                return;
            }

            int count = Math.Max(1, args.count);

            if (args.kind != null)
            {
                string requested = args.kind;
                if (Edit.KindName(top.Kind) != requested)
                {
                    Refuse(result, "matching newest edit",
                        $"The newest edit is a {Edit.KindName(top.Kind)} edit, not a {requested} edit. Undo always " +
                        "removes the newest edit first. Ask the user which they meant, then call again without 'kind'.");
                    result["nextUndo"] = Edit.KindName(top.Kind);
                    result["remaining"] = ChartManager.ActiveEdits.Count;
                    return;
                }
            }

            int did = 0, failed = 0;
            for (int i = 0; i < count && ChartManager.ActiveEdits.Peek() != null; i++)
            {
                if (controller.Undo()) did++;
                else failed++;
            }

            result["undone"] = did;
            if (failed > 0)
            {
                result["failed"] = failed;
                result["note"] = failed + " edit(s) could not be reverted because they no longer matched the scene; " +
                                 "they were dropped from the history. Tell the user rather than reporting success.";
            }
            else if (did < count)
                result["note"] = $"Only {did} of the {count} you asked for were on the timeline; the rest never existed.";
            result["remaining"] = ChartManager.ActiveEdits.Count;
            var next = ChartManager.ActiveEdits.Peek();
            if (next != null) result["nextUndo"] = Edit.KindName(next.Kind);
        }
    }
}
