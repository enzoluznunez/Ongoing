using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class VectorFieldModule : IChartModule
{
    private const int MostArrows = 10000;
    private const float DarkestShade = 0.35f;

    public ChartGeometry Build(ChartSpec spec, Blueprint blueprint, DataTable table)
    {
        DataColumn xColumn = table.Column(spec.ColumnFor("x"));
        DataColumn yColumn = table.Column(spec.ColumnFor("y"));
        DataColumn zColumn = table.Column(spec.ColumnFor("z"));
        DataColumn uColumn = table.Column(spec.ColumnFor("u"));
        DataColumn vColumn = table.Column(spec.ColumnFor("v"));
        DataColumn wColumn = table.Column(spec.ColumnFor("w"));
        DataColumn colorColumn = table.Column(spec.ColumnFor("color"));

        float footprint = (float)blueprint.LayoutValue("footprint", 0.6d);
        float height = (float)blueprint.LayoutValue("height", 0.4d);
        float half = footprint * 0.5f;

        var x = new AxisScale(xColumn.Min, xColumn.Max, -half, half);
        var y = new AxisScale(yColumn.Min, yColumn.Max, 0f, height);
        var z = new AxisScale(zColumn.Min, zColumn.Max, -half, half);

        var speeds = new double[table.RowCount];
        double fastest = 1e-9;
        for (int r = 0; r < table.RowCount; r++)
        {
            double u = uColumn.Numbers[r], v = vColumn.Numbers[r], w = wColumn.Numbers[r];
            speeds[r] = Math.Sqrt(u * u + v * v + w * w);
            if (speeds[r] > fastest) fastest = speeds[r];
        }

        float spacing = footprint / Math.Max(1, Distinct(xColumn) - 1);
        float longest = spacing * (float)blueprint.LayoutValue("longestShare", 0.9d);
        float thickest = longest * (float)blueprint.LayoutValue("widthShare", 0.3d);

        var geometry = new ChartGeometry
        {
            ColumnTitle = spec.TitleFor("x", xColumn.Name),
            HeightTitle = spec.TitleFor("y", yColumn.Name),
            RowTitle = spec.TitleFor("z", zColumn.Name)
        };

        int stride = Math.Max(1, (int)Math.Ceiling(table.RowCount / (double)MostArrows));
        if (stride > 1) geometry.Note = $"Showing every {stride}th of {table.RowCount} rows.";

        double rampLow = colorColumn != null && colorColumn.IsNumber ? colorColumn.Min : 0d;
        double rampHigh = colorColumn != null && colorColumn.IsNumber ? colorColumn.Max : fastest;
        for (int r = 0; r < table.RowCount; r += stride)
        {
            float length = longest * (float)(speeds[r] / fastest);
            if (length <= 1e-5f) continue;

            float3 direction = new float3((float)uColumn.Numbers[r], (float)wColumn.Numbers[r], (float)vColumn.Numbers[r]);
            double rampValue = colorColumn != null && colorColumn.IsNumber ? colorColumn.Numbers[r] : speeds[r];
            float share = (float)((rampValue - rampLow) / Math.Max(rampHigh - rampLow, 1e-9));

            geometry.Pending.Add(new Mark
            {
                glyph = Glyphs.Arrow,
                center = new float3(x.Place(xColumn.Numbers[r]), y.Place(yColumn.Numbers[r]), z.Place(zColumn.Numbers[r])),
                size = new float3(thickest, length, thickest),
                rotation = Mark.TurnUpTo(direction),
                colorSlot = 0,
                shade = Mathf.Lerp(DarkestShade, 1f, Mathf.Clamp01(share))
            });
        }

        ChartAxes.AddTicks(geometry.ColumnLabels, x);
        ChartAxes.AddTicks(geometry.RowLabels, z);
        ChartAxes.AddTicks(geometry.HeightLabels, y);
        ChartAxes.GridFromTicks(geometry.GridX, geometry.ColumnLabels);
        ChartAxes.GridFromTicks(geometry.GridZ, geometry.RowLabels);
        geometry.AddRampLegend(spec.TitleFor("color", "Speed"), rampLow, rampHigh, 0, DarkestShade);
        ChartAxes.Finish(geometry, half, half, height, footprint * 0.04f);
        return geometry;
    }

    private static int Distinct(DataColumn column)
    {
        var seen = new HashSet<double>();
        foreach (double value in column.Numbers)
            if (!double.IsNaN(value)) seen.Add(value);
        return seen.Count;
    }
}
