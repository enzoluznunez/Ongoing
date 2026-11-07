using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeminiLive;
using UnityEngine.Profiling;

public abstract class AgenticTool : Function {

    private readonly string sample;

    protected AgenticTool() {
        sample = "GeminiTool." + GetType().Name;
    }

    protected abstract void Run(Dictionary<string, object> args, Dictionary<string, object> result);

    private static readonly string[] RefusalKeys =
        { "error", "preconditionUnmet", "needsChoice" };

    protected static bool IsRefusal(Dictionary<string, object> result) {
        for (int i = 0; i < RefusalKeys.Length; i++)
            if (result.ContainsKey(RefusalKeys[i])) return true;
        return false;
    }

    protected virtual bool EditsAreOutcome => false;

    protected override Task<Dictionary<string, object>> Execute(Dictionary<string, object> args) =>
        Perform(args, result => Run(args ?? new Dictionary<string, object>(), result));

    protected async Task<Dictionary<string, object>> Perform(Dictionary<string, object> args,
        Action<Dictionary<string, object>> act) {
        var result = new Dictionary<string, object>();
        var total = System.Diagnostics.Stopwatch.StartNew();
        long runMs = 0;
        int editsBefore = 0, editsAfter = 0;

        await MainThread.Run(() => {
            var run = System.Diagnostics.Stopwatch.StartNew();
            Profiler.BeginSample(sample);
            StateChannel.InAgentCall = true;
            try {
                AgentTurn.NoteToolCall();
                editsBefore = ManageCharts.ActiveEdits != null ? ManageCharts.ActiveEdits.Count : 0;
                act(result);
            }
            catch (Exception e) {
                result.Clear();
                result["error"] = e.Message;
            }
            finally {
                editsAfter = ManageCharts.ActiveEdits != null ? ManageCharts.ActiveEdits.Count : 0;
                StateChannel.InAgentCall = false;
                string did = StateChannel.TakeAgentBatch();
                if (!string.IsNullOrEmpty(did)) result["did"] = did;
                Profiler.EndSample();
                run.Stop();
                runMs = run.ElapsedMilliseconds;
            }
        });
        total.Stop();

        bool changed = editsAfter != editsBefore;
        if (EditsAreOutcome) result["changed"] = changed;

        if (!IsRefusal(result) && !result.ContainsKey("ok")) {
            result["ok"] = !EditsAreOutcome || changed;
            if (EditsAreOutcome && !changed && !result.ContainsKey("note"))
                result["note"] = "Nothing changed; the chart is already the way this call would leave it.";
        }

        UnityEngine.Debug.Log($"[Gemini][tool] {Name} args={Brief(args)} -> {Brief(result)} " +
            $"({total.ElapsedMilliseconds} ms, run {runMs} ms, edits {editsBefore}->{editsAfter})");

        return result;
    }

    private static string Brief(Dictionary<string, object> map) {
        if (map == null || map.Count == 0) return "{}";
        try {
            string head = BriefJson(map, 300, out long totalBytes);
            return totalBytes > 300 ? head + "..." : head;
        }
        catch (Exception e) { return "<unserializable: " + e.Message + ">"; }
    }

    protected static Schema ParametersFor(System.Type args) => ToolArguments.Schema(args);

    protected static bool TryWorldDirection(string direction, out UnityEngine.Vector3 world) {
        world = UnityEngine.Vector3.zero;
        if (direction == "up") { world = UnityEngine.Vector3.up; return true; }
        if (direction == "down") { world = UnityEngine.Vector3.down; return true; }

        UnityEngine.Transform cam = CameraRig.MainTransform;
        if (cam == null) return false;

        UnityEngine.Vector3 forward = CameraRig.FlatForward;
        UnityEngine.Vector3 right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, forward).normalized;

