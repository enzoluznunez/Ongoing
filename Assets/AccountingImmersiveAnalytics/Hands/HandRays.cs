using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.UI.Charts.View;
using AccountingImmersiveAnalytics.UI.Interactions;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Hands
{
    public class HandRays : MonoBehaviour
    {
        private const float CanvasRescanSeconds = 2f;
        private const int ExpectedCanvases = 4;

        private readonly Dictionary<IHand, HandRay> _rays = new Dictionary<IHand, HandRay>();
        private readonly HashSet<IHand> _tracked = new HashSet<IHand>();
        private readonly List<PointableCanvas> _canvases = new List<PointableCanvas>();
        private float _nextCanvasScan;
        private int _pinnedVersion;
        private DataInspector _inspector;

        public IReadOnlyList<PointableCanvas> Canvases => _canvases;

        private void Start()
        {
            _inspector = new DataInspector(transform);
        }

        private void Update()
        {
            RescanCanvases();
            PointerSpots.Clear();

            _tracked.Clear();
            IReadOnlyList<Fingertips.Source> sources = Fingertips.All;
            for (int i = 0; i < sources.Count; i++)
            {
                IHand hand = sources[i].hand;
                if (hand == null || !_tracked.Add(hand)) continue;
                if (!_rays.TryGetValue(hand, out HandRay ray))
                {
                    ray = new HandRay(this, sources[i], _rays.Count);
                    _rays[hand] = ray;
                }
                ray.Tick();
                if (ray.PointsAtCanvas) PointerSpots.Add(ray.CanvasPoint);
                if (sources[i].Live) PointerSpots.Add(sources[i].poke.Origin);
            }

            foreach (KeyValuePair<IHand, HandRay> entry in _rays)
                if (!_tracked.Contains(entry.Key)) entry.Value.Lose();

            _inspector.Tick(_rays.Values);
        }

        private void RescanCanvases()
        {
            bool added = _pinnedVersion != PinnedChart.CanvasVersion;
            if (!added && _canvases.Count >= ExpectedCanvases && !_canvases.Contains(null)) return;
            if (!added && Time.unscaledTime < _nextCanvasScan) return;
            _pinnedVersion = PinnedChart.CanvasVersion;
            _nextCanvasScan = Time.unscaledTime + CanvasRescanSeconds;
            _canvases.Clear();
            _canvases.AddRange(FindObjectsByType<PointableCanvas>());
        }

        private void OnDisable()
        {
            foreach (HandRay ray in _rays.Values) ray.Lose();
            _inspector?.Clear();
        }

        private void OnDestroy()
        {
            _inspector?.Dispose();
        }
    }
}
