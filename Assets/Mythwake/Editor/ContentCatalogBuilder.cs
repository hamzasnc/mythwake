using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mythwake.Game;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace Mythwake.Editor
{
    /// <summary>Scans Content/ and writes the ContentCatalog the game loads at runtime. Run after adding or changing heroes.</summary>
    public static class ContentCatalogBuilder
    {
        public const string ContentRoot = "Assets/Mythwake/Content";
        public const string CatalogPath = ContentRoot + "/ContentCatalog.asset";

        [MenuItem("Mythwake/Rebuild Content Catalog")]
        public static void Rebuild()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ContentCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ContentCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var data = Directory.GetFiles(ContentRoot + "/Data", "*.json")
                .Select(p => p.Replace('\\', '/'))
                .OrderBy(p => p, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<TextAsset>)
                .Where(t => t != null)
                .ToList();
            catalog.dataFiles = data.Where(t => !t.name.StartsWith("strings.")).ToArray();
            catalog.strings = data.Where(t => t.name.StartsWith("strings.")).ToArray();

            var heroes = new List<ContentCatalog.HeroAssets>();
            foreach (var dir in Directory.GetDirectories(ContentRoot + "/Heroes").OrderBy(d => d, System.StringComparer.Ordinal))
            {
                var id = Path.GetFileName(dir);
                var folder = dir.Replace('\\', '/');
                var entry = new ContentCatalog.HeroAssets
                {
                    id = id,
                    definition = AssetDatabase.LoadAssetAtPath<TextAsset>($"{folder}/{id}.hero.json"),
                    skeleton = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>($"{folder}/{id}_SkeletonData.asset"),
                    portrait = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{id}_portrait.png"),
                    full = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{id}_full.png"),
                    cutIn = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{id}_cutin.png")
                };
                if (entry.definition == null)
                {
                    Debug.LogWarning($"[Content] {id}: no {id}.hero.json, skipped");
                    continue;
                }
                if (entry.skeleton == null)
                    Debug.LogWarning($"[Content] {id}: no SkeletonData asset");
                foreach (var mat in Directory.GetFiles(folder, "*_Material.mat"))
                    HeroAssetPostprocessor.FixMaterial(AssetDatabase.LoadAssetAtPath<Material>(mat.Replace('\\', '/')));
                heroes.Add(entry);
            }
            catalog.heroes = heroes.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            // validate by loading exactly as the game does
            var content = catalog.LoadContent();
            Debug.Log($"[Content] catalog rebuilt: {catalog.dataFiles.Length} data files, {content.Heroes.Count} heroes, " +
                      $"{heroes.Count(h => h.portrait != null)} portraits, {heroes.Count(h => h.cutIn != null)} cut-ins");
        }
    }
}
