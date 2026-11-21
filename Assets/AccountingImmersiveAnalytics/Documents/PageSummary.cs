using System;
using System.Text;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant;
using AccountingImmersiveAnalytics.Assistant.FlashLite;
using AccountingImmersiveAnalytics.Data;
using Newtonsoft.Json.Linq;

namespace AccountingImmersiveAnalytics.Documents
{
    public static class PageSummary
    {
        private const int MostPageText = 12000;

        public static Task<string> Write(DocumentFile page) => Ask(Prompt(page));

        public static async Task<string> WriteTable(DocumentEntry entry)
        {
            string csv = await StreamingFiles.ReadText(entry.File);
            if (string.IsNullOrWhiteSpace(csv)) return null;
            return await Ask(TablePrompt(entry.Title, csv));
        }

        private static async Task<string> Ask(string prompt)
        {
            JObject answer = await FlashLiteClient.AskOnce(Instructions, prompt,
                FlashLiteClient.Schema(("summary", "STRING")), 25);
            string summary = ((string)answer?["summary"])?.Trim();
            return string.IsNullOrEmpty(summary) ? null : summary;
        }

        private static string TablePrompt(string title, string csv)
        {
            var text = new StringBuilder(MostPageText + 512);
            text.Append("Title: ").Append(title).Append('\n');
            text.Append("\nThis document is a table of data in CSV form; say what it records.\n");
            text.Append("\nTable:\n");
            if (csv.Length > MostPageText) text.Append(csv, 0, MostPageText).Append("\n(The table goes on past this point.)");
            else text.Append(csv);
            return text.ToString();
        }

        private static readonly string Instructions =
            "You are the Assistant in a mixed-reality app for people who do not read data or science well. " +
            "The user opened a document in a reader panel. Write the short card shown above it.\n" +
            "'summary': two or three plain sentences saying what the document is about and what it shows, with an everyday " +
            "comparison when it helps.\n" +
            "Use only what the document says. Never invent a number or a claim.\n" +
            "Write in English; the app translates it for the user.\n" +
            SystemPrompt.ExplanationRules;

        private static string Prompt(DocumentFile page)
        {
            var text = new StringBuilder(MostPageText + 512);
            text.Append("Title: ").Append(page.Title).Append('\n');
            text.Append("\nParagraphs:\n");
            for (int i = 0; i < page.Passages.Count && text.Length < MostPageText; i++)
            {
                string passage = page.Passages[i].Text;
                text.Append('[').Append(i).Append("] ");
                int room = Math.Max(0, MostPageText - text.Length);
                if (passage.Length > room) text.Append(passage, 0, room).Append("\n(The document goes on past this point.)");
                else text.Append(passage).Append("\n\n");
            }
            return text.ToString();
        }
    }
}
