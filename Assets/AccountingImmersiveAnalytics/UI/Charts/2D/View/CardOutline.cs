using AccountingImmersiveAnalytics.UI.Charts.TwoD.Elements;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.View
{
    public static class CardOutline
    {
        public static Image Make(Transform parent)
        {
            RectTransform rt = DesignRect.Child(parent, "Outline");
            Image ring = LayoutTools.Ensure<Image>(rt.gameObject);
            ring.raycastTarget = false;
            ring.color = Style.Ink;
            LayoutTools.Ensure<LayoutElement>(rt.gameObject).ignoreLayout = true;
            return ring;
        }

        public static void Draw(Image ring, float width, float height, float radius)
        {
            if (ring == null) return;
            ring.rectTransform.sizeDelta = new Vector2(width, height);
            RoundedSprite.ApplyRing(ring, radius, Style.Stroke);
            ring.color = Style.Ink;
        }
    }
}
