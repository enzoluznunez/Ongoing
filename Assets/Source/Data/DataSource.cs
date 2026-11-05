using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class DataSource : MonoBehaviour
{
    public IReadOnlyList<string> ColumnTitles => _columnTitles;
    public IReadOnlyList<string> RowTitles => _rowTitles;
    public int ColumnCount => _columnTitles.Count;
    public int RowCount => _rowTitles.Count;

    public bool HasRowCategories => _rowCategories.Count > 0;

    public string RowCategoryAt(int dataRow) =>
        dataRow >= 0 && dataRow < _rowCategories.Count ? _rowCategories[dataRow] : null;

    public Color RowColorAt(int dataRow) =>
        dataRow >= 0 && dataRow < _rowColors.Count ? _rowColors[dataRow] : Color.white;

    public List<string> CategoriesInOrder(IReadOnlyList<int> rows)
    {
        var found = new List<string>();
        if (rows == null) return found;

        for (int i = 0; i < rows.Count; i++)
        {
            string category = RowCategoryAt(rows[i]);
            if (string.IsNullOrEmpty(category) || found.Contains(category)) continue;
            found.Add(category);
        }
        return found;
    }

    public string ColumnAxisTitle => _columnAxisTitle;
    public string RowAxisTitle => _rowAxisTitle;

    public IReadOnlyList<int> ColumnOrder => _visibleColumns;
    public IReadOnlyList<int> RowOrder => _visibleRows;

    public string TitleAt(bool columns, int visIndex)
    {
        IReadOnlyList<int> order = columns ? ColumnOrder : RowOrder;
        IReadOnlyList<string> titles = columns ? _columnTitles : _rowTitles;
        if (order == null || titles == null || visIndex < 0 || visIndex >= order.Count) return null;
        int d = order[visIndex];
        return d >= 0 && d < titles.Count ? titles[d] : null;
    }

    public static string LabelAt(DataSource data, bool columns, int visIndex)
    {
        string title = data != null ? data.TitleAt(columns, visIndex) : null;
        return string.IsNullOrEmpty(title) ? $"{(columns ? "column" : "row")} {visIndex + 1}" : title;
    }

    public int VisIndexOf(bool columns, int dataIndex)
    {
        IReadOnlyList<int> order = columns ? ColumnOrder : RowOrder;
        if (order == null || dataIndex < 0) return -1;
        for (int v = 0; v < order.Count; v++)
            if (order[v] == dataIndex) return v;
        return -1;
    }

    public int ColumnGroupSize => _columnGroupSize;
    public IReadOnlyList<string> SeriesTitles => _seriesTitles;

    public bool IsGrouped(bool columns) => GroupSize(columns) > 1;

    public int GroupSize(bool columns) => columns ? _columnGroupSize : 1;

    public int GroupCount(bool columns)
    {
        return (columns ? ColumnOrder : RowOrder).Count / GroupSize(columns);
    }

    private readonly HashSet<int> _hiddenGroups = new HashSet<int>();
    private readonly List<int> _visibleColumns = new List<int>();

    public int DataGroupCount => ColumnCount / _columnGroupSize;

    public int DataGroupOf(int dataColumn) => dataColumn / _columnGroupSize;

    public string DataGroupTitleAt(int dataGroup) => GroupTitleOfData(true, dataGroup * _columnGroupSize);

    private List<int> DataGroupsInOrder()
    {
        var groups = new List<int>(DataGroupCount);
        for (int i = 0; i < _columnOrder.Count; i += _columnGroupSize)
            groups.Add(DataGroupOf(_columnOrder[i]));
        return groups;
    }

    private bool SetHidden(HashSet<int> target, IEnumerable<int> hidden, int count, string noun,
                           out string refusal)
    {
        refusal = null;

        var wanted = new HashSet<int>();
        if (hidden != null)
            foreach (int id in hidden)
            {
                if (id < 0 || id >= count)
                {
                    refusal = $"There is no {noun} {id + 1} to hide.";
                    return false;
                }
                wanted.Add(id);
            }

        if (wanted.Count >= count)
        {
            refusal = $"At least one {noun} has to stay on the sheet.";
            return false;
        }

        if (wanted.SetEquals(target)) return false;

        target.Clear();
        target.UnionWith(wanted);
        RaiseOrderChanged();
        return true;
    }

    private readonly HashSet<int> _hiddenRows = new HashSet<int>();
    private readonly List<int> _visibleRows = new List<int>();

    public List<int> FilterLinesInOrder(bool rows) => rows ? new List<int>(_rowOrder) : DataGroupsInOrder();

    public List<int> HiddenInOrder(bool rows)
    {
        HashSet<int> hiddenSet = HiddenSet(rows);
        List<int> lines = FilterLinesInOrder(rows);
        lines.RemoveAll(id => !hiddenSet.Contains(id));
        return lines;
    }

    public bool IsHidden(bool rows, int id) => HiddenSet(rows).Contains(id);

    public bool SetHiddenOn(bool rows, IEnumerable<int> hidden, out string refusal) =>
        SetHidden(HiddenSet(rows), hidden, rows ? RowCount : DataGroupCount,
            FilterNoun(this, rows), out refusal);

    public static string FilterNoun(DataSource data, bool rows) => rows ? RowNoun(data) : GroupNoun(data, true);

    private HashSet<int> HiddenSet(bool rows) => rows ? _hiddenRows : _hiddenGroups;

    public static string FilterLabel(DataSource data, bool rows, int id) =>
        rows ? RowLabelOfData(data, id) : GroupLabelOfData(data, id);

    private void RebuildVisibleRows()
    {
        _visibleRows.Clear();
        _rowSlots.Clear();
        for (int i = 0; i < _rowOrder.Count; i++)
        {
            if (_hiddenRows.Contains(_rowOrder[i])) continue;
            _visibleRows.Add(_rowOrder[i]);
            _rowSlots.Add(i);
        }
    }

    private readonly List<int> _rowSlots = new List<int>();
    private readonly List<int> _columnSlots = new List<int>();

    public IReadOnlyList<int> Slots(bool columns) => columns ? _columnSlots : _rowSlots;

    public int SlotCount(bool columns) => columns ? _columnOrder.Count : _rowOrder.Count;

    public List<int> FullOrder(bool columns) => new List<int>(columns ? _columnOrder : _rowOrder);

    public bool SetFullOrder(bool columns, IReadOnlyList<int> order)
    {
        List<int> target = columns ? _columnOrder : _rowOrder;
        if (order == null || order.Count != target.Count || !IsRearrangementOf(order, target)) return false;
        if (columns && !IsGroupAligned(order, _columnGroupSize)) return false;

        bool same = true;
        for (int i = 0; i < order.Count && same; i++) same = order[i] == target[i];
        if (same) return false;

        for (int i = 0; i < order.Count; i++) target[i] = order[i];
        RaiseOrderChanged();
        return true;
    }

    public static string RowNoun(DataSource data) =>
        data != null && !string.IsNullOrWhiteSpace(data.RowAxisTitle)
            ? data.RowAxisTitle.Trim().ToLowerInvariant()
            : "row";

    public static string Plural(string noun)
    {
        if (string.IsNullOrEmpty(noun)) return noun;
        if (noun.Length > 1 && noun.EndsWith("y") && "aeiou".IndexOf(noun[noun.Length - 2]) < 0)
            return noun.Substring(0, noun.Length - 1) + "ies";
        return noun.EndsWith("s") ? noun : noun + "s";
    }

    public static string RowLabelOfData(DataSource data, int dataRow)
    {
        if (data != null && dataRow >= 0 && dataRow < data._rowTitles.Count)
        {
            string title = data._rowTitles[dataRow];
            if (!string.IsNullOrEmpty(title)) return title;
        }
        return $"{RowNoun(data)} {dataRow + 1}";
    }

    private void RebuildVisibleColumns()
    {
        _visibleColumns.Clear();
        _columnSlots.Clear();
        int size = _columnGroupSize;

        for (int i = 0; i + size <= _columnOrder.Count; i += size)
        {
            if (_hiddenGroups.Contains(DataGroupOf(_columnOrder[i]))) continue;
            for (int s = 0; s < size; s++)
            {
                _visibleColumns.Add(_columnOrder[i + s]);
                _columnSlots.Add(i + s);
            }
        }
    }

    public int GroupOf(bool columns, int visIndex) => visIndex / GroupSize(columns);

    public void GroupSpan(bool columns, int group, out int lo, out int hi)
    {
        int size = GroupSize(columns);
        lo = group * size;
        hi = lo + size - 1;
    }

    public string GroupTitleAt(bool columns, int group)
    {
        int size = GroupSize(columns);
        if (size <= 1) return TitleAt(columns, group);

        IReadOnlyList<int> order = columns ? ColumnOrder : RowOrder;
        int vis = group * size;
        if (order == null || vis < 0 || vis >= order.Count) return null;

        return GroupTitleOfData(columns, order[vis]) ?? TitleAt(columns, vis);
    }

    public string GroupTitleOfData(bool columns, int dataIndex)
    {
        IReadOnlyList<string> titles = columns ? _columnTitles : _rowTitles;
        int size = GroupSize(columns);
        if (size <= 1)
            return dataIndex >= 0 && dataIndex < titles.Count ? titles[dataIndex] : null;

        int g = dataIndex / size;
        return g >= 0 && g < _groupTitles.Count ? _groupTitles[g] : null;
    }

    public static List<string> BlockTitlesFor(DataSource data, bool columns, IReadOnlyList<int> order)
    {
        var list = new List<string>();
        if (data == null || order == null) return list;

        int size = data.GroupSize(columns);
        for (int i = 0; i < order.Count; i += size)
            list.Add(data.GroupTitleOfData(columns, order[i]));
        return list;
    }

    public string SeriesTitleAt(bool columns, int visIndex)
    {
        int size = GroupSize(columns);
        if (size <= 1) return null;
        int s = visIndex % size;
        return s >= 0 && s < _seriesTitles.Count ? _seriesTitles[s] : null;
    }

    public static string GroupLabelAt(DataSource data, bool columns, int group)
    {
        string title = data != null ? data.GroupTitleAt(columns, group) : null;
        if (!string.IsNullOrEmpty(title)) return title;
        return $"{GroupNoun(data, columns)} {group + 1}";
    }

    public static string GroupLabelOfData(DataSource data, int dataGroup)
    {
        string title = data != null ? data.DataGroupTitleAt(dataGroup) : null;
        if (!string.IsNullOrEmpty(title)) return title;
        return $"{GroupNoun(data, true)} {dataGroup + 1}";
    }

    public static string GroupNoun(DataSource data, bool columns) =>
        data != null && data.IsGrouped(columns) ? "metric" : columns ? "column" : "row";

    protected void SetColumnGroupSize(int size)
    {
        _columnGroupSize = size > 1 ? size : 1;
        BuildColumnGroups();
    }

    private void BuildColumnGroups()
    {
        _groupTitles.Clear();
        _seriesTitles.Clear();

        int size = _columnGroupSize;
        if (size <= 1) return;

        int groups = _columnTitles.Count / size;
        for (int g = 0; g < groups; g++)
        {
            int first = g * size;
            string shared = _columnTitles[first];
            for (int s = 1; s < size; s++) shared = SharedPrefix(shared, _columnTitles[first + s]);
            shared = shared.Trim();
            _groupTitles.Add(shared.Length > 0 ? shared : _columnTitles[first]);
        }

        string stem = groups > 0 ? _groupTitles[0] : "";
        for (int s = 0; s < size; s++)
        {
            string title = s < _columnTitles.Count ? _columnTitles[s] : "";
            _seriesTitles.Add(stem.Length > 0 && title.StartsWith(stem, StringComparison.Ordinal)
                ? title.Substring(stem.Length).Trim()
                : title);
        }
    }

    private static string SharedPrefix(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return "";
        if (string.Equals(a, b, StringComparison.Ordinal)) return a;

        int n = Mathf.Min(a.Length, b.Length);
        int i = 0;
        while (i < n && a[i] == b[i]) i++;
        while (i > 0 && !char.IsWhiteSpace(a[i - 1])) i--;
        return a.Substring(0, i);
    }

    public bool IsLoaded => _isLoaded;

    public string RawText => _rawText;
    protected string _rawText;

    public event Action OnDataLoaded;

    public event Action OnLayoutInvalidated;

    protected List<string> _columnTitles = new List<string>();
    protected List<string> _rowTitles = new List<string>();

    protected List<string> _rowCategories = new List<string>();
    protected List<Color> _rowColors = new List<Color>();

    protected float[,] _heights;
    protected string _columnAxisTitle;
    protected string _rowAxisTitle;
    protected float[,] _values = new float[0, 0];

    protected bool[,] _valid = new bool[0, 0];
    protected float _globalMin;
    protected float _globalMax = 1f;

    protected int _columnGroupSize = 1;
    private readonly List<string> _groupTitles = new List<string>();
    private readonly List<string> _seriesTitles = new List<string>();

    private float[] _scales = new float[0];

    protected List<int> _columnOrder = new List<int>();
    protected List<int> _rowOrder = new List<int>();

    protected bool _isLoaded;

    protected int FillGrid(int rowCount, int colCount, Func<int, int, float?> sample)
    {
        _values = new float[rowCount, colCount];
        _valid = new bool[rowCount, colCount];
        int filled = 0;

        for (int r = 0; r < rowCount; r++)
        {
            for (int c = 0; c < colCount; c++)
            {
                float? v = sample(r, c);
                if (v.HasValue)
                {
                    _values[r, c] = v.Value;
                    _valid[r, c] = true;
                    filled++;
                }
                else
                {
                    _values[r, c] = float.NaN;
                    _valid[r, c] = false;
                }
            }
        }

        BuildScales(rowCount, colCount, filled);
        return filled;
    }

    protected void FillHeights(int rowCount, int colCount, Func<int, int, float?> sample)
    {
        _heights = new float[rowCount, colCount];
        for (int r = 0; r < rowCount; r++)
            for (int c = 0; c < colCount; c++)
                _heights[r, c] = Mathf.Clamp01(sample(r, c) ?? 0f);
    }

    public bool HasHeights => _heights != null;

    private int BucketOf(int colIndex) => _columnGroupSize > 1 ? colIndex / _columnGroupSize : 0;

    private void BuildScales(int rowCount, int colCount, int filled)
    {
        int buckets = _columnGroupSize > 1 ? BucketOf(colCount - 1) + 1 : 1;
        _scales = new float[Mathf.Max(buckets, 1)];

        for (int c = 0; c < colCount; c++)
        {
            int b = BucketOf(c);
            for (int r = 0; r < rowCount; r++)
            {
                if (!_valid[r, c]) continue;
                float m = Mathf.Abs(_values[r, c]);
                if (m > _scales[b]) _scales[b] = m;
            }
        }

        for (int b = 0; b < _scales.Length; b++)
            if (_scales[b] <= 0f) _scales[b] = 1f;

        _globalMin = float.MaxValue;
        _globalMax = float.MinValue;
        for (int r = 0; r < rowCount; r++)
            for (int c = 0; c < colCount; c++)
            {
                if (!_valid[r, c]) continue;
                float n = Normalized(r, c);
                if (n < _globalMin) _globalMin = n;
                if (n > _globalMax) _globalMax = n;
            }

        if (filled == 0)
        {
            _globalMin = 0f;
            _globalMax = 1f;
        }
        else if (Mathf.Approximately(_globalMin, _globalMax))
        {
            if (_globalMin > 0f) _globalMin = 0f;
            else if (_globalMax < 0f) _globalMax = 0f;
            else _globalMax = _globalMin + 1f;
        }
    }

    private float Normalized(int rowIndex, int colIndex)
    {
        int b = BucketOf(colIndex);
        float k = b >= 0 && b < _scales.Length ? _scales[b] : 1f;
        return k > 0f ? _values[rowIndex, colIndex] / k : _values[rowIndex, colIndex];
    }

    private void EnsureOrders()
    {
        EnsureOrder(_columnOrder, ColumnCount);
        EnsureOrder(_rowOrder, RowCount);
        RebuildVisibleColumns();
        RebuildVisibleRows();
    }

    private void RaiseOrderChanged()
    {
        EnsureOrders();
        OnLayoutInvalidated?.Invoke();
    }

    public bool InDataOrder => IsIdentity(_columnOrder, ColumnCount) && IsIdentity(_rowOrder, RowCount);

    public void ResetOrder()
    {
        InitIdentity(_columnOrder, ColumnCount);
        InitIdentity(_rowOrder, RowCount);
        RaiseOrderChanged();
    }

    public bool SetColumnOrder(IReadOnlyList<int> order)
    {
        if (!SpliceVisibleColumns(order)) return false;
        RaiseOrderChanged();
        return true;
    }

    private bool SpliceVisibleColumns(IReadOnlyList<int> order)
    {
        if (order == null || order.Count != _visibleColumns.Count) return false;
        if (!IsRearrangementOf(order, _visibleColumns)) return false;
        if (!IsGroupAligned(order, _columnGroupSize)) return false;

        int taken = 0;
        int size = _columnGroupSize;
        for (int i = 0; i + size <= _columnOrder.Count; i += size)
        {
            if (_hiddenGroups.Contains(DataGroupOf(_columnOrder[i]))) continue;
            for (int s = 0; s < size; s++) _columnOrder[i + s] = order[taken++];
        }
        return true;
    }

    private static bool IsRearrangementOf(IReadOnlyList<int> order, List<int> of)
    {
        var allowed = new HashSet<int>(of);
        for (int i = 0; i < order.Count; i++)
            if (!allowed.Remove(order[i])) return false;
        return true;
    }

    public bool SetRowOrder(IReadOnlyList<int> order)
    {
        if (!SpliceVisibleRows(order)) return false;
        RaiseOrderChanged();
        return true;
    }

    public void MoveColumn(int fromPos, int toPos)
    {
        List<int> arrangement = new List<int>(_visibleColumns);
        bool moved = _columnGroupSize > 1
            ? MoveGroupWithin(arrangement, fromPos / _columnGroupSize, toPos / _columnGroupSize, _columnGroupSize)
            : MoveWithin(arrangement, fromPos, toPos);
        if (moved) SetColumnOrder(arrangement);
    }

    public void MoveRow(int fromPos, int toPos)
    {
        List<int> arrangement = new List<int>(_visibleRows);
        if (MoveWithin(arrangement, fromPos, toPos)) SetRowOrder(arrangement);
    }

    private bool SpliceVisibleRows(IReadOnlyList<int> order)
    {
        if (order == null || order.Count != _visibleRows.Count) return false;
        if (!IsRearrangementOf(order, _visibleRows)) return false;

        int taken = 0;
        for (int i = 0; i < _rowOrder.Count; i++)
        {
            if (_hiddenRows.Contains(_rowOrder[i])) continue;
            _rowOrder[i] = order[taken++];
        }
        return true;
    }

    private static bool MoveWithin(List<int> order, int fromPos, int toPos)
    {
        if (fromPos < 0 || fromPos >= order.Count) return false;
        toPos = Mathf.Clamp(toPos, 0, order.Count - 1);
        if (toPos == fromPos) return false;
        int value = order[fromPos];
        order.RemoveAt(fromPos);
        order.Insert(toPos, value);
        return true;
    }

    public static bool MoveGroupWithin(List<int> order, int fromGroup, int toGroup, int groupSize)
    {
        int groups = order.Count / groupSize;
        if (groups <= 0 || fromGroup < 0 || fromGroup >= groups) return false;
        toGroup = Mathf.Clamp(toGroup, 0, groups - 1);
        if (toGroup == fromGroup) return false;

        List<int> block = order.GetRange(fromGroup * groupSize, groupSize);
        order.RemoveRange(fromGroup * groupSize, groupSize);
        order.InsertRange(toGroup * groupSize, block);
        return true;
    }

    private static bool IsGroupAligned(IReadOnlyList<int> order, int groupSize)
    {
        if (groupSize <= 1) return true;
        if (order.Count % groupSize != 0) return false;

        for (int i = 0; i < order.Count; i += groupSize)
        {
            int first = order[i];
            if (first % groupSize != 0) return false;
            for (int s = 1; s < groupSize; s++)
                if (order[i + s] != first + s) return false;
        }
        return true;
    }

    private static void InitIdentity(List<int> order, int count)
    {
        order.Clear();
        for (int i = 0; i < count; i++) order.Add(i);
    }

    private static bool IsIdentity(List<int> order, int count)
    {
        if (order.Count != count) return false;
        for (int i = 0; i < count; i++)
            if (order[i] != i) return false;
        return true;
    }

    private static void EnsureOrder(List<int> order, int count)
    {
        if (order.Count == count) return;
        InitIdentity(order, count);
    }

    public float GetValue(int rowIndex, int colIndex)
    {
        if (rowIndex < 0 || rowIndex >= RowCount || colIndex < 0 || colIndex >= ColumnCount)
            return 0f;
        return _values[rowIndex, colIndex];
    }

    public bool TryValueAt(int visRow, int visCol, out double value)
    {
        value = 0d;
        IReadOnlyList<int> rows = RowOrder, cols = ColumnOrder;
        if (visRow < 0 || visRow >= rows.Count || visCol < 0 || visCol >= cols.Count) return false;
        int dataRow = rows[visRow], dataCol = cols[visCol];
        if (!HasValue(dataRow, dataCol)) return false;
        value = GetValue(dataRow, dataCol);
        return true;
    }

    public bool HasValue(int rowIndex, int colIndex)
    {
        return rowIndex >= 0 && rowIndex < _valid.GetLength(0) &&
               colIndex >= 0 && colIndex < _valid.GetLength(1) &&
               _valid[rowIndex, colIndex];
    }

    private static bool TryBaselineRange(float min, float max, out float lo, out float range)
    {
        lo = Mathf.Min(0f, min);
        range = Mathf.Max(0f, max) - lo;
        return range > 0f;
    }

    public float GetHeightFraction(int rowIndex, int colIndex)
    {
        if (!HasValue(rowIndex, colIndex)) return 0f;
        if (_heights != null) return _heights[rowIndex, colIndex];
        if (!TryBaselineRange(_globalMin, _globalMax, out float lo, out float range)) return 0f;
        return (Normalized(rowIndex, colIndex) - lo) / range;
    }

    public float ZeroFraction =>
        _heights == null && TryBaselineRange(_globalMin, _globalMax, out float lo, out float range) ? -lo / range : 0f;

    public float GetColorFraction(int rowIndex, int colIndex)
    {
        if (_heights != null) return HasValue(rowIndex, colIndex) ? _heights[rowIndex, colIndex] : 0f;

        float range = _globalMax - _globalMin;
        if (range <= 0f) return 0f;

        if (!HasValue(rowIndex, colIndex)) return 0f;
        float v = Normalized(rowIndex, colIndex);
        if (float.IsNaN(v)) return 0f;
        return (v - _globalMin) / range;
    }

    protected void RaiseDataLoaded() => RaiseLoadComplete(true);

    protected void RaiseLoadFailed() => RaiseLoadComplete(false);

    private void RaiseLoadComplete(bool loaded)
    {
        _isLoaded = loaded;
        _hiddenGroups.Clear();
        _hiddenRows.Clear();
        InitIdentity(_columnOrder, ColumnCount);
        InitIdentity(_rowOrder, RowCount);
        RebuildVisibleColumns();
        RebuildVisibleRows();
        OnDataLoaded?.Invoke();
    }
}
