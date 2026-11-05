using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class PosterCapture
{
    private const string OutputDirectory = "poster";
    private const int Supersample = 3;

    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);
    private static readonly Color White = Color.white;

    [MenuItem("Tools/Poster/Capture Prototype Views")]
    public static void CaptureAll()
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), OutputDirectory);
        Directory.CreateDirectory(directory);

        CaptureNetwork(directory);
        CaptureSurface(directory);

        Debug.Log($"[PosterCapture] Wrote the prototype views to {directory}.");
    }

    public static void CaptureAllBatch()
    {
        try
        {
            CaptureAll();
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PosterCapture] {e}");
            EditorApplication.Exit(1);
        }
    }

    private static void CaptureNetwork(string directory)
    {
        var host = new GameObject("PosterNetworkGraph");
        Material material = Material();
        try
        {
            var view = host.AddComponent<NetworkGraphView>();
            view.Build(material);

            Render(directory, "network-graph", 2048, 2048,
                   pivot: Vector3.zero,
                   euler: Vector3.zero,
                   orthographicSize: 1.38f,
                   fieldOfView: 30f);
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(material);
        }
    }

    private static void CaptureSurface(string directory)
    {
        var host = new GameObject("PosterSurfaceGraph");
        Material material = Material();
        try
        {
            var view = host.AddComponent<SurfaceGraphView>();
            view.Build(material);

            Render(directory, "surface-graph", 2048, 2048,
                   pivot: new Vector3(0f, 0.86f, 0f),
                   euler: new Vector3(44f, -30f, 0f),
                   orthographicSize: 1.72f);
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(material);
        }
    }

    private static Material Material()
    {
        var material = Resources.Load<Material>("Materials/SheetMaterial");
        if (material == null)
            throw new FileNotFoundException("Resources/Materials/SheetMaterial is missing.");

        var copy = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
        copy.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        return copy;
    }

    private static void Render(string directory, string name,
                               int width, int height, Vector3 pivot, Vector3 euler,
                               float orthographicSize, float fieldOfView = 0f)
    {
        Write(Path.Combine(directory, name + ".png"),
              Draw(width, height, pivot, euler, orthographicSize, fieldOfView, Transparent));
        Write(Path.Combine(directory, name + "-white.png"),
              Draw(width, height, pivot, euler, orthographicSize, fieldOfView, White));
    }

    private static Texture2D Draw(int width, int height,
                                  Vector3 pivot, Vector3 euler,
                                  float orthographicSize, float fieldOfView,
                                  Color background)
    {
        var cameraObject = new GameObject("PosterCamera");
        RenderTexture target = null;
        Texture2D full = null;

        try
        {
            bool perspective = fieldOfView > 0f;
            float distance = perspective
                ? orthographicSize / Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad)
                : 10f;

            Quaternion facing = Quaternion.Euler(euler);
            cameraObject.transform.SetPositionAndRotation(pivot - facing * Vector3.forward * distance, facing);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = !perspective;
            camera.orthographicSize = orthographicSize;
            camera.fieldOfView = perspective ? fieldOfView : 60f;
            camera.aspect = (float)width / height;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.cullingMask = ~0;

            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.antialiasing = AntialiasingMode.None;

            target = new RenderTexture(width * Supersample, height * Supersample, 24,
                                       RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            camera.Render();

            full = ReadBack(target);
            return Downsample(full, width, height);
        }
        finally
        {
            if (full != null) Object.DestroyImmediate(full);
            if (target != null)
            {
                RenderTexture.active = null;
                target.Release();
                Object.DestroyImmediate(target);
            }
            Object.DestroyImmediate(cameraObject);
        }
    }

    private static Texture2D ReadBack(RenderTexture target)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;

        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, false);
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        texture.Apply();

        RenderTexture.active = previous;
        return texture;
    }

    private static Texture2D Downsample(Texture2D source, int width, int height)
    {
        Color[] pixels = source.GetPixels();
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        var output = new Color[width * height];

        int block = Supersample;
        float count = block * block;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;

                for (int sy = 0; sy < block; sy++)
                {
                    int row = (y * block + sy) * source.width;
                    for (int sx = 0; sx < block; sx++)
                    {
                        Color p = pixels[row + x * block + sx];
                        r += p.r * p.a; g += p.g * p.a; b += p.b * p.a; a += p.a;
                    }
                }

                output[y * width + x] = a > 0f
                    ? new Color(r / a, g / a, b / a, a / count)
                    : new Color(0f, 0f, 0f, 0f);
            }
        }

        result.SetPixels(output);
        result.Apply();
        return result;
    }

    private static void Write(string path, Texture2D texture)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }
}
