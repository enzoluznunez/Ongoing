using System;
using System.Collections;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public enum MarkStyle { Flat, Shaded }

public struct ChartLook
{
    public MarkStyle Style;
    public float Alpha;

    public static ChartLook Current => new ChartLook { Style = Settings.ChartStyle, Alpha = ChartDisplay.Opacity };
}

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody), typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ChartView : MonoBehaviour
{
    public const float DefaultScale = 1f;

    private static readonly int GrowId = Shader.PropertyToID("_Grow");
    private static readonly int BaseId = Shader.PropertyToID("_BaseY");
    private static readonly int ShadedId = Shader.PropertyToID("_Shaded");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
    private static readonly int ColorMaskId = Shader.PropertyToID("_ColorMask");
    private const int OpaqueQueue = 2000;
    private const int TransparentQueue = 3000;
    private const float SolidAlpha = 0.999f;

    public int chartId = -1;

    private BoxCollider _bounds;
    private Rigidbody _body;
    private Grabbable _grabbable;
    private HandGrabInteractable _handGrab;
    private OneGrabTranslateTransformer _slide;
    private MeshRenderer _renderer;
    private Mesh _mesh;
    private Material _material;
    private Material _depthMaterial;
    private Material[] _opaqueMaterials;
    private Material[] _transparentMaterials;
    private ChartLabels _labels;
    private ChartBackdrop _backdrop;
    private ChartInfoCard _card;
    private ChartGeometry _geometry;

    private Coroutine _motion;
    private Action _motionDone;
    private GrabSwell _swell;
    private bool _shown;
    private bool _grabWanted = true;
    private bool _wasGrabbed;
    private Vector3 _grabPos;
    private Quaternion _grabRot;
    private Vector3 _grabScale;

    public bool Shown => _shown;
    public bool Falling { get; private set; }
    public ChartGeometry Geometry => _geometry;
    public Mesh MarkMesh => _mesh;
    public Bounds LocalBounds => _geometry != null ? _geometry.Bounds : new Bounds();

    public void Init(Material material)
    {
        EnsureComponents();
        if (_material != null) return;
        _material = new Material(material);
        _depthMaterial = new Material(material);
        _depthMaterial.SetFloat(ZWriteId, 1f);
        _depthMaterial.SetFloat(ColorMaskId, 0f);
        _depthMaterial.renderQueue = TransparentQueue - 1;
        _opaqueMaterials = new[] { _material };
        _transparentMaterials = new[] { _depthMaterial, _material };
        SetLook(ChartLook.Current);
        _mesh = new Mesh { name = "ChartMarks" };
        _mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = _mesh;
        _labels = new ChartLabels(transform);
        _backdrop = new ChartBackdrop(transform, material);
        _card = new ChartInfoCard(transform);
        ApplyDisplayParts();
        _renderer.enabled = false;
    }

    public void SetLook(ChartLook look)
    {
        bool solid = look.Alpha >= SolidAlpha;
        _material.SetFloat(ShadedId, look.Style == MarkStyle.Shaded ? 1f : 0f);
        _material.SetFloat(AlphaId, Mathf.Clamp01(look.Alpha));
        _material.SetFloat(SrcBlendId, (float)UnityEngine.Rendering.BlendMode.One);
        _material.SetFloat(DstBlendId, (float)(solid
            ? UnityEngine.Rendering.BlendMode.Zero
            : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        _material.SetFloat(ZWriteId, solid ? 1f : 0f);
        _material.renderQueue = solid ? OpaqueQueue : TransparentQueue;
        _renderer.sharedMaterials = solid ? _opaqueMaterials : _transparentMaterials;
    }

    private void SetBoth(int id, float value)
    {
        _material.SetFloat(id, value);
        _depthMaterial.SetFloat(id, value);
    }

    public void ApplyDisplayParts()
    {
        if (_labels == null) return;
        _backdrop.SetParts(Settings.Backdrop, Settings.BackdropGridlines);
        _backdrop.SetAppearance(ChartDisplay.BackdropStrength, ChartDisplay.Light);
        _labels.SetAppearance(ChartDisplay.LabelScale, ChartDisplay.Light ? Style.Black : Style.White);
        _labels.SetParts(Settings.TickerLabels, Settings.AxisLabels);
        _card.SetParts(Settings.ChartKey, Settings.ChartDescription);
    }

    public void Bind(ChartSpec spec, ChartGeometry geometry)
    {
        _geometry = geometry;
        SetBoth(BaseId, geometry.BaseY);
        _bounds.center = geometry.Bounds.center;
        _bounds.size = geometry.Bounds.size;
        _labels.Rebuild(geometry);
        _backdrop.Rebuild(geometry);
        _card.Rebuild(spec, geometry);
        ApplyDisplayParts();
    }

    private void ApplyDecorationsShown(bool on)
    {
        _backdrop.SetShown(on);
        _card.SetShown(on);
    }

    public void SetShown(bool on)
    {
        _shown = on;
        _renderer.enabled = on;
        _bounds.enabled = on;
        _labels.SetShown(on && !Falling);
        ApplyDecorationsShown(on && !Falling);
        ApplyGrabbable();
    }

    public void PlayGrow(float seconds, Action done = null) => Play(0f, 1f, seconds, false, done);

    public void PlayFall(float seconds, Action done)
    {
        _labels.SetShown(false);
        ApplyDecorationsShown(false);
        SetGrabbable(false);
        Play(1f, 0f, seconds, true, done);
    }

    private void Play(float from, float to, float seconds, bool falling, Action done)
    {
        CompleteMotion();
        Falling = falling;
        _motionDone = done;
        SetBoth(GrowId, from);
        if (seconds <= 0f || !isActiveAndEnabled) { FinishMotion(to); return; }
        _motion = StartCoroutine(MotionRoutine(from, to, seconds));
    }

    private IEnumerator MotionRoutine(float from, float to, float seconds)
    {
        yield return Style.Tween(seconds, k => SetBoth(GrowId, Mathf.Lerp(from, to, k)));
        _motion = null;
        FinishMotion(to);
    }

    public void CompleteMotion()
    {
        if (_motion == null) return;
        StopCoroutine(_motion);
        _motion = null;
        FinishMotion(Falling ? 0f : 1f);
    }

    private void FinishMotion(float value)
    {
        SetBoth(GrowId, value);
        Falling = false;
        Action done = _motionDone;
        _motionDone = null;
        done?.Invoke();
    }

    private void LateUpdate()
    {
        if (_labels == null || !_shown) return;
        _labels.SetShown(!Falling);
        _labels.FaceViewer(Time.unscaledDeltaTime);
        _backdrop.FaceViewer();
        _card.Follow(Time.unscaledDeltaTime);
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
        if (_material != null) Destroy(_material);
        if (_depthMaterial != null) Destroy(_depthMaterial);
        _backdrop?.Destroy();
    }

    public void SetGrabLook(bool on) => _swell.Set(transform, on);

    public void ForgetGrabLook() => _swell.Forget(transform);

    private void EnsureComponents()
    {
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        if (_bounds == null) _bounds = GetComponent<BoxCollider>();
        if (_body == null)
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
        }
        if (_grabbable == null) _grabbable = GetComponent<Grabbable>();
        if (_grabbable != null && _grabbable.Transform == null)
            _grabbable.InjectOptionalTargetTransform(_grabbable.transform);
        if (_handGrab == null) _handGrab = GetComponent<HandGrabInteractable>();
        if (_handGrab != null && _handGrab.gameObject == gameObject) MoveGrabToGrip();
        if (_slide == null) _slide = GetComponent<OneGrabTranslateTransformer>();
    }

    public Grabbable Grip
    {
        get
        {
            EnsureComponents();
            return _grabbable;
        }
    }

    public bool IsGrabbed
    {
        get
        {
            EnsureComponents();
            return _grabbable != null && _grabbable.SelectingPointsCount > 0;
        }
    }

    public bool PollGrabRelease(out Vector3 prePos, out Quaternion preRot, out Vector3 preScale)
    {
        bool grabbed = IsGrabbed;
        if (grabbed && !_wasGrabbed)
        {
            _wasGrabbed = true;
            _grabPos = transform.localPosition;
            _grabRot = transform.localRotation;
            _grabScale = transform.localScale;
        }
        else if (!grabbed) return ForceGrabRelease(out prePos, out preRot, out preScale);

        prePos = Vector3.zero;
        preRot = Quaternion.identity;
        preScale = Vector3.one;
        return false;
    }

    public bool ForceGrabRelease(out Vector3 prePos, out Quaternion preRot, out Vector3 preScale)
    {
        prePos = Vector3.zero;
        preRot = Quaternion.identity;
        preScale = Vector3.one;

        if (!_wasGrabbed) return false;
        _wasGrabbed = false;

        prePos = _grabPos;
        preRot = _grabRot;
        preScale = _grabScale;
        return true;
    }

    public void SetGrabbable(bool on)
    {
        _grabWanted = on;
        ApplyGrabbable();
    }

    private void ApplyGrabbable()
    {
        EnsureComponents();
        bool on = _grabWanted && _shown;
        if (_grabbable != null) _grabbable.enabled = on;
        if (_handGrab != null) _handGrab.enabled = on;
    }

    private void MoveGrabToGrip()
    {
        HandGrabInteractable old = _handGrab;

        var go = new GameObject("Grip");
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        Rigidbody body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        BoxCollider box = go.AddComponent<BoxCollider>();
        if (_bounds != null) _bounds.enabled = false;

        HandGrabInteractable grip = go.AddComponent<HandGrabInteractable>();
        grip.InjectAllHandGrabInteractable(old.SupportedGrabTypes, body, old.PinchGrabRules, old.PalmGrabRules);
        grip.InjectOptionalScoreModifier(old.ScoreModifier);
        grip.InjectOptionalPointableElement(_grabbable);
        grip.enabled = _grabWanted && _shown;

        old.enabled = false;
        Destroy(old);

        _handGrab = grip;
        _bounds = box;
    }

    public void SetOneGrab()
    {
        EnsureComponents();
        if (_grabbable == null) return;

        _grabbable.MaxGrabPoints = 1;
        if (_slide == null) return;
        _grabbable.InjectOptionalOneGrabTransformer(_slide);
        _slide.Initialize(_grabbable);
    }

    public void SetTwoGrab(ITransformer transformer)
    {
        EnsureComponents();
        if (_grabbable == null || transformer == null) return;

        _grabbable.MaxGrabPoints = 2;
        _grabbable.InjectOptionalOneGrabTransformer(null);
        _grabbable.InjectOptionalTwoGrabTransformer(transformer);
        transformer.Initialize(_grabbable);
    }
}
