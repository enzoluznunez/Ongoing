using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class Style
{
    private const string BodyFontPath = "Fonts/Inter-Medium SDF";
    private const string TitleFontPath = "Fonts/Inter-Bold SDF";

    private static TMP_FontAsset _bodyFont;
    private static TMP_FontAsset _titleFont;

    public static TMP_FontAsset BodyFont => Load(ref _bodyFont, BodyFontPath);

    public static TMP_FontAsset TitleFont => Load(ref _titleFont, TitleFontPath);

    public static void ApplyBody(TMP_Text label)
    {
        if (label == null) return;
        if (BodyFont != null) label.font = BodyFont;
        label.fontSize = Body;
        Track(_bodies, label);
    }

    public static void ApplyTitle(TMP_Text label)
    {
        if (label == null) return;
        if (TitleFont != null) label.font = TitleFont;
        label.fontSize = Title;
        Track(_titles, label);
    }

    public static void ApplyWorldBody(TMP_Text label)
    {
        if (label == null) return;
        if (BodyFont != null) label.font = BodyFont;
        label.fontSize = WorldBody;
    }

    public static void ApplyWorldTitle(TMP_Text label)
    {
        if (label == null) return;
        if (TitleFont != null) label.font = TitleFont;
        label.fontSize = WorldTitle;
    }

    private static readonly List<TMP_Text> _bodies = new List<TMP_Text>();
    private static readonly List<TMP_Text> _titles = new List<TMP_Text>();
    private static bool _listening;

    private static void Track(List<TMP_Text> list, TMP_Text label)
    {
        if (!_listening)
        {
            _listening = true;
            Settings.Changed += OnSettingChanged;
        }
        (list == _bodies ? _titles : _bodies).Remove(label);
        if (!list.Contains(label)) list.Add(label);
    }

    private static void OnSettingChanged(Setting which)
    {
        if (which != Setting.TextSize) return;
        Resize(_bodies, Body);
        Resize(_titles, Title);
    }

    private static void Resize(List<TMP_Text> list, float size)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null) { list.RemoveAt(i); continue; }
            list[i].fontSize = size;
        }
    }

    private static TMP_FontAsset Load(ref TMP_FontAsset cached, string path)
    {
        if (cached != null) return cached;

        cached = Resources.Load<TMP_FontAsset>(path);
        if (cached == null)
            Debug.LogError($"[Style] No font at Resources/{path}; text keeps whatever font TMP defaults to.");
        return cached;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _bodyFont = null;
        _titleFont = null;
        _bodies.Clear();
        _titles.Clear();
        _listening = false;
    }

    public static readonly Color White = Hex(255, 255, 255);
    public static readonly Color Black = Hex(000, 000, 000);

    public const float SurfaceTint = 0.05f;

    public const float EngageScale = 1.03f;
    public const float EngageSwell = EngageScale - 1f;
    public const float PreviewSwell = 0.10f;

    public const float SmallBorder = 2f;

    public static float Body => BodySize(Settings.TextSize);
    public static float Title => Body + 2f;

    public static float BodySize(TextSize size) =>
        size == TextSize.Small ? 8f : size == TextSize.Medium ? 12f : 16f;

    public const float LayoutBody = 16f;
    public const float LayoutTitle = 18f;

    public const float WorldBody = 10f;
    public const float WorldTitle = 12f;

    public const float WorldScale = 0.001f;
    public const float WorldTextScale = WorldScale * 10f;

    public static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    public static bool Animate => Settings.Animations;

    public static float Seconds(float seconds) => Animate ? seconds : 0f;

    public static IEnumerator Tween(float seconds, Action<float> apply)
    {
        if (!Animate || seconds <= 0f)
        {
            apply(1f);
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            yield return null;
            t += Time.deltaTime;
            apply(EaseOut(t / seconds));
        }
    }

    public static float Damp(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);

    public const float SmallPadding = 10f;
    public const float MediumPadding = 19f;
    public const float ButtonTextPad = 16f;
    public const float PanelInset = MediumPadding;

    public const float HeaderHeight = 48f;

    public const float OutRadius = 13f;
    public const float ChipRadius = 6f;
    public const float PanelRadius = 19f;
    public const float PanelInnerRadius = PanelRadius - SmallBorder;

    public static float RadiusFor(float height) => height <= Subbutton.y ? ChipRadius : OutRadius;

    public static float InnerRadius(float radius) => radius - SmallBorder;

    public const float TileRadiusRatio = 0.12f;

    public static float TileRadius(float side) =>
        Mathf.Round(Mathf.Clamp(side * TileRadiusRatio, OutRadius, PanelRadius + SmallPadding));

    public static readonly Vector2 Subbutton = new Vector2(64f, 32f);
    public static readonly Vector2 Button = new Vector2(80f, 40f);
    public const float TitleBarButtonWidth = 128f;

    public const int SortPanels = 0;
    public const int SortHandUI = 0;
    public const int SortNotices = 10;

    public static readonly Vector2 Subpanel = new Vector2(640f, 448f);
    public static readonly Vector2 Panel = new Vector2(640f, 640f);
    public const float TooltipWidth = 576f;

    public static Color Alpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    internal static Color Hex(byte r, byte g, byte b) => new Color32(r, g, b, 255);
}
