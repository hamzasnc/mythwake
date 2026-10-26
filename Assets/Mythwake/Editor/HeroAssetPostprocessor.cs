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

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(HeroesRoot))
                return;
            var importer = (TextureImporter)assetImporter;
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
                if (path.StartsWith(HeroesRoot) && path.EndsWith("_Material.mat"))
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
