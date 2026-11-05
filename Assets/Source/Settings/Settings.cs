using System;
using UnityEngine;

public enum WristHand { Left, Right }

public enum TextSize { Small, Medium, Large }

public enum ColorMode { Standard, RedGreen, BlueYellow }

public enum AppLanguage { English, Spanish }

public enum ExplanationLevel { Simple, Standard, Expert }

public enum Setting { Animations, SoundEffects, Handedness, TextSize, ColorMode, Language, ExplanationLevel }

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

    public static bool Animations { get { Load(); return _animations; } set => Set(Setting.Animations, ref _animations, value); }
    public static bool SoundEffects { get { Load(); return _soundEffects; } set => Set(Setting.SoundEffects, ref _soundEffects, value); }
    public static WristHand Handedness { get { Load(); return _handedness; } set => Set(Setting.Handedness, ref _handedness, value); }
    public static TextSize TextSize { get { Load(); return _textSize; } set => Set(Setting.TextSize, ref _textSize, value); }
    public static ColorMode ColorMode { get { Load(); return _colorMode; } set => Set(Setting.ColorMode, ref _colorMode, value); }
    public static AppLanguage Language { get { Load(); return _language; } set => Set(Setting.Language, ref _language, value); }
    public static ExplanationLevel ExplanationLevel { get { Load(); return _explanationLevel; } set => Set(Setting.ExplanationLevel, ref _explanationLevel, value); }

    private const string Prefix = "settings.";

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
    }

    private static T Read<T>(Setting which, T fallback)
    {
        string key = Prefix + which;
        if (!PlayerPrefs.HasKey(key)) return fallback;
        int raw = PlayerPrefs.GetInt(key);
        if (typeof(T) == typeof(bool)) return (T)(object)(raw != 0);
        return Enum.IsDefined(typeof(T), raw) ? (T)Enum.ToObject(typeof(T), raw) : fallback;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Changed = null;
        _loaded = false;
        _animations = true;
        _soundEffects = true;
        _handedness = WristHand.Left;
        _textSize = TextSize.Large;
        _colorMode = ColorMode.Standard;
        _language = AppLanguage.English;
        _explanationLevel = ExplanationLevel.Standard;
    }
}
