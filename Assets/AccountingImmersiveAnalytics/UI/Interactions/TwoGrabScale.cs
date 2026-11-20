using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public sealed class TwoGrabScale : MonoBehaviour, ITransformer
    {
        private const float ShortestSpan = 0.01f;

        private IGrabbable _grabbable;
        private float _span;
        private Vector3 _middle;
        private Vector3 _offset;
        private Vector3 _scale;

        public void Initialize(IGrabbable grabbable) => _grabbable = grabbable;

        public void BeginTransform()
        {
            Transform target = _grabbable.Transform;
            List<Pose> points = _grabbable.GrabPoints;
            _span = Mathf.Max(ShortestSpan, Vector3.Distance(points[0].position, points[1].position));
            _middle = (points[0].position + points[1].position) * 0.5f;
            _offset = target.position - _middle;
            _scale = target.localScale;
        }

        public void UpdateTransform()
        {
            List<Pose> points = _grabbable.GrabPoints;
            float span = Mathf.Max(ShortestSpan, Vector3.Distance(points[0].position, points[1].position));
            float factor = span / _span;
            Transform target = _grabbable.Transform;
            target.localScale = _scale * factor;
            target.position = _middle + _offset * factor;
        }

        public void EndTransform() { }
    }
}
