using System.Collections.Generic;
using OneText;
using OneText.UGUI;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public static class Style
    {
        public const float DisabledAlpha = 0.35f;

        public static readonly Color Ink = Color.black;
        public static readonly Color Card = Color.white;
        public static readonly Color AiFill = Hex(0xeeeeee);
        public static readonly Color Muted = Faded(Ink, 0.6f);
        public static readonly Color Disabled = Faded(Ink, DisabledAlpha);

        public static Color Faded(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color Hex(int rgb) =>
            new Color32((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255), 255);

        public const float SmallText = 12f;
        public const float MediumText = 16f;
        public const float LargeText = 32f;

        public const float PanelTitle = MediumText;
        public const float ChartTitle = LargeText;

        public const float WorldTitle = 12f;

        public const float NormalWeight = 400f;
        public const float BoldWeight = 600f;

        public const float InkAscent = 0.714f;
        public const float InkDescent = 0.24f;

        public static float Ascent(float size) => size * InkAscent;

        public static float Descent(float size) => size * InkDescent;

        public static float InkHeight(float size) => size * (InkAscent + InkDescent);

        public static readonly TextDecoration Outline = new TextDecoration
        {
            Set = TextDecoration.Parts.Outline,
            OutlineColor = Ink,
            OutlineWidth = 0.25f
        };

        public static void ApplyText(OneTextLabel label, float size, bool bold)
        {
            if (label == null) return;
            SetBold(label, bold);
            label.FontSize = size;
        }

        public static void ApplyBody(OneTextLabel label) => ApplyText(label, MediumText, false);

        public static void ApplyTitle(OneTextLabel label) => ApplyText(label, PanelTitle, true);

        public static void SetBold(OneTextLabel label, bool bold) => SetWeight(label, bold ? BoldWeight : NormalWeight);

        public static void SetBold(OneTextMesh label, bool bold) => SetWeight(label, bold ? BoldWeight : NormalWeight);

        public static void SetWeight(OneTextLabel label, float weight)
        {
            if (Mathf.Approximately(weight, NormalWeight)) label.SetVariations();
            else label.SetVariations(Variations(weight));
        }

        public static void SetWeight(OneTextMesh label, float weight)
        {
            if (Mathf.Approximately(weight, NormalWeight)) label.SetVariations();
            else label.SetVariations(Variations(weight));
        }

        private static readonly Dictionary<float, FontVariation[]> Weights = new Dictionary<float, FontVariation[]>();

        private static FontVariation[] Variations(float weight)
        {
            if (!Weights.TryGetValue(weight, out FontVariation[] variations))
                Weights[weight] = variations = new[] { new FontVariation("wght", weight) };
            return variations;
        }

        public static void ApplyWorldTitle(OneTextMesh label)
        {
            if (label == null) return;
            SetBold(label, true);
            label.FontSize = WorldTitle;
        }

        public const float SmallPad = 10f;
        public const float MediumPad = 20f;
        public const float LargePad = 30f;

        public const float SmallRadius = 2f;
        public const float MediumRadius = 8f;
        public const float LargeRadius = 32f;

        public const float Stroke = 2f;

        public static float InnerRadius(float radius) => radius - Stroke;

        public const float ButtonSize = 50f;
        public const float IconSize = 30f;
        public const float TrackHeight = 12f;

        public static readonly Vector2 Panel = new Vector2(640f, 640f);
        public const float PanelScale = 0.65f;
        public const float PanelTilt = 16f;

        public const float NoticeWidth = 576f;
        public const float DataTooltipWidth = 360f;

        public static readonly Vector2 BannerSize = new Vector2(40f, 8f);

        public const int SortPanels = 0;
        public const int SortPinned = 1;
        public const int SortHandUI = 5;
        public const int SortNotices = 10;

        public const float WorldScale = 0.001f;
        public const float WorldTextScale = WorldScale * 10f;

        public const float Layer = 0.001f;
        public const float HoverRise = 0.003f;

        public const float HandOffset = 0.025f;
        public const float LineupGap = 0.03f;
        public const float AnchorOffset = 0.05f;

        public const float EngageScale = 1.03f;
    }
}
