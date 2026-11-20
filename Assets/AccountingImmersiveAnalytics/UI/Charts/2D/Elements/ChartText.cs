using System.Collections.Generic;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Helpers;
using OneText;
using OneText.UGUI;
using UnityEngine;
using TextAlignment = OneText.TextAlignment;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Elements
{
    public enum Align { Start, Middle, End }

    public sealed class ChartText
    {
        private const float WideRoom = 4000f;
        private const string Ellipsis = "…";

        private readonly RectTransform _root;
        private readonly List<OneTextLabel> _labels = new List<OneTextLabel>();
        private int _used;

        private static OneTextLabel _ruler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _ruler = null;

        public ChartText(RectTransform root) => _root = root;


        public static bool Uncut { get; set; }

        public static string Shown(string text) => string.IsNullOrEmpty(text) ? "" : Numbers.Text(text);

        public static float Width(string text, float size, float weight = Style.NormalWeight)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            OneTextLabel ruler = Ruler;
            ruler.FontSize = size;
            Style.SetWeight(ruler, weight);
            ruler.Text = text;
            return ruler.preferredWidth;
        }

        public static string Cap(string text) => CapAt(text, Uncut ? ChartStyle.PinnedChars : ChartStyle.TickChars);

        public static string Heading(string text, float size, float room, float weight = Style.BoldWeight) =>
            Uncut ? CapAt(text, ChartStyle.PinnedChars) : Clip(CapAt(text, ChartStyle.PinnedChars), size, room, weight);

        public static string CapAt(string text, int most)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= most) return text;
            Boundaries.Clear();
            OneText.Unicode.TextSegmenter.GraphemeBoundaries(text, Boundaries);
            if (Boundaries.Count - 1 <= most) return text;
            return Whole(text, Boundaries[System.Math.Max(1, most - 3)]).TrimEnd() + Ellipsis;
        }

        public static string Fit(string text, float size, float room, float weight = Style.NormalWeight) =>
            Uncut ? Cap(text) : Cut(text, size, room, weight);

        public static string Cut(string text, float size, float room, float weight = Style.NormalWeight) =>
            Clip(Uncut ? text : Cap(text), size, room, weight);

        public static string Clip(string text, float size, float room, float weight = Style.NormalWeight)
        {
            if (string.IsNullOrEmpty(text) || Width(text, size, weight) <= room) return text ?? "";
            string cut = text.TrimEnd('…');
            Boundaries.Clear();
            OneText.Unicode.TextSegmenter.GraphemeBoundaries(cut, Boundaries);
            int last = Boundaries.Count - 1;
            while (last > 1 && Boundaries[last] > 0 && Width(Whole(cut, Boundaries[last]) + Ellipsis, size, weight) > room) last--;
            cut = last >= 1 ? Whole(cut, Boundaries[last]) : cut;
            return cut.TrimEnd() + Ellipsis;
        }

        private static readonly List<int> Boundaries = new List<int>();

        private static string Whole(string text, int end)
        {
            while (end > 0)
            {
                char c = text[end - 1];
                bool leading = (c >= '\u0E40' && c <= '\u0E44') || (c >= '\u0EC0' && c <= '\u0EC4');
                if (!leading && c != '\u200D' && c != '\u200C') break;
                end--;
            }
            return text.Substring(0, end);
        }

        public void Begin() => _used = 0;

        public void End()
        {
            for (int i = _used; i < _labels.Count; i++) _labels[i].gameObject.SetActive(false);
        }

        public OneTextLabel Put(string text, float size, float weight, float x, float baseline, Align align,
            bool rising = false, Color? color = null)
        {
            if (string.IsNullOrEmpty(text)) return null;
            OneTextLabel label = Next();
            label.FontSize = size;
            Style.SetWeight(label, weight);
            label.Language = Language.Current;
            label.Text = text;
            label.color = color ?? Style.Ink;
            label.Alignment = align == Align.Start ? TextAlignment.Left : align == Align.End ? TextAlignment.Right : TextAlignment.Center;

            RectTransform rt = label.rectTransform;
            float pivotX = align == Align.Start ? 0f : align == Align.End ? 1f : 0.5f;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(pivotX, 1f);
            rt.sizeDelta = new Vector2(WideRoom, size * 3f);
            float drop = BaselineDrop(label, size);
            if (rising)
            {
                rt.localRotation = Quaternion.Euler(0f, 0f, 90f);
                rt.anchoredPosition = new Vector2(x - drop, -baseline);
            }
            else
            {
                rt.localRotation = Quaternion.identity;
                rt.anchoredPosition = new Vector2(x, -(baseline - drop));
            }
            return label;
        }

        private static float BaselineDrop(OneTextLabel label, float size)
        {
            TextLayoutResult layout = label.EnsureLayout();
            if (layout != null && layout.Lines.Count > 0) return layout.Lines[0].Baseline;
            return Style.Ascent(size);
        }

        private OneTextLabel Next()
        {
            while (_labels.Count <= _used)
            {
                var go = new GameObject($"Text_{_labels.Count}", typeof(RectTransform));
                go.layer = _root.gameObject.layer;
                go.transform.SetParent(_root, false);
                OneTextLabel label = go.AddComponent<OneTextLabel>();
                Prepare(label);
                _labels.Add(label);
            }
            OneTextLabel next = _labels[_used++];
            next.gameObject.SetActive(true);
            return next;
        }

        private static void Prepare(OneTextLabel label)
        {
            label.RichText = false;
            label.raycastTarget = false;
            label.Wrap = TextWrap.NoWrap;
            label.Overflow = TextOverflow.Overflow;
            label.VerticalAlignment = VerticalAlignment.Top;
            label.Decoration = default;
        }

        private static OneTextLabel Ruler
        {
            get
            {
                if (_ruler != null) return _ruler;
                var go = new GameObject("Chart Text Ruler", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
                _ruler = go.AddComponent<OneTextLabel>();
                Prepare(_ruler);
                _ruler.FontSize = Style.MediumText;
                return _ruler;
            }
        }
    }
}
