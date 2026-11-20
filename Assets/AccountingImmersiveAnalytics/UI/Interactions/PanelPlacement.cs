using AccountingImmersiveAnalytics.App;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public static class PanelPlacement
    {
        public const float MinReach = 0.35f;
        public const float MaxReach = 2f;

        public static Vector3 Reachable(Vector3 target, out bool limited)
        {
            limited = false;

            Transform cam = CameraRig.MainTransform;
            if (cam == null) return target;

            Vector3 delta = target - cam.position;
            float distance = delta.magnitude;
            if (distance < 1e-4f) return target;

            float clamped = Mathf.Clamp(distance, MinReach, MaxReach);
            if (Mathf.Approximately(clamped, distance)) return target;

            limited = true;
            return cam.position + delta / distance * clamped;
        }
    }
}
