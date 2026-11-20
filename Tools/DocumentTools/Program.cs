using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AccountingImmersiveAnalytics.Documents;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length >= 4 && args[0] == "samples")
        {
            SampleWriter.Fonts = args[3];
            Samples.Write(args[1], args[2]);
            return 0;
        }
        if (args.Length >= 3 && args[0] == "accounting")
        {
            SampleWriter.Fonts = args[2];
            AccountingSamples.Write(args[1]);
            return 0;
        }
        if (args.Length >= 2 && args[0] == "read")
        {
            DocumentFile file = PdfDocumentReader.Read(File.ReadAllBytes(args[1]), new DocumentEntry { Id = "test", Title = PdfDocumentReader.TitleOf(File.ReadAllBytes(args[1])) ?? Path.GetFileNameWithoutExtension(args[1]) });
            Console.WriteLine($"title: {file.Title} | pages: {file.Pages.Count} | passages: {file.Passages.Count} | minutes: {file.Minutes}");
            foreach (DocumentPassage passage in file.Passages) Console.WriteLine($"p{passage.Page + 1} top {passage.Top:0.00}: {passage.Text}");
            foreach (PdfTable table in file.Tables)
            {
                Console.WriteLine($"table on p{table.Page + 1}, {table.Rows.Count - 1} rows: {table.Caption}");
                foreach (List<string> row in table.Rows) Console.WriteLine("  " + string.Join(" | ", row));
            }
            return 0;
        }
        Console.WriteLine("usage: samples <output> <data> <fonts> | accounting <output> <fonts> | read <pdf>");
        return 1;
    }
}
