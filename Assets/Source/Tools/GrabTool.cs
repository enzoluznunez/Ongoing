using UnityEngine;

public abstract class GrabTool : Tool
{
    private bool _armPending;

    protected abstract EditKind RecordKind { get; }

    protected abstract string Describe(float distance);

    protected abstract void Arm();

    protected virtual void PrepareChart(ChartView view) { }

    protected override void OnToolStart()
    {
        ManageCharts.PrepareGrabs += PrepareChart;
        if (chartManager == null) return;
        chartManager.OnChartChanged += ApplyGrabEnabled;
        chartManager.OnChartMoveCommitted += OnChartMoveCommitted;
    }

    protected override void OnToolDestroy()
    {
        ManageCharts.PrepareGrabs -= PrepareChart;
        if (chartManager == null) return;
        chartManager.OnChartChanged -= ApplyGrabEnabled;
        chartManager.OnChartMoveCommitted -= OnChartMoveCommitted;
    }

    protected virtual bool ReattachOnReset => false;

    protected override void OnResetTool()
    {
        if (chartManager != null) chartManager.ResetGrabs(ReattachOnReset);
    }

    protected override void OnActiveChanged(bool active)
    {
        if (chartManager == null) return;
        if (active) { _armPending = true; return; }
        _armPending = false;
        chartManager.SetGrabbable(false);
    }

    private void OnChartMoveCommitted(ChartView view, Vector3 prePos, Quaternion preRot, Vector3 preScale)
    {
        if (!Active || view == null || toolManager == null || chartManager == null) return;

        Transform t = view.transform;
        if (MoveRecord.SamePose(t.localPosition, t.localRotation, t.localScale, prePos, preRot, preScale)) return;

        float distance = chartManager.transform.TransformVector(t.localPosition - prePos).magnitude;

        Report(Describe(distance));

        ManageCharts.ActiveEdits.PushMove(new MoveRecord
        {
            chartId = view.chartId,
            prePos = prePos,
            preRot = preRot,
            preScale = preScale,
            postPos = t.localPosition,
            postRot = t.localRotation,
            postScale = t.localScale,
            distance = distance,
            anchorPos = chartManager.transform.position,
            anchorRot = chartManager.transform.rotation
        }, RecordKind);
    }

    private void ApplyGrabEnabled()
    {
        if (Active) _armPending = true;
    }

    private void LateUpdate()
    {
        if (!_armPending) return;
        _armPending = false;
        if (!Active || chartManager == null) return;
        Arm();
    }
}
