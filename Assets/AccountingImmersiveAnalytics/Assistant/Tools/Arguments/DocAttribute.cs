using System;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Arguments
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DocAttribute : Attribute
    {
        public readonly string Text;
        public DocAttribute(string text) { Text = text; }
    }
}
