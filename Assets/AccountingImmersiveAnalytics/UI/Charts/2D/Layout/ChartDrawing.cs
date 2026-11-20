using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Elements;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Marks;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout
{
    public sealed class ChartDrawing
    {
        private const float E = Style.LargePad;

        public readonly ChartSpec Spec;
        public readonly CsvTable Table;
        public readonly ChartShaping Shaping;
        public readonly MarkSurface Marks;
        public readonly ChartText Text;
        public readonly List<KeySection> Key = new List<KeySection>();
        public readonly List<MarkInfo> Hits = new List<MarkInfo>();
        public readonly List<string> LeftOut = new List<string>();

        public Rect Plot { get; private set; }
        public AxisSpec X { get; private set; }
        public AxisSpec Y { get; private set; }
        public string Notice;
        public float Grow { get; private set; } = 1f;
        public float Width { get; }
        public float Height { get; }

        private bool _outlined;

        public ChartDrawing(ChartSpec spec, CsvTable table, ChartShaping shaping, MarkSurface marks, ChartText text,
            float width = ChartStyle.CardWidth)
        {
            Width = width;
            Height = ChartStyle.HeightFor(width);
            Spec = spec;
            Table = table;
            Shaping = shaping ?? new ChartShaping();
            _titleField = TitleField;
            Marks = marks;
            Text = text;
        }

        private static float AxisBand => Style.InkHeight(Style.MediumText);
        private static float TickBand => Style.InkHeight(Style.SmallText);
        private static float PlotTop => Style.LargePad + ChartStyle.TitleRow + Style.MediumPad;

        private void Card()
        {
            Marks.ClipTo(null);
            Marks.Box(new Rect(0f, 0f, Width, Height), Style.Card, Style.LargeRadius);
            Text.Put(TitleText(), Style.ChartTitle, Style.BoldWeight,
                Width * 0.5f, Style.CentredBaseline(Style.LargePad + ChartStyle.TitleRow * 0.5f, Style.ChartTitle), Align.Middle);
        }

        private string TitleText()
        {
            string shown = ChartText.Shown(Spec.Title);
            if (!ChartText.Uncut) shown = ChartText.CapAt(shown, ChartStyle.PinnedChars);
            return ChartText.Clip(shown, Style.ChartTitle, Width - E * 2f, Style.BoldWeight);
        }

        public void Axes(AxisSpec x, AxisSpec y, bool grid = true)
        {
            X = x;
            Y = y;
            Card();
            var xLabels = new List<string>();
            var yLabels = new List<string>();
            for (int i = 0; i < x.TickCount; i++) xLabels.Add(x.TickText(i));
            for (int i = 0; i < y.TickCount; i++) yLabels.Add(y.TickText(i));
            float yTickWidth = 0f;
            foreach (string label in yLabels) yTickWidth = Mathf.Max(yTickWidth, ChartText.Width(label, Style.SmallText));

            float left = E + AxisBand + E + yTickWidth + Style.SmallPad;
            float right = Width - E;
            float top = PlotTop;
            float bottom = Height - E - AxisBand - E - TickBand - Style.SmallPad;
            Plot = Rect.MinMaxRect(left, top, right, bottom);
            string xTitle = ChartText.Heading(ChartText.Shown(x.Title), Style.MediumText, Plot.width);
            string yTitle = ChartText.Heading(ChartText.Shown(y.Title), Style.MediumText, Plot.height);
            if (ChartText.Uncut)
            {
                NeedRoom(xLabels, x.IsCategory);
                NeedTitleRoom(xTitle, yTitle);
            }

            if (grid)
            {
                if (x.IsCategory)
                    for (int i = 1; i < x.Categories.Count; i++) VerticalLine(left + XBand * i);
                else
                    for (int i = 1; i < ChartStyle.Ticks - 1; i++) VerticalLine(XAt(x.Numbers.TickValue(i)));
                if (y.IsCategory)
                    for (int i = 1; i < y.Categories.Count; i++) HorizontalLine(top + YBand * i);
                else
                    for (int i = 1; i < ChartStyle.Ticks - 1; i++) HorizontalLine(YAt(y.Numbers.TickValue(i)));
            }

            float xBaseline = bottom + Style.SmallPad + Style.Ascent(Style.SmallText);
            int lastX = xLabels.Count - 1;
            for (int i = 0; i < xLabels.Count; i++)
            {
                if (x.IsCategory) Text.Put(ChartText.Fit(xLabels[i], Style.SmallText, XBand - Style.SmallPad), Style.SmallText, Style.NormalWeight, XCategory(i), xBaseline, Align.Middle);
                else
                {
                    Align align = i == 0 ? Align.Start : i == lastX ? Align.End : Align.Middle;
                    float at = XAt(x.Numbers.TickValue(i));
                    if (x.Numbers.Reversed) align = i == 0 ? Align.End : i == lastX ? Align.Start : Align.Middle;
                    Text.Put(xLabels[i], Style.SmallText, Style.NormalWeight, at, xBaseline, align);
                }
            }

            float yRight = left - Style.SmallPad;
            float centred = Style.Ascent(Style.SmallText) * 0.5f;
            for (int i = 0; i < yLabels.Count; i++)
            {
                if (y.IsCategory)
                {
                    Text.Put(yLabels[i], Style.SmallText, Style.NormalWeight, yRight, YCategory(i) + centred, Align.End);
                    continue;
                }
                float at = YAt(y.Numbers.TickValue(i));
                bool atBottom = Mathf.Abs(at - bottom) < 0.5f, atTop = Mathf.Abs(at - top) < 0.5f;
                float baseline = atBottom ? at - Style.Descent(Style.SmallText)
                    : atTop ? at + Style.Ascent(Style.SmallText)
                    : at + centred;
                Text.Put(yLabels[i], Style.SmallText, Style.NormalWeight, yRight, baseline, Align.End);
            }

            Text.Put(xTitle, Style.MediumText, Style.BoldWeight, Plot.center.x,
                Height - E - Style.Descent(Style.MediumText), Align.Middle);
            Text.Put(yTitle, Style.MediumText, Style.BoldWeight,
                E + Style.Ascent(Style.MediumText), Plot.center.y, Align.Middle, rising: true);

            _outlined = true;
            Marks.ClipTo(Plot);
        }

        public void Open(bool framed)
        {
            X = null;
            Y = null;
            Card();
            Plot = Rect.MinMaxRect(E, PlotTop, Width - E, Height - E);
            _outlined = framed;
            Marks.ClipTo(framed ? Plot : (Rect?)null);
        }

        public void NeedScale(float ratio)
        {
            if (ratio > Grow) Grow = ratio;
        }

        private void NeedRoom(List<string> xLabels, bool categories)
        {
            float fewest = (ChartStyle.CardWidth - Style.LargePad * 2f) * ChartStyle.FewestPlotShare;
            if (Plot.width < fewest) NeedScale((Width + fewest - Plot.width) / Width);
            if (!categories || xLabels.Count == 0) return;
            float widest = 0f;
            foreach (string label in xLabels) widest = Mathf.Max(widest, ChartText.Width(label, Style.SmallText));
            float band = Plot.width / xLabels.Count;
            float need = widest + ChartStyle.LabelRoom;
            if (need > band) NeedScale((Width + (need - band) * xLabels.Count) / Width);
        }

        private void NeedTitleRoom(string xTitle, string yTitle)
        {
            float across = ChartText.Width(xTitle, Style.MediumText, Style.BoldWeight);
            if (across > Plot.width) NeedScale((Width + across - Plot.width) / Width);
            float up = ChartText.Width(yTitle, Style.MediumText, Style.BoldWeight);
            if (up > Plot.height) NeedScale((Height + up - Plot.height) / Height);
        }

        private void DrawTrend()
        {
            if (X == null || Y == null || X.IsCategory || Y.IsCategory) return;
            TrendFit fit = TrendFitter.Fit(Spec, Table, Shaping, Shaping.Trend);
            if (fit == null) return;
            var points = new List<Vector2>(fit.Curve.Count);
            foreach ((double x, double y) in fit.Curve) points.Add(new Vector2(XAt(x), YAt(y)));
            Marks.ClipTo(Plot);
            Marks.Polyline(points, ChartStyle.TrendWidth, ChartStyle.Accent);
        }

        public void Finish()
        {
            DrawTrend();
            Marks.ClipTo(null);
            if (_outlined) Marks.Box(Plot, Style.Ink, Style.MediumRadius, Style.Stroke);
            if (Notice != null)
                Text.Put(ChartText.Shown(Notice), Style.SmallText, Style.NormalWeight, Plot.center.x,
                    Style.CentredBaseline(Plot.center.y, Style.SmallText), Align.Middle);
            Marks.Commit();
        }

        public float XBand => X != null && X.IsCategory ? Plot.width / Mathf.Max(1, X.Categories.Count) : 0f;
        public float YBand => Y != null && Y.IsCategory ? Plot.height / Mathf.Max(1, Y.Categories.Count) : 0f;

        public float XCategory(int i) => Plot.xMin + XBand * (i + 0.5f);
        public float YCategory(int i) => Plot.yMin + YBand * (i + 0.5f);

        public float XAt(double v) => Plot.xMin + X.Numbers.Fraction(v) * Plot.width;
        public float YAt(double v) => Plot.yMax - Y.Numbers.Fraction(v) * Plot.height;

        private void VerticalLine(float x) =>
            Marks.Box(new Rect(x - Style.Stroke * 0.5f, Plot.yMin, Style.Stroke, Plot.height), Style.Ink);

        private void HorizontalLine(float y) =>
            Marks.Box(new Rect(Plot.xMin, y - Style.Stroke * 0.5f, Plot.width, Style.Stroke), Style.Ink);

        public MarkInfo Hit(int shape, string title)
        {
            if (shape < 0) return new MarkInfo();
            var info = new MarkInfo { Id = Hits.Count, Shape = shape, Title = title, TitleField = _titleField };
            Hits.Add(info);
            return info;
        }

        private readonly string _titleField;

        private string TitleField
        {
            get
            {
                foreach (Role role in new[] { Role.Category, Role.Item, Role.Group })
                    if (Spec.FieldFor(role) != null) return Spec.TitleFor(role);
                if (Spec.Kind == ChartKind.Histogram) return Spec.TitleFor(Role.Value);
                if (Spec.Kind == ChartKind.Heatmap) return $"{Spec.TitleFor(Role.X)}, {Spec.TitleFor(Role.Y)}";
                return null;
            }
        }

        public MarkInfo HitBox(int shape, Rect box, string title)
        {
            MarkInfo info = Hit(shape, title);
            info.Area = HitShape.Box;
            info.Box = box;
            return info;
        }

        public MarkInfo HitCircle(int shape, Vector2 centre, float radius, string title)
        {
            MarkInfo info = Hit(shape, title);
            info.Area = HitShape.Circle;
            info.Centre = centre;
            info.Outer = radius;
            return info;
        }
    }
}
