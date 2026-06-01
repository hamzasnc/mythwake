using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HeroProgressionValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string TestHeroId = "hero_astra";

    [MenuItem("Mythwake/Validate Hero Progression")]
    public static void RunHeroProgressionValidation()
    {
        try
        {
            ValidateHeroProgression();
            Debug.Log("Hero progression validated: level 100 cap, level-gated Awakening, shard spend, stat growth, and Hero Detail copy are stable.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateHeroProgression()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var controller = FindSceneComponent<IdlePrototypeController>();
        if (controller == null)
        {
            throw new InvalidOperationException("Missing IdlePrototypeController in SampleScene.");
        }

        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "RegisterNavigation");
        SetPrivateField(controller, "backendGameplayEnabled", false);
        SetPrivateField(controller, "language", MythwakeLanguage.English);

        try
        {
            controller.ResetProgress();
            Canvas.ForceUpdateCanvases();

            ValidateAwakeningLockedBelowLevelCap(controller);
            ValidateLevelOneHundredCap(controller);
            ValidateAwakeningSpendAndStats(controller);
            ValidateHeroDetailAwakeningCopy(controller);
        }
        finally
        {
            controller.ResetProgress();
        }
    }

    private static void ValidateAwakeningLockedBelowLevelCap(IdlePrototypeController controller)
    {
        SetHeroProgress(controller, level: 99, awakening: 0, shards: 1000);
        var result = controller.AscendHero(TestHeroId);
        if (result.success || result.errorCode != "level_required")
        {
            throw new InvalidOperationException($"Awakening should require level 100, got success={result.success}, error={result.errorCode}, message={result.message}.");
        }
    }

    private static void ValidateLevelOneHundredCap(IdlePrototypeController controller)
    {
        SetHeroProgress(controller, level: 99, awakening: 0, shards: 0);
        SetPrivateField(controller, "mythEssence", 100000);

        var result = controller.LevelHero(TestHeroId);
        if (!result.success)
        {
            throw new InvalidOperationException($"Hero should level from 99 to 100, got {result.errorCode}: {result.message}");
        }

        var levels = GetPrivateField<int[]>(controller, "heroLevels");
        if (levels[0] != 100)
        {
            throw new InvalidOperationException($"Hero should be level 100 after final level-up, got {levels[0]}.");
        }

        var capped = controller.LevelHero(TestHeroId);
        if (capped.success || capped.errorCode != "max_level")
        {
            throw new InvalidOperationException($"Hero should be capped at level 100, got success={capped.success}, error={capped.errorCode}.");
        }
    }

    private static void ValidateAwakeningSpendAndStats(IdlePrototypeController controller)
    {
        SetHeroProgress(controller, level: 100, awakening: 0, shards: 20);
        InvokePrivate(controller, "RefreshUi");
        var powerBefore = (int)InvokePrivate(controller, "GetHeroPower", 0);
        var attackBefore = (int)InvokePrivate(controller, "GetHeroEffectiveAttack", 0);
        var healthBefore = (int)InvokePrivate(controller, "GetHeroCombatMaxHealth", 0);

        var result = controller.AscendHero(TestHeroId);
        if (!result.success)
        {
            throw new InvalidOperationException($"Awakening at level 100 with enough shards should succeed, got {result.errorCode}: {result.message}");
        }

        var awakenings = GetPrivateField<int[]>(controller, "heroAscensions");
        var shards = GetPrivateField<int[]>(controller, "heroShards");
        if (awakenings[0] != 1 || shards[0] != 0)
        {
            throw new InvalidOperationException($"Awakening should consume 20 shards and set stage 1, got awakening={awakenings[0]}, shards={shards[0]}.");
        }

        var powerAfter = (int)InvokePrivate(controller, "GetHeroPower", 0);
        var attackAfter = (int)InvokePrivate(controller, "GetHeroEffectiveAttack", 0);
        var healthAfter = (int)InvokePrivate(controller, "GetHeroCombatMaxHealth", 0);
        if (powerAfter <= powerBefore || attackAfter <= attackBefore || healthAfter <= healthBefore)
        {
            throw new InvalidOperationException($"Awakening should raise power, ATK, and HP. Before power={powerBefore} atk={attackBefore} hp={healthBefore}; after power={powerAfter} atk={attackAfter} hp={healthAfter}.");
        }
    }

    private static void ValidateHeroDetailAwakeningCopy(IdlePrototypeController controller)
    {
        SetHeroProgress(controller, level: 100, awakening: 0, shards: 20);
        controller.ShowHeroes();
        InvokePrivate(controller, "ShowHeroDetail", 0);
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();

        var levelButton = RequireObjectField<Button>(controller, "heroDetailLevelButton");
        AssertButtonLabel(levelButton, "Awaken", "Hero Detail should switch the main progression button to Awaken at level 100.");
        if (!levelButton.interactable)
        {
            throw new InvalidOperationException("Hero Detail Awaken button should be interactable when level 100 and shards are sufficient.");
        }

        var stats = RequireObjectField<TMP_Text>(controller, "heroDetailStatsText");
        RequireCopy(stats.text, "Lv 100/100", "Hero Detail level cap copy");
        RequireCopy(stats.text, "Awk 0/10", "Hero Detail Awakening stage copy");
        RequireCopy(stats.text, "+0 ATK +0 HP", "Hero Detail Awakening bonus copy");
        AssertTextFits(stats, "Hero Detail stats");

        SetHeroProgress(controller, level: 99, awakening: 0, shards: 1000);
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();
        var resources = RequireObjectField<TMP_Text>(controller, "heroDetailResourceText");
        RequireCopy(resources.text, "Reach Lv 100", "Hero Detail locked Awakening guidance");
        AssertTextFits(resources, "Hero Detail resource copy");
    }

    private static void SetHeroProgress(IdlePrototypeController controller, int level, int awakening, int shards)
    {
        var levels = GetPrivateField<int[]>(controller, "heroLevels");
        var awakenings = GetPrivateField<int[]>(controller, "heroAscensions");
        var shardCounts = GetPrivateField<int[]>(controller, "heroShards");
        levels[0] = level;
        awakenings[0] = awakening;
        shardCounts[0] = shards;
        SetPrivateField(controller, "heroLevels", levels);
        SetPrivateField(controller, "heroAscensions", awakenings);
        SetPrivateField(controller, "heroShards", shardCounts);
        SetPrivateField(controller, "selectedHeroIndex", 0);
    }

    private static void AssertButtonLabel(Button button, string expected, string context)
    {
        var label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (label == null)
        {
            throw new InvalidOperationException($"{context} Button '{button.name}' has no TMP label.");
        }

        if (!string.Equals(label.text, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{context} Expected '{expected}', got '{label.text}'.");
        }

        AssertTextFits(label, $"{button.name} label");
    }

    private static void RequireCopy(string text, string expectedFragment, string context)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.Contains(expectedFragment))
        {
            throw new InvalidOperationException($"{context} should contain '{expectedFragment}', got '{text}'.");
        }
    }

    private static void AssertTextFits(TMP_Text text, string context)
    {
        if (text == null)
        {
            throw new InvalidOperationException($"{context} text is missing.");
        }

        text.ForceMeshUpdate();
        var rect = text.rectTransform.rect;
        var preferred = text.GetPreferredValues(text.text, rect.width, 0f);
        if (preferred.y > rect.height + 1f)
        {
            throw new InvalidOperationException($"{context} overflows vertically. Preferred={preferred.y:0.#}, rect={rect.height:0.#}, text='{text.text}'.");
        }
    }

    private static T RequireObjectField<T>(object target, string fieldName) where T : UnityEngine.Object
    {
        var value = GetPrivateField<T>(target, fieldName);
        if (value == null)
        {
            throw new InvalidOperationException($"{fieldName} should not be null.");
        }

        return value;
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field: {fieldName}");
        }

        return (T)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field: {fieldName}");
        }

        field.SetValue(target, value);
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Missing private method: {methodName}");
        }

        return method.Invoke(target, args);
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        var components = Resources.FindObjectsOfTypeAll<T>();
        for (var i = 0; i < components.Length; i++)
        {
            var component = components[i];
            if (component != null && component.gameObject.scene.IsValid())
            {
                return component;
            }
        }

        return null;
    }
}
