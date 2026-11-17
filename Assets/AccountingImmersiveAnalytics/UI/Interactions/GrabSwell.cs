using System;
using System.Collections;
using AccountingImmersiveAnalytics.UI.Helpers;
using Oculus.Interaction;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public struct GrabSwell
    {
        private bool _on;
        private Vector3 _rest;

        public bool On => _on;

        public void Set(Transform t, bool held)
        {
            if (held == _on) return;
            if (held)
            {
                _rest = t.localScale;
                t.localScale = _rest * Style.EngageScale;
            }
            else t.localScale = _rest;
            _on = held;
        }
    }
}
