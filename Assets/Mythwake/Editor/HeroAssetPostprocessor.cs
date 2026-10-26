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

        private void OnPreprocessTexture()
        {
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
