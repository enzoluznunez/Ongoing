using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionList
{
    private readonly RectTransform _canvas;
    private RectTransform _blocker;
    private RectTransform _list;
    private readonly List<UIButton.Handle> _options = new List<UIButton.Handle>();
    private Action<int> _picked;
    private Action _closed;

    public bool IsOpen => _list != null && _list.gameObject.activeSelf;

    public RectTransform Owner { get; private set; }

    public OptionList(RectTransform canvas) => _canvas = canvas;

    public void Open(RectTransform owner, IReadOnlyList<string> labels, int selected,
        Action<int> picked, Action closed = null)
    {
        Close();
        if (_canvas == null || owner == null) return;

        Owner = owner;
        _picked = picked;
        _closed = closed;
        EnsureBuilt();
        Fill(labels, selected);
        Place(owner, labels.Count);

        _blocker.gameObject.SetActive(true);
        _list.gameObject.SetActive(true);
        _blocker.SetAsLastSibling();
        _list.SetAsLastSibling();
    }

    public void Close()
    {
        if (!IsOpen) return;
        _blocker.gameObject.SetActive(false);
        _list.gameObject.SetActive(false);
        Owner = null;
        Action closed = _closed;
        _picked = null;
        _closed = null;
        closed?.Invoke();
    }

    private void Pick(int index)
    {
        Action<int> picked = _picked;
        Close();
        picked?.Invoke(index);
    }

    private void EnsureBuilt()
    {
        if (_list != null) return;

        _blocker = UILayout.EnsureChild(_canvas, "DropdownBlocker");
        UILayout.Stretch(_blocker);
        Image catcher = UILayout.Ensure<Image>(_blocker.gameObject);
        catcher.color = Style.Alpha(Style.White, 0f);
        catcher.raycastTarget = true;
        UnityEngine.UI.Button close = UILayout.Ensure<UnityEngine.UI.Button>(_blocker.gameObject);
        close.transition = Selectable.Transition.None;
        close.onClick.AddListener(Close);
        UILayout.Ensure<LayoutElement>(_blocker.gameObject).ignoreLayout = true;

        _list = UILayout.EnsureChild(_canvas, "DropdownList");
        UILayout.Ensure<LayoutElement>(_list.gameObject).ignoreLayout = true;
        _list.anchorMin = _list.anchorMax = new Vector2(0.5f, 0.5f);
        _list.pivot = new Vector2(0f, 1f);

        Image frame = UILayout.Ensure<Image>(_list.gameObject);
        RoundedSprite.Apply(frame, Style.OutRadius);
        frame.color = Style.Black;
        frame.raycastTarget = true;
        PanelUI.ApplyFront(frame);

        RectTransform innerRt = UILayout.EnsureChild(_list, "Inner");
        UILayout.Stretch(innerRt, Style.SmallBorder);
        Image inner = UILayout.Ensure<Image>(innerRt.gameObject);
        RoundedSprite.Apply(inner, Style.InnerRadius(Style.OutRadius));
        inner.color = Style.White;
        inner.raycastTarget = false;
        PanelUI.ApplyFront(inner);

        VerticalLayoutGroup layout = UILayout.Ensure<VerticalLayoutGroup>(_list.gameObject);
        int pad = (int)Style.SmallPadding;
        layout.padding = new RectOffset(pad, pad, pad, pad);
        layout.spacing = Style.SmallPadding;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        UILayout.Ensure<LayoutElement>(innerRt.gameObject).ignoreLayout = true;
    }

    private void Fill(IReadOnlyList<string> labels, int selected)
    {
        while (_options.Count < labels.Count)
        {
            int index = _options.Count;
            UIButton.Handle h = UIButton.Create(_list, $"Option_{index}", "", flexibleWidth: true);
            h.Button.onClick.AddListener(() => Pick(index));
            _options.Add(h);
        }

        for (int i = 0; i < _options.Count; i++)
        {
            bool used = i < labels.Count;
            _options[i].Root.SetActive(used);
            if (!used) continue;
            _options[i].Text.text = labels[i];
            UIButton.SetSelected(_options[i], i == selected);
        }
    }

    private void Place(RectTransform owner, int count)
    {
        float height = count * Style.Button.y + (count - 1) * Style.SmallPadding + Style.SmallPadding * 2f;

        var corners = new Vector3[4];
        owner.GetWorldCorners(corners);
        Vector2 bottomLeft = _canvas.InverseTransformPoint(corners[0]);
        Vector2 topLeft = _canvas.InverseTransformPoint(corners[1]);
        Vector2 topRight = _canvas.InverseTransformPoint(corners[2]);
        float width = topRight.x - topLeft.x;

        Rect bounds = _canvas.rect;
        bool below = bottomLeft.y - Style.SmallPadding - height >= bounds.yMin;
        float top = below
            ? bottomLeft.y - Style.SmallPadding
            : topLeft.y + Style.SmallPadding + height;
        top = Mathf.Min(top, bounds.yMax);

        _list.sizeDelta = new Vector2(width, height);
        _list.anchoredPosition = new Vector2(topLeft.x, top) - bounds.center;
    }
}
