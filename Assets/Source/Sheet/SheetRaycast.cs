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

    public struct RayHit
    {
        public CreateSheet sheet;
        public CreateCube cube;
        public Vector3 point;
        public float distance;
        public int rowSlot;
        public int columnSlot;
        public bool emptyRow;
        public float shareOfBar;

        public bool PicksRow => cube == null ? emptyRow : shareOfBar < 0.5f;

        public int SlotOfPick => PicksRow ? rowSlot : columnSlot;
    }

    public static bool Cast(Vector3 origin, Vector3 direction, float maxDistance, bool emptySlotsCount, out RayHit hit)
    {
        hit = default;

        ManageSheets sheets = Scene.Sheets;
        if (sheets == null) return false;

        CreateSheet sheet = sheets.Sheet;
        if (sheet == null || !sheet.Shown || !sheet.IsBuilt || sheet.Falling) return false;

        Transform frame = sheet.transform;
        Vector3 localOrigin = frame.InverseTransformPoint(origin);
        Vector3 localStep = frame.InverseTransformVector(direction);

        int firstRow = sheet.FirstSlot(false), lastRow = sheet.LastSlot(false);
        int firstColumn = sheet.FirstSlot(true), lastColumn = sheet.LastSlot(true);
        float halfRow = sheet.PitchOn(false) * 0.5f, halfColumn = sheet.PitchOn(true) * 0.5f;

        float enter = 0f, exit = maxDistance;
        if (!Clip(localOrigin.x, localStep.x, sheet.SlotCoord(true, firstColumn) - halfColumn,
                sheet.SlotCoord(true, lastColumn) + halfColumn, ref enter, ref exit)) return false;
        if (!Clip(localOrigin.z, localStep.z, sheet.SlotCoord(false, firstRow) - halfRow,
                sheet.SlotCoord(false, lastRow) + halfRow, ref enter, ref exit)) return false;

        SpanOf(sheet, false, localOrigin.z, localStep.z, enter, exit, firstRow, lastRow, out int rowLow, out int rowHigh);

        float best = float.MaxValue;
        float height = sheets.Height;
        for (int rowSlot = rowLow; rowSlot <= rowHigh; rowSlot++)
        {
            float rowCoord = sheet.SlotCoord(false, rowSlot);
            float rowEnter = enter, rowExit = exit;
            if (!Clip(localOrigin.z, localStep.z, rowCoord - halfRow, rowCoord + halfRow, ref rowEnter, ref rowExit))
                continue;

            SpanOf(sheet, true, localOrigin.x, localStep.x, rowEnter, rowExit, firstColumn, lastColumn,
                out int columnLow, out int columnHigh);
            int row = sheet.LineAtSlot(false, rowSlot);

            for (int columnSlot = columnLow; columnSlot <= columnHigh; columnSlot++)
            {
                int column = sheet.LineAtSlot(true, columnSlot);
                CreateCube cube = row >= 0 && column >= 0 ? sheet.CubeAt(row, column) : null;
                Vector3 center, size;
                if (cube != null)
                {
                    if (!cube.Touchable) continue;
                    center = cube.transform.localPosition;
                    size = cube.transform.localScale;
                }
                else
                {
                    if (!emptySlotsCount || (row >= 0 && column >= 0)) continue;
                    center = new Vector3(sheet.SlotCoord(true, columnSlot), height * 0.5f, rowCoord);
                    size = new Vector3(halfColumn * 2f, height, halfRow * 2f);
                }

                float boxEnter = 0f, boxExit = maxDistance;
                Vector3 half = size * 0.5f;
                if (!Clip(localOrigin.x, localStep.x, center.x - half.x, center.x + half.x, ref boxEnter, ref boxExit)) continue;
                if (!Clip(localOrigin.y, localStep.y, center.y - half.y, center.y + half.y, ref boxEnter, ref boxExit)) continue;
                if (!Clip(localOrigin.z, localStep.z, center.z - half.z, center.z + half.z, ref boxEnter, ref boxExit)) continue;
                if (boxEnter >= best) continue;

                best = boxEnter;
                float localY = localOrigin.y + localStep.y * boxEnter;
                hit = new RayHit
                {
                    sheet = sheet,
                    cube = cube,
                    point = origin + direction * boxEnter,
                    distance = boxEnter,
                    rowSlot = rowSlot,
                    columnSlot = columnSlot,
                    emptyRow = row < 0,
                    shareOfBar = Mathf.Clamp01((localY - (center.y - half.y)) / Mathf.Max(size.y, 1e-6f))
                };
            }
        }
        return hit.sheet != null;
    }

    private static void SpanOf(CreateSheet sheet, bool columns, float start, float step, float enter, float exit,
        int first, int last, out int low, out int high)
    {
        float a = start + step * enter, b = start + step * exit;
        low = Mathf.Max(first, Mathf.FloorToInt(sheet.SlotFraction(columns, Mathf.Min(a, b))) - Slack);
        high = Mathf.Min(last, Mathf.CeilToInt(sheet.SlotFraction(columns, Mathf.Max(a, b))) + Slack);
    }

    private static bool Clip(float start, float step, float low, float high, ref float enter, ref float exit)
    {
        if (Mathf.Abs(step) < 1e-7f) return start >= low && start <= high;

        float a = (low - start) / step, b = (high - start) / step;
        if (a > b) (a, b) = (b, a);
        enter = Mathf.Max(enter, a);
        exit = Mathf.Min(exit, b);
        return enter <= exit;
    }

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
