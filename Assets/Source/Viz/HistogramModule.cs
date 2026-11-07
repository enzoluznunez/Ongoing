using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class HistogramModule : IChartModule
{
    private const int MostDiscreteBins = 24;
    private const int ContinuousBins = 12;
    private const float DarkestShade = 0.4f;

    private readonly struct Binning
    {
        public readonly double Low;
        public readonly double Step;
        public readonly int Count;

        public Binning(double low, double step, int count)
        {
            Low = low;
            Step = step;
            Count = count;
        }

        public int BinOf(double value) => Math.Min(Count - 1, Math.Max(0, (int)Math.Floor((value - Low) / Step)));
    }

    public ChartGeometry Build(ChartSpec spec, Blueprint blueprint, DataTable table)
    {
        DataColumn xColumn = table.Column(spec.ColumnFor("x"));
        DataColumn zColumn = table.Column(spec.ColumnFor("z"));

        Binning xBins = BinningFor(xColumn);
        Binning zBins = BinningFor(zColumn);
        var counts = new int[xBins.Count, zBins.Count];
        int most = 0;
        for (int r = 0; r < table.RowCount; r++)
        {
            double x = xColumn.Numbers[r], z = zColumn.Numbers[r];
            if (double.IsNaN(x) || double.IsNaN(z)) continue;
            int count = ++counts[xBins.BinOf(x), zBins.BinOf(z)];
            if (count > most) most = count;
        }

        float footprint = (float)blueprint.LayoutValue("footprint", 0.6d);
        float height = (float)blueprint.LayoutValue("height", 0.3d);
        float fill = (float)blueprint.LayoutValue("fill", 0.88d);
        float cellX = footprint / xBins.Count, cellZ = footprint / zBins.Count;
        float half = footprint * 0.5f;

        var geometry = new ChartGeometry
        {
            ColumnTitle = spec.TitleFor("x", xColumn.Name),
            RowTitle = spec.TitleFor("z", zColumn.Name),
            HeightTitle = spec.TitleFor("height", "Count")
        };

        var yScale = new AxisScale(0d, ChartAxes.NiceCeiling(Math.Max(most, 1)), 0f, height);
        for (int ix = 0; ix < xBins.Count; ix++)
            for (int iz = 0; iz < zBins.Count; iz++)
            {
                int count = counts[ix, iz];
                if (count == 0) continue;
                float top = yScale.Place(count);
                geometry.Pending.Add(Mark.Upright(Glyphs.Box,
                    new float3(-half + (ix + 0.5f) * cellX, top * 0.5f, -half + (iz + 0.5f) * cellZ),
                    new float3(cellX * fill, top, cellZ * fill),
                    0,
                    Mathf.Lerp(DarkestShade, 1f, count / (float)Math.Max(most, 1))));
            }

        for (int i = 1; i < xBins.Count; i++) geometry.GridX.Add(-half + i * cellX);
        for (int i = 1; i < zBins.Count; i++) geometry.GridZ.Add(-half + i * cellZ);
        AddBinTicks(geometry.ColumnLabels, xBins, -half, cellX);
        AddBinTicks(geometry.RowLabels, zBins, -half, cellZ);
        ChartAxes.AddTicks(geometry.HeightLabels, yScale);
        geometry.AddRampLegend(geometry.HeightTitle, 0d, most, 0, DarkestShade);
        ChartAxes.Finish(geometry, half, half, height, Mathf.Min(cellX, cellZ) * 0.4f);
        return geometry;
    }

    private static Binning BinningFor(DataColumn column)
    {
        if (!column.IsNumber) throw new InvalidOperationException($"'{column.Name}' must hold numbers to make a histogram.");

        double low = column.Min, high = column.Max;
        if (IsWhole(column) && high - low + 1d <= MostDiscreteBins)
            return new Binning(low - 0.5d, 1d, (int)(high - low) + 1);

        double step = ChartAxes.NiceStep(low, high, ContinuousBins);
        double start = Math.Floor(low / step) * step;
        int count = Math.Max(1, (int)Math.Ceiling((high - start) / step));
        return new Binning(start, step, count);
    }

    private static bool IsWhole(DataColumn column)
    {
        foreach (double value in column.Numbers)
            if (!double.IsNaN(value) && Math.Abs(value - Math.Round(value)) > 1e-9) return false;
        return true;
    }

    private static void AddBinTicks(List<ChartLabel> labels, Binning bins, float from, float cell)
    {
        int stride = Math.Max(1, (int)Math.Ceiling(bins.Count / 6d));
        for (int bin = 0; bin < bins.Count; bin += stride)
            labels.Add(new ChartLabel
            {
                text = ChartAxes.Format(bins.Low + bins.Step * (bin + 0.5d)),
                coord = from + (bin + 0.5f) * cell
            });
    }
}
