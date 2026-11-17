using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.Spec;

namespace AccountingImmersiveAnalytics.UI.Charts.View
{
    public sealed class ChartEntry
    {
        public int Id;
        public string File;
        public string Source;
        public ChartSpec Spec;
        public CsvTable Table;
        public ChartShaping Shaping = new ChartShaping();
        public ChartCard Preview;
        public System.Collections.Generic.List<string> LeftOut = new System.Collections.Generic.List<string>();

        public string Title => Spec.Title;
    }
}
