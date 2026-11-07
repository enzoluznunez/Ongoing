using TMPro;
using UnityEngine;

public class LoadingPhase : MonoBehaviour
{
    private const float GiveUpSeconds = 45f;
    private const float AheadMetres = 1.2f;
    private const float FollowRate = 4f;
    private const float LongestFrame = 0.25f;

    public static bool Done { get; private set; }

    private static LoadingPhase _instance;

    private TextMeshPro _label;
    private float _elapsed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        Done = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create() => Spawn.Once(ref _instance);

    private void Start()
    {
        _label = new GameObject("LoadingLabel").AddComponent<TextMeshPro>();
        _label.transform.SetParent(transform, false);
        Style.ApplyWorldTitle(_label);
        _label.alignment = TextAlignmentOptions.Center;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.color = Style.White;
        _label.rectTransform.sizeDelta = new Vector2(40f, 8f);
        _label.transform.localScale = Vector3.one * (Style.WorldTextScale * 2f);
        _label.text = Strings.Get("loading");
    }

    private void Update()
    {
        if (Done) return;

        _elapsed += Mathf.Min(Time.unscaledDeltaTime, LongestFrame);
        string stage = Stage();
        bool timedOut = _elapsed > GiveUpSeconds;
        if (stage == null || timedOut)
        {
            Done = true;
            _label.renderer.enabled = false;
            float took = _elapsed;
            if (timedOut) Debug.LogWarning($"[LoadingPhase] Gave up waiting after {took:F1} s, at: {stage}");
            else Debug.Log($"[LoadingPhase] Loaded in {took:F1} s.");
            return;
        }

        if (stage != _shownStage)
        {
            _shownStage = stage;
            _label.text = $"{Strings.Get("loading")}\n<size=55%>{stage}</size>";
        }
        FollowHead();
    }

    private string _shownStage;

    private string Stage()
    {
        ManageCharts charts = Scene.Charts;
        if (charts != null && !charts.CatalogReady) return Strings.Get("loading.listing");
        if (charts != null && !charts.FirstChartReady) return Strings.Get("loading.reading");
        if (!GraphicsWarm.Step()) return Strings.Get("loading.graphics");
        return null;
    }

    private void FollowHead()
    {
        Transform cam = CameraRig.MainTransform;
        if (cam == null) return;
        Vector3 forward = CameraRig.FlatForward;
        Vector3 target = cam.position + forward * AheadMetres;
        Quaternion facing = Quaternion.LookRotation(forward);
        Transform t = _label.transform;
        float k = Style.Damp(FollowRate, Time.unscaledDeltaTime);
        bool first = t.position == Vector3.zero;
        t.SetPositionAndRotation(first ? target : Vector3.Lerp(t.position, target, k),
            first ? facing : Quaternion.Slerp(t.rotation, facing, k));
    }
}
