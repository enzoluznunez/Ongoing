using System;
using System.Collections;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Session;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    [DisallowMultipleComponent]
    public sealed class ListMotion : MonoBehaviour
    {
        private const float ReadingMetresPerSecond = 0.012f;

        private static readonly List<ListMotion> Live = new List<ListMotion>();

        public string Name { get; private set; }
        public string ItemWord { get; private set; }
        public bool Auto => _auto;

        private ScrollRect _scroll;
        private bool _horizontal, _reversed;
        private Func<IReadOnlyList<RectTransform>> _items;
        private Func<int, string> _describe;
        private Coroutine _glide;
        private bool _auto;

        public static ListMotion Attach(ScrollRect scroll, string name, string itemWord, bool horizontal, bool reversed,
            Func<IReadOnlyList<RectTransform>> items, Func<int, string> describe)
        {
            if (scroll == null) return null;
            ListMotion motion = LayoutTools.Ensure<ListMotion>(scroll.gameObject);
            motion._scroll = scroll;
            motion.Name = name;
            motion.ItemWord = itemWord;
            motion._horizontal = horizontal;
            motion._reversed = reversed;
            motion._items = items;
            motion._describe = describe;
            return motion;
        }

        private void OnEnable()
        {
            if (!Live.Contains(this)) Live.Add(this);
        }

        private void OnDisable()
        {
            Live.Remove(this);
            if (_auto) StopAuto($"the auto-scroll of the {Name} list stopped because the list left the screen");
            StopGlide();
        }

        public static List<ListMotion> Shown()
        {
            var shown = new List<ListMotion>();
            foreach (ListMotion motion in Live)
                if (motion != null && motion.isActiveAndEnabled && motion._scroll != null && motion.Count > 0) shown.Add(motion);
            return shown;
        }

        public static ListMotion Find(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string wanted = name.Trim();
            foreach (ListMotion motion in Shown())
                if (string.Equals(motion.Name, wanted, StringComparison.OrdinalIgnoreCase)) return motion;
            return null;
        }

        public static string Names()
        {
            var names = new List<string>();
            foreach (ListMotion motion in Shown()) names.Add($"'{motion.Name}' ({motion.Count} {motion.ItemWord}s)");
            return names.Count == 0 ? "none" : string.Join(", ", names);
        }

        public int Count => _items?.Invoke()?.Count ?? 0;

        public string Describe(int index) => _describe?.Invoke(index);

        public static void UserMoved(ScrollRect scroll)
        {
            if (scroll == null || !scroll.TryGetComponent(out ListMotion motion)) return;
            motion.StopGlide();
            if (motion._auto) motion.StopAuto($"the user took hold of the {motion.Name} list, so its auto-scroll stopped");
        }

        public void GoTo(int index)
        {
            if (_auto) StopAuto(null);
            Canvas.ForceUpdateCanvases();
            float target = Mathf.Clamp(OffsetOf(index), 0f, Most);
            StopGlide();
            float metres = Mathf.Abs(target - Offset) * UnitScale;
            float seconds = ChartManager.MotionSeconds(metres);
            if (!isActiveAndEnabled || seconds <= 0f)
            {
                Offset = target;
                return;
            }
            _glide = StartCoroutine(Glide(Offset, target, seconds));
        }

        public void StartAuto()
        {
            StopGlide();
            _auto = true;
        }

        public void StopAuto(string why)
        {
            _auto = false;
            if (!string.IsNullOrEmpty(why)) StateChannel.Record(why);
        }

        public int Current
        {
            get
            {
                int count = Count;
                if (count == 0) return -1;
                float now = Offset, most = Most;
                int best = 0;
                float nearest = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    float gap = Mathf.Abs(Mathf.Clamp(OffsetOf(i), 0f, most) - now);
                    if (gap < nearest - 0.5f) { nearest = gap; best = i; }
                }
                return best;
            }
        }

        private void Update()
        {
            if (!_auto || _scroll == null) return;
            float step = ReadingMetresPerSecond / Mathf.Max(1e-6f, UnitScale) * Time.unscaledDeltaTime;
            float next = Offset + (_reversed ? -step : step);
            bool end = _reversed ? next <= 0f : next >= Most;
            Offset = Mathf.Clamp(next, 0f, Most);
            if (end) StopAuto($"the auto-scroll of the {Name} list reached its end");
        }

        private IEnumerator Glide(float from, float to, float seconds)
        {
            yield return Style.Tween(seconds, k => Offset = Mathf.Lerp(from, to, k));
            _glide = null;
        }

        private void StopGlide()
        {
            if (_glide == null) return;
            StopCoroutine(_glide);
            _glide = null;
        }

        private RectTransform Content => _scroll.content;
        private RectTransform Viewport => _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;

        private float UnitScale => _horizontal ? Mathf.Abs(Content.lossyScale.x) : Mathf.Abs(Content.lossyScale.y);

        private float Most => _horizontal
            ? Mathf.Max(0f, Content.rect.width - Viewport.rect.width)
            : Mathf.Max(0f, Content.rect.height - Viewport.rect.height);

        private float Offset
        {
            get => _horizontal ? -Content.anchoredPosition.x : Content.anchoredPosition.y;
            set
            {
                _scroll.StopMovement();
                Vector2 at = Content.anchoredPosition;
                Content.anchoredPosition = _horizontal ? new Vector2(-value, at.y) : new Vector2(at.x, value);
            }
        }

        private float OffsetOf(int index)
        {
            IReadOnlyList<RectTransform> items = _items?.Invoke();
            if (items == null || items.Count == 0) return 0f;
            RectTransform item = items[Mathf.Clamp(index, 0, items.Count - 1)];
            if (item == null) return 0f;
            Rect r = item.rect;
            if (!_horizontal)
            {
                Vector3 top = Content.InverseTransformPoint(item.TransformPoint(new Vector3(r.xMin, r.yMax, 0f)));
                return Content.rect.yMax - top.y;
            }
            Vector3 left = Content.InverseTransformPoint(item.TransformPoint(new Vector3(r.xMin, r.yMin, 0f)));
            Vector3 right = Content.InverseTransformPoint(item.TransformPoint(new Vector3(r.xMax, r.yMin, 0f)));
            return _reversed
                ? right.x - Content.rect.xMin - Viewport.rect.width
                : left.x - Content.rect.xMin;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Live.Clear();
    }
}