        switch (direction) {
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

    protected static bool TryTurnSign(string direction, out float sign) {
        sign = direction == "right" ? 1f : direction == "left" ? -1f : 0f;
        return sign != 0f;
    }

    protected static bool TryGet(Dictionary<string, object> args, string key, out object value) {
        value = null;
        return args != null && args.TryGetValue(key, out value) && value != null;
    }

    protected static string AsString(object v) {
        return LiveWire.Text(v);
    }

    protected static object Round(double v) {
        if (double.IsNaN(v) || double.IsInfinity(v)) return null;
        if (v > (double)decimal.MaxValue || v < (double)decimal.MinValue) return null;
        return (decimal)Math.Round(v, 4);
    }


    protected readonly struct PiecePose {
        public readonly UnityEngine.Vector3 pos;
        public readonly UnityEngine.Quaternion rot;
        public readonly UnityEngine.Vector3 scale;
        public PiecePose(UnityEngine.Transform t) { pos = t.localPosition; rot = t.localRotation; scale = t.localScale; }
    }

    protected static bool TryResolveChart(Dictionary<string, object> result,
        string verb, out ManageCharts mgr, out ChartView chart) {
        mgr = Scene.Charts;
        chart = mgr != null ? mgr.View : null;
        if (chart == null || !chart.Shown) { result["error"] = $"The chart is not ready to {verb}; is a chart shown?"; return false; }
        return true;
    }

    protected static PiecePose ApplyChartTransform(ManageCharts mgr, ChartView chart, Action<UnityEngine.Transform> mutate) {
        mgr.CompleteMotion(chart);
        UnityEngine.Transform t = chart.transform;
        var pre = new PiecePose(t);
        mutate(t);
        mgr.NotifyMoveCommitted(chart, pre.pos, pre.rot, pre.scale);
        mgr.AnimateFrom(chart, pre.pos, pre.rot, pre.scale);
        return pre;
    }

    protected static void NeedChoice(Dictionary<string, object> result, string what,
        IReadOnlyList<string> options, string message) {
        result["needsChoice"] = what;
        if (options != null && options.Count > 0) result["options"] = new List<object>(options);
        result["message"] = message;
    }

    protected static void Refuse(Dictionary<string, object> result, string unmet, string message) {
        result["preconditionUnmet"] = unmet;
        result["message"] = message;
    }

    protected static bool EnsureToolPanelOpen(Dictionary<string, object> result) {
        var panel = Scene.ToolPanel;
        if (panel == null) { result["error"] = "The tool panel was not found in the scene."; return false; }
        if (!panel.IsVisible) {
            if (AgentTurn.UserTookOver) {
                Refuse(result, "tool panel closed by the user",
                    "The user closed the tool panel while you were working, so it is not yours to reopen. " +
                    "Tell them what you had done and ask before carrying on.");
                return false;
            }
            panel.ShowPanel();
        }
        return true;
    }

    protected static bool EnsureToolSelected(ToolType tool, Dictionary<string, object> result) {
        if (!EnsureToolPanelOpen(result)) return false;

        var tools = Scene.Tools;
        if (tools == null) { result["error"] = "The tool manager was not found in the scene."; return false; }
        if (tools.SelectedTool != tool) {
            if (AgentTurn.UserTookOver) {
                Refuse(result, "tool changed by the user",
                    $"The user changed the tool while you were working, so the {tool} tool is no longer selected. " +
                    "Tell them what you had done and ask before carrying on.");
                return false;
            }
            tools.SelectTool(tool);
        }

        if (tools.SelectedTool != tool) {
            result["error"] = $"The {tool} tool could not be selected.";
            return false;
        }
        return true;
    }

    protected static bool EnsureToolSelected<T>(ToolType kind, T tool, Dictionary<string, object> result) where T : Tool {
        if (!EnsureToolSelected(kind, result)) return false;
        if (tool == null) { result["error"] = $"{kind} tool not found in scene."; return false; }
        return true;
    }
}

public abstract class AgenticTool<TArgs> : AgenticTool where TArgs : class, new() {

    protected abstract void Run(TArgs args, Dictionary<string, object> result);

    protected virtual Task<string> Prepare(TArgs args) => Task.FromResult<string>(null);

    protected sealed override async Task<Dictionary<string, object>> Execute(Dictionary<string, object> args) {
        var bound = ToolArguments.Bind(typeof(TArgs), args, out string error) as TArgs;
        if (bound != null) error = await Prepare(bound).ConfigureAwait(false);
        return await Perform(args, result => {
            if (error != null) result["error"] = error;
            else Run(bound, result);
        }).ConfigureAwait(false);
    }

    protected sealed override void Run(Dictionary<string, object> args, Dictionary<string, object> result) =>
        throw new NotSupportedException();
}
