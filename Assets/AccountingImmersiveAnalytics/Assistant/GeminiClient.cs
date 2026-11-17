using System;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant
{
    public enum InitPolicy { OnFirstConnect, AtLaunch }

    public class GeminiClient : MonoBehaviour {

        [Tooltip("Google Search grounding, always on. It needs a paid-tier (billing-enabled) Gemini API key: on the free tier the session is rejected and the assistant turns itself off.")]
        public bool enableWebSearch = true;

        [Tooltip("When the Gemini SDK, mic permission and audio are initialised. OnFirstConnect defers all of it until the assistant is first needed.")]
        public InitPolicy initPolicy = InitPolicy.AtLaunch;

        private bool _ready;
        private bool _active;
        private GeminiStatus _lastStatus = GeminiStatus.Off;

        public bool Active => _active;
        public event Action<bool> ActiveChanged;
        public event Action<GeminiStatus> StatusChanged;
        public event Action ContextWarning;
        public event Action ContextExhausted;

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

        void Update() {
            if (!_ready) return;

            var status = Gemini.Status;
            if (status != _lastStatus) {
                _lastStatus = status;
                StatusChanged?.Invoke(status);
            }

            if (Gemini.ConsumeClosingNotice()) ContextWarning?.Invoke();
            if (Gemini.ConsumeClosedNotice()) ContextExhausted?.Invoke();

        }

        void OnDisable() {
            if (_ready) Gemini.Disconnect();
        }

        void OnDestroy() {
            Gemini.Destroy();
        }

        void OnApplicationPause(bool paused) {
            if (!_ready) return;

            if (paused) {
                Gemini.Disconnect();
                return;
            }

            if (_active) _ = ConnectNow();
        }
    }
}
