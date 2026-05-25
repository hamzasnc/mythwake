using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

public static class PaladinSpineValidation
{
    private const string SkeletonDataAssetPath = "Assets/_Mythwake/ArtSource/Generated/paladin_spine/spine_export/hero_paladin_spine_SkeletonData.asset";
    private const string TransitionMixPath = "Assets/_Mythwake/ArtSource/Generated/paladin_spine/hero_paladin_spine_transition_mixes.json";
    private const string PaladinHeroId = "hero_paladin";
    private const string StandardSummonBannerId = "hero_shard_standard";
    private const string VanguardSummonBannerId = "hero_shard_vanguard";
    private const string ClientControllerPath = "Assets/_Mythwake/Scripts/IdlePrototypeController.cs";
    private const string BackendDefinitionsPath = "backend/internal/balance/definitions.go";
    private const string BackendPaladinMigrationPath = "backend/internal/database/migrations/0025_paladin_hero_definition.sql";
    private const float FloatTolerance = 0.0001f;

    private static readonly string[] RequiredPaladinTexturePaths =
    {
        "Assets/_Mythwake/Resources/Mythwake/Art/Runtime/hero_paladin.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/CombatAnimated/hero_paladin_sheet_alpha.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/CombatAnimated/hero_paladin_abilities_sheet_alpha.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/CombatAnimated/hero_paladin_death_sheet_alpha.png"
    };

    private static readonly string[] RequiredPaladinSkeletalPartPaths =
    {
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/arm_sword.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/belt_gem.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/cape_back.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/fx_holy_barrier.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/fx_shield_flash.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/fx_sword_slash.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/head_helmet.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/leg_left.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/leg_right.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/shadow_holy_ring.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/shield.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/sword.png",
        "Assets/_Mythwake/Resources/Mythwake/Art/Skeletal/Paladin/parts/torso_armor.png"
    };

    private static readonly string[] RequiredPaladinLocalizationKeys =
    {
        "hero.hero_paladin.name",
        "hero.hero_paladin.title",
        "hero.hero_paladin.description",
        "hero.hero_paladin.ability.name",
        "hero.hero_paladin.ability.description"
    };

    private static readonly string[] RequiredClientRuntimeHookSnippets =
    {
        "private PaladinSkeletalCombatView[] formationHeroPaladinViews;",
        "private PaladinSkeletalCombatView[] fightHeroPaladinViews;",
        "formationHeroPaladinViews[i] = PaladinSkeletalCombatView.Create",
        "fightHeroPaladinViews[i] = PaladinSkeletalCombatView.Create",
        "TryApplyPaladinSkeletalFightPose",
        "PaladinSkeletalCombatView.Clip.Death"
    };

    private static readonly string[] RequiredBackendDefinitionSnippets =
    {
        "{ID: \"hero_paladin\", DisplayName: \"Paladin\"",
        "{HeroID: \"hero_paladin\", Shards: 1"
    };

    private static readonly string[] RequiredBackendMigrationSnippets =
    {
        "('hero_paladin', 'Paladin', 60, true)",
        "WHERE id = 'hero_paladin';",
        "('hero_shard_standard', 'hero_paladin', 1, 60, 'reward_summon_shards')"
    };

    [MenuItem("Mythwake/Validate Paladin Spine Handoff")]
    public static void RunPaladinSpineValidation()
    {
        var skeletonDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(SkeletonDataAssetPath);
        if (skeletonDataAsset == null)
        {
            throw new InvalidOperationException($"Missing Paladin SkeletonDataAsset at {SkeletonDataAssetPath}");
        }

        skeletonDataAsset.Clear();
        var skeletonData = skeletonDataAsset.GetSkeletonData(false);
        if (skeletonData == null)
        {
            throw new InvalidOperationException("Paladin SkeletonDataAsset did not load through spine-unity.");
        }

        var mixFile = LoadMixFile();
        ValidateAnimationSet(skeletonData, mixFile.animation_set);
        ValidateRequiredBones(skeletonData);
        ValidateMixTable(skeletonData, skeletonDataAsset, mixFile);

        Debug.Log($"Paladin Spine handoff validated: {mixFile.animation_set.Length} animations, {mixFile.recommended_spine_mixes_seconds.Length} custom mixes.");
    }

