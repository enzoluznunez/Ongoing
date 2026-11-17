using System.Collections.Generic;

namespace AccountingImmersiveAnalytics.UI.Charts.Spec
{
    public sealed class TrendFit
    {
        public TrendKind Kind;
        public readonly List<(double X, double Y)> Curve = new List<(double X, double Y)>();
        public string Meaning;
    }
}
