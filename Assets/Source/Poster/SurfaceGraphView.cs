using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SurfaceGraphView : MonoBehaviour
{
    [Tooltip("Cells along each axis of the floor.")]
    public int cells = 288;

    [Tooltip("Floor extent in world units, along x and along z.")]
    public Vector2 extent = new Vector2(2.4f, 2.0f);

    [Tooltip("World height from the floor of the scoop to its highest lip.")]
    public float height = 1.05f;

    public void Build(Material material)
    {
        var mesh = new PosterMesh();
        int n = Mathf.Max(cells, 2);

        for (int ix = 0; ix < n; ix++)
        {
            for (int iz = 0; iz < n; iz++)
            {
                float u0 = (float)ix / n, u1 = (float)(ix + 1) / n;
                float v0 = (float)iz / n, v1 = (float)(iz + 1) / n;

                Vector3 a = Point(u0, v0);
                Vector3 b = Point(u1, v0);
                Vector3 c = Point(u1, v1);
                Vector3 d = Point(u0, v1);

                Vector3 normal = Vector3.Cross(c - a, d - b).normalized;
                if (normal.y < 0f) normal = -normal;

                float depth = Depth((u0 + u1) * 0.5f, (v0 + v1) * 0.5f);
                Color color = PosterPalette.Band(depth);
                mesh.AddQuad(a, b, c, d, PosterMesh.Shade(color, normal, Exposure(depth)));
            }
        }

        var filter = GetComponent<MeshFilter>();
        filter.sharedMesh = mesh.Build("SurfaceGraph");
        GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private Vector3 Point(float u, float v) =>
        new Vector3((u - 0.5f) * extent.x, Depth(u, v) * height, (v - 0.5f) * extent.y);

    private static float Depth(float u, float v)
    {
        float dx = (u - 0.5f) * 2f;
        float dz = (v - 0.5f) * 2f;

        float r = Mathf.Sqrt(dx * dx + 0.9f * dz * dz) / 1.34f;
        float wall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(r));
        return Mathf.Clamp01(0.90f * wall + 0.05f * dx + 0.02f * dz);
    }

    private static float Exposure(float depth) => Mathf.Lerp(0.46f, 1f, depth);
}
