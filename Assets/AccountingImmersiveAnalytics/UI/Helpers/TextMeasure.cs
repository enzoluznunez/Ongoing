using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public static class TextMeasure
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        public static bool TryLocalBounds(RectTransform layoutRoot, RectTransform[] targets, Transform space,
            out Vector3 center, out Vector3 size)
        {
            center = Vector3.zero;
            size = Vector3.zero;
            if (layoutRoot == null || targets == null || space == null) return false;

            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool any = false;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || !LayoutTools.PartShown(targets[i].gameObject)) continue;

                targets[i].GetWorldCorners(Corners);
                for (int c = 0; c < 4; c++)
                {
                    Vector3 local = space.InverseTransformPoint(Corners[c]);
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
                any = true;
            }
            if (!any) return false;

            center = (min + max) * 0.5f;
            size = max - min;
            return true;
        }
    }
}
