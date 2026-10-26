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
        public static void Android()
        {
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.mythwake.game");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            // phones are ARM64; emulators like MuMu run x86_64
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            // dev builds talk to the local server over plain HTTP; release builds must use HTTPS
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            Build(BuildTarget.Android, "Builds/Android/Mythwake.apk");
        }

        public const string AppIconPath = "Assets/Mythwake/Content/UI/AppIcon.png";

        private static void Build(BuildTarget target, string output)
        {
            ContentCatalogBuilder.Rebuild();
            // app icon from the UI kit (docs/UI_KIT.md)
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon != null)
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
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
