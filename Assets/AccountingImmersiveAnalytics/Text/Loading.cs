using System.Threading.Tasks;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Text
{
    public static class Loading
    {
        private const string Key = "loading";
        private const float HeldSeconds = LoadingDots.Most * LoadingDots.TickSeconds;

        private static string _word;
        private static int _hold;

        public static bool Holding { get; private set; }

        public static string Original => Strings.Original(Key);

        public static string ShownLanguage { get; private set; } = Language.English;

        public static void Leaving(string language)
        {
            if (!Translator.TryCached(Original, Language.English, language, out string word) || string.IsNullOrEmpty(word)) return;
            _word = word;
            ShownLanguage = language;
        }

        public static async void Hold()
        {
            int hold = ++_hold;
            Holding = true;
            await Task.Delay(Mathf.CeilToInt(HeldSeconds * 1000f));
            if (hold != _hold) return;
            Holding = false;
            Texts.Refresh();
        }

        private static string _steadyWord;
        private static int _steadyDots = -1;
        private static string _steadyText;

        public static (string text, string language) Steady()
        {
            string word = _word ?? Original;
            int dots = LoadingDots.Count;
            if (dots != _steadyDots || !ReferenceEquals(word, _steadyWord))
            {
                _steadyWord = word;
                _steadyDots = dots;
                _steadyText = LoadingDots.Steady(word, dots);
            }
            return (_steadyText, ShownLanguage);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _word = null;
            _steadyWord = null;
            _steadyDots = -1;
            ShownLanguage = Language.English;
            _hold++;
            Holding = false;
        }
    }
}
