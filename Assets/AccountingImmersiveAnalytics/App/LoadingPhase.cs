using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    public class LoadingPhase : MonoBehaviour
    {
        private const float GiveUpSeconds = 45f;
        private const float LongestFrame = 0.25f;
        private const string ListingStage = "listing";

        public static bool Done { get; private set; }

        private LoadingBanner _banner;
        private float _elapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Done = false;
        }

        private void Start()
        {
            _banner = new LoadingBanner(transform);
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
                _banner.Hide();
                float took = _elapsed;
                if (timedOut) Debug.LogWarning($"[LoadingPhase] Gave up waiting after {took:F1} s, at: {stage}");
                else Debug.Log($"[LoadingPhase] Loaded in {took:F1} s.");
                return;
            }

            _banner.Show(stage == ListingStage ? null : stage);
        }

        private string Stage()
        {
            if (Scene.Charts != null && !AccountingImmersiveAnalytics.Documents.DocumentLibrary.Ready) return ListingStage;
            if (!GraphicsWarm.Step()) return "loading.graphics";
            return null;
        }
    }
}
