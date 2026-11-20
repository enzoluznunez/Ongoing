using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public sealed class GrabSurface : MonoBehaviour, ICanvasRaycastFilter
    {
        public RectTransform[] Exempt = new RectTransform[0];

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            for (int i = 0; i < Exempt.Length; i++)
                if (Exempt[i] != null && RectTransformUtility.RectangleContainsScreenPoint(Exempt[i], screenPoint, eventCamera)) return false;
            return true;
        }
    }
}
