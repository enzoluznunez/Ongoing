using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using OneText;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public sealed class PanelCard
    {
        public const float RowHeight = Style.ButtonSize;
        public const float RowGap = Style.MediumPad;
        public const float Inset = Style.LargePad;
        public const float AiGap = Style.SmallPad;
        public const float ChipGap = Style.SmallPad;
        public const float KeyGap = Style.LargePad;
        public const float ListGap = Style.SmallPad;
        public const float ListHeight = RowHeight * 4f + RowGap * 3f;

        public readonly RectTransform Root;
        private float _width;
        private float _y;
        private int _rows;
        private RectTransform _list;
        private float _listY;
        private int _listRows;
        private readonly List<Fit> _fits = new List<Fit>();
        private readonly CardFit _watch;
        private float _built;
        private bool _ended;
        private float _oneLine = -1f;

        private sealed class Fit
        {
            public RectTransform Row;
            public float Top;
            public float Height;
            public readonly List<FitText> Texts = new List<FitText>();
        }

        private sealed class FitText
        {
            public OneTextLabel Label;
            public float Slack;
            public float Height;
        }

        public event Action Resized;

        public PanelCard(Transform parent, string name, float width)
        {
            Root = LayoutTools.EnsureChild(parent, name);
            Root.gameObject.layer = parent.gameObject.layer;
            Root.anchorMin = Root.anchorMax = new Vector2(0f, 1f);
            Root.pivot = new Vector2(0f, 1f);
            Root.localScale = Vector3.one;
            Root.localRotation = Quaternion.identity;
            Image fill = LayoutTools.Ensure<Image>(Root.gameObject);
            RoundedSprite.Apply(fill, Style.LargeRadius);
            fill.color = Style.Card;
            fill.raycastTarget = true;
            BackFace.Add(Root, Style.LargeRadius);
            _watch = LayoutTools.Ensure<CardFit>(Root.gameObject);
            _watch.Refit = () => Refit(true);
            _width = width;
            Begin(width);
        }

        public float Width => _width;
        public float Half => _width * 0.5f;
        public float ControlWidth => _width * 0.5f - Inset;
        public float InnerWidth => _width - Inset * 2f;
        public float Height { get; private set; }
        public bool Mirrored { get; private set; }

        public void Begin(float width)
        {
            _width = width;
            Mirrored = Text.Language.RightToLeft;
            LayoutTools.Clear(Root);
            _y = Inset;
            _rows = 0;
            _list = null;
            _fits.Clear();
            _ended = false;
            _oneLine = -1f;
        }

        public void BeginList()
        {
            RectTransform viewport = Row(0f);
            _y -= RowGap;
            _list = Place(LayoutTools.EnsureChild(viewport, "Content"), 0f, 0f, InnerWidth, 0f);
            _listY = 0f;
            _listRows = 0;
        }

        public void EndList(string name = null, string itemWord = null, Func<int, string> describe = null)
        {
            if (_list == null) return;
            RectTransform content = _list;
            RectTransform viewport = (RectTransform)content.parent;
            _list = null;
            float total = Mathf.Max(0f, _listY - RowGap);
            float shown = ListHeight;
            content.sizeDelta = new Vector2(InnerWidth, total);
            viewport.sizeDelta = new Vector2(InnerWidth, shown);
            _y += shown + RowGap;
            if (total <= shown) return;

            LayoutTools.Ensure<RectMask2D>(viewport.gameObject);
            Image surface = LayoutTools.Ensure<Image>(viewport.gameObject);
            surface.color = Style.Faded(Style.Card, 0f);
            Panel.ApplyFront(surface);
            GlideScroll scroll = GlideScroll.Attach(viewport, viewport, content);
            if (name == null) return;
            var rows = new List<RectTransform>();
            foreach (Transform child in content) rows.Add((RectTransform)child);
            ListMotion.Attach(scroll, name, itemWord, false, false, () => rows, describe);
        }

        public float End()
        {
            Height = _rows == 0 ? Inset * 2f : _y - RowGap + Inset;
            Root.sizeDelta = new Vector2(_width, Height);
            _built = Height;
            foreach (Fit fit in _fits)
            {
                fit.Top = -fit.Row.anchoredPosition.y;
                fit.Height = fit.Row.sizeDelta.y;
                foreach (FitText text in fit.Texts) text.Height = text.Label.rectTransform.sizeDelta.y;
            }
            _ended = true;
            Refit(false);
            return Height;
        }

        private void Refit(bool notify)
        {
            if (!_ended) return;
            float shift = 0f;
            foreach (Fit fit in _fits)
            {
                if (fit.Row == null) continue;
                float need = fit.Height;
                foreach (FitText text in fit.Texts)
                    if (text.Label != null && text.Label.gameObject.activeSelf)
                        need = Mathf.Max(need, Mathf.Ceil(text.Label.preferredHeight) + text.Slack);
                float grow = need - fit.Height;
                SetTopAndHeight(fit.Row, fit.Top + shift, need);
                foreach (FitText text in fit.Texts)
                    if (text.Label != null) SetTopAndHeight(text.Label.rectTransform, -text.Label.rectTransform.anchoredPosition.y, text.Height + grow);
                shift += grow;
            }
            float height = _built + shift;
            if (Mathf.Approximately(height, Height)) return;
            Height = height;
            Root.sizeDelta = new Vector2(_width, Height);
            if (notify) Resized?.Invoke();
        }

        private static void SetTopAndHeight(RectTransform rt, float top, float height)
        {
            Vector2 at = new Vector2(rt.anchoredPosition.x, -top);
            if (rt.anchoredPosition != at) rt.anchoredPosition = at;
            Vector2 size = new Vector2(rt.sizeDelta.x, height);
            if (rt.sizeDelta != size) rt.sizeDelta = size;
        }

        private void Track(RectTransform row, OneTextLabel label, float slack)
        {
            for (int i = _fits.Count - 1; i >= 0; i--)
            {
                if (_fits[i].Row != row) continue;
                _fits[i].Texts.Add(new FitText { Label = label, Slack = slack });
                label.RegisterDirtyLayoutCallback(_watch.MarkDirty);
                return;
            }
        }

        public RectTransform Row(float height, float gapAbove = RowGap)
        {
            if (_list != null)
            {
                RectTransform item = Place(LayoutTools.EnsureChild(_list, $"Row {_listRows}"), 0f, _listY, InnerWidth, height);
                _listRows++;
                _listY += height + RowGap;
                return item;
            }
            if (_rows > 0) _y += gapAbove - RowGap;
            RectTransform row = Place(LayoutTools.EnsureChild(Root, $"Row {_rows}"), Inset, _y, InnerWidth, height);
            _fits.Add(new Fit { Row = row });
            _rows++;
            _y += height + RowGap;
            return row;
        }

        public static RectTransform Place(RectTransform rt, float x, float y, float width, float height)
        {
            rt.gameObject.layer = rt.parent.gameObject.layer;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public RectTransform PlaceIn(RectTransform rt, float x, float y, float width, float height)
        {
            float room = ((RectTransform)rt.parent).rect.width;
            return Place(rt, Mirrored ? room - x - width : x, y, width, height);
        }

        private LabelAnchor Side(LabelAnchor anchor)
        {
            if (!Mirrored) return anchor;
            switch (anchor)
            {
                case LabelAnchor.Start:
                case LabelAnchor.Left: return LabelAnchor.Right;
                case LabelAnchor.Right: return LabelAnchor.Left;
                default: return anchor;
            }
        }

        public OneTextLabel Label(RectTransform row, Func<string> text, float x, float width, LabelAnchor anchor,
            bool muted = false, bool wrap = false, bool keepSide = false)
        {
            OneTextLabel label = LabelFactory.Make(row, $"Label {row.childCount}", muted ? Style.Muted : Style.Ink,
                keepSide ? anchor : Side(anchor), wrap ? TextOverflow.Overflow : TextOverflow.Ellipsis, wrap: wrap);
            if (keepSide) Place(label.rectTransform, x, 0f, width, row.rect.height);
            else PlaceIn(label.rectTransform, x, 0f, width, row.rect.height);
            Texts.Bind(label, text);
            if (wrap) Track(row, label, RowHeight - OneLine);
            return label;
        }

        public float NameRoom => Half - Inset - Style.SmallPad;
        private float ChipRoom => ControlWidth - Style.MediumPad * 2f;

        public OneTextLabel Name(RectTransform row, Func<string> text) =>
            Label(row, text, 0f, NameRoom, LabelAnchor.Start, wrap: true);

        public float TextHeight(Func<string> text, float width)
        {
            OneTextLabel probe = LabelFactory.Make(Root, "Measure", Style.Ink, LabelAnchor.TopStart, TextOverflow.Overflow, wrap: true);
            probe.rectTransform.sizeDelta = new Vector2(width, 1000f);
            Texts.Bind(probe, text);
            float height = Mathf.Ceil(probe.preferredHeight);
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            return height;
        }

        private float OneLine => _oneLine >= 0f ? _oneLine : _oneLine = TextHeight(() => "A", 1000f);

        public float RowFor(Func<string> text, float width) =>
            text == null ? RowHeight : Mathf.Max(RowHeight, TextHeight(text, width) + RowHeight - OneLine);

        public float NameRow(Func<string> name) => RowFor(name, NameRoom);

        public RectTransform Centred(RectTransform row, float height) =>
            Place(LayoutTools.EnsureChild(row, "Control"), 0f, (row.rect.height - height) * 0.5f, InnerWidth, height);

        public OneTextLabel Line(Func<string> text)
        {
            RectTransform row = Row(RowFor(text, InnerWidth));
            return Label(row, text, 0f, InnerWidth, LabelAnchor.Start, muted: true, wrap: true);
        }

        public OneTextLabel CountingLine(Func<string> word)
        {
            RectTransform row = Row(RowFor(() => word() + new string('.', LoadingDots.Most), InnerWidth));
            OneTextLabel label = Label(row, word, 0f, InnerWidth, LabelAnchor.Start, muted: true, wrap: true);
            LoadingDots.Bind(label, word, counting: true);
            return label;
        }

        public void Pair(Func<string> name, Func<string> value, bool mutedValue, bool wrap = true)
        {
            if (!wrap)
            {
                float nameRoom = Half - Inset - Style.MediumPad;
                RectTransform line = Row(Mathf.Max(RowFor(name, nameRoom), RowFor(value, ControlWidth)));
                Label(line, name, 0f, nameRoom, LabelAnchor.Start, wrap: true);
                Label(line, value, Half - Inset, ControlWidth, LabelAnchor.Right, mutedValue, wrap: true);
                return;
            }
            RectTransform row = Row(NameRow(name));
            Name(row, name);
            Label(Centred(row, RowHeight), value, Half - Inset, ControlWidth, LabelAnchor.Right, mutedValue);
        }

        public ButtonFactory.Handle Wide(Func<string> name, Func<string> label, bool selected, bool dimmed, bool enabled,
            Action press, float gapAbove = RowGap)
        {
            RectTransform row = Row(NameRow(name), gapAbove);
            if (name != null) Name(row, name);
            ButtonFactory.Handle button = ButtonFactory.Create(row, "Button", "");
            PlaceIn((RectTransform)button.Root.transform, Half - Inset, (row.rect.height - RowHeight) * 0.5f, ControlWidth, RowHeight);
            LayoutTools.Ensure<LayoutElement>(button.Root).ignoreLayout = true;
            ButtonFactory.SetSelected(button, selected);
            ButtonFactory.FixText(button);
            Texts.Bind(button.Text, label);
            ButtonFactory.SetEnabled(button, enabled);
            if (dimmed && enabled) LayoutTools.Ensure<CanvasGroup>(button.Root).alpha = Style.DisabledAlpha;
            button.Button.onClick.AddListener(() => press?.Invoke());
            return button;
        }

        public void Chips(Func<string> name, IReadOnlyList<(Func<string> Label, bool Selected, Action Press)> chips,
            string listName = null, Func<int, string> describe = null)
        {
            RectTransform row = Row(NameRow(name));
            Name(row, name);
            var items = new List<(float Width, Action<RectTransform> Build)>();
            foreach (var chip in chips)
            {
                var c = chip;
                float wide = Mathf.Ceil(Charts.TwoD.Elements.ChartText.Width(Charts.TwoD.Elements.ChartText.Clip(c.Label(), Style.MediumText, ChipRoom), Style.MediumText)) + Style.MediumPad * 2f;
                items.Add((wide, holder =>
                {
                    ButtonFactory.Handle button = ButtonFactory.Create(holder, "Chip", "");
                    LayoutTools.Ensure<LayoutElement>(button.Root).ignoreLayout = true;
                    LayoutTools.Stretch((RectTransform)button.Root.transform);
                    ButtonFactory.SetSelected(button, c.Selected);
                    ButtonFactory.FixText(button);
                    Texts.Bind(button.Text, () => Charts.TwoD.Elements.ChartText.Clip(c.Label(), Style.MediumText, ChipRoom));
                    button.Button.onClick.AddListener(() => c.Press?.Invoke());
                }));
            }
            Slide(Centred(row, RowHeight), items, ChipGap, listName, "category", describe);
        }

        public void Slide(RectTransform row, IReadOnlyList<(float Width, Action<RectTransform> Build)> items, float gap,
            string name = null, string itemWord = null, Func<int, string> describe = null)
        {
            float room = ControlWidth, height = row.rect.height;
            float x0 = Mirrored ? 0f : Half - Inset;
            RectTransform viewport = Place(LayoutTools.EnsureChild(row, "Slide"), x0, 0f, room, height);
            LayoutTools.Ensure<RectMask2D>(viewport.gameObject);
            Image surface = LayoutTools.Ensure<Image>(viewport.gameObject);
            surface.color = Style.Faded(Style.Card, 0f);
            Panel.ApplyFront(surface);

            float total = 0f;
            var stops = new List<float>();
            var holders = new List<RectTransform>();
            for (int i = 0; i < items.Count; i++)
            {
                stops.Add(total);
                total += items[i].Width + (i < items.Count - 1 ? gap : 0f);
            }
            float span = Mathf.Max(total, room);
            RectTransform content = Place(LayoutTools.EnsureChild(viewport, "Content"), 0f, 0f, span, height);
            float x = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                float at = Mirrored ? span - x - items[i].Width : x;
                RectTransform holder = Place(LayoutTools.EnsureChild(content, $"Item {i}"), at, 0f, items[i].Width, height);
                holders.Add(holder);
                items[i].Build(holder);
                x += items[i].Width + gap;
            }
            if (total <= room) return;

            GameObject left = Fade(row, "Fade Left", new Rect(x0, 0f, Style.MediumPad, height), true);
            GameObject right = Fade(row, "Fade Right", new Rect(x0 + room - Style.MediumPad, 0f, Style.MediumPad, height), false);
            SnapScroll scroll = LayoutTools.Ensure<SnapScroll>(viewport.gameObject);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.Stops.Clear();
            if (Mirrored)
            {
                foreach (float stop in stops) scroll.Stops.Add(Mathf.Max(0f, total - room - stop));
                content.anchoredPosition = new Vector2(-(total - room), content.anchoredPosition.y);
            }
            else scroll.Stops.AddRange(stops);
            scroll.FadeLeft = left;
            scroll.FadeRight = right;
            (Mirrored ? right : left).SetActive(false);
            if (name != null) ListMotion.Attach(scroll, name, itemWord, true, Mirrored, () => holders, describe);
        }

        private static GameObject Fade(RectTransform row, string name, Rect area, bool fromLeft)
        {
            RectTransform rt = Place(LayoutTools.EnsureChild(row, name), area.x, area.y, area.width, area.height);
            MarkSurface fade = LayoutTools.Ensure<MarkSurface>(rt.gameObject);
            Color solid = Style.Card, clear = Style.Faded(Style.Card, 0f);
            fade.Clear();
            fade.Fade(new Rect(0f, 0f, area.width, area.height), fromLeft ? solid : clear, fromLeft ? clear : solid);
            fade.Commit();
            return rt.gameObject;
        }

        public RangeSlider Range(Func<string> name, double min, double max, double low, double high, bool whole)
        {
            float valuesHeight = Style.InkHeight(Style.MediumText);
            float topHeight = NameRow(name), sliderTop = (topHeight - RowHeight) * 0.5f;
            RectTransform row = Row(Mathf.Max(topHeight, sliderTop + RowHeight + Style.SmallPad + valuesHeight));
            RectTransform top = Place(LayoutTools.EnsureChild(row, "Top"), 0f, 0f, InnerWidth, topHeight);
            Name(top, name);
            RectTransform area = PlaceIn(LayoutTools.EnsureChild(top, "Range"), Half - Inset, sliderTop, ControlWidth, RowHeight);
            RangeSlider slider = RangeSlider.Make(area, min, max, low, high, whole);

            RectTransform values = PlaceIn(LayoutTools.EnsureChild(row, "Values"), Half - Inset, sliderTop + RowHeight + Style.SmallPad, ControlWidth, valuesHeight);
            OneTextLabel lowText = Label(values, () => "", 0f, ControlWidth, LabelAnchor.Left, keepSide: true);
            OneTextLabel highText = Label(values, () => "", 0f, ControlWidth, LabelAnchor.Right, keepSide: true);
            void Show(double a, double b)
            {
                string la = Numbers.Shown(a), lb = Numbers.Shown(b);
                Texts.Bind(lowText, () => la);
                Texts.Bind(highText, () => lb);
            }
            Show(low, high);
            slider.Changed += Show;
            return slider;
        }

        public OneTextLabel Ai(Func<string> text, float gapAbove)
        {
            float boxWidth = InnerWidth;
            float textHeight = TextHeight(text, boxWidth - Style.MediumPad * 2f);

            RectTransform row = Row(textHeight + Style.SmallPad * 2f, gapAbove);
            Image fill = LayoutTools.Ensure<Image>(row.gameObject);
            RoundedSprite.Apply(fill, Style.MediumRadius);
            fill.color = Style.AiFill;
            fill.raycastTarget = false;
            OneTextLabel label = LabelFactory.Make(row, "Text", Style.Ink, LabelAnchor.TopStart, TextOverflow.Overflow, wrap: true);
            Place(label.rectTransform, Style.MediumPad, Style.SmallPad, boxWidth - Style.MediumPad * 2f, textHeight);
            Texts.Bind(label, text);
            Track(row, label, Style.SmallPad * 2f);
            return label;
        }
    }
}
