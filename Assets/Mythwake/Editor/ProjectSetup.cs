using System.IO;
using Mythwake.Game;
using Mythwake.Presentation;
using Mythwake.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Mythwake.Editor
{
    /// <summary>
    /// Reproducible project setup: player settings, URP, UI Toolkit panel, and the Main scene.
    /// Batch: Unity -batchmode -quit -executeMethod Mythwake.Editor.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        private const string SettingsDir = "Assets/Mythwake/Settings";
        private const string ScenePath = "Assets/Mythwake/Scenes/Main.unity";
        private const string StylePath = "Assets/Mythwake/Runtime/UI/Styles/Mythwake.uss";
        private const string ThemePath = "Assets/Mythwake/Runtime/UI/Styles/MythwakeTheme.tss";

        [MenuItem("Mythwake/Setup Project (settings, URP, scene)")]
        public static void Run()
        {
            Directory.CreateDirectory(SettingsDir);
            ConfigurePlayer();
            ConfigureSpine();
            ConfigureUrp();
            ContentCatalogBuilder.Rebuild();
            var panel = CreatePanelSettings();
            CreateScene(panel);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] done");
        }

        /// <summary>Batch entry that also renders portraits (needs graphics).</summary>
        public static void RunWithPortraits()
        {
            Run();
            PortraitRenderer.RenderAll();
            ContentCatalogBuilder.Rebuild();
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Mythwake";
            PlayerSettings.productName = "Mythwake";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.mythwake.game");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.mythwake.game");
            PlayerSettings.defaultScreenWidth = 1080 / 2;
            PlayerSettings.defaultScreenHeight = 1920 / 2;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
        }

        /// <summary>Our Spine exports use straight alpha. Creating the settings here also keeps spine-unity from creating them mid-import.</summary>
        private static void ConfigureSpine()
        {
            const string path = Spine.Unity.Editor.SpinePreferences.SPINE_SETTINGS_ASSET_PATH;
            var prefs = AssetDatabase.LoadAssetAtPath<Spine.Unity.Editor.SpinePreferences>(path);
            if (prefs == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                prefs = ScriptableObject.CreateInstance<Spine.Unity.Editor.SpinePreferences>();
                AssetDatabase.CreateAsset(prefs, path);
            }
            var preset = AssetDatabase.FindAssets("StraightAlphaPreset t:Preset");
            if (preset.Length > 0)
                prefs.textureSettingsReference = AssetDatabase.GUIDToAssetPath(preset[0]);
            EditorUtility.SetDirty(prefs);
        }

        private static void ConfigureUrp()
        {
            var assetPath = SettingsDir + "/Mythwake_URP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, SettingsDir + "/Mythwake_URP_Renderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, assetPath);
            }
            // 2D battle: no shadows, no HDR, light MSAA for smooth Spine edges
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 2;
            pipeline.shadowDistance = 0f;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            var current = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        private static PanelSettings CreatePanelSettings()
        {
            if (!File.Exists(ThemePath))
            {
                File.WriteAllText(ThemePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(ThemePath);
            }
            var path = SettingsDir + "/Mythwake_Panel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, path);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1080, 1920);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.clearColor = false;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        private static void CreateScene(PanelSettings panel)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.6f;
            cam.transform.position = new Vector3(0f, 0.6f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.12f);
            camGo.AddComponent<UniversalAdditionalCameraData>();

            var background = new GameObject("Background");
            var sr = background.AddComponent<SpriteRenderer>();
            sr.sprite = BattleBackground();
            sr.sortingOrder = -10000;
            background.transform.position = new Vector3(0f, 1.4f, 5f); // horizon stays just above the back row

            var battleGo = new GameObject("Battle");
            var battle = battleGo.AddComponent<BattleView>();
            var so = new SerializedObject(battle);
            so.FindProperty("cam").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();

            var uiGo = new GameObject("UI");
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = panel;

            var appGo = new GameObject("App");
            var app = appGo.AddComponent<AppRoot>();
            var appSo = new SerializedObject(app);
            appSo.FindProperty("catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ContentCatalog>(ContentCatalogBuilder.CatalogPath);
            appSo.FindProperty("document").objectReferenceValue = doc;
            appSo.FindProperty("battle").objectReferenceValue = battle;
            appSo.FindProperty("style").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            appSo.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// <summary>
        /// Placeholder battlefield until the team delivers background art: dusk sky, distant hills, a forest line and
        /// a clearing for the fight. 8 × 16 world units, horizon just above the back row.
        /// </summary>
        private static Sprite BattleBackground()
        {
            var path = SettingsDir + "/BattleBackground.png";
            if (!File.Exists(path))
            {
                const int w = 256, h = 512;
                const float horizon = 0.556f;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var skyTop = new Color(0.09f, 0.07f, 0.20f);
                var skyLow = new Color(0.58f, 0.34f, 0.42f);
                var hills = new Color(0.22f, 0.17f, 0.31f);
                var trees = new Color(0.07f, 0.10f, 0.13f);
                var grass = new Color(0.13f, 0.21f, 0.16f);
                var deep = new Color(0.035f, 0.05f, 0.06f);
                for (var y = 0; y < h; y++)
                {
                    var t = y / (float)(h - 1);
                    for (var x = 0; x < w; x++)
                    {
                        var u = x / (float)(w - 1);
                        Color c;
                        var hillLine = horizon + 0.06f + 0.025f * Mathf.Sin(u * 7.1f + 1.3f) + 0.015f * Mathf.Sin(u * 17.3f);
                        var treeLine = horizon + 0.012f + 0.012f * Mathf.Abs(Mathf.Sin(u * 61f)) + 0.008f * Mathf.Sin(u * 23f + 0.7f);
                        if (t > hillLine)
                        {
                            c = Color.Lerp(skyLow, skyTop, Mathf.Pow((t - horizon) / (1f - horizon), 0.7f));
                            // faint purple glow of the Rift low in the sky
                            var gx = (u - 0.62f) / 0.35f;
                            var gy = (t - horizon - 0.05f) / 0.12f;
                            c += new Color(0.25f, 0.08f, 0.35f) * Mathf.Clamp01(1f - (gx * gx + gy * gy)) * 0.6f;
                        }
                        else if (t > treeLine)
                        {
                            c = Color.Lerp(hills, skyLow, Mathf.Clamp01((t - treeLine) / 0.12f) * 0.35f);
                        }
                        else if (t > horizon)
                        {
                            c = trees;
                        }
                        else
                        {
                            c = Color.Lerp(deep, grass, Mathf.Pow(t / horizon, 1.4f));
                            // lighter clearing where the fight happens
                            var cx = (u - 0.5f) / 0.55f;
                            var cy = (t - 0.4f) / 0.12f;
                            c += new Color(0.05f, 0.06f, 0.04f) * Mathf.Clamp01(1f - (cx * cx + cy * cy));
                        }
                        c.a = 1f;
                        tex.SetPixel(x, y, c);
                    }
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
