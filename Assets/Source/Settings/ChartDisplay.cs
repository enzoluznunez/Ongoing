public static class ChartDisplay
{
    public static float Opacity => PercentOf(Settings.ChartOpacity);

    public static float PercentOf(PercentLevel level)
    {
        switch (level)
        {
            case PercentLevel.P25: return 0.25f;
            case PercentLevel.P50: return 0.5f;
            case PercentLevel.P75: return 0.75f;
            default: return 1f;
        }
    }

    public static float BackdropStrength => Pick(Settings.BackdropOpacity, 0.5f, 1f, 1.6f);
    public static float LabelScale => Pick(Settings.ChartLabelSize, 0.75f, 1f, 1.35f);
    public static float DefaultScale => Pick(Settings.DefaultChartSize, 0.75f, 1f, 1.25f);
    public static float MotionMetresPerSecond => Pick(Settings.MotionSpeed, 0.25f, 0.5f, 1f);
    public static float HeightShare => Pick(Settings.ChartHeight, 0.6f, 0.75f, 0.9f);
    public static float ChartAhead => Pick(Settings.ChartDistance, 0.2f, 0.35f, 0.7f);
    public static float PanelAheadScale => Pick(Settings.ChartDistance, 0.8f, 1f, 1.35f);
    public static bool Light => Settings.ChartTheme == ChartTheme.Light;

    public static volatile bool FullNumbers;
    public static volatile bool LabelEveryone;

    public static void SyncWorkerFlags()
    {
        FullNumbers = Settings.NumberFormat == NumberFormat.Full;
        LabelEveryone = Settings.NetworkLabels == NetworkLabels.Everyone;
    }

    private static float Pick(Level3 level, float low, float medium, float high) =>
        level == Level3.Low ? low : level == Level3.High ? high : medium;
}
