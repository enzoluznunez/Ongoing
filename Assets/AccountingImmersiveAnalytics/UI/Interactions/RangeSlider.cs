using System;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public sealed class RangeSlider : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler,
        IInitializePotentialDragHandler
    {
        public const float TrackHeight = Style.TrackHeight;
        public const float HandleSize = Style.IconSize;

        private RectTransform _area;
        private RectTransform _fill;
        private RectTransform _lowHandle;
        private RectTransform _highHandle;
        private Image _lowInner;
        private Image _highInner;
        private double _min;
        private double _max;
        private double _low;
        private double _high;
        private bool _whole;
        private int _held = -1;

        public static bool AnyHeld { get; private set; }

        public Action<double, double> Changed;
        public Action<double, double> Released;

        public static RangeSlider Make(RectTransform area, double min, double max, double low, double high, bool whole)
        {
            RangeSlider slider = LayoutTools.Ensure<RangeSlider>(area.gameObject);
            slider.Build(area, min, max, low, high, whole);
            return slider;
        }

        private void Build(RectTransform area, double min, double max, double low, double high, bool whole)
        {
            _area = area;
            _min = min;
            _max = max > min ? max : min + 1d;
            _low = Math.Max(_min, Math.Min(low, _max));
            _high = Math.Max(_low, Math.Min(high, _max));
            _whole = whole;

            Image hit = LayoutTools.Ensure<Image>(area.gameObject);
            hit.color = Style.Faded(Style.Card, 0f);
            float slack = PokeRule.EdgeSlack * PokeRule.Millimetres;
            hit.raycastPadding = new Vector4(-slack, -slack, -slack, -slack);
            Panel.ApplyFront(hit);

            RectTransform track = Bar(area, "Track", HandleSize * 0.5f);
            Ring(track, TrackHeight * 0.5f);

            _fill = LayoutTools.EnsureChild(track, "Fill");
            Image fill = LayoutTools.Ensure<Image>(_fill.gameObject);
            RoundedSprite.Apply(fill, TrackHeight * 0.5f);
            fill.color = Style.Ink;
            fill.raycastTarget = false;
            _fill.anchorMin = new Vector2(0f, 0f);
            _fill.anchorMax = new Vector2(0f, 1f);

            _lowHandle = Handle(area, "Low", out _lowInner);
            _highHandle = Handle(area, "High", out _highInner);
            Refresh();
        }

        private static RectTransform Bar(RectTransform parent, string name, float inset)
        {
            RectTransform bar = LayoutTools.EnsureChild(parent, name);
            bar.anchorMin = new Vector2(0f, 0.5f);
            bar.anchorMax = new Vector2(1f, 0.5f);
            bar.offsetMin = new Vector2(inset, -TrackHeight * 0.5f);
            bar.offsetMax = new Vector2(-inset, TrackHeight * 0.5f);
            return bar;
        }

        private static Image Ring(RectTransform rect, float radius)
        {
            Image outer = LayoutTools.Ensure<Image>(rect.gameObject);
            RoundedSprite.Apply(outer, radius);
            outer.color = Style.Ink;
            outer.raycastTarget = false;
            RectTransform innerRect = LayoutTools.EnsureChild(rect, "Inner");
            LayoutTools.Stretch(innerRect, Style.Stroke);
            Image inner = LayoutTools.Ensure<Image>(innerRect.gameObject);
            RoundedSprite.Apply(inner, Style.InnerRadius(radius));
            inner.color = Style.Card;
            inner.raycastTarget = false;
            return inner;
        }

        private static RectTransform Handle(RectTransform parent, string name, out Image inner)
        {
            RectTransform handle = LayoutTools.EnsureChild(parent, name);
            handle.anchorMin = handle.anchorMax = new Vector2(0f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = Vector2.one * HandleSize;
            Ring(handle, HandleSize * 0.5f);
            inner = handle.Find("Inner").GetComponent<Image>();
            return handle;
        }

        private float Span => Mathf.Max(1f, _area.rect.width - HandleSize);

        private float XOf(double value) => HandleSize * 0.5f + (float)((value - _min) / (_max - _min)) * Span;

        private double ValueAt(float x)
        {
            double t = Mathf.Clamp01((x - HandleSize * 0.5f) / Span);
            double v = _min + t * (_max - _min);
            return _whole ? Math.Round(v) : v;
        }

        private void Refresh()
        {
            float low = XOf(_low), high = XOf(_high);
            _lowHandle.anchoredPosition = new Vector2(low, 0f);
            _highHandle.anchoredPosition = new Vector2(high, 0f);
            _fill.offsetMin = new Vector2(low - HandleSize * 0.5f, 0f);
            _fill.offsetMax = new Vector2(high - HandleSize * 0.5f, 0f);
            _lowInner.color = _held == 0 ? Style.Ink : Style.Card;
            _highInner.color = _held == 1 ? Style.Ink : Style.Card;
        }

        private bool TryLocal(PointerEventData data, out float x)
        {
            x = 0f;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, data.position, data.pressEventCamera, out Vector2 local))
                return false;
            x = local.x - _area.rect.xMin;
            return true;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!TryLocal(eventData, out float x)) return;
            float low = XOf(_low), high = XOf(_high);
            _held = Mathf.Abs(x - low) < Mathf.Abs(x - high) || (Mathf.Approximately(low, high) && x < low) ? 0 : 1;
            AnyHeld = true;
            Move(x);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_held >= 0 && TryLocal(eventData, out float x)) Move(x);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_held < 0) return;
            _held = -1;
            AnyHeld = false;
            Refresh();
            Released?.Invoke(_low, _high);
        }

        private void OnDisable()
        {
            if (_held >= 0) AnyHeld = false;
            _held = -1;
        }

        private void Move(float x)
        {
            double v = ValueAt(x);
            if (_held == 0) _low = Math.Min(v, _high);
            else _high = Math.Max(v, _low);
            Refresh();
            Changed?.Invoke(_low, _high);
        }
    }
}
