using System.Collections.Generic;
using UnityEngine;

public class FilterTool : Tool
{
    protected override ToolType Kind => ToolType.Filter;

    private const float HalfBand = SortLineProxy.BandHeight * 0.5f;
    private const float RowBandLow = SortLineProxy.RowBand - HalfBand, RowBandHigh = SortLineProxy.RowBand + HalfBand;
    private const float ColumnBandLow = SortLineProxy.ColumnBand - HalfBand, ColumnBandHigh = SortLineProxy.ColumnBand + HalfBand;

    private const float PokeDwell = 0.12f;

    private struct Tip
    {
        public bool fired;
        public bool rows;
        public int slot;
        public float since;
    }

    private readonly Dictionary<int, Tip> _inside = new Dictionary<int, Tip>();

    private static DataSource Data => Scene.Data;

    protected override void OnResetTool()
    {
        DataSource data = Data;
        if (data == null) return;

        bool columns = data.SetHiddenOn(false, null, out _);
        bool rows = data.SetHiddenOn(true, null, out _);

        string rowNouns = DataSource.Plural(DataSource.FilterNoun(data, true));
        string columnNouns = DataSource.Plural(DataSource.FilterNoun(data, false));
        if (columns && rows) Report($"put every one of the {rowNouns} and {columnNouns} back on the sheet");
        else if (columns) Report($"showed every one of the {columnNouns} again");
        else if (rows) Report($"showed every one of the {rowNouns} again");
    }

    protected override void ClearToolState()
    {
        ClearTint();
        _inside.Clear();
    }

    private void LateUpdate()
    {
        if (!Active) return;

        DataSource data = Data;
        CreateSheet sheet = sheetManager != null ? sheetManager.Sheet : null;
        if (data == null || !data.IsLoaded || sheet == null || !sheet.Shown)
        {
            ClearToolState();
            return;
        }

        IReadOnlyList<Fingertips.Source> tips = Fingertips.All;
        bool lit = false;
        float now = Time.unscaledTime;
        for (int i = 0; i < tips.Count; i++)
        {
            bool inVolume = false, rows = false;
            int slot = -1;
            bool on = tips[i].Live && TryLineAt(sheet, tips[i].poke.Origin, out inVolume, out rows, out slot);
            if (!inVolume) { _inside.Remove(i); continue; }

            if (!_inside.TryGetValue(i, out Tip tip)) tip.slot = -1;
            if (!on) { tip.slot = -1; _inside[i] = tip; continue; }
            if (tip.slot != slot || tip.rows != rows) { tip.rows = rows; tip.slot = slot; tip.since = now; }

            if (!tip.fired && now - tip.since >= PokeDwell)
            {
                tip.fired = true;
                _inside[i] = tip;
                Poke(data, sheet, rows, slot);
                continue;
            }
            _inside[i] = tip;
            if (tip.fired) continue;

            if (lit) continue;
            int line = sheet.LineAtSlot(!rows, slot);
            if (line < 0) continue;
            TintLine(sheet, !rows, line);
            lit = true;
        }
        if (!lit) ClearTint();
    }

    private bool TryLineAt(CreateSheet sheet, Vector3 world, out bool inVolume, out bool rows, out int slot)
    {
        inVolume = false;
        rows = false;
        slot = -1;
        Vector3 p = sheet.transform.InverseTransformPoint(world);
        float height = sheetManager.Height;
        if (p.y < 0f || p.y > height) return false;

        bool pastRowEnds = Outside(sheet, true, p.x, out bool inRowReach);
        bool pastColumnEnds = Outside(sheet, false, p.z, out bool inColumnReach);
        if (!inRowReach || !inColumnReach) return false;
        inVolume = true;
        if (pastRowEnds && pastColumnEnds) return false;

        float share = p.y / Mathf.Max(height, 1e-6f);
        if (pastRowEnds) rows = true;
        else if (pastColumnEnds) rows = false;
        else if (share >= RowBandLow && share <= RowBandHigh) rows = true;
        else if (share >= ColumnBandLow && share <= ColumnBandHigh) rows = false;
        else return false;

        bool columns = !rows;
        float coord = columns ? p.x : p.z;
        slot = Mathf.RoundToInt(sheet.SlotFraction(columns, coord));
        return slot >= sheet.FirstSlot(columns) && slot <= sheet.LastSlot(columns);
    }

    private static bool Outside(CreateSheet sheet, bool columns, float coord, out bool inReach)
    {
        float half = sheet.PitchOn(columns) * 0.5f;
        float low = sheet.SlotCoord(columns, sheet.FirstSlot(columns)) - half;
        float high = sheet.SlotCoord(columns, sheet.LastSlot(columns)) + half;
        float reach = sheet.BlockPitch(!columns);
        bool outside = coord < low || coord > high;
        inReach = coord >= low - reach && coord <= high + reach;
        return outside;
    }

