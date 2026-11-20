using System.Collections.Generic;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.View;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public sealed class DataTooltip
    {

        private readonly RectTransform _root;
        private readonly Canvas _canvas;
        private readonly PanelCard _card;

        public DataTooltip(Transform parent)
        {
            _root = LayoutTools.EnsureChild(parent, "Data Tooltip");
            _root.pivot = new Vector2(0f, 1f);
            _canvas = LayoutTools.Ensure<Canvas>(_root.gameObject);
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = Style.SortNotices;
            _card = new PanelCard(_root, "Card", Style.DataTooltipWidth);
            _card.Root.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            TranslationSurface.Add(_card.Root, 0f, Style.LargeRadius);
            _card.Resized += OnCardResized;
            _canvas.enabled = false;
        }

        private ChartCard _placedOn;
        private MarkInfo _placedMark;
        private float _placedButton;

        private void OnCardResized()
        {
            _root.sizeDelta = new Vector2(_card.Width, _card.Height);
            if (_placedOn != null) Place(_placedOn, _placedMark, _placedButton);
        }

        public void Show(MarkInfo detail, IReadOnlyList<string> lines, string waitingKey, ChartCard beside)
        {
            List<KeyValuePair<string, string>> rows = detail.Rows;
            float width = Mathf.Min(WidthFor(rows), MostWidth(beside));
            _card.Begin(width);
            for (int i = 0; i < rows.Count; i++)
            {
                string name = rows[i].Key, value = rows[i].Value;
                _card.Pair(() => Numbers.Text(name), () => Numbers.Text(value), false, wrap: false);
                if (lines != null && i < lines.Count && !string.IsNullOrEmpty(lines[i]))
                {
                    string line = lines[i];
                    _card.Ai(() => Numbers.Text(line), PanelCard.AiGap);
                }
            }
            if (!string.IsNullOrEmpty(waitingKey)) _card.CountingLine(() => Strings.Get(waitingKey));
            _card.End();
            _root.sizeDelta = new Vector2(width, _card.Height);
            DesignPlace(_card.Root);
            _canvas.enabled = true;
        }

        private static float WidthFor(List<KeyValuePair<string, string>> rows)
        {
            float half = Style.DataTooltipWidth * 0.5f;
            foreach (var row in rows)
            {
                float name = Mathf.Ceil(Charts.TwoD.Elements.ChartText.Width(Numbers.Text(row.Key), Style.MediumText));
                float value = Mathf.Ceil(Charts.TwoD.Elements.ChartText.Width(Numbers.Text(row.Value), Style.MediumText));
                half = Mathf.Max(half, name + PanelCard.Inset + Style.MediumPad, value + PanelCard.Inset);
            }
            return half * 2f;
        }

        private float MostWidth(ChartCard card)
        {
            if (card == null) return Charts.ChartStyle.CardWidth;
            Transform space = card.transform.parent != null ? card.transform.parent : card.transform;
            float units = Mathf.Max(1e-6f, space.lossyScale.x);
            return Mathf.Max(Style.DataTooltipWidth, card.Width * card.transform.lossyScale.x / units);
        }

        private static void DesignPlace(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
        }

        public void Place(ChartCard card, MarkInfo mark, float buttonSize)
        {
            if (card == null || mark == null) return;
            _placedOn = card;
            _placedMark = mark;
            _placedButton = buttonSize;
            Transform space = card.transform.parent != null ? card.transform.parent : card.transform;
            float units = space.lossyScale.x;
            float design = card.transform.lossyScale.x;
            float beside = (Style.SmallPad + buttonSize + Style.SmallPad) * units;
            float cardHeight = card.Height * design, tipHeight = _card.Height * units;
            float top = Mathf.Clamp(MarkTop(mark) * design, 0f, cardHeight);
            if (top + tipHeight > cardHeight) top = Mathf.Max(0f, cardHeight - tipHeight);
            Vector3 right = card.transform.right, up = card.transform.up, forward = card.transform.forward;
            Vector3 corner = card.TopRight + right * beside - up * top - forward * Style.Layer;
            _root.SetPositionAndRotation(corner, card.transform.rotation);
            _root.localScale = Vector3.one * units / Mathf.Max(1e-6f, _root.parent != null ? _root.parent.lossyScale.x : 1f);
        }

        private static float MarkTop(MarkInfo mark) =>
            mark.Area == HitShape.Box ? mark.Box.yMin : mark.Centre.y - mark.Outer;

        public void Hide() => _canvas.enabled = false;
    }
}
