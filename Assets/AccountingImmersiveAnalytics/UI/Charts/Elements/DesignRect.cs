using AccountingImmersiveAnalytics.UI.Charts.Marks;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.Elements
{
    public static class DesignRect
    {
        public static RectTransform Child(Transform parent, string name)
        {
            RectTransform rt = LayoutTools.EnsureChild(parent, name);
            rt.gameObject.layer = parent.gameObject.layer;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            return rt;
        }

        public static RectTransform Place(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public static MarkSurface Surface(Transform parent, string name, float width, float height)
        {
            RectTransform rt = Place(Child(parent, name), 0f, 0f, width, height);
            return LayoutTools.Ensure<MarkSurface>(rt.gameObject);
        }
    }
}
