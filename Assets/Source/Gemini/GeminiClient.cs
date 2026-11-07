using System;
using System.Threading.Tasks;
using UnityEngine;

public enum InitPolicy { OnFirstConnect, AtLaunch }

public enum ConnectPolicy { OnFirstUse, OnIntent }

public class GeminiClient : MonoBehaviour {

    [Tooltip("Google Search grounding, always on. It needs a paid-tier (billing-enabled) Gemini API key: on the free tier the session is rejected and the assistant turns itself off.")]
    public bool enableWebSearch = true;

    [Tooltip("When the Gemini SDK, mic permission and audio are initialised. OnFirstConnect defers all of it until the assistant is first needed.")]
    public InitPolicy initPolicy = InitPolicy.AtLaunch;

    [Tooltip("When a session handshake is opened. OnIntent connects when the tool panel opens; OnFirstUse waits until the assistant is switched on.")]
    public ConnectPolicy connectPolicy = ConnectPolicy.OnIntent;

    private bool _ready;
    private bool _active;
    private bool _intentHeld;
    private int _connectQueuedFrame = -1;
    private GeminiStatus _lastStatus = GeminiStatus.Off;

    public bool Active => _active;
    public event Action<bool> ActiveChanged;
    public event Action<GeminiStatus> StatusChanged;
    public event Action ContextWarning;
    public event Action ContextExhausted;

    void OnEnable() {
        Settings.Changed += OnSettingChanged;
        Gemini.SetAssistantVolume(ChartDisplay.PercentOf(Settings.AssistantVolume));
    }

    private void OnSettingChanged(Setting which) {
        if (which == Setting.AssistantVolume) {
            Gemini.SetAssistantVolume(ChartDisplay.PercentOf(Settings.AssistantVolume));
        }
        else if (which == Setting.Language || which == Setting.AssistantVoice || which == Setting.WebSearch) {
            Gemini.Reconfigure(fresh: true);
            if (_ready && _active && isActiveAndEnabled) {
                Gemini.Disconnect();
                _ = ConnectNow();
            }
        }
        else if (which == Setting.ExplanationLevel) {
            Gemini.Reconfigure(fresh: false);
            StateChannel.RecordState("explanationLevel",
                "the user changed the explanation level; from now on: " +
                SystemPrompt.Explanations(Settings.ExplanationLevel).Replace("# Explanations\n", "").Trim());
        }
    }

    async void Start() {
        if (initPolicy == InitPolicy.AtLaunch) {
            await Gemini.EnsureInit(enableWebSearch);
            if (this == null) return;
        }

        _ready = true;
        Gemini.SetKeepAlive(_active);
        if (_active && isActiveAndEnabled) await ConnectNow();
    }

    private async Task ConnectNow() {
        await Gemini.EnsureInit(enableWebSearch);
        if (this == null || !isActiveAndEnabled) return;

        Gemini.Connect();
        if (_active) Gemini.Listen();
    }

    private static bool CanWarm =>
        Gemini.Status == GeminiStatus.Off || Gemini.Status == GeminiStatus.Failed;

    private void QueueConnect() => _connectQueuedFrame = Time.frameCount;

    public void SetActive(bool active) {
        bool changed = _active != active;
        _active = active;

        if (_ready) {
            Gemini.SetKeepAlive(active);
            if (active) {
                _ = ConnectNow();
            }
            else {
                Gemini.Disconnect();
            }
        }

        if (changed) ActiveChanged?.Invoke(active);
    }

    public void NotifyIntent() {
        _intentHeld = true;
        if (!_ready || _active) return;
        if (connectPolicy == ConnectPolicy.OnFirstUse) return;
        if (!CanWarm) return;

        QueueConnect();
    }

    public void NotifyIntentEnded() {
        _intentHeld = false;
        if (!_ready || _active) return;
        _connectQueuedFrame = -1;
        if (Gemini.Status != GeminiStatus.Off)
            Debug.Log("[Gemini][session] tool panel closed; dropping the intent socket");
        Gemini.Disconnect();
    }

    void Update() {
        if (!_ready) return;

        if (_connectQueuedFrame >= 0 && Time.frameCount > _connectQueuedFrame) {
            _connectQueuedFrame = -1;
            if (CanWarm) _ = ConnectNow();
        }

        var status = Gemini.Status;
        if (status != _lastStatus) {
            _lastStatus = status;
            StatusChanged?.Invoke(status);
        }

        if (Gemini.ConsumeClosingNotice()) ContextWarning?.Invoke();
        if (Gemini.ConsumeClosedNotice()) ContextExhausted?.Invoke();

    }

    void OnDisable() {
        Settings.Changed -= OnSettingChanged;
        if (_ready) Gemini.Disconnect();
    }

    void OnDestroy() {
        Gemini.Destroy();
    }

    void OnApplicationPause(bool paused) {
        if (!_ready) return;

        if (paused) {
            _connectQueuedFrame = -1;
            Gemini.Disconnect();
            return;
        }

        if (_active) {
            _ = ConnectNow();
        }
        else if (_intentHeld && CanWarm && connectPolicy != ConnectPolicy.OnFirstUse) {
            QueueConnect();
        }
    }
}
