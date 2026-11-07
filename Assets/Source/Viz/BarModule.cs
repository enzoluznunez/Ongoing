using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class BarModule : IChartModule
{
    private const int MostBars = 65000;
    private const float ZeroPlateShare = 0.008f;
    private const float DarkestShade = 0.35f;

    private static readonly float[] Lightness =
        { 0f, 0.127f, 0.233f, 0.346f, 0.466f, 0.593f, 0.724f, 0.860f, 1f };

    public ChartGeometry Build(ChartSpec spec, Blueprint blueprint, DataTable table)
    {
        DataColumn rows = table.Column(spec.ColumnFor("row"));
        DataColumn columns = table.Column(spec.ColumnFor("column"));
        DataColumn values = table.Column(spec.ColumnFor("height"));
        DataColumn colors = table.Column(spec.ColumnFor("color"));

        if (rows.IsNumber || columns.IsNumber)
            throw new InvalidOperationException($"The rows and columns of '{spec.Title}' must be named categories.");

        int rowCount = rows.Categories.Count;
        int columnCount = columns.Categories.Count;
        var totals = new double[rowCount, columnCount];
        var filled = new bool[rowCount, columnCount];
        var slots = new int[rowCount, columnCount];
        int cellCount = 0;

        for (int r = 0; r < table.RowCount; r++)
        {
            int row = rows.Codes[r], column = columns.Codes[r];
            double value = values.Numbers[r];
            if (row < 0 || column < 0 || double.IsNaN(value)) continue;

            if (!filled[row, column])
            {
                filled[row, column] = true;
                cellCount++;
                slots[row, column] = colors != null && !colors.IsNumber ? colors.Codes[r] : -1;
            }
            totals[row, column] += value;
        }
        if (cellCount > MostBars)
            throw new InvalidOperationException($"'{spec.Title}' would need {cellCount} bars; the limit is {MostBars}.");

        double low = 0d, high = 0d;
        for (int row = 0; row < rowCount; row++)
            for (int column = 0; column < columnCount; column++)
            {
                if (!filled[row, column]) continue;
                low = Math.Min(low, totals[row, column]);
                high = Math.Max(high, totals[row, column]);
            }
        high = ChartAxes.NiceCeiling(high);
        bool colorByValue = colors != null && colors.IsNumber;

        float footprint = (float)blueprint.LayoutValue("footprint", 0.6d);
        float height = (float)blueprint.LayoutValue("height", 0.3d);
        float fill = (float)blueprint.LayoutValue("fill", 0.7d);
        float mostCell = (float)blueprint.LayoutValue("maxCell", 0.12d);
        float cell = Mathf.Min(mostCell, footprint / Mathf.Max(rowCount, columnCount));
        float width = cell * fill;
        var scale = new AxisScale(low, high, 0f, height);
        float baseY = scale.Place(0d);

        var geometry = new ChartGeometry
        {
            BaseY = baseY,
            RowTitle = spec.TitleFor("row", rows.Name),
            ColumnTitle = spec.TitleFor("column", columns.Name),
            HeightTitle = spec.TitleFor("height", values.Name)
        };

        float halfX = columnCount * cell * 0.5f;
        float halfZ = rowCount * cell * 0.5f;
        float firstX = cell * 0.5f - halfX;
        float firstZ = cell * 0.5f - halfZ;
        for (int row = 0; row < rowCount; row++)
        {
            float z = row * cell + firstZ;
            for (int column = 0; column < columnCount; column++)
            {
                if (!filled[row, column]) continue;

                float top = scale.Place(totals[row, column]);
                float bottom = Mathf.Min(top, baseY);
                float upper = Mathf.Max(top, baseY);
                if (upper - bottom < height * ZeroPlateShare)
                {
                    float plate = height * ZeroPlateShare * 0.5f;
                    bottom = baseY - plate;
                    upper = baseY + plate;
                }

                float share = (float)(Math.Abs(totals[row, column]) / Math.Max(Math.Max(Math.Abs(low), high), 1e-9));
                geometry.Pending.Add(Mark.Upright(Glyphs.Box,
                    new float3(column * cell + firstX, (bottom + upper) * 0.5f, z),
                    new float3(width, upper - bottom, width),
                    colorByValue ? 0 : slots[row, column],
                    colorByValue ? Mathf.Lerp(DarkestShade, 1f, share) : Perceptual(share)));
            }
        }

        for (int row = 0; row < rowCount; row++)
            geometry.RowLabels.Add(new ChartLabel { text = rows.Categories[row], coord = row * cell + firstZ });
        for (int column = 0; column < columnCount; column++)
            geometry.ColumnLabels.Add(new ChartLabel { text = columns.Categories[column], coord = column * cell + firstX });
        for (int i = 1; i < columnCount; i++) geometry.GridX.Add(i * cell - halfX);
        for (int i = 1; i < rowCount; i++) geometry.GridZ.Add(i * cell - halfZ);
        ChartAxes.AddTicks(geometry.HeightLabels, scale);

        if (colorByValue) geometry.AddRampLegend(geometry.HeightTitle, 0d, high, 0, DarkestShade);
        else geometry.AddCategoryLegend(colors);

        ChartAxes.Finish(geometry, halfX, halfZ, height, cell * 0.25f);
        return geometry;
    }

    private static float Perceptual(float share)
    {
        share = Mathf.Clamp01(share);
        float scaled = share * (Lightness.Length - 1);
        int lower = Mathf.FloorToInt(scaled);
        int upper = Mathf.Min(lower + 1, Lightness.Length - 1);
        return Mathf.Lerp(Lightness[lower], Lightness[upper], scaled - lower);
    }
}
