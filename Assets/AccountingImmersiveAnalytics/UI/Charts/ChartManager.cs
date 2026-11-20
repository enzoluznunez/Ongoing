using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Data;
using AccountingImmersiveAnalytics.Documents;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.View;
using AccountingImmersiveAnalytics.UI.Charts.View;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts
{
    public class ChartManager : MonoBehaviour
    {
        private const string ColumnName = "Chart Column";
        private const float MotionMetresPerSecond = 0.6f;
        private const float ShortestMotion = 0.1f;
        private const float LongestMotion = 1.5f;

        public static float MotionSeconds(float metres) => Mathf.Clamp(metres / MotionMetresPerSecond, ShortestMotion, LongestMotion);

        public static ChartManager Instance { get; private set; }

        private static readonly EditList Unowned = new EditList();

        public static EditList ActiveEdits => Instance != null ? Instance._edits : Unowned;


        private readonly EditList _edits = new EditList();
        private readonly List<ChartEntry> _entries = new List<ChartEntry>();
        private readonly Dictionary<string, List<ChartEntry>> _byFile = new Dictionary<string, List<ChartEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ChartEntry> _shown = new List<ChartEntry>();
        private readonly Dictionary<Transform, Glide> _glides = new Dictionary<Transform, Glide>();
        private ChartColumn _column;
        private RectTransform _columnRoot;
        private PinnedChart _pinned;
        private ChartEntry _pinnedEntry;
        private int _ticket;
        private int _nextId = 1;
        private bool _panelShown = true;

        public IReadOnlyList<ChartEntry> Shown => _shown;
        public ChartEntry Pinned => _pinnedEntry;
        public float PinnedButtonSize => _pinned != null ? _pinned.ButtonSize : Style.ButtonSize;
        public ChartCard PinnedCard => _pinnedEntry != null && _pinned != null ? _pinned.Card : null;
        public Transform PinnedTransform => _pinnedEntry != null && _pinned != null ? _pinned.Holder : null;
        public ChartEntry Active => _pinnedEntry ?? (_shown.Count == 1 ? _shown[0] : null);
        public string Status { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _pinned?.Destroy();
        }

        public void CollectLiveCards(List<ChartCard> into)
        {
            if (_pinned != null && _pinned.Card != null && _pinned.Card.isActiveAndEnabled) into.Add(_pinned.Card);
        }

        public ChartEntry Find(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Active;
            string wanted = query.Trim();
            foreach (List<ChartEntry> pool in new[] { _shown, _entries })
            {
                foreach (ChartEntry entry in pool)
                    if (string.Equals(entry.Title, wanted, StringComparison.OrdinalIgnoreCase) || entry.Id.ToString() == wanted) return entry;
                ChartEntry only = null;
                int hits = 0;
                foreach (ChartEntry entry in pool)
                {
                    if (entry.Title.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    only = entry;
                    hits++;
                }
                if (hits == 1) return only;
            }
            return null;
        }

        public ChartEntry WithId(int id)
        {
            foreach (ChartEntry entry in _entries)
                if (entry.Id == id) return entry;
            return null;
        }

        public async Task<(IReadOnlyList<ChartEntry> charts, string failure, bool unreadable)> OpenCsv(DocumentEntry document)
        {
            int ticket = ++_ticket;
            if (!EnsureColumn()) return (null, "The Documents panel was not found, so there is nowhere to show the charts.", false);
            _column.Clear();
            _shown.Clear();

            if (_byFile.TryGetValue(document.File, out List<ChartEntry> known))
            {
                ShowEntries(known);
                return (_shown, null, false);
            }

            string text = await StreamingFiles.ReadText(document.File);
            if (this == null || ticket != _ticket) return (null, "Another file was opened before this one finished.", false);
            if (!CsvTable.LooksLikeCsv(text)) return Unreadable($"\"{document.Title}\" is not a readable CSV file.");

            CsvTable table;
            try
            {
                table = await Task.Run(() => CsvTable.Parse(text));
            }
            catch (Exception e)
            {
                return Unreadable($"\"{document.Title}\" could not be loaded: {e.Message}");
            }
            if (this == null || ticket != _ticket) return (null, "Another file was opened before this one finished.", false);

            List<ChartSpec> specs = await ChartPlanner.Plan(document.Title, table);
            if (this == null || ticket != _ticket) return (null, "Another file was opened before this one finished.", false);
            if (specs.Count == 0) return Fail($"No chart fits the columns in \"{document.Title}\".");

            var made = new List<ChartEntry>();
            foreach (ChartSpec spec in specs)
            {
                var entry = new ChartEntry { Id = _nextId++, File = document.File, Source = document.Title, Spec = spec, Table = table };
                _entries.Add(entry);
                made.Add(entry);
            }
            _byFile[document.File] = made;
            ShowEntries(made);
            return (_shown, null, false);

            (IReadOnlyList<ChartEntry>, string, bool) Fail(string reason)
            {
                SetStatus(reason);
                Debug.LogWarning($"[ChartManager] {reason}");
                return (null, reason, false);
            }

            (IReadOnlyList<ChartEntry>, string, bool) Unreadable(string reason)
            {
                Debug.LogWarning($"[ChartManager] {reason}");
                return (null, reason, true);
            }
        }

        public async Task<(IReadOnlyList<ChartEntry> charts, string failure)> OpenTables(DocumentEntry document, IReadOnlyList<PdfTable> tables)
        {
            int ticket = ++_ticket;
            if (tables == null || tables.Count == 0)
            {
                CloseColumn();
                return (null, null);
            }
            if (!EnsureColumn()) return (null, "The Documents panel was not found, so there is nowhere to show the charts.");
            _column.Clear();
            _shown.Clear();

            if (_byFile.TryGetValue(document.File, out List<ChartEntry> known))
            {
                ShowEntries(known);
                return (_shown, null);
            }

            var read = new List<(PdfTable source, CsvTable table)>();
            foreach (PdfTable source in tables)
            {
                try
                {
                    read.Add((source, CsvTable.FromRows(source.Rows)));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ChartManager] A table on page {source.Page + 1} of \"{document.Title}\" could not be read: {e.Message}");
                }
            }

            ChartSpec[] specs = await Task.WhenAll(read.Select(r => ChartPlanner.PlanTable(document.Title, r.table, r.source.Caption)));
            if (this == null || ticket != _ticket) return (null, "Another file was opened before this one finished.");

            var made = new List<ChartEntry>();
            for (int i = 0; i < specs.Length; i++)
            {
                if (specs[i] == null) continue;
                var entry = new ChartEntry { Id = _nextId++, File = document.File, Source = document.Title, Spec = specs[i], Table = read[i].table };
                _entries.Add(entry);
                made.Add(entry);
            }
            if (made.Count == 0)
            {
                string reason = $"No chart fits the tables in \"{document.Title}\".";
                SetStatus(reason);
                Debug.LogWarning($"[ChartManager] {reason}");
                return (null, reason);
            }
            _byFile[document.File] = made;
            ShowEntries(made);
            return (_shown, null);
        }

        private void ShowEntries(List<ChartEntry> entries)
        {
            SetStatus(null);
            foreach (ChartEntry entry in entries)
            {
                entry.Preview = _column.Add(entry, TogglePin);
                _shown.Add(entry);
            }
            _column.SetPinned(_pinnedEntry);
            var titles = new List<string>();
            foreach (ChartEntry entry in entries) titles.Add($"\"{entry.Title}\" ({entry.Spec.Describe()})");
            ActivityFeed.RecordState("charts", $"the chart column beside the Documents panel shows {titles.Count} charts: {string.Join(", ", titles)}");
        }

        private void SetStatus(string message)
        {
            Status = message;
            _column?.ShowStatus(message);
        }

        private bool EnsureColumn()
        {
            DocumentsPanel documents = App.Scene.Documents;
            if (documents == null) return false;
            _columnRoot = documents.ShowRightCard(ColumnName, ChartColumn.Width);
            if (_columnRoot == null) return false;
            if (_column == null) _column = new ChartColumn(_columnRoot);
            return true;
        }

        public void CloseColumn()
        {
            _ticket++;
            _shown.Clear();
            _column?.Clear();
            DocumentsPanel documents = App.Scene.Documents;
            if (documents != null) documents.HideRightCard();
        }

        public void SetPanelShown(bool on)
        {
            _panelShown = on;
            _pinned?.SetShown(on);
        }

        public void TogglePin(ChartEntry entry)
        {
            if (ActivityFeed.UserDriven) AssistantTurn.UserTookControl();
            Pin(_pinnedEntry == entry ? null : entry);
        }

        public bool Pin(ChartEntry entry, bool record = true)
        {
            if (entry == _pinnedEntry) return false;
            int before = _pinnedEntry?.Id ?? -1;
            if (entry == null)
            {
                _pinned?.Clear();
                _pinnedEntry = null;
                ToolManager tools = App.Scene.Tools;
                if (tools != null) tools.DeselectTool();
            }
            else
            {
                DocumentsPanel documents = App.Scene.Documents;
                if (documents == null || documents.PrimaryRect == null) return false;
                if (_pinned == null) _pinned = new PinnedChart(documents.transform, documents.gameObject.layer, this);
                _pinnedEntry = entry;
                _pinned.Show(entry, PinPose(documents), UnitsToMetres(documents));
                _pinned.SetShown(_panelShown);
            }
            _column?.SetPinned(_pinnedEntry);
            if (record) _edits.PushPin(before);
            ActivityFeed.Record(_pinnedEntry != null
                ? $"pinned the chart \"{_pinnedEntry.Title}\" in front of the Documents panel"
                : "unpinned the chart");
            ActivityFeed.RecordState("pinned", _pinnedEntry != null
                ? $"the chart \"{_pinnedEntry.Title}\" is pinned in front of the Documents panel"
                : "no chart is pinned");
            return true;
        }

        private static float UnitsToMetres(DocumentsPanel documents)
        {
            RectTransform primary = documents.PrimaryRect;
            return primary.lossyScale.x;
        }

        private static Pose PinPose(DocumentsPanel documents)
        {
            RectTransform primary = documents.PrimaryRect;
            Vector3 centre = primary.TransformPoint(primary.rect.center);
            Quaternion facing = primary.rotation;
            return new Pose(centre - primary.forward * ChartStyle.PinnedLift, facing);
        }

        public bool Reshape(ChartEntry entry, ChartShaping next, EditKind kind, string what, string label)
        {
            if (entry == null || next == null || entry.Shaping.SameAs(next)) return false;
            ChartShaping before = entry.Shaping.Copy();
            Apply(entry, next);
            _edits.PushShape(kind, entry.Id, before, label);
            ActivityFeed.Record(what);
            return true;
        }

        public void Preview(ChartEntry entry, ChartShaping shaping)
        {
            if (entry == null || shaping == null || entry.Shaping.SameAs(shaping)) return;
            entry.Shaping = shaping.Copy();
            if (entry.Preview != null) entry.Preview.Redraw();
            if (entry == _pinnedEntry && _pinned?.Card != null) _pinned.Card.Redraw();
        }

        public bool Commit(ChartEntry entry, ChartShaping before, EditKind kind, string what, string label)
        {
            if (entry == null || before == null || entry.Shaping.SameAs(before)) return false;
            _edits.PushShape(kind, entry.Id, before.Copy(), label);
            ActivityFeed.Record(what);
            Apply(entry, entry.Shaping);
            return true;
        }

        public bool SetTrend(ChartEntry entry, TrendKind trend)
        {
            if (entry == null) return false;
            ChartShaping next = entry.Shaping.Copy();
            next.Trend = trend;
            bool off = trend == TrendKind.None;
            string label = off ? "Turned the trend line off" : $"Set the trend to {trend}";
            string what = off ? $"turned off the trend line on \"{entry.Title}\"" : $"set the trend on \"{entry.Title}\" to {trend.ToString().ToLowerInvariant()}";
            return Reshape(entry, next, EditKind.Trend, what, label);
        }

        public bool UndoNewest(ChartEntry entry)
        {
            if (entry == null) return false;
            int index = _edits.NewestFor(entry.Id);
            if (index < 0) return false;
            Edit edit = _edits[index];
            bool applied = Undo(edit);
            _edits.DropAt(index);
            if (!applied) return false;
            ActivityFeed.Record($"undid the {Edit.KindName(edit.Kind)} edit on \"{entry.Title}\"");
            return true;
        }

        private void Apply(ChartEntry entry, ChartShaping shaping)
        {
            entry.Shaping = shaping.Copy();
            if (entry.Preview != null) entry.Preview.Redraw();
            if (entry == _pinnedEntry && _pinned?.Card != null)
            {
                _pinned.Card.Redraw();
                _pinned.ShapingChanged();
            }
        }

        public bool Undo(Edit edit)
        {
            if (edit == null) return false;
            switch (edit.Kind)
            {
                case EditKind.Pin:
                {
                    int id = edit.PinnedBefore;
                    ChartEntry target = id >= 0 ? WithId(id) : null;
                    if (id >= 0 && target == null) return false;
                    Pin(target, record: false);
                    return true;
                }
                case EditKind.Sort:
                case EditKind.Filter:
                case EditKind.Trend:
                {
                    ChartEntry entry = WithId(edit.ChartId);
                    if (entry == null) return false;
                    Apply(entry, edit.Before);
                    return true;
                }
                default:
                {
                    Transform pinned = PinnedTransform;
                    if (pinned == null || _pinnedEntry.Id != edit.Move.ChartId) return false;
                    MoveRecord m = edit.Move;
                    RestorePose(pinned, m.PrePos, m.PreRot, m.PreScale);
                    return true;
                }
            }
        }

        public void NotifyMoveCommitted(Transform chart, Vector3 prePos, Quaternion preRot, Vector3 preScale, EditKind kind,
            string what = null)
        {
            if (chart == null || _pinnedEntry == null) return;
            if (MoveRecord.SamePose(chart.localPosition, chart.localRotation, chart.localScale, prePos, preRot, preScale)) return;
            _edits.PushMove(new MoveRecord
            {
                ChartId = _pinnedEntry.Id,
                PrePos = prePos,
                PreRot = preRot,
                PreScale = preScale
            }, kind, what ?? (kind == EditKind.Scale ? "Resized the chart" : kind == EditKind.Rotate ? "Turned the chart" : "Moved the chart"));
        }

        private sealed class Glide
        {
            public Vector3 ToPos;
            public Quaternion ToRot;
            public Vector3 ToScale;
            public Coroutine Routine;
        }

        public void GetCommittedPose(Transform chart, out Vector3 pos, out Quaternion rot, out Vector3 scale)
        {
            bool gliding = _glides.TryGetValue(chart, out Glide g);
            pos = gliding ? g.ToPos : chart.localPosition;
            rot = gliding ? g.ToRot : chart.localRotation;
            scale = gliding ? g.ToScale : chart.localScale;
        }

        public void CompleteMotion(Transform chart)
        {
            if (chart == null || !_glides.TryGetValue(chart, out Glide g)) return;
            if (g.Routine != null) StopCoroutine(g.Routine);
            _glides.Remove(chart);
            chart.localPosition = g.ToPos;
            chart.localRotation = g.ToRot;
            chart.localScale = g.ToScale;
        }

        public void Interrupt()
        {
            Transform pinned = PinnedTransform;
            if (pinned != null) CompleteMotion(pinned);
        }

        public void RestorePose(Transform chart, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            CompleteMotion(chart);
            Vector3 fromPos = chart.localPosition;
            Quaternion fromRot = chart.localRotation;
            Vector3 fromScale = chart.localScale;
            chart.localPosition = pos;
            chart.localRotation = rot;
            chart.localScale = scale;
            if (ActivityFeed.InAssistantCall) AnimateFrom(chart, fromPos, fromRot, fromScale);
        }

        public void AnimateFrom(Transform chart, Vector3 prePos, Quaternion preRot, Vector3 preScale)
        {
            if (chart == null || !isActiveAndEnabled) return;
            CompleteMotion(chart);
            var g = new Glide { ToPos = chart.localPosition, ToRot = chart.localRotation, ToScale = chart.localScale };
            Transform space = chart.parent != null ? chart.parent : transform;
            float size = ChartStyle.CardWidth * ChartStyle.PinnedScale * Style.WorldScale;
            float moved = space.TransformVector(g.ToPos - prePos).magnitude;
            float turned = Quaternion.Angle(preRot, g.ToRot) * Mathf.Deg2Rad * size;
            float grown = Mathf.Abs(g.ToScale.x - preScale.x) * size;
            float seconds = Mathf.Clamp(Mathf.Max(moved, Mathf.Max(turned, grown)) / MotionMetresPerSecond, ShortestMotion, LongestMotion);
            chart.localPosition = prePos;
            chart.localRotation = preRot;
            chart.localScale = preScale;
            g.Routine = StartCoroutine(GlideRoutine(chart, g, prePos, preRot, preScale, seconds));
            _glides[chart] = g;
        }

        private IEnumerator GlideRoutine(Transform chart, Glide g, Vector3 fromPos, Quaternion fromRot, Vector3 fromScale, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                yield return null;
                if (chart == null) yield break;
                t += Time.deltaTime;
                float k = Animate.EaseOut(t / seconds);
                chart.localPosition = Vector3.Lerp(fromPos, g.ToPos, k);
                chart.localRotation = Quaternion.Slerp(fromRot, g.ToRot, k);
                chart.localScale = Vector3.Lerp(fromScale, g.ToScale, k);
            }
            if (chart == null) yield break;
            chart.localPosition = g.ToPos;
            chart.localRotation = g.ToRot;
            chart.localScale = g.ToScale;
            _glides.Remove(chart);
        }
    }
}
