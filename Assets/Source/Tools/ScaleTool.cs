using UnityEngine;
using Oculus.Interaction;

public class ScaleTool : GrabTool
{
    public const float MinScale = ChartView.DefaultScale * 0.75f;
    public const float MaxScale = ChartView.DefaultScale * 1.25f;

    private static ITransformer ScaleTransformerFor(ChartView chart)
    {
        GrabFreeTransformer scale = chart.GetComponent<GrabFreeTransformer>();
        if (scale == null)
        {
            scale = chart.gameObject.AddComponent<GrabFreeTransformer>();
            scale.InjectOptionalPositionConstraints(PinnedPosition());
            scale.InjectOptionalScaleConstraints(ScaleLimits());
        }

        scale.InjectOptionalRotationConstraints(PinnedRotation(chart.transform.localEulerAngles));
        return scale;
    }

    protected override void PrepareChart(ChartView chart) => ScaleTransformerFor(chart);

    private static TransformerUtils.ConstrainedAxis Pin(float value) => Range(value, value);

    private static TransformerUtils.ConstrainedAxis Range(float min, float max) =>
        new TransformerUtils.ConstrainedAxis
        {
            ConstrainAxis = true,
            AxisRange = new TransformerUtils.FloatRange { Min = min, Max = max }
        };

    private static TransformerUtils.PositionConstraints PinnedPosition() =>
        new TransformerUtils.PositionConstraints
        {
            ConstraintsAreRelative = true,
            XAxis = Pin(0f),
            YAxis = Pin(0f),
            ZAxis = Pin(0f)
        };

    private static TransformerUtils.RotationConstraints PinnedRotation(Vector3 euler) =>
        new TransformerUtils.RotationConstraints
        {
            XAxis = Pin(euler.x),
            YAxis = Pin(euler.y),
            ZAxis = Pin(euler.z)
        };

    private static TransformerUtils.ScaleConstraints ScaleLimits() =>
        new TransformerUtils.ScaleConstraints
        {
            ConstraintsAreRelative = false,
            XAxis = Range(MinScale, MaxScale),
            YAxis = Range(MinScale, MaxScale),
            ZAxis = Range(MinScale, MaxScale)
        };

    protected override ToolType Kind => ToolType.Scale;

    protected override EditKind RecordKind => EditKind.Scale;

    protected override string Describe(float distance) => "resized the chart";

    protected override void Arm()
    {
        chartManager.SetGrabbable(true);
        chartManager.SetTwoGrab(ScaleTransformerFor);
    }
}
