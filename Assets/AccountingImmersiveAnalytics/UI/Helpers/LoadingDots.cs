using System;
using AccountingImmersiveAnalytics.Text;
using OneText.UGUI;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public static class LoadingDots
    {
        public const int Most = 3;
        public const float TickSeconds = Animate.Slow;

        public static int Count => (int)(Time.unscaledTime / TickSeconds) % (Most + 1);

        public static string Steady(string word) => Steady(word, Count);

        public static string Steady(string word, int dots)
        {
            dots = Mathf.Clamp(dots, 0, Most);
            string hidden = dots < Most ? $"<alpha=#00>{new string('.', Most - dots)}" : "";
            return $"<noparse>{word}{new string('.', dots)}</noparse>{hidden}";
        }

        public static void Bind(OneTextLabel label, Func<string> word, bool counting)
        {
            if (label == null) return;
            label.RichText = counting;
            if (counting) Texts.Bind(label, () => Steady(word()));
            else Texts.Bind(label, word);
        }
    }
}
