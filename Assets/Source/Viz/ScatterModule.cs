using System;
using Unity.Mathematics;
using UnityEngine;

public sealed class ScatterModule : IChartModule
{
    private const int MostPoints = 20000;
    private const int FinePointLimit = 2000;

    public ChartGeometry Build(ChartSpec spec, Blueprint blueprint, DataTable table)
    {
        DataColumn xColumn = table.Column(spec.ColumnFor("x"));
        DataColumn yColumn = table.Column(spec.ColumnFor("y"));
        DataColumn zColumn = table.Column(spec.ColumnFor("z"));
        DataColumn sizeColumn = table.Column(spec.ColumnFor("size"));
        DataColumn colorColumn = table.Column(spec.ColumnFor("color"));

        float footprint = (float)blueprint.LayoutValue("footprint", 0.6d);
        float height = (float)blueprint.LayoutValue("height", 0.4d);
        float smallest = (float)blueprint.LayoutValue("smallestDiameter", 0.012d);
        float largest = (float)blueprint.LayoutValue("largestDiameter", 0.05d);
        float half = footprint * 0.5f;

        var x = new AxisScale(xColumn.Min, xColumn.Max, -half, half);
        var y = new AxisScale(yColumn.Min, yColumn.Max, 0f, height);
        var z = new AxisScale(zColumn.Min, zColumn.Max, -half, half);

        var geometry = new ChartGeometry
        {
            ColumnTitle = spec.TitleFor("x", xColumn.Name),
            HeightTitle = spec.TitleFor("y", yColumn.Name),
            RowTitle = spec.TitleFor("z", zColumn.Name)
        };

        int stride = Math.Max(1, (int)Math.Ceiling(table.RowCount / (double)MostPoints));
        if (stride > 1) geometry.Note = $"Showing every {stride}th of {table.RowCount} rows.";
        int glyph = table.RowCount / stride > FinePointLimit ? Glyphs.SphereLow : Glyphs.Sphere;

        for (int r = 0; r < table.RowCount; r += stride)
        {
            double vx = xColumn.Numbers[r], vy = yColumn.Numbers[r], vz = zColumn.Numbers[r];
            if (double.IsNaN(vx) || double.IsNaN(vy) || double.IsNaN(vz)) continue;

            float diameter = smallest;
            if (sizeColumn != null && !double.IsNaN(sizeColumn.Numbers[r]))
            {
                double span = Math.Max(sizeColumn.Max - sizeColumn.Min, 1e-9);
                float share = (float)((sizeColumn.Numbers[r] - sizeColumn.Min) / span);
                diameter = Mathf.Lerp(smallest, largest, Mathf.Sqrt(Mathf.Clamp01(share)));
            }

            int slot = colorColumn != null && !colorColumn.IsNumber ? colorColumn.Codes[r] : 0;
            geometry.Pending.Add(Mark.Upright(glyph, new float3(x.Place(vx), y.Place(vy), z.Place(vz)),
                new float3(diameter), slot, 1f));
        }

        ChartAxes.AddTicks(geometry.ColumnLabels, x);
        ChartAxes.AddTicks(geometry.RowLabels, z);
        ChartAxes.AddTicks(geometry.HeightLabels, y);
        ChartAxes.GridFromTicks(geometry.GridX, geometry.ColumnLabels);
        ChartAxes.GridFromTicks(geometry.GridZ, geometry.RowLabels);
        geometry.AddCategoryLegend(colorColumn);
        ChartAxes.Finish(geometry, half, half, height, footprint * 0.04f);
        return geometry;
    }
}
