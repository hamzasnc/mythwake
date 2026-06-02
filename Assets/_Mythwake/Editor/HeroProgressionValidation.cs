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
            Debug.Log("Hero progression validated: level 100 cap, Awakening Shards, hero-specific Star shards, Hero Shard Chest, Bag Use flow, reward popup, stat growth, and Hero Detail copy are stable.");
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
            ValidateHeroStarUpgradeAndChest(controller);
            ValidateInventoryChestUseFlow(controller);
            ValidateHeroDetailAwakeningCopy(controller);
        }
        finally
        {
            controller.ResetProgress();
        }
    }

    private static void ValidateAwakeningLockedBelowLevelCap(IdlePrototypeController controller)
    {
        SetHeroProgress(controller, level: 99, awakening: 0, shards: 1000, awakeningShards: 1000);
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
        SetHeroProgress(controller, level: 100, awakening: 0, shards: 7, awakeningShards: 20);
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
        var awakeningShards = GetPrivateField<int>(controller, "awakeningShards");
        if (awakenings[0] != 1 || shards[0] != 7 || awakeningShards != 0)
        {
            throw new InvalidOperationException($"Awakening should consume 20 Awakening Shards, keep hero shards, and set stage 1; got awakening={awakenings[0]}, heroShards={shards[0]}, awakeningShards={awakeningShards}.");
        }

        var powerAfter = (int)InvokePrivate(controller, "GetHeroPower", 0);
        var attackAfter = (int)InvokePrivate(controller, "GetHeroEffectiveAttack", 0);
        var healthAfter = (int)InvokePrivate(controller, "GetHeroCombatMaxHealth", 0);
        if (powerAfter <= powerBefore || attackAfter <= attackBefore || healthAfter <= healthBefore)
        {
            throw new InvalidOperationException($"Awakening should raise power, ATK, and HP. Before power={powerBefore} atk={attackBefore} hp={healthBefore}; after power={powerAfter} atk={attackAfter} hp={healthAfter}.");
        }
    }

    private static void ValidateHeroStarUpgradeAndChest(IdlePrototypeController controller)
    {
        SetHeroProgress(controller, level: 1, awakening: 0, shards: 5, awakeningShards: 0, star: 0);
        InvokePrivate(controller, "RefreshUi");
        var powerBefore = (int)InvokePrivate(controller, "GetHeroPower", 0);
        var attackBefore = (int)InvokePrivate(controller, "GetHeroEffectiveAttack", 0);
        var healthBefore = (int)InvokePrivate(controller, "GetHeroCombatMaxHealth", 0);

        var result = controller.UpgradeHeroStar(TestHeroId);
        if (!result.success)
        {
            throw new InvalidOperationException($"Star upgrade with 5 hero shards should succeed, got {result.errorCode}: {result.message}");
        }

        var stars = GetPrivateField<int[]>(controller, "heroStarLevels");
        var shards = GetPrivateField<int[]>(controller, "heroShards");
        if (stars[0] != 1 || shards[0] != 0)
        {
            throw new InvalidOperationException($"Star upgrade should consume 5 hero shards and set star level 1, got star={stars[0]}, shards={shards[0]}.");
        }

        var powerAfter = (int)InvokePrivate(controller, "GetHeroPower", 0);
        var attackAfter = (int)InvokePrivate(controller, "GetHeroEffectiveAttack", 0);
        var healthAfter = (int)InvokePrivate(controller, "GetHeroCombatMaxHealth", 0);
        if (powerAfter <= powerBefore || attackAfter <= attackBefore || healthAfter <= healthBefore)
        {
            throw new InvalidOperationException($"Star upgrade should raise power, ATK, and HP. Before power={powerBefore} atk={attackBefore} hp={healthBefore}; after power={powerAfter} atk={attackAfter} hp={healthAfter}.");
        }

        SetHeroProgress(controller, level: 1, awakening: 0, shards: 9, awakeningShards: 0, star: 1);
        var blocked = controller.UpgradeHeroStar(TestHeroId);
        if (blocked.success || blocked.errorCode != "insufficient_hero_shards")
        {
            throw new InvalidOperationException($"Second star should cost 10 hero shards, got success={blocked.success}, error={blocked.errorCode}.");
        }

        SetHeroProgress(controller, level: 1, awakening: 0, shards: 0, awakeningShards: 0, star: 0, chests: 1);
        var chest = controller.OpenHeroShardChest();
        if (!chest.success)
        {
            throw new InvalidOperationException($"Hero Shard Chest should open, got {chest.errorCode}: {chest.message}");
        }

        var chestCount = GetPrivateField<int>(controller, "heroShardChests");
        shards = GetPrivateField<int[]>(controller, "heroShards");
        var totalShards = 0;
        for (var i = 0; i < shards.Length; i++)
        {
            totalShards += shards[i];
        }

        if (chestCount != 0 || totalShards <= 0)
        {
            throw new InvalidOperationException($"Hero Shard Chest should consume one chest and grant hero shards, got chests={chestCount}, totalShards={totalShards}.");
        }
    }

    private static void ValidateHeroDetailAwakeningCopy(IdlePrototypeController controller)
    {
        SetHeroProgress(controller, level: 100, awakening: 0, shards: 5, awakeningShards: 20, star: 0, chests: 1);
        controller.ShowHeroes();
        InvokePrivate(controller, "ShowHeroDetail", 0);
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();

        var levelButton = RequireObjectField<Button>(controller, "heroDetailLevelButton");
        AssertButtonLabel(levelButton, "Awaken", "Hero Detail should switch the main progression button to Awaken at level 100.");
        if (!levelButton.interactable)
        {
            throw new InvalidOperationException("Hero Detail Awaken button should be interactable when level 100 and Awakening Shards are sufficient.");
        }

        var starButton = RequireObjectField<Button>(controller, "heroDetailStarButton");
        AssertButtonLabel(starButton, "Star 0->1", "Hero Detail should expose star-level upgrade.");
        if (!starButton.interactable)
        {
            throw new InvalidOperationException("Hero Detail Star button should be interactable when hero shards are sufficient.");
        }

        var chestButton = RequireObjectField<Button>(controller, "heroDetailOpenChestButton");
        AssertButtonLabel(chestButton, "Open Chest (1)", "Hero Detail should expose Hero Shard Chest opening.");
        if (!chestButton.interactable)
        {
            throw new InvalidOperationException("Hero Detail Open Chest button should be interactable when a chest is available.");
        }

        var stats = RequireObjectField<TMP_Text>(controller, "heroDetailStatsText");
        RequireCopy(stats.text, "Lv 100/100", "Hero Detail level cap copy");
        RequireCopy(stats.text, "Awk 0/10", "Hero Detail Awakening stage copy");
        RequireCopy(stats.text, "Star 0/5", "Hero Detail star stage copy");
        RequireCopy(stats.text, "+0 ATK +0 HP", "Hero Detail Awakening bonus copy");
        AssertTextFits(stats, "Hero Detail stats");

        var resourcesAtCap = RequireObjectField<TMP_Text>(controller, "heroDetailResourceText");
        RequireCopy(resourcesAtCap.text, "Awakening Shards 20/20", "Hero Detail Awakening Shards resource copy");
        RequireCopy(resourcesAtCap.text, "Hero Shards 5/5", "Hero Detail hero shard resource copy");
        AssertTextFits(resourcesAtCap, "Hero Detail resource copy at cap");

        SetHeroProgress(controller, level: 99, awakening: 0, shards: 1000, awakeningShards: 1000);
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();
        var resources = RequireObjectField<TMP_Text>(controller, "heroDetailResourceText");
        RequireCopy(resources.text, "Reach Lv 100", "Hero Detail locked Awakening guidance");
        AssertTextFits(resources, "Hero Detail resource copy");
    }

    private static void ValidateInventoryChestUseFlow(IdlePrototypeController controller)
    {
        SetHeroProgress(controller, level: 1, awakening: 0, shards: 0, awakeningShards: 0, star: 0, chests: 3);
        InvokePrivate(controller, "ShowInventoryPopup");
        InvokePrivate(controller, "SelectInventoryItem", 0);
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();

        var useOneButton = RequireObjectField<Button>(controller, "inventoryUseOneButton");
        var useAmountButton = RequireObjectField<Button>(controller, "inventoryUseAmountButton");
        var useAllButton = RequireObjectField<Button>(controller, "inventoryUseAllButton");
        var amountInput = RequireObjectField<TMP_InputField>(controller, "inventoryUseAmountInput");
        if (!useOneButton.gameObject.activeInHierarchy || !useOneButton.interactable)
        {
            throw new InvalidOperationException("Inventory Hero Shard Chest detail should expose an interactable Use 1 button.");
        }

        AssertButtonLabel(useAllButton, "All (3)", "Inventory Hero Shard Chest should expose Use All with owned count.");
        amountInput.text = "2";
        useAmountButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        var chestCount = GetPrivateField<int>(controller, "heroShardChests");
        if (chestCount != 1)
        {
            throw new InvalidOperationException($"Inventory Use amount should consume 2 of 3 Hero Shard Chests, got {chestCount}.");
        }

        var rewardRoot = RequireObjectField<RectTransform>(controller, "inventoryRewardPopupRoot");
        if (!rewardRoot.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Inventory Use should show the reward popup.");
        }

        var rewardSummary = RequireObjectField<TMP_Text>(controller, "inventoryRewardSummaryText");
        RequireCopy(rewardSummary.text, "Opened 2 Hero Shard Chests", "Inventory reward summary");
        AssertTextFits(rewardSummary, "Inventory reward summary");

        var rewardFrames = GetPrivateField<Image[]>(controller, "inventoryRewardFrames");
        var visibleReward = false;
        for (var i = 0; rewardFrames != null && i < rewardFrames.Length; i++)
        {
            if (rewardFrames[i] != null && rewardFrames[i].gameObject.activeInHierarchy)
            {
                visibleReward = true;
                break;
            }
        }

        if (!visibleReward)
        {
            throw new InvalidOperationException("Inventory reward popup should show at least one visible reward slot.");
        }
    }

    private static void SetHeroProgress(IdlePrototypeController controller, int level, int awakening, int shards, int awakeningShards = 0, int star = 0, int chests = 0)
    {
        var levels = GetPrivateField<int[]>(controller, "heroLevels");
        var awakenings = GetPrivateField<int[]>(controller, "heroAscensions");
        var shardCounts = GetPrivateField<int[]>(controller, "heroShards");
        var stars = GetPrivateField<int[]>(controller, "heroStarLevels");
        levels[0] = level;
        awakenings[0] = awakening;
        shardCounts[0] = shards;
        stars[0] = star;
        SetPrivateField(controller, "heroLevels", levels);
        SetPrivateField(controller, "heroAscensions", awakenings);
        SetPrivateField(controller, "heroShards", shardCounts);
        SetPrivateField(controller, "heroStarLevels", stars);
        SetPrivateField(controller, "awakeningShards", awakeningShards);
        SetPrivateField(controller, "heroShardChests", chests);
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
