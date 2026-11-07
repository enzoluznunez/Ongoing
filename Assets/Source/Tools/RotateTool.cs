using UnityEngine;
using Oculus.Interaction;

public class RotateTool : GrabTool
{
    private static ITransformer YawTransformerFor(ChartView chart) => PanelPlacement.FreeSpin(chart.gameObject);

    protected override void PrepareChart(ChartView chart) => YawTransformerFor(chart);

    protected override ToolType Kind => ToolType.Rotate;

    protected override EditKind RecordKind => EditKind.Rotate;

    protected override string Describe(float distance) => "rotated the chart";

    protected override void Arm()
    {
        chartManager.SetGrabbable(true);
        chartManager.SetTwoGrab(YawTransformerFor);
    }
}
