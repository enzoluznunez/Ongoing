using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts
{
    public struct MoveRecord
    {
        public int ChartId;
        public Vector3 PrePos;
        public Quaternion PreRot;
        public Vector3 PreScale;

        public static bool Moved(Vector3 a, Vector3 b) => (a - b).sqrMagnitude > 1e-8f;

        public static bool Turned(Quaternion a, Quaternion b) => Quaternion.Angle(a, b) > 0.01f;

        public static bool SamePose(Vector3 aPos, Quaternion aRot, Vector3 aScale,
            Vector3 bPos, Quaternion bRot, Vector3 bScale) =>
            !Moved(aPos, bPos) && !Turned(aRot, bRot) && !Moved(aScale, bScale);
    }
}
