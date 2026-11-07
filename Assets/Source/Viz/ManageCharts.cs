using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Serialization;

public class ManageCharts : MonoBehaviour
{
    public const int FirstChartId = 1;

    public sealed class Entry
    {
        public ChartSpec Spec;
        public DataTable Table;
        public Blueprint Blueprint;
        public ChartGeometry Geometry;
        public Task<string> Loading;
        public string Failure;
        public readonly EditList Edits = new EditList();

        public bool Loaded => Geometry != null;
    }

    public static ManageCharts Instance { get; private set; }

    private static readonly EditList Unowned = new EditList();

    public static EditList ActiveEdits =>
        Instance != null && Instance.Active != null ? Instance.Active.Edits : Unowned;

    [FormerlySerializedAs("sheetPrefab")] public GameObject chartPrefab;
    [FormerlySerializedAs("cubeMaterial")] public Material markMaterial;

    [Header("System motion")]
    [Tooltip("Shortest system motion, seconds.")]
    public float minMotionSeconds = 0.1f;
    [Tooltip("Longest system motion, seconds; a large motion moves faster than the shared speed instead of running longer.")]
    public float maxMotionSeconds = 1.5f;

    public event Action OnChartChanged;
    public event Action<ChartView, Vector3, Quaternion, Vector3> OnChartMoveCommitted;
    public static event Action<ChartView> PrepareGrabs;

    private readonly List<Entry> _entries = new List<Entry>();
    private ChartView _view;
    private ChartView _spare;
    private ChartView _sinking;
    private Transform _root;
    private Entry _active;
    private Entry _next;
    private MarkMeshBuild _building;
    private bool _waitingToShow;
    private bool _chartsGrabbable;
    private Func<ChartView, ITransformer> _twoGrabFor;
    private ChartLook _look;
    private int _reloadTicket;

    private Vector3 _homePos;
    private Quaternion _homeRot;
    private bool _hasHome;

    private float _agentMotionSpeed = -1f;
    private bool _agentMotionInstant;

    public IReadOnlyList<Entry> Entries => _entries;
    public Entry Active => _active;
    public ChartView View => _view;
    public bool CatalogReady { get; private set; }
    public bool FirstChartReady { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        _look = ChartLook.Current;
        ChartDisplay.SyncWorkerFlags();
        ApplyAgentMotion();
        if (GetComponent<HandRays>() == null) gameObject.AddComponent<HandRays>();
    }

    private void OnEnable() => Settings.Changed += OnSettingChanged;

