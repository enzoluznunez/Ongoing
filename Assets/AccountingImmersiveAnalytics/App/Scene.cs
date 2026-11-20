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

        public static ToolManager Tools => Resolve(ref _toolManager);
        public static Menu Menu => Resolve(ref _menu);
        public static ChartManager Charts => ChartManager.Instance != null ? ChartManager.Instance : Resolve(ref _chartManager);
        public static Tooltip Tooltip => Resolve(ref _tooltip);
        public static DocumentsPanel Documents => Resolve(ref _documentsPanel);

        public static string ChartLabel
        {
            get
            {
                ChartManager charts = Charts;
                return charts != null && charts.Active != null ? charts.Active.Title : "";
            }
        }

        private static T Resolve<T>(ref T cached) where T : Object
        {
            if (cached == null) cached = Object.FindAnyObjectByType<T>();
            return cached;
        }
    }
}
