using System;
using System.Collections.Generic;

public class SettingsPanelUI : ModularPanel
{
    private static readonly string[] Levels = { "option.Low", "option.Medium", "option.High" };
    private static readonly string[] Sizes = { "option.Small", "option.Medium", "option.Large" };
    private static readonly string[] Percents = { "option.P25", "option.P50", "option.P75", "option.P100" };

    private readonly Dictionary<PanelRow, string> _keyOf = new Dictionary<PanelRow, string>();
    private string _focus;

    private void Awake()
    {
        BuildPanel("panel.settings", "SettingsGrid", hasRight: false);

        Rows.AddHeading("section.Interface");
        Toggle(Setting.Animations, () => Settings.Animations, on => Settings.Animations = on);
        Toggle(Setting.SoundEffects, () => Settings.SoundEffects, on => Settings.SoundEffects = on);
        Choice(Setting.SoundVolume, Percents, () => (int)Settings.SoundVolume, v => Settings.SoundVolume = (PercentLevel)v);
        Choice(Setting.Handedness, new[] { "option.Left", "option.Right" },
            () => (int)Settings.Handedness, v => Settings.Handedness = (WristHand)v);
        Choice(Setting.TextSize, Sizes, () => (int)Settings.TextSize, v => Settings.TextSize = (TextSize)v);
        Choice(Setting.PanelContrast, new[] { "option.Standard", "option.High" },
            () => (int)Settings.PanelContrast, v => Settings.PanelContrast = (PanelContrast)v);
        Choice(Setting.Language, new[] { "option.English", "option.Spanish" },
            () => (int)Settings.Language, v => Settings.Language = (AppLanguage)v);

        Rows.AddHeading("section.ChartParts");
        Toggle(Setting.Backdrop, () => Settings.Backdrop, on => Settings.Backdrop = on);
        Toggle(Setting.BackdropGridlines, () => Settings.BackdropGridlines, on => Settings.BackdropGridlines = on);
        Toggle(Setting.TickerLabels, () => Settings.TickerLabels, on => Settings.TickerLabels = on);
        Toggle(Setting.AxisLabels, () => Settings.AxisLabels, on => Settings.AxisLabels = on);
        Toggle(Setting.ChartKey, () => Settings.ChartKey, on => Settings.ChartKey = on);
        Toggle(Setting.ChartDescription, () => Settings.ChartDescription, on => Settings.ChartDescription = on);

        Rows.AddHeading("section.ChartLook");
        Choice(Setting.ChartStyle, new[] { "option.Solid", "option.Shaded" },
            () => (int)Settings.ChartStyle, v => Settings.ChartStyle = (MarkStyle)v);
        Choice(Setting.ChartOpacity, Percents, () => (int)Settings.ChartOpacity, v => Settings.ChartOpacity = (PercentLevel)v);
        Choice(Setting.BackdropOpacity, Levels, () => (int)Settings.BackdropOpacity, v => Settings.BackdropOpacity = (Level3)v);
        Choice(Setting.ChartLabelSize, Sizes, () => (int)Settings.ChartLabelSize, v => Settings.ChartLabelSize = (Level3)v);
        Choice(Setting.ChartTheme, new[] { "option.Dark", "option.Light" },
            () => (int)Settings.ChartTheme, v => Settings.ChartTheme = (ChartTheme)v);
        Choice(Setting.ColorMode, new[] { "option.Standard", "option.RedGreen", "option.BlueYellow" },
            () => (int)Settings.ColorMode, v => Settings.ColorMode = (ColorMode)v);

        Rows.AddHeading("section.Comfort");
        Choice(Setting.DefaultChartSize, new[] { "option.P75", "option.P100", "option.P125" },
            () => (int)Settings.DefaultChartSize, v => Settings.DefaultChartSize = (Level3)v);
        Choice(Setting.MotionSpeed, new[] { "option.Slow", "option.Normal", "option.Fast" },
            () => (int)Settings.MotionSpeed, v => Settings.MotionSpeed = (Level3)v);
        Choice(Setting.ChartHeight, Levels, () => (int)Settings.ChartHeight, v => Settings.ChartHeight = (Level3)v);
        Choice(Setting.ChartDistance, new[] { "option.Near", "option.Normal", "option.Far" },
            () => (int)Settings.ChartDistance, v => Settings.ChartDistance = (Level3)v);
        Toggle(Setting.Captions, () => Settings.Captions, on => Settings.Captions = on);

        Rows.AddHeading("section.Assistant");
        Choice(Setting.ExplanationLevel, new[] { "option.Simple", "option.Standard", "option.Expert" },
            () => (int)Settings.ExplanationLevel, v => Settings.ExplanationLevel = (ExplanationLevel)v);
        Choice(Setting.AssistantSpeed, new[] { "speed.half", "speed.one", "speed.instant" },
            () => (int)Settings.AssistantSpeed, v => Settings.AssistantSpeed = (AssistantSpeed)v);
        AddVoiceRow();
        Choice(Setting.AssistantVolume, Percents, () => (int)Settings.AssistantVolume, v => Settings.AssistantVolume = (PercentLevel)v);
        Toggle(Setting.WebSearch, () => Settings.WebSearch, on => Settings.WebSearch = on);

        Rows.AddHeading("section.Data");
        Choice(Setting.NumberFormat, new[] { "option.Short", "option.Full" },
            () => (int)Settings.NumberFormat, v => Settings.NumberFormat = (NumberFormat)v);
        Choice(Setting.NetworkLabels, new[] { "option.Hubs", "option.Everyone" },
            () => (int)Settings.NetworkLabels, v => Settings.NetworkLabels = (NetworkLabels)v);
        AddStartupRow();

        Rows.AddHeading("section.App");
        Track(Rows.Add(PanelRow.Action("setting.Recenter", Recenter.Request)), "setting.Recenter");
        Track(Rows.Add(PanelRow.Action("setting.Reset", Settings.ResetAll)), "setting.Reset");

        Settings.Changed += OnSettingChanged;
        RefreshPanel();

        if (_canvas != null) StartHidden();
    }

