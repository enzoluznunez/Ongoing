using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Interactions;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.View
{
    public sealed class ChartGrab : MonoBehaviour
    {
        private const float GripDepth = 0.03f;

        private Grabbable _grabbable;
        private HandGrabInteractable _handGrab;
        private BoxCollider _gripBox;
        private OneGrabTranslateTransformer _slide;
        private TwoGrabTurn _turn;
        private TwoGrabScale _scale;
        private ToolType _mode = ToolType.None;
        private RectTransform _exempt;
        private bool _held;
        private Vector3 _prePos;
        private Quaternion _preRot;
        private Vector3 _preScale;

        public Grabbable Grabbable => _grabbable;
        public bool Armed => _mode != ToolType.None;

        public static ChartGrab Add(GameObject holder)
        {
            ChartGrab grab = holder.AddComponent<ChartGrab>();
            grab._grabbable = holder.AddComponent<Grabbable>();
            grab._slide = holder.AddComponent<OneGrabTranslateTransformer>();
            grab._turn = holder.AddComponent<TwoGrabTurn>();
            grab._scale = holder.AddComponent<TwoGrabScale>();
            grab.BuildGrip();
            grab.Apply();
            return grab;
        }

        public void SetExempt(RectTransform exempt) => _exempt = exempt;

        private void BuildGrip()
        {
            var grip = new GameObject("Grip");
            grip.layer = gameObject.layer;
            grip.transform.SetParent(transform, false);

            Rigidbody body = grip.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            _gripBox = grip.AddComponent<BoxCollider>();

            GrabbingRule pinch = new GrabbingRule(HandFingerFlags.Thumb | HandFingerFlags.Index, GrabbingRule.DefaultPinchRule);
            _handGrab = grip.AddComponent<HandGrabInteractable>();
            _handGrab.InjectAllHandGrabInteractable(GrabTypeFlags.Pinch | GrabTypeFlags.Palm, body, pinch, GrabbingRule.DefaultPalmRule);
            _handGrab.InjectOptionalPointableElement(_grabbable);
        }

        public void FitGrip(RectTransform area)
        {
            if (area == null || _gripBox == null) return;
            var corners = new Vector3[4];
            area.GetWorldCorners(corners);
            Transform grip = _gripBox.transform;
            grip.SetPositionAndRotation((corners[0] + corners[2]) * 0.5f, area.rotation);
            Vector3 scale = grip.lossyScale;
            float width = Vector3.Distance(corners[0], corners[3]);
            float height = Vector3.Distance(corners[0], corners[1]);
            _gripBox.center = Vector3.zero;
            _gripBox.size = new Vector3(width / Mathf.Max(1e-6f, scale.x), height / Mathf.Max(1e-6f, scale.y),
                GripDepth / Mathf.Max(1e-6f, scale.z));
        }

        public bool IsExempt(Vector3 point) =>
            _exempt != null && _exempt.rect.Contains(_exempt.InverseTransformPoint(point));

        public void SetMode(ToolType tool)
        {
            ToolType mode = tool == ToolType.Place ? ToolType.Place
                : tool == ToolType.Scale ? ToolType.Scale : ToolType.None;
            if (mode == _mode) return;
            _mode = mode;
            Apply();
        }

        private void Apply()
        {
            ITransformer one = _mode == ToolType.Place ? _slide : null;
            ITransformer two = _mode == ToolType.Place ? _turn : _mode == ToolType.Scale ? _scale : null;
            _grabbable.InjectOptionalOneGrabTransformer(one);
            _grabbable.InjectOptionalTwoGrabTransformer(two);
            one?.Initialize(_grabbable);
            two?.Initialize(_grabbable);
            if (_handGrab != null) _handGrab.enabled = Armed;
            if (_gripBox != null) _gripBox.enabled = Armed;
        }

        private void LateUpdate()
        {
            bool held = _grabbable != null && _grabbable.SelectingPointsCount > 0;
            if (held == _held) return;
            _held = held;
            Transform t = transform;
            if (held)
            {
                _prePos = t.localPosition;
                _preRot = t.localRotation;
                _preScale = t.localScale;
                return;
            }
            ChartManager charts = ChartManager.Instance;
            if (charts == null) return;
            bool moved = (t.localPosition - _prePos).sqrMagnitude > 1e-8f;
            bool turned = Quaternion.Angle(t.localRotation, _preRot) > 0.01f;
            bool resized = (t.localScale - _preScale).sqrMagnitude > 1e-8f;
            if (resized) charts.NotifyMoveCommitted(t, _prePos, _preRot, _preScale, EditKind.Scale, "Resized the chart");
            else if (moved && turned) charts.NotifyMoveCommitted(t, _prePos, _preRot, _preScale, EditKind.Move, "Moved and turned the chart");
            else if (turned) charts.NotifyMoveCommitted(t, _prePos, _preRot, _preScale, EditKind.Rotate, "Turned the chart");
            else if (moved) charts.NotifyMoveCommitted(t, _prePos, _preRot, _preScale, EditKind.Move, "Moved the chart");
        }
    }
}
