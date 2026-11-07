using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class ChartLabels
{
    private const float FadeSeconds = 0.12f;
    private const float UpwardTiltDegrees = 45f;
    private const float TextScale = 0.012f;
    private static readonly Vector2 LabelBox = new Vector2(30f, 4f);

    private readonly Transform _owner;
    private readonly List<TextMeshPro> _pool = new List<TextMeshPro>();
    private readonly List<TextMeshPro> _rows = new List<TextMeshPro>();
    private readonly List<TextMeshPro> _columns = new List<TextMeshPro>();
    private readonly List<TextMeshPro> _heights = new List<TextMeshPro>();
    private readonly List<TextMeshPro> _free = new List<TextMeshPro>();
    private TextMeshPro _rowTitle;
    private TextMeshPro _columnTitle;
    private TextMeshPro _heightTitle;
    private float _titleOut;
    private float _heightTitleY;
    private Transform _root;

    private float _lowX, _highX, _lowZ, _highZ, _y, _margin;
    private bool _viewerHighX, _viewerHighZ;
    private bool _wantHighX, _wantHighZ;
    private bool _placed;
    private bool _shown = true;
    private bool _tickersOn = true;
    private float _sizeScale = 1f;
    private Color _color = Color.white;
    private bool _titlesOn = true;
    private float _fade = 1f;
    private bool _flipping;

    public ChartLabels(Transform owner) => _owner = owner;

    public void Rebuild(ChartGeometry geometry)
    {
        _rows.Clear();
        _columns.Clear();
        _heights.Clear();
        _free.Clear();
        _rowTitle = _columnTitle = _heightTitle = null;
        _placed = false;
        _fade = 1f;
        _flipping = false;

        Bounds b = geometry.Bounds;
        _lowX = b.min.x - geometry.LabelGap;
        _highX = b.max.x + geometry.LabelGap;
        _lowZ = b.min.z - geometry.LabelGap;
        _highZ = b.max.z + geometry.LabelGap;
        _y = geometry.BaseY;
        _margin = geometry.LabelGap;

        int used = 0;
        foreach (ChartLabel label in geometry.RowLabels) _rows.Add(Write(used++, label.text));
        foreach (ChartLabel label in geometry.ColumnLabels) _columns.Add(Write(used++, label.text));
        foreach (ChartLabel label in geometry.HeightLabels) _heights.Add(Write(used++, label.text));
        foreach (FreeLabel label in geometry.FreeLabels) _free.Add(Write(used++, label.text));
        if (!string.IsNullOrEmpty(geometry.RowTitle)) _rowTitle = Write(used++, geometry.RowTitle);
        if (!string.IsNullOrEmpty(geometry.ColumnTitle)) _columnTitle = Write(used++, geometry.ColumnTitle);
        if (!string.IsNullOrEmpty(geometry.HeightTitle)) _heightTitle = Write(used++, geometry.HeightTitle);
        for (int i = used; i < _pool.Count; i++) _pool[i].renderer.enabled = false;
        ApplyAll();

        for (int i = 0; i < _rows.Count; i++)
            _rows[i].transform.localPosition = new Vector3(_lowX, _y, geometry.RowLabels[i].coord);
        for (int i = 0; i < _columns.Count; i++)
            _columns[i].transform.localPosition = new Vector3(geometry.ColumnLabels[i].coord, _y, _lowZ);
        for (int i = 0; i < _heights.Count; i++)
            _heights[i].transform.localPosition = new Vector3(_lowX, geometry.HeightLabels[i].coord, _highZ);
        for (int i = 0; i < _free.Count; i++)
        {
            _free[i].transform.localPosition = geometry.FreeLabels[i].position;
            _free[i].alignment = TextAlignmentOptions.Center;
            _free[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
        _titleOut = Mathf.Max(geometry.LabelGap * 2.5f, 0.03f);
        _heightTitleY = geometry.Height + _titleOut * 0.6f;
        if (_columnTitle != null)
        {
            _columnTitle.alignment = TextAlignmentOptions.Center;
            _columnTitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    public void SetShown(bool on)
    {
        if (_shown == on) return;
        _shown = on;
        if (on) _placed = false;
        ApplyAll();
    }

    public void SetAppearance(float sizeScale, Color color)
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            _pool[i].transform.localScale = Vector3.one * (TextScale * sizeScale);
            _pool[i].color = new Color(color.r, color.g, color.b, _pool[i].alpha);
        }
        _sizeScale = sizeScale;
        _color = color;
    }

    public void SetParts(bool tickers, bool titles)
    {
        _tickersOn = tickers;
        _titlesOn = titles;
        ApplyAll();
    }

    private void ApplyAll()
    {
        ApplyEnabled(_rows);
        ApplyEnabled(_columns);
        ApplyEnabled(_heights);
        ApplyEnabled(_free);
        if (_rowTitle != null) _rowTitle.renderer.enabled = _shown && _titlesOn;
        if (_columnTitle != null) _columnTitle.renderer.enabled = _shown && _titlesOn;
        if (_heightTitle != null) _heightTitle.renderer.enabled = _shown && _titlesOn;
    }

    private void ApplyEnabled(List<TextMeshPro> labels)
    {
        for (int i = 0; i < labels.Count; i++) labels[i].renderer.enabled = _shown && _tickersOn;
    }

    public void FaceViewer(float deltaTime)
    {
        if (!_shown || _rows.Count + _columns.Count + _heights.Count + _free.Count == 0) return;
        Transform cam = CameraRig.MainTransform;
        if (cam == null) return;

        Vector3 viewer = _owner.InverseTransformPoint(cam.position);
        if (!_placed)
        {
            _placed = true;
            _viewerHighX = viewer.x > 0f;
            _viewerHighZ = viewer.z > 0f;
            ApplySides();
            ApplyAlpha(1f);
            return;
        }

        if (!_flipping)
        {
            _wantHighX = _viewerHighX ? viewer.x > -_margin : viewer.x > _margin;
            _wantHighZ = _viewerHighZ ? viewer.z > -_margin : viewer.z > _margin;
            _flipping = _wantHighX != _viewerHighX || _wantHighZ != _viewerHighZ;
        }

        if (_flipping)
        {
            _fade -= (Style.Animate ? deltaTime / FadeSeconds : 2f);
            if (_fade <= 0f)
            {
                _fade = 0f;
                _flipping = false;
                _viewerHighX = _wantHighX;
                _viewerHighZ = _wantHighZ;
                ApplySides();
            }
            ApplyAlpha(_fade);
        }
        else if (_fade < 1f)
        {
            _fade = Mathf.Min(1f, _fade + (Style.Animate ? deltaTime / FadeSeconds : 2f));
            ApplyAlpha(_fade);
        }
    }

    private void ApplySides()
    {
        Quaternion facing = Quaternion.Euler(0f, _viewerHighZ ? 180f : 0f, 0f) * Quaternion.Euler(UpwardTiltDegrees, 0f, 0f);

        float rowX = _viewerHighX ? _highX : _lowX;
        bool readsAway = _viewerHighX != _viewerHighZ;
        for (int i = 0; i < _rows.Count; i++)
        {
            TextMeshPro label = _rows[i];
            Vector3 p = label.transform.localPosition;
            p.x = rowX;
            label.transform.SetLocalPositionAndRotation(p, facing);
            label.alignment = readsAway ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            label.rectTransform.pivot = new Vector2(readsAway ? 0f : 1f, 0.5f);
        }

        float columnZ = _viewerHighZ ? _highZ : _lowZ;
        for (int i = 0; i < _columns.Count; i++)
        {
            TextMeshPro label = _columns[i];
            Vector3 p = label.transform.localPosition;
            p.z = columnZ;
            label.transform.SetLocalPositionAndRotation(p, facing);
        }

        float farZ = _viewerHighZ ? _lowZ : _highZ;
        void PlaceOutward(TextMeshPro label, Vector3 position)
        {
            label.transform.SetLocalPositionAndRotation(position, facing);
            label.alignment = readsAway ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            label.rectTransform.pivot = new Vector2(readsAway ? 0f : 1f, 0.5f);
        }

        for (int i = 0; i < _heights.Count; i++)
            PlaceOutward(_heights[i], new Vector3(rowX, _heights[i].transform.localPosition.y, farZ));
        if (_heightTitle != null) PlaceOutward(_heightTitle, new Vector3(rowX, _heightTitleY, farZ));
        if (_rowTitle != null)
            PlaceOutward(_rowTitle, new Vector3(rowX, _y, farZ + (_viewerHighZ ? -_titleOut : _titleOut)));
        if (_columnTitle != null)
            _columnTitle.transform.SetLocalPositionAndRotation(
                new Vector3(0f, _y, columnZ + (_viewerHighZ ? _titleOut : -_titleOut)), facing);
        for (int i = 0; i < _free.Count; i++)
            _free[i].transform.localRotation = facing;
    }

    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < _rows.Count; i++) _rows[i].alpha = alpha;
        for (int i = 0; i < _columns.Count; i++) _columns[i].alpha = alpha;
        for (int i = 0; i < _heights.Count; i++) _heights[i].alpha = alpha;
        for (int i = 0; i < _free.Count; i++) _free[i].alpha = alpha;
        if (_rowTitle != null) _rowTitle.alpha = alpha;
        if (_columnTitle != null) _columnTitle.alpha = alpha;
        if (_heightTitle != null) _heightTitle.alpha = alpha;
    }

    private TextMeshPro Write(int index, string text)
    {
        TextMeshPro label = Acquire(index);
        label.text = text;
        label.renderer.enabled = _shown;
        return label;
    }

    private TextMeshPro Acquire(int index)
    {
        if (_root == null)
        {
            _root = new GameObject("Labels").transform;
            _root.SetParent(_owner, false);
        }

        while (_pool.Count <= index)
        {
            var go = new GameObject($"Label_{_pool.Count}");
            go.transform.SetParent(_root, false);
            TextMeshPro label = go.AddComponent<TextMeshPro>();
            Style.ApplyWorldBody(label);
            label.richText = false;
            label.color = _color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.rectTransform.sizeDelta = LabelBox;
            label.transform.localScale = Vector3.one * (TextScale * _sizeScale);
            _pool.Add(label);
        }
        return _pool[index];
    }
}
