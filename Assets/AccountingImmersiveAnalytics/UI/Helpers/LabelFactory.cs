using OneText;
using OneText.UGUI;
using UnityEngine;
using TextAlignment = OneText.TextAlignment;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public enum LabelAnchor { TopStart, Start, Center, Left, Right }

    public static class LabelFactory
    {
        public static OneTextLabel Make(Transform parent, string name,
            Color color, LabelAnchor anchor, TextOverflow overflow,
            bool bold = false, bool wrap = false)
        {
            GameObject obj = LayoutTools.EnsureChild(parent, name).gameObject;

            OneTextLabel label = LayoutTools.Ensure<OneTextLabel>(obj);
            if (bold) Style.ApplyTitle(label);
            else Style.ApplyBody(label);
            label.color = color;
            Place(label, anchor);
            label.Overflow = overflow;
            label.Wrap = wrap ? TextWrap.Wrap : TextWrap.NoWrap;
            label.RichText = false;
            label.raycastTarget = false;
            return label;
        }

        public static void Place(OneTextLabel label, LabelAnchor anchor)
        {
            label.Alignment = anchor == LabelAnchor.Center ? TextAlignment.Center
                : anchor == LabelAnchor.Right ? TextAlignment.Right
                : anchor == LabelAnchor.Left ? TextAlignment.Left
                : TextAlignment.Start;
            label.VerticalAlignment = anchor == LabelAnchor.TopStart ? VerticalAlignment.Top : VerticalAlignment.Middle;
        }
    }
}
