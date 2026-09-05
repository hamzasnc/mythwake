#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Keeps the generated shop artwork in a predictable UI-ready format. The source
/// icons are imported as centered 2D sprites and packed into one atlas so cards can
/// use them without creating a texture/material per offer.
/// </summary>
public static class MythwakeShopIconAssetBuilder
{
    private const string IconFolder = "Assets/_Mythwake/Resources/Mythwake/UI/Shop/Icons";
    private const string AtlasPath = "Assets/_Mythwake/UI/Shop/ShopIcons.spriteatlas";

    [MenuItem("Mythwake/Shop/Configure Shop Icons")]
    public static void Configure()
    {
        if (!Directory.Exists(IconFolder))
        {
            Debug.LogWarning($"Shop icon folder is missing: {IconFolder}");
            return;
        }

        var iconGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder });
        var iconObjects = new Object[iconGuids.Length];
        for (var i = 0; i < iconGuids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(iconGuids[i]);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            iconObjects[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
        if (atlas == null)
        {
            var directory = Path.GetDirectoryName(AtlasPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            atlas = new SpriteAtlas();
            var packing = atlas.GetPackingSettings();
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.padding = 4;
            atlas.SetPackingSettings(packing);
            var textureSettings = atlas.GetTextureSettings();
            textureSettings.generateMipMaps = false;
            textureSettings.readable = false;
            atlas.SetTextureSettings(textureSettings);
            AssetDatabase.CreateAsset(atlas, AtlasPath);
        }

        atlas.Remove(iconObjects);
        atlas.Add(iconObjects);
        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Configured {iconObjects.Length} Mythwake shop UI sprites and packed {AtlasPath}.");
    }
}
#endif
