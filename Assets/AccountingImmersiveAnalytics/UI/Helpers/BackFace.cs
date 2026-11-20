using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public sealed class BackFace : MonoBehaviour
    {
        private RectTransform _back;

        public static Image Add(RectTransform target, float radius)
        {
            if (target == null) return null;
            BackFace face = LayoutTools.Ensure<BackFace>(target.gameObject);

            RectTransform rt = LayoutTools.EnsureChild(target, "BackFace");
            rt.gameObject.layer = target.gameObject.layer;
            LayoutTools.Stretch(rt);
            LayoutTools.Ensure<LayoutElement>(rt.gameObject).ignoreLayout = true;

            Image back = LayoutTools.Ensure<Image>(rt.gameObject);
            RoundedSprite.Apply(back, radius, back: true);
            back.color = Style.Ink;
            back.raycastTarget = false;
            back.enabled = Panel.BackMaterial != null;

            face._back = rt;
            face.KeepLast();
            return back;
        }

        private void OnTransformChildrenChanged() => KeepLast();

        private void KeepLast()
        {
            if (_back != null && _back.GetSiblingIndex() != transform.childCount - 1) _back.SetAsLastSibling();
        }
    }
}
