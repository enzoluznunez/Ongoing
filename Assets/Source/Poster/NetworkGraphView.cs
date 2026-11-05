using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class NetworkGraphView : MonoBehaviour
{
    [Tooltip("How far the outer ring of nodes sits from the centre, in world units.")]
    public float radius = 1.0f;

    [Tooltip("How far the nearest and furthest nodes stand from the middle plane.")]
    public float depthSpread = 0.85f;

    [Tooltip("Node radius at the smallest and largest value of the size variable.")]
    public float minNodeRadius = 0.075f;
    public float maxNodeRadius = 0.16f;

    [Tooltip("Edge ribbon width at the weakest and strongest tie.")]
    public float minEdgeWidth = 0.008f;
    public float maxEdgeWidth = 0.030f;

    [Tooltip("How much light the furthest node keeps; the nearest keeps all of it.")]
    [Range(0f, 1f)] public float farExposure = 0.46f;

    [Tooltip("How much of its nodes' colour an edge carries; the rest is neutral.")]
    [Range(0f, 1f)] public float edgeTint = 0.55f;

    private static readonly Vector4[] Layout =
    {
        new Vector4(  98f, 1.00f, 0.45f, 0.18f),
        new Vector4(  46f, 0.86f, 0.62f, 0.72f),
        new Vector4(   4f, 1.00f, 0.38f, 0.05f),
        new Vector4( 322f, 0.78f, 0.95f, 0.95f),
        new Vector4( 274f, 1.00f, 0.58f, 0.41f),
        new Vector4( 232f, 0.83f, 0.34f, 0.28f),
        new Vector4( 190f, 1.00f, 0.71f, 0.85f),
        new Vector4( 148f, 0.80f, 0.88f, 0.55f),
        new Vector4(  22f, 0.34f, 1.00f, 0.64f),
        new Vector4( 205f, 0.38f, 0.22f, 0.34f),
    };

    private static readonly Vector3[] Edges =
    {
        new Vector3(0, 8, 0.55f), new Vector3(0, 6, 0.42f), new Vector3(0, 4, 0.30f),
        new Vector3(1, 3, 0.88f), new Vector3(1, 4, 0.61f), new Vector3(1, 2, 0.34f),
        new Vector3(2, 3, 0.52f), new Vector3(2, 7, 0.44f), new Vector3(2, 9, 0.26f),
        new Vector3(3, 5, 0.73f), new Vector3(3, 8, 0.80f), new Vector3(3, 4, 0.66f),
        new Vector3(4, 6, 0.58f), new Vector3(4, 8, 0.47f),
        new Vector3(5, 6, 0.69f), new Vector3(5, 8, 0.36f),
        new Vector3(6, 7, 0.63f), new Vector3(6, 8, 0.75f),
        new Vector3(7, 8, 0.92f), new Vector3(7, 9, 0.48f),
        new Vector3(8, 9, 0.57f),
    };

    private static readonly Color EdgeNeutral = new Color(0.62f, 0.64f, 0.68f);

    public void Build(Material material)
    {
        var mesh = new PosterMesh();

        Vector3[] centers = new Vector3[Layout.Length];
        float[] radii = new float[Layout.Length];
        float[] depths = new float[Layout.Length];

        for (int i = 0; i < Layout.Length; i++)
        {
            float angle = Layout[i].x * Mathf.Deg2Rad;
            float distance = Layout[i].y * radius;
            depths[i] = Layout[i].w;

            centers[i] = new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                (0.5f - depths[i]) * 2f * depthSpread);

            radii[i] = Mathf.Lerp(minNodeRadius, maxNodeRadius, Layout[i].z);
        }

        foreach (Vector3 edge in Edges)
        {
            int from = (int)edge.x;
            int to = (int)edge.y;
            float strength = edge.z;

            float back = Mathf.Max(radii[from], radii[to]) + 0.05f;
            Vector3 a = centers[from] + Vector3.forward * back;
            Vector3 b = centers[to] + Vector3.forward * back;
            PosterMesh.Trim(ref a, ref b, radii[from] * 0.92f, radii[to] * 0.92f);

            Color ca = Color.Lerp(EdgeNeutral, PosterPalette.Band(depths[from]), edgeTint * strength);
            Color cb = Color.Lerp(EdgeNeutral, PosterPalette.Band(depths[to]), edgeTint * strength);
            float width = Mathf.Lerp(minEdgeWidth, maxEdgeWidth, strength);

            mesh.AddRibbon(a, b, width,
                PosterMesh.Shade(ca, -Vector3.forward, Exposure(depths[from])),
                PosterMesh.Shade(cb, -Vector3.forward, Exposure(depths[to])));
        }

        for (int i = 0; i < centers.Length; i++)
            mesh.AddSphere(centers[i], radii[i], PosterPalette.Band(depths[i]), Exposure(depths[i]));

        var filter = GetComponent<MeshFilter>();
        filter.sharedMesh = mesh.Build("NetworkGraph");
        GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private float Exposure(float depth) => Mathf.Lerp(farExposure, 1f, depth);
}
