using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Assistant.FlashLite;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout;
using AccountingImmersiveAnalytics.UI.Charts.TwoD.View;
using AccountingImmersiveAnalytics.UI.Charts.View;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Hands
{
    public sealed class DataInspector
    {
        private const float GraceSeconds = 0.3f;

        private readonly DataTooltip _tooltip;

        public static DataInspector Current { get; private set; }

        private HandRay _owner;
        private ChartCard _view;
        private MarkInfo _markInfo;
        private int _mark = -1;
        private float _lostAt = -1f;
        private ChartEntry _entry;
        private MarkInfo _detail;
        private Explanation? _explanation;
        private int _shownDots = -1;

        public DataInspector(Transform parent)
        {
            _tooltip = new DataTooltip(parent);
            Texts.Refreshed += OnTextsRefreshed;
            Current = this;
        }

        public bool IsShowing => _mark >= 0 && _entry != null;

        public string Describe()
        {
            if (!IsShowing) return null;
            var text = new System.Text.StringBuilder();
            text.Append("on the chart '").Append(_entry.Title).Append("', the shape ").Append(_detail.Title);
            if (!string.IsNullOrEmpty(_detail.Body)) text.Append(" (").Append(_detail.Body.Replace("\n", "; ")).Append(')');
            if (_explanation.HasValue)
                text.Append(". Its tooltip explains: ").Append(_explanation.Value.Text);
            return text.ToString();
        }

        public void Tick<T>(Dictionary<T, HandRay>.ValueCollection rays)
        {
            if (!HandRay.ToolSelected)
            {
                PickOwner(rays);
            }
            else
            {
                _owner = null;
            }

            if (_owner == null || !_owner.HasMark)
            {
                Lost();
                return;
            }

            ChartHit hit = _owner.Mark;
            _lostAt = -1f;

            if (hit.Card != _view) Clear();

            bool pinched = _owner.PinchedMark;
            if (hit.Mark.Id != _mark) Show(hit.Card, hit.Mark);
            if (pinched && _mark >= 0) Explain();
            if (_mark >= 0) TickPulse();
        }

        private void PickOwner<T>(Dictionary<T, HandRay>.ValueCollection rays)
        {
            if (_owner != null && _owner.HasMark) return;
            _owner = null;
            foreach (HandRay ray in rays)
            {
                if (!ray.HasMark) continue;
                _owner = ray;
                return;
            }
        }

        private void Lost()
        {
            if (_mark < 0) return;

            bool stale = HandRay.ToolSelected || _view == null || !_view.Shown || _view.Mark(_mark) != _markInfo;
            if (stale)
            {
                Clear();
                return;
            }

            if (_lostAt < 0f) _lostAt = Time.unscaledTime;
            if (Time.unscaledTime - _lostAt >= GraceSeconds)
            {
                Clear();
            }
        }

        private void Show(ChartCard view, MarkInfo mark)
        {
            if (_view != null && _view != view) _view.ClearHighlight();

            _view = view;
            _markInfo = mark;
            _mark = mark.Id;
            _entry = view.Entry;
            view.Highlight(mark);

            _detail = mark;
            _explanation = MarkExplainer.TryCached(_entry, mark, out Explanation cached) ? cached : (Explanation?)null;
            Render();
        }

        private bool Waiting => _explanation == null && _entry != null && MarkExplainer.IsWriting(_entry, _markInfo);

        private void Render()
        {
            string waiting = Waiting ? (MarkExplainer.Reached ? "loading" : "assistant.connecting") : null;
            _shownDots = Waiting ? LoadingDots.Count : -1;
            _tooltip.Show(_detail, _explanation?.Lines, waiting, _view);
            ChartManager charts = Scene.Charts;
            _tooltip.Place(_view, _markInfo, charts != null ? charts.PinnedButtonSize : Style.ButtonSize);
        }

        private void OnTextsRefreshed()
        {
            if (_mark >= 0 && _view != null && _detail != null) Render();
        }

        private void TickPulse()
        {
            if (Waiting && LoadingDots.Count != _shownDots) Render();
        }

        private void Explain()
        {
            if (_entry == null || _markInfo == null) return;
            if (_explanation.HasValue)
            {
                TellAda();
                return;
            }

            ChartEntry entry = _entry;
            MarkInfo mark = _markInfo;
            MarkExplainer.Request(entry, mark, _detail, result =>
            {
                if (_entry != entry || _markInfo != mark) return;
                _explanation = result;
                Render();
                if (result.HasValue) TellAda();
            });
            Render();
        }

        private void TellAda()
        {
            Menu assistant = Scene.Assistant;
            if (assistant != null && assistant.IsAssistantActive) ActivityFeed.Point(Describe());
        }

        public void Clear()
        {
            if (_view != null) _view.ClearHighlight();
            _tooltip.Hide();
            _view = null;
            _markInfo = null;
            _entry = null;
            _explanation = null;
            _mark = -1;
            _lostAt = -1f;
        }
    }
}
