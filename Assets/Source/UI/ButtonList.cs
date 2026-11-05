using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ButtonList
{
    private readonly RectTransform _root;
    private readonly List<UIButton.Handle> _items = new List<UIButton.Handle>();

    public IReadOnlyList<UIButton.Handle> Items => _items;

    public ButtonList(RectTransform root)
    {
        _root = root;

        HorizontalLayoutGroup lg = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
        lg.spacing = Style.SmallPadding;
        lg.childAlignment = TextAnchor.UpperLeft;
        lg.childControlWidth = true;
        lg.childControlHeight = true;
        lg.childForceExpandWidth = true;
        lg.childForceExpandHeight = true;
    }

    public UIButton.Handle Add(string name, string label, UnityAction onClick)
    {
        UIButton.Handle h = UIButton.Create(_root, name, label, flexibleWidth: true);
        if (onClick != null) h.Button.onClick.AddListener(onClick);
        _items.Add(h);
        return h;
    }

    public void SetSelected(int activeIndex)
    {
        for (int i = 0; i < _items.Count; i++)
            UIButton.SetSelected(_items[i], i == activeIndex);
    }
}
