using System;
using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    public class Recenter : MonoBehaviour
    {
        private const double AwaySeconds = 5.0;


        private static bool _hasHome;
        private static Vector3 _homePos;
        private static float _homeYaw;

        private DateTime _offSince;
        private bool _off;
        private bool _pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _hasHome = false;
        }

        public static void NoteHome()
        {
            if (_hasHome) return;
            Transform cam = CameraRig.MainTransform;
            if (cam == null) return;
            _homePos = cam.position;
            _homeYaw = YawOf(cam);
            _hasHome = true;
        }

        private void OnEnable()
        {
            OVRManager.HMDUnmounted += OnUnmounted;
            OVRManager.HMDMounted += OnMounted;
            if (OVRManager.display != null) OVRManager.display.RecenteredPose += OnRecentered;
        }

        private void OnDisable()
        {
            OVRManager.HMDUnmounted -= OnUnmounted;
            OVRManager.HMDMounted -= OnMounted;
            if (OVRManager.display != null) OVRManager.display.RecenteredPose -= OnRecentered;
        }

        private void OnUnmounted()
        {
            _off = true;
            _offSince = DateTime.UtcNow;
        }

        private void OnMounted()
        {
            if (!_off) return;
            _off = false;
            if ((DateTime.UtcNow - _offSince).TotalSeconds >= AwaySeconds) _pending = true;
        }

        private void OnRecentered() => _pending = true;

        private void LateUpdate()
        {
            if (!_hasHome && LoadingPhase.Done && CameraRig.TryGetBasis(out _, out _)) NoteHome();
            if (!_pending || !_hasHome) return;
            if (!CameraRig.TryGetBasis(out _, out _)) return;
            _pending = false;
            Apply();
        }

        private static void Apply()
        {
            Transform cam = CameraRig.MainTransform;
            Transform rig = RigRoot(cam);
            if (cam == null || rig == null) return;

            Vector3 head = cam.position;
            Quaternion turn = Quaternion.AngleAxis(Mathf.DeltaAngle(YawOf(cam), _homeYaw), Vector3.up);
            rig.SetPositionAndRotation(_homePos + turn * (rig.position - head), turn * rig.rotation);
        }

        private static Transform RigRoot(Transform cam)
        {
            OVRCameraRig rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null) return rig.transform;
            return cam != null && cam.parent != null ? cam.parent.parent : null;
        }

        private static float YawOf(Transform t) =>
            Quaternion.LookRotation(CameraRig.Flatten(t.forward, Vector3.forward), Vector3.up).eulerAngles.y;
    }
}