    private void Track(PanelRow row, string key) => _keyOf[row] = key;

    private void Toggle(Setting setting, Func<bool> get, Action<bool> set) =>
        Track(Rows.Add(PanelRow.Toggle("setting." + setting, get, set)), "setting." + setting);

    private void Choice(Setting setting, string[] options, Func<int> get, Action<int> set) =>
        Track(Rows.Add(PanelRow.Choice("setting." + setting, options, get, set)), "setting." + setting);

    private void AddVoiceRow()
    {
        int count = Enum.GetValues(typeof(AssistantVoice)).Length;
        Track(Rows.Add(new PanelRow
        {
            NameKey = "setting.AssistantVoice",
            Value = () => Settings.AssistantVoice.ToString(),
            Step = direction => Settings.AssistantVoice = (AssistantVoice)(((int)Settings.AssistantVoice + direction + count) % count)
        }), "setting.AssistantVoice");
    }

    private void AddStartupRow()
    {
        Track(Rows.Add(new PanelRow
        {
            NameKey = "setting.StartupChart",
            Value = StartupValue,
            Step = StepStartup
        }), "setting.StartupChart");
    }

    private static string StartupValue()
    {
        ManageCharts charts = Scene.Charts;
        ManageCharts.Entry entry = charts != null && !string.IsNullOrEmpty(Settings.StartupChart)
            ? charts.Find(Settings.StartupChart)
            : null;
        return entry != null ? entry.Spec.Title : Strings.Get("option.CatalogDefault");
    }

    private void StepStartup(int direction)
    {
        ManageCharts charts = Scene.Charts;
        if (charts == null || charts.Entries.Count == 0) return;

        int count = charts.Entries.Count + 1;
        int current = 0;
        for (int i = 0; i < charts.Entries.Count; i++)
            if (charts.Entries[i].Spec.Id == Settings.StartupChart) current = i + 1;

        int next = (current + direction + count) % count;
        Settings.StartupChart = next == 0 ? "" : charts.Entries[next - 1].Spec.Id;
    }

    private void Start()
    {
        ResolveLayoutNow();
        if (ManageCharts.Instance != null) ManageCharts.Instance.OnChartChanged += RefreshPanel;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Settings.Changed -= OnSettingChanged;
        if (ManageCharts.Instance != null) ManageCharts.Instance.OnChartChanged -= RefreshPanel;
    }

    private void OnSettingChanged(Setting which) => RefreshPanel();

    protected override void OnRowPressed(PanelRow row)
    {
        if (_keyOf.TryGetValue(row, out string key)) _focus = key;
        RefreshPanel();
    }

    protected override void RefreshPanel()
    {
        Rows.Refresh();
        bool focused = _focus != null;
        SetBottomOpen(focused);
        if (focused)
        {
            Strings.Bind(BottomHeader, _focus);
            Strings.Bind(BottomText, _focus + ".desc");
            UILayout.Clear(BottomExtra);
            if (_focus == "setting." + Setting.ColorMode)
                ColorLegend.For(Scene.Charts != null && Scene.Charts.Active != null ? Scene.Charts.Active.Geometry : null)
                    .Build(BottomExtra);
        }
        QueueSettle();
    }

    public override void ShowPanel()
    {
        if (_canvas == null) return;
        ShowCanvas();
        RefreshPanel();
        StateChannel.RecordState("settingsPanel", "the settings panel is open");
    }

    public override void HidePanel()
    {
        if (_canvas == null) return;
        _focus = null;
        SetBottomOpen(false);
        QueueSettle();
        HideCanvas();
        StateChannel.RecordState("settingsPanel", "the settings panel is closed");
    }
}
