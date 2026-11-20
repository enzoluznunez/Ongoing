using System.Collections.Generic;
using AccountingImmersiveAnalytics.Documents;
using AccountingImmersiveAnalytics.UI.Charts.View;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public sealed class DocumentOutcome
    {
        public DocumentFile Page;
        public string Failure;
        public string ChartFailure;
        public IReadOnlyList<ChartEntry> Charts;
    }
}
