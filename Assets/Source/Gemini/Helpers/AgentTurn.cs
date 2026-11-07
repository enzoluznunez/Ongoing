using System.Collections.Generic;
using UnityEngine;

public static class AgentTurn
{
    public static bool Marked { get; private set; }
    public static bool UserTookOver { get; private set; }

    private static readonly Dictionary<EditList, int> baselines = new Dictionary<EditList, int>();

    public static void NoteToolCall()
    {
        Marked = true;
        EditList edits = ManageCharts.ActiveEdits;
        if (edits != null && !baselines.ContainsKey(edits)) baselines[edits] = edits.Count;
    }

    private static int BaselineOf(EditList edits) =>
        edits != null && baselines.TryGetValue(edits, out int start) ? start : edits?.Count ?? 0;

    public static int AppliedSoFar()
    {
        EditList edits = ManageCharts.ActiveEdits;
        if (!Marked || edits == null) return 0;
        return Mathf.Max(0, edits.Count - BaselineOf(edits));
    }

    public static void UserTookControl()
    {
        ManageCharts charts = Scene.Charts;
        if (charts != null) charts.Interrupt();

        UserTookOver = true;

        int applied = AppliedSoFar();
        if (applied <= 0) return;

        int stamped = ManageCharts.ActiveEdits.StampGroup(BaselineOf(ManageCharts.ActiveEdits));
        if (stamped <= 0) return;

        StateChannel.Record($"the user stopped you after {stamped} of your changes had been applied");

        if (charts != null)
            Notices.Show(charts, Strings.Get("notice.stopped"), stamped == 1
                ? Strings.Get("notice.stopped.one")
                : Strings.Format("notice.stopped.many", stamped));
    }

    public static void Clear()
    {
        Marked = false;
        UserTookOver = false;
        baselines.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Clear();
}
