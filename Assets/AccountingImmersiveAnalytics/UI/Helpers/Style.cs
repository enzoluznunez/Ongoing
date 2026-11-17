using System;
using System.Collections;
using OneText;
using OneText.UGUI;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public static class Style
    {
        public const float SmallText = 12f;
        public const float MediumText = 16f;
        public const float LargeText = 32f;

        public const float NormalWeight = 400f;
        public const float BoldWeight = 600f;

        public const float InkAscent = 0.714f;
        public const float InkDescent = 0.24f;

        public const float SmallPad = 10f;
        public const float MediumPad = 20f;
        public const float LargePad = 30f;

        public const float SmallRadius = 2f;
        public const float MediumRadius = 8f;
        public const float LargeRadius = 32f;

        public const float Stroke = 2f;

        public const float ButtonSize = 50f;
        public const float IconSize = 30f;

        public static readonly Color Ink = Color.black;
        public static readonly Color Card = Color.white;
        public static readonly Color AiFill = Hex(0xeeeeee);
        public static readonly Color Muted = Faded(Ink, 0.6f);
        public static readonly Color Disabled = Faded(Ink, DisabledAlpha);

        public static float Ascent(float size) => size * InkAscent;

        public static float Descent(float size) => size * InkDescent;

        public static float InkHeight(float size) => size * (InkAscent + InkDescent);

        public static Color Faded(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color Hex(int rgb) =>
            new Color32((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255), 255);

        private static readonly FontVariation[] BoldVariations = { new FontVariation("wght", BoldWeight) };

        public static void ApplyText(OneTextLabel label, float size, bool bold)
        {
            if (label == null) return;
            SetBold(label, bold);
            label.FontSize = size;
        }

        public static void ApplyBody(OneTextLabel label) => ApplyText(label, MediumText, false);

        public static void ApplyTitle(OneTextLabel label) => ApplyText(label, MediumText, true);

        public static void SetBold(OneTextLabel label, bool bold)
        {
            if (bold) label.SetVariations(BoldVariations);
            else label.SetVariations();
        }

        public static readonly TextDecoration Outline = new TextDecoration
        {
            Set = TextDecoration.Parts.Outline,
            OutlineColor = new Color32(0, 0, 0, 255),
            OutlineWidth = 0.25f
        };

        public static void ApplyWorldTitle(OneTextMesh label)
        {
            if (label == null) return;
            label.SetVariations(BoldVariations);
            label.FontSize = WorldTitle;
        }

        public static OneTextMesh MakeWorldBanner(Transform parent, string name, Vector2 size)
        {
            RectTransform holder = LayoutTools.EnsureChild(parent, name);
            holder.sizeDelta = size;
            OneTextMesh label = LayoutTools.Ensure<OneTextMesh>(holder.gameObject);
            ApplyWorldTitle(label);
            label.Alignment = OneText.TextAlignment.Center;
            label.Wrap = TextWrap.NoWrap;
            label.Color = Card;
            label.transform.localScale = Vector3.one * (WorldTextScale * 2f);
            return label;
        }

        public const float DisabledAlpha = 0.35f;

        public const float EngageScale = 1.03f;

        public const float HoverRise = 0.003f;

        public const float WorldTitle = 12f;

        public const float WorldScale = 0.001f;
        public const float WorldTextScale = WorldScale * 10f;

        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        public static IEnumerator Tween(float seconds, Action<float> apply)
        {
            if (seconds <= 0f)
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

        public static float InnerRadius(float radius) => radius - Stroke;

        public const int SortPanels = 0;
        public const int SortHandUI = 0;
        public const int SortNotices = 10;

        public static readonly Vector2 Panel = new Vector2(640f, 640f);
        public const float PanelScale = 0.65f;
        public const float PanelTilt = 16f;
        public const float TooltipWidth = 576f;

        public const int MostDots = 3;
        public const float DotSeconds = 0.4f;

        public static int Dots => (int)(Time.unscaledTime / DotSeconds) % (MostDots + 1);

        public static string Counting(string word) => word + new string('.', Dots);
    }
}
