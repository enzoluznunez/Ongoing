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

    private enum Hold { None, Canvas, Carry, Slide, Profile, Spent }

    private readonly HandRays _owner;
    private readonly IHand _hand;
    private readonly int _pointerId;
    private readonly RayVisual _visual;
    private static readonly List<Selectable> ControlBuffer = new List<Selectable>();

    private PointableCanvas _hoveredCanvas;
    private bool _hoveringSheet;
    private Vector3 _end;

    private Hold _hold;
    private PointableCanvas _heldCanvas;
    private Grabbable _carried;
    private float _carryDistance;
    private Plane _slidePlane;
    private Vector3 _lastSlidePoint;
    private CreateCube _pressedCube;
    private Vector3 _pressedTip;

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
            out SheetRaycast.RayHit sheetHit, out bool onSheet);

        if (canvas != null)
        {
            StopHoveringSheet();
            HoverCanvas(canvas, canvasPoint);
            _end = canvasPoint;
            if (pinchStarted) PressCanvas(canvas, canvasPoint, Vector3.Distance(origin, canvasPoint));
        }
        else if (onSheet)
        {
            StopHoveringCanvas();
            HoverSheet(sheetHit, origin);
            _end = sheetHit.point;
            if (pinchStarted) PressSheet(sheetHit, origin);
        }
        else
        {
            StopHoveringCanvas();
            StopHoveringSheet();
            _visual.Hide();
            return;
        }

        _visual.Show(origin, _end, _hold != Hold.None);
    }

    private void FindTarget(Vector3 origin, Vector3 direction, out PointableCanvas canvas, out Vector3 canvasPoint,
        out SheetRaycast.RayHit sheetHit, out bool onSheet)
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

        sheetHit = default;
        onSheet = false;
        ManageTools tools = Scene.Tools;
        ToolType tool = tools != null ? tools.SelectedTool : ToolType.None;
        if (tool != ToolType.None &&
            SheetRaycast.Cast(origin, direction, nearest, tool == ToolType.Filter, out sheetHit) &&
            sheetHit.distance >= NearestUse)
        {
            onSheet = true;
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

    private void HoverSheet(SheetRaycast.RayHit hit, Vector3 origin)
    {
        _hoveringSheet = true;
        ToolType tool = Scene.Tools.SelectedTool;

        if (tool == ToolType.Profile)
        {
            if (!TakeSheetEvents() || hit.cube == null) return;
            _owner.Hub.Hover(ReadingOf(hit.cube, hit.point, hit.point, origin));
            return;
        }

        if (tool != ToolType.Filter && tool != ToolType.Sort) return;

        bool rows = hit.PicksRow;
        int line = hit.sheet.LineAtSlot(!rows, hit.SlotOfPick);
        if (line < 0) return;
        if (tool == ToolType.Filter) Scene.Filter?.TintFromRay(hit.sheet, !rows, line);
        else Scene.Sort?.TintFromRay(hit.sheet, !rows, line);
    }

    private void StopHoveringSheet()
    {
        if (!_hoveringSheet) return;
        _hoveringSheet = false;
        ReleaseSheetEvents();
    }

    private bool TakeSheetEvents()
    {
        if (HandRays.SheetEventsOwner == this) return true;
        if (HandRays.SheetEventsOwner != null || _owner.FingertipOnSheet) return false;
        HandRays.SheetEventsOwner = this;
        return true;
    }

    private void ReleaseSheetEvents()
    {
        if (HandRays.SheetEventsOwner != this) return;
        HandRays.SheetEventsOwner = null;
        _owner.Hub.Cleared();
    }

    private void PressSheet(SheetRaycast.RayHit hit, Vector3 origin)
    {
        switch (Scene.Tools.SelectedTool)
        {
            case ToolType.Move:
            case ToolType.Rotate:
            case ToolType.Scale:
                Grabbable grip = hit.sheet.Grip;
                if (grip != null && grip.isActiveAndEnabled) BeginCarry(grip, hit.point, hit.distance);
                break;
            case ToolType.Profile:
                PressProfile(hit, origin);
                break;
            case ToolType.Sort:
                PressSort(hit);
                break;
            case ToolType.Filter:
                Scene.Filter?.PokeFromRay(hit.PicksRow, hit.SlotOfPick);
                _hold = Hold.Spent;
                break;
        }
    }

    private void PressProfile(SheetRaycast.RayHit hit, Vector3 origin)
    {
        if (hit.cube == null || !TakeSheetEvents()) return;

        _hold = Hold.Profile;
        _pressedCube = hit.cube;
        _pressedTip = hit.point;
        _slidePlane = new Plane(hit.sheet.transform.up, hit.point);
        _owner.Hub.Select(ReadingOf(hit.cube, hit.point, hit.point, origin));
    }

    private void PressSort(SheetRaycast.RayHit hit)
    {
        bool columns = !hit.PicksRow;
        int visibleLine = hit.sheet.LineAtSlot(columns, hit.SlotOfPick);
        if (visibleLine < 0) return;
        int block = visibleLine / hit.sheet.GroupSizeOn(columns);
        Grabbable line = Scene.Sort != null ? Scene.Sort.GripOfLine(columns, block) : null;
        if (line == null || !line.isActiveAndEnabled) return;

        _hold = Hold.Slide;
        _carried = line;
        _slidePlane = new Plane(hit.sheet.transform.up, hit.point);
        _lastSlidePoint = hit.point;
        Send(line, PointerEventType.Hover, hit.point);
        Send(line, PointerEventType.Select, hit.point);
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
            case Hold.Slide:
                ContinueSlide(origin, direction, pinching);
                break;
            case Hold.Profile:
                ContinueProfile(origin, direction, pinching);
                break;
            case Hold.Spent:
                if (!pinching) _hold = Hold.None;
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

    private void ContinueSlide(Vector3 origin, Vector3 direction, bool pinching)
    {
        if (_carried == null || !_carried.isActiveAndEnabled)
        {
            _hold = Hold.None;
            _carried = null;
            return;
        }

        Ray ray = new Ray(origin, direction);
        if (_slidePlane.Raycast(ray, out float distance) && distance <= MaxDistance)
            _lastSlidePoint = ray.GetPoint(distance);
        _end = _lastSlidePoint;
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

    private void ContinueProfile(Vector3 origin, Vector3 direction, bool pinching)
    {
        if (_pressedCube == null)
        {
            _hold = Hold.None;
            _owner.Hub.Cleared();
            return;
        }

        Ray ray = new Ray(origin, direction);
        if (_slidePlane.Raycast(ray, out float distance) && distance <= MaxDistance)
            _pressedTip = ray.GetPoint(distance);

        if (SheetRaycast.Cast(origin, direction, MaxDistance, false, out SheetRaycast.RayHit hit) && hit.cube != null)
            _pressedCube = hit.cube;

        _end = _pressedTip;
        ReadSheets.Reading reading = ReadingOf(_pressedCube, _pressedTip, _pressedTip, origin);

        if (pinching)
        {
            _owner.Hub.Hover(reading);
            return;
        }

        _owner.Hub.Release(reading);
        _hold = Hold.None;
        _pressedCube = null;
    }

    private static ReadSheets.Reading ReadingOf(CreateCube cube, Vector3 point, Vector3 tip, Vector3 origin)
    {
        ReadSheets.Reading reading = ReadSheets.Describe(cube, point);
        reading.tip = tip;
        reading.wrist = origin;
        return reading;
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
            case Hold.Slide:
                if (_carried != null) Send(_carried, PointerEventType.Cancel, _end);
                break;
        }

        _hold = Hold.None;
        _heldCanvas = null;
        _carried = null;
        _pressedCube = null;
        StopHoveringCanvas();
        _hoveringSheet = true;
        StopHoveringSheet();
    }

    private void Send(PointableElement target, PointerEventType type, Vector3 point) =>
        target.ProcessPointerEvent(new PointerEvent(_pointerId, type, new Pose(point, Quaternion.identity)));
}
