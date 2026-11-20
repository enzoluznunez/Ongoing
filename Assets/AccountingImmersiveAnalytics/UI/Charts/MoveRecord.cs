using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts
{
    public struct MoveRecord
    {
        public int ChartId;
        public Vector3 PrePos;
        public Quaternion PreRot;
        public Vector3 PreScale;

        public static bool SamePose(Vector3 aPos, Quaternion aRot, Vector3 aScale,
            Vector3 bPos, Quaternion bRot, Vector3 bScale) =>
            (aPos - bPos).sqrMagnitude < 1e-8f && Quaternion.Angle(aRot, bRot) < 0.01f &&
            (aScale - bScale).sqrMagnitude < 1e-8f;
    }
}
