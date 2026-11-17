using System;
using System.Collections.Generic;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

public enum Run { Regular, Bold, Italic, Code, Strike }

public sealed class SampleWriter
{
    private const double Width = 595;
    private const double Height = 842;
    private const double Margin = 60;
    private const double Body = 11;
    private const double Leading = 1.45;

    private readonly PdfDocumentBuilder _builder = new PdfDocumentBuilder();
    private readonly PdfDocumentBuilder.AddedFont _regular;
    private readonly PdfDocumentBuilder.AddedFont _bold;
    private readonly PdfDocumentBuilder.AddedFont _italic;
    private readonly PdfDocumentBuilder.AddedFont _mono;
    private PdfPageBuilder _page;
    private double _y;

    public static string Fonts;

    public SampleWriter(string title, string author)
    {
        _builder.DocumentInformation.Title = title;
        _builder.DocumentInformation.Author = author;
        _regular = _builder.AddTrueTypeFont(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Fonts, "NotoSans-Regular.ttf")));
        _bold = _builder.AddTrueTypeFont(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Fonts, "NotoSans-Bold.ttf")));
        _italic = _builder.AddTrueTypeFont(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Fonts, "NotoSans-Italic.ttf")));
        _mono = _builder.AddTrueTypeFont(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Fonts, "NotoSansMono-Regular.ttf")));
        NewPage();
    }

    public byte[] Build() => _builder.Build();

    private void NewPage()
    {
        _page = _builder.AddPage(Width, Height);
        _y = Height - Margin;
    }

    private void Need(double height)
    {
        if (_y - height < Margin) NewPage();
    }

    private PdfDocumentBuilder.AddedFont FontOf(Run run) =>
        run == Run.Bold ? _bold : run == Run.Italic ? _italic : run == Run.Code ? _mono : _regular;

    private double Measure(string text, double size, PdfDocumentBuilder.AddedFont font)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var letters = _page.MeasureText(text, size, new PdfPoint(0, 0), font);
        return letters.Count == 0 ? 0 : letters[letters.Count - 1].EndBaseLine.X - letters[0].StartBaseLine.X;
    }

    public void Title(string text)
    {
        Lines(new List<(string, Run)> { (text, Run.Bold) }, 22, 0);
        _y -= 8;
    }

    public void Heading(string text, int level)
    {
        double size = level == 1 ? 16 : 13;
        _y -= 10;
        Need(size * 3 + Body * 5);
        Lines(new List<(string, Run)> { (text, Run.Bold) }, size, 0);
        _y -= 2;
    }

    public void Paragraph(params (string text, Run run)[] runs)
    {
        Lines(new List<(string, Run)>(runs), Body, 0);
        _y -= Body * 0.6;
    }

    public void Bullets(params string[] items)
    {
        foreach (string item in items) Item("•", new List<(string, Run)> { (item, Run.Regular) });
        _y -= Body * 0.4;
    }

    public void Numbered(params string[] items)
    {
        for (int i = 0; i < items.Length; i++) Item($"{i + 1}.", new List<(string, Run)> { (items[i], Run.Regular) });
        _y -= Body * 0.4;
    }

    private void Item(string marker, List<(string, Run)> runs)
    {
        Need(Body * Leading);
        _page.AddText(marker, Body, new PdfPoint(Margin + 6, _y - Body), _regular);
        Lines(runs, Body, 22);
    }

    public void Code(string line)
    {
        double size = Body - 1;
        Need(size * 3);
        _page.SetStrokeColor(235, 235, 235);
        _page.SetTextAndFillColor(242, 242, 242);
        _page.DrawRectangle(new PdfPoint(Margin, _y - size * 2.1), Width - Margin * 2, size * 2.1, 0.5, true);
        _page.ResetColor();
        _page.AddText(line, size, new PdfPoint(Margin + 8, _y - size * 1.45), _mono);
        _y -= size * 2.1 + Body * 0.8;
    }

    public void Image(byte[] png, double aspect, string caption)
    {
        double width = Width - Margin * 2, height = width / aspect;
        Need(height + Body * 4);
        _page.AddPng(png, new PdfRectangle(Margin, _y - height, Margin + width, _y));
        _y -= height + 6;
        Lines(new List<(string, Run)> { (caption, Run.Italic) }, Body - 1.5, 0);
        _y -= Body * 0.6;
    }

    public void Table(params string[][] rows)
    {
        double size = Body - 1, rowHeight = size * 2;
        double width = Width - Margin * 2, cell = width / rows[0].Length;
        Need(rowHeight * rows.Length + Body);
        for (int r = 0; r < rows.Length; r++)
        {
            double top = _y - r * rowHeight;
            if (r == 0)
            {
                _page.SetTextAndFillColor(230, 230, 230);
                _page.DrawRectangle(new PdfPoint(Margin, top - rowHeight), width, rowHeight, 0.5, true);
                _page.ResetColor();
            }
            _page.DrawLine(new PdfPoint(Margin, top - rowHeight), new PdfPoint(Margin + width, top - rowHeight), 0.5);
            for (int c = 0; c < rows[r].Length; c++)
                _page.AddText(rows[r][c], size, new PdfPoint(Margin + c * cell + 6, top - rowHeight + size * 0.65), r == 0 ? _bold : _regular);
        }
        _page.DrawLine(new PdfPoint(Margin, _y), new PdfPoint(Margin + width, _y), 0.5);
        _y -= rowHeight * rows.Length + Body;
    }

    private void Lines(List<(string text, Run run)> runs, double size, double indent)
    {
        double left = Margin + indent, right = Width - Margin, line = size * Leading;
        var words = new List<List<(string text, Run run)>>();
        var current = new List<(string text, Run run)>();
        foreach ((string text, Run run) in runs)
        {
            var piece = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (c != ' ')
                {
                    piece.Append(c);
                    continue;
                }
                if (piece.Length > 0) current.Add((piece.ToString(), run));
                piece.Clear();
                if (current.Count > 0) words.Add(current);
                current = new List<(string, Run)>();
            }
            if (piece.Length > 0) current.Add((piece.ToString(), run));
        }
        if (current.Count > 0) words.Add(current);

        Need(line);
        double x = left;
        double space = Measure(" ", size, _regular);
        foreach (List<(string text, Run run)> word in words)
        {
            double wide = 0;
            foreach ((string text, Run run) in word) wide += Measure(text, size, FontOf(run));
            if (x + wide > right && x > left)
            {
                _y -= line;
                Need(line);
                x = left;
            }
            foreach ((string text, Run run) in word)
            {
                double part = Measure(text, size, FontOf(run));
                _page.AddText(text, size, new PdfPoint(x, _y - size), FontOf(run));
                if (run == Run.Strike) _page.DrawLine(new PdfPoint(x, _y - size * 0.68), new PdfPoint(x + part, _y - size * 0.68), 0.8);
                x += part;
            }
            x += space;
        }
        _y -= line;
    }
}
