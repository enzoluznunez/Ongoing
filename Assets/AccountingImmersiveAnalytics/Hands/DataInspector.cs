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
using Oculus.Interaction.Input;
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
        private MarkInfo _mark;
        private string _explanationKey;
        private float _lostAt = -1f;
        private Explanation? _explanation;
        private int _shownDots = -1;

        public DataInspector(Transform parent)
        {
            _tooltip = new DataTooltip(parent);
            Texts.Refreshed += OnTextsRefreshed;
            Current = this;
        }

        public void Dispose()
        {
            Clear();
            Texts.Refreshed -= OnTextsRefreshed;
            if (Current == this) Current = null;
        }

        public string Describe()
        {
            if (_mark == null) return null;
            var text = new System.Text.StringBuilder();
            text.Append("on the chart '").Append(_view.Entry.Title).Append("', the shape ").Append(_mark.Title);
            if (!string.IsNullOrEmpty(_mark.Body)) text.Append(" (").Append(_mark.Body).Append(')');
            if (_explanation.HasValue)
                text.Append(". Its tooltip explains: ").Append(_explanation.Value.Text);
            return text.ToString();
        }

        public void Tick(Dictionary<IHand, HandRay>.ValueCollection rays)
        {
            PickOwner(rays);
            if (_owner == null)
            {
                Lost();
                return;
            }

            ChartHit hit = _owner.Mark;
            _lostAt = -1f;

            if (hit.Card != _view) Clear();
            if (hit.Mark != _mark) Show(hit.Card, hit.Mark);
            if (_owner.PinchedMark) Explain();
            TickPulse();
        }

        private void PickOwner(Dictionary<IHand, HandRay>.ValueCollection rays)
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
            if (_mark == null) return;

            bool stale = Scene.ToolSelected || _view == null || !_view.Shown || _view.Mark(_mark.Id) != _mark;
            if (_lostAt < 0f) _lostAt = Time.unscaledTime;
            if (stale || Time.unscaledTime - _lostAt >= GraceSeconds) Clear();
        }

        private void Show(ChartCard view, MarkInfo mark)
        {
            _view = view;
            _mark = mark;
            view.Highlight(mark);
            _explanationKey = MarkExplainer.KeyFor(view.Entry, mark);
            _explanation = MarkExplainer.TryCached(view.Entry, mark, out Explanation cached) ? cached : (Explanation?)null;
            Render();
        }

        private bool Waiting => _explanation == null && MarkExplainer.IsWriting(_explanationKey);

        private void Render()
        {
            bool waiting = Waiting;
            _shownDots = waiting ? LoadingDots.Count : -1;
            string status = waiting ? (FlashLiteClient.Reached ? "loading" : "assistant.connecting") : null;
            _tooltip.Show(_mark, _explanation?.Lines, status, _view);
            ChartManager charts = Scene.Charts;
            _tooltip.Place(_view, _mark, charts != null ? charts.PinnedButtonSize : Style.ButtonSize);
        }

        private void OnTextsRefreshed()
        {
            if (_mark == null) return;
            if (_view == null) Clear();
            else Render();
        }

        private void TickPulse()
        {
            if (Waiting && LoadingDots.Count != _shownDots) Render();
        }

        private void Explain()
        {
            if (_explanation.HasValue)
            {
                TellAssistant();
                return;
            }

            ChartEntry entry = _view.Entry;
            MarkInfo mark = _mark;
            MarkExplainer.Request(entry, mark, result =>
            {
                if (_view == null || _view.Entry != entry || _mark != mark) return;
                _explanation = result;
                Render();
                if (result.HasValue) TellAssistant();
            });
            Render();
        }

        private void TellAssistant()
        {
            Menu assistant = Scene.Menu;
            if (assistant != null && assistant.IsAssistantActive) ActivityFeed.Point(Describe());
        }

        public void Clear()
        {
            if (_view != null) _view.ClearHighlight();
            _tooltip.Hide();
            _view = null;
            _mark = null;
            _explanation = null;
            _shownDots = -1;
            _lostAt = -1f;
        }
    }
}
