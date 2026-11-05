using System.Collections.Generic;
using UnityEngine;

public static class SheetRaycast
{
    public struct Hit
    {
        public bool valid;
        public CreateCube cube;
        public Vector3 point;
        public float distance;
    }

    private const int Slack = 1;

    public static bool NearestCube(Vector3 point, float radius, out Hit hit)
    {
        hit = default;
        if (radius <= 0f) return false;

        ManageSheets sheets = Scene.Sheets;
        if (sheets == null) return false;

        CreateSheet sheet = sheets.Sheet;
        if (sheet == null || !sheet.Shown || !sheet.IsBuilt || sheet.Falling) return false;

        float best = float.MaxValue;
        Nearest(sheet, point, radius, ref best, ref hit);
        return hit.valid;
    }

    public static bool Contains(CreateCube cube, Vector3 point) =>
        cube != null && cube.Touchable && cube.Contains(point);

    private static void Nearest(CreateSheet sheet, Vector3 world, float radius, ref float best, ref Hit hit)
    {
        float scale = Mathf.Abs(sheet.transform.lossyScale.x);
        if (scale < 1e-6f) return;

        Vector3 p = sheet.transform.InverseTransformPoint(world);
        float reach = radius / scale;

        Gather(sheet, true, p.x, reach, Columns);
        Gather(sheet, false, p.z, reach, Rows);

        for (int c = 0; c < Columns.Count; c++)
        {
            int visCol = Columns[c];
            for (int r = 0; r < Rows.Count; r++)
            {
                int visRow = Rows[r];

                CreateCube cube = sheet.CubeAt(visRow, visCol);
                if (cube == null || !cube.Touchable) continue;

                Vector3 closest = cube.ClosestPoint(world);
                float distance = Vector3.Distance(closest, world);
                if (distance > radius || distance >= best) continue;

                best = distance;
                hit = new Hit { valid = true, cube = cube, point = closest, distance = distance };
            }
        }
    }

    private static readonly List<int> Columns = new List<int>();
    private static readonly List<int> Rows = new List<int>();

    private static void Gather(CreateSheet sheet, bool columns, float coord, float reach, List<int> into)
    {
        into.Clear();
        if (SlotSpan(sheet, columns, coord, reach, out int lo, out int hi))
            for (int slot = lo; slot <= hi; slot++)
            {
                int line = sheet.LineAtSlot(columns, slot);
                if (line >= 0) into.Add(line);
            }
        foreach (int line in sheet.LinesOffSlot(columns))
            if (!into.Contains(line)) into.Add(line);
    }

    private static bool SlotSpan(CreateSheet sheet, bool columns, float coord, float reach,
        out int lo, out int hi)
    {
        int first = sheet.FirstSlot(columns), last = sheet.LastSlot(columns);
        lo = Mathf.FloorToInt(sheet.SlotFraction(columns, coord - reach)) - Slack;
        hi = Mathf.CeilToInt(sheet.SlotFraction(columns, coord + reach)) + Slack;
        if (hi < first || lo > last) return false;
        lo = Mathf.Max(lo, first);
        hi = Mathf.Min(hi, last);
        return true;
    }
}
