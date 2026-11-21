using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Elements;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.View;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Charts.View
{
    public sealed class ChartColumn
    {
        private const float StatusHeight = Style.ButtonSize * 2f + Style.MediumPad;

        public static float Width => ChartStyle.CardWidth * ChartStyle.PreviewScale;

        private sealed class Slot
        {
            public RectTransform Root;
            public ChartCard Card;
            public Image Outline;
        }

        private readonly RectTransform _root;
        private readonly RectTransform _content;
        private readonly ScrollRect _scroll;
        private readonly List<Slot> _slots = new List<Slot>();
        private RectTransform _status;
        private ChartText _statusText;
        private string _statusMessage;
        private ChartEntry _pinned;

        public ChartColumn(RectTransform root)
        {
            _root = root;
            _scroll = GlideScroll.Column(root, Style.SmallPad);
            _content = _scroll.content;
            ListMotion.Attach(_scroll, "charts", "chart", false, false, Items, i => $"\"{_slots[i].Card.Entry.Title}\"");
            AccountingImmersiveAnalytics.Text.Texts.Refreshed += OnTextsRefreshed;
        }

        private List<RectTransform> Items()
        {
            var items = new List<RectTransform>();
            foreach (Slot slot in _slots)
                if (slot.Root != null && slot.Card != null) items.Add(slot.Root);
            return items;
        }

        private void OnTextsRefreshed()
        {
            if (_root == null)
            {
                AccountingImmersiveAnalytics.Text.Texts.Refreshed -= OnTextsRefreshed;
                return;
            }
            if (!string.IsNullOrEmpty(_statusMessage)) ShowStatus(_statusMessage);
        }

        public void Clear()
        {
            foreach (Slot slot in _slots)
            {
                if (slot.Root == null) continue;
                slot.Root.SetParent(null, false);
                UnityEngine.Object.Destroy(slot.Root.gameObject);
            }
            _slots.Clear();
            ShowStatus(null);
            _scroll.verticalNormalizedPosition = 1f;
        }

        public ChartCard Add(ChartEntry entry, Action<ChartEntry> press)
        {
            RectTransform root = LayoutTools.EnsureChild(_content, $"Slot {entry.Id}");
            root.gameObject.layer = _root.gameObject.layer;
            ChartCard card = ChartCard.Make(root, entry, pinned: false);
            RectTransform rt = (RectTransform)card.transform;
            rt.localScale = Vector3.one * ChartStyle.PreviewScale;
            DesignRect.Place(rt, 0f, 0f, ChartStyle.CardWidth, card.Height);

            Image area = LayoutTools.Ensure<Image>(root.gameObject);
            area.color = Style.Faded(Style.Card, 0f);
            Button button = LayoutTools.Ensure<Button>(root.gameObject);
            button.transition = Selectable.Transition.None;
            button.targetGraphic = area;
            button.onClick.AddListener(() => press(entry));

            Image outline = CardOutline.Make(root);

            var slot = new Slot { Root = root, Card = card, Outline = outline };
            card.Resized += _ => Fit(slot);
            _slots.Add(slot);
            Fit(slot);
            if (_status != null) _status.SetAsLastSibling();
            return card;
        }

        private void Fit(Slot slot)
        {
            float height = slot.Card.Height * ChartStyle.PreviewScale;
            LayoutElement element = LayoutTools.Ensure<LayoutElement>(slot.Root.gameObject);
            element.preferredHeight = element.minHeight = height;
            LayoutRebuilder.MarkLayoutForRebuild(slot.Root.parent as RectTransform);
            DrawOutline(slot);
        }

        public void SetPinned(ChartEntry pinned)
        {
            _pinned = pinned;
            foreach (Slot slot in _slots) DrawOutline(slot);
        }

        private void DrawOutline(Slot slot)
        {
            if (slot.Outline == null || slot.Card == null) return;
            float height = slot.Card.Height * ChartStyle.PreviewScale;
            RectTransform rt = DesignRect.Place((RectTransform)slot.Outline.transform, 0f, 0f, Width, height);
            rt.SetAsLastSibling();
            CardOutline.Draw(slot.Outline, Width, height, ChartStyle.PreviewRadius);
            slot.Outline.enabled = _pinned != null && slot.Card.Entry == _pinned;
        }

        public void ShowStatus(string message)
        {
            _statusMessage = message;
            if (string.IsNullOrEmpty(message))
            {
                if (_status != null)
                {
                    _status.SetParent(null, false);
                    UnityEngine.Object.Destroy(_status.gameObject);
                }
                _status = null;
                _statusText = null;
                return;
            }
            if (_status == null)
            {
                _status = LayoutTools.EnsureChild(_content, "Status");
                _status.gameObject.layer = _root.gameObject.layer;
                LayoutTools.Ensure<LayoutElement>(_status.gameObject).preferredHeight = StatusHeight;
            }
            _status.SetAsLastSibling();
            float height = StatusHeight / ChartStyle.PreviewScale;
            RectTransform inner = DesignRect.Place(DesignRect.Child(_status, "Card"), 0f, 0f, ChartStyle.CardWidth, height);
            inner.localScale = Vector3.one * ChartStyle.PreviewScale;
            MarkSurface back = LayoutTools.Ensure<MarkSurface>(inner.gameObject);
            back.Clear();
            back.Box(new Rect(0f, 0f, ChartStyle.CardWidth, height), Style.Card, Style.LargeRadius);
            back.Commit();
            BackFace.Add(inner, Style.LargeRadius);
            RectTransform words = DesignRect.Place(DesignRect.Child(inner, "Text"), 0f, 0f, ChartStyle.CardWidth, height);
            if (_statusText == null) _statusText = new ChartText(words);
            ChartText text = _statusText;
            text.Begin();
            text.Put(ChartText.Clip(ChartText.Shown(message).TrimEnd('.'), Style.MediumText, ChartStyle.CardWidth - Style.LargePad * 2f), Style.MediumText,
                Style.BoldWeight, ChartStyle.CardWidth * 0.5f, Style.CentredBaseline(height * 0.5f, Style.MediumText), Align.Middle);
            text.End();
        }
    }
}
