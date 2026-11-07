using System.Collections.Generic;
using UnityEngine;

public sealed class ChartBackdrop
{
    private const float PlaneAlpha = 0.22f;
    private const float LineAlpha = 0.55f;
    private const float EdgeWidth = 0.002f;
    private const float GridWidth = 0.001f;
    private const float PlaneShade = 0.55f;
    private const float LineShade = 1f;
    private const float LightPlaneShade = 1f;
    private const float LightLineShade = 0.15f;
    private static float s_planeShade = PlaneShade;
    private static float s_lineShade = LineShade;
    private const float LineLift = 0.0006f;
    private const int FallbackDivisions = 4;
    private const int BackdropQueue = 2990;
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int ShadedId = Shader.PropertyToID("_Shaded");
    private static readonly int GrowId = Shader.PropertyToID("_Grow");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
    private static readonly int CullId = Shader.PropertyToID("_Cull");

    private sealed class Surface
    {
        public Mesh Plane;
        public Mesh Edges;
        public Mesh Grid;
        public MeshRenderer PlaneRenderer;
        public MeshRenderer EdgesRenderer;
        public MeshRenderer GridRenderer;

        public void Show(bool plane, bool grid)
        {
            PlaneRenderer.enabled = plane;
            EdgesRenderer.enabled = plane;
            GridRenderer.enabled = grid;
        }
    }

    private readonly Surface _floor;
    private readonly Surface _lowX;
    private readonly Surface _highX;
    private readonly Surface _lowZ;
    private readonly Surface _highZ;
    private readonly Material _planeMaterial;
    private readonly Material _lineMaterial;
    private readonly Transform _owner;

    private ChartGeometry _geometry;
    private bool _light;
    private bool _viewerHighX;
    private bool _viewerHighZ;
    private bool _backdropOn = true;
    private bool _gridOn = true;
    private bool _shown = true;
    private float _margin = 0.02f;

    public ChartBackdrop(Transform owner, Material template)
    {
        _owner = owner;
        _planeMaterial = MakeMaterial(template, PlaneAlpha);
        _lineMaterial = MakeMaterial(template, LineAlpha);
        var root = new GameObject("Backdrop").transform;
        root.SetParent(owner, false);
        _floor = MakeSurface(root, "Floor");
        _lowX = MakeSurface(root, "WallLowX");
        _highX = MakeSurface(root, "WallHighX");
        _lowZ = MakeSurface(root, "WallLowZ");
        _highZ = MakeSurface(root, "WallHighZ");
    }

