using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.UI.Charts.Layout;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.View
{
    public static class ChartHits
    {
        private const float PlaneSlack = 0.004f;

        private static readonly List<ChartCard> Live = new List<ChartCard>();

        public static bool AtPoint(Vector3 world, out ChartHit hit) => Near(world, PlaneSlack, out hit);

        public static bool Near(Vector3 world, float depth, out ChartHit hit)
        {
            hit = default;
            ChartManager charts = Scene.Charts;
            if (charts == null) return false;
            float closest = float.MaxValue;
            Live.Clear();
            charts.CollectLiveCards(Live);
            foreach (ChartCard card in Live)
            {
                if (!card.TryDesignPoint(world, out Vector2 design, out float away) || away > depth) continue;
                MarkInfo mark = card.MarkAt(design);
                if (mark == null || away >= closest) continue;
                closest = away;
                hit = new ChartHit { Card = card, Mark = mark, Point = world };
            }
            return hit.Card != null;
        }
    }
}
