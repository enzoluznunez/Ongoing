using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PanelRow
{
    private const float DisabledAlpha = 0.35f;
    private const float HighContrastDisabledAlpha = 0.65f;
    private const float ValueWidth = 170f;

    public string NameKey;
    public Func<string> Value;
    public Action<int> Step;
    public Func<bool> Enabled;

    private TextMeshProUGUI _value;
    private TextMeshProUGUI _name;
    private CanvasGroup _group;
    private UIButton.Handle _less;
    private UIButton.Handle _more;

    public static PanelRow Toggle(string nameKey, Func<bool> get, Action<bool> set) => new PanelRow
    {
        NameKey = nameKey,
        Value = () => Strings.Get(get() ? "option.on" : "option.off"),
        Step = _ => set(!get())
    };

    public static PanelRow Choice(string nameKey, string[] optionKeys, Func<int> get, Action<int> set) => new PanelRow
    {
        NameKey = nameKey,
        Value = () => Strings.Get(optionKeys[get()]),
        Step = direction => set((get() + direction + optionKeys.Length) % optionKeys.Length)
    };

    public static PanelRow Action(string nameKey, Action run) => new PanelRow
    {
        NameKey = nameKey,
        Value = () => Strings.Get("option.run"),
        Step = _ => run()
    };

    public void Build(Transform parent, Action<PanelRow> pressed)
    {
        var root = new GameObject($"Row_{NameKey}", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        _group = root.AddComponent<CanvasGroup>();

        HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = Style.SmallPadding;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        LayoutElement rowElement = root.AddComponent<LayoutElement>();
        rowElement.minHeight = Style.Button.y;

        _name = UILabel.Make(root.transform, "Name", Style.Black,
            TextAlignmentOptions.Left, TextOverflowModes.Overflow, wrap: true);
        Strings.Bind(_name, NameKey);
        UILayout.Ensure<LayoutElement>(_name.gameObject).flexibleWidth = 1f;

        _less = MakeArrow(root.transform, "Less", "<");
        _less.Button.onClick.AddListener(() => Press(-1, pressed));

        _value = UILabel.Make(root.transform, "Value", Style.Black,
            TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
        LayoutElement valueElement = UILayout.Ensure<LayoutElement>(_value.gameObject);
        valueElement.minWidth = ValueWidth;
        valueElement.preferredWidth = ValueWidth;
        valueElement.flexibleWidth = 0f;

        _more = MakeArrow(root.transform, "More", ">");
        _more.Button.onClick.AddListener(() => Press(1, pressed));

        Refresh();
    }

    private static UIButton.Handle MakeArrow(Transform parent, string name, string glyph)
    {
        UIButton.Handle arrow = UIButton.Create(parent, name, "", Style.Button.y);
        arrow.Text.richText = false;
        arrow.Text.text = glyph;
        arrow.Text.overflowMode = TextOverflowModes.Overflow;
        RectTransform text = arrow.Text.rectTransform;
        text.offsetMin = Vector2.zero;
        text.offsetMax = Vector2.zero;
        return arrow;
    }

    private void Press(int direction, Action<PanelRow> pressed)
    {
        Step?.Invoke(direction);
        pressed?.Invoke(this);
    }

    public void Refresh()
    {
        if (_value == null) return;
        _value.text = Value();

        bool high = Settings.PanelContrast == PanelContrast.High;
        FontStyles weight = high ? FontStyles.Bold : FontStyles.Normal;
        _name.fontStyle = weight;
        _value.fontStyle = weight;

        bool enabled = Enabled == null || Enabled();
        _group.alpha = enabled ? 1f : high ? HighContrastDisabledAlpha : DisabledAlpha;
        _group.interactable = enabled;
        _less.Button.interactable = enabled;
        _more.Button.interactable = enabled;
    }
}

public static class PanelScroll
{
    public static RectTransform Make(Transform parent, string name)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();

        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(root.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        UILayout.Stretch(viewportRect);
        viewport.AddComponent<RectMask2D>();
        Image dragSurface = viewport.AddComponent<Image>();
        dragSurface.color = new Color(1f, 1f, 1f, 0f);
        PanelUI.ApplyFront(dragSurface);

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)Style.PanelInset, (int)Style.PanelInset,
            (int)Style.SmallPadding, (int)Style.SmallPadding);
        layout.spacing = Style.SmallPadding;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;

        LayoutElement element = root.AddComponent<LayoutElement>();
        element.flexibleHeight = 1f;
        element.flexibleWidth = 1f;
        return contentRect;
    }
}

public sealed class PanelRows
{
    private readonly List<PanelRow> _rows = new List<PanelRow>();
    private readonly RectTransform _content;
    private readonly Action<PanelRow> _pressed;

    public PanelRows(Transform parent, Action<PanelRow> pressed)
    {
        _pressed = pressed;
        RectTransform scroll = (RectTransform)PanelScroll.Make(parent, "RowList").parent.parent;
        UILayout.Ensure<LayoutElement>(scroll.gameObject).ignoreLayout = true;
        UILayout.Stretch(scroll);
        _content = scroll.GetComponent<ScrollRect>().content;
    }

    public void AddHeading(string key)
    {
        TextMeshProUGUI heading = UILabel.Make(_content, $"Heading_{key}", Style.Black,
            TextAlignmentOptions.BottomLeft, TextOverflowModes.Ellipsis, bold: true);
        Strings.Bind(heading, key);
        UILayout.FixedHeight(heading.gameObject, Style.HeaderHeight);
    }

    public PanelRow Add(PanelRow row)
    {
        row.Build(_content, _pressed);
        _rows.Add(row);
        return row;
    }

    public void Refresh()
    {
        for (int i = 0; i < _rows.Count; i++) _rows[i].Refresh();
    }
}
