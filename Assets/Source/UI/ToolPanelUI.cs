using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolPanelUI : ModularPanel
{
    private const float StepImageHeight = 170f;
    private const float ConnectingPulseSeconds = 0.4f;

    public static readonly string[] AssistantSpeedLabels = { "0.5 m/s", "1 m/s", "Instant" };

    public static readonly ToolType[] Tools =
    {
        ToolType.Move, ToolType.Rotate, ToolType.Scale,
        ToolType.Profile, ToolType.Filter, ToolType.Sort
    };

    private static readonly Dictionary<ToolType, int> StepCounts = new Dictionary<ToolType, int>
    {
        { ToolType.Move, 3 }, { ToolType.Rotate, 3 }, { ToolType.Scale, 3 }
    };

    public ManageTools toolManager;

    private Watch _assistant;
    private ToolType _stepsShown = ToolType.None;
    private AppLanguage _stepsLanguage;
    private Coroutine _connectingPulse;
    private int _connectingDots;

    public static string Label(ToolType tool) => tool.ToString();

    private void Awake()
    {
        BuildPanel("panel.tools", "ToolGrid", hasRight: true);
        if (toolManager == null) toolManager = GetComponent<ManageTools>();

        HideTitleBarButtons();

        foreach (ToolType tool in Tools)
        {
            ToolType captured = tool;
            Rows.Add(new PanelRow
            {
                NameKey = $"tool.{Label(tool)}",
                Value = () => Strings.Get(ManageTools.OnHold(captured) ? "option.onHold"
                    : toolManager != null && toolManager.SelectedTool == captured ? "option.on" : "option.off"),
                Step = _ => { if (toolManager != null) toolManager.SelectTool(captured); },
                Enabled = () => !ManageTools.OnHold(captured)
            });
        }

        Rows.Add(new PanelRow
        {
            NameKey = "assistant",
            Value = AssistantValue,
            Step = _ => ToggleAssistant()
        });
        Rows.Add(PanelRow.Action("undo", () => { if (toolManager != null) toolManager.Undo(); }));
        Rows.Add(PanelRow.Action("undo.all", OnUndoAll));

        if (toolManager != null) toolManager.OnToolChanged += OnToolChanged;
        RefreshPanel();

        if (_canvas != null) StartHidden();
    }

    private void HideTitleBarButtons()
    {
        Transform undoAll = FindTransform.FindDeep(transform, "UndoAll_Btn");
        if (undoAll == null) return;
        Transform target = undoAll.parent != null && undoAll.parent.name.EndsWith("_Border") ? undoAll.parent : undoAll;
        target.gameObject.SetActive(false);
    }

    private Watch Assistant
    {
        get
        {
            if (_assistant == null)
            {
                _assistant = Scene.Assistant;
                if (_assistant != null) _assistant.AssistantActiveChanged += OnAssistantActiveChanged;
            }
            return _assistant;
        }
    }

    private void Start()
    {
        ResolveLayoutNow();
        if (ManageCharts.Instance != null) ManageCharts.Instance.OnChartChanged += OnChartChanged;
        _ = Assistant;
        RefreshPanel();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _connectingPulse = null;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (toolManager != null) toolManager.OnToolChanged -= OnToolChanged;
        if (ManageCharts.Instance != null) ManageCharts.Instance.OnChartChanged -= OnChartChanged;
        if (_assistant != null) _assistant.AssistantActiveChanged -= OnAssistantActiveChanged;
    }

    private void OnChartChanged()
    {
        if (toolManager != null) toolManager.ForgetSuspendedTool();
    }

    private void OnToolChanged(ToolType selected) => RefreshPanel();

    private string AssistantValue()
    {
        Watch assistant = _assistant;
        if (assistant == null || !assistant.IsGeminiActive) return Strings.Get("option.off");
        if (assistant.Status == GeminiStatus.Live) return Strings.Get("option.on");
        return Strings.Get("assistant.connecting") + new string('.', _connectingDots);
    }

    private void ToggleAssistant()
    {
        Watch assistant = Assistant;
        if (assistant == null) return;
        assistant.SetGeminiActive(!assistant.IsGeminiActive, AssistantCause.User);
    }

    private void OnAssistantActiveChanged(bool active, GeminiStatus status)
    {
        bool connecting = active && status != GeminiStatus.Live;
        if (connecting && _connectingPulse == null && isActiveAndEnabled && Style.Animate)
            _connectingPulse = StartCoroutine(ConnectingPulse());
        if (!connecting && _connectingPulse != null)
        {
            StopCoroutine(_connectingPulse);
            _connectingPulse = null;
        }
        RefreshPanel();
    }

    private IEnumerator ConnectingPulse()
    {
        var wait = new WaitForSeconds(ConnectingPulseSeconds);
        while (true)
        {
            _connectingDots = (_connectingDots + 1) % 4;
            Rows.Refresh();
            yield return wait;
        }
    }

    public string AssistantSpeedName => AssistantSpeedLabels[(int)Settings.AssistantSpeed];

    public bool SetAssistantSpeed(string option)
    {
        int index = MatchAssistantSpeed(option);
        if (index < 0) return false;
        Settings.AssistantSpeed = (AssistantSpeed)index;
        StateChannel.RecordState("assistantSpeed", $"the assistant's motion speed is {AssistantSpeedLabels[index]}");
        return true;
    }

    private static string NormalizeSpeed(string s) => s.Trim().ToLowerInvariant().Replace(" ", "");

    private static int MatchAssistantSpeed(string option)
    {
        if (string.IsNullOrEmpty(option)) return -1;
        string wanted = NormalizeSpeed(option);

        for (int i = 0; i < AssistantSpeedLabels.Length; i++)
        {
            string label = NormalizeSpeed(AssistantSpeedLabels[i]);
            if (wanted == label) return i;

            int unit = label.IndexOf("m/s", StringComparison.Ordinal);
            if (unit > 0 && wanted == label.Substring(0, unit)) return i;
        }
        return -1;
    }

    private void OnUndoAll()
    {
        if (toolManager == null) return;
        toolManager.UndoAll();
        toolManager.DeselectTool();
        toolManager.ForgetSuspendedTool();
    }

    protected override void RefreshPanel()
    {
        Rows.Refresh();

        ToolType selected = toolManager != null ? toolManager.SelectedTool : ToolType.None;
        bool hasTool = selected != ToolType.None;
        bool assistantOn = _assistant != null && _assistant.IsGeminiActive;

        SetRightOpen(hasTool);
        if (hasTool) ShowSteps(selected);

        SetBottomOpen(hasTool || assistantOn);
        if (hasTool)
        {
            Strings.Bind(BottomHeader, $"tool.{Label(selected)}");
            Strings.Bind(BottomText, $"tool.{Label(selected)}.desc");
        }
        else if (assistantOn)
        {
            Strings.Bind(BottomHeader, "assistant");
            Strings.Bind(BottomText, "assistant.desc");
        }

        QueueSettle();
        StateChannel.SetState("optionsPanels", hasTool
            ? "the selected tool's steps and description are open"
            : assistantOn ? "the assistant's description is open" : "no subpanel is open");
    }

    private void ShowSteps(ToolType tool)
    {
        if (RightContent == null) return;
        if (_stepsShown == tool && _stepsLanguage == Settings.Language) return;
        _stepsShown = tool;
        _stepsLanguage = Settings.Language;

        Strings.Bind(RightHeader, "tool.steps");
        UILayout.Clear(RightContent);

        if (!StepCounts.TryGetValue(tool, out int count)) return;
        for (int step = 1; step <= count; step++)
        {
            TextMeshProUGUI text = UILabel.Make(RightContent, $"Step_{step}", Style.Black,
                TextAlignmentOptions.TopLeft, TextOverflowModes.Overflow, wrap: true);
            text.text = $"{step}. {Strings.Get($"tool.{Label(tool)}.step{step}")}";
            AddStepImage(tool, step);
        }
    }

    private void AddStepImage(ToolType tool, int step)
    {
        RectTransform frame = UILayout.EnsureChild(RightContent, $"StepFrame_{step}");
        UILayout.FixedHeight(frame.gameObject, StepImageHeight);
        Image border = UILayout.Ensure<Image>(frame.gameObject);
        RoundedSprite.Apply(border, Style.ChipRadius);
        border.color = Style.Black;
        border.raycastTarget = false;

        RectTransform fill = UILayout.EnsureChild(frame, "Picture");
        UILayout.Stretch(fill, Style.SmallBorder);
        Image picture = UILayout.Ensure<Image>(fill.gameObject);
        picture.raycastTarget = false;
        picture.preserveAspect = true;
        Sprite sprite = Resources.Load<Sprite>($"ToolSteps/{Label(tool)}_{step}");
        picture.sprite = sprite;
        picture.color = Style.White;
        if (sprite == null) RoundedSprite.Apply(picture, Style.InnerRadius(Style.ChipRadius));
    }

    public override void ShowPanel()
    {
        if (_canvas == null) return;
        ShowCanvas();
        PanelGuard.ClearToolPanel();
        StateChannel.RecordState("toolPanel", "the tool panel is open, so the tool rows are on screen");

        Watch assistant = Assistant;
        if (assistant != null) assistant.NotifyIntent();

        if (toolManager != null) toolManager.ResumeTool();
        RefreshPanel();
    }

    public override void HidePanel()
    {
        if (_canvas == null) return;
        if (IsVisible && toolManager != null) toolManager.SuspendTool();
        HideCanvas();
        if (StateChannel.UserDriven) PanelGuard.MarkToolPanelClosedByUser();
        StateChannel.RecordState("toolPanel", "the tool panel is closed, so the tool rows are off screen");

        Watch assistant = Assistant;
        if (assistant != null) assistant.NotifyIntentEnded();
    }
}
