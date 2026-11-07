using System.Collections.Generic;
using GeminiLive;
using UnityEngine;

public sealed class CallScaleTool : AgenticTool<CallScaleTool.Args> {

    private const float DefaultPercent = 50f;
    private const float MinPercent = 1f;
    private const float MaxPercent = 100f;

    public class Args {
        [Doc("'enlarge' makes the chart bigger, 'shrink' makes it smaller.")]
        [Values("enlarge", "shrink")]
        public string direction;
        [Doc("How much to resize, as a percentage of the current size. Shrink by 42 leaves it at 58 percent; enlarge by 42 leaves it at 142 percent. Enlarge by 100 doubles it.")]
        [Limits(1, 100), DefaultsTo(50), Optional]
        public float? percent;
    }

    protected override bool EditsAreOutcome => true;

    public override FunctionDeclaration Declaration => new FunctionDeclaration {
        Name = "CallScaleTool",
        Description = "Resize the chart, as if the user grabbed it with both hands and moved them apart to enlarge " +
                      "or together to shrink. Its built size is scale 1. It never goes above 1.25 times that, and never " +
                      "shrinks below 0.75 times it, the same limits the user's hands have, so 'scale' in the result is " +
                      "the size it actually reached.",
        Parameters = ParametersFor(typeof(Args))
    };

    protected override void Run(Args args, Dictionary<string, object> result) {
        if (!EnsureToolSelected(ToolType.Scale, result)) return;

        string direction = args.direction;
        bool enlarge = direction == "enlarge";

        float percent = Mathf.Clamp(args.percent ?? DefaultPercent, MinPercent, MaxPercent);
        float mult = enlarge ? 1f + percent / 100f : 1f - percent / 100f;

        System.Action<Transform> resize = t => {
            float v = Mathf.Clamp(t.localScale.x * mult, ScaleTool.MinScale, ScaleTool.MaxScale);
            t.localScale = new Vector3(v, v, v);
        };

        if (!TryResolveChart(result, "resize", out var mgr, out var chart)) return;

        var pre = ApplyChartTransform(mgr, chart, resize);

        float before = pre.scale.x;
        mgr.GetCommittedPose(chart, out _, out _, out Vector3 nowScale);
        float after = nowScale.x;

        result["resized"] = direction;
        result["percent"] = System.Math.Round(percent, 1);
        result["scaleBefore"] = System.Math.Round(before, 3);
        result["scale"] = System.Math.Round(after, 3);
        if (Mathf.Abs(after - before * mult) > 1e-3f)
            result["note"] = "The chart reached its size limit and changed less than asked.";
    }
}
