using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class Glyphs
{
    public const int Box = 0;
    public const int Sphere = 1;
    public const int SphereLow = 2;
    public const int Arrow = 3;

    private const int ArrowSides = 6;
    private const float ArrowShaftRadius = 0.18f;
    private const float ArrowHeadRadius = 0.5f;
    private const float ArrowShaftLength = 0.68f;

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public float3 position;
        public byte shade;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Range
    {
        public int firstVertex;
        public int vertexCount;
        public int firstIndex;
        public int indexCount;
    }

    private sealed class Shape
    {
        public readonly List<Vertex> Vertices = new List<Vertex>();
        public readonly List<uint> Indices = new List<uint>();

        public void Quad(float3 a, float3 b, float3 c, float3 d, float3 outward)
        {
            int first = Vertices.Count;
            byte shade = ShadeOf(outward);
            Vertices.Add(new Vertex { position = a, shade = shade });
            Vertices.Add(new Vertex { position = b, shade = shade });
            Vertices.Add(new Vertex { position = c, shade = shade });
            Vertices.Add(new Vertex { position = d, shade = shade });
            Wind(first, first + 1, first + 2, outward);
            Wind(first, first + 2, first + 3, outward);
        }

        public void Triangle(float3 a, float3 b, float3 c, float3 outward)
        {
            int first = Vertices.Count;
            byte shade = ShadeOf(outward);
            Vertices.Add(new Vertex { position = a, shade = shade });
            Vertices.Add(new Vertex { position = b, shade = shade });
            Vertices.Add(new Vertex { position = c, shade = shade });
            Wind(first, first + 1, first + 2, outward);
        }

        public void Wind(int a, int b, int c, float3 outward)
        {
            float3 pa = Vertices[a].position, pb = Vertices[b].position, pc = Vertices[c].position;
            bool facing = math.dot(math.cross(pb - pa, pc - pa), outward) > 0f;
            Indices.Add((uint)a);
            Indices.Add((uint)(facing ? b : c));
            Indices.Add((uint)(facing ? c : b));
        }
    }

    private static readonly Shape[] Shapes =
    {
        MakeBox(),
        MakeSphere(2),
        MakeSphere(0),
        MakeArrow()
    };

    private static readonly Vertex[] AllVertices;
    private static readonly uint[] AllIndices;
    private static readonly Range[] AllRanges;

    static Glyphs()
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        AllRanges = new Range[Shapes.Length];
        for (int i = 0; i < Shapes.Length; i++)
        {
            AllRanges[i] = new Range
            {
                firstVertex = vertices.Count,
                vertexCount = Shapes[i].Vertices.Count,
                firstIndex = indices.Count,
                indexCount = Shapes[i].Indices.Count
            };
            vertices.AddRange(Shapes[i].Vertices);
            indices.AddRange(Shapes[i].Indices);
        }
        AllVertices = vertices.ToArray();
        AllIndices = indices.ToArray();
    }

    public static int VertexCount(int glyph) => AllRanges[glyph].vertexCount;

    public static int IndexCount(int glyph) => AllRanges[glyph].indexCount;

    public static NativeArray<Vertex> TemplateVertices() => new NativeArray<Vertex>(AllVertices, Allocator.TempJob);

    public static NativeArray<uint> TemplateIndices() => new NativeArray<uint>(AllIndices, Allocator.TempJob);

    public static NativeArray<Range> TemplateRanges() => new NativeArray<Range>(AllRanges, Allocator.TempJob);

    private static byte ShadeOf(float3 outward)
    {
        float3 n = math.normalizesafe(outward);
        float light = 0.55f + 0.45f * (n.y * 0.5f + 0.5f);
        light -= 0.12f * math.abs(n.x);
        return (byte)(math.saturate(light) * 255f);
    }

    private static Shape MakeBox()
    {
        var shape = new Shape();
        float3 C(float x, float y, float z) => new float3(x, y, z) * 0.5f;
        shape.Quad(C(-1, -1, -1), C(1, -1, -1), C(1, -1, 1), C(-1, -1, 1), new float3(0, -1, 0));
        shape.Quad(C(-1, 1, -1), C(1, 1, -1), C(1, 1, 1), C(-1, 1, 1), new float3(0, 1, 0));
        shape.Quad(C(-1, -1, -1), C(1, -1, -1), C(1, 1, -1), C(-1, 1, -1), new float3(0, 0, -1));
        shape.Quad(C(-1, -1, 1), C(1, -1, 1), C(1, 1, 1), C(-1, 1, 1), new float3(0, 0, 1));
        shape.Quad(C(-1, -1, -1), C(-1, -1, 1), C(-1, 1, 1), C(-1, 1, -1), new float3(-1, 0, 0));
        shape.Quad(C(1, -1, -1), C(1, -1, 1), C(1, 1, 1), C(1, 1, -1), new float3(1, 0, 0));
        return shape;
    }

    private static Shape MakeSphere(int subdivisions)
    {
        float t = (1f + math.sqrt(5f)) * 0.5f;
        var points = new List<float3>
        {
            new float3(-1, t, 0), new float3(1, t, 0), new float3(-1, -t, 0), new float3(1, -t, 0),
            new float3(0, -1, t), new float3(0, 1, t), new float3(0, -1, -t), new float3(0, 1, -t),
            new float3(t, 0, -1), new float3(t, 0, 1), new float3(-t, 0, -1), new float3(-t, 0, 1)
        };
        for (int i = 0; i < points.Count; i++) points[i] = math.normalize(points[i]);

        var faces = new List<int3>
        {
            new int3(0, 11, 5), new int3(0, 5, 1), new int3(0, 1, 7), new int3(0, 7, 10), new int3(0, 10, 11),
            new int3(1, 5, 9), new int3(5, 11, 4), new int3(11, 10, 2), new int3(10, 7, 6), new int3(7, 1, 8),
            new int3(3, 9, 4), new int3(3, 4, 2), new int3(3, 2, 6), new int3(3, 6, 8), new int3(3, 8, 9),
            new int3(4, 9, 5), new int3(2, 4, 11), new int3(6, 2, 10), new int3(8, 6, 7), new int3(9, 8, 1)
        };

        for (int level = 0; level < subdivisions; level++)
        {
            var middles = new Dictionary<long, int>();
            int Middle(int a, int b)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (middles.TryGetValue(key, out int at)) return at;
                at = points.Count;
                points.Add(math.normalize(points[a] + points[b]));
                middles[key] = at;
                return at;
            }

            var finer = new List<int3>(faces.Count * 4);
            foreach (int3 face in faces)
            {
                int ab = Middle(face.x, face.y), bc = Middle(face.y, face.z), ca = Middle(face.z, face.x);
                finer.Add(new int3(face.x, ab, ca));
                finer.Add(new int3(face.y, bc, ab));
                finer.Add(new int3(face.z, ca, bc));
                finer.Add(new int3(ab, bc, ca));
            }
            faces = finer;
        }

        var shape = new Shape();
        foreach (float3 point in points)
            shape.Vertices.Add(new Vertex { position = point * 0.5f, shade = ShadeOf(point) });
        foreach (int3 face in faces)
            shape.Wind(face.x, face.y, face.z, points[face.x] + points[face.y] + points[face.z]);
        return shape;
    }

    private static Shape MakeArrow()
    {
        var shape = new Shape();
        float3 Ring(float radius, float y, int side)
        {
            float angle = side * 2f * math.PI / ArrowSides;
            return new float3(math.cos(angle) * radius, y, math.sin(angle) * radius);
        }

        float3 tip = new float3(0f, 1f, 0f);
        for (int side = 0; side < ArrowSides; side++)
        {
            int next = side + 1;
            float mid = (side + 0.5f) * 2f * math.PI / ArrowSides;
            float3 outward = new float3(math.cos(mid), 0f, math.sin(mid));

            shape.Quad(Ring(ArrowShaftRadius, 0f, side), Ring(ArrowShaftRadius, 0f, next),
                Ring(ArrowShaftRadius, ArrowShaftLength, next), Ring(ArrowShaftRadius, ArrowShaftLength, side), outward);
            shape.Triangle(Ring(ArrowHeadRadius, ArrowShaftLength, side), Ring(ArrowHeadRadius, ArrowShaftLength, next), tip,
                outward + new float3(0f, 0.6f, 0f));
            shape.Quad(Ring(ArrowShaftRadius, ArrowShaftLength, side), Ring(ArrowShaftRadius, ArrowShaftLength, next),
                Ring(ArrowHeadRadius, ArrowShaftLength, next), Ring(ArrowHeadRadius, ArrowShaftLength, side), new float3(0f, -1f, 0f));
        }
        return shape;
    }
}
