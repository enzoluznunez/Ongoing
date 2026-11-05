using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class ManageSheets : MonoBehaviour
{
    public const int FirstSheetId = 1;

    public DataSource dataSource;
    public GameObject sheetPrefab;
    public Material cubeMaterial;

    [Tooltip("Width of one bar's footprint, in metres.")]
    [Range(0.005f, 0.15f)] public float cubeSide = 0.025f;
    [Tooltip("Clear space between neighbouring bars, in metres.")]
    [Range(0f, 0.15f)] public float cubeGap = 0.02f;
    [Tooltip("Extra space between column groups, in metres. Only used when the dataset pairs its columns.")]
    [Range(0f, 0.2f)] public float groupGap = 0.03f;
    [Tooltip("Height of the full value range, in metres.")]
    [Range(0.02f, 1f)] public float maximumHeight = 0.25f;

    [Tooltip("How far ahead of the user a sheet's near edge opens, in metres.")]
    public float minimumZOffsetFromCamera = 0f;

    [Header("Row and column titles")]
    public bool showLabels = true;
    [Tooltip("Distance from the sheet edge out to the title, in metres.")]
    [Range(0f, 0.5f)] public float labelGap = 0.037f;

    [Header("System motion")]
    [Tooltip("Speed of the fastest-moving visible point in any system motion, metres per second.")]
    public float systemMotionSpeed = 0.5f;
    [Tooltip("Shortest system motion, seconds.")]
    public float minMotionSeconds = 0.1f;
    [Tooltip("Longest system motion, seconds; a large motion moves faster than the shared speed instead of running longer.")]
    public float maxMotionSeconds = 1.5f;

    public event Action OnSheetsChanged;
    public event Action<CreateSheet, Vector3, Quaternion, Vector3> OnSheetMoveCommitted;

    private class Projection
    {
        public ProjectionRecord rec;
        public CreateSheet view;
        public CreateSheet source;
        public int visRow;
        public int visCol;
        public float rise;
        public Coroutine riseRoutine;
        public bool pending;
        public float riseSpeed;
    }

    private CreateSheet _sheet;

    private readonly List<Projection> _projections = new List<Projection>();

    private DataSource _bound;
    private Transform _root;

    private int _rowCount;
    private int _colCount;
    private float _cellSize;
    private float _rowPitch;
    private float _groupGap;
    private float _baseY;

    private bool _spawnPending;
    private bool _spawnAnimates = true;

    private bool _sheetsGrabbable;

    public CreateSheet Sheet => _sheet;
    public int RowCount => _rowCount;
    public int ColCount => _colCount;
    public float Height => maximumHeight;
    public bool IsBuilt => _sheet != null;

    private void OnEnable()
    {
        Bind(dataSource != null ? dataSource : ManageDatasets.ActiveSource);
        Settings.Changed += OnSettingChanged;
    }

    private void OnSettingChanged(Setting which)
    {
        if (which != Setting.ColorMode) return;
        DataSource data = _bound;
        if (_sheet != null) _sheet.Repaint(data);
        for (int i = 0; i < _projections.Count; i++)
            if (_projections[i].view != null) _projections[i].view.Repaint(data);
    }

    private void OnDisable()
    {
        Settings.Changed -= OnSettingChanged;
        Unbind();
        if (_readHook != null) _readHook.OnDatasetRead -= OnDatasetRead;
        _readHook = null;
    }

    private void Update()
    {
        if (_spawnPending && TrySpawn()) _spawnPending = false;
        HookReads();

        CreateSheet s = _sheet;
        if (s == null) return;

        bool held = s.IsGrabbed && !ScaleArmed;

        if (!held) s.SetGrabLook(false);
        if (s.PollGrabRelease(out Vector3 pos, out Quaternion rot, out Vector3 scale))
            NotifyMoveCommitted(s, pos, rot, scale);
        if (held) s.SetGrabLook(true);
    }

    private static bool ScaleArmed =>
        Scene.Tools != null && Scene.Tools.SelectedTool == ToolType.Scale;

    private void LateUpdate()
    {
        KeepShown();
        BuildPendingStrip();
        PlaceCurrentProjection();
    }

    public void SetDataSource(DataSource source)
    {
        Unbind();
        dataSource = source;
        ClearProjections();
        if (source != null) RetireSheets();
        else ClearSheets();
        Bind(source);
    }

    private void Bind(DataSource source)
    {
        if (source == null || _bound == source) return;
        _bound = source;
        dataSource = source;
        _bound.OnDataLoaded += RebuildAll;
        _bound.OnLayoutInvalidated += RebuildAll;
        if (_bound.IsLoaded) RebuildAll();
    }

    private void Unbind()
    {
        if (_bound == null) return;
        _bound.OnDataLoaded -= RebuildAll;
        _bound.OnLayoutInvalidated -= RebuildAll;
        _bound = null;
    }

    private static readonly Unity.Profiling.ProfilerMarker RebuildMarker =
        new Unity.Profiling.ProfilerMarker("ManageSheets.RebuildAll");

    public void RebuildAll()
    {
        using var marker = RebuildMarker.Auto();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        DataSource data = _bound;
        if (data == null || !data.IsLoaded) { ClearProjections(); ClearSheets(); return; }

        _rowCount = data.RowOrder.Count;
        _colCount = data.ColumnOrder.Count;
        if (_rowCount == 0 || _colCount == 0) { ClearProjections(); ClearSheets(); return; }

        _cellSize = cubeSide + cubeGap;
        _rowPitch = RowPitch(data);
        _groupGap = data.ColumnGroupSize > 1 ? groupGap : 0f;
        _baseY = data.ZeroFraction * maximumHeight;

        EnsureRoot();

        ClearLeaving();

        bool skipReflow = _skipReflowOnce;
        _skipReflowOnce = false;
        bool reflow = !skipReflow && _sheet != null;
        if (reflow) SnapshotLineCoords();
        bool animate = ChangeAnimates;
        List<LeavingBar> leaving = reflow && animate ? LeavingBars(data) : null;

        if (_sheet == null)
        {
            _sheet = NewSheet();
            BuildWhole(_sheet, data);
            _spawnPending = true;
            float rise = SwitchDuration(_sheet);
            _spawnAnimates = rise > 0f;
            _sheet.PlayGrow(rise);
            PrewarmSecondSheet(data);
        }
        else
        {
            BuildWhole(_sheet, data);
        }

        if (reflow)
        {
            if (!animate) StopReflow();
            else
            {
                float shrink = leaving != null && leaving.Count > 0 ? GrowDuration(_sheet, LeavingTravel(leaving)) : 0f;
                if (shrink > 0f) ShrinkLeaving(leaving, shrink);
                StartReflow(shrink);
                GrowReturning(shrink);
            }
        }

        RebuildProjections();
        if (!_spawnPending) SyncProjectionToStack();
        OnSheetsChanged?.Invoke();
        HitchLog.Note($"sheet rebuilt, {_rowCount}x{_colCount} bars, {clock.Elapsed.TotalMilliseconds:F1} ms");
    }

    private const float RowTitleGap = 0.01f;

    private float RowPitch(DataSource data)
    {
        float pitch = _cellSize;
        if (!showLabels || data == null) return pitch;

        if (data == _pitchData && Mathf.Approximately(_cellSize, _pitchCell)) return _pitch;

        IReadOnlyList<string> titles = data.RowTitles;
        float longest = 0f;
        for (int i = 0; i < titles.Count; i++)
            longest = Mathf.Max(longest, SheetLabels.TitleLength(titles[i], _cellSize));
        _pitchData = data;
        _pitchCell = _cellSize;
        _pitch = Mathf.Max(pitch, longest + RowTitleGap);
        return _pitch;
    }

    private DataSource _pitchData;
    private float _pitchCell;
    private float _pitch;

    private CreateSheet CreateDetachedPiece(string label)
    {
        CreateSheet s = MakeSheet();
        s.name = label;
        s.sheetId = -1;
        s.SetGrabbable(false);
        s.SetPickable(false);
        return s;
    }

    public SheetLabelStyle LabelStyle() => new SheetLabelStyle
    {
        show = showLabels,
        color = Style.White,
        gap = labelGap
    };

    public List<string> CategoriesIn(int rowMin, int rowMax)
    {
        var found = new List<string>();
        if (_bound == null || !_bound.HasRowCategories) return found;

        IReadOnlyList<int> rowOrder = _bound.RowOrder;
        var rows = new List<int>();
        for (int vr = rowMin; vr <= rowMax; vr++)
            if (vr >= 0 && vr < rowOrder.Count) rows.Add(rowOrder[vr]);

        return _bound.CategoriesInOrder(rows);
    }

    private void BuildWhole(CreateSheet sheet, DataSource data) => BuildSpan(sheet, data, 0, _rowCount - 1, 0, _colCount - 1);

    private void BuildSpan(CreateSheet sheet, DataSource data, int rMin, int rMax, int cMin, int cMax) =>
        sheet.Build(data, cubeMaterial, rMin, rMax, cMin, cMax,
            _cellSize, _rowPitch, _groupGap, maximumHeight, _baseY, cubeSide, LabelStyle());

    public CreateSheet SheetAt(int visRow, int visCol) =>
        _sheet != null && _sheet.Contains(visRow, visCol) ? _sheet : null;

    public CreateCube CubeAt(int visRow, int visCol)
    {
        CreateSheet sheet = SheetAt(visRow, visCol);
        return sheet != null ? sheet.CubeAt(visRow, visCol) : null;
    }

    public enum UndoResult { Applied, Stale, Unreachable }

    public UndoResult Undo(Edit e)
    {
        if (e == null) return UndoResult.Stale;

        switch (e.kind)
        {
            case EditKind.Move:
            case EditKind.Rotate:
            case EditKind.Scale:
                return RestoreSheetPose(e.move.sheetId, e.move.prePos, e.move.preRot, e.move.preScale,
                        animate: AsAgent)
                    ? UndoResult.Applied : UndoResult.Stale;

            case EditKind.Profile:
                return UndoResult.Applied;

            case EditKind.Filter:
                if (_bound == null) return UndoResult.Unreachable;
                return _bound.SetHiddenOn(e.filterIsRow, e.filterPreHidden, out _)
                    ? UndoResult.Applied : UndoResult.Stale;

            case EditKind.Sort:
                if (_bound == null) return UndoResult.Unreachable;
                if (!AsAgent) SuppressNextReflow();
                bool reordered = e.reorderPreFull != null
                    ? _bound.SetFullOrder(e.reorderIsColumn, e.reorderPreFull)
                    : e.reorderIsColumn
                        ? _bound.SetColumnOrder(e.reorderPreOrder)
                        : _bound.SetRowOrder(e.reorderPreOrder);
                if (!reordered) _skipReflowOnce = false;
                return reordered ? UndoResult.Applied : UndoResult.Stale;
        }

        return UndoResult.Stale;
    }

    public void ReplayEdits(IReadOnlyList<Edit> edits)
    {
        if (edits == null || _bound == null) return;

        for (int i = 0; i < edits.Count; i++)
        {
            Edit e = edits[i];
            switch (e.kind)
            {
                case EditKind.Move:
                case EditKind.Rotate:
                case EditKind.Scale:
                    RestoreSheetPose(e.move.sheetId, e.move.postPos, e.move.postRot, e.move.postScale);
                    break;

                case EditKind.Filter:
                    _bound.SetHiddenOn(e.filterIsRow, e.filterPostHidden, out _);
                    break;
            }
        }

        if (!_spawnPending) SyncProjectionToStack();
    }

    public bool PushProjection(ProjectionRecord rec, EditKind kind, out bool lowered)
    {
        lowered = false;
        if (_bound == null) return false;

        lowered = IsProjected(rec);
        rec.lower = lowered;
        ManageDatasets.ActiveEdits.PushProjection(rec, kind);
        SyncProjectionToStack(StateChannel.InAgentCall);
        return true;
    }

    public bool IsProjected(ProjectionRecord rec) => FindProjection(rec) >= 0;

    private int FindProjection(ProjectionRecord rec)
    {
        for (int i = 0; i < _projections.Count; i++)
            if (SameRecord(_projections[i].rec, rec)) return i;
        return -1;
    }

    public void SyncProjectionToStack(bool animateNew = false, bool spread = false)
    {
        var wanted = new List<ProjectionRecord>();
        EditList edits = ManageDatasets.ActiveEdits;
        for (int i = 0; i < edits.Count; i++)
        {
            Edit e = edits[i];
            if (e.kind != EditKind.Profile) continue;

            int at = IndexOfRecord(wanted, e.projection);
            if (e.projection.lower) { if (at >= 0) wanted.RemoveAt(at); }
            else if (at < 0) wanted.Add(e.projection);
        }

        float lowerSpeed = StripSpeed(animateNew);
        for (int i = _projections.Count - 1; i >= 0; i--)
            if (IndexOfRecord(wanted, _projections[i].rec) < 0) LowerProjectionAt(i, lowerSpeed);

        foreach (ProjectionRecord rec in wanted)
            if (FindProjection(rec) < 0) ShowProjection(rec, animateNew, spread || animateNew);
    }

    private int IndexOfRecord(List<ProjectionRecord> records, ProjectionRecord rec)
    {
        for (int i = 0; i < records.Count; i++)
            if (SameRecord(records[i], rec)) return i;
        return -1;
    }

    public void CollectProjections(List<ProjectionRecord> into)
    {
        into.Clear();
        foreach (Projection p in _projections) into.Add(p.rec);
    }

    public bool TryResolveProjection(ProjectionRecord rec, out int visRow, out int visCol)
    {
        visRow = visCol = -1;
        if (_bound == null) return false;

        visRow = _bound.VisIndexOf(false, rec.dataRow);
        visCol = _bound.VisIndexOf(true, rec.dataCol);
        if (rec.isColumn ? visCol < 0 : visRow < 0) return false;
        if (visRow < 0) visRow = 0;
        if (visCol < 0) visCol = 0;
        return true;
    }

    private bool SameRecord(ProjectionRecord a, ProjectionRecord b)
    {
        if (a.isColumn != b.isColumn) return false;
        if (!a.isColumn) return a.dataRow == b.dataRow;
        return _bound != null && _bound.IsGrouped(true)
            ? _bound.DataGroupOf(a.dataCol) == _bound.DataGroupOf(b.dataCol)
            : a.dataCol == b.dataCol;
    }

    private void ShowProjection(ProjectionRecord rec, bool animate, bool defer)
    {
        if (_bound == null || !TryResolveProjection(rec, out _, out _)) return;
        float speed = StripSpeed(animate);
        if (defer) _projections.Add(new Projection { rec = rec, pending = true, riseSpeed = speed });
        else BuildStrip(rec, speed);
    }

    private float StripSpeed(bool animate) =>
        animate && isActiveAndEnabled && !AgentInstant ? ActiveMotionSpeed : 0f;

    private void BuildPendingStrip()
    {
        for (int i = 0; i < _projections.Count; i++)
        {
            Projection p = _projections[i];
            if (!p.pending) continue;
            _projections.RemoveAt(i);
            BuildStrip(p.rec, p.riseSpeed, i);
            return;
        }
    }

    private bool BuildStrip(ProjectionRecord rec, float riseSpeed, int at = -1)
    {
        if (!TryResolveProjection(rec, out _, out _)) return false;

        CreateSheet view = TakeStrip();
        if (view == null) return false;

        Projection p = new Projection { rec = rec, view = view };
        if (!BuildProjection(p))
        {
            PutStripAway(view);
            return false;
        }

        if (at >= 0 && at <= _projections.Count) _projections.Insert(at, p);
        else _projections.Add(p);
        if (riseSpeed > 0f) StartProjectionRise(p, riseSpeed);
        return true;
    }

    private void StartProjectionRise(Projection p, float speed)
    {
        StopRise(p);
        if (!isActiveAndEnabled || speed <= 0f) return;

        p.rise = maximumHeight + p.rec.lift;
        PlaceProjection(p);
        p.riseRoutine = StartCoroutine(RiseRoutine(p, p.rise, 0f, speed, _projections));
    }

    private IEnumerator RiseRoutine(Projection p, float from, float to, float speed, List<Projection> members)
    {
        float sourceScale = p.source != null ? Mathf.Abs(p.source.transform.lossyScale.y) : 1f;
        float duration = MotionSeconds(Mathf.Abs(to - from) * sourceScale, speed);
        float t = 0f;
        while (t < duration && members.Contains(p))
        {
            yield return null;
            t += Time.deltaTime;
            p.rise = Mathf.Lerp(from, to, Style.EaseOut(t / duration));
        }
        p.rise = to;
        p.riseRoutine = null;
        if (members == _lowering) FinishLowering(p);
    }

    private void StopRise(Projection p)
    {
        if (p.riseRoutine != null) { StopCoroutine(p.riseRoutine); p.riseRoutine = null; }
        p.rise = 0f;
    }

    private void RemoveProjectionAt(int index)
    {
        Projection p = _projections[index];
        StopRise(p);
        PutStripAway(p.view);
        _projections.RemoveAt(index);
    }

    private readonly List<Projection> _lowering = new List<Projection>();

    private void LowerProjectionAt(int index, float speed)
    {
        Projection p = _projections[index];
        _projections.RemoveAt(index);
        float from = p.rise;
        StopRise(p);
        if (p.pending || p.view == null || speed <= 0f || !isActiveAndEnabled)
        {
            PutStripAway(p.view);
            return;
        }

        _lowering.Add(p);
        p.rise = from;
        p.riseRoutine = StartCoroutine(RiseRoutine(p, from, maximumHeight + p.rec.lift, speed, _lowering));
    }

    private void FinishLowering(Projection p)
    {
        if (!_lowering.Remove(p)) return;
        if (p.riseRoutine != null) { StopCoroutine(p.riseRoutine); p.riseRoutine = null; }
        PutStripAway(p.view);
    }

    private void CompleteLowering()
    {
        for (int i = _lowering.Count - 1; i >= 0; i--) FinishLowering(_lowering[i]);
    }

    private void PlaceLowering()
    {
        for (int i = _lowering.Count - 1; i >= 0; i--)
            if (!PlaceProjection(_lowering[i])) FinishLowering(_lowering[i]);
    }

    private const int StripsReady = 4;
    private readonly List<CreateSheet> _stripSpares = new List<CreateSheet>();

    private CreateSheet TakeStrip()
    {
        CreateSheet s = PopLive(_stripSpares) ?? CreateDetachedPiece("Projection_Strip");
        s.SetShown(true);
        return s;
    }

    private void PutStripAway(CreateSheet s)
    {
        if (s == null || _stripSpares.Contains(s)) return;
        s.CompleteGrow();
        s.SetShown(false);
        _stripSpares.Add(s);
    }

    private void ClearProjections()
    {
        CompleteLowering();
        for (int i = _projections.Count - 1; i >= 0; i--) RemoveProjectionAt(i);
    }

    private void RebuildProjections()
    {
        for (int i = _projections.Count - 1; i >= 0; i--)
        {
            Projection p = _projections[i];
            if (p.pending) continue;
            if (p.view == null || !BuildProjection(p)) RemoveProjectionAt(i);
        }
    }

    public bool LinesDragged { get; set; }

    private bool _stripsFollowing;

    private void FollowLines()
    {
        bool moving = _reflow != null || LinesDragged;
        if (!moving && !_stripsFollowing) return;
        _stripsFollowing = moving;

        for (int i = 0; i < _projections.Count; i++)
        {
            Projection p = _projections[i];
            if (p.view == null || p.source == null) continue;
            bool columns = !p.rec.isColumn;
            int min = columns ? p.view.colMin : p.view.rowMin;
            int max = columns ? p.view.colMax : p.view.rowMax;
            for (int line = min; line <= max; line++)
                p.view.LayoutLine(columns, line, p.source.LineOffset(columns, line));
        }
    }

    public void CompleteLineMotion() => CompleteReflow();

    private void PlaceCurrentProjection()
    {
        FollowLines();
        for (int i = _projections.Count - 1; i >= 0; i--)
        {
            Projection p = _projections[i];
            if (p.pending || PlaceProjection(p)) continue;
            if (p.view == null || !BuildProjection(p)) RemoveProjectionAt(i);
        }
        PlaceLowering();
    }

    public bool TryStripPoint(CreateSheet source, bool column, int visLine, float lift, out Vector3 world)
    {
        world = Vector3.zero;
        if (source == null || !source.IsBuilt) return false;

        Vector3 originLocal = column
            ? new Vector3(ColumnStripOffset(source, visLine), 0f, 0f)
            : new Vector3(0f, 0f, source.LineOffset(false, visLine));
        world = source.transform.TransformPoint(originLocal + Vector3.up * (maximumHeight + lift));
        return true;
    }

    public void ColumnStripSpan(int visCol, out int lo, out int hi)
    {
        lo = hi = visCol;
        if (_bound == null || !_bound.IsGrouped(true)) return;
        _bound.GroupSpan(true, _bound.GroupOf(true, visCol), out lo, out hi);
    }

    private float ColumnStripOffset(CreateSheet source, int visCol)
    {
        ColumnStripSpan(visCol, out int lo, out int hi);
        return (source.LineOffset(true, lo) + source.LineOffset(true, hi)) * 0.5f;
    }

    private bool PlaceProjection(Projection p)
    {
        if (p.view == null || p.source == null || !p.source.IsBuilt) return false;

        if (!TryStripPoint(p.source, p.rec.isColumn, p.rec.isColumn ? p.visCol : p.visRow,
                p.rec.lift, out Vector3 world)) return false;

        if (p.rise > 0f)
            world -= p.source.transform.TransformVector(Vector3.up * p.rise);

        Transform src = p.source.transform;

        p.view.SetShown(p.source.Shown);
        Transform t = p.view.transform;
        Vector3 local = transform.InverseTransformPoint(world);
        if (t.localPosition != local || t.localRotation != src.localRotation)
            t.SetLocalPositionAndRotation(local, src.localRotation);
        if (t.localScale != src.localScale) t.localScale = src.localScale;
        return true;
    }

    private bool BuildProjection(Projection p)
    {
        if (p.view == null || !TryResolveProjection(p.rec, out int visRow, out int visCol)) return false;

        CreateSheet source = SheetAt(visRow, visCol);
        if (source == null) return false;

        p.source = source;
        p.visRow = visRow;
        p.visCol = visCol;

        return BuildStripProjection(p, source, visRow, visCol);
    }

    private bool BuildStripProjection(Projection p, CreateSheet source,
        int visRow, int visCol)
    {
        ProjectionRecord rec = p.rec;
        CreateSheet view = p.view;
        int rMin, rMax, cMin, cMax;

        if (rec.isColumn)
        {
            rMin = source.rowMin; rMax = source.rowMax;
            ColumnStripSpan(visCol, out cMin, out cMax);
        }
        else
        {
            rMin = rMax = visRow;
            cMin = source.colMin; cMax = source.colMax;
        }

        BuildSpan(view, _bound, rMin, rMax, cMin, cMax);
        view.SetPickable(false);

        return PlaceProjection(p);
    }

    public void SetGrabbable(bool on)
    {
        _sheetsGrabbable = on;
        if (_sheet != null) _sheet.SetGrabbable(GrabbableNow(_sheet));
    }

    private bool GrabbableNow(CreateSheet sheet) => _sheetsGrabbable;

    private Func<CreateSheet, ITransformer> _twoGrabFor;

    public void SetOneGrab()
    {
        _twoGrabFor = null;
        if (_sheet != null) _sheet.SetOneGrab();
    }

    public void SetTwoGrab(Func<CreateSheet, ITransformer> transformerFor)
    {
        if (transformerFor == null) return;

        _twoGrabFor = transformerFor;
        ApplyTwoGrab(_sheet, transformerFor);
    }

    private void ReanchorGrab(CreateSheet sheet)
    {
        if (_twoGrabFor == null || sheet == null) return;
        ApplyTwoGrab(sheet, _twoGrabFor);
    }

    private static void ApplyTwoGrab(CreateSheet sheet, Func<CreateSheet, ITransformer> transformerFor)
    {
        if (sheet == null) return;

        try
        {
            sheet.SetTwoGrab(transformerFor(sheet));
        }
        catch (Exception e)
        {
            Debug.LogError($"[ManageSheets] Sheet {sheet.sheetId} could not take the two-hand transformer, " +
                           $"so it stays on its previous grab behaviour: {e}");
        }
    }

    public void ResetGrabs(bool reattach)
    {
        _twoGrabFor = null;
        CreateSheet s = _sheet;
        if (s == null) return;
        s.ForgetGrabLook();
        s.transform.localRotation = Quaternion.identity;
        s.transform.localScale = Vector3.one;
        if (reattach) s.transform.localPosition = SpawnOffset(s);
    }

    public void NotifyMoveCommitted(CreateSheet sheet, Vector3 prePos, Quaternion preRot, Vector3 preScale)
    {
        if (sheet == null) return;
        OnSheetMoveCommitted?.Invoke(sheet, prePos, preRot, preScale);
    }

    private struct LineMove
    {
        public CreateSheet piece;
        public bool columns;
        public int line;
        public float from;
        public float to;
        public float duration;
    }

    private Coroutine _reflow;
    private List<LineMove> _reflowMoves;
    private bool _skipReflowOnce;

    private class GlideState
    {
        public Vector3 toPos;
        public Quaternion toRot;
        public Vector3 toScale;
        public Coroutine routine;
        public bool amend = true;
    }

    private readonly Dictionary<CreateSheet, GlideState> _pieceGlides = new Dictionary<CreateSheet, GlideState>();

    public void GetCommittedPose(CreateSheet piece, out Vector3 pos, out Quaternion rot, out Vector3 scale)
    {
        Transform t = piece.transform;
        bool gliding = _pieceGlides.TryGetValue(piece, out GlideState g);
        pos = gliding ? g.toPos : t.localPosition;
        rot = gliding ? g.toRot : t.localRotation;
        scale = gliding ? g.toScale : t.localScale;
    }

    public void SuppressNextReflow() => _skipReflowOnce = true;

    private float _agentMotionSpeed = -1f;
    private bool _agentMotionInstant;

    public void SetAgentMotion(float speed, bool instant)
    {
        _agentMotionSpeed = speed;
        _agentMotionInstant = instant;
    }

    public bool ForceAgentMotion { get; set; }

    private bool AsAgent => StateChannel.InAgentCall || ForceAgentMotion;

    public bool AgentMotionAnimates => AsAgent && !_agentMotionInstant;

    private bool ChangeAnimates => Style.Animate && (AsAgent ? !_agentMotionInstant : UserChangeAnimates);

    public bool UserChangeAnimates { get; set; }

    public float AgentMotionSeconds(float meters) =>
        !AsAgent ? -1f : _agentMotionInstant ? 0f : MotionDuration(meters);

    private float ActiveMotionSpeed =>
        AsAgent && _agentMotionSpeed > 0f ? _agentMotionSpeed : systemMotionSpeed;

    private bool AgentInstant => AsAgent && _agentMotionInstant;

    private float GrowDuration(CreateSheet piece, float localTravel)
    {
        if (!isActiveAndEnabled || ActiveMotionSpeed <= 0f || AgentInstant || piece == null) return 0f;
        return MotionDuration(localTravel * Mathf.Abs(piece.transform.lossyScale.y));
    }

    public float UserMotionSeconds(float meters) =>
        systemMotionSpeed <= 0f ? 0f : MotionSeconds(meters, systemMotionSpeed);

    private float SwitchDuration(CreateSheet piece) =>
        piece == null ? 0f : GrowDuration(piece, piece.GrowTravel());

    private float MotionDuration(float meters) => MotionSeconds(meters, ActiveMotionSpeed);

    private float MotionSeconds(float meters, float speed) =>
        !Style.Animate ? 0f : Mathf.Clamp(meters / Mathf.Max(speed, 1e-3f), minMotionSeconds, maxMotionSeconds);

    private float PieceRadius(CreateSheet piece)
    {
        float halfCols = piece.ColCount * _cellSize * 0.5f;
        float halfRows = piece.RowCount * _rowPitch * 0.5f;
        return Mathf.Sqrt(halfCols * halfCols + halfRows * halfRows);
    }

    private readonly Dictionary<int, float> _oldColumns = new Dictionary<int, float>();
    private readonly Dictionary<int, float> _oldRows = new Dictionary<int, float>();

    private void SnapshotLineCoords()
    {
        _oldColumns.Clear();
        _oldRows.Clear();
        _sheet.CollectLineCoords(_oldColumns, _oldRows);
    }

    private struct LeavingBar
    {
        public CreateSheet piece;
        public Vector3 center;
        public Vector3 size;
        public Color color;
    }

    private readonly List<CreateCube> _leaving = new List<CreateCube>();
    private Coroutine _leavingRoutine;

    private readonly HashSet<int> _shownRows = new HashSet<int>();
    private readonly HashSet<int> _shownColumns = new HashSet<int>();

    private List<LeavingBar> LeavingBars(DataSource data)
    {
        _shownRows.Clear();
        _shownRows.UnionWith(data.RowOrder);
        _shownColumns.Clear();
        _shownColumns.UnionWith(data.ColumnOrder);
        var found = new List<LeavingBar>();
        foreach (CreateCube cube in _sheet.LiveBars)
        {
            if (cube == null || !cube.IsVisible) continue;
            if (_shownRows.Contains(cube.dataRow) && _shownColumns.Contains(cube.dataCol)) continue;
            found.Add(new LeavingBar
            {
                piece = _sheet,
                center = cube.transform.localPosition,
                size = cube.transform.localScale,
                color = cube.BaseColor
            });
        }
        return found;
    }

    private float LeavingTravel(List<LeavingBar> bars)
    {
        float most = 0f;
        for (int i = 0; i < bars.Count; i++)
            most = Mathf.Max(most, CreateSheet.BarTravel(bars[i].center, bars[i].size, _baseY));
        return most;
    }

    private void ShrinkLeaving(List<LeavingBar> bars, float duration)
    {
        for (int i = 0; i < bars.Count; i++)
        {
            LeavingBar b = bars[i];
            if (b.piece == null) continue;
            CreateCube cube = TakeLeavingBar();
            if (cube.transform.parent != b.piece.transform) cube.transform.SetParent(b.piece.transform, false);
            cube.SetColor(b.color);
            cube.SetBox(b.center, b.size);
            cube.SetVisible(true);
            _leaving.Add(cube);
        }
        _leavingRoutine = StartCoroutine(ShrinkRoutine(bars, duration));
    }

    private IEnumerator ShrinkRoutine(List<LeavingBar> bars, float duration)
    {
        yield return Style.Tween(duration, eased =>
        {
            for (int i = 0; i < _leaving.Count && i < bars.Count; i++)
                if (_leaving[i] != null) CreateSheet.GrowBar(_leaving[i], bars[i].center, bars[i].size, _baseY, 1f - eased);
        });
        _leavingRoutine = null;
        ClearLeaving();
    }

    private void ClearLeaving()
    {
        if (_leavingRoutine != null) { StopCoroutine(_leavingRoutine); _leavingRoutine = null; }
        for (int i = 0; i < _leaving.Count; i++)
        {
            if (_leaving[i] == null) continue;
            _leaving[i].SetVisible(false);
            _leavingSpares.Add(_leaving[i]);
        }
        _leaving.Clear();
    }

    private readonly List<CreateCube> _leavingSpares = new List<CreateCube>();

    private CreateCube TakeLeavingBar() => PopLive(_leavingSpares) ?? MakeLeavingBar();

    public static T PopLive<T>(List<T> list) where T : UnityEngine.Object
    {
        while (list.Count > 0)
        {
            T last = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            if (last != null) return last;
        }
        return null;
    }

    private CreateCube MakeLeavingBar()
    {
        EnsureRoot();
        var go = new GameObject("Leaving", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(_root, false);
        CreateCube cube = go.AddComponent<CreateCube>();
        cube.Init(cubeMaterial);
        cube.SetPickable(false);
        cube.SetVisible(false);
        return cube;
    }

    private void GrowReturning(float delay)
    {
        CreateSheet s = _sheet;
        var returning = new List<CreateCube>();
        foreach (CreateCube cube in s.LiveBars)
            if (cube != null && (!_oldRows.ContainsKey(cube.dataRow) || !_oldColumns.ContainsKey(cube.dataCol)))
                returning.Add(cube);
        if (returning.Count == 0) return;
        float duration = GrowDuration(s, s.GrowTravel(returning));
        if (duration > 0f) s.PlayGrowOf(returning, duration, delay);
    }

    private void StopReflow()
    {
        if (_reflow != null) { StopCoroutine(_reflow); _reflow = null; }
    }

    public IEnumerator WaitForReflow()
    {
        while (_reflow != null) yield return null;
    }

    private void StartReflow(float delay)
    {
        StopReflow();
        if (_bound == null) return;

        var moves = new List<LineMove>();
        CollectMoves(_sheet, true, _bound.ColumnOrder, _oldColumns, moves);
        CollectMoves(_sheet, false, _bound.RowOrder, _oldRows, moves);
        if (moves.Count == 0) return;

        for (int i = 0; i < moves.Count; i++)
            moves[i].piece.LayoutLine(moves[i].columns, moves[i].line, moves[i].from);
        _reflowMoves = moves;
        _reflow = StartCoroutine(ReflowRoutine(moves, delay));
    }

    private void CollectMoves(CreateSheet s, bool columns, IReadOnlyList<int> order,
        Dictionary<int, float> old, List<LineMove> moves)
    {
        int min = columns ? s.colMin : s.rowMin;
        int max = columns ? s.colMax : s.rowMax;
        for (int v = min; v <= max; v++)
        {
            if (v < 0 || v >= order.Count) continue;
            if (!old.TryGetValue(order[v], out float from)) continue;

            float to = s.LineCoord(columns, v);
            if (Mathf.Abs(from - to) < 1e-4f) continue;

            Vector3 lossy = s.transform.lossyScale;
            float axisScale = Mathf.Abs(columns ? lossy.x : lossy.z);
            moves.Add(new LineMove
            {
                piece = s,
                columns = columns,
                line = v,
                from = from,
                to = to,
                duration = MotionDuration(Mathf.Abs(from - to) * axisScale)
            });
        }
    }

    private IEnumerator ReflowRoutine(List<LineMove> moves, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f;
        bool moving = true;
        while (moving)
        {
            yield return null;
            t += Time.deltaTime;
            moving = false;
            for (int i = 0; i < moves.Count; i++)
            {
                LineMove m = moves[i];
                if (m.piece == null) continue;
                float k = m.duration > 0f ? Style.EaseOut(t / m.duration) : 1f;
                m.piece.LayoutLine(m.columns, m.line, Mathf.Lerp(m.from, m.to, k));
                if (k < 1f) moving = true;
            }
        }
        _reflow = null;
        _reflowMoves = null;
    }

    private void CompleteReflow()
    {
        ClearLeaving();
        StopReflow();
        if (_reflowMoves == null) return;

        for (int i = 0; i < _reflowMoves.Count; i++)
        {
            LineMove m = _reflowMoves[i];
            if (m.piece != null) m.piece.LayoutLine(m.columns, m.line, m.to);
        }
        _reflowMoves = null;
    }

    private bool TryStopGlide(CreateSheet piece, out GlideState g)
    {
        if (piece == null || !_pieceGlides.TryGetValue(piece, out g)) { g = null; return false; }
        if (g.routine != null) StopCoroutine(g.routine);
        _pieceGlides.Remove(piece);
        return true;
    }

    public void CompletePieceMotion(CreateSheet piece)
    {
        if (TryStopGlide(piece, out GlideState g)) LandGlide(piece, g);
    }

    private static void LandGlide(CreateSheet piece, GlideState g)
    {
        Transform t = piece.transform;
        t.localPosition = g.toPos;
        t.localRotation = g.toRot;
        t.localScale = g.toScale;
    }

    public void Interrupt()
    {
        if (_sheet != null)
        {
            HaltPieceMotion(_sheet);
            _sheet.CompleteGrow();
        }

        CompleteReflow();

        bool risen = false;
        foreach (Projection p in _projections)
        {
            risen |= p.rise > 0f;
            StopRise(p);
            p.riseSpeed = 0f;
        }
        CompleteLowering();
        if (risen) PlaceCurrentProjection();
    }

    public void AmendReorderRecord()
    {
        EditList edits = ManageDatasets.ActiveEdits;
        if (edits == null || _bound == null) return;

        for (int i = edits.Count - 1; i >= 0; i--)
        {
            Edit e = edits[i];
            if (e.kind != EditKind.Sort || e.reorderPreOrder == null) continue;

            IReadOnlyList<int> live = e.reorderIsColumn ? _bound.ColumnOrder : _bound.RowOrder;
            int changed = 0;
            for (int v = 0; v < live.Count && v < e.reorderPreOrder.Count; v++)
                if (live[v] != e.reorderPreOrder[v]) changed++;

            e.reorderLines = changed;
            if (changed == 0) edits.DropAt(i);
            return;
        }
    }

    public void HaltPieceMotion(CreateSheet piece)
    {
        if (!TryStopGlide(piece, out GlideState g)) return;
        if (g.amend) AmendPoseRecord(piece);
        else LandGlide(piece, g);
    }

    private void AmendPoseRecord(CreateSheet piece)
    {
        EditList edits = ManageDatasets.ActiveEdits;
        if (edits == null || piece == null) return;

        for (int i = edits.Count - 1; i >= 0; i--)
        {
            Edit e = edits[i];
            if (!IsPoseEdit(e, piece.sheetId)) continue;

            Transform t = piece.transform;
            MoveRecord m = e.move;
            m.postPos = t.localPosition;
            m.postRot = t.localRotation;
            m.postScale = t.localScale;
            m.distance = transform.TransformVector(m.postPos - m.prePos).magnitude;
            e.move = m;

            if (m.IsNoChange) edits.DropAt(i);
            return;
        }
    }

    public void CancelPieceMotion(CreateSheet piece) => TryStopGlide(piece, out _);

    public void AnimatePieceFrom(CreateSheet piece, Vector3 prePos, Quaternion preRot, Vector3 preScale,
        bool amend = true)
    {
        if (piece == null || !isActiveAndEnabled) return;
        if (AgentInstant) { CancelPieceMotion(piece); return; }
        CompletePieceMotion(piece);

        Transform t = piece.transform;
        var g = new GlideState
        {
            toPos = t.localPosition, toRot = t.localRotation, toScale = t.localScale, amend = amend
        };

        float rootScale = Mathf.Abs(transform.lossyScale.x);
        float radius = PieceRadius(piece) * rootScale;
        float atScale = radius * Mathf.Max(Mathf.Abs(preScale.x), Mathf.Abs(g.toScale.x));

        float moveMeters = transform.TransformVector(g.toPos - prePos).magnitude;
        float turnMeters = Quaternion.Angle(preRot, g.toRot) * Mathf.Deg2Rad * atScale;
        float scaleMeters = Mathf.Abs(g.toScale.x - preScale.x) * radius;
        float duration = MotionDuration(Mathf.Max(moveMeters, Mathf.Max(turnMeters, scaleMeters)));

        t.localPosition = prePos;
        t.localRotation = preRot;
        t.localScale = preScale;

        g.routine = StartCoroutine(PieceGlideRoutine(piece, g, prePos, preRot, preScale, duration));
        _pieceGlides[piece] = g;
    }

    private IEnumerator PieceGlideRoutine(CreateSheet piece, GlideState g,
        Vector3 fromPos, Quaternion fromRot, Vector3 fromScale, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            yield return null;
            if (piece == null) yield break;
            if (piece.IsGrabbed) { _pieceGlides.Remove(piece); yield break; }

            t += Time.deltaTime;
            float k = Style.EaseOut(t / duration);
            Transform tr = piece.transform;
            tr.localPosition = Vector3.Lerp(fromPos, g.toPos, k);
            tr.localRotation = Quaternion.Slerp(fromRot, g.toRot, k);
            tr.localScale = Vector3.Lerp(fromScale, g.toScale, k);
        }
        if (piece != null && duration <= 0f)
            piece.transform.SetLocalPositionAndRotation(g.toPos, g.toRot);
        if (piece != null && duration <= 0f) piece.transform.localScale = g.toScale;
        _pieceGlides.Remove(piece);
    }

    public void CommitPendingGrabs()
    {
        CreateSheet s = _sheet;
        if (s != null && s.ForceGrabRelease(out Vector3 pos, out Quaternion rot, out Vector3 scale))
            NotifyMoveCommitted(s, pos, rot, scale);
    }

    public bool RestoreSheetPose(int sheetId, Vector3 pos, Quaternion rot, Vector3 scale, bool animate = false)
    {
        CreateSheet sheet = Sheet;
        if (sheet == null || sheet.sheetId != sheetId) return false;
        CancelPieceMotion(sheet);
        sheet.ForgetGrabLook();
        Transform t = sheet.transform;
        Vector3 fromPos = t.localPosition;
        Quaternion fromRot = t.localRotation;
        Vector3 fromScale = t.localScale;
        t.localPosition = pos;
        t.localRotation = rot;
        t.localScale = scale;
        ReanchorGrab(sheet);
        if (animate) AnimatePieceFrom(sheet, fromPos, fromRot, fromScale, amend: false);
        return true;
    }

    public void SetLineTint(CreateSheet target, bool columns, int min, int max, float swell)
    {
        if (_sheet == null) return;
        if (_sheet == target) _sheet.SetHoverTint(columns, min, max, swell);
        else _sheet.ClearTint();
    }

    public void ClearHoverTint()
    {
        if (_sheet != null) _sheet.ClearTint();
    }

    private void EnsureRoot()
    {
        if (_root != null) return;
        GameObject go = new GameObject("Sheets");
        _root = go.transform;
        _root.SetParent(transform, false);
    }

    private const int SpareCount = 2;
    private readonly List<CreateSheet> _spares = new List<CreateSheet>();

    private CreateSheet _sinking;
    private bool _secondMade;

    private Vector3 _homePos;
    private Quaternion _homeRot;
    private bool _hasHome;

    private CreateSheet MakeSheet()
    {
        EnsureRoot();
        GameObject made = sheetPrefab != null ? Instantiate(sheetPrefab, _root) : new GameObject("Sheet");
        if (made.transform.parent != _root) made.transform.SetParent(_root, false);
        CreateSheet s = made.GetComponent<CreateSheet>();
        return s != null ? s : made.AddComponent<CreateSheet>();
    }

    private CreateSheet NewSheet()
    {
        CreateSheet s = TakeSpare() ?? MakeSheet();
        GameObject go = s.gameObject;
        s.SetShown(false);
        s.SetGrabbable(GrabbableNow(s));
        s.sheetId = FirstSheetId;
        go.name = $"Sheet_{s.sheetId}";
        return s;
    }

    private CreateSheet TakeSpare()
    {
        for (int i = 0; i < _spares.Count; i++)
            if (_spares[i] != null) _spares[i].CompleteFall();
        CreateSheet s = PopLive(_spares);
        if (s == null) return null;

        Transform from = sheetPrefab != null ? sheetPrefab.transform : null;
        s.ResetForReuse(from != null ? from.localPosition : Vector3.zero,
            from != null ? from.localRotation : Quaternion.identity,
            from != null ? from.localScale : Vector3.one);
        return s;
    }

    private void ClearSheets()
    {
        FinishLeaving();
        ForgetSheets();
        Park(_sheet);
        _sheet = null;
    }

    private void RetireSheets()
    {
        FinishLeaving();
        ForgetSheets();
        CreateSheet s = _sheet;
        _sheet = null;
        if (s == null || !s.Shown) { Park(s); return; }

        _sinking = s;
        s.SetGrabbable(false);
        s.ClearTint();
        s.PlayFall(SwitchDuration(s), () =>
        {
            if (_sinking == s) _sinking = null;
            Park(s);
        });
    }

    private void FinishLeaving()
    {
        if (_sinking != null) _sinking.CompleteFall();
        _sinking = null;
    }

    private void ForgetSheets()
    {
        ClearLeaving();
        StopReflow();
        _pieceGlides.Clear();
    }

    private void Park(CreateSheet s)
    {
        if (s == null || _spares.Contains(s)) return;
        if (_spares.Count >= SpareCount) { Destroy(s.gameObject); return; }
        _spares.Add(s);
        s.SetShown(false);
    }

    private ManageDatasets _readHook;

    private void HookReads()
    {
        ManageDatasets datasets = ManageDatasets.Instance;
        if (datasets == null || _readHook == datasets) return;
        if (_readHook != null) _readHook.OnDatasetRead -= OnDatasetRead;
        _readHook = datasets;
        datasets.OnDatasetRead += OnDatasetRead;
        foreach (ManageDatasets.Dataset d in datasets.Datasets)
            if (d.loaded) OnDatasetRead(d.source);
    }

    private static void OnDatasetRead(DataSource data) => SheetLabels.WarmGlyphs(data);

    public struct Room
    {
        public int cubes;
        public int labels;
        public int stripCubes;
        public int stripLabels;
    }

    public bool PrepareRoom(Room room, double budgetMs)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        double Left() => budgetMs - clock.Elapsed.TotalMilliseconds;

        while ((_sheet != null ? 1 : 0) + _spares.Count < SpareCount)
        {
            CreateSheet made = MakeSheet();
            made.SetShown(false);
            Park(made);
        }
        if (_sheet != null && (Left() <= 0 || !_sheet.Reserve(cubeMaterial, room.cubes, room.labels, Left())))
            return false;
        for (int i = 0; i < _spares.Count; i++)
            if (_spares[i] != null && (Left() <= 0 || !_spares[i].Reserve(cubeMaterial, room.cubes, room.labels, Left())))
                return false;

        while (_stripSpares.Count + _projections.Count < StripsReady)
        {
            if (Left() <= 0) return false;
            CreateSheet strip = CreateDetachedPiece("Projection_Strip");
            strip.SetShown(false);
            _stripSpares.Add(strip);
        }
        for (int i = 0; i < _stripSpares.Count; i++)
            if (_stripSpares[i] != null &&
                (Left() <= 0 || !_stripSpares[i].Reserve(cubeMaterial, room.stripCubes, room.stripLabels, Left())))
                return false;

        while (_leavingSpares.Count + _leaving.Count < room.cubes)
        {
            if (Left() <= 0) return false;
            _leavingSpares.Add(MakeLeavingBar());
        }

        if (_sheet != null) PrepareGrabs?.Invoke(_sheet);
        for (int i = 0; i < _spares.Count; i++) PrepareGrabs?.Invoke(_spares[i]);
        return true;
    }

    public static event Action<CreateSheet> PrepareGrabs;

    private void PrewarmSecondSheet(DataSource data)
    {
        if (_secondMade) return;
        _secondMade = true;
        if (_spares.Count > 0) return;

        CreateSheet s = MakeSheet();
        s.SetShown(false);
        BuildWhole(s, data);
        Park(s);
    }

    private void KeepShown()
    {
        CreateSheet sheet = Sheet;
        if (sheet == null || _spawnPending || sheet.IsGrabbed) return;
        sheet.SetShown(true);
    }

    private bool TrySpawn()
    {
        CreateSheet sheet = Sheet;
        if (sheet == null) return true;

        if (_sinking != null) return false;

        if (TryLooseAnchor(sheet.sheetId, out Vector3 loosePos, out Quaternion looseRot))
            transform.SetPositionAndRotation(loosePos, looseRot);
        else if (!AnchorAhead(sheet)) return false;

        if (!HasPoseEdit(sheet.sheetId))
            sheet.transform.SetLocalPositionAndRotation(SpawnOffset(sheet), Quaternion.identity);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        sheet.SetShown(true);
        double shownMs = clock.Elapsed.TotalMilliseconds;
        ReanchorGrab(sheet);
        SyncProjectionToStack(animateNew: _spawnAnimates, spread: true);
        HitchLog.Note($"sheet shown, {shownMs:F1} ms to show, {clock.Elapsed.TotalMilliseconds:F1} ms in all");
        return true;
    }

    private bool AnchorAhead(CreateSheet sheet)
    {
        if (_hasHome)
        {
            transform.SetPositionAndRotation(_homePos, _homeRot);
            return true;
        }

        if (!LoadingPhase.Done) return false;
        if (!CameraRig.TryGetBasis(out Vector3 camPos, out Quaternion yaw)) return false;

        float halfDepth = Mathf.Max(_rowCount - 1, 0) * _rowPitch * 0.5f;
        Vector3 centre = camPos + yaw * new Vector3(
            0f, 0f, Mathf.Max(minimumZOffsetFromCamera, 0f) + halfDepth);
        centre.y = camPos.y * 0.75f;

        transform.SetPositionAndRotation(centre - yaw * SpawnOffset(sheet), yaw);
        Recenter.NoteHome();
        _homePos = transform.position;
        _homeRot = transform.rotation;
        _hasHome = true;
        return true;
    }

    private Vector3 SpawnOffset(CreateSheet sheet)
    {
        float scale = Mathf.Abs(sheet.transform.localScale.x);
        float halfWidth = (sheet.SlotCoord(true, sheet.LastSlot(true)) + cubeSide * 0.5f + labelGap) * scale;
        return new Vector3(-halfWidth, 0f, 0f);
    }

    private static bool HasPoseEdit(int sheetId)
    {
        EditList edits = ManageDatasets.ActiveEdits;
        for (int i = 0; i < edits.Count; i++)
            if (IsPoseEdit(edits[i], sheetId)) return true;
        return false;
    }

    private static bool IsPoseEdit(Edit e, int sheetId) =>
        e != null && e.IsPose && e.move.sheetId == sheetId;

    public bool IsDetached(int sheetId) => TryLooseAnchor(sheetId, out _, out _);

    private static bool TryLooseAnchor(int sheetId, out Vector3 pos, out Quaternion rot)
    {
        EditList edits = ManageDatasets.ActiveEdits;
        for (int i = 0; i < edits.Count; i++)
        {
            Edit e = edits[i];
            if (e == null || e.kind != EditKind.Move || e.move.sheetId != sheetId) continue;
            pos = e.move.anchorPos;
            rot = e.move.anchorRot;
            return true;
        }
        pos = Vector3.zero;
        rot = Quaternion.identity;
        return false;
    }
}
