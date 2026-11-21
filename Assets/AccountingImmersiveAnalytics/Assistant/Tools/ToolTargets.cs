using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Charts.View;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolResult;

namespace AccountingImmersiveAnalytics.Assistant.Tools
{
    public static class ToolTargets
    {
        public static bool TryWorldDirection(string direction, out Vector3 world)
        {
            world = Vector3.zero;
            if (direction == "up") { world = Vector3.up; return true; }
            if (direction == "down") { world = Vector3.down; return true; }

            Transform head = CameraRig.MainTransform;
            if (head == null) return false;

            Vector3 forward = CameraRig.FlatForward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            switch (direction)
            {
                case "forward": world = forward; return true;
                case "back": world = -forward; return true;
                case "left": world = -right; return true;
                case "right": world = right; return true;
                case "forward-left": world = (forward - right).normalized; return true;
                case "forward-right": world = (forward + right).normalized; return true;
                case "back-left": world = (-forward - right).normalized; return true;
                case "back-right": world = (-forward + right).normalized; return true;
            }
            return false;
        }

        public const string DocumentsPanelName = "Documents panel";

        public static bool TryCharts(Dictionary<string, object> result, out ChartManager charts)
        {
            charts = Scene.Charts;
            if (charts != null) return true;
            result["error"] = "The chart manager was not found in the scene.";
            return false;
        }

        public static List<string> ShownTitles(ChartManager charts)
        {
            var titles = new List<string>();
            foreach (ChartEntry shown in charts.Shown) titles.Add(shown.Title);
            return titles;
        }

        public static bool TryPinnedChart(Dictionary<string, object> result, string refusal, out ChartManager charts)
        {
            if (!TryCharts(result, out charts)) return false;
            if (charts.PinnedTransform != null) return true;
            Refuse(result, "pinned chart", refusal + " Pin one with CallPin first, then call again.");
            return false;
        }

        public static ChartPose ChangeChartPose(ChartManager charts, Transform chart, EditKind kind, Action<Transform> change)
        {
            charts.CompleteMotion(chart);
            var before = new ChartPose(chart);
            change(chart);
            charts.NotifyMoveCommitted(chart, before.Position, before.Rotation, before.Scale, kind);
            charts.AnimateFrom(chart, before.Position, before.Rotation, before.Scale);
            return before;
        }

        public static bool TryChart(string query, Dictionary<string, object> result, out ChartEntry entry)
        {
            entry = null;
            if (!TryCharts(result, out ChartManager charts)) return false;
            entry = charts.Find(query);
            if (entry != null) return true;
            List<string> titles = ShownTitles(charts);
            if (titles.Count == 0)
            {
                Refuse(result, "charts shown", "No charts are shown. Open a document with SetDocument first.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                NeedChoice(result, "chart", titles, "Several charts are shown and none is pinned. Name the chart by its title.");
                return false;
            }
            result["error"] = $"No single chart matches '{query}'.";
            result["charts"] = new List<object>(titles);
            return false;
        }

        public static bool TryShownChart(string query, Dictionary<string, object> result, out ChartEntry entry)
        {
            if (!TryChart(query, result, out entry)) return false;
            ChartManager charts = Scene.Charts;
            if (entry == charts.Pinned) return true;
            foreach (ChartEntry shown in charts.Shown)
                if (shown == entry) return true;
            Refuse(result, "chart shown",
                $"\"{entry.Title}\" belongs to \"{entry.Source}\", which is not open, so it is not shown. Open it with SetDocument first.");
            entry = null;
            return false;
        }

        public static bool EnsureToolFor(ToolType tool, ChartEntry entry, Dictionary<string, object> result)
        {
            if (entry == Scene.Charts?.Pinned) return EnsureToolSelected(tool, result);
            result["panel"] = $"\"{entry.Title}\" is not pinned, so no {tool} panel opens; the change shows on its card in the chart column.";
            return true;
        }

        public static bool EnsureToolSelected(ToolType tool, Dictionary<string, object> result)
        {
            ToolManager tools = Scene.Tools;
            if (tools == null) { result["error"] = "The tool manager was not found in the scene."; return false; }
            if (tools.SelectedTool != tool)
            {
                if (AssistantTurn.UserTookOver)
                {
                    Refuse(result, "tool changed by the user",
                        $"The user changed the tool while you were working, so the {tool} tool is no longer selected. " +
                        "Tell them what you had done and ask before carrying on.");
                    return false;
                }
                tools.SelectTool(tool);
            }

            if (tools.SelectedTool != tool)
            {
                result["error"] = $"The {tool} tool could not be selected.";
                return false;
            }
            result["on"] = true;
            return true;
        }

        public static bool SwitchedOff(ToolType tool, bool on, Dictionary<string, object> result)
        {
            if (on) return false;
            ToolManager tools = Scene.Tools;
            if (tools == null) { result["error"] = "The tool manager was not found in the scene."; return true; }
            if (tools.SelectedTool == tool) tools.DeselectTool();
            result["on"] = false;
            result["ok"] = true;
            return true;
        }

        public static bool TryDocumentsPanel(Dictionary<string, object> result, out DocumentsPanel panel)
        {
            panel = Scene.Documents;
            if (panel != null) return true;
            result["error"] = $"The {DocumentsPanelName} was not found in the scene.";
            return false;
        }
    }
}
