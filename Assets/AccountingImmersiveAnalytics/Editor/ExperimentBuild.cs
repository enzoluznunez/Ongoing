using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Editor
{
    public static class ExperimentBuild
    {
        private const string ProfilePath = "Assets/Settings/Build Profiles/Quest Default.asset";
        private const string ApkPath = "Builds/Experiments.apk";

        public static void Build()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
            {
                buildProfile = profile,
                locationPathName = ApkPath
            });
            Debug.Log($"[ExperimentBuild] {report.summary.result}, {report.summary.totalErrors} errors, {report.summary.outputPath}");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
