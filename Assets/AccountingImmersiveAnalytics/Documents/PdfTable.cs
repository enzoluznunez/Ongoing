using System.Collections.Generic;

namespace AccountingImmersiveAnalytics.Documents
{
    public sealed class PdfTable
    {
        public int Page;
        public string Caption;
        public readonly List<List<string>> Rows = new List<List<string>>();
    }
}
