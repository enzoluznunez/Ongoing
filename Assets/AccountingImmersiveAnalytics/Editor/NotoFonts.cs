using System.Collections.Generic;
using System.IO;
using System.Text;
using OneText;
using OneText.Editor;
using UnityEditor;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Editor
{
    public static class NotoFonts
    {
        private const string MenuPath = "Tools/Text/Build Noto Fonts";
        private const string SourceFolder = "Assets/AccountingImmersiveAnalytics/Text/Fonts/Noto";
        private const string AssetFolder = "Assets/Resources/Fonts/Noto";
        private const string DictionaryFolder = "Assets/AccountingImmersiveAnalytics/Text/Fonts/Dictionaries";
        private const string DefaultFamily = "NotoSans";
        private const string MonospaceFamily = "NotoSansMono";
        private const float BaseWeight = 500f;

        private static readonly string[] OnDemandFamilies =
        {
            "NotoSansArabic", "NotoSansHebrew", "NotoSansArmenian", "NotoSansGeorgian", "NotoSansEthiopic",
            "NotoSansDevanagari", "NotoSansBengali", "NotoSansGurmukhi", "NotoSansGujarati", "NotoSansOriya",
            "NotoSansTamil", "NotoSansTelugu", "NotoSansKannada", "NotoSansMalayalam", "NotoSansSinhala",
            "NotoSansThai", "NotoSansLao", "NotoSansKhmer", "NotoSansMyanmar",
            "NotoSansSC", "NotoSansTC", "NotoSansJP", "NotoSansKR", "NotoSansMath"
        };

        private static readonly Dictionary<string, string> LanguageOf = new Dictionary<string, string>
        {
            ["NotoSansSC"] = "zh-Hans",
            ["NotoSansTC"] = "zh-Hant",
            ["NotoSansJP"] = "ja",
            ["NotoSansKR"] = "ko"
        };

        [MenuItem(MenuPath)]
        public static void Build()
        {
            Directory.CreateDirectory(AssetFolder);
            Directory.CreateDirectory(DictionaryFolder);

            OneFontAsset main;
            var onDemand = new List<OneFontReference>();
            var paths = new List<string>();
            try
            {
                main = MakeFont(DefaultFamily, 0, paths);
                for (int i = 0; i < OnDemandFamilies.Length; i++)
                {
                    OneFontAsset asset = MakeFont(OnDemandFamilies[i], (i + 1f) / (OnDemandFamilies.Length + 1), paths);
                    if (asset != null) onDemand.Add(OneFontReferences.Make(asset, OneFontSourceKind.Resources));
                }
                MakeFont(MonospaceFamily, 1f, paths);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }
            if (main == null) return;

            long bytes = 0;
            foreach (string path in paths) bytes += new FileInfo(path).Length;

            List<OneTextDictionary> dictionaries = InstallDictionaries();

            OneTextSettings settings = OneTextSettingsProvider.GetOrCreate();
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_systemFontFallback").boolValue = false;
            SerializedProperty list = serialized.FindProperty("_dictionaries");
            list.arraySize = dictionaries.Count;
            for (int i = 0; i < dictionaries.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = dictionaries[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            settings.DefaultFont = main;
            settings.FontSource = OneFontSourceKind.Resources;
            settings.LoadOnDemand = true;
            settings.SetFallbackFonts(System.Array.Empty<OneFontAsset>());
            settings.SetOnDemandFonts(onDemand);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            OneTextSettings.Invalidate();

            Debug.Log($"[NotoFonts] Default font {DefaultFamily}, {onDemand.Count} fonts on demand, " +
                      $"{dictionaries.Count} word lists, system fonts off. Font assets total {bytes / 1048576f:F1} MB.");
        }

        private static OneFontAsset MakeFont(string family, float progress, List<string> paths)
        {
            EditorUtility.DisplayProgressBar("Building Noto fonts", family, progress);
            string source = $"{SourceFolder}/{family}.ttf";
            if (!File.Exists(source))
            {
                Debug.LogError($"[NotoFonts] Missing {source}.");
                return null;
            }

            string path = $"{AssetFolder}/{family}.asset";
            OneFontAsset asset = AssetDatabase.LoadAssetAtPath<OneFontAsset>(path);
            bool isNew = asset == null;
            if (isNew) asset = ScriptableObject.CreateInstance<OneFontAsset>();
            if (!OneFontAssetCreator.FillFromFontFile(asset, source))
            {
                Debug.LogError($"[NotoFonts] Could not read {source}.");
                if (isNew) Object.DestroyImmediate(asset);
                return null;
            }

            asset.SetBaseVariations(new[] { new FontVariation("wght", BaseWeight) });
            if (LanguageOf.TryGetValue(family, out string language)) asset.Language = language;
            if (isNew) AssetDatabase.CreateAsset(asset, path);
            else EditorUtility.SetDirty(asset);
            paths.Add(path);
            return asset;
        }

        private static List<OneTextDictionary> InstallDictionaries()
        {
            var made = new List<OneTextDictionary>();
            string folder = HubDictionariesTab.BundledWordListFolder();
            if (folder == null)
            {
                Debug.LogError("[NotoFonts] OneText's word lists were not found, so Thai, Lao, Khmer and Burmese wrap without them.");
                return made;
            }

            foreach ((string file, string script) in HubDictionariesTab.BundledLists)
            {
                string source = Path.Combine(folder, file);
                if (!File.Exists(source)) continue;

                string path = $"{DictionaryFolder}/{script}.asset";
                OneTextDictionary dictionary = AssetDatabase.LoadAssetAtPath<OneTextDictionary>(path);
                bool isNew = dictionary == null;
                if (isNew) dictionary = ScriptableObject.CreateInstance<OneTextDictionary>();
                dictionary.Initialize(File.ReadAllText(source, Encoding.UTF8), script, $"{file} (bundled with OneText)",
                    "Word list from the ICU project (Unicode licence).");
                if (isNew) AssetDatabase.CreateAsset(dictionary, path);
                else EditorUtility.SetDirty(dictionary);
                dictionary.Install();
                made.Add(dictionary);
            }
            AssetDatabase.SaveAssets();
            return made;
        }
    }
}
