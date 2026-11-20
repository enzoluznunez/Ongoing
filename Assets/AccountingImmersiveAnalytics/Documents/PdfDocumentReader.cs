using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace AccountingImmersiveAnalytics.Documents
{
    public static class PdfDocumentReader
    {
        public static string TitleOf(byte[] bytes)
        {
            try
            {
                using (PdfDocument pdf = PdfDocument.Open(bytes)) return pdf.Information?.Title;
            }
            catch
            {
                return null;
            }
        }

        public static (string Title, double Width, double Height)? Peek(byte[] bytes)
        {
            try
            {
                using (PdfDocument pdf = PdfDocument.Open(bytes))
                {
                    if (pdf.NumberOfPages < 1) return null;
                    Page first = pdf.GetPage(1);
                    return (pdf.Information?.Title, first.Width, first.Height);
                }
            }
            catch
            {
                return null;
            }
        }

        public static DocumentFile Read(byte[] bytes, DocumentEntry entry)
        {
            var file = new DocumentFile { Entry = entry, Title = entry.Title };
            using (PdfDocument pdf = PdfDocument.Open(bytes))
            {
                foreach (Page page in pdf.GetPages())
                {
                    int index = file.Pages.Count;
                    file.Pages.Add(new DocumentPage { Width = page.Width, Height = page.Height });
                    foreach (DocumentPassage passage in Passages(page, index)) file.Passages.Add(passage);
                    file.Tables.AddRange(PdfTables.Find(page, index));
                }
            }
            file.Finish();
            return file;
        }

        private static IEnumerable<DocumentPassage> Passages(Page page, int index)
        {
            List<Word> words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters)
                .Where(word => !string.IsNullOrWhiteSpace(word.Text)).ToList();
            if (words.Count == 0) yield break;

            IReadOnlyList<TextBlock> blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
            foreach (TextBlock block in UnsupervisedReadingOrderDetector.Instance.Get(blocks))
            {
                string text = Join(block.TextLines.Select(line => line.Text));
                if (!text.Any(char.IsLetterOrDigit)) continue;
                double top = page.Height > 0 ? (page.Height - block.BoundingBox.Top) / page.Height : 0;
                yield return new DocumentPassage { Text = text, Page = index, Top = System.Math.Max(0, System.Math.Min(1, top)) };
            }
        }

        private static string Join(IEnumerable<string> lines)
        {
            var text = new StringBuilder();
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (text.Length > 0)
                {
                    bool split = text[text.Length - 1] == '-' && char.IsLower(line[0])
                                 && text.Length > 1 && char.IsLetter(text[text.Length - 2]);
                    if (split) text.Length--;
                    else text.Append(' ');
                }
                text.Append(line);
            }
            return text.ToString();
        }
    }
}
