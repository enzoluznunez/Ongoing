using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Elements;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.View;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using AccountingImmersiveAnalytics.UI.Panels;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Charts.View
{
    public sealed class PinnedChart
    {
        public static int CanvasVersion { get; private set; }

        private static readonly (ToolType Tool, string Icon)[] Tools =
        {
            (ToolType.Sort, "Sort"), (ToolType.Filter, "Filter"), (ToolType.Trendline, "Trendline"),
            (ToolType.Key, "Key"), (ToolType.Edits, "Edits"), (ToolType.Place, "Place"), (ToolType.Scale, "Scale")
        };

        private readonly Transform _holder;
        private readonly RectTransform _canvasRect;
        private readonly Canvas _canvas;
        private readonly ChartGrab _grab;
        private readonly RectTransform _column;
        private readonly List<ButtonFactory.Handle> _buttons = new List<ButtonFactory.Handle>();
        private PanelCard _panel;
        private ToolType _open = ToolType.None;
        private ChartManager _charts;
        private ToolManager _tools;

        public ChartCard Card { get; private set; }
        public Transform Holder => _holder;
        public float ButtonSize { get; private set; } = Style.ButtonSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => CanvasVersion = 0;

        public PinnedChart(Transform anchor, int layer, ChartManager charts)
        {
            _charts = charts;
            _holder = new GameObject("Pinned Chart").transform;
            _holder.gameObject.layer = layer;
            _holder.SetParent(anchor, false);

            var go = new GameObject("Pinned Chart Canvas", typeof(RectTransform));
            go.layer = layer;
            go.SetActive(false);
            _canvasRect = (RectTransform)go.transform;
            _canvasRect.SetParent(_holder, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = Style.SortPinned;
            go.AddComponent<GraphicRaycaster>();
            AddPokeInteraction(_canvasRect, layer).InjectAllPointableCanvas(_canvas);
            go.SetActive(true);
            CanvasVersion++;

            _column = DesignRect.Child(_canvasRect, "Tools");
            foreach ((ToolType tool, string icon) in Tools)
            {
                ToolType t = tool;
                ButtonFactory.Handle button = ButtonFactory.Icon(_column, icon, icon, () => Press(t));
                ButtonFactory.SetBorderless(button);
                ButtonFactory.AddBack(button);
                LayoutTools.Ensure<LayoutElement>(button.Root).ignoreLayout = true;
                _buttons.Add(button);
            }

            _grab = ChartGrab.Add(_holder.gameObject);
            _grab.SetExempt(_column);
            EditList.Changed += OnEditsChanged;
            AccountingImmersiveAnalytics.Text.Texts.Refreshed += OnLanguageChanged;
        }

        private static PointableCanvas AddPokeInteraction(RectTransform canvas, int layer)
        {
            GameObject prefab = App.AssetLibrary.Current != null ? App.AssetLibrary.Current.PokeCanvas : null;
            if (prefab == null)
            {
                Debug.LogError("[PinnedChart] The asset library has no poke canvas prefab, so the pinned chart only takes the hand ray.");
                return canvas.gameObject.AddComponent<PointableCanvas>();
            }
            GameObject poke = UnityEngine.Object.Instantiate(prefab, canvas, false);
            poke.name = "Poke";
            foreach (Transform t in poke.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            return poke.GetComponent<PointableCanvas>();
        }

        private void OnLanguageChanged()
        {
            if (Card == null || _open == ToolType.None || RangeSlider.AnyHeld) return;
            Card.Redraw();
            RebuildPanel();
            Layout();
        }

        public void Show(ChartEntry entry, Pose pose, float unitsToMetres)
        {
            Clear();
            _holder.gameObject.SetActive(true);
            _holder.SetPositionAndRotation(pose.position, pose.rotation);
            _holder.localScale = Vector3.one;
            _canvasRect.localScale = Vector3.one * (unitsToMetres / Mathf.Max(1e-6f, _holder.lossyScale.x)) * ChartStyle.PinnedScale;
            _canvasRect.localPosition = Vector3.zero;
            Card = ChartCard.Make(_canvasRect, entry, pinned: true);
            Card.Resized += _ => Layout();
            Card.transform.SetAsFirstSibling();

            if (_tools == null)
            {
                _tools = Scene.Tools;
                if (_tools != null) _tools.OnToolChanged += OnToolChanged;
            }
            OnToolChanged(_tools != null ? _tools.SelectedTool : ToolType.None);
        }

        private void Press(ToolType tool)
        {
            if (Card == null || _tools == null) return;
            ToolType selected = _tools.SelectedTool;
            if (IsOn(tool, selected)) _tools.DeselectTool();
            else if (selected == ToolType.None) _tools.SelectTool(tool);
        }

        private static bool IsOn(ToolType button, ToolType selected) =>
            button == selected;

        private void OnToolChanged(ToolType selected)
        {
            if (Card == null) return;
            if (DropUnofferedTool(selected)) return;
            _grab.SetMode(selected);
            _open = ToolManager.IsGrabTool(selected) ? ToolType.None : selected;
            RebuildPanel();
            RefreshButtons(selected);
            Layout();
        }

        private void OnEditsChanged()
        {
            if (Card != null && _open == ToolType.Edits)
            {
                RebuildPanel();
                Layout();
            }
        }

        private bool DropUnofferedTool(ToolType selected)
        {
            if (_tools == null || selected == ToolType.None || ToolManager.Offered(selected, Card.Entry.Spec, Card)) return false;
            _tools.DeselectTool();
            return true;
        }

        public void ShapingChanged()
        {
            if (Card == null) return;
            if (DropUnofferedTool(_tools != null ? _tools.SelectedTool : ToolType.None)) return;
            if (_open != ToolType.None && _open != ToolType.Edits) RebuildPanel();
            RefreshButtons(_tools != null ? _tools.SelectedTool : ToolType.None);
            Layout();
        }

        private void RebuildPanel()
        {
            if (_open == ToolType.None)
            {
                if (_panel != null) _panel.Root.gameObject.SetActive(false);
                return;
            }
            if (_panel == null)
            {
                _panel = new PanelCard(_canvasRect, "Tool Panel", Card.Width);
                _panel.Resized += Layout;
            }
            _panel.Root.gameObject.SetActive(true);
            _panel.Begin(Card.Width);
            ChartToolPanels.Build(_panel, _open, Card.Entry, Card, _charts);
            _panel.End();
        }

        private void RefreshButtons(ToolType selected)
        {
            ChartSpec spec = Card.Entry.Spec;
            bool idle = selected == ToolType.None;
            for (int i = 0; i < Tools.Length; i++)
            {
                ToolType tool = Tools[i].Tool;
                ButtonFactory.Handle button = _buttons[i];
                bool on = IsOn(tool, selected);
                bool offered = ToolManager.Offered(tool, spec, Card);
                bool free = idle || on;
                ButtonFactory.SetSelected(button, on);
                ButtonFactory.SetEnabled(button, offered && free);
            }
        }

        private void Layout()
        {
            if (Card == null) return;
            float w = Card.Width, h = Card.Height;
            float gap = Style.SmallPad;
            ButtonSize = (h - gap * (Tools.Length - 1)) / Tools.Length;
            DesignRect.Place(_column, w + gap, 0f, ButtonSize, h);
            for (int i = 0; i < _buttons.Count; i++)
            {
                ButtonFactory.SetSquare(_buttons[i], ButtonSize);
                DesignRect.Place((RectTransform)_buttons[i].Root.transform, 0f, i * (ButtonSize + gap), ButtonSize, ButtonSize);
            }

            float total = h;
            if (_panel != null && _panel.Root.gameObject.activeSelf)
            {
                if (!Mathf.Approximately(_panel.Width, w)) RebuildPanel();
                DesignRect.Place(_panel.Root, 0f, h + gap, w, _panel.Height);
                total += gap + _panel.Height;
            }
            float width = w + gap + ButtonSize;
            _canvasRect.sizeDelta = new Vector2(width, total);
            _canvasRect.pivot = new Vector2(w * 0.5f / width, 1f - h * 0.5f / total);
            _canvasRect.localPosition = Vector3.zero;
            RectTransform card = (RectTransform)Card.transform;
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0f, 1f);
            DesignRect.Place(card, 0f, 0f, w, h);
            LayoutTools.FitPokeBounds(_canvasRect);
            _grab.FitGrip(card);
        }

        public void Clear()
        {
            if (Card != null) UnityEngine.Object.Destroy(Card.gameObject);
            Card = null;
            if (_panel != null) _panel.Root.gameObject.SetActive(false);
            _open = ToolType.None;
            _grab.SetMode(ToolType.None);
            _holder.gameObject.SetActive(false);
        }

        public void SetShown(bool on)
        {
            if (Card == null) return;
            _holder.gameObject.SetActive(on);
        }

        public void Destroy()
        {
            EditList.Changed -= OnEditsChanged;
            AccountingImmersiveAnalytics.Text.Texts.Refreshed -= OnLanguageChanged;
            if (_tools != null) _tools.OnToolChanged -= OnToolChanged;
            if (_holder != null) UnityEngine.Object.Destroy(_holder.gameObject);
        }
    }
}
