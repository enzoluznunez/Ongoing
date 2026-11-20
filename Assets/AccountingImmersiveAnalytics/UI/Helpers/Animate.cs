using System;
using System.Collections;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public static class Animate
    {
        public const float Fast = 0.1f;
        public const float Medium = 0.2f;
        public const float Slow = 0.3f;

        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        public static IEnumerator Tween(float seconds, Action<float> apply)
        {
            if (seconds <= 0f)
            {
                apply(1f);
                yield break;
            }

            float t = 0f;
            while (t < seconds)
            {
                yield return null;
                t += Time.deltaTime;
                apply(EaseOut(t / seconds));
            }
        }

        public static float Damp(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);
    }
}
