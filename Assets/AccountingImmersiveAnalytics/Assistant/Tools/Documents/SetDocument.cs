using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.Documents;
using AccountingImmersiveAnalytics.UI.Charts.View;
using AccountingImmersiveAnalytics.UI.Panels;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Documents
{
    public sealed class SetDocument : AssistantTool<SetDocument.Args>
    {
        private const int MostTextSent = 8000;
        private const string None = "none";

        public class Args
        {
            [Doc("The document to open: its title, or words from it, as the system prompt lists them. 'none' closes the open document.")]
            public string title;
        }

        private sealed class Opened
        {
            public DocumentEntry Entry;
            public DocumentOutcome Outcome;
        }

        private readonly ConditionalWeakTable<Args, Opened> _opened = new ConditionalWeakTable<Args, Opened>();

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "SetDocument",
            Description = "Choose the document shown in the Documents panel, as pressing a page in the documents column does. " +
                          "Give a title to open it: the panel opens if it was closed. A PDF shows its pages exactly as printed and " +
                          "returns its text; each table in it becomes one chart in the scrolling chart column beside the panel, of the kind " +
                          "its caption names, and the result lists them. A CSV file cannot be displayed as pages; the panel shows a short " +
                          "summary above a blank page, and its charts are planned and shown in the chart column. " +
                          "Give 'none' to close the open document, as pressing its outlined page does: the panel goes back to US Letter " +
                          "size, its title goes blank, it asks the user to select a document, and the charts beside it close. " +
                          "To hide or show the panel without changing its document, use SetPanel.",
            Parameters = ParametersFor(typeof(Args))
        };

        private static bool Closing(Args args) => string.Equals(args.title?.Trim(), None, StringComparison.OrdinalIgnoreCase);

        protected override async Task<string> Prepare(Args args)
        {
            if (string.IsNullOrWhiteSpace(args.title)) return "Give the title of the document to open, or 'none' to close the open one.";
            if (Closing(args)) return null;

            Task loading = null;
            await MainThread.Run(() => loading = DocumentLibrary.Load()).ConfigureAwait(false);
            await loading.ConfigureAwait(false);
            Task<DocumentOutcome> opening = null;
            DocumentEntry entry = null;
            string problem = null;
            await MainThread.Run(() =>
            {
                entry = DocumentLibrary.Find(args.title);
                DocumentsPanel documents = Scene.Documents;
                if (documents == null) problem = "The Documents panel was not found in the scene.";
                else if (entry == null) problem = $"There is no single document matching '{args.title}'. The documents are: {DocumentLibrary.ForPrompt()}.";
                else opening = documents.Open(entry);
            }).ConfigureAwait(false);
            if (problem != null) return problem;
            DocumentOutcome outcome = await opening.ConfigureAwait(false);
            _opened.Add(args, new Opened { Entry = entry, Outcome = outcome });
            return null;
        }

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            if (Closing(args))
            {
                Close(result);
                return;
            }
            _opened.TryGetValue(args, out Opened opened);
            _opened.Remove(args);
            Report(opened, args.title, result);
        }

        private static void Close(Dictionary<string, object> result)
        {
            DocumentsPanel documents = Scene.Documents;
            if (documents == null)
            {
                result["error"] = "The Documents panel was not found in the scene.";
                return;
            }
            DocumentEntry shown = documents.Shown;
            if (shown == null)
            {
                result["note"] = "No document is open, so there is nothing to close.";
                result["ok"] = true;
                return;
            }
            documents.CloseDocument();
            result["closed"] = shown.Title;
        }

        private static void Report(Opened opened, string asked, Dictionary<string, object> result)
        {
            DocumentOutcome outcome = opened?.Outcome;
            if (outcome == null)
            {
                result["error"] = $"The document '{asked}' could not be opened.";
                return;
            }

            bool csv = opened.Entry != null && opened.Entry.IsCsv;
            result["opened"] = opened.Entry?.Title ?? asked;
            result["file"] = csv ? "CSV" : "PDF";

            DocumentFile page = outcome.Page;
            if (page == null)
            {
                if (outcome.Charts == null)
                {
                    result.Remove("opened");
                    result.Remove("file");
                    result["error"] = outcome.Failure ?? $"The document '{asked}' could not be opened.";
                    return;
                }
                result["charts"] = Charts(outcome.Charts);
                result["note"] = (csv
                    ? "A CSV file cannot be shown as pages, so the panel shows a short summary above a blank page saying it cannot be displayed. "
                    : $"The pages could not be drawn ({outcome.Failure}), so the panel shows a blank page saying it cannot be displayed. ") +
                    "The charts stand in the column beside the panel; CallPin pins one in front of it.";
                return;
            }

            result["title"] = page.Title;
            result["pages"] = page.Pages.Count;
            string text = page.BodyText(MostTextSent);
            if (text.Length > 0) result["text"] = text;
            else result["note"] = "The document has no readable text, only pictures of its pages, so you cannot read or quote it.";
            if (outcome.Charts != null) result["charts"] = Charts(outcome.Charts);
            if (outcome.ChartFailure != null) result["chartsProblem"] = outcome.ChartFailure;
        }

        private static List<object> Charts(IReadOnlyList<ChartEntry> made)
        {
            var charts = new List<object>();
            foreach (ChartEntry entry in made)
                charts.Add(new Dictionary<string, object>
                {
                    { "title", entry.Title }, { "kind", entry.Spec.Describe() }, { "insight", entry.Spec.Insight ?? "" }
                });
            return charts;
        }
    }
}
