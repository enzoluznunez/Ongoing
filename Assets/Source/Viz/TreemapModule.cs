using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class TreemapModule : IChartModule
{
    private const float GroupGapShare = 0.015f;
    private const float TileGapShare = 0.006f;
    private const float PlateShare = 0.02f;

    private struct Rect2
    {
        public float x, z, width, depth;
    }

    private sealed class Item
    {
        public string Label;
        public double Size;
        public double Height;
        public int Slot;
        public List<Item> Children;
    }

    public ChartGeometry Build(ChartSpec spec, Blueprint blueprint, DataTable table)
    {
        DataColumn groupColumn = table.Column(spec.ColumnFor("group"));
        DataColumn labelColumn = table.Column(spec.ColumnFor("label"));
        DataColumn sizeColumn = table.Column(spec.ColumnFor("size"));
        DataColumn heightColumn = table.Column(spec.ColumnFor("height"));

        var groups = new List<Item>();
        if (groupColumn.IsNumber) throw new InvalidOperationException($"'{groupColumn.Name}' must hold names to group a treemap.");
        foreach (string category in groupColumn.Categories)
            groups.Add(new Item { Label = category, Slot = groups.Count, Children = new List<Item>() });

        double tallest = 1e-9;
        for (int r = 0; r < table.RowCount; r++)
        {
            int group = groupColumn.Codes[r];
            double size = sizeColumn.Numbers[r];
            if (group < 0 || double.IsNaN(size) || size <= 0d) continue;

            double value = heightColumn != null && !double.IsNaN(heightColumn.Numbers[r]) ? heightColumn.Numbers[r] : 0d;
            tallest = Math.Max(tallest, value);
            groups[group].Children.Add(new Item
            {
                Label = labelColumn.TextAt(r),
                Size = size,
                Height = value,
                Slot = groups[group].Slot
            });
            groups[group].Size += size;
        }
        groups.RemoveAll(g => g.Children.Count == 0);

        float footprint = (float)blueprint.LayoutValue("footprint", 0.6d);
        float depthShare = (float)blueprint.LayoutValue("depthShare", 0.6d);
        float height = (float)blueprint.LayoutValue("height", 0.14d);
        float halfX = footprint * 0.5f;
        float depth = footprint * depthShare;
        float halfZ = depth * 0.5f;
        float groupGap = footprint * GroupGapShare;
        float tileGap = footprint * TileGapShare;

        var geometry = new ChartGeometry
        {
            ColumnTitle = spec.TitleFor("x", groupColumn.Name),
            RowTitle = spec.TitleFor("z", null),
            HeightTitle = spec.TitleFor("height", heightColumn?.Name)
        };
        var scale = new AxisScale(0d, ChartAxes.NiceCeiling(tallest), 0f, height);

        double total = 0d;
        foreach (Item group in groups) total += group.Size;
        float x = -halfX;
        foreach (Item group in groups)
        {
            float width = (float)(group.Size / total) * footprint;
            float z = -halfZ;
            foreach (Item tile in group.Children)
            {
                float tileDepth = (float)(tile.Size / group.Size) * depth;
                Rect2 box = Inset(new Rect2 { x = x + groupGap * 0.5f, z = z, width = width - groupGap, depth = tileDepth }, tileGap * 0.5f);
                float top = Mathf.Max(scale.Place(tile.Height), height * PlateShare);
                float3 center = new float3(box.x + box.width * 0.5f, top * 0.5f, box.z + box.depth * 0.5f);
                geometry.Pending.Add(Mark.Upright(Glyphs.Box, center,
                    new float3(box.width, top, box.depth), tile.Slot, 1f));
                geometry.FreeLabels.Add(new FreeLabel
                {
                    text = tile.Label,
                    position = new Vector3(center.x, top + 0.012f, center.z)
                });
                z += tileDepth;
            }
            geometry.ColumnLabels.Add(new ChartLabel { text = group.Label, coord = x + width * 0.5f });
            x += width;
            if (x < halfX - 1e-4f) geometry.GridX.Add(x);
        }

        if (heightColumn != null) ChartAxes.AddTicks(geometry.HeightLabels, scale);
        geometry.AddCategoryLegend(groupColumn);
        ChartAxes.Finish(geometry, halfX, halfZ, height, footprint * 0.04f);
        return geometry;
    }

    private static Rect2 Inset(Rect2 rect, float by) => new Rect2
    {
        x = rect.x + by,
        z = rect.z + by,
        width = Mathf.Max(rect.width - by * 2f, 1e-4f),
        depth = Mathf.Max(rect.depth - by * 2f, 1e-4f)
    };
}
