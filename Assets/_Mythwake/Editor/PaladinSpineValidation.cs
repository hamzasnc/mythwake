using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

public static class PaladinSpineValidation
{
    private const string SkeletonDataAssetPath = "Assets/_Mythwake/ArtSource/Generated/paladin_spine/spine_export/hero_paladin_spine_SkeletonData.asset";
    private const string TransitionMixPath = "Assets/_Mythwake/ArtSource/Generated/paladin_spine/hero_paladin_spine_transition_mixes.json";
    private const float FloatTolerance = 0.0001f;

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