    private void OnDisable() => Settings.Changed -= OnSettingChanged;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_building != null) { _building.Finish(); _building = null; }
        foreach (Entry entry in _entries) entry.Geometry?.Dispose();
    }

    private IEnumerator Start()
    {
        Task<(List<ChartSpec> charts, string start)> catalog = BlueprintStore.FetchCatalog();
        while (!catalog.IsCompleted) yield return null;
        CatalogReady = true;

        if (catalog.Status != TaskStatus.RanToCompletion)
        {
            Debug.LogError($"[ManageCharts] {catalog.Exception?.GetBaseException().Message}");
            Notices.Show(this, Strings.Get("notice.noCharts"), Strings.Format("notice.noCharts.body", "Charts/catalog.json"));
            FirstChartReady = true;
            yield break;
        }

        foreach (ChartSpec spec in catalog.Result.charts) _entries.Add(new Entry { Spec = spec });
        if (_entries.Count == 0) { FirstChartReady = true; yield break; }

        Open(Find(Settings.StartupChart) ?? Find(catalog.Result.start) ?? _entries[0]);
    }

    public Entry Find(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        string wanted = query.Trim();

        Entry only = null;
        int hits = 0;
        foreach (Entry entry in _entries)
        {
            if (string.Equals(entry.Spec.Id, wanted, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.Spec.Title, wanted, StringComparison.OrdinalIgnoreCase)) return entry;
            if (entry.Spec.Title.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0) continue;
            only = entry;
            hits++;
        }
        return hits == 1 ? only : null;
    }

    public Task<string> Load(Entry entry)
    {
        if (entry.Loaded) return Task.FromResult<string>(null);
        return entry.Loading ??= LoadAsync(entry);
    }

    private static async Task<string> LoadAsync(Entry entry)
    {
        try
        {
            Blueprint blueprint = await BlueprintStore.Fetch(entry.Spec.BlueprintKind);
            string text = await StreamingFiles.ReadText(entry.Spec.Dataset);
            if (text == null) throw new InvalidOperationException($"'{entry.Spec.Dataset}' could not be read.");

            (DataTable table, ChartGeometry geometry) = await Task.Run(() =>
            {
                DataTable read = DataTable.ParseCsv(text);
                string problem = entry.Spec.Validate(blueprint, read);
                if (problem != null) throw new InvalidOperationException(problem);

                IChartModule module = ChartModules.For(entry.Spec.BlueprintKind);
                if (module == null)
                    throw new InvalidOperationException($"Charts of kind '{entry.Spec.BlueprintKind}' are not built yet.");
                return (read, module.Build(entry.Spec, blueprint, read));
            });

            entry.Blueprint = blueprint;
            entry.Table = table;
            entry.Geometry = geometry;
            entry.Failure = null;
        }
        catch (Exception e)
        {
            entry.Failure = e.GetBaseException().Message;
            Debug.LogError($"[ManageCharts] {entry.Spec.Title}: {entry.Failure}");
        }
        entry.Loading = null;
        return entry.Failure;
    }

    public async void Open(Entry entry)
    {
        await Load(entry);
        if (this == null) return;

        if (!entry.Loaded)
        {
            Notices.Show(this, Strings.Get("notice.chartUnavailable"), entry.Failure ?? entry.Spec.Title);
            FirstChartReady = true;
            return;
        }
        Show(entry);
    }

    public void Show(Entry entry)
    {
        if (entry == _active || entry == null || !entry.Loaded) return;

        CommitPendingGrabs();
        Scene.Tools?.DeselectTool();
        Scene.Tools?.ForgetSuspendedTool();
        RetireCurrent();

        _active = entry;
        _next = entry;
        _view = TakeView();
        ResetPose(_view);
        _view.Bind(entry.Spec, entry.Geometry);
        _building = new MarkMeshBuild(_view.MarkMesh, entry.Geometry, ColorModes.Palette32());
        _waitingToShow = true;

        StateChannel.RecordState("chart",
            $"the {entry.Spec.Title} chart is open; its edits and undo history belong to it");
        OnChartChanged?.Invoke();
    }

    private void Update()
    {
        if (_waitingToShow && TryShowNext()) _waitingToShow = false;

        ChartView view = _view;
        if (view == null || !view.Shown) return;

        bool held = view.IsGrabbed && !ScaleArmed;
        if (!held) view.SetGrabLook(false);
        if (view.PollGrabRelease(out Vector3 pos, out Quaternion rot, out Vector3 scale))
            NotifyMoveCommitted(view, pos, rot, scale);
        if (held) view.SetGrabLook(true);
    }

    private static bool ScaleArmed => Scene.Tools != null && Scene.Tools.SelectedTool == ToolType.Scale;

    private bool TryShowNext()
    {
        if (_next == null || _view == null) return true;
        if (_building != null)
        {
            if (!_building.Done) return false;
            _building.Finish();
            _building = null;
        }
        if (_sinking != null || !LoadingPhase.Done) { FirstChartReady = true; return false; }

        FirstChartReady = true;
        if (TryLooseAnchor(_view.chartId, out Vector3 loosePos, out Quaternion looseRot))
            transform.SetPositionAndRotation(loosePos, looseRot);
        else if (!AnchorAhead(_next.Geometry)) return false;

        ApplyStoredPose(_view, _next.Edits);
        _next = null;

        _view.SetShown(true);
        _view.SetGrabbable(_chartsGrabbable);
        ReanchorGrab(_view);
        _view.PlayGrow(MotionDuration(HeightMetres(_view)));
        HitchLog.Note($"chart shown, {_view.Geometry.Marks.Length} marks");
        OnChartChanged?.Invoke();
        return true;
    }

    private bool AnchorAhead(ChartGeometry geometry)
    {
        if (_hasHome)
        {
            transform.SetPositionAndRotation(_homePos, _homeRot);
            return true;
        }

        if (!CameraRig.TryGetBasis(out Vector3 camPos, out Quaternion yaw)) return false;

        float halfDepth = geometry.Bounds.size.z * 0.5f;
        Vector3 centre = camPos + yaw * new Vector3(0f, 0f, ChartDisplay.ChartAhead + halfDepth);
        centre.y = camPos.y * ChartDisplay.HeightShare;

        transform.SetPositionAndRotation(centre, yaw);
        Recenter.NoteHome();
        _homePos = transform.position;
        _homeRot = transform.rotation;
        _hasHome = true;
        return true;
    }

    private void ApplyStoredPose(ChartView view, EditList edits)
    {
        for (int i = edits.Count - 1; i >= 0; i--)
        {
            if (!edits[i].IsPose) continue;
            MoveRecord m = edits[i].move;
            view.transform.SetLocalPositionAndRotation(m.postPos, m.postRot);
            view.transform.localScale = m.postScale;
            break;
        }
        view.ForgetGrabLook();
    }

    private void RetireCurrent()
    {
        if (_building != null) { _building.Finish(); _building = null; }
        ChartView old = _view;
        _view = null;
        _next = null;
        if (old == null) return;

        CancelMotion(old);
        if (!old.Shown) { Park(old); return; }

        if (_sinking != null) _sinking.CompleteMotion();
        _sinking = old;
        old.PlayFall(MotionDuration(HeightMetres(old)), () =>
        {
            old.SetShown(false);
            Park(old);
            if (_sinking == old) _sinking = null;
        });
    }

    private void Park(ChartView view)
    {
        if (_spare != null && _spare != view) Destroy(view.gameObject);
        else _spare = view;
    }

    private ChartView TakeView()
    {
        ChartView view = _spare;
        _spare = null;
        if (view == null) view = MakeView();
        view.chartId = FirstChartId;
        view.gameObject.name = "Chart";
        view.SetLook(_look);
        return view;
    }

    private ChartView MakeView()
    {
        if (_root == null)
        {
            _root = new GameObject("Charts").transform;
            _root.SetParent(transform, false);
        }
        GameObject made = Instantiate(chartPrefab, _root);
        ChartView view = made.GetComponent<ChartView>();
        view.Init(markMaterial);
        view.SetShown(false);
        PrepareGrabs?.Invoke(view);
        return view;
    }

    private static void ResetPose(ChartView view)
    {
        view.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        view.transform.localScale = Vector3.one * ChartDisplay.DefaultScale;
        view.ForgetGrabLook();
    }

    private void OnSettingChanged(Setting which)
    {
        switch (which)
        {
            case Setting.ChartStyle:
            case Setting.ChartOpacity:
                SetLook(ChartLook.Current);
                return;
            case Setting.AssistantSpeed:
                ApplyAgentMotion();
                return;
            case Setting.NumberFormat:
            case Setting.NetworkLabels:
                ChartDisplay.SyncWorkerFlags();
                ReloadLayouts();
                return;
            case Setting.ChartHeight:
            case Setting.ChartDistance:
                ReplaceAhead();
                return;
            case Setting.DefaultChartSize:
                ApplyDefaultSize();
                return;
            case Setting.ColorMode:
                Repaint();
                return;
            case Setting.Backdrop:
            case Setting.BackdropGridlines:
            case Setting.TickerLabels:
            case Setting.AxisLabels:
            case Setting.ChartKey:
            case Setting.ChartDescription:
            case Setting.BackdropOpacity:
            case Setting.ChartLabelSize:
            case Setting.ChartTheme:
                if (_view != null) _view.ApplyDisplayParts();
                if (_spare != null) _spare.ApplyDisplayParts();
                return;
        }
    }

    private void Repaint()
    {
        if (_view == null || _active == null || !_active.Loaded) return;
        if (_building != null) _building.Finish();
        _building = new MarkMeshBuild(_view.MarkMesh, _active.Geometry, ColorModes.Palette32());
    }

    private void ApplyAgentMotion()
    {
        switch (Settings.AssistantSpeed)
        {
            case AssistantSpeed.Fast: SetAgentMotion(1f, false); break;
            case AssistantSpeed.Instant: SetAgentMotion(0f, true); break;
            default: SetAgentMotion(0.5f, false); break;
        }
    }

    private void ReplaceAhead()
    {
        _hasHome = false;
        if (_view == null || _active == null || !_active.Loaded || _next != null) return;
        if (!TryLooseAnchor(_view.chartId, out _, out _)) AnchorAhead(_active.Geometry);
    }

    private void ApplyDefaultSize()
    {
        if (_view == null || _active == null) return;
        foreach (Edit edit in _active.Edits)
            if (edit.IsPose) return;
        _view.transform.localScale = Vector3.one * ChartDisplay.DefaultScale;
        _view.ForgetGrabLook();
    }

    private async void ReloadLayouts()
    {
        int ticket = ++_reloadTicket;
        Entry shown = _active;
        foreach (Entry entry in _entries)
        {
            if (entry == shown || !entry.Loaded) continue;
            entry.Geometry.Dispose();
            entry.Geometry = null;
        }
        if (shown == null || !shown.Loaded) return;

        ChartSpec spec = shown.Spec;
        Blueprint blueprint = shown.Blueprint;
        DataTable table = shown.Table;
        ChartGeometry fresh = await Task.Run(() => ChartModules.For(spec.BlueprintKind).Build(spec, blueprint, table));

        if (this == null || ticket != _reloadTicket || shown != _active)
        {
            fresh.Dispose();
            return;
        }

        if (_building != null) { _building.Finish(); _building = null; }
        ChartGeometry old = shown.Geometry;
        shown.Geometry = fresh;
        if (_view != null)
        {
            _view.Bind(spec, fresh);
            _building = new MarkMeshBuild(_view.MarkMesh, fresh, ColorModes.Palette32());
        }
        old.Dispose();
        OnChartChanged?.Invoke();
    }

    private void LateUpdate()
    {
        if (_next != null || _building == null || !_building.Done) return;
        _building.Finish();
        _building = null;
    }

    public void SetLook(ChartLook look)
    {
        _look = look;
        if (_view != null) _view.SetLook(look);
        if (_spare != null) _spare.SetLook(look);
    }

    public void SetGrabbable(bool on)
    {
        _chartsGrabbable = on;
        if (_view != null) _view.SetGrabbable(on);
    }

    public void SetOneGrab()
    {
        _twoGrabFor = null;
        if (_view != null) _view.SetOneGrab();
    }

    public void SetTwoGrab(Func<ChartView, ITransformer> transformerFor)
    {
        if (transformerFor == null) return;
        _twoGrabFor = transformerFor;
        ReanchorGrab(_view);
    }

    private void ReanchorGrab(ChartView view)
    {
        if (_twoGrabFor == null || view == null) return;
        try
        {
            view.SetTwoGrab(_twoGrabFor(view));
        }
        catch (Exception e)
        {
            Debug.LogError($"[ManageCharts] The chart could not take the two-hand transformer: {e}");
        }
    }

    public void ResetGrabs(bool reattach)
    {
        _twoGrabFor = null;
        ChartView view = _view;
        if (view == null) return;
        view.ForgetGrabLook();
        view.transform.localRotation = Quaternion.identity;
        view.transform.localScale = Vector3.one;
        if (reattach) view.transform.localPosition = Vector3.zero;
    }

    public void NotifyMoveCommitted(ChartView view, Vector3 prePos, Quaternion preRot, Vector3 preScale)
    {
        if (view == null) return;
        OnChartMoveCommitted?.Invoke(view, prePos, preRot, preScale);
    }

    public void CommitPendingGrabs()
    {
        ChartView view = _view;
        if (view != null && view.ForceGrabRelease(out Vector3 pos, out Quaternion rot, out Vector3 scale))
            NotifyMoveCommitted(view, pos, rot, scale);
    }

    public bool Undo(Edit edit)
    {
        if (edit == null || !edit.IsPose) return false;
        return RestorePose(edit.move.chartId, edit.move.prePos, edit.move.preRot, edit.move.preScale, AsAgent);
    }

    public bool RestorePose(int chartId, Vector3 pos, Quaternion rot, Vector3 scale, bool animate = false)
    {
        ChartView view = _view;
        if (view == null || view.chartId != chartId) return false;
        CancelMotion(view);
        view.ForgetGrabLook();
        Transform t = view.transform;
        Vector3 fromPos = t.localPosition;
        Quaternion fromRot = t.localRotation;
        Vector3 fromScale = t.localScale;
        t.localPosition = pos;
        t.localRotation = rot;
        t.localScale = scale;
        ReanchorGrab(view);
        if (animate) AnimateFrom(view, fromPos, fromRot, fromScale, amend: false);
        return true;
    }

    public void SetAgentMotion(float speed, bool instant)
    {
        _agentMotionSpeed = speed;
        _agentMotionInstant = instant;
    }

    private static bool AsAgent => StateChannel.InAgentCall;
    private bool AgentInstant => AsAgent && _agentMotionInstant;
    private float ActiveMotionSpeed => AsAgent && _agentMotionSpeed > 0f ? _agentMotionSpeed : ChartDisplay.MotionMetresPerSecond;

    private float MotionDuration(float metres)
    {
        if (!Style.Animate || AgentInstant || !isActiveAndEnabled) return 0f;
        return Mathf.Clamp(metres / Mathf.Max(ActiveMotionSpeed, 1e-3f), minMotionSeconds, maxMotionSeconds);
    }

    private float HeightMetres(ChartView view)
    {
        Bounds b = view.LocalBounds;
        return b.size.y * Mathf.Abs(view.transform.lossyScale.y);
    }

    private sealed class Glide
    {
        public Vector3 toPos;
        public Quaternion toRot;
        public Vector3 toScale;
        public Coroutine routine;
        public bool amend = true;
    }

    private readonly Dictionary<ChartView, Glide> _glides = new Dictionary<ChartView, Glide>();

    public void GetCommittedPose(ChartView view, out Vector3 pos, out Quaternion rot, out Vector3 scale)
    {
        Transform t = view.transform;
        bool gliding = _glides.TryGetValue(view, out Glide g);
        pos = gliding ? g.toPos : t.localPosition;
        rot = gliding ? g.toRot : t.localRotation;
        scale = gliding ? g.toScale : t.localScale;
    }

    private bool TryStopGlide(ChartView view, out Glide glide)
    {
        if (view == null || !_glides.TryGetValue(view, out glide)) { glide = null; return false; }
        if (glide.routine != null) StopCoroutine(glide.routine);
        _glides.Remove(view);
        return true;
    }

    public void CompleteMotion(ChartView view)
    {
        if (TryStopGlide(view, out Glide g)) Land(view, g);
    }

    private static void Land(ChartView view, Glide g)
    {
        Transform t = view.transform;
        t.localPosition = g.toPos;
        t.localRotation = g.toRot;
        t.localScale = g.toScale;
    }

    public void CancelMotion(ChartView view) => TryStopGlide(view, out _);

    public void Interrupt()
    {
        ChartView view = _view;
        if (view == null) return;
        view.CompleteMotion();
        if (!TryStopGlide(view, out Glide g)) return;
        if (g.amend) AmendPoseRecord(view);
        else Land(view, g);
    }

    private void AmendPoseRecord(ChartView view)
    {
        EditList edits = ActiveEdits;
        for (int i = edits.Count - 1; i >= 0; i--)
        {
            Edit e = edits[i];
            if (!e.IsPose || e.move.chartId != view.chartId) continue;

            Transform t = view.transform;
            MoveRecord m = e.move;
            m.postPos = t.localPosition;
            m.postRot = t.localRotation;
            m.postScale = t.localScale;
            m.distance = transform.TransformVector(m.postPos - m.prePos).magnitude;
            e.move = m;

            if (m.IsNoChange) edits.DropAt(i);
            return;
        }
    }

    public void AnimateFrom(ChartView view, Vector3 prePos, Quaternion preRot, Vector3 preScale, bool amend = true)
    {
        if (view == null || !isActiveAndEnabled) return;
        if (AgentInstant) { CancelMotion(view); return; }
        CompleteMotion(view);

        Transform t = view.transform;
        var g = new Glide { toPos = t.localPosition, toRot = t.localRotation, toScale = t.localScale, amend = amend };

        float rootScale = Mathf.Abs(transform.lossyScale.x);
        float radius = view.LocalBounds.extents.magnitude * rootScale;
        float atScale = radius * Mathf.Max(Mathf.Abs(preScale.x), Mathf.Abs(g.toScale.x));

        float moveMetres = transform.TransformVector(g.toPos - prePos).magnitude;
        float turnMetres = Quaternion.Angle(preRot, g.toRot) * Mathf.Deg2Rad * atScale;
        float scaleMetres = Mathf.Abs(g.toScale.x - preScale.x) * radius;
        float duration = MotionDuration(Mathf.Max(moveMetres, Mathf.Max(turnMetres, scaleMetres)));

        t.localPosition = prePos;
        t.localRotation = preRot;
        t.localScale = preScale;

        g.routine = StartCoroutine(GlideRoutine(view, g, prePos, preRot, preScale, duration));
        _glides[view] = g;
    }

    private IEnumerator GlideRoutine(ChartView view, Glide g, Vector3 fromPos, Quaternion fromRot, Vector3 fromScale, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            yield return null;
            if (view == null) yield break;
            if (view.IsGrabbed) { _glides.Remove(view); yield break; }

            t += Time.deltaTime;
            float k = Style.EaseOut(t / duration);
            Transform tr = view.transform;
            tr.localPosition = Vector3.Lerp(fromPos, g.toPos, k);
            tr.localRotation = Quaternion.Slerp(fromRot, g.toRot, k);
            tr.localScale = Vector3.Lerp(fromScale, g.toScale, k);
        }
        if (view != null) Land(view, g);
        _glides.Remove(view);
    }

    public bool IsDetached(int chartId) => TryLooseAnchor(chartId, out _, out _);

    private static bool TryLooseAnchor(int chartId, out Vector3 pos, out Quaternion rot)
    {
        EditList edits = ActiveEdits;
        for (int i = 0; i < edits.Count; i++)
        {
            Edit e = edits[i];
            if (e == null || e.kind != EditKind.Move || e.move.chartId != chartId) continue;
            pos = e.move.anchorPos;
            rot = e.move.anchorRot;
            return true;
        }
        pos = Vector3.zero;
        rot = Quaternion.identity;
        return false;
    }
}
