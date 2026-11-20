using System;

namespace AccountingImmersiveAnalytics.Documents
{
    public sealed class DocumentEntry
    {
        public string Id;
        public string Title;
        public string File;
        public double PageWidth = LetterWidth;
        public double PageHeight = LetterHeight;
        public bool Showable;

        public const string CsvExtension = ".csv";
        public const double LetterWidth = 612, LetterHeight = 792;

        public bool IsCsv => File != null && File.EndsWith(CsvExtension, StringComparison.OrdinalIgnoreCase);
    }
}
