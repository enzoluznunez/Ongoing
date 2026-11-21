using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace AccountingImmersiveAnalytics.Assistant.Tools
{
    public abstract class AssistantTool
    {
        private const int LoggedBytes = 300;

        private readonly string _profilerSample;

        protected AssistantTool()
        {
            _profilerSample = "AssistantTool." + GetType().Name;
        }

        public abstract FunctionDeclaration Declaration { get; }

        public string Name { get; internal set; }

        protected virtual bool EditsAreOutcome => false;

        protected abstract void Run(Dictionary<string, object> args, Dictionary<string, object> result);

        internal virtual Task<Dictionary<string, object>> Execute(Dictionary<string, object> args) =>
            Perform(args, result => Run(args ?? new Dictionary<string, object>(), result));

        protected static Schema ParametersFor(Type args) => ToolArguments.Schema(args);

        protected static object Round(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return null;
            if (value > (double)decimal.MaxValue || value < (double)decimal.MinValue) return null;
            return (decimal)Math.Round(value, 4);
        }

        protected async Task<Dictionary<string, object>> Perform(Dictionary<string, object> args,
            Action<Dictionary<string, object>> act)
        {
            var result = new Dictionary<string, object>();
            var total = Stopwatch.StartNew();
            long runMilliseconds = 0;
            int editsBefore = 0, editsAfter = 0;

            await MainThread.Run(() =>
            {
                var run = Stopwatch.StartNew();
                Profiler.BeginSample(_profilerSample);
                ActivityFeed.InAssistantCall = true;
                try
                {
                    AssistantTurn.NoteToolCall();
                    editsBefore = ChartManager.ActiveEdits.Count;
                    act(result);
                }
                catch (Exception e)
                {
                    result.Clear();
                    result["error"] = e.Message;
                }
                finally
                {
                    editsAfter = ChartManager.ActiveEdits.Count;
                    ActivityFeed.InAssistantCall = false;
                    string did = ActivityFeed.TakeAssistantBatch();
                    if (!string.IsNullOrEmpty(did)) result["did"] = did;
                    Profiler.EndSample();
                    run.Stop();
                    runMilliseconds = run.ElapsedMilliseconds;
                }
            });
            total.Stop();

            bool changed = editsAfter != editsBefore;
            if (EditsAreOutcome)
            {
                if (result.TryGetValue("changed", out object stated) && stated is bool told) changed = told;
                result["changed"] = changed;
            }

            if (!ToolResult.IsRefusal(result) && !result.ContainsKey("ok"))
            {
                result["ok"] = !EditsAreOutcome || changed;
                if (EditsAreOutcome && !changed && !result.ContainsKey("note"))
                    result["note"] = "Nothing changed; the chart is already the way this call would leave it.";
            }

            Debug.Log($"[AssistantTool] {Name} args={Brief(args)} -> {Brief(result)} " +
                      $"({total.ElapsedMilliseconds} ms, run {runMilliseconds} ms, edits {editsBefore}->{editsAfter})");

            return result;
        }

        private static string Brief(Dictionary<string, object> map)
        {
            if (map == null || map.Count == 0) return "{}";
            try
            {
                string head = ToolRegistry.BriefJson(map, LoggedBytes, out long totalBytes);
                return totalBytes > LoggedBytes ? head + "..." : head;
            }
            catch (Exception e) { return "<unserializable: " + e.Message + ">"; }
        }
    }

    public abstract class AssistantTool<TArgs> : AssistantTool where TArgs : class, new()
    {
        protected abstract void Run(TArgs args, Dictionary<string, object> result);

        protected virtual Task<string> Prepare(TArgs args) => Task.FromResult<string>(null);

        internal sealed override async Task<Dictionary<string, object>> Execute(Dictionary<string, object> args)
        {
            var bound = ToolArguments.Bind(typeof(TArgs), args, out string error) as TArgs;
            if (bound != null) error = await Prepare(bound).ConfigureAwait(false);
            return await Perform(args, result =>
            {
                if (error != null) result["error"] = error;
                else Run(bound, result);
            }).ConfigureAwait(false);
        }

        protected sealed override void Run(Dictionary<string, object> args, Dictionary<string, object> result) =>
            throw new NotSupportedException();
    }
}
