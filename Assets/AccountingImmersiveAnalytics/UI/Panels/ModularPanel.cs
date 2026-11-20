using System;
using System.Collections;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public abstract class ModularPanel : Panel
    {
        private static readonly string[] UnusedParts = { "DropdownPanel", "PickerPanel" };
        private static readonly string[] UnusedButtons = { "Close_Btn", "Pages_Btn" };

        private RectTransform _primary;
        private Coroutine _settle;
        private Transform _titleBar;
        private OneTextLabel _title;
        private ButtonFactory.Handle _moveButton;
        private GameObject _grabSurface;
        private Vector3 _canvasHome;
        private RectTransform _rightCard;
        private float _rightCardWidth;
        private RectTransform _leftCard;
        private float _leftCardWidth;

        protected RectTransform Body { get; private set; }

        protected void SetTitle(Func<string> compose) => Texts.Bind(_title, compose);

        protected virtual float PanelWidth => Style.Panel.x;

        public RectTransform PrimaryRect => _primary;

        protected void BuildPanel(string titleKey, string contentName)
        {
            transform.localScale = Vector3.one * Style.PanelScale;
            InitPanel(new Vector2(PanelWidth, PanelHeight));
            PinCanvasToTopLeft();

            _primary = FindTransform.FindDeep(transform, "PrimaryPanel") as RectTransform;
            PinTopLeft(_primary, Vector2.zero);
            if (_primary != null) _primary.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            foreach (string part in UnusedParts)
            {
                Transform unused = FindTransform.FindDeep(transform, part);
                if (unused != null) unused.gameObject.SetActive(false);
            }

            _titleBar = FindTransform.FindDeep(_primary, "TitleBar");
            Transform title = _titleBar != null ? _titleBar.Find("Title") : null;
            if (title == null) title = FindTransform.FindDeep(_primary, "Title");
            _title = title != null ? title.GetComponent<OneTextLabel>() : null;
            SetTitle(() => Strings.Get(titleKey));

            Transform content = FindTransform.FindDeep(transform, contentName) ?? ContentOf(_primary);
            if (content == null)
            {
                Debug.LogError($"[{GetType().Name}] No content area '{contentName}' under the panel; it cannot be built.");
                return;
            }
            foreach (LayoutGroup group in content.GetComponents<LayoutGroup>()) group.enabled = false;
            LayoutElement contentElement = LayoutTools.Ensure<LayoutElement>(content.gameObject);
            contentElement.minHeight = 0f;
            contentElement.preferredHeight = 0f;
            contentElement.flexibleHeight = 1f;
            Body = (RectTransform)content;

            foreach (string unused in UnusedButtons)
            {
                Transform old = _titleBar != null ? _titleBar.Find(unused) : null;
                if (old != null) old.gameObject.SetActive(false);
            }
            _moveButton = ButtonFactory.Icon(_titleBar, "Move_Btn", "Place", () => ToggleGrab(GrabMode.Move));
            _moveButton.Root.transform.SetAsLastSibling();
            BuildGrabSurface();
            SetGrabRects(_primary);
            SetPivotRect(_primary);
            ApplySplits(transform);
            ApplyTitleBars(transform);
            Texts.Refreshed += OnLanguageChanged;
            MirrorTitleBar();
        }

        protected virtual void OnDestroy() => Texts.Refreshed -= OnLanguageChanged;

        private static Transform ContentOf(Transform primary)
        {
            Transform root = primary != null ? FindTransform.FindDeep(primary, "PanelRoot") : null;
            if (root == null) return null;
            foreach (Transform child in root)
                if (child.name != "TitleBar") return child;
            return null;
        }

        protected virtual void OnDisable() => _settle = null;

        private void OnLanguageChanged()
        {
            MirrorTitleBar();
            RefreshPanel();
            QueueSettle();
        }

        protected abstract void RefreshPanel();

        private void MirrorTitleBar()
        {
            if (_titleBar == null) return;
            HorizontalLayoutGroup row = _titleBar.GetComponent<HorizontalLayoutGroup>();
            if (row == null || row.reverseArrangement == Language.RightToLeft) return;
            row.reverseArrangement = Language.RightToLeft;
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)_titleBar);
        }

        private void BuildGrabSurface()
        {
            RectTransform canvas = CanvasRect;
            if (canvas == null) return;

            RectTransform rt = LayoutTools.EnsureChild(canvas, "GrabSurface");
            LayoutTools.Stretch(rt);
            rt.SetAsLastSibling();
            Image surface = LayoutTools.Ensure<Image>(rt.gameObject);
            surface.color = Style.Faded(Style.Card, 0f);
            Panel.ApplyFront(surface);
            GrabSurface filter = LayoutTools.Ensure<GrabSurface>(rt.gameObject);
            filter.Exempt = new[] { (RectTransform)_moveButton.Root.transform };
            _grabSurface = rt.gameObject;
            _grabSurface.SetActive(false);
            SetPlacementIcons(filter.Exempt);
        }

        public bool PlaceOn => GrabMode == GrabMode.Move;

        public void SetPlace(bool on)
        {
            if (PlaceOn == on) return;
            ToggleGrab(GrabMode.Move);
        }

        private void ToggleGrab(GrabMode mode)
        {
            SetGrabMode(GrabMode == mode ? GrabMode.None : mode);
            RefreshPanel();
        }

        protected override void OnGrabModeChanged(GrabMode mode)
        {
            if (_grabSurface != null) _grabSurface.SetActive(mode != GrabMode.None);
            RefreshPlacementIcons();
        }

        protected void RefreshPlacementIcons() => ButtonFactory.SetSelected(_moveButton, GrabMode == GrabMode.Move);

        protected override void OnLineupChanged() => QueueSettle();

        protected override void OnRevealed() => QueueSettle();

        private float RightCardTrail =>
            _rightCard != null && _rightCard.gameObject.activeSelf ? Style.SmallPad + _rightCardWidth : 0f;

        private float LeftCardTrail =>
            _leftCard != null && _leftCard.gameObject.activeSelf ? _leftCardWidth + Style.SmallPad : 0f;

        public RectTransform ShowRightCard(string name, float width) => ShowSideCard(ref _rightCard, ref _rightCardWidth, name, width);

        public void HideRightCard() => HideSideCard(_rightCard);

        public RectTransform ShowLeftCard(string name, float width) => ShowSideCard(ref _leftCard, ref _leftCardWidth, name, width);

        private RectTransform ShowSideCard(ref RectTransform slot, ref float slotWidth, string name, float width)
        {
            RectTransform canvas = CanvasRect;
            if (canvas == null) return null;
            RectTransform card = LayoutTools.EnsureChild(canvas, name);
            card.gameObject.layer = canvas.gameObject.layer;
            if (_grabSurface != null) card.SetSiblingIndex(_grabSurface.transform.GetSiblingIndex());
            bool changed = slot != card || !card.gameObject.activeSelf || !Mathf.Approximately(slotWidth, width);
            slot = card;
            slotWidth = width;
            card.gameObject.SetActive(true);
            if (!changed) return card;
            Relayout();
            return card;
        }

        private void HideSideCard(RectTransform card)
        {
            if (card == null || !card.gameObject.activeSelf) return;
            card.gameObject.SetActive(false);
            Relayout();
        }

        private void Relayout()
        {
            LayoutCards();
            QueueSettle();
            PanelLineup.Resize(this);
        }

        protected override float SlotLeadUnits => LeftCardTrail;

        protected override float SlotUnits => LeftCardTrail + PanelWidth + RightCardTrail;

        protected override void ResolveDeferredLayout() => LayoutCards();

        private void PinCanvasToTopLeft()
        {
            RectTransform canvas = CanvasRect;
            if (canvas == null) return;

            var corners = new Vector3[4];
            canvas.GetWorldCorners(corners);
            Vector3 topLeft = corners[1];
            canvas.pivot = new Vector2(0f, 1f);
            canvas.position = topLeft;
            _canvasHome = canvas.localPosition;
        }

        private static void PinTopLeft(RectTransform rect, Vector2 at)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = at;
        }

        protected void QueueSettle()
        {
            LayoutCards();
            if (!isActiveAndEnabled) return;
            if (_settle != null) StopCoroutine(_settle);
            _settle = StartCoroutine(SettleLayout());
        }

        private IEnumerator SettleLayout()
        {
            yield return LayoutTools.Converge(() => IsVisible, LayoutCards, () => CanvasRect != null ? CanvasRect.sizeDelta.x : -1f);
            _settle = null;
        }

        private void LayoutCards()
        {
            RectTransform canvas = CanvasRect;
            if (canvas == null) return;

            float lead = LeftCardTrail;
            float width = lead + PanelWidth;
            PinTopLeft(_primary, new Vector2(lead, 0f));
            if (_primary != null) _primary.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            if (lead > 0f)
            {
                PinTopLeft(_leftCard, Vector2.zero);
                _leftCard.sizeDelta = new Vector2(_leftCardWidth, PanelHeight);
            }

            if (RightCardTrail > 0f)
            {
                PinTopLeft(_rightCard, new Vector2(width + Style.SmallPad, 0f));
                _rightCard.sizeDelta = new Vector2(_rightCardWidth, PanelHeight);
                width += RightCardTrail;
            }

            canvas.sizeDelta = new Vector2(width, PanelHeight);
            canvas.localPosition = _canvasHome - canvas.localRotation * new Vector3(lead * canvas.localScale.x, 0f, 0f);
            InvalidateGrabBounds();
        }
    }
}
