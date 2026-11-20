using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts
{
    public static class ChartStyle
    {
        public const float CardWidth = 721f;
        public const float CardHeight = 481f;
        public const float TitleRow = 40f;
        public const float LabelRoom = 20f;
        public const float FewestPlotShare = 0.6f;
        public const float TrendWidth = 4f;
        public static readonly Color Accent = Style.Hex(0xc51b8a);

        public static float HeightFor(float width) => width <= CardWidth ? CardHeight : width * 2f / 3f;

        public const int Ticks = 5;
        public const int TickChars = 10;
        public const int PinnedChars = 50;
        public const int MostShown = 10;

        public const float BarShare = 0.6f;
        public const float DotRadius = 5f;
        public const float DotAlpha = 0.75f;
        public const float LargestBubble = 16f;
        public const float DonutHole = 0.6f;
        public const float DonutInset = 2f;
        public const float AntiAlias = 2f;

        public const float RampHeight = Style.TrackHeight;

        public const float PreviewScale = 0.5f;
        public const float PinnedScale = 1f;
        public const float PinnedLift = 0.1f;

        public static readonly Color SizeGrey = Style.Hex(0x8a9096);

        public static readonly Color[] Palette =
        {
            Style.Hex(0x4e9de0), Style.Hex(0xf28e2b), Style.Hex(0x4fc4c4), Style.Hex(0xedc948), Style.Hex(0xff9da7),
            Style.Hex(0x59c26b), Style.Hex(0xb07aa1), Style.Hex(0xe15759), Style.Hex(0x9c755f), Style.Hex(0xbab0ac)
        };

        public static readonly Color[] Ramp =
        {
            Style.Hex(0xe6f0fa), Style.Hex(0xcfe4f7), Style.Hex(0x8fc0eb), Style.Hex(0x4e9de0), Style.Hex(0x1f5f9c), Style.Hex(0x0b2f52)
        };

        public static Color Group(int index) => Palette[((index % Palette.Length) + Palette.Length) % Palette.Length];

        public static Color RampAt(float t)
        {
            float s = Mathf.Clamp01(t) * (Ramp.Length - 1);
            int i = Mathf.Min(Ramp.Length - 2, Mathf.FloorToInt(s));
            return Color.Lerp(Ramp[i], Ramp[i + 1], s - i);
        }
    }
}
