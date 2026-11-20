using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.Data;
using AccountingImmersiveAnalytics.Documents;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.View;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using OneText;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public sealed class DocumentColumn
    {
        private const float Radius = Style.LargeRadius * ChartStyle.PreviewScale;
        private static float PanelUnitsPerPoint => Style.Panel.x / (float)DocumentEntry.LetterWidth;
        private static float UnitsPerPoint => PanelUnitsPerPoint * ChartStyle.PreviewScale;

        public static float WidthFor(IReadOnlyList<DocumentEntry> entries)
        {
            double widest = DocumentEntry.LetterWidth;
            if (entries.Count > 0)
            {
                widest = 0;
                foreach (DocumentEntry entry in entries) widest = Math.Max(widest, entry.PageWidth);
            }
            return (float)widest * UnitsPerPoint;
        }

        private sealed class Slot
        {
            public DocumentEntry Entry;
            public RectTransform Root;
            public Image Card;
            public float Width;
            public RawImage Picture;
            public Texture2D Texture;
            public Image Outline;
        }

        private readonly RectTransform _root;
        private readonly RectTransform _content;
        private readonly List<Slot> _slots = new List<Slot>();
        private bool _disposed;

        public DocumentColumn(RectTransform root, IReadOnlyList<DocumentEntry> entries, Action<DocumentEntry> press)
        {
            _root = root;
            ScrollRect scroll = GlideScroll.Column(root, Style.SmallPad);
            _content = scroll.content;
            ListMotion.Attach(scroll, "documents", "document", false, false, Items, i => $"\"{_slots[i].Entry.Title}\"");

            for (int i = 0; i < entries.Count; i++) _slots.Add(Add(i, entries[i], press));
            DrawPages();
        }

        private List<RectTransform> Items()
        {
            var items = new List<RectTransform>();
            foreach (Slot slot in _slots)
                if (slot.Root != null) items.Add(slot.Root);
            return items;
        }

        private Slot Add(int index, DocumentEntry entry, Action<DocumentEntry> press)
        {
            float width = (float)entry.PageWidth * UnitsPerPoint, height = (float)entry.PageHeight * UnitsPerPoint;
            RectTransform root = LayoutTools.EnsureChild(_content, $"Document {index + 1}");
            root.gameObject.layer = _root.gameObject.layer;
            LayoutElement size = LayoutTools.Ensure<LayoutElement>(root.gameObject);
            size.preferredHeight = size.minHeight = height;

            RectTransform page = PinRight(LayoutTools.EnsureChild(root, "Page"), width, height);
            page.gameObject.layer = root.gameObject.layer;
            Image card = LayoutTools.Ensure<Image>(page.gameObject);
            RoundedSprite.Apply(card, Radius);
            card.color = Style.Card;
            Button button = LayoutTools.Ensure<Button>(page.gameObject);
            button.transition = Selectable.Transition.None;
            button.targetGraphic = card;
            button.onClick.AddListener(() => press(entry));

            RectTransform paper = LayoutTools.EnsureChild(page, "Paper");
            LayoutTools.Stretch(paper);
            RawImage picture = LayoutTools.Ensure<RawImage>(paper.gameObject);
            picture.color = Style.Card;
            picture.uvRect = new Rect(0f, 1f, 1f, -1f);
            picture.raycastTarget = false;
            if (Panel.FrontMaterial != null) picture.material = Panel.FrontMaterial;
            picture.enabled = false;

            if (!entry.Showable) AddMessage(page);
            BackFace.Add(page, Radius);

            Image outline = CardOutline.Make(root);
            RectTransform ring = (RectTransform)outline.transform;
            ring.anchorMin = ring.anchorMax = Vector2.one;
            ring.pivot = new Vector2(0f, 1f);
            ring.anchoredPosition = new Vector2(-width, 0f);
            CardOutline.Draw(outline, width, height, Radius);
            outline.enabled = false;
            ring.SetAsLastSibling();

            return new Slot { Entry = entry, Root = root, Card = card, Width = width, Picture = picture, Outline = outline };
        }

        private static RectTransform PinRight(RectTransform rt, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        private void AddMessage(RectTransform page)
        {
            OneTextLabel label = LabelFactory.Make(page, "Message", Style.Ink, LabelAnchor.Center, TextOverflow.Overflow, bold: true, wrap: true);
            label.raycastTarget = false;
            LayoutTools.Stretch(label.rectTransform, Style.LargePad);
            Texts.Bind(label, () => Strings.Get("documents.display"));
        }

        public void SetOpen(DocumentEntry open)
        {
            foreach (Slot slot in _slots)
                if (slot.Outline != null) slot.Outline.enabled = open != null && slot.Entry == open;
        }

        private async void DrawPages()
        {
            if (!PdfPages.Supported) return;
            foreach (Slot slot in _slots.ToArray())
            {
                if (!slot.Entry.Showable) continue;
                PdfPages pages = null;
                try
                {
                    byte[] bytes = await StreamingFiles.ReadBytes(slot.Entry.File);
                    if (_disposed) return;
                    pages = await PdfPages.Open(bytes);
                    if (_disposed || pages == null) continue;
                    Vector2Int pixels = Pixels(slot.Entry);
                    PagePixels drawn = await pages.Render(0, pixels.x, pixels.y);
                    if (_disposed || slot.Picture == null)
                    {
                        PdfPages.Discard(drawn);
                        return;
                    }
                    if (drawn == null) continue;
                    slot.Texture = PdfPages.MakeTexture(drawn, Radius * drawn.Width / Mathf.Max(1f, slot.Width));
                    slot.Picture.texture = slot.Texture;
                    slot.Picture.enabled = true;
                    slot.Card.color = Style.Faded(Style.Card, 0f);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DocumentColumn] The first page of \"{slot.Entry.Title}\" could not be drawn: {e.Message}");
                }
                finally
                {
                    pages?.Dispose();
                }
            }
        }

        private Vector2Int Pixels(DocumentEntry entry)
        {
            Vector2Int full = PdfPages.PixelsFor(entry.PageWidth, entry.PageHeight);
            float share = ChartStyle.PreviewScale;
            return new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(full.x * share)), Mathf.Max(1, Mathf.RoundToInt(full.y * share)));
        }

        public void Dispose()
        {
            _disposed = true;
            foreach (Slot slot in _slots)
            {
                if (slot.Texture != null) UnityEngine.Object.Destroy(slot.Texture);
                slot.Texture = null;
            }
        }
    }
}
