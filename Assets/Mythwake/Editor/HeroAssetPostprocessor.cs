using UnityEditor;
using UnityEngine;

namespace Mythwake.Editor
{
    /// <summary>
    /// Import settings for hero content. Spine atlases are exported with straight alpha, so textures stay sRGB with
    /// alpha-is-transparency and the materials read straight alpha. Portraits and cut-ins are plain UI textures.
    /// </summary>
    public sealed class HeroAssetPostprocessor : AssetPostprocessor
    {
        public const string HeroesRoot = "Assets/Mythwake/Content/Heroes/";
        public const string MonstersRoot = "Assets/Mythwake/Content/Monsters/";
        public const string TownRoot = "Assets/Mythwake/Content/Town/";
        public const string IconsRoot = "Assets/Mythwake/Content/Icons/";
        public const string BackgroundsRoot = "Assets/Mythwake/Content/Backgrounds/";
        public const string StoryRoot = "Assets/Mythwake/Content/Story/";
        public const string UiRoot = "Assets/Mythwake/Content/UI/";

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(IconsRoot))
            {
                var icon = (TextureImporter)assetImporter;
                icon.textureType = TextureImporterType.Default;
                icon.alphaIsTransparency = true;
                icon.mipmapEnabled = false;
                icon.wrapMode = TextureWrapMode.Clamp;
                icon.npotScale = TextureImporterNPOTScale.None;
                icon.maxTextureSize = 512;
                icon.textureCompression = TextureImporterCompression.CompressedHQ;
                return;
            }
            if (assetPath.StartsWith(BackgroundsRoot))
            {
                // battle backgrounds are world sprites behind the fight (BattleView scales them to the camera)
                var bg = (TextureImporter)assetImporter;
                bg.textureType = TextureImporterType.Sprite;
                bg.spriteImportMode = SpriteImportMode.Single;
                bg.spritePixelsPerUnit = 128;
                bg.mipmapEnabled = false;
                bg.wrapMode = TextureWrapMode.Clamp;
                bg.maxTextureSize = 2048;
                bg.textureCompression = TextureImporterCompression.CompressedHQ;
                return;
            }
            if (assetPath.StartsWith(UiRoot))
            {
                // UI kit: icons, 9-slice frames and screen backgrounds, referenced from Mythwake.uss
                var kit = (TextureImporter)assetImporter;
                kit.textureType = TextureImporterType.Default;
                kit.alphaIsTransparency = true;
                kit.mipmapEnabled = false;
                kit.wrapMode = TextureWrapMode.Clamp;
                kit.npotScale = TextureImporterNPOTScale.None;
                kit.maxTextureSize = assetPath.Contains("/Backgrounds/") ? 2048 : 1024;
                kit.textureCompression = TextureImporterCompression.CompressedHQ;
                return;
            }
            if (assetPath.StartsWith(StoryRoot))
            {
                var story = (TextureImporter)assetImporter;
                story.textureType = TextureImporterType.Default;
                story.mipmapEnabled = false;
                story.wrapMode = TextureWrapMode.Clamp;
                story.npotScale = TextureImporterNPOTScale.None;
                story.maxTextureSize = 2048;
                story.textureCompression = TextureImporterCompression.CompressedHQ;
                return;
            }
            if (assetPath.StartsWith(TownRoot))
            {
                // hub pictures and building cut-outs are UI images
                var town = (TextureImporter)assetImporter;
                town.textureType = TextureImporterType.Default;
                town.alphaIsTransparency = true;
                town.mipmapEnabled = false;
                town.wrapMode = TextureWrapMode.Clamp;
                town.npotScale = TextureImporterNPOTScale.None;
                town.maxTextureSize = assetPath.Contains("/Buildings/") || assetPath.EndsWith("ruin.png") ? 1024 : 2048;
                town.textureCompression = TextureImporterCompression.CompressedHQ;
                return;
            }
            if (!assetPath.StartsWith(HeroesRoot) && !assetPath.StartsWith(MonstersRoot))
                return;
            var importer = (TextureImporter)assetImporter;
            if (assetPath.EndsWith("_sprite.png"))
            {
                // monster stand-in art before its rig: a plain sprite with the pivot at the feet
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 400;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.SetTextureSettings(settings);
                return;
            }
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            var ui = assetPath.EndsWith("_portrait.png") || assetPath.EndsWith("_cutin.png") || assetPath.EndsWith("_full.png");
            importer.maxTextureSize = ui ? 1024 : 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
                if ((path.StartsWith(HeroesRoot) || path.StartsWith(MonstersRoot)) && path.EndsWith("_Material.mat"))
                    FixMaterial(AssetDatabase.LoadAssetAtPath<Material>(path));
        }

        public static void FixMaterial(Material material)
        {
            if (material == null || !material.HasProperty("_StraightAlphaInput"))
                return;
            if (material.GetFloat("_StraightAlphaInput") > 0.5f)
                return;
            material.SetFloat("_StraightAlphaInput", 1f);
            material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
            EditorUtility.SetDirty(material);
        }
    }
}
