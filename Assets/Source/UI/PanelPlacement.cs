using System;
using System.Collections;
using Oculus.Interaction;
using UnityEngine;

public static class PanelPlacement
{
    public const float MinReach = 0.35f;
    public const float MaxReach = 2f;
    public const float FlipMargin = 15f;

    public static Vector3 Reachable(Vector3 target, out bool limited)
    {
        limited = false;

        Transform cam = CameraRig.MainTransform;
        if (cam == null) return target;

        Vector3 delta = target - cam.position;
        float distance = delta.magnitude;
        if (distance < 1e-4f) return target;

        float clamped = Mathf.Clamp(distance, MinReach, MaxReach);
        if (Mathf.Approximately(clamped, distance)) return target;

        limited = true;
        return cam.position + delta / distance * clamped;
    }

    public static bool ShouldFlip(Vector3 position, Vector3 forward, Vector3 viewer)
    {
        Vector3 toViewer = viewer - position;
        toViewer.y = 0f;
        if (toViewer.sqrMagnitude < 1e-6f) return false;

        Vector3 face = forward;
        face.y = 0f;
        if (face.sqrMagnitude < 1e-6f) return false;

        return Vector3.Angle(-face, toViewer) > 90f + FlipMargin;
    }

    public const float FlipSeconds = 0.35f;

    public static TwoGrabRotateTransformer FreeSpin(GameObject go)
    {
        TwoGrabRotateTransformer spin = go.GetComponent<TwoGrabRotateTransformer>();
        if (spin != null) return spin;

        spin = go.AddComponent<TwoGrabRotateTransformer>();
        spin.InjectOptionalConstraints(new TwoGrabRotateTransformer.TwoGrabRotateConstraints
        {
            MinAngle = new FloatConstraint(),
            MaxAngle = new FloatConstraint()
        });
        return spin;
    }

    public static TwoGrabRotateTransformer UprightSpin(GameObject go, Transform pivot)
    {
        TwoGrabRotateTransformer spin = FreeSpin(go);
        spin.InjectOptionalPivotTransform(pivot);
        spin.InjectOptionalRotationAxis(TwoGrabRotateTransformer.Axis.Up);
        return spin;
    }

    public static IEnumerator Flip(Transform t, Vector3 pivot, Func<bool> stop)
    {
        Quaternion from = t.rotation;
        Quaternion to = Quaternion.AngleAxis(180f, Vector3.up) * from;
        Quaternion half = to * Quaternion.Inverse(from);
        Vector3 offset = t.position - pivot;
        Vector3 wrotePos = t.localPosition;
        Quaternion wroteRot = t.localRotation;

        if (!Style.Animate)
        {
            t.SetPositionAndRotation(pivot + half * offset, to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < FlipSeconds)
        {
            yield return null;
            if (stop() || Moved(t, wrotePos, wroteRot)) yield break;
            elapsed += Time.unscaledDeltaTime;
            Quaternion turn = Quaternion.Slerp(Quaternion.identity, half, Mathf.Clamp01(elapsed / FlipSeconds));
            t.SetPositionAndRotation(pivot + turn * offset, turn * from);
            wrotePos = t.localPosition;
            wroteRot = t.localRotation;
        }
    }

    private static bool Moved(Transform t, Vector3 localPos, Quaternion localRot) =>
        (t.localPosition - localPos).sqrMagnitude > 1e-8f || Quaternion.Angle(t.localRotation, localRot) > 0.01f;
}

public struct GrabSwell
{
    private bool _on;
    private Vector3 _rest;

    public bool On => _on;

    public Vector3 Rest(Transform t) => _on ? _rest : t.localScale;

    public void Set(Transform t, bool held)
    {
        if (held == _on) return;
        if (held)
        {
            _rest = t.localScale;
            t.localScale = _rest * Style.EngageScale;
        }
        else t.localScale = _rest;
        _on = held;
    }

    public void Forget(Transform t)
    {
        _on = false;
        _rest = t.localScale;
    }
}
