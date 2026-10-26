using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace Mythwake.Editor
{
    /// <summary>
    /// Builds the UI font assets from Content/UI/Fonts (Google Fonts, SIL OFL, docs/UI_KIT.md): Baloo 2 for body text,
    /// Lilita One for titles, buttons and numbers. Dynamic atlases with every character the game uses baked in.
    /// Unity -batchmode -quit -executeMethod Mythwake.Editor.UiFontBuilder.Build
    /// </summary>
    public static class UiFontBuilder
    {
        private const string Folder = "Assets/Mythwake/Content/UI/Fonts/";
        public const string StylePath = "Assets/Mythwake/Runtime/UI/Styles/Mythwake.uss";

        /// <summary>Every character of the German texts and the UI code (scanned when the fonts were added).</summary>
        private const string Characters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?+-%/()[]'\"#&=<>_*@·ÄÖ×Üßäöü–…›→−▶◀";

        [MenuItem("Mythwake/Build UI Fonts")]
        public static void Build()
        {
            var body = Make("Baloo2");
            var display = Make("LilitaOne");
            // Lilita One has no arrows: Baloo 2 steps in for missing characters
            if (body != null && display != null)
            {
                display.fallbackFontAssetTable = new System.Collections.Generic.List<FontAsset> { body };
                EditorUtility.SetDirty(display);
            }
            AssetDatabase.SaveAssets();
            // stylesheets resolve the font reference when they are imported
            AssetDatabase.ImportAsset(StylePath, ImportAssetOptions.ForceUpdate);
        }

        private static FontAsset Make(string name)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(Folder + name + ".ttf");
            if (font == null)
            {
                Debug.LogError($"[Font] {Folder}{name}.ttf missing");
                return null;
            }
            var path = Folder + name + "SDF.asset";
            AssetDatabase.DeleteAsset(path);
            var asset = FontAsset.CreateFontAsset(font, 90, 12, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            asset.name = name + " SDF";
            AssetDatabase.CreateAsset(asset, path);
            // the atlas texture and material live inside the font asset
            foreach (var texture in asset.atlasTextures)
                if (texture != null)
                {
                    texture.name = name + " Atlas";
                    AssetDatabase.AddObjectToAsset(texture, asset);
                }
            if (asset.material != null)
            {
                asset.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            var ok = asset.TryAddCharacters(Characters, out var missing);
            EditorUtility.SetDirty(asset);
            Debug.Log($"[Font] {path}: {asset.glyphTable.Count} glyphs" + (ok ? "" : $", missing '{missing}'"));
            return asset;
        }
    }
}
