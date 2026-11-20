using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Charts;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Context
{
    public static class AssistantTurn
    {
        public static bool UserTookOver { get; private set; }

        private static readonly Dictionary<EditList, int> _baselines = new Dictionary<EditList, int>();

        public static void NoteToolCall()
        {
            EditList edits = ChartManager.ActiveEdits;
            if (!_baselines.ContainsKey(edits)) _baselines[edits] = edits.Count;
        }

        public static int AppliedSoFar()
        {
            EditList edits = ChartManager.ActiveEdits;
            return _baselines.TryGetValue(edits, out int start) ? Mathf.Max(0, edits.Count - start) : 0;
        }

        public static void UserActed()
        {
            if (!ActivityFeed.InAssistantCall) UserTookControl();
        }

        public static void UserTookControl()
        {
            ChartManager charts = Scene.Charts;
            if (charts != null) charts.Interrupt();

            UserTookOver = true;

            int applied = AppliedSoFar();
            if (applied <= 0) return;

            ActivityFeed.Record($"the user stopped you after {applied} of your changes had been applied");

            if (charts != null)
                Notices.Show(charts, applied == 1
                    ? Strings.Original("notice.stopped.one")
                    : Strings.FormatOriginal("notice.stopped.many", applied));
        }

        public static void Clear()
        {
            UserTookOver = false;
            _baselines.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();
    }
}
