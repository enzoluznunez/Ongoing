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
            PanelCapture capture = new GameObject("PanelCapture").AddComponent<PanelCapture>();
            capture.StartCoroutine(capture.Run());
        }

        private IEnumerator Run()
        {
            Directory.CreateDirectory(OutputDirectory);

            DocumentsPanel documents = Scene.Documents;
            if (documents != null)
            {
                documents.ShowPanel();
                yield return Shoot(documents, "documents");
                documents.HidePanel();
            }

            Debug.Log($"[PanelCapture] Wrote the panel images to {Path.GetFullPath(OutputDirectory)}.");
            Destroy(gameObject);
        }

        private static IEnumerator Shoot(Panel panel, string name)
        {
            for (int i = 0; i < SettleFrames; i++) yield return null;
            yield return new WaitForSecondsRealtime(SettleSeconds);

            RectTransform canvas = panel.GetComponentInChildren<Canvas>(true).transform as RectTransform;
            var corners = new Vector3[4];
            canvas.GetWorldCorners(corners);
            Vector3 centre = (corners[0] + corners[2]) * 0.5f;
            float worldHeight = Vector3.Distance(corners[0], corners[1]);

            int width = Mathf.RoundToInt(canvas.rect.width * PixelsPerUnit);
            int height = Mathf.RoundToInt(canvas.rect.height * PixelsPerUnit);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);

            var holder = new GameObject("PanelCaptureCamera");
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

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(Path.Combine(OutputDirectory, name + ".png"), image.EncodeToPNG());

            Destroy(image);
            camera.targetTexture = null;
            Destroy(holder);
            target.Release();
            Destroy(target);
        }
    }
}
