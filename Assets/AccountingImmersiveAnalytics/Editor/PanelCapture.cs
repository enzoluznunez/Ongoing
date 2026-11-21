using System.Collections;
using System.IO;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEditor;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Editor
{
    public sealed class PanelCapture : MonoBehaviour
    {
        private const string OutputDirectory = "Temp/PanelCaptures";
        private const string MenuPath = "Tools/Panels/Capture PNGs";
        private const int PixelsPerUnit = 2;
        private const int SettleFrames = 8;
        private const float SettleSeconds = 0.4f;
        private const float CameraGap = 0.1f;
        private const float Margin = 1.02f;

        private static readonly Color Backdrop = new Color(0.25f, 0.25f, 0.25f, 1f);

        [MenuItem(MenuPath, true)]
        private static bool CanCapture() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static void Capture()
        {
            var holder = new GameObject("PanelCapture");
            PanelCapture capture = holder.AddComponent<PanelCapture>();
            capture.StartCoroutine(capture.Run());
        }

        private IEnumerator Run()
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);

                DocumentsPanel documents = Scene.Documents;
                if (documents == null)
                {
                    Debug.LogWarning("[PanelCapture] No Documents panel was found, so nothing was captured.");
                    yield break;
                }

                bool wasVisible = documents.IsVisible;
                if (!wasVisible) documents.ShowPanel();
                try
                {
                    yield return Shoot(documents, "documents");
                }
                finally
                {
                    if (!wasVisible) documents.HidePanel();
                }

                Debug.Log($"[PanelCapture] Wrote the panel images to {Path.GetFullPath(OutputDirectory)}.");
            }
            finally
            {
                Destroy(gameObject);
            }
        }

        private static IEnumerator Shoot(Panel panel, string name)
        {
            for (int i = 0; i < SettleFrames; i++) yield return null;
            yield return new WaitForSecondsRealtime(SettleSeconds);

            Canvas found = panel.GetComponentInChildren<Canvas>(true);
            if (found == null)
            {
                Debug.LogWarning($"[PanelCapture] {panel.name} has no canvas, so {name}.png was not written.");
                yield break;
            }

            RectTransform canvas = found.transform as RectTransform;
            var corners = new Vector3[4];
            canvas.GetWorldCorners(corners);
            Vector3 centre = (corners[0] + corners[2]) * 0.5f;
            float worldHeight = Vector3.Distance(corners[0], corners[1]);

            int width = Mathf.RoundToInt(canvas.rect.width * PixelsPerUnit);
            int height = Mathf.RoundToInt(canvas.rect.height * PixelsPerUnit);
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);

            var holder = new GameObject("PanelCaptureCamera");
            Texture2D image = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = holder.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = worldHeight * 0.5f * Margin;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Backdrop;
                camera.cullingMask = 1 << canvas.gameObject.layer;
                camera.nearClipPlane = CameraGap * 0.5f;
                camera.farClipPlane = CameraGap * 1.5f;
                camera.targetTexture = target;
                camera.stereoTargetEye = StereoTargetEyeMask.None;
                holder.transform.SetPositionAndRotation(centre - panel.transform.forward * CameraGap, panel.transform.rotation);
                camera.Render();

                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RenderTexture.active = previous;

                File.WriteAllBytes(Path.Combine(OutputDirectory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (image != null) Destroy(image);
                holder.SetActive(false);
                Destroy(holder);
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
