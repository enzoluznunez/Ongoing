using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using UnityEngine;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolTargets;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Chart
{
    public sealed class CallScale : AssistantTool<CallScale.Args>
    {

        private const float DefaultPercent = 50f;
        private const float MinPercent = 1f;
        private const float MaxPercent = 100f;
        private const float MostShrink = 99f;

        public class Args
        {
            [Doc("'enlarge' makes the chart bigger, 'shrink' makes it smaller. Leave it out to only switch Scale on."), Optional]
            [Values("enlarge", "shrink")]
            public string direction;
            [Doc("How much to resize, as a percentage of the current size. Shrink by 42 leaves it at 58 percent; enlarge by 42 leaves it at 142 percent. Enlarge by 100 doubles it. Shrink stops at 99, since 100 would erase the chart.")]
            [Limits(1, 100), DefaultsTo(50), Optional]
            public float? percent;
            [Doc("false switches the button off, as pressing it again does. Defaults to true."), DefaultsTo(true), Optional]
            public bool? on;
        }

        protected override bool EditsAreOutcome => true;

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "CallScale",
            Description = "Press the pinned chart's Scale button and resize the chart as the user does with it on, by " +
                          "grabbing it with both hands and moving them apart to enlarge or together to shrink. Its built size is scale 1. There is no size limit, for you or the " +
                          "user's hands; repeat the call to go further than one step allows.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            if (SwitchedOff(ToolType.Scale, args.on, result)) return;
            string direction = args.direction;
            bool enlarge = direction == "enlarge";

            float percent = Mathf.Clamp(args.percent ?? DefaultPercent, MinPercent, enlarge ? MaxPercent : MostShrink);
            float mult = enlarge ? 1f + percent / 100f : 1f - percent / 100f;

            System.Action<Transform> resize = t =>
            {
                float v = t.localScale.x * mult;
                t.localScale = new Vector3(v, v, v);
            };

            if (!TryPinnedChart(result, "resize", out var mgr, out var chart)) return;
            if (!EnsureToolSelected(ToolType.Scale, result)) return;
            result["on"] = true;
            if (direction == null) { result["ok"] = true; return; }

            var pre = ChangeChartPose(mgr, chart, EditKind.Scale, resize);

            float before = pre.Scale.x;
            mgr.GetCommittedPose(chart, out _, out _, out Vector3 nowScale);
            float after = nowScale.x;

            result["resized"] = direction;
            result["percent"] = System.Math.Round(percent, 1);
            result["scaleBefore"] = System.Math.Round(before, 3);
            result["scale"] = System.Math.Round(after, 3);
        }
    }
}
