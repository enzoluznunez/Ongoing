using AccountingImmersiveAnalytics.Data;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Helpers;
using OneText;
using UnityEngine;
using TextAlignment = OneText.TextAlignment;

namespace AccountingImmersiveAnalytics.App
{
    public class LoadingPhase : MonoBehaviour
    {
        private const float GiveUpSeconds = 45f;
        private const float AheadMetres = 1.2f;
        private const float FollowRate = 4f;
        private const float LongestFrame = 0.25f;

        public static bool Done { get; private set; }


        private OneTextMesh _label;
        private float _elapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Done = false;
        }

        private void Start()
        {
            _label = Style.MakeWorldBanner(transform, "LoadingLabel", new Vector2(40f, 8f));
            Texts.Bind(_label, () => Strings.Get("loading"));
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
                _label.GetComponent<MeshRenderer>().enabled = false;
                float took = _elapsed;
                if (timedOut) Debug.LogWarning($"[LoadingPhase] Gave up waiting after {took:F1} s, at: {stage}");
                else Debug.Log($"[LoadingPhase] Loaded in {took:F1} s.");
                return;
            }

            int dots = Style.Dots;
            if (stage != _shownStage || dots != _shownDots)
            {
                _shownStage = stage;
                _shownDots = dots;
                string key = stage;
                string word = Strings.Get("loading") + new string('.', dots);
                Texts.Bind(_label, () => key == ListingStage ? word : $"{word}\n<size=55%>{Strings.Get(key)}</size>");
            }
            FollowHead();
        }

        private string _shownStage;
        private int _shownDots = -1;
        private const string ListingStage = "listing";

        private string Stage()
        {
            if (Scene.Charts != null && !AccountingImmersiveAnalytics.Documents.DocumentLibrary.Ready) return ListingStage;
            if (!GraphicsWarm.Step()) return "loading.graphics";
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
}
