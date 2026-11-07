using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Mathematics;
using UnityEngine;

public readonly struct AxisScale
{
    public readonly double Low;
    public readonly double High;
    public readonly float From;
    public readonly float To;

    public AxisScale(double low, double high, float from, float to)
    {
        Low = low;
        High = high > low ? high : low + 1d;
        From = from;
        To = to;
    }

    public float Place(double value) => From + (float)((value - Low) / (High - Low)) * (To - From);
}

public static class ChartAxes
{
    private const int TickTarget = 5;

    public static string Format(double value)
    {
        if (ChartDisplay.FullNumbers) return value.ToString("#,0.##", CultureInfo.InvariantCulture);
        double size = Math.Abs(value);
        if (size >= 1e6) return (value / 1e6).ToString("0.##", CultureInfo.InvariantCulture) + "M";
        if (size >= 1e4) return (value / 1e3).ToString("0.#", CultureInfo.InvariantCulture) + "k";
        if (size >= 100) return value.ToString("0", CultureInfo.InvariantCulture);
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static double NiceStep(double low, double high, int count)
    {
        double rough = Math.Max(high - low, 1e-9) / Math.Max(count, 1);
        double power = Math.Pow(10d, Math.Floor(Math.Log10(rough)));
        double share = rough / power;
        double nice = share <= 1d ? 1d : share <= 2d ? 2d : share <= 2.5d ? 2.5d : share <= 5d ? 5d : 10d;
        return nice * power;
    }

    public static List<double> Ticks(double low, double high)
    {
        double step = NiceStep(low, high, TickTarget);
        var ticks = new List<double>();
        double at = Math.Ceiling(low / step - 1e-9) * step;
        for (int guard = 0; at <= high + step * 1e-6 && guard < 32; guard++, at += step)
            ticks.Add(Math.Abs(at) < step * 1e-9 ? 0d : at);
        return ticks;
    }

    public static void AddTicks(List<ChartLabel> labels, AxisScale scale)
    {
        foreach (double tick in Ticks(scale.Low, scale.High))
            labels.Add(new ChartLabel { text = Format(tick), coord = scale.Place(tick) });
    }

    public static double NiceCeiling(double value)
    {
        double step = NiceStep(0d, Math.Max(value, 1e-9), TickTarget);
        return Math.Ceiling(value / step - 1e-9) * step;
    }

    public static void GridFromTicks(List<float> grid, List<ChartLabel> labels)
    {
        foreach (ChartLabel label in labels) grid.Add(label.coord);
    }

    public static void Finish(ChartGeometry geometry, float halfX, float halfZ, float height, float gap)
    {
        geometry.Height = height;
        geometry.LabelGap = gap;
        geometry.Bounds = new Bounds(new Vector3(0f, height * 0.5f, 0f), new Vector3(halfX * 2f, height, halfZ * 2f));
        if (geometry.GridY.Count == 0) GridFromTicks(geometry.GridY, geometry.HeightLabels);
        geometry.Seal();
    }
}
