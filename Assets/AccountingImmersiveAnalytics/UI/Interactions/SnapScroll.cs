using System.Collections;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public sealed class SnapScroll : ScrollRect
    {
        private const float SnapSeconds = Animate.Medium;
        private const float EndSlack = 0.5f;

        public readonly List<float> Stops = new List<float>();
        public GameObject FadeLeft;
        public GameObject FadeRight;

        private ScrollRect _outer;
        private bool _routed;
        private Coroutine _snap;

        protected override void Awake()
        {
            base.Awake();
            horizontal = true;
            vertical = false;
            inertia = false;
            movementType = MovementType.Clamped;
            scrollSensitivity = 4f;
        }

        protected override void Start()
        {
            base.Start();
            _outer = transform.parent != null ? transform.parent.GetComponentInParent<ScrollRect>() : null;
        }

        private float Most => content != null && viewport != null ? Mathf.Max(0f, content.rect.width - viewport.rect.width) : 0f;

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (_outer != null) _outer.OnInitializePotentialDrag(eventData);
            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            StopSnap();
            ListMotion.UserMoved(this);
            _routed = _outer != null && Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x);
            if (_routed) _outer.OnBeginDrag(eventData);
            else base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (_routed) _outer.OnDrag(eventData);
            else base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (_routed)
            {
                _outer.OnEndDrag(eventData);
                _routed = false;
                return;
            }
            base.OnEndDrag(eventData);
            Snap();
        }

        public override void OnScroll(PointerEventData data)
        {
            ListMotion.UserMoved(this);
            base.OnScroll(data);
            Snap();
        }

        private void Snap()
        {
            if (content == null || Stops.Count == 0) return;
            float offset = -content.anchoredPosition.x;
            float best = Stops[0];
            foreach (float stop in Stops)
                if (Mathf.Abs(stop - offset) < Mathf.Abs(best - offset)) best = stop;
            SnapTo(best);
        }

        private void SnapTo(float target)
        {
            StopSnap();
            target = Mathf.Clamp(target, 0f, Most);
            float from = -content.anchoredPosition.x;
            if (!isActiveAndEnabled)
            {
                Set(target);
                return;
            }
            _snap = StartCoroutine(Glide(from, target));
        }

        private IEnumerator Glide(float from, float to)
        {
            yield return Animate.Tween(SnapSeconds, k => Set(Mathf.Lerp(from, to, k)));
            _snap = null;
        }

        private void StopSnap()
        {
            if (_snap == null) return;
            StopCoroutine(_snap);
            _snap = null;
        }

        private void Set(float offset)
        {
            StopMovement();
            content.anchoredPosition = new Vector2(-offset, content.anchoredPosition.y);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (content == null) return;
            float offset = -content.anchoredPosition.x;
            if (FadeLeft != null && FadeLeft.activeSelf != offset > EndSlack) FadeLeft.SetActive(offset > EndSlack);
            bool more = offset < Most - EndSlack;
            if (FadeRight != null && FadeRight.activeSelf != more) FadeRight.SetActive(more);
        }
    }
}
