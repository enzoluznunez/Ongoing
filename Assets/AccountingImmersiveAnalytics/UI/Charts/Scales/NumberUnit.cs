namespace AccountingImmersiveAnalytics.UI.Charts.Scales
{
    public readonly struct NumberUnit
    {
        public readonly double Threshold, Divisor;
        public readonly string Prefix, Suffix, OnePrefix, OneSuffix;

        public NumberUnit(double threshold, double divisor, string prefix, string suffix, string onePrefix = null, string oneSuffix = null)
        {
            Threshold = threshold;
            Divisor = divisor;
            Prefix = prefix;
            Suffix = suffix;
            OnePrefix = onePrefix ?? prefix;
            OneSuffix = oneSuffix ?? suffix;
        }
    }
}
