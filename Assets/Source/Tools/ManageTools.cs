using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum ToolType { None, Filter, Move, Sort, Rotate, Scale, Profile }

public class ManageTools : MonoBehaviour
{
    public event Action<ToolType> OnToolChanged;

    public event Action<ToolType> OnToolReset;

    [FormerlySerializedAs("sheetManager")] public ManageCharts chartManager;

    public ToolType SelectedTool { get; private set; } = ToolType.None;

    public static bool OnHold(ToolType tool) =>
        tool == ToolType.Sort || tool == ToolType.Filter || tool == ToolType.Profile;

    private void Start()
    {
        if (chartManager == null) chartManager = Scene.Charts;
    }

    public void SelectTool(ToolType tool)
    {
        if (OnHold(tool)) return;
        if (StateChannel.UserDriven) AgentTurn.UserTookControl();
        ToolType next = SelectedTool == tool ? ToolType.None : tool;
        if (next == SelectedTool) return;

        SelectedTool = next;
        OnToolChanged?.Invoke(SelectedTool);
        ReportSelection();
    }

    public void DeselectTool()
    {
        if (StateChannel.UserDriven) AgentTurn.UserTookControl();
        if (SelectedTool == ToolType.None) return;

        SelectedTool = ToolType.None;
        OnToolChanged?.Invoke(SelectedTool);
        ReportSelection();
    }

    private void ReportSelection() => StateChannel.RecordState("tool",
        SelectedTool == ToolType.None ? "no tool is selected" : $"the {SelectedTool} tool is selected");

    private ToolType _suspended = ToolType.None;

    public void SuspendTool()
    {
        _suspended = SelectedTool;
        DeselectTool();
    }

    public void ResumeTool()
    {
        if (_suspended == ToolType.None) return;
        ToolType resume = _suspended;
        _suspended = ToolType.None;
        SelectTool(resume);
    }

    public void ForgetSuspendedTool() => _suspended = ToolType.None;

    public void ResetTool(ToolType tool)
    {
        OnToolReset?.Invoke(tool);
        if (TryEditKindOf(tool, out EditKind kind)) ManageCharts.ActiveEdits.DropKind(kind);
    }

    private static bool TryEditKindOf(ToolType tool, out EditKind kind) =>
        Enum.TryParse(tool.ToString(), out kind);

    public bool Undo()
    {
        EditList edits = ManageCharts.ActiveEdits;
        Edit top = edits.Peek();
        if (top == null) return false;

        string kindName = Edit.KindName(top.kind);
        int inGroup = edits.TopGroupSize();
        bool applied = chartManager != null && chartManager.Undo(top);
        edits.Pop();

        if (!applied)
        {
            Debug.LogWarning($"[ManageTools] {kindName} undo no longer matches the scene; the record was dropped.");
            return false;
        }

        for (int i = 1; i < inGroup; i++)
        {
            Edit next = edits.Pop();
            if (next != null && chartManager != null) chartManager.Undo(next);
        }

        StateChannel.Record(inGroup > 1
            ? $"undid the {kindName} edit ({inGroup} steps, one action)"
            : $"undid the {kindName} edit");
        return true;
    }

    public void UndoAll()
    {
        EditList edits = ManageCharts.ActiveEdits;
        int had = edits.Count;

        while (edits.Peek() != null)
        {
            Edit top = edits.Pop();
            if (chartManager != null) chartManager.Undo(top);
        }

        foreach (ToolType tool in Enum.GetValues(typeof(ToolType)))
            if (tool != ToolType.None && !OnHold(tool)) OnToolReset?.Invoke(tool);

        if (had > 0) StateChannel.Record($"undid all {had} edits");
    }
}
