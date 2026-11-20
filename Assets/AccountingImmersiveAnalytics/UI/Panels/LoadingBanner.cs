using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Helpers;
using OneText;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public sealed class LoadingBanner
    {
        private const float AheadMetres = 1.2f;
        private const float FollowRate = 4f;

        private readonly OneTextMesh _label;
        private string _shownStage;
        private int _shownDots = -1;

        public LoadingBanner(Transform parent)
        {
            _label = LabelFactory.MakeWorldBanner(parent, "LoadingLabel", Style.BannerSize);
            _label.RichText = true;
            Texts.Bind(_label, () => LoadingDots.Steady(Strings.Get("loading")));
        }

        public void Show(string stageKey)
        {
            int dots = LoadingDots.Count;
            if (stageKey != _shownStage || dots != _shownDots)
            {
                _shownStage = stageKey;
                _shownDots = dots;
                string key = stageKey;
                Texts.Bind(_label, () =>
                {
                    string word = LoadingDots.Steady(Strings.Get("loading"), dots);
                    return key == null ? word : $"{word}\n<size=55%>{Strings.Get(key)}</size>";
                });
            }
            FollowHead();
        }

        public void Hide() => _label.GetComponent<MeshRenderer>().enabled = false;

        private void FollowHead()
        {
            Transform cam = CameraRig.MainTransform;
            if (cam == null) return;
            Vector3 forward = CameraRig.FlatForward;
            Vector3 target = cam.position + forward * AheadMetres;
            Quaternion facing = Quaternion.LookRotation(forward);
            Transform t = _label.transform;
            float k = Animate.Damp(FollowRate, Time.unscaledDeltaTime);
            bool first = t.position == Vector3.zero;
            t.SetPositionAndRotation(first ? target : Vector3.Lerp(t.position, target, k),
                first ? facing : Quaternion.Slerp(t.rotation, facing, k));
        }
    }
}
