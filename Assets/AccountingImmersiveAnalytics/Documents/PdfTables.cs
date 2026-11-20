using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Content;

namespace AccountingImmersiveAnalytics.Documents
{
    public static class PdfTables
    {
        private const double CellGap = 1.2;
        private const double SameLine = 0.35;
        private const double StepSlack = 0.25;
        private const double CaptionReach = 4;
        private const double CaptionLeading = 1.8;
        private const int FewestRows = 2;

        private sealed class Line
        {
            public double Baseline;
            public double Size;
            public readonly List<Word> Words = new List<Word>();
            public List<string> Cells;
            public string Text;
        }

        public static List<PdfTable> Find(IEnumerable<Word> words, int index)
        {
            var tables = new List<PdfTable>();
            List<Line> lines = Lines(words);
            int at = 0;
            while (at < lines.Count)
            {
                int end = TableEnd(lines, at);
                if (end - at < FewestRows + 1)
                {
                    at++;
                    continue;
                }
                var table = new PdfTable { Page = index, Caption = CaptionAfter(lines, end) };
                for (int i = at; i < end; i++) table.Rows.Add(lines[i].Cells);
                tables.Add(table);
                at = end;
            }
            return tables;
        }

        private static int TableEnd(List<Line> lines, int start)
        {
            int columns = lines[start].Cells.Count;
            if (columns < 2) return start;
            int end = start + 1;
            double step = 0;
            while (end < lines.Count && lines[end].Cells.Count == columns)
            {
                double gap = lines[end - 1].Baseline - lines[end].Baseline;
                if (step == 0) step = gap;
                else if (Math.Abs(gap - step) > step * StepSlack) break;
                end++;
            }
            return end;
        }

        private static string CaptionAfter(List<Line> lines, int end)
        {
            if (end >= lines.Count || end < 1) return null;
            Line first = lines[end];
            if (lines[end - 1].Baseline - first.Baseline > first.Size * CaptionReach) return null;
            if (!first.Text.StartsWith("Table", StringComparison.OrdinalIgnoreCase)) return null;
            var caption = new StringBuilder(first.Text);
            for (int i = end + 1; i < lines.Count && lines[i].Cells.Count == 1; i++)
            {
                if (lines[i - 1].Baseline - lines[i].Baseline > lines[i].Size * CaptionLeading) break;
                caption.Append(' ').Append(lines[i].Text);
            }
            return caption.ToString();
        }

        private static List<Line> Lines(IEnumerable<Word> words)
        {
            var lines = new List<Line>();
            foreach (Word word in words.OrderByDescending(Baseline).ThenBy(word => word.BoundingBox.Left))
            {
                double size = Size(word), baseline = Baseline(word);
                Line line = lines.Count > 0 ? lines[lines.Count - 1] : null;
                if (line == null || Math.Abs(line.Baseline - baseline) > Math.Max(line.Size, size) * SameLine)
                {
                    line = new Line { Baseline = baseline, Size = size };
                    lines.Add(line);
                }
                line.Size = Math.Max(line.Size, size);
                line.Words.Add(word);
            }
            foreach (Line line in lines)
            {
                line.Cells = Cells(line);
                line.Text = string.Join(" ", line.Cells);
            }
            return lines;
        }

        private static List<string> Cells(Line line)
        {
            var cells = new List<string>();
            var cell = new StringBuilder();
            double right = double.NaN;
            foreach (Word word in line.Words.OrderBy(w => w.BoundingBox.Left))
            {
                if (cell.Length > 0)
                {
                    if (word.BoundingBox.Left - right > line.Size * CellGap)
                    {
                        cells.Add(cell.ToString());
                        cell.Clear();
                    }
                    else cell.Append(' ');
                }
                cell.Append(word.Text.Trim());
                right = word.BoundingBox.Right;
            }
            if (cell.Length > 0) cells.Add(cell.ToString());
            return cells;
        }

        private static double Baseline(Word word) => word.Letters[0].StartBaseLine.Y;

        private static double Size(Word word)
        {
            double size = 0;
            foreach (Letter letter in word.Letters) size = Math.Max(size, letter.PointSize);
            return size > 1 ? size : Math.Max(1, word.BoundingBox.Height);
        }
    }
}
