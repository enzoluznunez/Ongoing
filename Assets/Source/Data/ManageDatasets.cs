using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ManageDatasets : MonoBehaviour
{
    public static ManageDatasets Instance { get; private set; }
    public static DataSource ActiveSource => Instance != null ? Instance.Active : null;

    public ManageSheets sheetManager;
    public ManageTools toolManager;

    public event Action<int> OnActiveDatasetChanged;

    public event Action<string> OnRequestFailed;

    public event Action<DataSource> OnDatasetRead;

    public class Dataset
    {
        public string label;
        public string payload;

        public string Name => string.IsNullOrEmpty(label) ? "dataset" : label;

        public string description;

        public DataSource source;
        public bool loaded;

        public bool loading => source != null && !loaded;

        public readonly EditList Edits = new EditList();
    }

    private readonly Dictionary<Parser, Dataset> _reads = new Dictionary<Parser, Dataset>();

    private readonly List<Dataset> _datasets = new List<Dataset>();
    private int _active = -1;

    private Dataset _requested;
    private int _datasetsCreated;
    private static readonly EditList Unowned = new EditList();

    public IReadOnlyList<Dataset> Datasets => _datasets;
    public int ActiveIndex => _active;
    public int DatasetCount => _datasets.Count;
    public DataSource Active => (_active >= 0 && _active < _datasets.Count) ? _datasets[_active].source : null;
    public Dataset ActiveDataset => (_active >= 0 && _active < _datasets.Count) ? _datasets[_active] : null;

    public static EditList ActiveEdits
    {
        get
        {
            Dataset d = Instance != null ? Instance.ActiveDataset : null;
            return d != null ? d.Edits : Unowned;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        ResolveRefs();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void ResolveRefs()
    {
        if (sheetManager == null) sheetManager = Scene.Sheets;
        if (toolManager == null) toolManager = Scene.Tools;
    }

    public string PayloadOf(DataSource source)
    {
        if (source == null) return null;
        for (int i = 0; i < _datasets.Count; i++)
            if (_datasets[i].source == source) return _datasets[i].payload;
        return null;
    }

    public int IndexOf(string payload)
    {
        for (int i = 0; i < _datasets.Count; i++)
            if (_datasets[i].payload == payload) return i;
        return -1;
    }

    public void AddCatalogEntries(IEnumerable<(string file, string label, string description)> entries)
    {
        if (entries == null) return;

        foreach ((string file, string label, string description) in entries)
        {
            if (string.IsNullOrEmpty(file) || IndexOf(file) >= 0) continue;

            _datasets.Add(new Dataset
            {
                payload = file,
                label = string.IsNullOrEmpty(label) ? Stylize(DeriveLabel(file, _datasets.Count)) : label,
                description = description
            });
        }
    }

    private readonly Dictionary<Dataset, TaskCompletionSource<bool>> _awaitingLoad =
        new Dictionary<Dataset, TaskCompletionSource<bool>>();

    public Task<bool> EnsureLoaded(int index)
    {
        if (index < 0 || index >= _datasets.Count) return Task.FromResult(false);

        Dataset dataset = _datasets[index];
        if (dataset.loaded) return Task.FromResult(true);

        if (!_awaitingLoad.TryGetValue(dataset, out TaskCompletionSource<bool> waiting))
        {
            waiting = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _awaitingLoad[dataset] = waiting;
        }

        BeginLoad(dataset);
        return waiting.Task;
    }

    private bool SettleLoad(Dataset dataset, bool ok)
    {
        if (!_awaitingLoad.TryGetValue(dataset, out TaskCompletionSource<bool> waiting)) return false;
        _awaitingLoad.Remove(dataset);
        waiting.SetResult(ok);
        return true;
    }

    private const int PreloadsAtOnce = 3;
    private readonly Queue<Dataset> _preloads = new Queue<Dataset>();
    private readonly HashSet<Dataset> _preloading = new HashSet<Dataset>();

    public void Preload(int index)
    {
        if (index < 0 || index >= _datasets.Count) return;
        _preloads.Enqueue(_datasets[index]);
        PreloadsQueued++;
        PumpPreloads();
    }

    public int PreloadsQueued { get; private set; }
    public int PreloadsLeft => _preloads.Count + _preloading.Count;

    private void PumpPreloads()
    {
        while (_preloading.Count < PreloadsAtOnce && _preloads.Count > 0)
        {
            Dataset next = _preloads.Dequeue();
            if (next.loading || next.loaded || !_datasets.Contains(next)) continue;
            _preloading.Add(next);
            BeginLoad(next);
        }
    }

    private void SettlePreload(Dataset dataset)
    {
        if (_preloading.Remove(dataset)) PumpPreloads();
    }

    private void BeginLoad(Dataset dataset)
    {
        if (dataset == null || dataset.loading || dataset.loaded) return;

        GameObject host = new GameObject($"Dataset_{_datasetsCreated++}");
        host.transform.SetParent(transform, false);

        Parser reader = host.AddComponent<Parser>();
        dataset.source = reader;
        _reads[reader] = dataset;

        reader.onLoadResult = (ok, reason) => OnDatasetLoadResult(reader, ok, reason);
        reader.Load(dataset.payload);
    }

    private void OnDatasetLoadResult(Parser reader, bool ok, string reason)
    {
        if (!_reads.TryGetValue(reader, out Dataset dataset)) return;
        _reads.Remove(reader);
        SettlePreload(dataset);

        int index = _datasets.IndexOf(dataset);
        if (index < 0) return;

        bool awaited = SettleLoad(dataset, ok);

        bool requested = dataset == _requested;
        if (requested) _requested = null;
        bool preloaded = !requested && !awaited;

        if (!ok)
        {
            Unload(dataset);
            if (!preloaded)
            {
                Notices.Show(this, Strings.Get("notice.sheetUnavailable"),
                    reason ?? Strings.Format("notice.sheetUnavailable.body", dataset.label));
                OnRequestFailed?.Invoke(dataset.payload);
            }
            return;
        }

        dataset.loaded = true;
        OnDatasetRead?.Invoke(dataset.source);

        if (!preloaded) StateChannel.Record($"loaded a new dataset, {dataset.label}");
        if (requested) Activate(index);
    }

    public void SwitchDataset(int index)
    {
        if (index < 0 || index >= _datasets.Count) return;

        _requested = null;
        if (index == _active && _datasets[index].loaded) return;

        if (!_datasets[index].loaded)
        {
            Request(_datasets[index]);
            return;
        }

        Activate(index);
    }

    public void CancelRequest() => _requested = null;


    private static readonly Unity.Profiling.ProfilerMarker ActivateMarker =
        new Unity.Profiling.ProfilerMarker("ManageDatasets.Activate");

    private void Activate(int index)
    {
        using var marker = ActivateMarker.Auto();
        SettleHands();

        _active = index;
        Dataset next = _datasets[index];

        Rebind(next);
        OnActiveDatasetChanged?.Invoke(index);

        StateChannel.RecordState("dataset",
            $"the {next.Name} dataset is open" +
            "; row and column numbers and edits all belong to it");

        ReportAxes(next.source);
    }

    private static void ReportAxes(DataSource source)
    {
        if (source == null) return;

        string columns = AxisSketch(source, true);
        string rows = AxisSketch(source, false);
        if (columns == null || rows == null) return;

        StateChannel.SetState("axes", $"the columns are {columns}; the rows are {rows}");
    }

    private static string AxisSketch(DataSource source, bool columns)
    {
        IReadOnlyList<int> order = columns ? source.ColumnOrder : source.RowOrder;
        if (order == null || order.Count == 0) return null;

        var shown = new List<string>(2);
        for (int i = 0; i < order.Count && shown.Count < 2; i++)
        {
            string title = source.TitleAt(columns, i);
            if (!string.IsNullOrEmpty(title)) shown.Add(title);
        }
        if (shown.Count == 0) return null;

        int rest = order.Count - shown.Count;
        string list = string.Join(", ", shown);
        return rest > 0 ? $"{list} and {rest} more" : list;
    }

    private void Request(Dataset dataset)
    {
        _requested = dataset;
        BeginLoad(dataset);
    }

    private void SettleHands()
    {
        if (sheetManager != null) sheetManager.CommitPendingGrabs();
        if (toolManager != null) toolManager.DeselectTool();
    }

    private void Unload(Dataset dataset)
    {
        dataset.loaded = false;
        if (dataset.source != null) Destroy(dataset.source.gameObject);
        dataset.source = null;
    }

    private void Rebind(Dataset dataset)
    {
        TryStep("sheetManager", () => { if (sheetManager != null) sheetManager.SetDataSource(dataset.source); });
        TryStep("replay", () => { if (sheetManager != null) sheetManager.ReplayEdits(dataset.Edits); });
    }

    private void TryStep(string label, Action step)
    {
        try { step(); }
        catch (Exception e) { Debug.LogError($"[ManageDatasets] Rebind step '{label}' failed: {e}"); }
    }

    private static string Stylize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        string[] words = raw.Split(new[] { '_', '-', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return raw;

        for (int i = 0; i < words.Length; i++)
        {
            if (HasInnerUppercase(words[i])) continue;
            string w = words[i].ToLowerInvariant();
            bool edge = i == 0 || i == words.Length - 1;
            words[i] = (!edge && SmallWords.Contains(w))
                ? w
                : char.ToUpperInvariant(w[0]) + w.Substring(1);
        }
        return string.Join(" ", words);
    }

    private static readonly HashSet<string> SmallWords = new HashSet<string>
    {
        "a", "an", "and", "as", "at", "but", "by", "for", "if", "in", "nor", "of",
        "on", "or", "per", "so", "the", "to", "up", "via", "vs", "yet"
    };

    private static bool HasInnerUppercase(string word)
    {
        for (int i = 1; i < word.Length; i++)
            if (char.IsUpper(word[i])) return true;
        return false;
    }

    private static string DeriveLabel(string payload, int ordinal)
    {
        string fallback = $"Dataset {ordinal + 1}";
        if (string.IsNullOrEmpty(payload)) return fallback;

        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return DeriveDataUriName(payload) ?? fallback;

        if (payload.IndexOf('\n') >= 0)
            return fallback;

        string trimmed = payload.Trim();
        int query = trimmed.IndexOf('?');
        if (query >= 0) trimmed = trimmed.Substring(0, query);
        trimmed = trimmed.TrimEnd('/');

        int slash = trimmed.LastIndexOf('/');
        string name = slash >= 0 ? trimmed.Substring(slash + 1) : trimmed;

        int dot = name.LastIndexOf('.');
        if (dot > 0) name = name.Substring(0, dot);

        return string.IsNullOrEmpty(name) ? fallback : name;
    }

    private static string DeriveDataUriName(string payload)
    {
        int comma = payload.IndexOf(',');
        string header = comma >= 0 ? payload.Substring(0, comma) : payload;

        int nameIdx = header.IndexOf("name=", StringComparison.OrdinalIgnoreCase);
        if (nameIdx < 0) return null;

        string name = header.Substring(nameIdx + 5);
        int semi = name.IndexOf(';');
        if (semi >= 0) name = name.Substring(0, semi);
        name = name.Trim();

        int dot = name.LastIndexOf('.');
        if (dot > 0) name = name.Substring(0, dot);

        return string.IsNullOrEmpty(name) ? null : name;
    }
}
