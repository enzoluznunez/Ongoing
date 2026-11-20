using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ScrollMomentum : MonoBehaviour
    {
        private const float Deceleration = 0.135f;

        private ScrollRect _scroll;

        private void Awake()
        {
            _scroll = GetComponent<ScrollRect>();
            _scroll.decelerationRate = Deceleration;
            _scroll.inertia = true;
        }
    }
}
