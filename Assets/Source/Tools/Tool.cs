using UnityEngine;

public abstract class Tool : MonoBehaviour
{
    public ManageTools toolManager;
    public ManageSheets sheetManager;
    public ReadSheets readSheets;

    private bool _active;

    protected bool Active => _active;

    protected abstract ToolType Kind { get; }

    protected void Report(string what) => StateChannel.Record(what);

    protected virtual bool UsesSheetEvents => false;

    protected virtual void OnSheetHover(ReadSheets.Reading reading) { }

    protected virtual void OnSheetSelect(ReadSheets.Reading reading) { }

    protected virtual void OnSheetRelease(ReadSheets.Reading reading) { }

    protected virtual void OnSheetCommit(ReadSheets.Reading reading) { }

    protected virtual void OnSheetCleared() { }

    protected void ClearTint()
    {
        if (sheetManager != null) sheetManager.ClearHoverTint();
    }

    protected void TintLine(ReadSheets.Reading reading, bool columns, float swell = Style.PreviewSwell) =>
        TintLine(reading.sheet, columns, columns ? reading.visCol : reading.visRow, swell);

    protected void TintLine(CreateSheet sheet, bool columns, int line, float swell = Style.PreviewSwell)
    {
        int lo = line, hi = line;
        if (columns) sheetManager.ColumnStripSpan(line, out lo, out hi);
        sheetManager.SetLineTint(sheet, columns, lo, hi, swell);
    }

    private CreateSheet _rayTintSheet;
    private bool _rayTintColumns;
    private int _rayTintLine = -1;

    public void TintFromRay(CreateSheet sheet, bool columns, int line)
    {
        _rayTintSheet = sheet;
        _rayTintColumns = columns;
        _rayTintLine = line;
    }

    protected bool ApplyRayTint()
    {
        if (_rayTintLine < 0 || _rayTintSheet == null) return false;
        TintLine(_rayTintSheet, _rayTintColumns, _rayTintLine);
        _rayTintLine = -1;
        return true;
    }

    private bool _listening;

    private void ListenSheets(bool on)
    {
        if (readSheets == null || on == _listening) return;
        _listening = on;
        if (on)
        {
            readSheets.OnHover += OnSheetHover;
            readSheets.OnSelect += OnSheetSelect;
            readSheets.OnRelease += OnSheetRelease;
            readSheets.OnCommit += OnSheetCommit;
            readSheets.OnCleared += OnSheetCleared;
        }
        else
        {
            readSheets.OnHover -= OnSheetHover;
            readSheets.OnSelect -= OnSheetSelect;
            readSheets.OnRelease -= OnSheetRelease;
            readSheets.OnCommit -= OnSheetCommit;
            readSheets.OnCleared -= OnSheetCleared;
        }
    }

    protected virtual void OnToolStart() { }

    protected virtual void OnToolDestroy() { }

    protected virtual void OnActiveChanged(bool active) { }

    protected virtual void ClearToolState() { }

    protected virtual void OnResetTool() { }

    protected virtual void Start()
    {
        if (toolManager == null) toolManager = Scene.Tools;
        if (sheetManager == null) sheetManager = Scene.Sheets;
        if (readSheets == null && sheetManager != null) readSheets = sheetManager.GetComponent<ReadSheets>();
        if (readSheets == null) readSheets = FindAnyObjectByType<ReadSheets>();

        OnToolStart();

        if (toolManager != null)
        {
            toolManager.OnToolChanged += HandleToolChanged;
            toolManager.OnToolReset += HandleToolReset;
        }

        SetActive(toolManager != null && toolManager.SelectedTool == Kind);
    }

    protected virtual void OnDestroy()
    {
        if (toolManager != null)
        {
            toolManager.OnToolChanged -= HandleToolChanged;
            toolManager.OnToolReset -= HandleToolReset;
        }
        ListenSheets(false);
        OnToolDestroy();
    }

    private void HandleToolChanged(ToolType selected) => SetActive(selected == Kind);

    private void HandleToolReset(ToolType tool)
    {
        if (tool != Kind) return;
        ClearToolState();
        OnResetTool();
    }

    private void SetActive(bool active)
    {
        if (_active == active) return;
        _active = active;
        if (!active) ClearToolState();
        if (UsesSheetEvents) ListenSheets(active);
        OnActiveChanged(active);
    }
}
