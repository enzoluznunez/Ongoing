using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.UI;

public sealed class HandRay
{
    private const float MaxDistance = 8f;
    private const float NearestUse = 0.12f;
    private const int FirstPointerId = -1000;

    private enum Hold { None, Canvas, Carry }

    private readonly HandRays _owner;
    private readonly IHand _hand;
    private readonly int _pointerId;
    private readonly RayVisual _visual;
    private static readonly List<Selectable> ControlBuffer = new List<Selectable>();

    private PointableCanvas _hoveredCanvas;
    private Vector3 _end;

    private Hold _hold;
    private PointableCanvas _heldCanvas;
    private Grabbable _carried;
    private float _carryDistance;

    private bool _wasPinching;

    public HandRay(HandRays owner, IHand hand, int index)
    {
        _owner = owner;
        _hand = hand;
        _pointerId = FirstPointerId - index;
        _visual = new RayVisual(owner.transform, $"Hand Ray {index}");
    }

    public void Tick()
    {
        if (!_hand.IsTrackedDataValid || !_hand.GetPointerPose(out Pose pose))
        {
            Lose();
            return;
        }

        Vector3 origin = pose.position;
        Vector3 direction = pose.forward;
        bool pinching = _hand.GetIndexFingerIsPinching();
        bool pinchStarted = pinching && !_wasPinching;
        _wasPinching = pinching;

        if (_hold != Hold.None)
        {
            ContinueHold(origin, direction, pinching);
            return;
        }

        FindTarget(origin, direction, out PointableCanvas canvas, out Vector3 canvasPoint,
            out ChartRaycast.Hit chartHit, out bool onChart);

        if (canvas != null)
        {
            HoverCanvas(canvas, canvasPoint);
            _end = canvasPoint;
            if (pinchStarted) PressCanvas(canvas, canvasPoint, Vector3.Distance(origin, canvasPoint));
        }
        else if (onChart)
        {
            StopHoveringCanvas();
            _end = chartHit.point;
            if (pinchStarted) PressChart(chartHit);
        }
        else
        {
            StopHoveringCanvas();
            _visual.Hide();
            return;
        }

        _visual.Show(origin, _end, _hold != Hold.None);
    }

    private void FindTarget(Vector3 origin, Vector3 direction, out PointableCanvas canvas, out Vector3 canvasPoint,
        out ChartRaycast.Hit chartHit, out bool onChart)
    {
        canvas = null;
        canvasPoint = default;
        float nearest = MaxDistance;
        IReadOnlyList<PointableCanvas> canvases = _owner.Canvases;
        for (int i = 0; i < canvases.Count; i++)
        {
            if (!TryHitCanvas(canvases[i], origin, direction, out float distance, out Vector3 point)) continue;
            if (distance >= nearest) continue;
            nearest = distance;
            canvas = canvases[i];
            canvasPoint = point;
        }

        chartHit = default;
        onChart = false;
        ManageTools tools = Scene.Tools;
        ToolType tool = tools != null ? tools.SelectedTool : ToolType.None;
        if (tool != ToolType.None &&
            ChartRaycast.Cast(origin, direction, nearest, out chartHit) &&
            chartHit.distance >= NearestUse)
        {
            onChart = true;
            canvas = null;
        }

        if (canvas != null && nearest < NearestUse) canvas = null;
    }

    private static bool TryHitCanvas(PointableCanvas pointable, Vector3 origin, Vector3 direction,
        out float distance, out Vector3 point)
    {
        distance = 0f;
        point = default;
        if (pointable == null) return false;

        Canvas canvas = pointable.Canvas;
        if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy) return false;

        RectTransform rect = canvas.transform as RectTransform;
        if (rect == null) return false;

        float facing = Vector3.Dot(direction, rect.forward);
        if (facing <= 1e-4f) return false;

        distance = Vector3.Dot(rect.position - origin, rect.forward) / facing;
        if (distance <= 0f || distance > MaxDistance) return false;

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
            RectTransform rect = control.transform as RectTransform;
            if (rect != null && rect.rect.Contains(rect.InverseTransformPoint(point))) return true;
        }
        return false;
    }

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
        if (IsOnControl(canvas.Canvas, point))
        {
            _hold = Hold.Canvas;
            _heldCanvas = canvas;
            Send(canvas, PointerEventType.Select, point);
            return;
        }

        Grabbable panel = canvas.GetComponentInParent<Grabbable>();
        if (panel == null || !panel.isActiveAndEnabled) return;

        StopHoveringCanvas();
        BeginCarry(panel, point, distance);
    }

    private void BeginCarry(Grabbable grabbable, Vector3 point, float distance)
    {
        _hold = Hold.Carry;
        _carried = grabbable;
        _carryDistance = distance;
        Send(grabbable, PointerEventType.Hover, point);
        Send(grabbable, PointerEventType.Select, point);
    }

    private void PressChart(ChartRaycast.Hit hit)
    {
        Grabbable grip = hit.view.Grip;
        if (grip != null && grip.isActiveAndEnabled) BeginCarry(grip, hit.point, hit.distance);
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
            _hold = Hold.None;
            _heldCanvas = null;
            _hoveredCanvas = null;
            return;
        }

        Plane plane = new Plane(canvas.transform.forward, canvas.transform.position);
        Ray ray = new Ray(origin, direction);
        if (plane.Raycast(ray, out float distance) && distance <= MaxDistance)
        {
            _end = ray.GetPoint(distance);
            Send(_heldCanvas, PointerEventType.Move, _end);
        }
        if (pinching) return;

        Send(_heldCanvas, PointerEventType.Unselect, _end);
        _hold = Hold.None;
        _heldCanvas = null;
    }

    private void ContinueCarry(Vector3 origin, Vector3 direction, bool pinching)
    {
        if (_carried == null || !_carried.isActiveAndEnabled)
        {
            _hold = Hold.None;
            _carried = null;
            return;
        }

        _end = origin + direction * _carryDistance;
        Send(_carried, PointerEventType.Move, _end);
        if (pinching) return;
        EndGrab();
    }

    private void EndGrab()
    {
        Send(_carried, PointerEventType.Unselect, _end);
        Send(_carried, PointerEventType.Unhover, _end);
        _carried = null;
        _hold = Hold.None;
    }

    public void Lose()
    {
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

        _hold = Hold.None;
        _heldCanvas = null;
        _carried = null;
        StopHoveringCanvas();
    }

    private void Send(PointableElement target, PointerEventType type, Vector3 point) =>
        target.ProcessPointerEvent(new PointerEvent(_pointerId, type, new Pose(point, Quaternion.identity)));
}
