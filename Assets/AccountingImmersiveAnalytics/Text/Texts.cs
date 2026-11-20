using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Helpers;
using OneText;
using OneText.UGUI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AccountingImmersiveAnalytics.Text
{
    public static class Texts
    {
        private sealed class Binding
        {
            public Component Label;
            public Func<string> Compose;
            public Action<string> Apply;
            public Action<string, string> ShowLoading;
            public TranslationSurface Surface;
            public TranslationWait Wait;
            public bool ShowingLoading;
            public bool RichBefore;
        }

        private static readonly Dictionary<Object, Binding> Bound = new Dictionary<Object, Binding>();
        private static readonly List<Binding> Snapshot = new List<Binding>();
        private static bool _listening;

        public static event Action Refreshed;

        public static void Bind(OneTextLabel label, Func<string> compose)
        {
            if (label == null) return;
            Bind(label, compose,
                text =>
                {
                    label.Language = Language.Current;
                    label.Text = text;
                },
                (text, language) =>
                {
                    label.Language = language;
                    label.Text = text;
                });
        }

        public static void Bind(OneTextMesh label, Func<string> compose)
        {
            if (label == null) return;
            Bind(label, compose, text => label.Text = text, (text, _) => label.Text = text);
        }

        public static void Refresh()
        {
            Snapshot.Clear();
            Snapshot.AddRange(Bound.Values);
            foreach (Binding binding in Snapshot)
            {
                if (binding.Label == null)
                {
                    Bound.Remove(binding.Label);
                    continue;
                }
                Show(binding);
            }
            Snapshot.Clear();
            Refreshed?.Invoke();
        }

        private static void Bind(Component label, Func<string> compose, Action<string> apply, Action<string, string> showLoading)
        {
            Listen();
            if (!Bound.TryGetValue(label, out Binding binding))
                Bound[label] = binding = new Binding { Label = label };
            binding.Compose = compose;
            binding.Apply = apply;
            binding.ShowLoading = showLoading;
            Show(binding);
        }

        private static void Show(Binding binding)
        {
            int misses = Translator.Misses;
            int lookups = Translator.Lookups;
            string text = binding.Compose();
            bool waiting = Translator.Misses != misses || (Loading.Holding && Translator.Lookups != lookups);

            TranslationSurface surface = binding.Label.TryGetComponent(out LoadsAlone _) ? null : binding.Label.GetComponentInParent<TranslationSurface>(true);
            if (surface != binding.Surface)
            {
                if (binding.Surface != null) binding.Surface.SetWaiting(binding.Label, false);
                binding.Surface = surface;
            }

            if (surface != null)
            {
                if (binding.Wait != null) binding.Wait.End();
                EndLoading(binding);
                surface.SetWaiting(binding.Label, waiting);
                binding.Apply(text);
                return;
            }

            if (waiting)
            {
                StartLoading(binding);
                if (binding.Wait == null) binding.Wait = LayoutTools.Ensure<TranslationWait>(binding.Label.gameObject);
                binding.Wait.Begin(binding.ShowLoading);
                return;
            }

            if (binding.Wait != null) binding.Wait.End();
            EndLoading(binding);
            binding.Apply(text);
        }

        private static void StartLoading(Binding binding)
        {
            if (binding.ShowingLoading) return;
            binding.ShowingLoading = true;
            binding.RichBefore = RichOf(binding.Label);
            SetRich(binding.Label, true);
        }

        private static void EndLoading(Binding binding)
        {
            if (!binding.ShowingLoading) return;
            binding.ShowingLoading = false;
            SetRich(binding.Label, binding.RichBefore);
        }

        private static bool RichOf(Component label) =>
            label is OneTextLabel flat ? flat.RichText : label is OneTextMesh mesh && mesh.RichText;

        private static void SetRich(Component label, bool rich)
        {
            if (label is OneTextLabel flat) flat.RichText = rich;
            else if (label is OneTextMesh mesh) mesh.RichText = rich;
        }

        private static void Listen()
        {
            if (_listening) return;
            _listening = true;
            Language.Changed += Refresh;
            Translator.Changed += Refresh;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Bound.Clear();
            Snapshot.Clear();
            _listening = false;
            Refreshed = null;
        }
    }
}
