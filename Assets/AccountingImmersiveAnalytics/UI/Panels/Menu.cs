using System;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant;
using AccountingImmersiveAnalytics.Assistant.Session;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Charts.Elements;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public enum AssistantCause { User, Agent, Failure }

    public class Menu : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        public GeminiClient geminiClient;

        public float yOffsetFromHand = 0.03f;

        [Header("Pose smoothing")]
        [Tooltip("Lowest position smoothing frequency in Hz; lower removes more shimmer at rest.")]
        public float posMinCutoff = 1f;
        [Tooltip("How quickly position smoothing releases as the hand speeds up; raise if the Menu trails fast motion.")]
        public float posBeta = 0.02f;
        [Tooltip("Lowest rotation smoothing frequency in Hz; lower removes more shimmer at rest.")]
        public float rotMinCutoff = 1f;
        [Tooltip("How quickly rotation smoothing releases as the hand speeds up.")]
        public float rotBeta = 0.05f;
        [Tooltip("Smoothing multiplier while hand tracking confidence is low; lower holds steadier.")]
        [Range(0.05f, 1f)] public float lowConfidenceHardening = 0.25f;

        private const float HandPlacement = 0.6f;
        private const HandJointId AlongJoint = HandJointId.HandMiddle1;
        private const HandJointId IndexJoint = HandJointId.HandIndex1;
        private const HandJointId PinkyJoint = HandJointId.HandPinky1;

        private const float TrackingGraceSeconds = 0.25f;
        private const float CalibrationRate = 2f;


        private IHand _ihand;
        private ButtonFactory.Handle _documentsBtn;
        private ButtonFactory.Handle _assistantBtn;
        private Image _assistantIcon;
        private OneText.UGUI.OneTextLabel _assistantText;
        private bool _assistantOn;
        private int _assistantDots = -1;

        private Canvas _canvas;
        private Transform _canvasTransform;
        private bool _documentsPanelOpen;
        private int _documentsDots = -1;


        private Vector3 _posePosition;
        private Quaternion _poseRotation = Quaternion.identity;
        private bool _hasPose;
        private float _lostAt = -1f;

        private Vector3 _anchorOffset;
        private Quaternion _anchorRotation = Quaternion.identity;
        private bool _calibrated;

        private readonly OneEuroPoseFilter _poseFilter = new OneEuroPoseFilter();

        private void Awake()
        {
            _ihand = _hand as IHand;
            Texts.Refreshed += LayOutButtons;

            _canvas = GetComponentInChildren<Canvas>(true);
            if (_canvas != null)
            {
                _canvasTransform = _canvas.transform;
                _canvas.sortingOrder = Style.SortHandUI;
            }

            BindButtons();

            if (_canvas != null)
            {
                _pokes = _canvas.GetComponentsInChildren<PokeInteractable>(true);
                ShowCanvas(false);
            }
        }

        private void BindButtons()
        {
            _documentsBtn = BindPanelButton("Documents_Btn", "panel.documents", OnDocumentsClicked);
            BindAssistantButton();
            HideUnboundButtons();
            LayOutButtons();
        }

        private void BindAssistantButton()
        {
            Transform t = FindTransform.FindDeep(transform, "Assistant_Btn");
            if (t == null) return;

            _assistantBtn = ButtonFactory.Adopt(t.gameObject);
            _assistantText = _assistantBtn.Text;
            _assistantBtn.Text = null;
            ButtonFactory.SetBorderless(_assistantBtn);
            _assistantIcon = AddIcon(t, "Assistant");
            ShowBack(_assistantBtn);
            _assistantBtn.Highlight.SetTextTarget(_assistantIcon);
            ButtonFactory.SetSelected(_assistantBtn, false);
            if (_assistantText != null)
            {
                _assistantText.AutoSize = false;
                _assistantText.gameObject.SetActive(false);
            }
            _assistantBtn.Button.onClick.AddListener(OnAssistantClicked);
        }

        private void OnAssistantClicked() => SetGeminiActive(!IsGeminiActive, AssistantCause.User);

        private void SyncAssistantButton()
        {
            if (_assistantBtn == null) return;

            bool on = IsGeminiActive;
            bool connecting = on && Gemini.Status != GeminiStatus.Live;
            int dots = connecting ? Style.Dots : -1;

            if (on != _assistantOn)
            {
                _assistantOn = on;
                ButtonFactory.SetSelected(_assistantBtn, on);
            }
            if (dots == _assistantDots) return;
            _assistantDots = dots;

            _assistantIcon.enabled = !connecting;
            if (_assistantText == null) return;
            _assistantText.gameObject.SetActive(connecting);
            if (!connecting) return;
            _assistantText.color = Style.Card;
            Texts.Bind(_assistantText, () => Style.Counting(Strings.Get("assistant.connecting")));
        }


        private void OnDestroy()
        {
            Texts.Refreshed -= LayOutButtons;
        }

        private void HideUnboundButtons()
        {
            RectTransform group = FindTransform.FindDeep(transform, "Buttons") as RectTransform;
            if (group == null) return;

            GameObject documents = _documentsBtn != null ? _documentsBtn.Root : null;
            GameObject assistant = _assistantBtn != null ? _assistantBtn.Root : null;
            for (int i = 0; i < group.childCount; i++)
            {
                GameObject child = group.GetChild(i).gameObject;
                if (child != documents && child != assistant && child.activeSelf) child.SetActive(false);
            }
        }

        private void LayOutButtons()
        {
            RectTransform group = FindTransform.FindDeep(transform, "Buttons") as RectTransform;
            if (group == null) return;

            VerticalLayoutGroup stack = group.GetComponent<VerticalLayoutGroup>();
            if (stack != null) stack.enabled = false;
            group.pivot = new Vector2(0.5f, 0.5f);
            LayoutTools.Stretch(group);

            float width = MenuWidth();
            float lower = _assistantBtn != null ? -(ButtonSize + ButtonGap) : 0f;
            PlaceButton(_documentsBtn, 0f, lower, width);
            PlaceButton(_assistantBtn, 0f, 0f, width);

            RectTransform canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
            if (canvasRect == null) return;
            canvasRect.sizeDelta = new Vector2(width, ButtonSize - lower);
            LayoutTools.FitPokeBounds(canvasRect);
        }

        private static float MenuWidth()
        {
            float documents = ChartText.Width(Strings.Get("panel.documents"), Style.MediumText);
            float connecting = ChartText.Width(Strings.Get("assistant.connecting") + MaxDots, Style.MediumText);
            float label = Mathf.Ceil(Mathf.Max(documents, connecting)) + Style.MediumPad * 2f;
            return Mathf.Max(ButtonSize * 2f + ButtonGap, label);
        }

        private const string MaxDots = "...";

        private static void PlaceButton(ButtonFactory.Handle button, float x, float y, float width)
        {
            if (button == null) return;

            RectTransform rt = button.Root.transform as RectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(width, ButtonSize);
            rt.anchoredPosition = new Vector2(x, y);
        }

        private static float ButtonSize => Style.ButtonSize;
        private const float ButtonGap = Style.SmallPad;

        private ButtonFactory.Handle BindPanelButton(string name, string labelKey, UnityEngine.Events.UnityAction onClick)
        {
            Transform t = FindTransform.FindDeep(transform, name);
            if (t == null)
            {
                Debug.LogError($"[Menu] No button named '{name}' under the Menu; that panel cannot be opened from the Menu.");
                return null;
            }

            ButtonFactory.Handle h = ButtonFactory.Adopt(t.gameObject);
            Transform oldIcon = t.Find("Icon");
            if (oldIcon != null) oldIcon.gameObject.SetActive(false);

            ButtonFactory.SetBorderless(h);
            ShowBack(h);
            h.Text.gameObject.SetActive(true);
            ButtonFactory.FixText(h);
            Texts.Bind(h.Text, () => Strings.Get(labelKey));
            ButtonFactory.SetSelected(h, false);
            h.Button.onClick.AddListener(onClick);
            return h;
        }

        private static void ShowBack(ButtonFactory.Handle h)
        {
            Image back = ButtonFactory.AddBack(h);
            if (back == null) return;
            back.gameObject.SetActive(true);
            back.transform.SetAsLastSibling();
        }

        private static Image AddIcon(Transform button, string iconName)
        {
            RectTransform rt = LayoutTools.EnsureChild(button, "Icon");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * Style.IconSize;
            rt.anchoredPosition = Vector2.zero;

            Image icon = LayoutTools.Ensure<Image>(rt.gameObject);
            icon.sprite = AssetLibrary.Icon(iconName);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Panel.ApplyFront(icon);
            return icon;
        }

        private void OnDocumentsClicked()
        {
            DocumentsPanel documents = Scene.Documents;
            if (documents != null) documents.TogglePanel();
        }


        public bool IsGeminiActive => geminiClient != null && geminiClient.Active;


        public void SetGeminiActive(bool active, AssistantCause cause)
        {
            if (geminiClient == null) return;
            if (geminiClient.Active == active) return;
            if (!active && cause != AssistantCause.Agent) AgentTurn.UserTookControl();
            if (!active) Notices.HideNotice();
            geminiClient.SetActive(active);
            ReportActivation(active, cause);
        }

        private void ReportActivation(bool active, AssistantCause cause)
        {
            string what = active
                ? "the assistant is on and listening"
                : "the assistant is off";

            if (cause == AssistantCause.Failure) StateChannel.SetState("assistant", what);
            else StateChannel.RecordState("assistant", what);
        }

        private void SyncPanelButtonStates()
        {
            DocumentsPanel documents = Scene.Documents;
            if (documents == null || _documentsBtn == null) return;
            bool loading = documents.IsLoading;
            bool selected = documents.IsVisible || loading;
            if (selected != _documentsPanelOpen)
            {
                _documentsPanelOpen = selected;
                ButtonFactory.SetSelected(_documentsBtn, selected);
            }
            int dots = loading ? Style.Dots : -1;
            if (dots == _documentsDots) return;
            _documentsDots = dots;
            if (loading) Texts.Bind(_documentsBtn.Text, () => Style.Counting(Strings.Get("loading")));
            else Texts.Bind(_documentsBtn.Text, () => Strings.Get("panel.documents"));
        }

        private void OnEnable()
        {
            _poseFilter.Reset();
            _hasPose = false;
            _lostAt = -1f;

            if (geminiClient == null) return;
            geminiClient.StatusChanged += OnGeminiStatusChanged;
            geminiClient.ContextWarning += OnContextWarning;
            geminiClient.ContextExhausted += OnContextExhausted;
        }

        private void OnDisable()
        {
            if (geminiClient == null) return;
            geminiClient.StatusChanged -= OnGeminiStatusChanged;
            geminiClient.ContextWarning -= OnContextWarning;
            geminiClient.ContextExhausted -= OnContextExhausted;
        }


        private void OnGeminiStatusChanged(GeminiStatus status)
        {
            if (status == GeminiStatus.Failed || status == GeminiStatus.MicDenied)
            {
                bool wasActive = IsGeminiActive;
                SetGeminiActive(false, AssistantCause.Failure);

                if (wasActive)
                    Notices.Show(this, Strings.Original(status == GeminiStatus.MicDenied ? "notice.micDenied" : "notice.disconnected"));
            }
        }

        private void OnContextWarning()
        {
            Notices.Show(this, Strings.Original("notice.nearlyFull.body"));
        }

        private void OnContextExhausted()
        {
            SetGeminiActive(false, AssistantCause.Failure);
            Notices.Show(this, Strings.Original("notice.full.body"));
        }

        private bool Engaged => IsEngaged(_documentsBtn) || IsEngaged(_assistantBtn);

        private static bool IsEngaged(ButtonFactory.Handle h) => h != null && h.Highlight != null && h.Highlight.IsEngaged;

        private void LateUpdate()
        {
            SyncPanelButtonStates();
            SyncAssistantButton();

            if (_ihand == null || _canvasTransform == null) return;

            if (!TryReadHandPose(out Vector3 position, out Quaternion rotation))
            {
                HoldOrHide();
                return;
            }

            _lostAt = -1f;

            if (!_hasPose) _poseFilter.Reset();
            _poseFilter.posMinCutoff = posMinCutoff;
            _poseFilter.posBeta = posBeta;
            _poseFilter.rotMinCutoff = rotMinCutoff;
            _poseFilter.rotBeta = rotBeta;

            float hardening = _ihand.IsHighConfidence ? 1f : lowConfidenceHardening;
            Pose filtered = _poseFilter.Filter(new Pose(position, rotation), Time.unscaledDeltaTime, hardening);

            _posePosition = filtered.position;
            _poseRotation = filtered.rotation;
            _hasPose = true;

            _canvasTransform.SetPositionAndRotation(_posePosition, _poseRotation);
            SetVisible(true);
        }

        private void HoldOrHide()
        {
            if (!_hasPose)
            {
                SetVisible(false);
                return;
            }

            if (_lostAt < 0f) _lostAt = Time.unscaledTime;

            if (Time.unscaledTime - _lostAt < TrackingGraceSeconds || Engaged)
            {
                _canvasTransform.SetPositionAndRotation(_posePosition, _poseRotation);
                SetVisible(true);
                return;
            }

            _hasPose = false;
            SetVisible(false);
        }

        private bool TryReadHandPose(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (!_ihand.IsTrackedDataValid) return false;
            if (!_ihand.GetJointPose(HandJointId.HandWristRoot, out Pose wrist)) return false;

            TryCalibrate(wrist);
            if (!_calibrated) return TryFingerPose(wrist, out position, out rotation);

            rotation = wrist.rotation * _anchorRotation;
            position = wrist.position + wrist.rotation * _anchorOffset;
            return true;
        }

        private void TryCalibrate(Pose wrist)
        {
            if (!_ihand.IsHighConfidence) return;
            if (!_ihand.GetFingerIsHighConfidence(HandFinger.Index) ||
                !_ihand.GetFingerIsHighConfidence(HandFinger.Middle) ||
                !_ihand.GetFingerIsHighConfidence(HandFinger.Pinky)) return;

            if (!TryFingerPose(wrist, out Vector3 position, out Quaternion rotation)) return;

            Quaternion inverse = Quaternion.Inverse(wrist.rotation);
            Vector3 offset = inverse * (position - wrist.position);
            Quaternion twist = inverse * rotation;

            if (!_calibrated)
            {
                _anchorOffset = offset;
                _anchorRotation = twist;
                _calibrated = true;
                return;
            }

            if (Engaged) return;

            float t = Style.Damp(CalibrationRate, Time.unscaledDeltaTime);
            _anchorOffset = Vector3.Lerp(_anchorOffset, offset, t);
            _anchorRotation = Quaternion.Slerp(_anchorRotation, twist, t);
        }

        [ContextMenu("Log Wrist Anchor")]
        private void LogWristAnchor()
        {
            Debug.Log($"[Menu] anchorOffset {_anchorOffset.ToString("F4")} " +
                      $"anchorRotation {_anchorRotation.eulerAngles.ToString("F2")} " +
                      $"(calibrated={_calibrated})");
        }

        private bool TryFingerPose(Pose wrist, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (!_ihand.GetJointPose(AlongJoint, out Pose middle) ||
                !_ihand.GetJointPose(IndexJoint, out Pose index) ||
                !_ihand.GetJointPose(PinkyJoint, out Pose pinky)) return false;

            Vector3 along = middle.position - wrist.position;
            Vector3 across = index.position - pinky.position;
            if (along.sqrMagnitude < 1e-8f || across.sqrMagnitude < 1e-8f) return false;

            along.Normalize();
            across.Normalize();

            Vector3 outward = Vector3.Cross(along, across).normalized;

            Vector3 stackUp = -across;
            stackUp = Vector3.ProjectOnPlane(stackUp, outward).normalized;
            if (stackUp.sqrMagnitude < 1e-8f) return false;

            Vector3 anchorPosition = Vector3.Lerp(wrist.position, middle.position, HandPlacement);
            rotation = Quaternion.LookRotation(-outward, stackUp);
            position = anchorPosition + outward * yOffsetFromHand;
            return true;
        }

        private void SetVisible(bool visible)
        {
            if (_canvas == null || _canvas.enabled == visible) return;
            ShowCanvas(visible);
        }

        private void ShowCanvas(bool visible) => LayoutTools.ShowCanvas(_canvas, _pokes, visible);

        private PokeInteractable[] _pokes = new PokeInteractable[0];
    }
}
