using System;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public sealed class CardFit : MonoBehaviour
    {
        public Action Refit;

        private bool _dirty;

        public void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;
            Refit?.Invoke();
        }
    }
}
