using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using Oculus.Interaction;
using OneText;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public enum GrabMode { None, Move }

    public abstract class Panel : MonoBehaviour
    {
        public float zOffsetFromCamera = 0.45f;

        [SerializeField] private Collider _grabCollider;

        private Transform _grabPivot;

        protected Canvas _canvas;

        protected RectTransform CanvasRect => _canvas != null ? _canvas.transform as RectTransform : null;

        public bool IsVisible => _canvas != null && _open && gameObject.activeInHierarchy;
        public bool CanvasShown => _shown;
        private bool _open;
        private bool _shown;
        private PokeInteractable[] _pokes = new PokeInteractable[0];

        public abstract void ShowPanel();
        public abstract void HidePanel();

        public void TogglePanel()
        {
            if (_canvas == null) return;
            if (IsVisible) HidePanel();
            else ShowPanel();
        }

        private bool _layoutDirty = true;

        protected virtual void ResolveDeferredLayout() { }

        protected void ShowCanvas()
        {
            _open = true;
            ResolveLayoutNow();
            SetGrabbable(true);
            PanelLineup.Open(this, Reveal);
        }

        private void Reveal()
        {
            if (!_open || _shown) return;
            SetShown(true);
            OnRevealed();
        }

        protected virtual void OnRevealed() { }

        protected void HideCanvas()
        {
            bool wasOpen = _open;
            _open = false;
            SetGrabMode(GrabMode.None);
            SetShown(false);
            SetGrabbable(false);
            if (wasOpen) PanelLineup.Close(this);
        }

        public void LineupChanged() => OnLineupChanged();

        protected virtual void OnLineupChanged() { }

        protected virtual float SlotLeadUnits => 0f;

        protected virtual float PanelHeight => Style.Panel.y;

        protected virtual float SlotUnits => _pivotRect != null ? _pivotRect.rect.width : Style.Panel.x;

        public float SlotWidth
        {
            get
            {
                RectTransform canvas = CanvasRect;
                float scale = canvas != null ? canvas.lossyScale.x / Mathf.Max(1e-6f, _swell.On ? Style.EngageScale : 1f) : 1f;
                return SlotUnits * scale;
            }
        }

        public Pose SlotPose(Vector3 head, Vector3 direction)
        {
            Quaternion facing = Facing(CameraRig.Flatten(direction, Vector3.forward));
            Vector3 centre = head + facing * Vector3.forward * zOffsetFromCamera;
            RectTransform canvas = CanvasRect;
            float halfHeight = canvas != null ? PanelHeight * 0.5f * canvas.lossyScale.y : 0f;
            Vector3 ahead = centre + facing * Vector3.up * halfHeight;

            Quaternion inverse = Quaternion.Inverse(transform.rotation);
            Vector3 primary = inverse * (SlotPoint(0.5f) - transform.position);
            Vector3 slot = inverse * (SlotPoint(SlotUnits * 0.5f, true) - transform.position);
            return new Pose(ahead + facing * (primary - slot), facing);
        }

        private Vector3 SlotPoint(float along, bool units = false)
        {
            RectTransform rect = _pivotRect != null ? _pivotRect : CanvasRect;
            if (rect == null) return transform.position;
            Rect r = rect.rect;
            float x = units ? r.xMin - SlotLeadUnits + along : r.xMin + r.width * along;
            return rect.TransformPoint(new Vector3(x, r.center.y, 0f));
        }

        private void SetShown(bool on)
        {
            _shown = on;
            LayoutTools.ShowCanvas(_canvas, _pokes, on, _warming);
        }

        private bool _warming;
        private CanvasGroup _warmGroup;
        private float _warmAlpha;

        public void PrewarmCanvas(bool on)
        {
            if (_canvas == null || on == _warming) return;
            _warming = on;
            if (_warmGroup == null) _warmGroup = LayoutTools.Ensure<CanvasGroup>(_canvas.gameObject);
            if (on && !_shown)
            {
                _warmAlpha = _warmGroup.alpha;
                _warmGroup.alpha = 0f;
            }
            else if (!on && !_shown) _warmGroup.alpha = _warmAlpha;
            _canvas.enabled = _shown || _warming;
        }

        protected void ResolveLayoutNow()
        {
            if (!_layoutDirty) return;
            _layoutDirty = false;
            ResolveDeferredLayout();
        }

        protected void StartHidden()
        {
            if (_canvas == null) return;
            _pokes = _canvas.GetComponentsInChildren<PokeInteractable>(true);
            HideCanvas();
        }

        protected void InitPanel(Vector2 size)
        {
            _canvas = GetComponentInChildren<Canvas>(true);
            if (_canvas != null) _canvas.sortingOrder = Style.SortPanels;
            ApplyPanelSize(size);
            ApplyPanelSprites(transform);
            ApplyTitleBars(transform);
            ApplySplits(transform);
            EnsureGrabTransformers();
            SetGrabbable(false);
        }

        private OneGrabTranslateTransformer _slideGrab;
        private TwoGrabTurn _turnGrab;

        private void EnsureGrabTransformers()
        {
            if (GetComponent<Grabbable>() == null) return;
            _slideGrab = LayoutTools.Ensure<OneGrabTranslateTransformer>(gameObject);
            _turnGrab = LayoutTools.Ensure<TwoGrabTurn>(gameObject);
            _turnGrab.Pivot = EnsureGrabPivot();
            ApplyGrabTransformers();
        }

        private void ApplyGrabTransformers()
        {
            Grabbable grabbable = GetComponent<Grabbable>();
            if (grabbable == null || _slideGrab == null) return;

            ITransformer one = GrabMode == GrabMode.Move ? _slideGrab : null;
            ITransformer two = GrabMode == GrabMode.Move ? _turnGrab : null;
            grabbable.InjectOptionalOneGrabTransformer(one);
            grabbable.InjectOptionalTwoGrabTransformer(two);
            one?.Initialize(grabbable);
            two?.Initialize(grabbable);
        }

        private Transform EnsureGrabPivot()
        {
            if (_grabPivot != null) return _grabPivot;

            _grabPivot = FindTransform.EnsureChild(transform, "GrabPivot");
            return _grabPivot;
        }

        public const float SplitGap = Style.SmallPad;

        public static void ApplySplits(Transform root)
        {
            foreach (Transform bar in FindTransform.FindAllDeep(root, "TitleBar"))
                if (Split(bar, Style.Card) && bar.parent.TryGetComponent(out VerticalLayoutGroup stack))
                    stack.spacing = SplitGap;

            foreach (Transform header in FindTransform.FindAllDeep(root, "Header"))
                Split(header, Style.Card);
        }

        private static bool Split(Transform bar, Color top)
        {
            Transform panelRoot = AncestorNamed(bar, "PanelRoot");
            Transform border = panelRoot != null ? panelRoot.parent : null;
            if (border == null || border.name != "PanelBorder") return false;

            float height = TitleBarHeight;
            Image rootFill = panelRoot.GetComponent<Image>();
            if (rootFill == null) return false;
            LayoutTools.Ensure<Mask>(panelRoot.gameObject).showMaskGraphic = false;

            Image body = SplitPiece(panelRoot, "BodyFill", height + SplitGap, 0f, false);
            body.color = Style.Card;
            body.material = rootFill.material;
            body.transform.SetAsFirstSibling();

            Image head = SplitPiece(panelRoot, "TopFill", 0f, height, false);
            head.color = top;
            head.material = rootFill.material;
            head.transform.SetAsFirstSibling();

            SplitPiece(border, "PanelBack", 0f, height, true).transform.SetAsLastSibling();
            SplitPiece(border, "PanelBackBody", height + SplitGap, 0f, true).transform.SetAsLastSibling();
            return true;
        }

        private static Image SplitPiece(Transform parent, string name, float top, float height, bool back)
        {
            RectTransform rt = LayoutTools.EnsureChild(parent, name);
            LayoutTools.Ensure<LayoutElement>(rt.gameObject).ignoreLayout = true;
            rt.pivot = new Vector2(0.5f, 1f);
            if (height > 0f)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(0f, -top - height);
                rt.offsetMax = new Vector2(0f, -top);
            }
            else
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = new Vector2(0f, -top);
            }

            Image image = LayoutTools.Ensure<Image>(rt.gameObject);
            RoundedSprite.Apply(image, Style.LargeRadius, back);
            image.raycastTarget = false;
            if (back) image.color = Style.Ink;
            return image;
        }

        private static Transform AncestorNamed(Transform from, string name)
        {
            for (Transform t = from.parent; t != null; t = t.parent)
                if (t.name == name) return t;
            return null;
        }

        private bool _grabWanted;

        private void SetGrabbable(bool on)
        {
            _grabWanted = on;
            ApplyGrabCollider();
        }

        private void ApplyGrabCollider()
        {
            if (_grabCollider == null) return;
            bool on = _grabWanted && GrabArmed;
            if (_grabCollider.enabled != on) _grabCollider.enabled = on;
        }

        private RectTransform[] _grabRects;
        private RectTransform _pivotRect;
        private bool _grabBoundsDirty;

        protected void SetGrabRects(params RectTransform[] rects) => _grabRects = rects;

        protected void SetPivotRect(RectTransform rect) => _pivotRect = rect;

        private RectTransform[] _placementIcons = new RectTransform[0];

        public GrabMode GrabMode { get; private set; }

        public bool GrabArmed => GrabMode != GrabMode.None;

        protected void SetPlacementIcons(params RectTransform[] icons) => _placementIcons = icons;

        public bool IsPlacementIcon(Vector3 point)
        {
            for (int i = 0; i < _placementIcons.Length; i++)
            {
                RectTransform icon = _placementIcons[i];
                if (icon != null && icon.rect.Contains(icon.InverseTransformPoint(point))) return true;
            }
            return false;
        }

        protected void SetGrabMode(GrabMode mode)
        {
            if (GrabMode == mode) return;
            GrabMode = mode;
            ApplyGrabTransformers();
            InvalidateGrabBounds();
            ApplyGrabCollider();
            OnGrabModeChanged(mode);
        }

        protected virtual void OnGrabModeChanged(GrabMode mode) { }

        protected void InvalidateGrabBounds() => _grabBoundsDirty = true;

        private Grabbable _grabbable;
        private bool _grabbableSearched;
        private GrabSwell _swell;

        private void ApplyGrabLook()
        {
            if (!_grabbableSearched)
            {
                _grabbableSearched = true;
                _grabbable = GetComponent<Grabbable>();
            }
            if (_grabbable == null) return;

            bool held = _grabbable.SelectingPointsCount > 0;
            _swell.Set(transform, held);
        }

        private Vector2 _pokeFitted;

        private void FitPokes()
        {
            RectTransform canvas = CanvasRect;
            if (canvas == null || canvas.rect.size == _pokeFitted) return;
            _pokeFitted = canvas.rect.size;
            LayoutTools.FitPokeBounds(canvas);
        }

        protected virtual void LateUpdate()
        {
            FitPokes();
            PanelLineup.UndockIfMoved(this);
            ApplyGrabCollider();
            ApplyGrabLook();

            if (!_grabBoundsDirty || !IsVisible) return;

            if (_grabRects == null || !(_grabCollider is BoxCollider))
            {
                _grabBoundsDirty = false;
                return;
            }

            if (RefreshGrabBounds(_grabRects)) _grabBoundsDirty = false;
        }

        protected bool RefreshGrabBounds(params RectTransform[] rects)
        {
            BoxCollider box = _grabCollider as BoxCollider;
            if (box == null) return false;

            Transform space = box.transform;
            if (!TextMeasure.TryLocalBounds(CanvasRect, rects, space, out Vector3 center, out Vector3 size)) return false;

            center.z = box.center.z;
            size.z = box.size.z;
            box.center = center;
            box.size = size;

            if (_grabPivot != null) _grabPivot.position = PivotPoint(space, center);
            return true;
        }

        private Vector3 PivotPoint(Transform space, Vector3 grabCenter)
        {
            if (_pivotRect == null) return space.TransformPoint(grabCenter);
            Rect r = _pivotRect.rect;
            return _pivotRect.TransformPoint(r.center);
        }

        private void ApplyPanelSize(Vector2 size)
        {
            if (_canvas == null) return;
            RectTransform rt = _canvas.transform as RectTransform;
            if (rt != null) rt.sizeDelta = size;

            RectTransform primaryRt = FindTransform.FindDeep(transform, "PrimaryPanel") as RectTransform;
            if (primaryRt != null)
                primaryRt.sizeDelta = new Vector2(primaryRt.sizeDelta.x, size.y);

            ApplyPanelBorders(transform);
        }

        public static void ApplyPanelSprites(Transform root)
        {
            foreach (Transform border in FindTransform.FindAllDeep(root, "PanelBorder"))
            {
                Image image = border.GetComponent<Image>();
                RoundedSprite.Apply(image, Style.LargeRadius);
                if (image != null) image.color = Style.Faded(image.color, 0f);
                EnsurePanelBack(border);
            }
            foreach (Transform surface in FindTransform.FindAllDeep(root, "PanelRoot"))
            {
                RoundedSprite.Apply(surface.GetComponent<Image>(), Style.LargeRadius);
                TranslationSurface.Add(surface as RectTransform);
            }
        }

        private static void EnsurePanelBack(Transform border)
        {
            if (border == null) return;

            RectTransform rt = LayoutTools.EnsureChild(border, "PanelBack");

            LayoutTools.Stretch(rt);

            Image back = LayoutTools.Ensure<Image>(rt.gameObject);

            RoundedSprite.Apply(back, Style.LargeRadius, back: true);
            back.color = Style.Ink;
            back.raycastTarget = false;

            rt.SetAsLastSibling();
        }

        public static Material FrontMaterial => AssetLibrary.Current != null ? AssetLibrary.Current.PanelFront : null;

        public static Material BackMaterial => AssetLibrary.Current != null ? AssetLibrary.Current.PanelBack : null;

        public static void ApplyFront(Image image)
        {
            if (image == null) return;

            Material mat = FrontMaterial;
            if (mat != null) image.material = mat;
        }

        public static void ApplyPanelBorders(Transform root)
        {
            List<Transform> roots = FindTransform.FindAllDeep(root, "PanelRoot");
            for (int i = 0; i < roots.Count; i++)
            {
                if (!(roots[i] is RectTransform rt)) continue;
                LayoutTools.Stretch(rt);
            }
        }

        public static float TitleBarHeight => Style.SmallPad * 2f + Style.ButtonSize;

        public static void ApplyTitleBars(Transform root)
        {
            float height = TitleBarHeight;
            List<Transform> bars = FindTransform.FindAllDeep(root, "TitleBar");

            for (int i = 0; i < bars.Count; i++)
            {
                Transform bar = bars[i];

                if (bar.GetComponent<LayoutElement>() != null) LayoutTools.FixedHeight(bar.gameObject, height);

                Image barFill = bar.GetComponent<Image>();
                if (barFill != null) barFill.color = Style.Faded(Style.Card, 0f);

                HorizontalLayoutGroup hlg = bar.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    ApplyPaneLayout(hlg);
                    hlg.childForceExpandHeight = false;
                }

                bool hasSpacer = StretchSpacer(bar) != null;

                List<Transform> titles = FindTransform.FindAllDeep(bar, "Title");
                for (int t = 0; t < titles.Count; t++)
                {
                    if (!StyleHeaderLabel(titles[t].GetComponent<OneTextLabel>())) continue;

                    OneTextLabel titleLabel = titles[t].GetComponent<OneTextLabel>();
                    titleLabel.Overflow = TextOverflow.Ellipsis;
                    titleLabel.Wrap = TextWrap.NoWrap;
                    LayoutElement titleLE = LayoutTools.Ensure<LayoutElement>(titles[t].gameObject);
                    titleLE.minWidth = 0f;
                    titleLE.preferredWidth = -1f;
                    titleLE.flexibleWidth = hasSpacer ? 0f : 1f;
                }
            }

            List<Transform> headers = FindTransform.FindAllDeep(root, "Header");
            for (int i = 0; i < headers.Count; i++)
                StyleHeaderLabel(headers[i].GetComponent<OneTextLabel>());
        }

        protected static RectOffset PanePadding() => new RectOffset(
            (int)Style.LargePad, (int)Style.LargePad,
            (int)Style.SmallPad, (int)Style.SmallPad);

        private static void ApplyPaneLayout(HorizontalOrVerticalLayoutGroup layout)
        {
            layout.padding = PanePadding();
            layout.spacing = Style.SmallPad;
        }

        private static LayoutElement StretchSpacer(Transform bar)
        {
            Transform spacer = bar.Find("Spacer");
            if (spacer == null) return null;

            LayoutElement le = LayoutTools.Ensure<LayoutElement>(spacer.gameObject);
            le.flexibleWidth = 1f;
            return le;
        }

        private static bool StyleHeaderLabel(OneTextLabel label)
        {
            if (label == null) return false;

            Style.ApplyTitle(label);
            LabelFactory.Place(label, LabelAnchor.Start);
            label.color = Style.Ink;
            return true;
        }

        public static Quaternion Facing(Vector3 flatForward) =>
            Quaternion.LookRotation(flatForward) * Quaternion.Euler(Style.PanelTilt, 0f, 0f);

    }
}
