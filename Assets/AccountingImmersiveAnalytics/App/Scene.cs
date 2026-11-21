using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    public static class Scene
    {

        private static ToolManager _toolManager;
        private static Menu _menu;
        private static ChartManager _chartManager;
        private static Tooltip _tooltip;
        private static DocumentsPanel _documentsPanel;

        private const float RetrySeconds = 1f;
        private static float _toolManagerRetry, _menuRetry, _chartManagerRetry, _tooltipRetry, _documentsPanelRetry;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRetries()
        {
            _toolManagerRetry = _menuRetry = _chartManagerRetry = _tooltipRetry = _documentsPanelRetry = 0f;
        }

        public static ToolManager Tools => Resolve(ref _toolManager, ref _toolManagerRetry);
        public static Menu Menu => Resolve(ref _menu, ref _menuRetry);
        public static ChartManager Charts => ChartManager.Instance != null ? ChartManager.Instance : Resolve(ref _chartManager, ref _chartManagerRetry);
        public static Tooltip Tooltip => Resolve(ref _tooltip, ref _tooltipRetry);
        public static DocumentsPanel Documents => Resolve(ref _documentsPanel, ref _documentsPanelRetry);

        public static string ChartLabel
        {
            get
            {
                ChartManager charts = Charts;
                return charts != null && charts.Active != null ? charts.Active.Title : "";
            }
        }

        public static bool ToolSelected
        {
            get
            {
                ToolManager tools = Tools;
                return tools != null && tools.SelectedTool != ToolType.None;
            }
        }

        private static T Resolve<T>(ref T cached, ref float retryAt) where T : Object
        {
            if (cached != null || Time.unscaledTime < retryAt) return cached;
            cached = Object.FindAnyObjectByType<T>();
            if (cached == null) retryAt = Time.unscaledTime + RetrySeconds;
            return cached;
        }
    }
}
