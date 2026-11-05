using UnityEngine;

public static class PosterPalette
{
    private static readonly string[] Hex =
    {
        "#2a78d6", "#eb6834", "#12a3b4", "#e34948", "#9b4dca",
        "#1baf7a", "#eda100", "#4a3aa7", "#008300", "#e87ba4",
    };

    public static readonly Color[] Colors = Build();

    public static readonly Color[] Ramp = BuildRamp();

    public static Color Band(float t)
    {
        int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(t) * Ramp.Length), 0, Ramp.Length - 1);
        return Ramp[index];
    }

    private static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    private static Color[] BuildRamp()
    {
        var ramp = (Color[])Colors.Clone();
        System.Array.Sort(ramp, (a, b) => Luminance(a).CompareTo(Luminance(b)));
        return ramp;
    }

    private static Color[] Build()
    {
        var colors = new Color[Hex.Length];
        for (int i = 0; i < Hex.Length; i++)
            colors[i] = ColorUtility.TryParseHtmlString(Hex[i], out Color c) ? c : Color.magenta;
        return colors;
    }
}
