using System.Globalization;
using AccountingImmersiveAnalytics.Assistant.FlashLite;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Text
{
    public static class LanguageDetector
    {
        private const int FewestWords = 5;
        private const int FewestUnspacedLetters = 10;
        private const float FewestConfidence = 0.8f;
        private const int TimeoutSeconds = 8;

        private static int _ticket;

        public static async void Heard(string transcript)
        {
            if (!LongEnough(transcript)) return;
            int ticket = ++_ticket;

            JObject answer = await FlashLiteClient.Ask(Instructions, transcript,
                FlashLiteClient.Schema(("language", "STRING"), ("confidence", "NUMBER")), TimeoutSeconds);
            if (ticket != _ticket) return;
            if (answer == null)
            {
                Debug.LogWarning($"[LanguageDetector] No answer for \"{transcript}\"; staying in {Language.CurrentName}.");
                return;
            }

            string code;
            float confidence;
            try
            {
                code = Language.Normalize((string)answer["language"]);
                confidence = (float?)answer["confidence"] ?? 0f;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LanguageDetector] Could not read the answer ({e.Message}); staying in {Language.CurrentName}.");
                return;
            }
            if (code == Language.Unknown || confidence < FewestConfidence)
            {
                Debug.Log($"[LanguageDetector] Not sure enough ({answer["language"]}, {confidence:F2}); staying in {Language.CurrentName}.");
                return;
            }
            Debug.Log($"[LanguageDetector] Heard {Language.NameOf(code)} ({confidence:F2}) in \"{transcript}\".");
            LanguageSwitch.To(code);
        }

        private static bool LongEnough(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript)) return false;
            int words = 0, unspaced = 0;
            bool inWord = false;
            foreach (char c in transcript)
            {
                bool letter = char.IsLetter(c);
                if (letter && !inWord) words++;
                inWord = letter || (inWord && IsMark(c));
                if (letter && Unspaced(c)) unspaced++;
            }
            return words >= FewestWords || unspaced >= FewestUnspacedLetters;
        }

        private static bool IsMark(char c) =>
            CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;

        private static bool Unspaced(char c) =>
            c >= '\u2e80' || (c >= '\u0e00' && c <= '\u0eff') || (c >= '\u1000' && c <= '\u109f') || (c >= '\u1780' && c <= '\u17ff');

        private static readonly string Instructions =
            "You identify the language a person spoke, from a transcript of their speech to a voice assistant. " +
            "Answer 'language' with a BCP-47 code from this list, or 'und' if it is none of them or cannot be told: " +
            string.Join(", ", Language.Supported.Keys) + ". " +
            "For Chinese say zh-Hans or zh-Hant from the characters used; for Portuguese say pt-BR unless the wording " +
            "is clearly European; say ku for Kurdish of any dialect, no for Bokmål or Nynorsk, sr for Serbian in either " +
            "script and pa for Punjabi in either script. Judge the language of the whole sentence, not of a single borrowed word or name. " +
            "Answer 'confidence' from 0 to 1.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _ticket = 0;
    }
}
