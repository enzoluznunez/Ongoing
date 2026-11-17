using System;
using System.Collections;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public sealed class PanelLineup : MonoBehaviour
    {
        public const float Gap = 0.03f;
        public const float SlideSeconds = 0.3f;

        private static PanelLineup _instance;
        private static readonly List<Panel> _docked = new List<Panel>();
        private static readonly Dictionary<Panel, Pose> _targets = new Dictionary<Panel, Pose>();
        private static Vector3 _head;
        private static Vector3 _forward = Vector3.forward;
        private static Coroutine _slide;
        private static Action _after;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _docked.Clear();
            _targets.Clear();
            _slide = null;
            _after = null;
        }

        private void Awake() => _instance = this;

        public static bool IsDocked(Panel panel) => _docked.Contains(panel);

        public static bool IsSliding => _slide != null;

        public static void Open(Panel panel, Action reveal)
        {
            Finish();
            _docked.Remove(panel);
            _docked.Insert(0, panel);
            FaceUser();
            Arrange(panel.CanvasShown ? null : panel, reveal, false);
        }

        public static void Close(Panel panel)
        {
            Finish();
            if (!_docked.Remove(panel)) return;
            Arrange(null, null, false);
        }

        public static void Resize(Panel panel)
        {
            Finish();
            if (IsDocked(panel)) Arrange(null, null, true);
        }

        public static void Undock(Panel panel)
        {
            if (!_docked.Remove(panel)) return;
            _targets.Remove(panel);
            panel.LineupChanged();
        }

        public static void UndockIfMoved(Panel panel)
        {
            if (_slide != null || !_targets.TryGetValue(panel, out Pose pose)) return;
            Transform t = panel.transform;
            if ((t.position - pose.position).sqrMagnitude > 1e-6f || Quaternion.Angle(t.rotation, pose.rotation) > 0.1f)
                Undock(panel);
        }

        private static void FaceUser()
        {
            Transform cam = CameraRig.MainTransform;
            if (cam == null) return;
            _head = cam.position;
            _forward = CameraRig.FlatForward;
        }

        private static void Arrange(Panel arriving, Action after, bool slide)
        {
            _targets.Clear();
            if (CameraRig.MainTransform == null)
            {
                Debug.LogWarning("[PanelLineup] No camera tagged MainCamera; panels open wherever the scene left them.");
                after?.Invoke();
                return;
            }

            float total = 0f;
            var halves = new float[_docked.Count];
            var gaps = new float[_docked.Count];
            for (int i = 0; i < _docked.Count; i++)
            {
                Panel panel = _docked[i];
                float halfWidth = panel.SlotWidth * 0.5f;
                float radius = panel.zOffsetFromCamera;
                halves[i] = Mathf.Atan2(halfWidth, radius) * Mathf.Rad2Deg;
                float rim = Mathf.Sqrt(radius * radius + halfWidth * halfWidth);
                gaps[i] = i == 0 ? 0f : 2f * Mathf.Asin(Mathf.Clamp01(Gap / (2f * rim))) * Mathf.Rad2Deg;
                total += gaps[i] + halves[i] * 2f;
            }

            float angle = -total * 0.5f;
            for (int i = 0; i < _docked.Count; i++)
            {
                angle += gaps[i] + halves[i];
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * _forward;
                _targets[_docked[i]] = _docked[i].SlotPose(_head, direction);
                angle += halves[i];
            }

            for (int i = 0; i < _docked.Count; i++) _docked[i].LineupChanged();

            if (arriving != null && _targets.TryGetValue(arriving, out Pose arrival)) Place(arriving, arrival);

            _after = after;
            if (!slide || !NeedsSlide(arriving))
            {
                Finish();
                return;
            }

            _slide = _instance.StartCoroutine(Slide(arriving));
        }

        private static bool NeedsSlide(Panel arriving)
        {
            foreach (KeyValuePair<Panel, Pose> entry in _targets)
            {
                if (entry.Key == arriving || entry.Key == null) continue;
                Transform t = entry.Key.transform;
                if ((t.position - entry.Value.position).sqrMagnitude > 1e-6f) return true;
                if (Quaternion.Angle(t.rotation, entry.Value.rotation) > 0.1f) return true;
            }
            return false;
        }

        private static IEnumerator Slide(Panel arriving)
        {
            var from = new Dictionary<Panel, Pose>();
            foreach (Panel panel in _targets.Keys)
                if (panel != null && panel != arriving) from[panel] = new Pose(panel.transform.position, panel.transform.rotation);

            yield return Style.Tween(SlideSeconds, t =>
            {
                foreach (KeyValuePair<Panel, Pose> start in from)
                {
                    if (start.Key == null || !IsDocked(start.Key) || !_targets.TryGetValue(start.Key, out Pose goal)) continue;
                    start.Key.transform.SetPositionAndRotation(
                        Vector3.Lerp(start.Value.position, goal.position, t),
                        Quaternion.Slerp(start.Value.rotation, goal.rotation, t));
                }
            });

            _slide = null;
            Finish();
        }

        private static void Finish()
        {
            if (_slide != null && _instance != null) _instance.StopCoroutine(_slide);
            _slide = null;

            foreach (KeyValuePair<Panel, Pose> target in _targets)
                if (target.Key != null && IsDocked(target.Key)) Place(target.Key, target.Value);

            Action after = _after;
            _after = null;
            after?.Invoke();
        }

        private static void Place(Panel panel, Pose pose) =>
            panel.transform.SetPositionAndRotation(pose.position, pose.rotation);
    }
}
