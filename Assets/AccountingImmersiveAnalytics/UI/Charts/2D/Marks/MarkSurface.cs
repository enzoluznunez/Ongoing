using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks
{
    public enum Side { Free, Gap, Edge }

    public sealed class MarkSurface : MaskableGraphic
    {
        private const float SolidKind = 0f;
        private const float BoxKind = 1f;
        private const float SliceKind = 2f;
        private const float BandKind = 3f;
        private const int MostVertices = 64000;

        private readonly List<UIVertex> _vertices = new List<UIVertex>();
        private readonly List<int> _indices = new List<int>();
        private readonly List<Vector2Int> _shapes = new List<Vector2Int>();
        private Rect? _clip;

        public override Material defaultMaterial =>
            AssetLibrary.Current != null && AssetLibrary.Current.ChartMarks != null ? AssetLibrary.Current.ChartMarks : base.defaultMaterial;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }


        public void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
            _shapes.Clear();
            _clip = null;
            SetVerticesDirty();
        }

        public void ClipTo(Rect? plot) => _clip = plot;

        public void Recolor(int shape, Color color)
        {
            if (shape < 0 || shape >= _shapes.Count) return;
            Vector2Int range = _shapes[shape];
            for (int i = range.x; i < range.y; i++)
            {
                UIVertex v = _vertices[i];
                v.color = color;
                _vertices[i] = v;
            }
            SetVerticesDirty();
        }

        public Color ColorOf(int shape) =>
            shape >= 0 && shape < _shapes.Count && _shapes[shape].x < _vertices.Count ? (Color)_vertices[_shapes[shape].x].color : Color.clear;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_vertices.Count == 0) return;
            vh.AddUIVertexStream(_vertices, _indices);
        }

        public int Box(Rect r, Color color, Vector4 radii, float stroke = 0f)
        {
            if (r.width <= 0f || r.height <= 0f) return -1;
            float limit = Mathf.Min(r.width, r.height) * 0.5f;
            radii = new Vector4(Mathf.Min(radii.x, limit), Mathf.Min(radii.y, limit), Mathf.Min(radii.z, limit), Mathf.Min(radii.w, limit));
            Vector2 half = r.size * 0.5f;
            int start = Begin();
            float m = ChartStyle.AntiAlias + stroke;
            Quad(r.center, half + Vector2.one * m, color, p => p, new Vector4(half.x, half.y, stroke, BoxKind), radii);
            return End(start);
        }

        public int Box(Rect r, Color color, float radius = 0f, float stroke = 0f) =>
            Box(r, color, Vector4.one * radius, stroke);

        public int Circle(Vector2 centre, float radius, Color color) =>
            Box(new Rect(centre.x - radius, centre.y - radius, radius * 2f, radius * 2f), color, radius);

        public int Cell(Rect r, Color color, Sides sides)
        {
            float half = Style.Stroke * 0.5f;
            float Inset(Side s) => s == Side.Gap ? half : 0f;
            var inner = Rect.MinMaxRect(r.xMin + Inset(sides.Left), r.yMin + Inset(sides.Top),
                r.xMax - Inset(sides.Right), r.yMax - Inset(sides.Bottom));
            float Round(Side a, Side b) => a == Side.Edge || b == Side.Edge ? 0f : Style.SmallRadius;
            return Box(inner, color, new Vector4(
                Round(sides.Top, sides.Left), Round(sides.Top, sides.Right),
                Round(sides.Bottom, sides.Right), Round(sides.Bottom, sides.Left)));
        }

        public int Ring(Vector2 centre, float outer, float inner, float from, float to, Color color)
        {
            float halfGap = Style.Stroke * 0.5f;
            float aperture = (to - from) * 0.5f;
            if (aperture <= halfGap / Mathf.Max(inner, 1f)) return -1;
            float bisector = (from + to) * 0.5f;
            Vector2 axis = new Vector2(Mathf.Cos(bisector), Mathf.Sin(bisector));
            Vector2 across = new Vector2(-axis.y, axis.x);
            float reach = outer + ChartStyle.AntiAlias;
            float full = aperture >= Mathf.PI - 0.0001f ? Mathf.PI : aperture;
            int start = Begin();
            Quad(centre, Vector2.one * reach, color,
                p => new Vector2(Vector2.Dot(p, axis), Vector2.Dot(p, across)),
                new Vector4(outer, inner, halfGap, SliceKind), new Vector4(full, Style.SmallRadius, 0f, 0f));
            return End(start);
        }

        public int Line(Vector2 from, Vector2 to, float width, Color color) =>
            Polyline(new List<Vector2> { from, to }, width, color);

        public int Polyline(IList<Vector2> points, float width, Color color)
        {
            if (points.Count < 2) return -1;
            float half = width * 0.5f;
            float pad = half + ChartStyle.AntiAlias;
            int start = Begin();
            var left = new Vector2[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 normal;
                float stretch = 1f;
                if (i == 0) normal = Normal(points[0], points[1]);
                else if (i == points.Count - 1) normal = Normal(points[i - 1], points[i]);
                else
                {
                    Vector2 a = Normal(points[i - 1], points[i]), b = Normal(points[i], points[i + 1]);
                    normal = (a + b).normalized;
                    if (normal.sqrMagnitude < 1e-6f) normal = a;
                    stretch = 1f / Mathf.Max(0.1f, Vector2.Dot(normal, a));
                }
                left[i] = normal * stretch;
            }
            for (int i = 0; i < points.Count - 1; i++)
            {
                int first = _vertices.Count;
                AddBand(points[i] + left[i] * pad, pad, color);
                AddBand(points[i] - left[i] * pad, -pad, color);
                AddBand(points[i + 1] - left[i + 1] * pad, -pad, color);
                AddBand(points[i + 1] + left[i + 1] * pad, pad, color);
                Triangles(first);
            }
            return End(start);

            void AddBand(Vector2 at, float across, Color c) =>
                Add(at, c, new Vector2(0f, across), new Vector4(half, 0f, 0f, BandKind), Vector4.zero);
        }

        public int Gradient(Rect r, Color[] stops, float radius)
        {
            if (stops.Length < 2) return Box(r, stops.Length > 0 ? stops[0] : Color.white, radius);
            Vector2 half = r.size * 0.5f;
            var sizes = new Vector4(half.x, half.y, 0f, BoxKind);
            var radii = Vector4.one * Mathf.Min(radius, Mathf.Min(half.x, half.y));
            int start = Begin();
            float m = ChartStyle.AntiAlias;
            for (int i = 0; i < stops.Length - 1; i++)
            {
                float x0 = r.xMin + r.width * i / (stops.Length - 1);
                float x1 = r.xMin + r.width * (i + 1) / (stops.Length - 1);
                if (i == 0) x0 -= m;
                if (i == stops.Length - 2) x1 += m;
                int first = _vertices.Count;
                Corner(x0, r.yMin - m, stops[i]);
                Corner(x1, r.yMin - m, stops[i + 1]);
                Corner(x1, r.yMax + m, stops[i + 1]);
                Corner(x0, r.yMax + m, stops[i]);
                Triangles(first);
            }
            return End(start);

            void Corner(float x, float y, Color c) => Add(new Vector2(x, y), c, new Vector2(x, y) - r.center, sizes, radii);
        }

        public int Fade(Rect r, Color left, Color right)
        {
            int start = Begin();
            int first = _vertices.Count;
            Add(new Vector2(r.xMin, r.yMin), left, Vector2.zero, new Vector4(0f, 0f, 0f, SolidKind), Vector4.zero);
            Add(new Vector2(r.xMax, r.yMin), right, Vector2.zero, new Vector4(0f, 0f, 0f, SolidKind), Vector4.zero);
            Add(new Vector2(r.xMax, r.yMax), right, Vector2.zero, new Vector4(0f, 0f, 0f, SolidKind), Vector4.zero);
            Add(new Vector2(r.xMin, r.yMax), left, Vector2.zero, new Vector4(0f, 0f, 0f, SolidKind), Vector4.zero);
            Triangles(first);
            return End(start);
        }

        public void Commit() => SetVerticesDirty();

        private static Vector2 Normal(Vector2 a, Vector2 b)
        {
            Vector2 d = (b - a).normalized;
            return new Vector2(-d.y, d.x);
        }

        private int Begin() => _vertices.Count;

        private int End(int start)
        {
            if (_vertices.Count > MostVertices)
            {
                _vertices.RemoveRange(start, _vertices.Count - start);
                int keep = _indices.Count;
                while (keep > 0 && _indices[keep - 1] >= start) keep--;
                _indices.RemoveRange(keep, _indices.Count - keep);
                return -1;
            }
            _shapes.Add(new Vector2Int(start, _vertices.Count));
            return _shapes.Count - 1;
        }

        private void Quad(Vector2 centre, Vector2 reach, Color color, System.Func<Vector2, Vector2> frame, Vector4 sizes, Vector4 corners)
        {
            int first = _vertices.Count;
            Corner(centre, new Vector2(-reach.x, -reach.y), color, frame, sizes, corners);
            Corner(centre, new Vector2(reach.x, -reach.y), color, frame, sizes, corners);
            Corner(centre, new Vector2(reach.x, reach.y), color, frame, sizes, corners);
            Corner(centre, new Vector2(-reach.x, reach.y), color, frame, sizes, corners);
            Triangles(first);
        }

        private void Corner(Vector2 centre, Vector2 offset, Color color, System.Func<Vector2, Vector2> frame, Vector4 sizes, Vector4 corners) =>
            Add(centre + offset, color, frame(offset), sizes, corners);

        private void Triangles(int first)
        {
            _indices.Add(first);
            _indices.Add(first + 1);
            _indices.Add(first + 2);
            _indices.Add(first);
            _indices.Add(first + 2);
            _indices.Add(first + 3);
        }

        private void Add(Vector2 design, Color color, Vector2 shape, Vector4 sizes, Vector4 corners)
        {
            Vector4 clipBox = Vector4.zero;
            if (_clip.HasValue)
            {
                Rect c = _clip.Value;
                Vector2 rel = design - c.center;
                clipBox = new Vector4(rel.x, rel.y, c.width * 0.5f, c.height * 0.5f);
            }
            _vertices.Add(new UIVertex
            {
                position = new Vector3(design.x, -design.y, 0f),
                color = color,
                uv0 = new Vector4(shape.x, shape.y, 0f, 0f),
                uv1 = sizes,
                uv2 = corners,
                uv3 = clipBox,
                normal = Vector3.back,
                tangent = new Vector4(1f, 0f, 0f, -1f)
            });
        }
    }
}
