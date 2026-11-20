using OneText;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Text
{
    public static class LanguageSwitch
    {
        public static void To(string code)
        {
            if (!Language.IsSupported(code) || code == Language.Current) return;

            Loading.Leaving(Language.Current);
            Loading.Hold();
            _ = Translator.Prioritize(code, Loading.Original);
            FontResidency.SetLanguages(code, Loading.ShownLanguage);
            Language.Set(code);
            Debug.Log($"[LanguageSwitch] Switched to {Language.NameOf(code)}; text shows Loading until it is translated.");
        }
    }
}
