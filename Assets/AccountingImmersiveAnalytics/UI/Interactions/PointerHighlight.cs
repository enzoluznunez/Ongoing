using System.Collections;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public class PointerHighlight : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float LiftDuration = Animate.Fast;

        public Graphic Target;
        public Color RestColor;
        public Color PressColor;

        public Transform LiftTarget;

        public Graphic TextTarget;
        public Color TextRestColor;
        public Color TextPressColor;

        private bool _hovered;
        private bool _pressed;
        private float _baseDepth;
        private float _liftT;
        private Coroutine _lift;

        private Transform Lifted => LiftTarget != null ? LiftTarget : transform;

        private bool Raised => _hovered && !_pressed;

        private void Awake()
        {
            _baseDepth = Lifted.localPosition.z;
        }

        public void SetLiftTarget(Transform t)
        {
            LiftTarget = t;
            _baseDepth = Lifted.localPosition.z;
        }

        public void SetTextTarget(Graphic text) => TextTarget = text;

        public bool IsEngaged => _hovered || _pressed;

        public void ClearPointer()
        {
            _hovered = false;
            _pressed = false;
            Apply();
            SnapLift();
        }

        public void SetState(Color rest, Color press, Color textRest, Color textPress)
        {
            RestColor = rest;
            PressColor = press;
            TextRestColor = textRest;
            TextPressColor = textPress;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Apply();
            DriveLift();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            Apply();
            DriveLift();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            Apply();
            DriveLift();
            PressFeedback.Play();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            Apply();
            DriveLift();
        }

        private void DriveLift()
        {
            if (!isActiveAndEnabled)
            {
                SnapLift();
                return;
            }
            if (Mathf.Approximately(_liftT, Raised ? 1f : 0f)) return;
            if (_lift == null) _lift = StartCoroutine(LiftRoutine());
        }

        private IEnumerator LiftRoutine()
        {
            while (true)
            {
                float goal = Raised ? 1f : 0f;
                if (Mathf.Approximately(_liftT, goal)) break;

                _liftT = Mathf.MoveTowards(_liftT, goal, Time.unscaledDeltaTime / LiftDuration);
                ApplyLift();
                yield return null;
            }
            _lift = null;
        }

        private void SnapLift()
        {
            _liftT = Raised ? 1f : 0f;
            ApplyLift();
        }

        private void ApplyLift()
        {
            Transform lifted = Lifted;
            Transform parent = lifted.parent;
            float metresPerUnit = parent != null ? Mathf.Abs(parent.lossyScale.z) : 1f;
            if (metresPerUnit < 1e-6f) metresPerUnit = parent != null ? Mathf.Abs(parent.lossyScale.x) : 1f;
            float rise = metresPerUnit > 1e-6f ? Style.HoverRise / metresPerUnit : 0f;
            Vector3 at = lifted.localPosition;
            at.z = _baseDepth - rise * _liftT;
            lifted.localPosition = at;
        }

        private void OnDisable()
        {
            _lift = null;
            _hovered = false;
            _pressed = false;
            SnapLift();
        }

        private void Apply()
        {
            if (Target != null)
                Target.color = _pressed ? PressColor : RestColor;
            if (TextTarget != null)
                TextTarget.color = _pressed ? TextPressColor : TextRestColor;
        }
    }
}
