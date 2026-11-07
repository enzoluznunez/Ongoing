using UnityEngine;

public static class ChartRaycast
{
    public struct Hit
    {
        public ChartView view;
        public Vector3 point;
        public float distance;
    }

    public static bool Cast(Vector3 origin, Vector3 direction, float maxDistance, out Hit hit)
    {
        hit = default;

        ManageCharts charts = Scene.Charts;
        ChartView view = charts != null ? charts.View : null;
        if (view == null || !view.Shown || view.Falling) return false;

        Transform frame = view.transform;
        Vector3 localOrigin = frame.InverseTransformPoint(origin);
        Vector3 localStep = frame.InverseTransformVector(direction);
        Bounds bounds = view.LocalBounds;

        float enter = 0f, exit = maxDistance;
        if (!Clip(localOrigin.x, localStep.x, bounds.min.x, bounds.max.x, ref enter, ref exit)) return false;
        if (!Clip(localOrigin.y, localStep.y, bounds.min.y, bounds.max.y, ref enter, ref exit)) return false;
        if (!Clip(localOrigin.z, localStep.z, bounds.min.z, bounds.max.z, ref enter, ref exit)) return false;

        hit = new Hit { view = view, point = origin + direction * enter, distance = enter };
        return true;
    }

    private static bool Clip(float start, float step, float low, float high, ref float enter, ref float exit)
    {
        if (Mathf.Abs(step) < 1e-7f) return start >= low && start <= high;

        float a = (low - start) / step, b = (high - start) / step;
        if (a > b) (a, b) = (b, a);
        enter = Mathf.Max(enter, a);
        exit = Mathf.Min(exit, b);
        return enter <= exit;
    }
}
