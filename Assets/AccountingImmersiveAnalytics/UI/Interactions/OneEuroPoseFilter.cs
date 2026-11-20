using System;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public class OneEuroPoseFilter
    {
        private const float DerivativeCutoff = 1f;

        [Serializable]
        public sealed class Settings
        {
            [Tooltip("Lowest position smoothing frequency in Hz; lower removes more shimmer at rest.")]
            public float posMinCutoff = 1f;
            [Tooltip("How quickly position smoothing releases as the hand speeds up; raise if it trails fast motion.")]
            public float posBeta = 0.02f;
            [Tooltip("Lowest rotation smoothing frequency in Hz; lower removes more shimmer at rest.")]
            public float rotMinCutoff = 1f;
            [Tooltip("How quickly rotation smoothing releases as the hand speeds up.")]
            public float rotBeta = 0.05f;
        }

        public Settings settings = new Settings();

        private bool _hasPrevious;
        private Vector3 _position;
        private Vector3 _velocity;
        private Quaternion _rotation = Quaternion.identity;
        private float _angularSpeed;

        public void Reset() => _hasPrevious = false;

        public Pose Filter(Pose raw, float dt, float hardening = 1f)
        {
            if (!_hasPrevious || dt <= 0f)
            {
                _hasPrevious = true;
                _position = raw.position;
                _rotation = raw.rotation;
                _velocity = Vector3.zero;
                _angularSpeed = 0f;
                return raw;
            }

            Vector3 rawVelocity = (raw.position - _position) / dt;
            _velocity = Vector3.Lerp(_velocity, rawVelocity, Alpha(DerivativeCutoff, dt));
            float posCutoff = (settings.posMinCutoff + settings.posBeta * _velocity.magnitude) * hardening;
            _position = Vector3.Lerp(_position, raw.position, Alpha(posCutoff, dt));

            float rawAngularSpeed = Quaternion.Angle(_rotation, raw.rotation) / dt;
            _angularSpeed = Mathf.Lerp(_angularSpeed, rawAngularSpeed, Alpha(DerivativeCutoff, dt));
            float rotCutoff = (settings.rotMinCutoff + settings.rotBeta * _angularSpeed) * hardening;
            _rotation = Quaternion.Slerp(_rotation, raw.rotation, Alpha(rotCutoff, dt));

            return new Pose(_position, _rotation);
        }

        private static float Alpha(float cutoff, float dt)
        {
            float tau = 1f / (2f * Mathf.PI * Mathf.Max(cutoff, 1e-4f));
            return 1f / (1f + tau / dt);
        }
    }
}
