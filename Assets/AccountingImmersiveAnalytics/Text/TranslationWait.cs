using System;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Text
{
    public sealed class TranslationWait : MonoBehaviour
    {
        private Action<string, string> _write;
        private string _shown;

        public void Begin(Action<string, string> write)
        {
            _write = write;
            _shown = null;
            enabled = true;
            Write();
        }

        public void End()
        {
            enabled = false;
            _write = null;
            _shown = null;
        }

        private void LateUpdate() => Write();

        private void Write()
        {
            (string text, string language) = Loading.Steady();
            if (text == _shown || _write == null) return;
            _shown = text;
            _write(text, language);
        }
    }
}
