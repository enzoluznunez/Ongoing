using System;
using UnityEngine;

public enum WristHand { Left, Right }

public enum TextSize { Small, Medium, Large }

public enum ColorMode { Standard, RedGreen, BlueYellow }

public enum AppLanguage { English, Spanish }

public enum ExplanationLevel { Simple, Standard, Expert }

public enum Level3 { Low, Medium, High }

public enum PercentLevel { P25, P50, P75, P100 }

public enum ChartTheme { Dark, Light }

public enum PanelContrast { Standard, High }

public enum AssistantSpeed { Slow, Fast, Instant }

public enum AssistantVoice { Charon, Puck, Kore, Aoede, Fenrir }

public enum NumberFormat { Short, Full }

public enum NetworkLabels { Hubs, Everyone }

public enum Setting
{
    Animations, SoundEffects, Handedness, TextSize, ColorMode, Language, ExplanationLevel,
    Backdrop, BackdropGridlines, TickerLabels, AxisLabels, ChartKey, ChartDescription,
    ChartStyle, ChartOpacity, BackdropOpacity, ChartLabelSize, ChartTheme, DefaultChartSize, MotionSpeed, ChartHeight, ChartDistance, PanelContrast, SoundVolume, Captions, AssistantSpeed, AssistantVoice, AssistantVolume, WebSearch, NumberFormat, NetworkLabels
}

public static class Settings
{
    public static event Action<Setting> Changed;

    private static bool _loaded;
    private static bool _animations = true;
    private static bool _soundEffects = true;
    private static WristHand _handedness = WristHand.Left;
    private static TextSize _textSize = TextSize.Large;
    private static ColorMode _colorMode = ColorMode.Standard;
    private static AppLanguage _language = AppLanguage.English;
    private static ExplanationLevel _explanationLevel = ExplanationLevel.Standard;
    private static bool _backdrop = true;
    private static bool _backdropGridlines = true;
    private static bool _tickerLabels = true;
    private static bool _axisLabels = true;
    private static bool _chartKey = true;
    private static bool _chartDescription = true;
    private static MarkStyle _chartStyle = MarkStyle.Shaded;
    private static PercentLevel _chartOpacity = PercentLevel.P100;
    private static Level3 _backdropOpacity = Level3.Medium;
    private static Level3 _chartLabelSize = Level3.Medium;
    private static ChartTheme _chartTheme = ChartTheme.Dark;
    private static Level3 _defaultChartSize = Level3.Medium;
    private static Level3 _motionSpeed = Level3.Medium;
    private static Level3 _chartHeight = Level3.Medium;
    private static Level3 _chartDistance = Level3.Medium;
    private static PanelContrast _panelContrast = PanelContrast.Standard;
    private static PercentLevel _soundVolume = PercentLevel.P100;
    private static bool _captions = false;
    private static AssistantSpeed _assistantSpeed = AssistantSpeed.Slow;
    private static AssistantVoice _assistantVoice = AssistantVoice.Charon;
    private static PercentLevel _assistantVolume = PercentLevel.P100;
    private static bool _webSearch = true;
    private static NumberFormat _numberFormat = NumberFormat.Short;
    private static NetworkLabels _networkLabels = NetworkLabels.Hubs;
    private static string _startupChart = "";

    public static bool Animations { get { Load(); return _animations; } set => Set(Setting.Animations, ref _animations, value); }
    public static bool SoundEffects { get { Load(); return _soundEffects; } set => Set(Setting.SoundEffects, ref _soundEffects, value); }
    public static WristHand Handedness { get { Load(); return _handedness; } set => Set(Setting.Handedness, ref _handedness, value); }
    public static TextSize TextSize { get { Load(); return _textSize; } set => Set(Setting.TextSize, ref _textSize, value); }
    public static ColorMode ColorMode { get { Load(); return _colorMode; } set => Set(Setting.ColorMode, ref _colorMode, value); }
    public static AppLanguage Language { get { Load(); return _language; } set => Set(Setting.Language, ref _language, value); }
    public static ExplanationLevel ExplanationLevel { get { Load(); return _explanationLevel; } set => Set(Setting.ExplanationLevel, ref _explanationLevel, value); }
    public static bool Backdrop { get { Load(); return _backdrop; } set => Set(Setting.Backdrop, ref _backdrop, value); }
    public static bool BackdropGridlines { get { Load(); return _backdropGridlines; } set => Set(Setting.BackdropGridlines, ref _backdropGridlines, value); }
    public static bool TickerLabels { get { Load(); return _tickerLabels; } set => Set(Setting.TickerLabels, ref _tickerLabels, value); }
    public static bool AxisLabels { get { Load(); return _axisLabels; } set => Set(Setting.AxisLabels, ref _axisLabels, value); }
    public static bool ChartKey { get { Load(); return _chartKey; } set => Set(Setting.ChartKey, ref _chartKey, value); }
    public static bool ChartDescription { get { Load(); return _chartDescription; } set => Set(Setting.ChartDescription, ref _chartDescription, value); }

