using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.View;
using AccountingImmersiveAnalytics.UI.Charts.View;
using AccountingImmersiveAnalytics.UI.Interactions;
using AccountingImmersiveAnalytics.UI.Panels;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.Hands
{
    public sealed class HandRay
    {
        private const float MaxDistance = 8f;
        private const float NearestUse = 0.12f;
        private const int FirstPointerId = -1000;

        private enum Hold { None, Canvas, Carry }

        private readonly HandRays _owner;
        private readonly IHand _hand;
        private readonly PokeInteractor _poke;
        private readonly int _pointerId;
        private readonly RayVisual _visual;
        private static readonly List<Selectable> ControlBuffer = new List<Selectable>();
        private static readonly List<RangeSlider> SliderBuffer = new List<RangeSlider>();
        private static readonly List<ScrollRect> ScrollBuffer = new List<ScrollRect>();

        private PointableCanvas _hoveredCanvas;
        private Vector3 _end;

        private Hold _hold;
        private PointableCanvas _heldCanvas;
        private Grabbable _carried;
        private float _carryDistance;
        private Quaternion _aim = Quaternion.identity;

        private bool _wasPinching;

        public bool HasMark { get; private set; }
        public ChartHit Mark { get; private set; }
        public bool PinchedMark { get; private set; }
        public bool PointsAtCanvas => _hoveredCanvas != null || _hold != Hold.None;
        public Vector3 CanvasPoint => _end;

        public HandRay(HandRays owner, Fingertips.Source source, int index)
        {
            _owner = owner;
            _hand = source.hand;
            _poke = source.poke;
            _pointerId = FirstPointerId - index;
            _visual = new RayVisual(owner.transform, $"Hand Ray {index}");
        }

        public void Tick()
        {
            HasMark = false;
            PinchedMark = false;

            if (!_hand.IsTrackedDataValid || !_hand.GetPointerPose(out Pose pose))
            {
                Lose();
                return;
            }

            Vector3 origin = pose.position;
            Vector3 direction = pose.forward;
            _aim = pose.rotation;
            bool pinching = _hand.GetIndexFingerIsPinching();
            bool pinchStarted = pinching && !_wasPinching;
            _wasPinching = pinching;

            if (_hold != Hold.None)
            {
                ContinueHold(origin, direction, pinching);
                return;
            }

            if (TryFingerMark(out ChartHit fingerHit))
            {
                SetMark(fingerHit, pinchStarted);
                StopHoveringCanvas();
                _visual.Hide();
                return;
            }

            FindTarget(origin, direction, out PointableCanvas canvas, out Vector3 canvasPoint);

            if (canvas == null)
            {
                StopHoveringCanvas();
                _visual.Hide();
                return;
            }

            HoverCanvas(canvas, canvasPoint);
            _end = canvasPoint;
            if (!Scene.ToolSelected && ChartHits.AtPoint(canvasPoint, out ChartHit hit)) SetMark(hit, pinchStarted);
            if (pinchStarted) PressCanvas(canvas, canvasPoint, Vector3.Distance(origin, canvasPoint));

            _visual.Show(origin, _end, _hold != Hold.None);
        }

        private bool TryFingerMark(out ChartHit hit)
        {
            hit = default;
            if (Scene.ToolSelected || _poke == null || !_poke.isActiveAndEnabled) return false;
            if (_poke.State != InteractorState.Normal) return false;
            return ChartHits.Near(_poke.Origin, PokeRule.HoverDepth, out hit);
        }

        private void SetMark(ChartHit hit, bool pinched)
        {
            HasMark = true;
            Mark = hit;
            PinchedMark = pinched;
        }

        private void FindTarget(Vector3 origin, Vector3 direction, out PointableCanvas canvas, out Vector3 canvasPoint)
        {
            canvas = null;
            canvasPoint = default;
            float nearest = MaxDistance;
            IReadOnlyList<PointableCanvas> canvases = _owner.Canvases;
            for (int i = 0; i < canvases.Count; i++)
            {
                if (!TryHitCanvas(canvases[i], origin, direction, nearest, out float distance, out Vector3 point)) continue;
                nearest = distance;
                canvas = canvases[i];
                canvasPoint = point;
            }
            if (canvas != null && nearest < NearestUse) canvas = null;
        }

        private static bool TryHitCanvas(PointableCanvas pointable, Vector3 origin, Vector3 direction, float nearest,
            out float distance, out Vector3 point)
        {
            distance = 0f;
            point = default;
            if (pointable == null) return false;

            Canvas canvas = pointable.Canvas;
            if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy) return false;

            RectTransform rect = canvas.transform as RectTransform;
            if (rect == null) return false;

            Vector3 forward = rect.forward;
            float facing = Vector3.Dot(direction, forward);
            if (facing <= 1e-4f) return false;

            distance = Vector3.Dot(rect.position - origin, forward) / facing;
            if (distance <= 0f || distance >= nearest) return false;

            point = origin + direction * distance;
            Vector2 local = rect.InverseTransformPoint(point);
            Rect area = rect.rect;
            if (area.Contains(local)) return true;

            Rect reachOfDropdowns = new Rect(area.x, area.y - area.height, area.width, area.height * 2f);
            return reachOfDropdowns.Contains(local) && IsOnControl(canvas, point);
        }

        private static bool IsOnControl(Canvas canvas, Vector3 point)
        {
            canvas.GetComponentsInChildren(false, ControlBuffer);
            for (int i = 0; i < ControlBuffer.Count; i++)
            {
                Selectable control = ControlBuffer[i];
                if (!control.IsInteractable()) continue;
                if (Contains(control.transform as RectTransform, point)) return true;
            }
            return false;
        }

        private static bool IsOnSlider(Canvas canvas, Vector3 point)
        {
            canvas.GetComponentsInChildren(false, SliderBuffer);
            for (int i = 0; i < SliderBuffer.Count; i++)
                if (Contains(SliderBuffer[i].transform as RectTransform, point)) return true;
            return false;
        }

        private static bool IsOnScroll(Canvas canvas, Vector3 point)
        {
            canvas.GetComponentsInChildren(false, ScrollBuffer);
            for (int i = 0; i < ScrollBuffer.Count; i++)
            {
                ScrollRect scroll = ScrollBuffer[i];
                if (!scroll.isActiveAndEnabled) continue;
                RectTransform rect = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
                if (Contains(rect, point)) return true;
            }
            return false;
        }

        private static bool Contains(RectTransform rect, Vector3 point) =>
            rect != null && rect.rect.Contains(rect.InverseTransformPoint(point));

        private void HoverCanvas(PointableCanvas canvas, Vector3 point)
        {
            if (_hoveredCanvas != canvas)
            {
                StopHoveringCanvas();
                _hoveredCanvas = canvas;
                Send(canvas, PointerEventType.Hover, point);
                return;
            }
            Send(canvas, PointerEventType.Move, point);
        }

        private void StopHoveringCanvas()
        {
            if (_hoveredCanvas == null) return;
            Send(_hoveredCanvas, PointerEventType.Unhover, _end);
            _hoveredCanvas = null;
        }

        private void PressCanvas(PointableCanvas canvas, Vector3 point, float distance)
        {
            ChartGrab chart = canvas.GetComponentInParent<ChartGrab>();
            Panel owner = chart == null ? canvas.GetComponentInParent<Panel>() : null;
            bool armed = owner != null ? owner.GrabArmed : chart != null && chart.Armed;
            bool exempt = owner != null ? owner.IsPlacementIcon(point) : chart != null && chart.IsExempt(point);
            bool takes = IsOnControl(canvas.Canvas, point) || IsOnSlider(canvas.Canvas, point) || IsOnScroll(canvas.Canvas, point);
            if (takes && (!armed || exempt))
            {
                _hold = Hold.Canvas;
                _heldCanvas = canvas;
                Send(canvas, PointerEventType.Select, point);
                return;
            }

            if (!armed) return;
            Grabbable target = chart != null ? chart.Grabbable : canvas.GetComponentInParent<Grabbable>();
            if (target == null || !target.isActiveAndEnabled) return;

            StopHoveringCanvas();
            BeginCarry(target, point, distance);
        }

        private void BeginCarry(Grabbable grabbable, Vector3 point, float distance)
        {
            _hold = Hold.Carry;
            _carried = grabbable;
            _carryDistance = distance;
            Send(grabbable, PointerEventType.Hover, point);
            Send(grabbable, PointerEventType.Select, point);
        }

        private void ContinueHold(Vector3 origin, Vector3 direction, bool pinching)
        {
            switch (_hold)
            {
                case Hold.Canvas:
                    ContinueCanvas(origin, direction, pinching);
                    break;
                case Hold.Carry:
                    ContinueCarry(origin, direction, pinching);
                    break;
            }

            if (_hold == Hold.None) _visual.Hide();
            else _visual.Show(origin, _end, true);
        }

        private void ContinueCanvas(Vector3 origin, Vector3 direction, bool pinching)
        {
            Canvas canvas = _heldCanvas != null ? _heldCanvas.Canvas : null;
            if (canvas == null || !canvas.enabled)
            {
                if (_heldCanvas != null) Send(_heldCanvas, PointerEventType.Cancel, _end);
                _hoveredCanvas = null;
                EndHold();
                return;
            }

            Transform frame = canvas.transform;
            Plane plane = new Plane(frame.forward, frame.position);
            Ray ray = new Ray(origin, direction);
            if (plane.Raycast(ray, out float distance) && distance <= MaxDistance)
            {
                _end = ray.GetPoint(distance);
                Send(_heldCanvas, PointerEventType.Move, _end);
            }
            if (pinching) return;

            Send(_heldCanvas, PointerEventType.Unselect, _end);
            EndHold();
        }

        private void ContinueCarry(Vector3 origin, Vector3 direction, bool pinching)
        {
            if (_carried == null || !_carried.isActiveAndEnabled)
            {
                EndHold();
                return;
            }

            _end = origin + direction * _carryDistance;
            Send(_carried, PointerEventType.Move, _end);
            if (pinching) return;
            Send(_carried, PointerEventType.Unselect, _end);
            Send(_carried, PointerEventType.Unhover, _end);
            EndHold();
        }

        private void EndHold()
        {
            _hold = Hold.None;
            _heldCanvas = null;
            _carried = null;
        }

        public void Lose()
        {
            HasMark = false;
            PinchedMark = false;
            _wasPinching = false;
            _visual.Hide();

            switch (_hold)
            {
                case Hold.Canvas:
                    if (_heldCanvas != null) Send(_heldCanvas, PointerEventType.Cancel, _end);
                    _hoveredCanvas = null;
                    break;
                case Hold.Carry:
                    if (_carried != null) Send(_carried, PointerEventType.Cancel, _end);
                    break;
            }

            EndHold();
            StopHoveringCanvas();
        }

        private void Send(PointableElement target, PointerEventType type, Vector3 point) =>
            target.ProcessPointerEvent(new PointerEvent(_pointerId, type,
                new Pose(point, target is Grabbable ? _aim : Quaternion.identity)));
    }
}
