using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChartInfoCard
{
    private const float CardWidth = 340f;
    private const float SideGap = 0.2f;
    private const float Scale = 0.0006f;
    private const float TurnRate = 6f;

    private readonly Transform _owner;
    private readonly GameObject _root;
    private readonly Canvas _canvas;
    private readonly RectTransform _rect;
    private readonly GameObject _textPart;
    private readonly GameObject _keyPart;
    private readonly TextMeshProUGUI _title;
    private readonly TextMeshProUGUI _summary;
    private readonly RectTransform _keySlot;
    private bool _wantKey = true;
    private bool _wantText = true;
    private bool _shown = true;
    private Vector3 _lowSide;
    private Vector3 _highSide;

    public ChartInfoCard(Transform owner)
    {
        _owner = owner;
        _root = new GameObject("ChartInfoCard", typeof(RectTransform));
        _root.transform.SetParent(owner, false);
        _root.transform.localScale = Vector3.one * Scale;

        _canvas = _root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = Style.SortPanels;
        _rect = (RectTransform)_root.transform;
        _rect.sizeDelta = new Vector2(CardWidth, 100f);
        _rect.pivot = new Vector2(0.5f, 0.5f);

        ContentSizeFitter fitter = _root.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image border = _root.AddComponent<Image>();
        RoundedSprite.Apply(border, Style.PanelRadius);
        border.color = Style.Black;
        border.raycastTarget = false;

        VerticalLayoutGroup outer = _root.AddComponent<VerticalLayoutGroup>();
        int edge = (int)Style.SmallBorder;
        outer.padding = new RectOffset(edge, edge, edge, edge);
        outer.childControlWidth = true;
        outer.childControlHeight = true;
        outer.childForceExpandWidth = true;
        outer.childForceExpandHeight = false;

        RectTransform inner = UILayout.EnsureChild(_root.transform, "Inner");
        Image fill = UILayout.Ensure<Image>(inner.gameObject);
        RoundedSprite.Apply(fill, Style.PanelInnerRadius);
        fill.color = Style.White;
        fill.raycastTarget = false;
        VerticalLayoutGroup layout = inner.gameObject.AddComponent<VerticalLayoutGroup>();
        int pad = (int)Style.PanelInset;
        layout.padding = new RectOffset(pad, pad, pad, pad);
        layout.spacing = Style.SmallPadding;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _textPart = MakePart(inner, "Description");
        _title = UILabel.Make(_textPart.transform, "Title", Style.Black,
            TextAlignmentOptions.TopLeft, TextOverflowModes.Overflow, bold: true, wrap: true);
        _summary = UILabel.Make(_textPart.transform, "Summary", Style.Black,
            TextAlignmentOptions.TopLeft, TextOverflowModes.Overflow, wrap: true);

        _keyPart = MakePart(inner, "Key");
        _keySlot = (RectTransform)_keyPart.transform;
    }

    private static GameObject MakePart(Transform parent, string name)
    {
        var part = new GameObject(name, typeof(RectTransform));
        part.transform.SetParent(parent, false);
        VerticalLayoutGroup layout = part.AddComponent<VerticalLayoutGroup>();
        layout.spacing = Style.SmallPadding;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return part;
    }

    public void Rebuild(ChartSpec spec, ChartGeometry geometry)
    {
        _title.text = spec.Title;
        _summary.text = spec.Summary ?? spec.Description ?? "";
        Bounds b = geometry.Bounds;
        float half = CardWidth * Scale * 0.5f;
        float y = b.center.y;
        _lowSide = new Vector3(b.min.x - SideGap - half, y, 0f);
        _highSide = new Vector3(b.max.x + SideGap + half, y, 0f);
        _root.transform.localPosition = _highSide;

        var legend = (DiscreteLegend)ColorLegend.For(geometry);
        legend.Width = CardWidth - (Style.SmallBorder + Style.PanelInset) * 2f;
        legend.Build(_keySlot);
        Apply();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
    }

    public void SetParts(bool key, bool description)
    {
        _wantKey = key;
        _wantText = description;
        Apply();
    }

    public void SetShown(bool on)
    {
        _shown = on;
        Apply();
    }

    private void Apply()
    {
        _textPart.SetActive(_wantText);
        _keyPart.SetActive(_wantKey);
        _canvas.enabled = _shown && (_wantKey || _wantText);
        LayoutRebuilder.MarkLayoutForRebuild(_rect);
    }

    public void Follow(float deltaTime)
    {
        if (!_canvas.enabled) return;
        Transform cam = CameraRig.MainTransform;
        if (cam == null) return;

        Vector3 viewer = _owner.InverseTransformPoint(cam.position);
        bool viewerBehind = viewer.z > 0f;
        Vector3 side = viewerBehind ? _lowSide : _highSide;
        Transform t = _root.transform;
        t.localPosition = side;

        Vector3 away = t.position - cam.position;
        away.y = 0f;
        if (away.sqrMagnitude < 1e-5f) return;
        Quaternion want = Quaternion.LookRotation(away.normalized, Vector3.up);
        float k = deltaTime <= 0f || !Style.Animate ? 1f : Style.Damp(TurnRate, deltaTime);
        t.rotation = Quaternion.Slerp(t.rotation, want, k);
    }
}
