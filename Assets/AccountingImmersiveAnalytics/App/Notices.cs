using System.Collections;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    public static class Notices
    {
        private const float NoticeSeconds = 3f;

        private static int _lastToastFrame = -1;
        private static int _generation;

        public static void Show(MonoBehaviour host, string message)
        {
            ActivityFeed.Record(message);

            if (Time.frameCount == _lastToastFrame) return;
            _lastToastFrame = Time.frameCount;

            Tooltip tooltip = Scene.Tooltip;
            if (tooltip == null || host == null || !host.isActiveAndEnabled) return;

            _generation++;
            tooltip.ShowNotice(message);
            host.StartCoroutine(HideAfterDelay(_generation));
        }

        public static void HideNotice()
        {
            _generation++;
            Tooltip tooltip = Scene.Tooltip;
            if (tooltip != null) tooltip.DismissNotice();
        }

        private static IEnumerator HideAfterDelay(int generation)
        {
            yield return new WaitForSecondsRealtime(NoticeSeconds);
            if (generation == _generation) HideNotice();
        }
    }
}
