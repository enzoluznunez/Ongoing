using System.Text;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant;
using AccountingImmersiveAnalytics.Assistant.Live;
using Newtonsoft.Json.Linq;

namespace AccountingImmersiveAnalytics.Documents
{
    public sealed class PageSummary
    {
        private const int MostPageText = 12000;

        public string Summary;

        public static async Task<PageSummary> Write(DocumentFile page)
        {
            JObject answer = await FlashLite.Ask(Instructions(), Prompt(page),
                FlashLite.Schema(("summary", "STRING")), 25);
            string summary = ((string)answer?["summary"])?.Trim();
            return string.IsNullOrEmpty(summary) ? null : new PageSummary { Summary = summary };
        }

        private static string Instructions() =>
            "You are the Assistant in a mixed-reality app for people who do not read data or science well. " +
            "The user opened a document in a reader panel. Write the short card shown above it.\n" +
            "'summary': two or three plain sentences saying what the document is about and what it shows, with an everyday " +
            "comparison when it helps.\n" +
            "Use only what the document says. Never invent a number or a claim.\n" +
            "Write in English; the app translates it for the user.\n" +
            SystemPrompt.Explanations.Replace("# Explanations\n", "");

        private static string Prompt(DocumentFile page)
        {
            var text = new StringBuilder(MostPageText + 512);
            text.Append("Title: ").Append(page.Title).Append('\n');
            text.Append("\nParagraphs:\n");
            for (int i = 0; i < page.Passages.Count && text.Length < MostPageText; i++)
                text.Append('[').Append(i).Append("] ").Append(page.Passages[i].Text).Append("\n\n");
            return text.ToString();
        }
    }
}