    public static MarkStyle ChartStyle { get { Load(); return _chartStyle; } set => Set(Setting.ChartStyle, ref _chartStyle, value); }
    public static PercentLevel ChartOpacity { get { Load(); return _chartOpacity; } set => Set(Setting.ChartOpacity, ref _chartOpacity, value); }
    public static Level3 BackdropOpacity { get { Load(); return _backdropOpacity; } set => Set(Setting.BackdropOpacity, ref _backdropOpacity, value); }
    public static Level3 ChartLabelSize { get { Load(); return _chartLabelSize; } set => Set(Setting.ChartLabelSize, ref _chartLabelSize, value); }
    public static ChartTheme ChartTheme { get { Load(); return _chartTheme; } set => Set(Setting.ChartTheme, ref _chartTheme, value); }
    public static Level3 DefaultChartSize { get { Load(); return _defaultChartSize; } set => Set(Setting.DefaultChartSize, ref _defaultChartSize, value); }
    public static Level3 MotionSpeed { get { Load(); return _motionSpeed; } set => Set(Setting.MotionSpeed, ref _motionSpeed, value); }
    public static Level3 ChartHeight { get { Load(); return _chartHeight; } set => Set(Setting.ChartHeight, ref _chartHeight, value); }
    public static Level3 ChartDistance { get { Load(); return _chartDistance; } set => Set(Setting.ChartDistance, ref _chartDistance, value); }
    public static PanelContrast PanelContrast { get { Load(); return _panelContrast; } set => Set(Setting.PanelContrast, ref _panelContrast, value); }
    public static PercentLevel SoundVolume { get { Load(); return _soundVolume; } set => Set(Setting.SoundVolume, ref _soundVolume, value); }
    public static bool Captions { get { Load(); return _captions; } set => Set(Setting.Captions, ref _captions, value); }
    public static AssistantSpeed AssistantSpeed { get { Load(); return _assistantSpeed; } set => Set(Setting.AssistantSpeed, ref _assistantSpeed, value); }
    public static AssistantVoice AssistantVoice { get { Load(); return _assistantVoice; } set => Set(Setting.AssistantVoice, ref _assistantVoice, value); }
    public static PercentLevel AssistantVolume { get { Load(); return _assistantVolume; } set => Set(Setting.AssistantVolume, ref _assistantVolume, value); }
    public static bool WebSearch { get { Load(); return _webSearch; } set => Set(Setting.WebSearch, ref _webSearch, value); }
    public static NumberFormat NumberFormat { get { Load(); return _numberFormat; } set => Set(Setting.NumberFormat, ref _numberFormat, value); }
    public static NetworkLabels NetworkLabels { get { Load(); return _networkLabels; } set => Set(Setting.NetworkLabels, ref _networkLabels, value); }
    public static string StartupChart
    {
        get { Load(); return _startupChart; }
        set
        {
            Load();
            _startupChart = value ?? "";
            PlayerPrefs.SetString(StartupKey, _startupChart);
            PlayerPrefs.Save();
        }
    }

    private const string Prefix = "settings.";
    private const string StartupKey = "settings.StartupChart";

    private static void Set<T>(Setting which, ref T field, T value)
    {
        Load();
        if (Equals(field, value)) return;
        field = value;
        PlayerPrefs.SetInt(Prefix + which, Convert.ToInt32(value));
        PlayerPrefs.Save();
        Changed?.Invoke(which);
    }

