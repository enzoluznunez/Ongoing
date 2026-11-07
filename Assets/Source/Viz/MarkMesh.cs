using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class MarkMeshBuild
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public float3 position;
        public Color32 color;
    }

    [BurstCompile]
    private struct WriteMarks : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Mark> marks;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Color32> palette;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Glyphs.Vertex> templateVertices;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<uint> templateIndices;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Glyphs.Range> ranges;
        [NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction, WriteOnly] public NativeArray<Vertex> vertices;
        [NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction, WriteOnly] public NativeArray<uint> indices;

        public void Execute(int at)
        {
            Mark mark = marks[at];
            Glyphs.Range range = ranges[mark.glyph];
            Color32 color = Shaded(mark);

            for (int v = 0; v < range.vertexCount; v++)
            {
                Glyphs.Vertex template = templateVertices[range.firstVertex + v];
                color.a = template.shade;
                vertices[mark.firstVertex + v] = new Vertex
                {
                    position = mark.center + math.rotate(mark.rotation, template.position * mark.size),
                    color = color
                };
            }

            for (int i = 0; i < range.indexCount; i++)
                indices[mark.firstIndex + i] = (uint)mark.firstVertex + templateIndices[range.firstIndex + i];
        }

        private Color32 Shaded(Mark mark)
        {
            Color32 top = mark.colorSlot < 0 || palette.Length == 0
                ? new Color32(255, 255, 255, 255)
                : palette[mark.colorSlot % palette.Length];
            return new Color32(
                (byte)(top.r * mark.shade),
                (byte)(top.g * mark.shade),
                (byte)(top.b * mark.shade),
                255);
        }
    }

    private readonly Mesh _mesh;
    private readonly Bounds _bounds;
    private readonly int _indexCount;
    private readonly int _vertexCount;
    private Mesh.MeshDataArray _writable;
    private JobHandle _handle;
    private bool _open;

    public bool Done => !_open || _handle.IsCompleted;

    public MarkMeshBuild(Mesh mesh, ChartGeometry geometry, Color32[] colors)
    {
        _mesh = mesh;
        _bounds = geometry.Bounds;
        _vertexCount = geometry.VertexCount;
        _indexCount = geometry.IndexCount;

        _writable = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData data = _writable[0];
        data.SetVertexBufferParams(_vertexCount,
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4));
        data.SetIndexBufferParams(_indexCount, IndexFormat.UInt32);

        _handle = new WriteMarks
        {
            marks = geometry.Marks,
            palette = new NativeArray<Color32>(colors, Allocator.TempJob),
            templateVertices = Glyphs.TemplateVertices(),
            templateIndices = Glyphs.TemplateIndices(),
            ranges = Glyphs.TemplateRanges(),
            vertices = data.GetVertexData<Vertex>(),
            indices = data.GetIndexData<uint>()
        }.Schedule(geometry.Marks.Length, 128);
        JobHandle.ScheduleBatchedJobs();
        _open = true;
    }

    public void Finish()
    {
        if (!_open) return;
        _open = false;
        _handle.Complete();

        const MeshUpdateFlags Quick = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices |
                                      MeshUpdateFlags.DontNotifyMeshUsers;
        Mesh.MeshData data = _writable[0];
        data.subMeshCount = 1;
        data.SetSubMesh(0, new SubMeshDescriptor(0, _indexCount)
        {
            bounds = _bounds,
            vertexCount = _vertexCount
        }, Quick);
        Mesh.ApplyAndDisposeWritableMeshData(_writable, _mesh, Quick);
        _mesh.bounds = _bounds;
    }
}