    private static Material MakeMaterial(Material template, float alpha)
    {
        var material = new Material(template);
        material.SetFloat(AlphaId, alpha);
        material.SetFloat(ShadedId, 0f);
        material.SetFloat(GrowId, 1f);
        material.SetFloat(SrcBlendId, (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat(DstBlendId, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat(ZWriteId, 0f);
        material.SetFloat(CullId, (float)UnityEngine.Rendering.CullMode.Off);
        material.renderQueue = BackdropQueue;
        return material;
    }

    private Surface MakeSurface(Transform root, string name)
    {
        var surface = new Surface();
        surface.Plane = MakeLayer(root, name + "_Plane", _planeMaterial, out surface.PlaneRenderer);
        surface.Edges = MakeLayer(root, name + "_Edges", _lineMaterial, out surface.EdgesRenderer);
        surface.Grid = MakeLayer(root, name + "_Grid", _lineMaterial, out surface.GridRenderer);
        return surface;
    }

    private static Mesh MakeLayer(Transform root, string name, Material material, out MeshRenderer renderer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var mesh = new Mesh { name = name };
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return mesh;
    }

    public void Rebuild(ChartGeometry geometry)
    {
        _geometry = geometry;
        s_planeShade = _light ? LightPlaneShade : PlaneShade;
        s_lineShade = _light ? LightLineShade : LineShade;
        Bounds b = geometry.Bounds;
        Vector3 min = b.min, max = b.max;
        _margin = Mathf.Max(geometry.LabelGap, 0.01f);

        List<float> gridX = Divisions(geometry.GridX, min.x, max.x);
        List<float> gridY = Divisions(geometry.GridY, min.y, max.y);
        List<float> gridZ = Divisions(geometry.GridZ, min.z, max.z);

        Vector3 F(float x, float z) => new Vector3(x, min.y, z);
        Fill(_floor, F(min.x, min.z), F(max.x, min.z), F(max.x, max.z), F(min.x, max.z), Vector3.up * LineLift,
            gridX, min.x, max.x, gridZ, min.z, max.z, true);

        FillWallX(_lowX, min.x, LineLift, min, max, gridZ, gridY);
        FillWallX(_highX, max.x, -LineLift, min, max, gridZ, gridY);
        FillWallZ(_lowZ, min.z, LineLift, min, max, gridX, gridY);
        FillWallZ(_highZ, max.z, -LineLift, min, max, gridX, gridY);
        Apply();
    }

    private static List<float> Divisions(List<float> given, float low, float high)
    {
        if (given.Count > 0) return given;
        var made = new List<float>(FallbackDivisions - 1);
        for (int i = 1; i < FallbackDivisions; i++) made.Add(Mathf.Lerp(low, high, i / (float)FallbackDivisions));
        return made;
    }

    private static void FillWallX(Surface surface, float x, float lift, Vector3 min, Vector3 max,
        List<float> gridZ, List<float> gridY)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        Quad(verts, tris, new Vector3(x, min.y, min.z), new Vector3(x, min.y, max.z), new Vector3(x, max.y, max.z), new Vector3(x, max.y, min.z));
        Write(surface.Plane, verts, tris, s_planeShade);

        Vector3 offset = Vector3.right * lift;
        var edges = new Lines();
        edges.Add(new Vector3(x, min.y, min.z) + offset, new Vector3(x, min.y, max.z) + offset, Vector3.up, EdgeWidth);
        edges.Add(new Vector3(x, max.y, min.z) + offset, new Vector3(x, max.y, max.z) + offset, Vector3.up, EdgeWidth);
        edges.Add(new Vector3(x, min.y, min.z) + offset, new Vector3(x, max.y, min.z) + offset, Vector3.forward, EdgeWidth);
        edges.Add(new Vector3(x, min.y, max.z) + offset, new Vector3(x, max.y, max.z) + offset, Vector3.forward, EdgeWidth);
        Write(surface.Edges, edges.Verts, edges.Tris, s_lineShade);

        var grid = new Lines();
        foreach (float z in gridZ)
            grid.Add(new Vector3(x, min.y, z) + offset, new Vector3(x, max.y, z) + offset, Vector3.forward, GridWidth);
        foreach (float y in gridY)
            grid.Add(new Vector3(x, y, min.z) + offset, new Vector3(x, y, max.z) + offset, Vector3.up, GridWidth);
        Write(surface.Grid, grid.Verts, grid.Tris, s_lineShade);
    }

    private static void FillWallZ(Surface surface, float z, float lift, Vector3 min, Vector3 max,
        List<float> gridX, List<float> gridY)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        Quad(verts, tris, new Vector3(min.x, min.y, z), new Vector3(max.x, min.y, z), new Vector3(max.x, max.y, z), new Vector3(min.x, max.y, z));
        Write(surface.Plane, verts, tris, s_planeShade);

        Vector3 offset = Vector3.forward * lift;
        var edges = new Lines();
        edges.Add(new Vector3(min.x, min.y, z) + offset, new Vector3(max.x, min.y, z) + offset, Vector3.up, EdgeWidth);
        edges.Add(new Vector3(min.x, max.y, z) + offset, new Vector3(max.x, max.y, z) + offset, Vector3.up, EdgeWidth);
        edges.Add(new Vector3(min.x, min.y, z) + offset, new Vector3(min.x, max.y, z) + offset, Vector3.right, EdgeWidth);
        edges.Add(new Vector3(max.x, min.y, z) + offset, new Vector3(max.x, max.y, z) + offset, Vector3.right, EdgeWidth);
        Write(surface.Edges, edges.Verts, edges.Tris, s_lineShade);

        var grid = new Lines();
        foreach (float x in gridX)
            grid.Add(new Vector3(x, min.y, z) + offset, new Vector3(x, max.y, z) + offset, Vector3.right, GridWidth);
        foreach (float y in gridY)
            grid.Add(new Vector3(min.x, y, z) + offset, new Vector3(max.x, y, z) + offset, Vector3.up, GridWidth);
        Write(surface.Grid, grid.Verts, grid.Tris, s_lineShade);
    }

    private static void Fill(Surface surface, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 offset,
        List<float> gridX, float minX, float maxX, List<float> gridZ, float minZ, float maxZ, bool floor)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        Quad(verts, tris, a, b, c, d);
        Write(surface.Plane, verts, tris, s_planeShade);

        float y = a.y;
        var edges = new Lines();
        edges.Add(a + offset, b + offset, Vector3.forward, EdgeWidth);
        edges.Add(d + offset, c + offset, Vector3.forward, EdgeWidth);
        edges.Add(a + offset, d + offset, Vector3.right, EdgeWidth);
        edges.Add(b + offset, c + offset, Vector3.right, EdgeWidth);
        Write(surface.Edges, edges.Verts, edges.Tris, s_lineShade);

        var grid = new Lines();
        foreach (float x in gridX)
            grid.Add(new Vector3(x, y, minZ) + offset, new Vector3(x, y, maxZ) + offset, Vector3.right, GridWidth);
        foreach (float z in gridZ)
            grid.Add(new Vector3(minX, y, z) + offset, new Vector3(maxX, y, z) + offset, Vector3.forward, GridWidth);
        Write(surface.Grid, grid.Verts, grid.Tris, s_lineShade);
    }

