using System.Collections;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Panels;
using OneText;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.App
{
    public class Prewarm : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;

            GameObject rig = BuildWarmRig();

            DocumentsPanel documents = Scene.Documents;
            Tooltip tooltip = FindAnyObjectByType<Tooltip>(FindObjectsInactive.Include);

            if (documents != null) documents.PrewarmCanvas(true);
            if (tooltip != null) tooltip.PrewarmCanvas(true);

            yield return null;
            yield return null;

            if (documents != null) documents.PrewarmCanvas(false);
            if (tooltip != null) tooltip.PrewarmCanvas(false);
            if (rig != null) Destroy(rig);

            Destroy(gameObject);
        }

        private static GameObject BuildWarmRig()
        {
            Transform cam = CameraRig.MainTransform;
            if (cam == null)
            {
                Debug.LogWarning("[Prewarm] No main camera yet; shader warm draws were skipped.");
                return null;
            }

            var root = new GameObject("PrewarmRig");
            root.transform.SetParent(cam, false);
            root.transform.localPosition = new Vector3(0f, 0f, 1f);

            var canvasGo = new GameObject("WarmCanvas", typeof(Canvas));
            canvasGo.transform.SetParent(root.transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)canvasGo.transform;
            rect.sizeDelta = new Vector2(2f, 2f);
            rect.localScale = Vector3.one * 0.001f;

            AddImage(canvasGo.transform, null);
            AddImage(canvasGo.transform, Panel.FrontMaterial);
            AddImage(canvasGo.transform, Panel.BackMaterial);
            AddLabel(canvasGo.transform, false, false);
            AddLabel(canvasGo.transform, true, false);
            AddLabel(canvasGo.transform, false, true);

            var world = new GameObject("WarmWorldLabel", typeof(RectTransform)).AddComponent<OneTextMesh>();
            world.transform.SetParent(root.transform, false);
            world.transform.localScale = Vector3.one * 0.001f;
            world.RichText = false;
            world.Text = CommonGlyphs;

            return root;
        }

        private static readonly string CommonGlyphs = BuildCommonGlyphs();

        private static string BuildCommonGlyphs()
        {
            var sb = new System.Text.StringBuilder();
            for (char c = ' '; c <= '~'; c++) sb.Append(c);
            for (char c = '\u00A1'; c <= '\u00FF'; c++) sb.Append(c);
            sb.Append("\u2018\u2019\u201C\u201D\u2013\u2014\u2026\u2022\u20AC\u2122");
            return sb.ToString();
        }

        private static void AddImage(Transform parent, Material material)
        {
            var go = new GameObject("WarmImage", typeof(Image));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(2f, 2f);
            if (material != null) go.GetComponent<Image>().material = material;
        }

        private static void AddLabel(Transform parent, bool bold, bool outlined)
        {
            var go = new GameObject("WarmLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(2f, 2f);
            var label = go.AddComponent<OneTextLabel>();
            Style.SetBold(label, bold);
            if (outlined) label.Decoration = Style.Outline;
            label.RichText = false;
            label.FontSize = 1f;
            label.Text = CommonGlyphs;
        }
    }
}