    [MenuItem("Mythwake/Validate Paladin Integration")]
    public static void RunPaladinIntegrationValidation()
    {
        var paladinHeroIndex = ValidateClientHeroDefinition();
        ValidateClientSummonBanners(paladinHeroIndex);
        ValidateClientRuntimeHooks();
        ValidateBackendDefinitions();
        ValidateLocalizationTable("English");
        ValidateLocalizationTable("German");
        ValidateTextureAssets(RequiredPaladinTexturePaths);
        ValidateTextureAssets(RequiredPaladinSkeletalPartPaths);
        ValidatePaladinSkeletalRuntimeView();

        Debug.Log("Paladin integration validated: client definition, local summon banners, formation/fight hooks, backend definition anchors, localization, and runtime assets.");
    }

    private static int ValidateClientHeroDefinition()
    {
        var heroDefinitions = GetStaticArray(typeof(IdlePrototypeController), "HeroDefinitions");
        for (var heroIndex = 0; heroIndex < heroDefinitions.Length; heroIndex++)
        {
            var hero = heroDefinitions.GetValue(heroIndex);
            if (GetInstanceField<string>(hero, "heroId") != PaladinHeroId)
            {
                continue;
            }

            ValidateEqual("Paladin hero name", "Paladin", GetInstanceField<string>(hero, "name"));
            ValidateEqual("Paladin role", "tank", GetInstanceField<string>(hero, "roleId"));
            ValidateEqual("Paladin rarity", "epic", GetInstanceField<string>(hero, "rarityId"));

            if (GetInstanceField<int>(hero, "summonShardReward") <= 0)
            {
                throw new InvalidOperationException("Paladin summon shard reward must be greater than zero.");
            }

            return heroIndex;
        }

        throw new InvalidOperationException("Paladin hero definition is missing from IdlePrototypeController.");
    }

    private static void ValidateClientSummonBanners(int paladinHeroIndex)
    {
        var summonBanners = GetStaticArray(typeof(IdlePrototypeController), "LocalSummonBanners");
        var standardBanner = FindBanner(summonBanners, StandardSummonBannerId);
        var vanguardBanner = FindBanner(summonBanners, VanguardSummonBannerId);

        ValidateBannerRateContainsHero(standardBanner, paladinHeroIndex, "epic", StandardSummonBannerId);
        ValidateFeaturedBannerContainsHero(vanguardBanner, paladinHeroIndex, VanguardSummonBannerId);
        ValidateBannerRateContainsHero(vanguardBanner, paladinHeroIndex, "epic", VanguardSummonBannerId);
    }

    private static object FindBanner(Array summonBanners, string bannerId)
    {
        foreach (var banner in summonBanners)
        {
            if (GetInstanceField<string>(banner, "bannerId") == bannerId)
            {
                return banner;
            }
        }

        throw new InvalidOperationException($"Summon banner is missing: {bannerId}");
    }

    private static void ValidateFeaturedBannerContainsHero(object banner, int heroIndex, string bannerId)
    {
        var featuredHeroIndexes = GetInstanceField<int[]>(banner, "featuredHeroIndexes");
        if (Array.IndexOf(featuredHeroIndexes, heroIndex) < 0)
        {
            throw new InvalidOperationException($"Paladin is not featured by local summon banner {bannerId}.");
        }
    }

    private static void ValidateBannerRateContainsHero(object banner, int heroIndex, string rarityId, string bannerId)
    {
        var rates = GetInstanceField<Array>(banner, "rates");
        foreach (var rate in rates)
        {
            if (GetInstanceField<string>(rate, "rarityId") != rarityId)
            {
                continue;
            }

            var heroIndexes = GetInstanceField<int[]>(rate, "heroIndexes");
            if (Array.IndexOf(heroIndexes, heroIndex) >= 0)
            {
                return;
            }
        }

        throw new InvalidOperationException($"Paladin is missing from the {rarityId} rate pool on local summon banner {bannerId}.");
    }

    private static void ValidateLocalizationTable(string tableFieldName)
    {
        var table = GetStaticField<IDictionary>(typeof(MythwakeLocalization), tableFieldName);
        foreach (var key in RequiredPaladinLocalizationKeys)
        {
            if (!table.Contains(key) || string.IsNullOrWhiteSpace(table[key] as string))
            {
                throw new InvalidOperationException($"Paladin localization key is missing from {tableFieldName}: {key}");
            }
        }
    }

    private static void ValidateBackendDefinitions()
    {
        ValidateFileContainsSnippets(BackendDefinitionsPath, RequiredBackendDefinitionSnippets);
        ValidateFileContainsSnippets(BackendPaladinMigrationPath, RequiredBackendMigrationSnippets);
    }

