using System;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.View;
using AccountingImmersiveAnalytics.UI.Charts.View;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.Tools
{
    public enum ToolType { None = 0, Filter = 1, Place = 2, Sort = 3, Scale = 5, Trendline = 6, Key = 7, Edits = 8 }

    public class ToolManager : MonoBehaviour
    {
        public event Action<ToolType> OnToolChanged;

        public ChartManager chartManager;

        public ToolType SelectedTool { get; private set; } = ToolType.None;

        public static readonly ToolType[] ChartTools =
        {
            ToolType.Place, ToolType.Scale, ToolType.Sort, ToolType.Filter,
            ToolType.Trendline, ToolType.Key, ToolType.Edits
        };

        public static bool IsGrabTool(ToolType tool) =>
            tool == ToolType.Place || tool == ToolType.Scale;

        public static bool Offered(ToolType tool)
        {
            ChartManager charts = ChartManager.Instance;
            ChartEntry entry = charts != null ? charts.Active : null;
            return entry == null || Offered(tool, entry.Spec, charts.PinnedCard);
        }

        public static bool Offered(ToolType tool, Spec.ChartSpec spec, ChartCard card)
        {
            switch (tool)
            {
                case ToolType.Sort: return Spec.ChartKinds.Sorts(spec.Kind);
                case ToolType.Trendline: return Spec.TrendFitter.Offered(spec);
                case ToolType.Key: return card == null || card.Key.Count > 0;
                default: return true;
            }
        }

        private void Start()
        {
            if (chartManager == null) chartManager = Scene.Charts;
        }

        public void SelectTool(ToolType tool) => Select(SelectedTool == tool ? ToolType.None : tool);

        public void DeselectTool() => Select(ToolType.None);

        private void Select(ToolType next)
        {
            if (ActivityFeed.UserDriven) AssistantTurn.UserTookControl();
            if (next == SelectedTool) return;

            SelectedTool = next;
            OnToolChanged?.Invoke(SelectedTool);
            ReportSelection();
        }

        private void ReportSelection() => ActivityFeed.RecordState("tool",
            SelectedTool == ToolType.None ? "no tool is selected" : $"the {SelectedTool} tool is selected");


        public bool Undo()
        {
            EditList edits = ChartManager.ActiveEdits;
            Edit top = edits.Peek();
            if (top == null) return false;

            string kindName = Edit.KindName(top.kind);
            bool applied = chartManager != null && chartManager.Undo(top);
            edits.Pop();
            if (!applied)
            {
                Debug.LogWarning($"[ToolManager] {kindName} undo no longer matches the scene; the record was dropped.");
                return false;
            }

            ActivityFeed.Record($"undid the {kindName} edit");
            return true;
        }
    }
}
