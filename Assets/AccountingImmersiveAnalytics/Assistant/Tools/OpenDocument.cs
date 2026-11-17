using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Session;
using AccountingImmersiveAnalytics.Documents;
using AccountingImmersiveAnalytics.UI.Panels;

namespace AccountingImmersiveAnalytics.Assistant.Tools
{
    public sealed class OpenDocument : AgenticTool<OpenDocument.Args> {

        private const int MostTextSent = 8000;

        public class Args {
            [Doc("The document's title, or words from it, as the system prompt lists them.")]
            public string title;
        }

        private readonly ConditionalWeakTable<Args, DocumentOutcome> _outcomes = new ConditionalWeakTable<Args, DocumentOutcome>();

        public override FunctionDeclaration Declaration => new FunctionDeclaration {
            Name = "OpenDocument",
            Description = "Open one of the app's documents in the Documents panel. A PDF shows its pages exactly as printed and " +
                          "returns its text. A CSV file cannot be displayed in the panel; instead its charts are planned and shown " +
                          "in the scrolling chart column beside the panel, and the result lists them.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override async Task<string> Prepare(Args args) {
            if (string.IsNullOrWhiteSpace(args.title)) return "Give the title of the document to open.";
            Task loading = null;
            await MainThread.Run(() => loading = DocumentLibrary.Load()).ConfigureAwait(false);
            await loading.ConfigureAwait(false);
            Task<DocumentOutcome> opening = null;
            string problem = null;
            await MainThread.Run(() => {
                DocumentEntry entry = DocumentLibrary.Find(args.title);
                DocumentsPanel documents = Scene.Documents;
                if (documents == null) problem = "The Documents panel was not found in the scene.";
                else if (entry == null) problem = $"There is no single document matching '{args.title}'. The documents are: {DocumentLibrary.ForPrompt()}.";
                else opening = documents.Open(entry);
            }).ConfigureAwait(false);
            if (problem != null) return problem;
            DocumentOutcome outcome = await opening.ConfigureAwait(false);
            if (outcome != null) _outcomes.Add(args, outcome);
            return null;
        }

        protected override void Run(Args args, Dictionary<string, object> result) {
            _outcomes.TryGetValue(args, out DocumentOutcome outcome);
            _outcomes.Remove(args);
            Report(outcome, args.title, result);
        }

        private static void Report(DocumentOutcome outcome, string asked, Dictionary<string, object> result) {
            if (outcome != null && outcome.Charts != null) {
                var charts = new List<object>();
                foreach (UI.Charts.View.ChartEntry entry in outcome.Charts)
                    charts.Add(new Dictionary<string, object> {
                        { "title", entry.Title }, { "kind", entry.Spec.Describe() }, { "insight", entry.Spec.Insight ?? "" }
                    });
                result["opened"] = asked;
                result["file"] = "CSV";
                result["charts"] = charts;
                result["note"] = "The panel says the file could not be displayed; the charts stand in the column beside it. CallPin pins one in front of the panel.";
                return;
            }
            if (outcome == null || outcome.Page == null) {
                result["error"] = outcome?.Failure ?? $"The document '{asked}' could not be opened.";
                return;
            }

            DocumentFile page = outcome.Page;
            result["title"] = page.Title;
            result["pages"] = page.Pages.Count;
            string text = page.BodyText(MostTextSent);
            if (text.Length > 0) result["text"] = text;
            else result["note"] = "The document has no readable text, only pictures of its pages, so you cannot read or quote it.";
        }
    }
}
