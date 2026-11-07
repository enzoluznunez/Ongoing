using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ModularPanel : PanelUI
{
    private const float RightWidth = 420f;

    private sealed class Card
    {
        public GameObject Root;
        public RectTransform Rect;
        public RectTransform Pane;
        public bool Open;
    }

    private Card _bottom;
    private Card _right;
    private RectTransform _primary;
    private Coroutine _settle;

    protected PanelRows Rows { get; private set; }
    protected TextMeshProUGUI BottomHeader { get; private set; }
    protected TextMeshProUGUI BottomText { get; private set; }
    protected RectTransform BottomExtra { get; private set; }
    protected TextMeshProUGUI RightHeader { get; private set; }
    protected RectTransform RightContent { get; private set; }

    protected void BuildPanel(string titleKey, string contentName, bool hasRight)
    {
        InitPanel(Style.Panel);
        PinCanvasToTopLeft();

        _primary = FindTransform.FindDeep(transform, "PrimaryPanel") as RectTransform;
        PinTopLeft(_primary, Vector2.zero);
        if (_primary != null) _primary.sizeDelta = Style.Panel;

        Transform titleBar = FindTransform.FindDeep(_primary, "TitleBar");
        Transform title = titleBar != null ? titleBar.Find("Title") : null;
        if (title == null) title = FindTransform.FindDeep(_primary, "Title");
        if (title != null) Strings.Bind(title.GetComponent<TMP_Text>(), titleKey);

        Transform content = FindTransform.FindDeep(transform, contentName);
        foreach (LayoutGroup group in content.GetComponents<LayoutGroup>()) group.enabled = false;
        UILayout.Clear(content);
        LayoutElement contentElement = UILayout.Ensure<LayoutElement>(content.gameObject);
        contentElement.minHeight = 0f;
        contentElement.preferredHeight = 0f;
        contentElement.flexibleHeight = 1f;
        Rows = new PanelRows(content, OnRowPressed);

        GameObject template = FindTransform.FindDeep(transform, "OptionsPanel")?.gameObject;
        if (template != null)
        {
            Transform area = FindTransform.FindDeep(template.transform, "ContentArea");
            UILayout.Clear(area);
            _bottom = Adopt(template, area);
            BuildBottomPane();

            if (hasRight)
            {
                GameObject copy = Instantiate(template, template.transform.parent, false);
                copy.name = "RightPanel";
                Transform copyArea = FindTransform.FindDeep(copy.transform, "ContentArea");
                UILayout.Clear(copyArea);
                _right = Adopt(copy, copyArea);
                BuildRightPane();
            }
        }

        SetGrabRects(_primary, _bottom?.Rect, _right?.Rect);
        ApplyDividers(transform);
        ApplyTitleBars(transform);
        Strings.Changed += OnLanguageChanged;
    }

    protected virtual void OnDestroy() => Strings.Changed -= OnLanguageChanged;

    protected virtual void OnDisable() => _settle = null;

    private void OnLanguageChanged()
    {
        RefreshPanel();
        QueueSettle();
    }

    protected abstract void RefreshPanel();

    protected virtual void OnRowPressed(PanelRow row) => RefreshPanel();

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
    }

    private static void PinTopLeft(RectTransform rect, Vector2 at)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = at;
    }

    private static Card Adopt(GameObject root, Transform area)
    {
        RectTransform rect = (RectTransform)root.transform;
        PinTopLeft(rect, Vector2.zero);
        UILayout.ShowPart(root, false);

        var pane = new GameObject("Pane", typeof(RectTransform));
        pane.transform.SetParent(area, false);
        RectTransform paneRect = pane.GetComponent<RectTransform>();
        UILayout.Stretch(paneRect);

        VerticalLayoutGroup layout = pane.AddComponent<VerticalLayoutGroup>();
        ApplyPaneLayout(layout);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return new Card { Root = root, Rect = rect, Pane = paneRect };
    }

    private static TextMeshProUGUI MakeHeader(Transform pane)
    {
        TextMeshProUGUI header = UILabel.Make(pane, "Header", Style.Black,
            TextAlignmentOptions.Left, TextOverflowModes.Ellipsis, bold: true);
        UILayout.FixedHeight(header.gameObject, Style.HeaderHeight);
        return header;
    }

    private void BuildBottomPane()
    {
        BottomHeader = MakeHeader(_bottom.Pane);
        BottomText = UILabel.Make(_bottom.Pane, "Description", Style.Black,
            TextAlignmentOptions.TopLeft, TextOverflowModes.Overflow, wrap: true);

        var extra = new GameObject("Extra", typeof(RectTransform));
        extra.transform.SetParent(_bottom.Pane, false);
        BottomExtra = extra.GetComponent<RectTransform>();
        VerticalLayoutGroup layout = extra.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private void BuildRightPane()
    {
        RightHeader = MakeHeader(_right.Pane);
        RightContent = PanelScroll.Make(_right.Pane, "RightList");
        RightContent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
    }

    protected void SetBottomOpen(bool open) => SetOpen(_bottom, open);

    protected void SetRightOpen(bool open) => SetOpen(_right, open);

    private void SetOpen(Card card, bool open)
    {
        if (card == null || card.Open == open) return;
        card.Open = open;
        UILayout.ShowPart(card.Root, open);
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
        yield return UILayout.Converge(() => IsVisible, LayoutCards,
            () => _bottom != null ? _bottom.Rect.sizeDelta.y : -1f);
        _settle = null;
    }

    private void LayoutCards()
    {
        RectTransform canvas = CanvasRect;
        if (canvas == null) return;

        float width = Style.Panel.x;
        float height = Style.Panel.y;

        bool rightOpen = _right != null && _right.Open;
        if (rightOpen)
        {
            _right.Rect.sizeDelta = new Vector2(RightWidth, Style.Panel.y);
            PinTopLeft(_right.Rect, new Vector2(Style.Panel.x + Style.SmallPadding, 0f));
            width += Style.SmallPadding + RightWidth;
        }

        if (_bottom != null && _bottom.Open)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvas);
            float bottomHeight = _bottom.Rect.sizeDelta.y;
            if (UIMeasure.TryPreferredHeight(_bottom.Rect, _bottom.Pane, out float content))
                bottomHeight = Mathf.Min(content + Style.SmallPadding, Style.Subpanel.y);
            _bottom.Rect.sizeDelta = new Vector2(width, bottomHeight);
            PinTopLeft(_bottom.Rect, new Vector2(0f, -(Style.Panel.y + Style.SmallPadding)));
            height += Style.SmallPadding + bottomHeight;
        }

        canvas.sizeDelta = new Vector2(width, height);
        InvalidateGrabBounds();
    }
}
