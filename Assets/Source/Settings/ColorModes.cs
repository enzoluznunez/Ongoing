using UnityEngine;

public static class ColorModes
{
    private static readonly Color32[] Standard = Parse(
        "#4e9de0", "#f28e2b", "#4fc4c4", "#edc948", "#ff9da7",
        "#59c26b", "#b07aa1", "#e15759", "#9c755f", "#bab0ac");

    private static readonly Color32[] RedGreen = Parse(
        "#8000ff", "#d9d921", "#8c5115", "#4cd9d9", "#008c7e",
        "#988cff", "#62b272", "#b27d00", "#b23e61", "#794d8c");

    private static readonly Color32[] BlueYellow = Parse(
        "#8000ff", "#00e5ff", "#ffcc00", "#b22a1b", "#1c8c00",
        "#ff26e9", "#988cff", "#8c7e00", "#bbd977", "#0057d9");

    public static Color32[] Palette32()
    {
        switch (Settings.ColorMode)
        {
            case ColorMode.RedGreen: return RedGreen;
            case ColorMode.BlueYellow: return BlueYellow;
            default: return Standard;
        }
    }

    public static Color ColorAt(int slot)
    {
        Color32[] palette = Palette32();
        return palette[slot % palette.Length];
    }

    private static Color32[] Parse(params string[] hex)
    {
        var colors = new Color32[hex.Length];
        for (int i = 0; i < hex.Length; i++)
            colors[i] = ColorUtility.TryParseHtmlString(hex[i], out Color c) ? c : Color.magenta;
        return colors;
    }
}
