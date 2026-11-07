using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

public class HandRays : MonoBehaviour
{
    private const float CanvasRescanSeconds = 2f;
    private const int ExpectedCanvases = 3;

    private readonly Dictionary<IHand, HandRay> _rays = new Dictionary<IHand, HandRay>();
    private readonly HashSet<IHand> _tracked = new HashSet<IHand>();
    private readonly List<PointableCanvas> _canvases = new List<PointableCanvas>();
    private float _nextCanvasScan;

    public IReadOnlyList<PointableCanvas> Canvases => _canvases;

    private void Update()
    {
        RescanCanvases();

        _tracked.Clear();
        IReadOnlyList<Fingertips.Source> sources = Fingertips.All;
        for (int i = 0; i < sources.Count; i++)
        {
            IHand hand = sources[i].hand;
            if (hand == null || !_tracked.Add(hand)) continue;
            if (!_rays.TryGetValue(hand, out HandRay ray))
            {
                ray = new HandRay(this, hand, _rays.Count);
                _rays[hand] = ray;
            }
            ray.Tick();
        }

        foreach (KeyValuePair<IHand, HandRay> entry in _rays)
            if (!_tracked.Contains(entry.Key)) entry.Value.Lose();
    }

    private void RescanCanvases()
    {
        if (_canvases.Count >= ExpectedCanvases && !_canvases.Contains(null)) return;
        if (Time.unscaledTime < _nextCanvasScan) return;
        _nextCanvasScan = Time.unscaledTime + CanvasRescanSeconds;
        _canvases.Clear();
        _canvases.AddRange(FindObjectsByType<PointableCanvas>());
    }

    private void OnDisable()
    {
        foreach (HandRay ray in _rays.Values) ray.Lose();
    }
}
