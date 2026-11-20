using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.UI.Charts.Spec;

namespace AccountingImmersiveAnalytics.UI.Charts
{
    public enum EditKind { Move, Rotate, Scale, Filter, Sort, Pin, Trend }

    public class Edit
    {
        public EditKind kind;
        public MoveRecord move;
        public int chartId;
        public ChartShaping before;
        public int pinnedBefore = -1;
        public string what;
        public bool byAssistant = ActivityFeed.InAssistantCall;

        public static string KindName(EditKind kind) =>
            kind == EditKind.Rotate ? "turn" : kind == EditKind.Scale ? "resize" : kind.ToString().ToLowerInvariant();
    }
}
