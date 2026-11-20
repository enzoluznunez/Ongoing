using System.Collections.Generic;

namespace AccountingImmersiveAnalytics.Assistant.FlashLite
{
    public struct Explanation
    {
        public List<string> Lines;

        public string Text => Lines == null ? "" : string.Join(" ", Lines);
    }
}
