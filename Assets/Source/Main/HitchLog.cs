using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class HitchLog : MonoBehaviour
{
    private const float HitchShare = 1.5f;
    private const float NoteWindow = 0.5f;
    private const float SummarySeconds = 10f;
    private const int TimingDepth = 4;

    private static HitchLog _instance;
    private static readonly List<(float at, string what)> Notes = new List<(float, string)>();

    private readonly FrameTiming[] _timings = new FrameTiming[TimingDepth];
    private readonly StringBuilder _line = new StringBuilder(256);

    private int _gcCount;
    private float _frameMs = 1000f / 72f;

    private ToolType _tool = ToolType.None;
    private string _chart;
    private bool _panel, _agent;

    private float _summaryStart;
    private int _frames, _hitches;
    private float _totalMs, _worstMs;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        Notes.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (_instance != null) return;

        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Spawn.Once(ref _instance);
    }

    public static void Note(string what)
    {
        if (_instance == null) return;
        Notes.Add((Time.unscaledTime, what));
        if (Notes.Count > 32) Notes.RemoveAt(0);
    }

    private void Start()
    {
        _gcCount = System.GC.CollectionCount(0);
        _summaryStart = Time.unscaledTime;
        Debug.Log($"[Hitch] Logging frames past {HitchShare:0.#}x the display's frame time.");
    }

    private void Update()
    {
        FrameTimingManager.CaptureFrameTimings();
        WatchState();

        float refresh = DisplayHz();
        if (refresh > 1f) _frameMs = 1000f / refresh;

        float ms = Time.unscaledDeltaTime * 1000f;
        int gc = System.GC.CollectionCount(0);
        bool collected = gc != _gcCount;
        _gcCount = gc;

        if (Time.frameCount > 2 && ms < 2000f)
        {
            _frames++;
            _totalMs += ms;
            if (ms > _worstMs) _worstMs = ms;
            if (ms > _frameMs * HitchShare)
            {
                _hitches++;
                LogHitch(ms, collected);
            }
        }

        if (Time.unscaledTime - _summaryStart >= SummarySeconds) LogSummary(refresh);
    }

    private static float DisplayHz()
    {
        try { return OVRPlugin.systemDisplayFrequency; }
        catch { return 0f; }
    }

    private void WatchState()
    {
        ManageTools tools = Scene.Tools;
        ToolType tool = tools != null ? tools.SelectedTool : ToolType.None;
        if (tool != _tool) { _tool = tool; Note($"tool -> {tool}"); }

        string chart = Scene.ChartLabel;
        if (chart != _chart)
        {
            bool first = _chart == null;
            _chart = chart;
            if (!first) Note($"chart -> {chart}");
        }

        ToolPanelUI panel = Scene.ToolPanel;
        bool panelOpen = panel != null && panel.IsVisible;
        if (panelOpen != _panel) { _panel = panelOpen; Note(panelOpen ? "tool panel opened" : "tool panel closed"); }


        bool agent = StateChannel.InAgentCall;
        if (agent != _agent) { _agent = agent; Note(agent ? "assistant call started" : "assistant call ended"); }
    }

    private void LogHitch(float ms, bool collected)
    {
        _line.Clear();
        _line.Append("[Hitch] ").Append(ms.ToString("F1")).Append(" ms (")
             .Append((ms / _frameMs).ToString("F1")).Append(" frames)");

        AppendTimings();
        if (collected) _line.Append(" | GC ran");

        _line.Append(" | tool ").Append(_tool);
        ManageCharts charts = Scene.Charts;
        if (charts != null && charts.View != null && charts.View.IsGrabbed) _line.Append(", chart held");
        if (_agent) _line.Append(", assistant working");

        float now = Time.unscaledTime;
        bool any = false;
        for (int i = 0; i < Notes.Count; i++)
        {
            if (now - Notes[i].at > NoteWindow) continue;
            _line.Append(any ? "; " : " | just before: ").Append(Notes[i].what);
            any = true;
        }

        Debug.Log(_line.ToString());
    }

    private void AppendTimings()
    {
        uint got = FrameTimingManager.GetLatestTimings(TimingDepth, _timings);
        if (got == 0) return;

        double main = 0, render = 0, gpu = 0;
        for (int i = 0; i < got; i++)
        {
            if (_timings[i].cpuMainThreadFrameTime > main) main = _timings[i].cpuMainThreadFrameTime;
            if (_timings[i].cpuRenderThreadFrameTime > render) render = _timings[i].cpuRenderThreadFrameTime;
            if (_timings[i].gpuFrameTime > gpu) gpu = _timings[i].gpuFrameTime;
        }
        _line.Append(" | main ").Append(main.ToString("F1"))
             .Append(", render ").Append(render.ToString("F1"))
             .Append(", gpu ").Append(gpu.ToString("F1"));
    }

    private void LogSummary(float refresh)
    {
        if (_frames > 0)
        {
            float avg = _totalMs / _frames;
            Debug.Log($"[Hitch] Last {SummarySeconds:0} s: {_frames} frames, avg {avg:F1} ms ({1000f / avg:F0} fps " +
                  $"of {refresh:F0} Hz), worst {_worstMs:F1} ms, {_hitches} long.");
        }
        _summaryStart = Time.unscaledTime;
        _frames = _hitches = 0;
        _totalMs = _worstMs = 0f;
    }
}
