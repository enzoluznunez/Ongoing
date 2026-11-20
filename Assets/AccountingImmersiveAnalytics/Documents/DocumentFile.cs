using System;
using System.Collections.Generic;
using System.Text;

namespace AccountingImmersiveAnalytics.Documents
{
    public sealed class DocumentFile
    {
        private const int WordsPerMinute = 230;
        private const int CharactersPerWordUnspaced = 2;

        public DocumentEntry Entry;
        public string Title => Entry.Title;
        public readonly List<DocumentPage> Pages = new List<DocumentPage>();
        public readonly List<DocumentPassage> Passages = new List<DocumentPassage>();
        public readonly List<PdfTable> Tables = new List<PdfTable>();
        private int? _words;

        public int Minutes => Math.Max(1, (int)Math.Round((_words ??= CountWords(Passages)) / (double)WordsPerMinute));

        public bool HasText => Passages.Count > 0;

        public string BodyText(int most)
        {
            var text = new StringBuilder();
            foreach (DocumentPassage passage in Passages)
            {
                if (text.Length + passage.Text.Length > most) break;
                text.Append(passage.Text).Append("\n\n");
            }
            return text.ToString();
        }

        private static int CountWords(List<DocumentPassage> passages)
        {
            int words = 0, unspaced = 0;
            foreach (DocumentPassage passage in passages)
            {
                bool inWord = false;
                foreach (char c in passage.Text)
                {
                    if ((c >= '぀' && c <= 'ヿ') || (c >= '㐀' && c <= '鿿') || (c >= '฀' && c <= '໿'))
                    {
                        unspaced++;
                        inWord = false;
                        continue;
                    }
                    bool letter = char.IsLetterOrDigit(c);
                    if (letter && !inWord) words++;
                    inWord = letter;
                }
            }
            return words + unspaced / CharactersPerWordUnspaced;
        }
    }
}
