using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

public static class SlideGrab
{
    public static (Grabbable grabbable, HandGrabInteractable handGrab) Attach(GameObject go, Rigidbody body,
        OneGrabTranslateTransformer.OneGrabTranslateConstraints limits, GrabTypeFlags grabTypes)
    {
        body.isKinematic = true;
        body.useGravity = false;

        OneGrabTranslateTransformer slide = go.AddComponent<OneGrabTranslateTransformer>();
        slide.InjectOptionalConstraints(limits);

        Grabbable grabbable = go.AddComponent<Grabbable>();
        grabbable.InjectOptionalTargetTransform(go.transform);
        grabbable.InjectOptionalRigidbody(body);
        grabbable.InjectOptionalThrowWhenUnselected(false);
        grabbable.InjectOptionalKinematicWhileSelected(true);
        grabbable.MaxGrabPoints = 1;
        grabbable.InjectOptionalOneGrabTransformer(slide);

        GrabbingRule pinch = new GrabbingRule(
            HandFingerFlags.Thumb | HandFingerFlags.Index, GrabbingRule.DefaultPinchRule);

        HandGrabInteractable handGrab = go.AddComponent<HandGrabInteractable>();
        handGrab.InjectAllHandGrabInteractable(grabTypes, body, pinch, GrabbingRule.DefaultPalmRule);
        handGrab.InjectOptionalPointableElement(grabbable);
        return (grabbable, handGrab);
    }

    public static FloatConstraint Pin(float value) => new FloatConstraint { Constrain = true, Value = value };
}
