using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : PanelUI
{
    private static readonly Setting[] GridTiles =
    {
        Setting.Animations, Setting.SoundEffects, Setting.Handedness,
        Setting.TextSize, Setting.ColorMode, Setting.Language,
    };

    private const Setting WideTile = Setting.ExplanationLevel;
    private const int Columns = 3;
    private const int Rows = 3;

    private class Tile
    {
        public Setting Setting;
        public UIButton.Handle Handle;
        public RectTransform Rect;
    }

    private readonly List<Tile> _tiles = new List<Tile>();
    private Tile _wide;
    private GridLayoutGroup _grid;
    private RectTransform _panelRootRect;
    private RectTransform _primaryRect;
    private OptionList _options;

    private RectTransform _cardRect;
    private RectTransform _keyPane;
    private RectTransform _legendRect;
    private bool _keyOpen;

    private Coroutine _settle;

    private void Awake()
    {
        InitPanel(Style.Panel);

        _primaryRect = FindTransform.FindDeep(transform, "PrimaryPanel") as RectTransform;
        _panelRootRect = _primaryRect != null ? FindTransform.FindDeep(_primaryRect, "PanelRoot") as RectTransform : null;
        _options = new OptionList(CanvasRect);

        Transform title = FindTransform.FindDeep(transform, "Title");
        if (title != null) Strings.Bind(title.GetComponent<TMP_Text>(), "panel.settings");

        BuildTiles();
        BuildKeyCard();

        SetGrabRects(_primaryRect, _cardRect);

        Settings.Changed += OnSettingChanged;
        Strings.Changed += RefreshTiles;
        RefreshTiles();

        if (_canvas != null) StartHidden();
    }

    private void Start()
    {
        ResolveLayoutNow();
        if (ManageDatasets.Instance != null) ManageDatasets.Instance.OnActiveDatasetChanged += OnSheetChanged;
    }

    private void OnDestroy()
    {
        Settings.Changed -= OnSettingChanged;
        Strings.Changed -= RefreshTiles;
        if (ManageDatasets.Instance != null) ManageDatasets.Instance.OnActiveDatasetChanged -= OnSheetChanged;
    }

    private void OnDisable()
    {
        _settle = null;
    }

    protected override void ResolveDeferredLayout()
    {
        FitTiles();
        LayoutCard();
    }

    public override void ShowPanel()
    {
        if (_canvas == null) return;
        ShowCanvas();
        QueueSettle();
        StateChannel.RecordState("settingsPanel", "the settings panel is open");
        InvalidateGrabBounds();
    }

    public override void HidePanel()
    {
        if (_canvas == null) return;
        _options.Close();
        SetKeyOpen(false);
        HideCanvas();
        StateChannel.RecordState("settingsPanel", "the settings panel is closed");
    }

    private void BuildTiles()
    {
        Transform gridT = FindTransform.FindDeep(transform, "SettingsGrid");
        if (gridT == null)
        {
            Debug.LogError("[SettingsPanelUI] No 'SettingsGrid' under the panel; it has no settings to show.");
            return;
        }
        UILayout.Clear(gridT);

        _grid = UILayout.Ensure<GridLayoutGroup>(gridT.gameObject);
        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = Columns;
        _grid.childAlignment = TextAnchor.UpperCenter;
        _grid.padding = PanePadding();
        _grid.spacing = new Vector2(Style.SmallPadding, Style.SmallPadding);

        LayoutElement le = UILayout.Ensure<LayoutElement>(gridT.gameObject);
        le.minHeight = 0f;
        le.preferredHeight = 0f;
        le.flexibleHeight = 1f;

        foreach (Setting setting in GridTiles) _tiles.Add(MakeTile(gridT, setting));

        _wide = MakeTile(gridT, WideTile);
        UILayout.Ensure<LayoutElement>(_wide.Handle.Root).ignoreLayout = true;
        _wide.Rect.anchorMin = _wide.Rect.anchorMax = new Vector2(0f, 1f);
        _wide.Rect.pivot = new Vector2(0f, 1f);
    }

    private Tile MakeTile(Transform parent, Setting setting)
    {
        UIButton.Handle h = UIButton.Create(parent, $"Setting_{setting}", "");
        Style.ApplyTitle(h.Text);
        h.Text.textWrappingMode = TextWrappingModes.Normal;
        h.Text.overflowMode = TextOverflowModes.Overflow;

        var tile = new Tile { Setting = setting, Handle = h, Rect = h.Root.GetComponent<RectTransform>() };
        h.Button.onClick.AddListener(() => OnTilePressed(tile));
        return tile;
    }

    private IEnumerable<Tile> AllTiles()
    {
        foreach (Tile t in _tiles) yield return t;
        if (_wide != null) yield return _wide;
    }

    private void RefreshTiles()
    {
        foreach (Tile tile in AllTiles())
        {
            tile.Handle.Text.text =
                $"{Strings.Get("setting." + tile.Setting)}\n<size=75%>{Strings.Get(OptionKeys(tile.Setting)[Current(tile.Setting)])}</size>";
            UIButton.SetSelected(tile.Handle, Selected(tile));
        }
        RefreshKey();
    }

    private bool Selected(Tile tile)
    {
        if (IsSwitch(tile.Setting)) return Current(tile.Setting) == 0;
        if (_options.IsOpen && _options.Owner == tile.Rect) return true;
        return tile.Setting == Setting.ColorMode && _keyOpen;
    }

    private void OnTilePressed(Tile tile)
    {
        if (tile.Setting != Setting.ColorMode) SetKeyOpen(false);

        if (IsSwitch(tile.Setting))
        {
            _options.Close();
            Apply(tile.Setting, Current(tile.Setting) == 0 ? 1 : 0);
            return;
        }

        if (_options.IsOpen && _options.Owner == tile.Rect)
        {
            _options.Close();
            return;
        }

        string[] keys = OptionKeys(tile.Setting);
        var labels = new List<string>(keys.Length);
        foreach (string key in keys) labels.Add(Strings.Get(key));

        _options.Open(tile.Rect, labels, Current(tile.Setting),
            picked =>
            {
                Apply(tile.Setting, picked);
                if (tile.Setting == Setting.ColorMode) SetKeyOpen(true);
            },
            RefreshTiles);
        RefreshTiles();
    }

    private static bool IsSwitch(Setting s) => s == Setting.Animations || s == Setting.SoundEffects;

    private static string[] OptionKeys(Setting s)
    {
        switch (s)
        {
            case Setting.Animations:
            case Setting.SoundEffects: return new[] { "option.on", "option.off" };
            case Setting.Handedness: return new[] { "option.Left", "option.Right" };
            case Setting.TextSize: return new[] { "option.Small", "option.Medium", "option.Large" };
            case Setting.ColorMode: return new[] { "option.Standard", "option.RedGreen", "option.BlueYellow" };
            case Setting.Language: return new[] { "option.English", "option.Spanish" };
            default: return new[] { "option.Simple", "option.Standard", "option.Expert" };
        }
    }

    private static int Current(Setting s)
    {
        switch (s)
        {
            case Setting.Animations: return Settings.Animations ? 0 : 1;
            case Setting.SoundEffects: return Settings.SoundEffects ? 0 : 1;
            case Setting.Handedness: return (int)Settings.Handedness;
            case Setting.TextSize: return (int)Settings.TextSize;
            case Setting.ColorMode: return (int)Settings.ColorMode;
            case Setting.Language: return (int)Settings.Language;
            default: return (int)Settings.ExplanationLevel;
        }
    }

    private static void Apply(Setting s, int option)
    {
        switch (s)
        {
            case Setting.Animations: Settings.Animations = option == 0; break;
            case Setting.SoundEffects: Settings.SoundEffects = option == 0; break;
            case Setting.Handedness: Settings.Handedness = (WristHand)option; break;
            case Setting.TextSize: Settings.TextSize = (TextSize)option; break;
            case Setting.ColorMode: Settings.ColorMode = (ColorMode)option; break;
            case Setting.Language: Settings.Language = (AppLanguage)option; break;
            default: Settings.ExplanationLevel = (ExplanationLevel)option; break;
        }
    }

    private void OnSettingChanged(Setting which) => RefreshTiles();

    private void QueueSettle()
    {
        FitTiles();
        LayoutCard();
        if (!isActiveAndEnabled) return;
        if (_settle != null) StopCoroutine(_settle);
        _settle = StartCoroutine(SettleLayout());
    }

    private IEnumerator SettleLayout()
    {
        yield return UILayout.Converge(() => IsVisible, () => { FitTiles(); LayoutCard(); },
            () => _grid != null ? _grid.cellSize.x + (_cardRect != null ? _cardRect.sizeDelta.y : 0f) : -1f);
        _settle = null;
    }

    private void FitTiles()
    {
        if (_grid == null || _panelRootRect == null) return;
        RectTransform gridRect = _grid.transform as RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRootRect);

        float byWidth = (gridRect.rect.width - _grid.padding.left - _grid.padding.right
            - (Columns - 1) * _grid.spacing.x) / Columns;
        float byHeight = (gridRect.rect.height - _grid.padding.top - _grid.padding.bottom
            - (Rows - 1) * _grid.spacing.y) / Rows;
        float cell = Mathf.Max(1f, Mathf.Min(byWidth, byHeight));
        if (!Mathf.Approximately(cell, _grid.cellSize.x)) _grid.cellSize = new Vector2(cell, cell);

        float radius = Style.TileRadius(cell);
        foreach (Tile tile in AllTiles()) UIButton.SetRadius(tile.Handle, radius);

        if (_wide == null) return;
        float rowsWidth = Columns * cell + (Columns - 1) * _grid.spacing.x;
        float inset = Mathf.Max(0f, (gridRect.rect.width - _grid.padding.left - _grid.padding.right - rowsWidth) * 0.5f);
        _wide.Rect.sizeDelta = new Vector2(rowsWidth, cell);
        _wide.Rect.anchoredPosition = new Vector2(
            _grid.padding.left + inset,
            -(_grid.padding.top + (Rows - 1) * (cell + _grid.spacing.y)));
    }

    private void BuildKeyCard()
    {
        Transform card = FindTransform.FindDeep(transform, "OptionsPanel");
        Transform contentArea = card != null ? FindTransform.FindDeep(card, "ContentArea") : null;
        if (card == null || contentArea == null)
        {
            Debug.LogError("[SettingsPanelUI] No 'OptionsPanel' with a 'ContentArea'; the Color Mode key cannot open.");
            return;
        }

        _cardRect = card as RectTransform;
        UILayout.Clear(contentArea);

        var pane = new GameObject("ColorKeyContent", typeof(RectTransform));
        pane.transform.SetParent(contentArea, false);
        _keyPane = pane.GetComponent<RectTransform>();
        UILayout.Stretch(_keyPane);

        VerticalLayoutGroup layout = pane.AddComponent<VerticalLayoutGroup>();
        ApplyPaneLayout(layout);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI header = UILabel.Make(pane.transform, "Header", Style.Black,
            TextAlignmentOptions.Left, TextOverflowModes.Ellipsis, bold: true);
        Strings.Bind(header, "setting.ColorMode");
        UILayout.FixedHeight(header.gameObject, Style.HeaderHeight);

        TextMeshProUGUI description = UILabel.Make(pane.transform, "Description", Style.Black,
            TextAlignmentOptions.TopLeft, TextOverflowModes.Overflow, wrap: true);
        Strings.Bind(description, "colorkey.desc");

        _legendRect = new GameObject("Legend", typeof(RectTransform)).GetComponent<RectTransform>();
        _legendRect.SetParent(pane.transform, false);
        VerticalLayoutGroup legendLayout = _legendRect.gameObject.AddComponent<VerticalLayoutGroup>();
        legendLayout.childControlWidth = true;
        legendLayout.childControlHeight = true;
        legendLayout.childForceExpandWidth = true;
        legendLayout.childForceExpandHeight = false;

        ApplyDividers(card);
        PanelUI.ApplyTitleBars(card);
        UILayout.ShowPart(card.gameObject, false);
    }

    private void SetKeyOpen(bool open)
    {
        if (_cardRect == null || _keyOpen == open) return;
        _keyOpen = open;
        UILayout.ShowPart(_cardRect.gameObject, open);
        if (open) RefreshKey();
        LayoutCard();
        QueueSettle();
        RefreshTiles();
    }

    private void OnSheetChanged(int index) => RefreshKey();

    private void RefreshKey()
    {
        if (!_keyOpen || _legendRect == null) return;
        ColorLegend.For(Scene.Data).Build(_legendRect);
        QueueSettle();
    }

    private void LayoutCard()
    {
        RectTransform canvas = CanvasRect;
        if (canvas == null) return;

        float height = Style.Panel.y;
        if (_keyOpen && _cardRect != null && _keyPane != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvas);
            if (UIMeasure.TryPreferredHeight(_cardRect, _keyPane, out float content))
                _cardRect.sizeDelta = new Vector2(_cardRect.sizeDelta.x, content + Style.SmallPadding);

            Vector2 pos = _cardRect.anchoredPosition;
            pos.y = -(Style.Panel.y + Style.SmallPadding);
            _cardRect.anchoredPosition = pos;
            height += Style.SmallPadding + _cardRect.sizeDelta.y;
        }

        canvas.sizeDelta = new Vector2(Style.Panel.x, height);
        InvalidateGrabBounds();
    }
}
