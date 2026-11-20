using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.UI.Charts.Spec;

namespace AccountingImmersiveAnalytics.UI.Charts
{
    public enum EditKind { Move, Rotate, Scale, Filter, Sort, Pin, Trend }

    public class Edit
    {
        public EditKind Kind;
        public MoveRecord Move;
        public int ChartId;
        public ChartShaping Before;
        public int PinnedBefore = -1;
        public string What;
        public bool ByAssistant = ActivityFeed.InAssistantCall;

        public static string KindName(EditKind kind) =>
            kind == EditKind.Rotate ? "turn" : kind == EditKind.Scale ? "resize" : kind.ToString().ToLowerInvariant();

        public string Describe() => $"{What ?? KindName(Kind)} ({(ByAssistant ? "Assistant" : "You")})";
    }
}
