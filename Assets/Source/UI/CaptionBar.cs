using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CaptionBar : MonoBehaviour
{
    private const float Ahead = 0.9f;
    private const float Below = 0.32f;
    private const float FollowRate = 3f;
    private const float HoldSeconds = 4f;
    private const float FadeSeconds = 0.5f;
    private const float Scale = 0.0009f;
    private const float Width = 760f;
    private const int MostCharacters = 150;

    private static CaptionBar _instance;
    private static volatile string _userText;
    private static volatile string _assistantText;

    private Canvas _canvas;
    private CanvasGroup _group;
    private TextMeshProUGUI _text;
    private RectTransform _rect;
    private string _shownUser;
    private string _shownAssistant;
    private float _lastChange = float.NegativeInfinity;
    private bool _placed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _userText = null;
        _assistantText = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create() => Spawn.Once(ref _instance);

    public static void Post(string text, bool user)
    {
        if (user) _userText = text;
        else _assistantText = text;
    }

    private void Start()
    {
        var root = new GameObject("Captions", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.localScale = Vector3.one * Scale;
        _rect = (RectTransform)root.transform;
        _rect.sizeDelta = new Vector2(Width, 100f);

        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = Style.SortNotices;
        _group = root.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;

        Image back = root.AddComponent<Image>();
        RoundedSprite.Apply(back, Style.PanelRadius);
        back.color = new Color(0f, 0f, 0f, 0.82f);
        back.raycastTarget = false;

        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        int pad = (int)Style.PanelInset;
        layout.padding = new RectOffset(pad, pad, pad, pad);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _text = UILabel.Make(root.transform, "Text", Style.White, TextAlignmentOptions.Left, TextOverflowModes.Overflow, wrap: true);
        _text.richText = false;
        _canvas.enabled = false;
    }

    private void LateUpdate()
    {
        if (_canvas == null) return;
        if (!Settings.Captions)
        {
            _canvas.enabled = false;
            return;
        }

        string user = _userText, assistant = _assistantText;
        if (user != _shownUser || assistant != _shownAssistant)
        {
            _shownUser = user;
            _shownAssistant = assistant;
            _lastChange = Time.unscaledTime;
            string line = string.IsNullOrEmpty(assistant) ? user : assistant;
            if (line != null && line.Length > MostCharacters) line = "…" + line.Substring(line.Length - MostCharacters);
            _text.text = line ?? "";
        }

        float idle = Time.unscaledTime - _lastChange;
        float alpha = Mathf.Clamp01(1f - (idle - HoldSeconds) / FadeSeconds);
        _group.alpha = alpha;
        _canvas.enabled = alpha > 0f && !string.IsNullOrEmpty(_text.text);
        if (!_canvas.enabled) { _placed = false; return; }

        Transform cam = CameraRig.MainTransform;
        if (cam == null) return;
        Vector3 forward = CameraRig.FlatForward;
        Vector3 target = cam.position + forward * Ahead + Vector3.down * Below;
        Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
        float k = _placed ? Style.Damp(FollowRate, Time.unscaledDeltaTime) : 1f;
        _rect.SetPositionAndRotation(Vector3.Lerp(_rect.position, target, k), Quaternion.Slerp(_rect.rotation, facing, k));
        _placed = true;
    }
}
