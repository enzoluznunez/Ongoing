using UnityEngine;
using UnityEngine.Serialization;

public abstract class Tool : MonoBehaviour
{
    public ManageTools toolManager;
    [FormerlySerializedAs("sheetManager")] public ManageCharts chartManager;

    private bool _active;

    protected bool Active => _active;

    protected abstract ToolType Kind { get; }

    protected void Report(string what) => StateChannel.Record(what);

    protected virtual void OnToolStart() { }

    protected virtual void OnToolDestroy() { }

    protected virtual void OnActiveChanged(bool active) { }

    protected virtual void ClearToolState() { }

    protected virtual void OnResetTool() { }

    protected virtual void Start()
    {
        if (toolManager == null) toolManager = Scene.Tools;
        if (chartManager == null) chartManager = Scene.Charts;

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
        OnActiveChanged(active);
    }
}
