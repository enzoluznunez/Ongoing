using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    public class HeadPose : MonoBehaviour
    {
        private const float SteadySeconds = 0.3f;
        private const float SteadyMetres = 0.02f;

        private const float LowestHead = 0.5f;

        private const float GiveUpSeconds = 3f;

        public static bool Settled { get; private set; }


        private float _height;
        private float _steadyFor;
        private float _trackedFor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Settled = false;
        }

        private void Update()
        {
            Transform cam = CameraRig.MainTransform;
            if (cam == null || !Tracked())
            {
                Settled = false;
                _trackedFor = 0f;
                _steadyFor = 0f;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _trackedFor += dt;

            float y = cam.localPosition.y;
            if (Mathf.Abs(y - _height) > SteadyMetres)
            {
                _height = y;
                _steadyFor = 0f;
            }
            else _steadyFor += dt;

            bool steady = _steadyFor >= SteadySeconds && OriginApplied() && (!OnFloor() || y >= LowestHead);
            Settled = steady || _trackedFor >= GiveUpSeconds;
        }

        private static bool Tracked()
        {
            if (!OVRManager.OVRManagerinitialized) return true;
            return OVRPlugin.userPresent && OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.EyeCenter);
        }

        private static bool OnFloor() =>
            OVRManager.instance != null && OVRManager.instance.trackingOriginType != OVRManager.TrackingOrigin.EyeLevel;

        private static bool OriginApplied()
        {
            if (!OVRManager.OVRManagerinitialized || OVRManager.instance == null) return true;
            return (int)OVRPlugin.GetTrackingOriginType() == (int)OVRManager.instance.trackingOriginType;
        }
    }
}
