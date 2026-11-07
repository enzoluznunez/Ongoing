using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class NetworkModule : IChartModule
{
    private const int MostNodes = 2000;
    private const int Iterations = 300;
    private const int MostHubs = 8;
    private const double HubDeviations = 2d;
    private const float BeamThickness = 0.002f;
    private const float PrimaryBeamShade = 0.8f;
    private const float OtherBeamShade = 0.45f;
    private const int LayoutSeed = 7;

    public ChartGeometry Build(ChartSpec spec, Blueprint blueprint, DataTable table)
    {
        DataColumn sourceColumn = table.Column(spec.ColumnFor("source"));
        DataColumn targetColumn = table.Column(spec.ColumnFor("target"));
        DataColumn sourceGroup = table.Column(spec.ColumnFor("sourceGroup"));
        DataColumn targetGroup = table.Column(spec.ColumnFor("targetGroup"));
        DataColumn tieColumn = table.Column(spec.ColumnFor("tie"));

        var ids = new Dictionary<string, int>();
        var names = new List<string>();
        var groupNames = new List<string>();
        var groupIds = new Dictionary<string, int>();
        var groups = new List<int>();

        int NodeOf(string name, string group)
        {
            if (ids.TryGetValue(name, out int id)) return id;
            id = names.Count;
            ids[name] = id;
            names.Add(name);
            if (group == null) group = "";
            if (!groupIds.TryGetValue(group, out int slot))
            {
                slot = groupNames.Count;
                groupIds[group] = slot;
                groupNames.Add(group);
            }
            groups.Add(slot);
            return id;
        }

        var edges = new List<(int from, int to, int tie)>();
        for (int r = 0; r < table.RowCount; r++)
        {
            string from = sourceColumn.TextAt(r), to = targetColumn.TextAt(r);
            if (from == null || to == null) continue;
            int a = NodeOf(from, sourceGroup?.TextAt(r));
            int b = NodeOf(to, targetGroup?.TextAt(r));
            if (a == b) continue;
            edges.Add((a, b, tieColumn != null && !tieColumn.IsNumber ? tieColumn.Codes[r] : 0));
        }
        if (names.Count > MostNodes)
            throw new InvalidOperationException($"'{spec.Title}' has {names.Count} people; the limit is {MostNodes}.");

        float footprint = (float)blueprint.LayoutValue("footprint", 0.6d);
        float height = (float)blueprint.LayoutValue("height", 0.45d);
        float smallest = (float)blueprint.LayoutValue("smallestDiameter", 0.02d);
        float largest = (float)blueprint.LayoutValue("largestDiameter", 0.055d);
        float half = footprint * 0.5f;

        var degree = new int[names.Count];
        foreach ((int from, int to, int _) in edges)
        {
            degree[from]++;
            degree[to]++;
        }
        int busiest = 1;
        foreach (int d in degree) busiest = Math.Max(busiest, d);

        float3[] positions = Fit(Layout(names.Count, edges), half, height);

        var geometry = new ChartGeometry();
        foreach ((int from, int to, int tie) in edges)
        {
            float3 a = positions[from], b = positions[to];
            float3 line = b - a;
            geometry.Pending.Add(new Mark
            {
                glyph = Glyphs.Box,
                center = (a + b) * 0.5f,
                size = new float3(BeamThickness, math.length(line), BeamThickness),
                rotation = Mark.TurnUpTo(line),
                colorSlot = -1,
                shade = tie == 0 ? OtherBeamShade : PrimaryBeamShade
            });
        }

        var diameters = new float[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            diameters[i] = Mathf.Lerp(smallest, largest, degree[i] / (float)busiest);
            geometry.Pending.Add(Mark.Upright(Glyphs.Sphere, positions[i], new float3(diameters[i]), groups[i], 1f));
        }

        double mean = 0d, spread = 0d;
        foreach (int d in degree) mean += d;
        mean /= Math.Max(degree.Length, 1);
        foreach (int d in degree) spread += (d - mean) * (d - mean);
        spread = Math.Sqrt(spread / Math.Max(degree.Length, 1));
        double hubDegree = mean + HubDeviations * spread;

        var order = new List<int>(names.Count);
        for (int i = 0; i < names.Count; i++) if (ChartDisplay.LabelEveryone || degree[i] >= hubDegree) order.Add(i);
        order.Sort((a, b) => degree[b].CompareTo(degree[a]));
        int labelled = ChartDisplay.LabelEveryone ? order.Count : Math.Min(order.Count, MostHubs);
        for (int i = 0; i < labelled; i++)
        {
            int node = order[i];
            geometry.FreeLabels.Add(new FreeLabel
            {
                text = names[node],
                position = new Vector3(positions[node].x, positions[node].y + diameters[node] * 0.5f + 0.012f, positions[node].z)
            });
        }

        for (int i = 0; i < groupNames.Count; i++)
            geometry.Legend.Add(new LegendEntry { slot = i, shade = 1f, label = groupNames[i] });
        if (tieColumn != null && !tieColumn.IsNumber)
            for (int i = 0; i < tieColumn.Categories.Count; i++)
                geometry.Legend.Add(new LegendEntry
                {
                    slot = -1,
                    shade = i == 0 ? OtherBeamShade : PrimaryBeamShade,
                    label = tieColumn.Categories[i]
                });

        ChartAxes.Finish(geometry, half, half, height, footprint * 0.04f);
        return geometry;
    }

    private static float3[] Layout(int count, List<(int from, int to, int tie)> edges)
    {
        var random = new System.Random(LayoutSeed);
        var positions = new float3[count];
        for (int i = 0; i < count; i++)
            positions[i] = new float3(
                (float)random.NextDouble() - 0.5f,
                (float)random.NextDouble() - 0.5f,
                (float)random.NextDouble() - 0.5f);

        float ideal = math.pow(1f / Math.Max(count, 1), 1f / 3f);
        var moves = new float3[count];
        for (int step = 0; step < Iterations; step++)
        {
            float heat = 0.1f * (1f - step / (float)Iterations) + 0.001f;
            Array.Clear(moves, 0, count);

            for (int i = 0; i < count; i++)
                for (int j = i + 1; j < count; j++)
                {
                    float3 away = positions[i] - positions[j];
                    float distance = Mathf.Max(math.length(away), 1e-3f);
                    float3 push = away / distance * (ideal * ideal / distance);
                    moves[i] += push;
                    moves[j] -= push;
                }

            foreach ((int from, int to, int _) in edges)
            {
                float3 along = positions[from] - positions[to];
                float distance = Mathf.Max(math.length(along), 1e-3f);
                float3 pull = along / distance * (distance * distance / ideal);
                moves[from] -= pull;
                moves[to] += pull;
            }

            for (int i = 0; i < count; i++)
            {
                moves[i] -= positions[i] * 0.2f;
                float size = Mathf.Max(math.length(moves[i]), 1e-5f);
                positions[i] += moves[i] / size * Mathf.Min(size, heat);
            }
        }
        return positions;
    }

    private static float3[] Fit(float3[] positions, float half, float height)
    {
        float3 low = new float3(float.MaxValue), high = new float3(float.MinValue);
        foreach (float3 p in positions)
        {
            low = math.min(low, p);
            high = math.max(high, p);
        }

        float3 span = math.max(high - low, new float3(1e-4f));
        var fitted = new float3[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            float3 share = (positions[i] - low) / span;
            fitted[i] = new float3(share.x * 2f * half - half, share.y * height, share.z * 2f * half - half);
        }
        return fitted;
    }
}
