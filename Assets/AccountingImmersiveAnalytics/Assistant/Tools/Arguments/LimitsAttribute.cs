using System;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Arguments
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LimitsAttribute : Attribute
    {
        public readonly double Min;
        public readonly double Max;
        public LimitsAttribute(double min, double max) { Min = min; Max = max; }
    }
}
