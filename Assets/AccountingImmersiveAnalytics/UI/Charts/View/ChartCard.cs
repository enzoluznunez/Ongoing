using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Charts.Elements;
using AccountingImmersiveAnalytics.UI.Charts.Layout;
using AccountingImmersiveAnalytics.UI.Charts.Marks;
using AccountingImmersiveAnalytics.UI.Charts.Types;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Charts.View
{
    public sealed class ChartCard : MonoBehaviour
    {
        private const float HoverReach = 3f;

        private RectTransform _root;
        private MarkSurface _marks;
        private ChartText _text;
        private RectTransform _textRoot;
        private List<MarkInfo> _hits = new List<MarkInfo>();
        private List<KeySection> _key = new List<KeySection>();
        private int _highlighted = -1;
        private Color _highlightedColor;

        public ChartEntry Entry { get; private set; }
        public bool Pinned { get; private set; }
        public float Width { get; private set; } = ChartStyle.CardWidth;
        public float Height { get; private set; } = ChartStyle.CardHeight;
        public IReadOnlyList<MarkInfo> Hits => _hits;
        public IReadOnlyList<KeySection> Key => _key;
        public bool Shown => isActiveAndEnabled;

        public event Action<ChartCard> Resized;

        public static ChartCard Make(Transform parent, ChartEntry entry, bool pinned)
        {
            RectTransform root = DesignRect.Child(parent, $"Chart {entry.Id}");
            ChartCard card = LayoutTools.Ensure<ChartCard>(root.gameObject);
            card.Setup(root, entry, pinned);
            return card;
        }

        private void Setup(RectTransform root, ChartEntry entry, bool pinned)
        {
            _root = root;
            Entry = entry;
            Pinned = pinned;
            EnsureChannels();
            _marks = DesignRect.Surface(root, "Card", ChartStyle.CardWidth, ChartStyle.CardHeight);
            _textRoot = DesignRect.Place(DesignRect.Child(root, "Text"), 0f, 0f, ChartStyle.CardWidth, ChartStyle.CardHeight);
            _text = new ChartText(_textRoot);
            BackFace.Add(root, Style.LargeRadius);
            Texts.Refreshed += Redraw;
            Redraw();
        }

        private void OnDestroy() => Texts.Refreshed -= Redraw;

        private void EnsureChannels()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            canvas = canvas.rootCanvas;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 |
                                               AdditionalCanvasShaderChannels.TexCoord2 |
                                               AdditionalCanvasShaderChannels.TexCoord3;
        }

        public void Redraw()
        {
            if (Entry == null || _marks == null) return;
            ClearHighlight();
            float width = ChartStyle.CardWidth;
            ChartDrawing drawing = Draw(width);
            while (Pinned && drawing.Grow > 1.001f)
            {
                width = Mathf.Ceil(width * drawing.Grow);
                drawing = Draw(width);
            }
            _hits = drawing.Hits;
            _key = drawing.Key;
            Entry.LeftOut = new List<string>(drawing.LeftOut);

            Width = drawing.Width;
            Height = drawing.Height;
            DesignRect.Place((RectTransform)_marks.transform, 0f, 0f, Width, Height);
            DesignRect.Place(_textRoot, 0f, 0f, Width, Height);
            _root.sizeDelta = new Vector2(Width, Height);
            Resized?.Invoke(this);
        }

        private ChartDrawing Draw(float width)
        {
            _marks.Clear();
            _text.Begin();
            ChartText.Uncut = Pinned;
            var drawing = new ChartDrawing(Entry.Spec, Entry.Table, Entry.Shaping, _marks, _text, width);
            try
            {
                ChartType.For(Entry.Spec.Kind).Draw(drawing);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChartCard] '{Entry.Title}' could not be drawn: {e}");
                _marks.Clear();
                _text.Begin();
                drawing = new ChartDrawing(Entry.Spec, Entry.Table, Entry.Shaping, _marks, _text, width);
                drawing.Open(false);
                drawing.Notice = "This chart could not be drawn.";
                drawing.Finish();
            }
            finally
            {
                ChartText.Uncut = false;
            }
            _text.End();
            return drawing;
        }

        public bool TryDesignPoint(Vector3 world, out Vector2 design, out float depth)
        {
            ToDesign(world, out design, out depth);
            return design.x >= 0f && design.y >= 0f && design.x <= Width && design.y <= Height;
        }

        private void ToDesign(Vector3 world, out Vector2 design, out float depth)
        {
            Vector3 local = _root.InverseTransformPoint(world);
            design = new Vector2(local.x - _root.rect.xMin, _root.rect.yMax - local.y);
            depth = _root.TransformVector(new Vector3(0f, 0f, local.z)).magnitude;
        }

        public MarkInfo MarkAt(Vector2 design, float reach = HoverReach)
        {
            MarkInfo best = null;
            float closest = reach;
            for (int i = _hits.Count - 1; i >= 0; i--)
            {
                float d = _hits[i].Distance(design);
                if (d > closest || (best != null && d >= closest)) continue;
                best = _hits[i];
                closest = d;
                if (d <= 0f) break;
            }
            return best;
        }

        public bool Contains(Vector3 world, float depth, float rightReach)
        {
            ToDesign(world, out Vector2 at, out float away);
            return away <= depth && at.x >= 0f && at.y >= 0f && at.x <= Width + rightReach && at.y <= Height;
        }

        public Vector3 TopRight => _root.TransformPoint(new Vector3(_root.rect.xMin + Width, _root.rect.yMax, 0f));

        public MarkInfo Mark(int id) => id >= 0 && id < _hits.Count ? _hits[id] : null;

        public void Highlight(MarkInfo mark)
        {
            if (mark == null || mark.Shape == _highlighted) return;
            ClearHighlight();
            _highlighted = mark.Shape;
            _highlightedColor = _marks.ColorOf(mark.Shape);
            _marks.Recolor(mark.Shape, Style.Faded(Style.Ink, Mathf.Max(_highlightedColor.a, 1f)));
        }

        public void ClearHighlight()
        {
            if (_highlighted < 0 || _marks == null) return;
            _marks.Recolor(_highlighted, _highlightedColor);
            _highlighted = -1;
        }
    }
}
