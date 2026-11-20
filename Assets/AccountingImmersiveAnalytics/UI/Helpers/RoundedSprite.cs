using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public static class RoundedSprite
    {
        private const int TexelsPerUnit = 4;
        private const float ReferencePixelsPerUnit = 100f;

        private static readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();
        private static readonly Dictionary<(int, int), Sprite> _rings = new Dictionary<(int, int), Sprite>();

        public static Sprite Get(float radiusUnits)
        {
            int r = Mathf.Max(2, Mathf.RoundToInt(radiusUnits * TexelsPerUnit));
            if (_sprites.TryGetValue(r, out Sprite cached) && cached != null) return cached;

            Sprite sprite = Paint(r, (dx, dy, x, y) =>
                dx > 0f && dy > 0f ? Coverage(Mathf.Sqrt(dx * dx + dy * dy), r + 0.5f) : (byte)255);
            _sprites[r] = sprite;
            return sprite;
        }

        public static Sprite GetRing(float radiusUnits, float strokeUnits)
        {
            int r = Mathf.Max(2, Mathf.RoundToInt(radiusUnits * TexelsPerUnit));
            int s = Mathf.Clamp(Mathf.RoundToInt(strokeUnits * TexelsPerUnit), 1, r - 1);
            if (_rings.TryGetValue((r, s), out Sprite cached) && cached != null) return cached;

            int size = Size(r);
            Sprite sprite = Paint(r, (dx, dy, x, y) =>
            {
                bool inside = y >= s && y < size - s && x >= s && x < size - s;
                float frame = 1f, inner = inside ? 1f : 0f;
                if (dx > 0f && dy > 0f)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    frame = Coverage(dist, r + 0.5f) / 255f;
                    inner = inside ? Coverage(dist, r - s + 0.5f) / 255f : 0f;
                }
                return (byte)Mathf.RoundToInt(255f * frame * (1f - inner));
            });
            _rings[(r, s)] = sprite;
            return sprite;
        }

        private static int Size(int r) => r * 2 + 4;

        private static float CornerDistance(int i, int r, int size)
        {
            float p = i + 0.5f;
            return p < r ? r - p : (p > size - r ? p - (size - r) : -1f);
        }

        private static Sprite Paint(int r, System.Func<float, float, int, int, byte> alpha)
        {
            int size = Size(r);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, 1, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float dy = CornerDistance(y, r, size);
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha(CornerDistance(x, r, size), dy, x, y));
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                ReferencePixelsPerUnit * TexelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        public static void ApplyRing(Image image, float radiusUnits, float strokeUnits)
        {
            if (image == null) return;

            image.sprite = GetRing(radiusUnits, strokeUnits);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
            Panel.ApplyFront(image);
        }

        public static void Apply(Image image, float radiusUnits, bool back = false)
        {
            if (image == null) return;

            image.sprite = Get(radiusUnits);
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;

            Material backMat = back ? Panel.BackMaterial : null;
            if (backMat != null) image.material = backMat;
            else Panel.ApplyFront(image);
        }

        private static byte Coverage(float dist, float radius)
        {
            float edge = dist - radius;
            if (edge <= -0.5f) return 255;
            if (edge >= 0.5f) return 0;
            return (byte)Mathf.RoundToInt(255f * (0.5f - edge));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            foreach (KeyValuePair<int, Sprite> kv in _sprites)
            {
                if (kv.Value == null) continue;
                Texture2D stale = kv.Value.texture;
                Object.Destroy(kv.Value);
                if (stale != null) Object.Destroy(stale);
            }
            _sprites.Clear();
            foreach (KeyValuePair<(int, int), Sprite> kv in _rings)
            {
                if (kv.Value == null) continue;
                Texture2D stale = kv.Value.texture;
                Object.Destroy(kv.Value);
                if (stale != null) Object.Destroy(stale);
            }
            _rings.Clear();
        }
    }
}
