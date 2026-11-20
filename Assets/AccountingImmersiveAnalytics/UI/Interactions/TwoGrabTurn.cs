using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public sealed class TwoGrabTurn : MonoBehaviour, ITransformer
    {
        public Transform Pivot;

        private IGrabbable _grabbable;
        private Vector3 _pivot;
        private Vector3 _offset;
        private Quaternion _start;
        private Vector3 _span;

        public void Initialize(IGrabbable grabbable) => _grabbable = grabbable;

        public void BeginTransform()
        {
            Transform target = _grabbable.Transform;
            List<Pose> points = _grabbable.GrabPoints;
            _pivot = Pivot != null ? Pivot.position : target.position;
            _offset = target.position - _pivot;
            _start = target.rotation;
            _span = Flat(points[1].position - points[0].position);
        }

        public void UpdateTransform()
        {
            List<Pose> points = _grabbable.GrabPoints;
            Vector3 span = Flat(points[1].position - points[0].position);
            if (span.sqrMagnitude < 1e-8f || _span.sqrMagnitude < 1e-8f) return;

            Quaternion turn = Quaternion.AngleAxis(Vector3.SignedAngle(_span, span, Vector3.up), Vector3.up);
            _grabbable.Transform.SetPositionAndRotation(_pivot + turn * _offset, turn * _start);
        }

        public void EndTransform() { }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