    private sealed class Lines
    {
        public readonly List<Vector3> Verts = new List<Vector3>();
        public readonly List<int> Tris = new List<int>();

        public void Add(Vector3 from, Vector3 to, Vector3 across, float width)
        {
            Vector3 half = across * (width * 0.5f);
            Quad(Verts, Tris, from - half, to - half, to + half, from + half);
        }
    }

    private static void Quad(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int first = verts.Count;
        verts.Add(a);
        verts.Add(b);
        verts.Add(c);
        verts.Add(d);
        tris.Add(first);
        tris.Add(first + 1);
        tris.Add(first + 2);
        tris.Add(first);
        tris.Add(first + 2);
        tris.Add(first + 3);
    }

    private static void Write(Mesh mesh, List<Vector3> verts, List<int> tris, float shade)
    {
        mesh.Clear();
        mesh.SetVertices(verts);
        var colors = new Color32[verts.Count];
        byte value = (byte)(shade * 255f);
        for (int i = 0; i < colors.Length; i++) colors[i] = new Color32(value, value, value, 255);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0, true);
    }

    public void SetAppearance(float strength, bool light)
    {
        _planeMaterial.SetFloat(AlphaId, Mathf.Clamp01(PlaneAlpha * strength));
        _lineMaterial.SetFloat(AlphaId, Mathf.Clamp01(LineAlpha * strength));
        if (light == _light) return;
        _light = light;
        if (_geometry != null) Rebuild(_geometry);
    }

    public void SetParts(bool backdrop, bool grid)
    {
        _backdropOn = backdrop;
        _gridOn = grid;
        Apply();
    }

    public void SetShown(bool on)
    {
        _shown = on;
        Apply();
    }

    public void FaceViewer()
    {
        if (!_shown) return;
        Transform cam = CameraRig.MainTransform;
        if (cam == null) return;

        Vector3 viewer = _owner.InverseTransformPoint(cam.position);
        bool highX = _viewerHighX ? viewer.x > -_margin : viewer.x > _margin;
        bool highZ = _viewerHighZ ? viewer.z > -_margin : viewer.z > _margin;
        if (highX == _viewerHighX && highZ == _viewerHighZ) return;
        _viewerHighX = highX;
        _viewerHighZ = highZ;
        Apply();
    }

    private void Apply()
    {
        bool plane = _shown && _backdropOn;
        bool grid = _shown && _gridOn;
        _floor.Show(plane, grid);
        _lowX.Show(plane && _viewerHighX, grid && _viewerHighX);
        _highX.Show(plane && !_viewerHighX, grid && !_viewerHighX);
        _lowZ.Show(plane && _viewerHighZ, grid && _viewerHighZ);
        _highZ.Show(plane && !_viewerHighZ, grid && !_viewerHighZ);
    }

    public void Destroy()
    {
        foreach (Surface surface in new[] { _floor, _lowX, _highX, _lowZ, _highZ })
        {
            Object.Destroy(surface.Plane);
            Object.Destroy(surface.Edges);
            Object.Destroy(surface.Grid);
        }
        Object.Destroy(_planeMaterial);
        Object.Destroy(_lineMaterial);
    }
}
