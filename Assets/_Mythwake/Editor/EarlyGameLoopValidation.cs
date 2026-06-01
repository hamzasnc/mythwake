using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class EarlyGameLoopValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string StandardSummonBannerId = "hero_shard_standard";

    [MenuItem("Mythwake/Validate Early Game Loop")]
    public static void RunEarlyGameLoopValidation()
    {
        try
        {
            ValidateEarlyGameLoop();
            Debug.Log("Early Game Loop validated: fresh-save guidance, first campaign clears, resource dungeons, gear drop/equip, Village build, starter summon, and localized Next Goal routing are stable.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateEarlyGameLoop()
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

        try
        {
            controller.ResetProgress();
            Canvas.ForceUpdateCanvases();

            ValidateFreshHomeGuidance(controller);
            ValidateStarterSummon(controller);
            ValidateCampaignAndDungeonLoop(controller);
            ValidateHeroGearVillageLoop(controller);
            ValidateResourceFallbackGuidance(controller);
        }
        finally
        {
            SetPrivateEnumField(controller, "language", "English");
            controller.ResetProgress();
        }
    }

    private static void ValidateFreshHomeGuidance(IdlePrototypeController controller)
    {
        controller.ShowHome();
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();

        var nextGoal = GetPrivateField<TMP_Text>(controller, "nextGoalText");
        if (nextGoal == null)
        {
            throw new InvalidOperationException("Home Next Goal text is missing.");
        }

        RequireCopy(nextGoal.text, "Next Goal", "fresh Home Next Goal title");
        RequireCopy(nextGoal.text, "Push Campaign Stage 1", "fresh Home Next Goal action");
        AssertTextFits(nextGoal, "fresh Home Next Goal");

        SetPrivateEnumField(controller, "language", "German");
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();

        RequireCopy(nextGoal.text, "Naechstes Ziel", "German Home Next Goal title");
        RequireCopy(nextGoal.text, "Kampagne Stufe 1", "German Home Next Goal action");
        AssertTextFits(nextGoal, "German fresh Home Next Goal");

        SetPrivateEnumField(controller, "language", "English");
        InvokePrivate(controller, "RefreshUi");
    }

    private static void ValidateStarterSummon(IdlePrototypeController controller)
    {
        controller.ResetProgress();
        controller.ShowSummon();
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();

        var startingGems = GetPrivateField<int>(controller, "gems");
        if (startingGems < 35)
        {
            throw new InvalidOperationException($"Fresh save should start with enough Gems for one Summon pull. Gems={startingGems}.");
        }

        var startingShards = SumPrivateIntArray(controller, "heroShards");
        var result = controller.PullMany(StandardSummonBannerId, 1);
        if (!result.success)
        {
            throw new InvalidOperationException($"Starter Summon should succeed, got '{result.message}'.");
        }

        RequireCopy(result.message, "Shards fuel Awakening", "starter Summon shard hint");
        if (GetPrivateField<int>(controller, "gems") >= startingGems)
        {
            throw new InvalidOperationException("Starter Summon should spend Gems.");
        }

        if (SumPrivateIntArray(controller, "heroShards") <= startingShards)
        {
            throw new InvalidOperationException("Starter Summon should grant hero shards.");
        }

        var failed = controller.PullMany(StandardSummonBannerId, 1);
        if (failed.success || failed.errorCode != "insufficient_currency")
        {
            throw new InvalidOperationException("Second fresh-save Summon should be clearly gated by Gems after the starter pull.");
        }
    }

    private static void ValidateCampaignAndDungeonLoop(IdlePrototypeController controller)
    {
        controller.ResetProgress();
        var startingEssence = GetPrivateField<int>(controller, "mythEssence");

        for (var i = 0; i < 3; i++)
        {
            var result = controller.FightCampaign();
            if (!result.success)
            {
                throw new InvalidOperationException($"Fresh campaign fight {i + 1} should clear smoothly, got '{result.message}'.");
            }

            RequireCopy(result.message, "Next:", $"campaign result {i + 1} next-step copy");
        }

        if (GetPrivateField<int>(controller, "enemyLevel") < 4)
        {
            throw new InvalidOperationException("Three fresh campaign clears should advance the current stage to at least 4.");
        }

        if (GetPrivateField<int>(controller, "mythEssence") <= startingEssence)
        {
            throw new InvalidOperationException("Campaign clears should visibly grant Myth Essence.");
        }

        var goldBefore = GetPrivateField<int>(controller, "gold");
        var goldResult = controller.RunDungeon("gold_dungeon");
        if (!goldResult.success || GetPrivateField<int>(controller, "gold") <= goldBefore)
        {
            throw new InvalidOperationException($"Gold Dungeon F1 should grant useful upgrade Gold, got '{goldResult.message}'.");
        }

        RequireCopy(goldResult.message, "Next:", "Gold Dungeon result next-step copy");

        var essenceBefore = GetPrivateField<int>(controller, "mythEssence");
        var essenceResult = controller.RunDungeon("essence_dungeon");
        if (!essenceResult.success || GetPrivateField<int>(controller, "mythEssence") <= essenceBefore)
        {
            throw new InvalidOperationException($"Essence Dungeon F1 should grant useful Myth Essence, got '{essenceResult.message}'.");
        }

        var accessoryCopiesBefore = SumPrivateIntArray(controller, "accessoryInventory");
        var gearResult = controller.RunDungeon("gear_dungeon");
        if (!gearResult.success || SumPrivateIntArray(controller, "accessoryInventory") <= accessoryCopiesBefore)
        {
            throw new InvalidOperationException($"Gear Dungeon F1 should grant an accessory copy, got '{gearResult.message}'.");
        }

        RequireCopy(gearResult.message, "Next:", "Gear Dungeon result next-step copy");
    }

    private static void ValidateHeroGearVillageLoop(IdlePrototypeController controller)
    {
        controller.ResetProgress();
        var powerBefore = GetPrivateIntMethod(controller, "GetTeamPower");
        var heroResult = controller.LevelHero("hero_astra");
        if (!heroResult.success)
        {
            throw new InvalidOperationException($"Fresh Hero level-up should be reachable, got '{heroResult.message}'.");
        }

        var powerAfterHeroLevel = GetPrivateIntMethod(controller, "GetTeamPower");
        if (powerAfterHeroLevel <= powerBefore)
        {
            throw new InvalidOperationException("Hero level-up should increase visible team power.");
        }

        InvokePrivate(controller, "SelectVillagePlot", 0);
        InvokePrivate(controller, "BuildSelectedVillagePlot");
        var builtStates = GetPrivateField<bool[]>(controller, "villagePlotBuiltStates");
        if (builtStates == null || builtStates.Length == 0 || !builtStates[0])
        {
            throw new InvalidOperationException("Fresh loop should allow building the first Village plot after the first hero level.");
        }

        var powerAfterVillage = GetPrivateIntMethod(controller, "GetTeamPower");
        if (powerAfterVillage <= powerAfterHeroLevel)
        {
            throw new InvalidOperationException("Village build should make its stat bonus visible in team power.");
        }

        var gearResult = controller.RunDungeon("gear_dungeon");
        if (!gearResult.success)
        {
            throw new InvalidOperationException($"Gear Dungeon should be clearable before equipping its first drop, got '{gearResult.message}'.");
        }

        var accessoryId = FindFirstOwnedAccessoryId(controller);
        var powerBeforeEquip = GetPrivateIntMethod(controller, "GetTeamPower");
        var equipResult = controller.EquipAccessory(accessoryId);
        if (!equipResult.success)
        {
            throw new InvalidOperationException($"First Gear Dungeon accessory should equip cleanly, got '{equipResult.message}'.");
        }

        if (GetPrivateIntMethod(controller, "GetTeamPower") <= powerBeforeEquip)
        {
            throw new InvalidOperationException("Equipping an accessory drop should increase visible team power.");
        }
    }

    private static void ValidateResourceFallbackGuidance(IdlePrototypeController controller)
    {
        controller.ResetProgress();
        SetPrivateField(controller, "enemyLevel", 40);
        SetPrivateField(controller, "selectedCampaignStage", 40);
        SetPrivateField(controller, "gold", 0);
        SetPrivateField(controller, "mythEssence", 0);
        SetPrivateField(controller, "gems", 0);
        InvokePrivate(controller, "RefreshUi");

        var nextGoal = (string)InvokePrivate(controller, "GetNextGoalText");
        RequireCopy(nextGoal, "Gold Dungeon", "resource fallback Next Goal");

        SetPrivateEnumField(controller, "language", "German");
        InvokePrivate(controller, "RefreshUi");
        var germanNextGoal = (string)InvokePrivate(controller, "GetNextGoalText");
        RequireCopy(germanNextGoal, "Gold-Dungeon", "German resource fallback Next Goal");
        SetPrivateEnumField(controller, "language", "English");
    }

    private static string FindFirstOwnedAccessoryId(IdlePrototypeController controller)
    {
        var inventory = GetPrivateField<int[]>(controller, "accessoryInventory");
        var definitions = GetStaticArray(typeof(IdlePrototypeController), "AccessoryDefinitions");
        if (inventory == null || definitions == null)
        {
            throw new InvalidOperationException("Accessory inventory or definitions are missing.");
        }

        var count = Math.Min(inventory.Length, definitions.Length);
        for (var i = 0; i < count; i++)
        {
            if (inventory[i] <= 0)
            {
                continue;
            }

            return GetInstanceField<string>(definitions.GetValue(i), "accessoryId");
        }

        throw new InvalidOperationException("Expected at least one owned accessory copy after Gear Dungeon.");
    }

    private static int SumPrivateIntArray(object target, string fieldName)
    {
        var values = GetPrivateField<int[]>(target, fieldName);
        var total = 0;
        if (values == null)
        {
            return 0;
        }

        for (var i = 0; i < values.Length; i++)
        {
            total += Math.Max(0, values[i]);
        }

        return total;
    }

    private static int GetPrivateIntMethod(object target, string methodName)
    {
        return (int)InvokePrivate(target, methodName);
    }

    private static T FindSceneComponent<T>() where T : UnityEngine.Object
    {
        var all = Resources.FindObjectsOfTypeAll<T>();
        for (var i = 0; i < all.Length; i++)
        {
            var component = all[i] as Component;
            if (component == null || component.gameObject.scene.IsValid())
            {
                return all[i];
            }
        }

        return null;
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Missing private method {methodName} on {target.GetType().Name}.");
        }

        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field {fieldName} on {target.GetType().Name}.");
        }

        return (T)field.GetValue(target);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field {fieldName} on {target.GetType().Name}.");
        }

        field.SetValue(target, value);
    }

    private static void SetPrivateEnumField(object target, string fieldName, string value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || !field.FieldType.IsEnum)
        {
            throw new InvalidOperationException($"Missing private enum field {fieldName} on {target.GetType().Name}.");
        }

        field.SetValue(target, Enum.Parse(field.FieldType, value));
    }

    private static Array GetStaticArray(Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing static field {fieldName} on {type.Name}.");
        }

        return field.GetValue(null) as Array;
    }

    private static T GetInstanceField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing field {fieldName} on {target.GetType().Name}.");
        }

        return (T)field.GetValue(target);
    }

    private static void RequireCopy(string text, string expected, string context)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.Contains(expected))
        {
            throw new InvalidOperationException($"{context} should contain '{expected}', got '{text}'.");
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
        var preferred = text.GetPreferredValues(text.text, rect.width, float.PositiveInfinity);
        if (preferred.y > rect.height + 4f)
        {
            throw new InvalidOperationException($"{context} text does not fit vertically. Preferred={preferred.y:0.##}, Rect={rect.height:0.##}, Text='{text.text}'.");
        }
    }
}
