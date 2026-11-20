using System;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Arguments
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DefaultsToAttribute : Attribute
    {
        public readonly object Value;
        public DefaultsToAttribute(object value) { Value = value; }
    }
}
