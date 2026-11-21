using System;
using System.Collections.Generic;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Text
{
    public static class Language
    {
        public const string English = "en";
        public const string Unknown = "und";

        public static readonly IReadOnlyDictionary<string, string> Supported = new Dictionary<string, string>
        {
            ["af"] = "Afrikaans", ["ak"] = "Akan", ["sq"] = "Albanian", ["am"] = "Amharic", ["ar"] = "Arabic",
            ["hy"] = "Armenian", ["as"] = "Assamese", ["az"] = "Azerbaijani", ["eu"] = "Basque", ["be"] = "Belarusian",
            ["bn"] = "Bengali", ["bs"] = "Bosnian", ["bg"] = "Bulgarian", ["my"] = "Burmese", ["ca"] = "Catalan",
            ["ceb"] = "Cebuano", ["zh-Hans"] = "Chinese (Simplified)", ["zh-Hant"] = "Chinese (Traditional)",
            ["hr"] = "Croatian", ["cs"] = "Czech", ["da"] = "Danish", ["nl"] = "Dutch", ["en"] = "English",
            ["et"] = "Estonian", ["fo"] = "Faroese", ["fil"] = "Filipino", ["fi"] = "Finnish", ["fr"] = "French",
            ["gl"] = "Galician", ["ka"] = "Georgian", ["de"] = "German", ["el"] = "Greek", ["gu"] = "Gujarati",
            ["ha"] = "Hausa", ["he"] = "Hebrew", ["hi"] = "Hindi", ["hu"] = "Hungarian", ["is"] = "Icelandic",
            ["id"] = "Indonesian", ["ga"] = "Irish", ["it"] = "Italian", ["ja"] = "Japanese", ["kn"] = "Kannada",
            ["kk"] = "Kazakh", ["km"] = "Khmer", ["rw"] = "Kinyarwanda", ["ko"] = "Korean", ["ku"] = "Kurdish",
            ["ky"] = "Kyrgyz", ["lo"] = "Lao", ["lv"] = "Latvian", ["lt"] = "Lithuanian", ["mk"] = "Macedonian",
            ["ms"] = "Malay", ["ml"] = "Malayalam", ["mt"] = "Maltese", ["mi"] = "Maori", ["mr"] = "Marathi",
            ["mn"] = "Mongolian", ["ne"] = "Nepali", ["no"] = "Norwegian", ["or"] = "Odia", ["om"] = "Oromo",
            ["ps"] = "Pashto", ["fa"] = "Persian", ["pl"] = "Polish", ["pt-BR"] = "Portuguese (Brazil)",
            ["pt-PT"] = "Portuguese (Portugal)", ["pa"] = "Punjabi", ["qu"] = "Quechua", ["ro"] = "Romanian",
            ["rm"] = "Romansh", ["ru"] = "Russian", ["sr"] = "Serbian", ["sd"] = "Sindhi", ["si"] = "Sinhala",
            ["sk"] = "Slovak", ["sl"] = "Slovenian", ["so"] = "Somali", ["st"] = "Southern Sotho", ["es"] = "Spanish",
            ["sw"] = "Swahili", ["sv"] = "Swedish", ["tg"] = "Tajik", ["ta"] = "Tamil", ["te"] = "Telugu",
            ["th"] = "Thai", ["tn"] = "Tswana", ["tr"] = "Turkish", ["tk"] = "Turkmen", ["uk"] = "Ukrainian",
            ["ur"] = "Urdu", ["uz"] = "Uzbek", ["vi"] = "Vietnamese", ["cy"] = "Welsh", ["fy"] = "Western Frisian",
            ["wo"] = "Wolof", ["yo"] = "Yoruba", ["zu"] = "Zulu"
        };

        public static event Action Changed;

        public static string Current { get; private set; } = English;

        private static readonly IReadOnlyDictionary<string, string> Scripts = new Dictionary<string, string>
        {
            ["af"] = "Latin", ["ak"] = "Latin", ["sq"] = "Latin", ["am"] = "Ge'ez (Ethiopic)", ["ar"] = "Arabic",
            ["hy"] = "Armenian", ["as"] = "Bengali-Assamese", ["az"] = "Latin", ["eu"] = "Latin", ["be"] = "Cyrillic",
            ["bn"] = "Bengali", ["bs"] = "Latin", ["bg"] = "Cyrillic", ["my"] = "Myanmar", ["ca"] = "Latin",
            ["ceb"] = "Latin", ["zh-Hans"] = "simplified Chinese", ["zh-Hant"] = "traditional Chinese", ["hr"] = "Latin", ["cs"] = "Latin",
            ["da"] = "Latin", ["nl"] = "Latin", ["en"] = "Latin", ["et"] = "Latin", ["fo"] = "Latin",
            ["fil"] = "Latin", ["fi"] = "Latin", ["fr"] = "Latin", ["gl"] = "Latin", ["ka"] = "Georgian",
            ["de"] = "Latin", ["el"] = "Greek", ["gu"] = "Gujarati", ["ha"] = "Latin", ["he"] = "Hebrew",
            ["hi"] = "Devanagari", ["hu"] = "Latin", ["is"] = "Latin", ["id"] = "Latin", ["ga"] = "Latin",
            ["it"] = "Latin", ["ja"] = "Japanese (kanji and kana)", ["kn"] = "Kannada", ["kk"] = "Cyrillic", ["km"] = "Khmer",
            ["rw"] = "Latin", ["ko"] = "Korean (Hangul)", ["ku"] = "Latin", ["ky"] = "Cyrillic", ["lo"] = "Lao",
            ["lv"] = "Latin", ["lt"] = "Latin", ["mk"] = "Cyrillic", ["ms"] = "Latin", ["ml"] = "Malayalam",
            ["mt"] = "Latin", ["mi"] = "Latin", ["mr"] = "Devanagari", ["mn"] = "Cyrillic", ["ne"] = "Devanagari",
            ["no"] = "Latin", ["or"] = "Odia", ["om"] = "Latin", ["ps"] = "Arabic", ["fa"] = "Arabic",
            ["pl"] = "Latin", ["pt-BR"] = "Latin", ["pt-PT"] = "Latin", ["pa"] = "Gurmukhi", ["qu"] = "Latin",
            ["ro"] = "Latin", ["rm"] = "Latin", ["ru"] = "Cyrillic", ["sr"] = "Cyrillic", ["sd"] = "Arabic",
            ["si"] = "Sinhala", ["sk"] = "Latin", ["sl"] = "Latin", ["so"] = "Latin", ["st"] = "Latin",
            ["es"] = "Latin", ["sw"] = "Latin", ["sv"] = "Latin", ["tg"] = "Cyrillic", ["ta"] = "Tamil",
            ["te"] = "Telugu", ["th"] = "Thai", ["tn"] = "Latin", ["tr"] = "Latin", ["tk"] = "Latin",
            ["uk"] = "Cyrillic", ["ur"] = "Arabic", ["uz"] = "Latin", ["vi"] = "Latin", ["cy"] = "Latin",
            ["fy"] = "Latin", ["wo"] = "Latin", ["yo"] = "Latin", ["zu"] = "Latin"
        };

        private static readonly Dictionary<string, string> Varieties = new Dictionary<string, string>
        {
            ["ku"] = "Northern Kurdish (Kurmanji)", ["no"] = "Norwegian Bokmål", ["pt-BR"] = "Brazilian Portuguese",
            ["pt-PT"] = "European Portuguese", ["pa"] = "Eastern Punjabi", ["fil"] = "Filipino (Tagalog)",
            ["ms"] = "Standard Malay", ["mn"] = "Khalkha Mongolian", ["ar"] = "Modern Standard Arabic",
            ["sw"] = "Standard Swahili", ["qu"] = "Southern Quechua"
        };

        private static string ScriptOf(string code) => code != null && Scripts.TryGetValue(code, out string script) ? script : null;

        public static string WritingOf(string code)
        {
            string script = ScriptOf(code);
            string name = code != null && Varieties.TryGetValue(code, out string variety) ? variety : NameOf(code);
            return script == null ? $"Write in {name}." : $"Write in {name}, in the {script} script only.";
        }

        public static string CurrentName => NameOf(Current);

        public static bool RightToLeft => ScriptOf(Current) is "Arabic" or "Hebrew";

        public static string NameOf(string code) => code != null && Supported.TryGetValue(code, out string name) ? name : code;

        public static bool IsSupported(string code) => code != null && Supported.ContainsKey(code);

        private static readonly Dictionary<string, string> SupportedIgnoringCase = BuildSupportedIgnoringCase();

        private static Dictionary<string, string> BuildSupportedIgnoringCase()
        {
            var codes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string code in Supported.Keys) codes[code] = code;
            return codes;
        }

        public static string Normalize(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return Unknown;
            string clean = tag.Trim().Replace('_', '-');
            if (SupportedIgnoringCase.TryGetValue(clean, out string exact)) return exact;

            string[] parts = clean.ToLowerInvariant().Split('-');
            string primary = parts[0];
            if (primary == "zh")
                return Array.Exists(parts, part => part == "hant" || part == "tw" || part == "hk" || part == "mo") ? "zh-Hant" : "zh-Hans";
            if (primary == "pt") return Array.FindIndex(parts, 1, part => part is "pt" or "ao" or "mz" or "cv" or "gw" or "st" or "tl" or "lu" or "ch") >= 0 ? "pt-PT" : "pt-BR";
            if (primary == "nb" || primary == "nn") return "no";
            if (primary == "tl") return "fil";
            if (primary == "iw") return "he";
            if (primary == "in") return "id";
            return Supported.ContainsKey(primary) ? primary : Unknown;
        }

        public static bool Same(string a, string b) => a == b && a != Unknown;

        public static void Set(string code)
        {
            if (!IsSupported(code))
            {
                Debug.LogWarning($"[Language] '{code}' is not one of the supported languages; staying in {CurrentName}.");
                return;
            }
            if (code == Current) return;
            Current = code;
            Debug.Log($"[Language] Now showing {CurrentName} ({code}).");
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = English;
            Changed = null;
        }
    }
}
