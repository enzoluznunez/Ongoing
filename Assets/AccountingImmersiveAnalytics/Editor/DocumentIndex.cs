using System;
using System.IO;
using System.Linq;
using AccountingImmersiveAnalytics.Documents;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Editor
{
    public sealed class DocumentIndex : AssetPostprocessor, IPreprocessBuildWithReport
    {
        private const string StreamingAsset = "Assets/StreamingAssets/";
        private const string FolderAsset = StreamingAsset + DocumentLibrary.Folder + "/";
        private static string FolderPath => Path.Combine(Application.streamingAssetsPath, DocumentLibrary.Folder);

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Write();

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsDocument)) Write();
        }

        private static bool IsDocument(string path) =>
            path.StartsWith(FolderAsset, StringComparison.Ordinal) && DocumentLibrary.Listed(path);

        private static string Quote(string name) => "  \"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        [MenuItem("Tools/Documents/Write Document Index")]
        public static void Write()
        {
            Directory.CreateDirectory(FolderPath);
            string[] names = Directory.GetFiles(FolderPath).Select(Path.GetFileName).Where(DocumentLibrary.Listed)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
            string path = Path.Combine(Application.streamingAssetsPath, DocumentLibrary.IndexFile);
            string json = "[\n" + string.Join(",\n", names.Select(Quote)) + "\n]";
            if (File.Exists(path) && File.ReadAllText(path) == json) return;
            File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(StreamingAsset + DocumentLibrary.IndexFile);
        }
    }
}
