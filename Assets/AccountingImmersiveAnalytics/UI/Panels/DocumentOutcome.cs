using System.Collections.Generic;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Documents;
using AccountingImmersiveAnalytics.UI.Charts.View;
using OneText;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

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
