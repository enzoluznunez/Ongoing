using System;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant.Voice;
using UnityEngine;
using UnityEngine.Serialization;

namespace AccountingImmersiveAnalytics.Assistant
{
    public class AssistantSwitch : MonoBehaviour
    {
        [Tooltip("Google Search grounding, always on. It needs a paid-tier (billing-enabled) Gemini API key: on the free tier the session is rejected and the assistant turns itself off.")]
        [SerializeField, FormerlySerializedAs("enableWebSearch")] private bool _webSearch = true;

        [Tooltip("When the voice session, mic permission and audio start. OnFirstConnect defers all of it until the assistant is first needed.")]
        [SerializeField, FormerlySerializedAs("initPolicy")] private StartPolicy _startPolicy = StartPolicy.AtLaunch;

        private bool _ready;
        private bool _active;
        private VoiceStatus _lastStatus = VoiceStatus.Off;

        public bool Active => _active;
        public event Action<bool> ActiveChanged;
        public event Action<VoiceStatus> StatusChanged;
        public event Action ContextWarning;
        public event Action ContextExhausted;

        private async void Start()
        {
            try
            {
                if (_startPolicy == StartPolicy.AtLaunch)
                {
                    await VoiceSetup.EnsureStarted(_webSearch);
                    if (this == null) return;
                }

                _ready = true;
                VoiceSession.SetKeepAlive(_active);
                if (_active && isActiveAndEnabled) await ConnectNow();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssistantSwitch] Start-up failed: {e}");
            }
        }

        private async Task ConnectNow()
        {
            await VoiceSetup.EnsureStarted(_webSearch);
            if (this == null || !isActiveAndEnabled) return;

            VoiceSession.Connect();
            if (_active) VoiceSession.Listen();
        }

        public void SetActive(bool active)
        {
            bool changed = _active != active;
            _active = active;

            if (_ready)
            {
                VoiceSession.SetKeepAlive(active);
                if (active) _ = ConnectNow();
                else VoiceSession.Disconnect();
            }

            if (changed) ActiveChanged?.Invoke(active);
        }

        private void Update()
        {
            if (!_ready) return;

            VoiceStatus status = VoiceSession.Status;
            if (status != _lastStatus)
            {
                _lastStatus = status;
                StatusChanged?.Invoke(status);
            }

            if (ContextBudget.ConsumeWarning()) ContextWarning?.Invoke();
            if (ContextBudget.ConsumeEnded()) ContextExhausted?.Invoke();
        }

        private void OnDisable()
        {
            if (_ready) VoiceSession.Disconnect();
        }

        private void OnDestroy()
        {
            VoiceSession.Destroy();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!_ready) return;

            if (paused)
            {
                VoiceSession.Disconnect();
                return;
            }

            if (_active) _ = ConnectNow();
        }
    }
}
