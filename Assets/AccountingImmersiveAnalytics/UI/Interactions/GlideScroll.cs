using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public sealed class GlideScroll : ScrollRect
    {
        private const float SampleWindow = 0.1f;
        private const float FastestGlide = 4000f;

        private readonly Queue<(float Time, Vector2 Position)> _samples = new Queue<(float, Vector2)>();

        public static GlideScroll Attach(RectTransform host, RectTransform viewport, RectTransform content)
        {
            GlideScroll scroll = LayoutTools.Ensure<GlideScroll>(host.gameObject);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = MovementType.Clamped;
            LayoutTools.Ensure<ScrollMomentum>(host.gameObject);
            return scroll;
        }

        public static GlideScroll Column(RectTransform root, float spacing)
        {
            RectTransform viewport = LayoutTools.EnsureChild(root, "Viewport");
            LayoutTools.Stretch(viewport);
            LayoutTools.Ensure<RectMask2D>(viewport.gameObject);
            LayoutTools.Ensure<Image>(viewport.gameObject).color = Style.Faded(Style.Card, 0f);

            RectTransform content = LayoutTools.EnsureChild(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = LayoutTools.Ensure<VerticalLayoutGroup>(content.gameObject);
            layout.spacing = spacing;
            LayoutTools.Stack(layout);
            LayoutTools.Ensure<ContentSizeFitter>(content.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return Attach(root, viewport, content);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            ListMotion.UserMoved(this);
            _samples.Clear();
            base.OnBeginDrag(eventData);
            Sample();
        }

        public override void OnScroll(PointerEventData data)
        {
            ListMotion.UserMoved(this);
            base.OnScroll(data);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            Sample();
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            Sample();
            base.OnEndDrag(eventData);
            if (inertia) velocity = Vector2.ClampMagnitude(Release(), FastestGlide);
            _samples.Clear();
        }

        private void Sample()
        {
            if (content == null) return;
            float now = Time.unscaledTime;
            _samples.Enqueue((now, content.anchoredPosition));
            while (_samples.Count > 2 && now - _samples.Peek().Time > SampleWindow) _samples.Dequeue();
        }

        private Vector2 Release()
        {
            if (_samples.Count < 2) return Vector2.zero;
            (float Time, Vector2 Position) first = _samples.Peek();
            (float Time, Vector2 Position) last = first;
            foreach (var sample in _samples) last = sample;
            float span = last.Time - first.Time;
            if (span <= 0f) return Vector2.zero;
            Vector2 speed = (last.Position - first.Position) / span;
            if (!horizontal) speed.x = 0f;
            if (!vertical) speed.y = 0f;
            return speed;
        }
    }
}
