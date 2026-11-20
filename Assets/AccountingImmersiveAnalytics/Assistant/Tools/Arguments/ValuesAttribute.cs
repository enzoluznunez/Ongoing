using System;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Arguments
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ValuesAttribute : Attribute
    {
        public readonly string[] Allowed;
        public ValuesAttribute(params string[] allowed) { Allowed = allowed; }
    }
}
