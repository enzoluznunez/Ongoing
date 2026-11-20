using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Hands
{
    public sealed class RayVisual
    {
        private const float LineStartWidth = 0.003f;
        private const float LineEndWidth = 0.0015f;
        private const float DotMinimumWidth = 0.01f;
        private const float DotWidthPerMetre = 0.008f;
        private const float DotLift = 0.003f;
        private const float DotDepth = 0.0005f;
        private const int RoundingVertices = 8;

        private readonly LineRenderer _line;
        private readonly LineRenderer _dot;
        private bool _pressed;

        public RayVisual(Transform parent, string name)
        {
            Material material = AssetLibrary.Current != null ? AssetLibrary.Current.RayLine : null;
            _line = MakeRenderer(parent, $"{name} Line", material);
            _line.startWidth = LineStartWidth;
            _line.endWidth = LineEndWidth;
            _dot = MakeRenderer(parent, $"{name} Dot", material);
            Hide();
        }

        private static LineRenderer MakeRenderer(Transform parent, string name, Material material)
        {
            GameObject go = FindTransform.EnsureChild(parent, name).gameObject;
            LineRenderer renderer = LayoutTools.Ensure<LineRenderer>(go);
            renderer.sharedMaterial = material;
            renderer.useWorldSpace = true;
            renderer.positionCount = 2;
            renderer.alignment = LineAlignment.View;
            renderer.numCapVertices = RoundingVertices;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.startColor = Style.Card;
            renderer.endColor = Style.Card;
            return renderer;
        }

        public void Show(Vector3 origin, Vector3 end, bool pressed)
        {
            Vector3 back = origin - end;
            float distance = back.magnitude;
            Vector3 toward = distance > 1e-5f ? back / distance : Vector3.up;
            Vector3 dotCenter = end + toward * DotLift;

            _line.SetPosition(0, origin);
            _line.SetPosition(1, dotCenter);
            _line.enabled = true;

            float width = Mathf.Max(DotMinimumWidth, distance * DotWidthPerMetre);
            _dot.startWidth = width;
            _dot.endWidth = width;
            if (pressed != _pressed)
            {
                _pressed = pressed;
                Color color = pressed ? Style.Ink : Style.Card;
                _dot.startColor = color;
                _dot.endColor = color;
            }
            _dot.SetPosition(0, dotCenter);
            _dot.SetPosition(1, dotCenter + toward * DotDepth);
            _dot.enabled = true;
        }

        public void Hide()
        {
            _line.enabled = false;
            _dot.enabled = false;
        }
    }
}
