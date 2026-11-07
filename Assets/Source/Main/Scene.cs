using UnityEngine;

public static class Scene {

    private static ManageTools toolManager;
    private static ToolPanelUI toolPanelUI;
    private static Watch watch;
    private static ManageCharts chartManager;
    private static Tooltip tooltip;
    private static SettingsPanelUI settingsPanel;

    public static ManageTools Tools => Resolve(ref toolManager);
    public static ToolPanelUI ToolPanel => Resolve(ref toolPanelUI);
    public static Watch Assistant => Resolve(ref watch);
    public static ManageCharts Charts => ManageCharts.Instance != null ? ManageCharts.Instance : Resolve(ref chartManager);
    public static Tooltip Tooltip => Resolve(ref tooltip);
    public static SettingsPanelUI SettingsPanel => Resolve(ref settingsPanel);

    public static string ChartLabel {
        get {
            ManageCharts charts = Charts;
            return charts != null && charts.Active != null ? charts.Active.Spec.Title : "";
        }
    }

    private static T Resolve<T>(ref T cached) where T : Object {
        if (cached == null) cached = Object.FindAnyObjectByType<T>();
        return cached;
    }
}
