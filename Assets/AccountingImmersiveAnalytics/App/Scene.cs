using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    public static class Scene
    {

        private static ToolManager toolManager;
        private static Menu menu;
        private static ChartManager chartManager;
        private static Tooltip tooltip;
        private static DocumentsPanel documentsPanel;

        public static ToolManager Tools => Resolve(ref toolManager);
        public static Menu Assistant => Resolve(ref menu);
        public static ChartManager Charts => ChartManager.Instance != null ? ChartManager.Instance : Resolve(ref chartManager);
        public static Tooltip Tooltip => Resolve(ref tooltip);
        public static DocumentsPanel Documents => Resolve(ref documentsPanel);

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