    private static void ValidateClientRuntimeHooks()
    {
        ValidateFileContainsSnippets(ClientControllerPath, RequiredClientRuntimeHookSnippets);
    }

    private static void ValidateFileContainsSnippets(string relativePath, string[] requiredSnippets)
    {
        var fullPath = GetProjectFilePath(relativePath);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Required Paladin backend file is missing: {relativePath}");
        }

        var text = File.ReadAllText(fullPath);
        foreach (var snippet in requiredSnippets)
        {
            if (!text.Contains(snippet))
            {
                throw new InvalidOperationException($"Required Paladin backend snippet is missing from {relativePath}: {snippet}");
            }
        }
    }

    private static string GetProjectFilePath(string relativePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new InvalidOperationException("Could not resolve Unity project root from Application.dataPath.");
        }

        return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void ValidateTextureAssets(string[] assetPaths)
    {
        foreach (var assetPath in assetPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) == null)
            {
                throw new InvalidOperationException($"Paladin texture asset is missing or not imported as Texture2D: {assetPath}");
            }
        }
    }

    private static void ValidatePaladinSkeletalRuntimeView()
    {
        var root = new GameObject("Paladin Runtime Validation Root", typeof(RectTransform));
        try
        {
            var view = PaladinSkeletalCombatView.Create(root.transform, "Paladin Runtime Validation View", Vector2.zero, 0.66f);
            if (view == null)
            {
                throw new InvalidOperationException("Paladin skeletal runtime view could not be created.");
            }

            var parts = GetInstanceField<object>(view, "parts");
            foreach (var assetPath in RequiredPaladinSkeletalPartPaths)
            {
                var partName = GetAssetNameWithoutExtension(assetPath);
                if (!DictionaryContainsKey(parts, partName))
                {
                    throw new InvalidOperationException($"Paladin skeletal runtime view did not load part: {partName}");
                }
            }

            view.ShowPreview(Vector2.zero, 1f, 1f);
            view.ApplyCombatPose(PaladinSkeletalCombatView.Clip.Attack1, Vector2.zero, 0f, 0.36f, 1f, 1f, Color.white, true, true, new Vector2(120f, -80f), 0f);
            view.ApplyCombatPose(PaladinSkeletalCombatView.Clip.Attack2, Vector2.zero, 0f, 0.48f, 1f, 1f, Color.white, true, true, new Vector2(120f, -80f), 0.5f);
            view.ApplyCombatPose(PaladinSkeletalCombatView.Clip.Death, Vector2.zero, 0f, 0f, 1f, 1f, Color.white, true, false, Vector2.zero, 0f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static PaladinTransitionMixFile LoadMixFile()
    {
        var mixAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(TransitionMixPath);
        if (mixAsset == null)
        {
            throw new InvalidOperationException($"Missing Paladin transition mix file at {TransitionMixPath}");
        }

        var mixFile = JsonUtility.FromJson<PaladinTransitionMixFile>(mixAsset.text);
        if (mixFile == null || mixFile.animation_set == null || mixFile.recommended_spine_mixes_seconds == null)
        {
            throw new InvalidOperationException("Paladin transition mix JSON could not be parsed.");
        }

        return mixFile;
    }

    private static void ValidateAnimationSet(SkeletonData skeletonData, string[] animationSet)
    {
        foreach (var animationName in animationSet)
        {
            if (skeletonData.FindAnimation(animationName) == null)
            {
                throw new InvalidOperationException($"Paladin Spine animation missing: {animationName}");
            }
        }
    }

    private static void ValidateRequiredBones(SkeletonData skeletonData)
    {
        var requiredBones = new[]
        {
            "root",
            "hips",
            "chest",
            "head",
            "arm_sword",
            "sword",
            "shield",
            "fx_sword_slash",
            "fx_shield_flash",
            "fx_holy_barrier"
        };

        foreach (var boneName in requiredBones)
        {
            if (skeletonData.FindBone(boneName) == null)
            {
                throw new InvalidOperationException($"Paladin Spine bone missing: {boneName}");
            }
        }
    }

    private static void ValidateMixTable(SkeletonData skeletonData, SkeletonDataAsset skeletonDataAsset, PaladinTransitionMixFile mixFile)
    {
        if (skeletonDataAsset.fromAnimation.Length != skeletonDataAsset.toAnimation.Length ||
            skeletonDataAsset.fromAnimation.Length != skeletonDataAsset.duration.Length)
        {
            throw new InvalidOperationException("Paladin SkeletonDataAsset custom mix arrays have mismatched lengths.");
        }

        if (Mathf.Abs(skeletonDataAsset.defaultMix - mixFile.spine_unity_default_mix_seconds) > FloatTolerance)
        {
            throw new InvalidOperationException($"Paladin default mix mismatch: asset={skeletonDataAsset.defaultMix}, json={mixFile.spine_unity_default_mix_seconds}");
        }

        var assetMixes = new Dictionary<string, float>();
        for (var i = 0; i < skeletonDataAsset.fromAnimation.Length; i++)
        {
            var from = skeletonDataAsset.fromAnimation[i];
            var to = skeletonDataAsset.toAnimation[i];
            if (skeletonData.FindAnimation(from) == null || skeletonData.FindAnimation(to) == null)
            {
                throw new InvalidOperationException($"Paladin SkeletonDataAsset references unknown mix animation: {from} -> {to}");
            }

            assetMixes[$"{from}>{to}"] = skeletonDataAsset.duration[i];
        }

        if (assetMixes.Count != mixFile.recommended_spine_mixes_seconds.Length)
        {
            throw new InvalidOperationException($"Paladin custom mix count mismatch: asset={assetMixes.Count}, json={mixFile.recommended_spine_mixes_seconds.Length}");
        }

        foreach (var mix in mixFile.recommended_spine_mixes_seconds)
        {
            var key = $"{mix.from}>{mix.to}";
            if (!assetMixes.TryGetValue(key, out var assetDuration))
            {
                throw new InvalidOperationException($"Paladin SkeletonDataAsset missing custom mix: {mix.from} -> {mix.to}");
            }

            if (Mathf.Abs(assetDuration - mix.duration) > FloatTolerance)
            {
                throw new InvalidOperationException($"Paladin custom mix duration mismatch for {key}: asset={assetDuration}, json={mix.duration}");
            }
        }
    }

    private static Array GetStaticArray(Type type, string fieldName)
    {
        var value = GetStaticField<object>(type, fieldName);
        if (value is Array array)
        {
            return array;
        }

        throw new InvalidOperationException($"{type.Name}.{fieldName} is not an array.");
    }

    private static T GetStaticField<T>(Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null)
        {
            throw new InvalidOperationException($"{type.Name}.{fieldName} could not be found.");
        }

        var value = field.GetValue(null);
        if (value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidOperationException($"{type.Name}.{fieldName} has unexpected type {field.FieldType.Name}.");
    }

    private static T GetInstanceField<T>(object instance, string fieldName)
    {
        if (instance == null)
        {
            throw new InvalidOperationException($"Cannot read field {fieldName} from a null instance.");
        }

        var type = instance.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            throw new InvalidOperationException($"{type.Name}.{fieldName} could not be found.");
        }

        var value = field.GetValue(instance);
        if (value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidOperationException($"{type.Name}.{fieldName} has unexpected type {field.FieldType.Name}.");
    }

    private static void ValidateEqual(string label, string expected, string actual)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"{label} mismatch: expected={expected}, actual={actual}");
        }
    }

    private static bool DictionaryContainsKey(object dictionary, string key)
    {
        var containsKey = dictionary.GetType().GetMethod("ContainsKey", new[] { typeof(string) });
        if (containsKey == null)
        {
            throw new InvalidOperationException($"{dictionary.GetType().Name}.ContainsKey(string) could not be found.");
        }

        return containsKey.Invoke(dictionary, new object[] { key }) is true;
    }

    private static string GetAssetNameWithoutExtension(string assetPath)
    {
        var fileNameStart = assetPath.LastIndexOf('/') + 1;
        var fileName = fileNameStart > 0 ? assetPath.Substring(fileNameStart) : assetPath;
        var extensionStart = fileName.LastIndexOf('.');
        return extensionStart > 0 ? fileName.Substring(0, extensionStart) : fileName;
    }

    [Serializable]
    private sealed class PaladinTransitionMixFile
    {
        public string hero_id;
        public string[] animation_set;
        public float spine_unity_default_mix_seconds;
        public PaladinTransitionMix[] recommended_spine_mixes_seconds;
    }

    [Serializable]
    private sealed class PaladinTransitionMix
    {
        public string from;
        public string to;
        public float duration;
    }
}
