using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mythwake.Editor
{
    /// <summary>Command-line builds. Unity -batchmode -quit -executeMethod Mythwake.Editor.BuildScript.Windows</summary>
    public static class BuildScript
    {
        [MenuItem("Mythwake/Build/Windows (dev)")]
        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Mythwake.exe");

        [MenuItem("Mythwake/Build/Android (dev)")]
        public static void Android() => Build(BuildTarget.Android, "Builds/Android/Mythwake.apk");

        private static void Build(BuildTarget target, string output)
        {
            ContentCatalogBuilder.Rebuild();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Mythwake/Scenes/Main.unity" },
                locationPathName = output,
                target = target,
                options = BuildOptions.Development
            });
            var summary = report.summary;
            Debug.Log($"[Build] {target}: {summary.result}, {summary.totalErrors} errors, {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime}");
            if (summary.result != BuildResult.Succeeded && Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }
}