    private void Poke(DataSource data, CreateSheet sheet, bool rows, int slot)
    {
        ClearTint();
        List<int> order = data.FullOrder(!rows);
        if (slot < 0 || slot >= order.Count) return;
        int id = rows ? order[slot] : data.DataGroupOf(order[slot]);
        UIPressFeedback.Play();
        if (!Toggle(rows, id, out string refusal) && refusal != null)
            Notices.Show(this, Strings.Get("notice.filter"), refusal);
    }

    public bool Toggle(bool rows, int id, out string refusal)
    {
        refusal = null;

        DataSource data = Data;
        if (data == null || !data.IsLoaded) { refusal = "No dataset is open."; return false; }

        List<int> hidden = data.HiddenInOrder(rows);
        if (data.IsHidden(rows, id)) hidden.Remove(id);
        else hidden.Add(id);

        return Apply(rows, hidden, out refusal);
    }

    public bool Apply(bool rows, IReadOnlyList<int> hidden, out string refusal)
    {
        refusal = null;

        DataSource data = Data;
        if (data == null || !data.IsLoaded) { refusal = "No dataset is open."; return false; }

        List<int> before = data.HiddenInOrder(rows);
        if (!data.SetHiddenOn(rows, hidden, out refusal)) return false;

        List<int> after = data.HiddenInOrder(rows);
        ManageDatasets.ActiveEdits.PushFilter(before, after, rows);

        Report(Describe(data, before, after, rows));
        return true;
    }

    private static string Describe(DataSource data, List<int> before, List<int> after, bool rows)
    {
        var gone = new List<string>();
        var back = new List<string>();

        var wasHidden = new HashSet<int>(before);
        var isHidden = new HashSet<int>(after);
        for (int i = 0; i < after.Count; i++)
            if (!wasHidden.Contains(after[i])) gone.Add(DataSource.FilterLabel(data, rows, after[i]));
        for (int i = 0; i < before.Count; i++)
            if (!isHidden.Contains(before[i])) back.Add(DataSource.FilterLabel(data, rows, before[i]));

        string noun = DataSource.FilterNoun(data, rows);
        var parts = new List<string>(2);
        if (gone.Count > 0) parts.Add($"took {Summarize(gone, noun)} off the sheet");
        if (back.Count > 0) parts.Add($"put {Summarize(back, noun)} back on the sheet");

        string what = parts.Count > 0
            ? string.Join(" and ", parts)
            : $"left the same {DataSource.Plural(noun)} on the sheet";

        int showing = rows ? data.RowOrder.Count : data.GroupCount(true);
        int total = rows ? data.RowCount : data.DataGroupCount;
        return $"{what}, leaving {showing} of {total} showing";
    }

    private static string Summarize(List<string> names, string noun)
    {
        if (names.Count == 1) return names[0];
        if (names.Count <= 3) return string.Join(", ", names);
        return $"{names.Count} {DataSource.Plural(noun)}";
    }

    public bool TryResolve(bool rows, string query, out int id)
    {
        id = -1;

        DataSource data = Data;
        if (data == null || !data.IsLoaded || string.IsNullOrWhiteSpace(query)) return false;

        string wanted = query.Trim();
        List<int> all = data.FilterLinesInOrder(rows);

        for (int i = 0; i < all.Count; i++)
            if (string.Equals(DataSource.FilterLabel(data, rows, all[i]), wanted, System.StringComparison.OrdinalIgnoreCase))
            {
                id = all[i];
                return true;
            }

        if (int.TryParse(wanted, out int position))
        {
            if (position < 1) return false;
            for (int i = 0; i < all.Count; i++)
            {
                if (data.IsHidden(rows, all[i])) continue;
                if (--position == 0) { id = all[i]; return true; }
            }
            return false;
        }

        int only = -1, hits = 0;
        for (int i = 0; i < all.Count; i++)
        {
            if (DataSource.FilterLabel(data, rows, all[i]).IndexOf(wanted, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            only = all[i];
            hits++;
        }
        if (hits != 1) return false;
        id = only;
        return true;
    }

    public List<string> Names(bool rows)
    {
        DataSource data = Data;
        var names = new List<string>();
        if (data == null || !data.IsLoaded) return names;

        foreach (int id in data.FilterLinesInOrder(rows)) names.Add(DataSource.FilterLabel(data, rows, id));
        return names;
    }
}
