using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Tools
{
    public readonly struct ChartPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;

        public ChartPose(Transform chart)
        {
            Position = chart.localPosition;
            Rotation = chart.localRotation;
            Scale = chart.localScale;
        }
    }
}
