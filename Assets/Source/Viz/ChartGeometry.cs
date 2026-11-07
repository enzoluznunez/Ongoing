using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct Mark
{
    public float3 center;
    public float3 size;
    public quaternion rotation;
    public int glyph;
    public int colorSlot;
    public float shade;
    public int firstVertex;
    public int firstIndex;

    public static Mark Upright(int glyph, float3 center, float3 size, int colorSlot, float shade) => new Mark
    {
        glyph = glyph,
        center = center,
        size = size,
        rotation = quaternion.identity,
        colorSlot = colorSlot,
        shade = shade
    };

    public static quaternion TurnUpTo(float3 direction)
    {
        float3 up = new float3(0f, 1f, 0f);
        float3 to = math.normalizesafe(direction, up);
        float along = math.dot(up, to);
        if (along < -0.9999f) return quaternion.AxisAngle(new float3(1f, 0f, 0f), math.PI);
        return math.normalize(new quaternion(new float4(math.cross(up, to), 1f + along)));
    }
}

public struct ChartLabel
{
    public string text;
    public float coord;
}

public struct FreeLabel
{
    public string text;
    public Vector3 position;
}

public struct LegendEntry
{
    public int slot;
    public float shade;
    public string label;
}

public sealed class ChartGeometry : IDisposable
{
    public readonly List<Mark> Pending = new List<Mark>();
    public NativeArray<Mark> Marks;
    public int VertexCount;
    public int IndexCount;
    public Bounds Bounds;
    public float BaseY;
    public float Height;
    public float LabelGap;
    public string RowTitle;
    public string ColumnTitle;
    public string HeightTitle;
    public string Note;
    public string LegendTitle;
    public readonly List<ChartLabel> RowLabels = new List<ChartLabel>();
    public readonly List<ChartLabel> ColumnLabels = new List<ChartLabel>();
    public readonly List<ChartLabel> HeightLabels = new List<ChartLabel>();
    public readonly List<float> GridX = new List<float>();
    public readonly List<float> GridY = new List<float>();
    public readonly List<float> GridZ = new List<float>();
    public readonly List<FreeLabel> FreeLabels = new List<FreeLabel>();
    public readonly List<LegendEntry> Legend = new List<LegendEntry>();

    public void Seal()
    {
        Marks = new NativeArray<Mark>(Pending.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        int vertices = 0, indices = 0;
        for (int i = 0; i < Pending.Count; i++)
        {
            Mark mark = Pending[i];
            mark.firstVertex = vertices;
            mark.firstIndex = indices;
            vertices += Glyphs.VertexCount(mark.glyph);
            indices += Glyphs.IndexCount(mark.glyph);
            Marks[i] = mark;
        }
        VertexCount = vertices;
        IndexCount = indices;
        Pending.Clear();
    }

    public void AddCategoryLegend(DataColumn column)
    {
        if (column == null || column.IsNumber) return;
        for (int i = 0; i < column.Categories.Count; i++)
            Legend.Add(new LegendEntry { slot = i, shade = 1f, label = column.Categories[i] });
    }

    public void AddRampLegend(string title, double low, double high, int slot, float darkest)
    {
        const int Steps = 5;
        LegendTitle = title;
        for (int step = 0; step < Steps; step++)
        {
            float share = step / (float)(Steps - 1);
            double value = low + (high - low) * share;
            Legend.Add(new LegendEntry
            {
                slot = slot,
                shade = Mathf.Lerp(darkest, 1f, share),
                label = ChartAxes.Format(value)
            });
        }
    }

    public void Dispose()
    {
        if (Marks.IsCreated) Marks.Dispose();
    }
}

public interface IChartModule
{
    ChartGeometry Build(ChartSpec spec, Blueprint blueprint, DataTable table);
}

public static class ChartModules
{
    private static readonly Dictionary<string, IChartModule> Registry = new Dictionary<string, IChartModule>
    {
        { "bar", new BarModule() },
        { "histogram", new HistogramModule() },
        { "scatter", new ScatterModule() },
        { "treemap", new TreemapModule() },
        { "vectorfield", new VectorFieldModule() },
        { "network", new NetworkModule() }
    };

    public static IChartModule For(string kind) =>
        Registry.TryGetValue(kind, out IChartModule module) ? module : null;
}
