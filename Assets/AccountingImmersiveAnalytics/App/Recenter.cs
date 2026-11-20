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

        private static void NoteHome(Vector3 head, float yaw)
        {
            _homePos = head;
            _homeYaw = yaw;
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
            if (!CameraRig.TryGetBasis(out Vector3 head, out Quaternion basis)) return;
            float yaw = basis.eulerAngles.y;
            if (!_hasHome)
            {
                if (LoadingPhase.Done) NoteHome(head, yaw);
                return;
            }
            if (!_pending) return;
            _pending = false;
            Apply(head, yaw);
        }

        private static void Apply(Vector3 head, float yaw)
        {
            Transform rig = RigRoot(CameraRig.MainTransform);
            if (rig == null) return;

            Quaternion turn = Quaternion.AngleAxis(Mathf.DeltaAngle(yaw, _homeYaw), Vector3.up);
            rig.SetPositionAndRotation(_homePos + turn * (rig.position - head), turn * rig.rotation);
        }

        private static Transform RigRoot(Transform cam)
        {
            OVRCameraRig rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null) return rig.transform;
            return cam != null && cam.parent != null ? cam.parent.parent : null;
        }
    }
}