    private static void Load()
    {
        if (_loaded) return;
        _loaded = true;
        _animations = Read(Setting.Animations, _animations);
        _soundEffects = Read(Setting.SoundEffects, _soundEffects);
        _handedness = Read(Setting.Handedness, _handedness);
        _textSize = Read(Setting.TextSize, _textSize);
        _colorMode = Read(Setting.ColorMode, _colorMode);
        _language = Read(Setting.Language, _language);
        _explanationLevel = Read(Setting.ExplanationLevel, _explanationLevel);
        _backdrop = Read(Setting.Backdrop, _backdrop);
        _backdropGridlines = Read(Setting.BackdropGridlines, _backdropGridlines);
        _tickerLabels = Read(Setting.TickerLabels, _tickerLabels);
        _axisLabels = Read(Setting.AxisLabels, _axisLabels);
        _chartKey = Read(Setting.ChartKey, _chartKey);
        _chartDescription = Read(Setting.ChartDescription, _chartDescription);
        _chartStyle = Read(Setting.ChartStyle, _chartStyle);
        _chartOpacity = Read(Setting.ChartOpacity, _chartOpacity);
        _backdropOpacity = Read(Setting.BackdropOpacity, _backdropOpacity);
        _chartLabelSize = Read(Setting.ChartLabelSize, _chartLabelSize);
        _chartTheme = Read(Setting.ChartTheme, _chartTheme);
        _defaultChartSize = Read(Setting.DefaultChartSize, _defaultChartSize);
        _motionSpeed = Read(Setting.MotionSpeed, _motionSpeed);
        _chartHeight = Read(Setting.ChartHeight, _chartHeight);
        _chartDistance = Read(Setting.ChartDistance, _chartDistance);
        _panelContrast = Read(Setting.PanelContrast, _panelContrast);
        _soundVolume = Read(Setting.SoundVolume, _soundVolume);
        _captions = Read(Setting.Captions, _captions);
        _assistantSpeed = Read(Setting.AssistantSpeed, _assistantSpeed);
        _assistantVoice = Read(Setting.AssistantVoice, _assistantVoice);
        _assistantVolume = Read(Setting.AssistantVolume, _assistantVolume);
        _webSearch = Read(Setting.WebSearch, _webSearch);
        _numberFormat = Read(Setting.NumberFormat, _numberFormat);
        _networkLabels = Read(Setting.NetworkLabels, _networkLabels);
        _startupChart = PlayerPrefs.GetString(StartupKey, "");
    }

    private static T Read<T>(Setting which, T fallback)
    {
        string key = Prefix + which;
        if (!PlayerPrefs.HasKey(key)) return fallback;
        int raw = PlayerPrefs.GetInt(key);
        if (typeof(T) == typeof(bool)) return (T)(object)(raw != 0);
        return Enum.IsDefined(typeof(T), raw) ? (T)Enum.ToObject(typeof(T), raw) : fallback;
    }

    public static void ResetAll()
    {
        foreach (Setting setting in Enum.GetValues(typeof(Setting))) PlayerPrefs.DeleteKey(Prefix + setting);
        PlayerPrefs.DeleteKey(StartupKey);
        PlayerPrefs.Save();
        RestoreDefaults();
        _loaded = true;
        foreach (Setting setting in Enum.GetValues(typeof(Setting))) Changed?.Invoke(setting);
    }

    private static void RestoreDefaults()
    {
        _animations = true;
        _soundEffects = true;
        _handedness = WristHand.Left;
        _textSize = TextSize.Large;
        _colorMode = ColorMode.Standard;
        _language = AppLanguage.English;
        _explanationLevel = ExplanationLevel.Standard;
        _backdrop = true;
        _backdropGridlines = true;
        _tickerLabels = true;
        _axisLabels = true;
        _chartKey = true;
        _chartDescription = true;
        _chartStyle = MarkStyle.Shaded;
        _chartOpacity = PercentLevel.P100;
        _backdropOpacity = Level3.Medium;
        _chartLabelSize = Level3.Medium;
        _chartTheme = ChartTheme.Dark;
        _defaultChartSize = Level3.Medium;
        _motionSpeed = Level3.Medium;
        _chartHeight = Level3.Medium;
        _chartDistance = Level3.Medium;
        _panelContrast = PanelContrast.Standard;
        _soundVolume = PercentLevel.P100;
        _captions = false;
        _assistantSpeed = AssistantSpeed.Slow;
        _assistantVoice = AssistantVoice.Charon;
        _assistantVolume = PercentLevel.P100;
        _webSearch = true;
        _numberFormat = NumberFormat.Short;
        _networkLabels = NetworkLabels.Hubs;
        _startupChart = "";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Changed = null;
        _loaded = false;
        RestoreDefaults();
    }
}
