using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Interactions;
using AccountingImmersiveAnalytics.UI.Panels;
using OneText;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public static class ButtonFactory
    {
        public class Handle
        {
            public GameObject Root;
            public Button Button;
            public Image Frame;
            public Image Inner;
            public Image Back;
            public OneTextLabel Text;
            public Image Icon;
            public PointerHighlight Highlight;
            public float Radius;
            public bool Borderless;

            public Color RestFill;
            public Color SelectFill;
            public Color RestText;
            public Color SelectText;
        }

        public static Handle Create(Transform parent, string name, string label)
        {
            GameObject root = LayoutTools.EnsureChild(parent, name).gameObject;

            LayoutElement le = LayoutTools.Ensure<LayoutElement>(root);
            le.minHeight = Style.ButtonSize;
            le.preferredHeight = le.minHeight;
            le.flexibleWidth = 0f;

            Handle h = new Handle { Root = root };
            Build(h, label, Style.MediumRadius);
            return h;
        }

        public static Handle Icon(Transform parent, string name, string iconName, System.Action press)
        {
            Handle button = Create(parent, name, "");
            LayoutTools.FixedSize(button.Root, Vector2.one * Style.ButtonSize);
            button.Text.gameObject.SetActive(false);
            button.Text = null;

            RectTransform rt = LayoutTools.EnsureChild(button.Root.transform, "Icon");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * Style.IconSize;
            Image icon = LayoutTools.Ensure<Image>(rt.gameObject);
            icon.sprite = string.IsNullOrEmpty(iconName) ? null : App.AssetLibrary.Icon(iconName);
            icon.enabled = icon.sprite != null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Panel.ApplyFront(icon);
            button.Icon = icon;

            button.Highlight.SetTextTarget(icon);
            SetSelected(button, false);
            if (press != null) button.Button.onClick.AddListener(() => press());
            return button;
        }

        public static void SetSquare(Handle h, float size)
        {
            if (h == null) return;
            RectTransform rt = (RectTransform)h.Root.transform;
            rt.sizeDelta = Vector2.one * size;
            LayoutTools.FixedSize(h.Root, Vector2.one * size);
            if (h.Icon != null) h.Icon.rectTransform.sizeDelta = Vector2.one * (size * IconShare);
        }

        public const float IconShare = Style.IconSize / Style.ButtonSize;

        public static Handle Adopt(GameObject root)
        {
            Handle h = new Handle { Root = root };
            Build(h, null, Style.MediumRadius);
            return h;
        }

        public static Image AddBack(Handle h)
        {
            if (h == null || h.Root == null) return null;
            if (h.Back != null) return h.Back;

            RectTransform rt = LayoutTools.EnsureChild(h.Root.transform, "ButtonBack");
            LayoutTools.Stretch(rt);

            Image back = LayoutTools.Ensure<Image>(rt.gameObject);
            RoundedSprite.Apply(back, h.Radius, back: true);
            back.color = Style.Ink;
            back.raycastTarget = false;

            rt.SetAsLastSibling();

            h.Back = back;
            return back;
        }

        public static void SetSelected(Handle h, bool selected)
        {
            if (h == null) return;

            Color fill = selected ? h.SelectFill : h.RestFill;
            Color press = selected ? h.RestFill : h.SelectFill;
            Color textColor = selected ? h.SelectText : h.RestText;
            Color pressText = selected ? h.RestText : h.SelectText;

            if (h.Highlight != null) h.Highlight.SetState(fill, press, textColor, pressText);
            else if (h.Inner != null) h.Inner.color = fill;

            if (h.Text != null) h.Text.color = textColor;
        }

        public static void SetBorderless(Handle h)
        {
            if (h == null || h.Borderless) return;
            h.Borderless = true;

            h.RestFill = Style.Card;
            h.Frame.color = Color.clear;
            LayoutTools.Stretch(h.Inner.rectTransform);
            RoundedSprite.Apply(h.Inner, h.Radius);
        }

        public static void SetEnabled(Handle h, bool on)
        {
            if (h == null || h.Button.interactable == on) return;
            h.Button.interactable = on;
            h.Frame.raycastTarget = on;
            if (!on && h.Highlight != null) h.Highlight.ClearPointer();
            h.Frame.color = h.Borderless ? Color.clear : on ? Style.Ink : Style.Disabled;
            float alpha = on ? 1f : Style.DisabledAlpha;
            foreach (Transform part in h.Root.transform)
                if (part != h.Inner.transform && part != h.Back?.transform)
                    LayoutTools.Ensure<CanvasGroup>(part.gameObject).alpha = alpha;
        }

        public static void FixText(Handle h)
        {
            if (h == null || h.Text == null) return;
            h.Text.AutoSize = false;
        }

        private static void Build(Handle h, string label, float radius)
        {
            GameObject root = h.Root;
            h.Radius = radius;

            Image frame = LayoutTools.Ensure<Image>(root);
            RoundedSprite.Apply(frame, radius);
            frame.color = Style.Ink;
            float slack = PokeRule.EdgeSlack * PokeRule.Millimetres;
            frame.raycastPadding = new Vector4(-slack, -slack, -slack, -slack);
            h.Frame = frame;

            Button button = LayoutTools.Ensure<Button>(root);
            button.transition = Selectable.Transition.None;
            button.targetGraphic = frame;
            frame.canvasRenderer.SetColor(Color.white);
            h.Button = button;

            RectTransform innerRt = LayoutTools.EnsureChild(root.transform, "Inner");
            LayoutTools.Stretch(innerRt, Style.Stroke);
            Image inner = LayoutTools.Ensure<Image>(innerRt.gameObject);
            RoundedSprite.Apply(inner, Style.InnerRadius(radius));
            inner.raycastTarget = false;
            h.Inner = inner;

            RectTransform textRt = LayoutTools.EnsureChild(root.transform, "Text");
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(Style.MediumPad, 0f);
            textRt.offsetMax = new Vector2(-Style.MediumPad, 0f);
            OneTextLabel text = LayoutTools.Ensure<OneTextLabel>(textRt.gameObject);
            if (label != null) Texts.Bind(text, () => label);
            Style.ApplyBody(text);
            LabelFactory.Place(text, LabelAnchor.Center);
            text.color = Style.Ink;
            text.raycastTarget = false;
            text.Wrap = TextWrap.NoWrap;
            text.Overflow = TextOverflow.Ellipsis;
            h.Text = text;

            PointerHighlight hl = LayoutTools.Ensure<PointerHighlight>(root);
            hl.SetLiftTarget(root.transform);
            h.Highlight = hl;

            h.RestFill = Style.Card;
            h.SelectFill = Style.Ink;
            h.RestText = Style.Ink;
            h.SelectText = Style.Card;
            hl.Target = inner;
            hl.SetTextTarget(text);
            hl.SetState(h.RestFill, h.SelectFill, h.RestText, h.SelectText);
            inner.color = h.RestFill;

            if (root.transform.Find("ButtonBack") != null) AddBack(h);
        }
    }
}
