using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Deliberately does not open a scene, activate the controller, reset a profile,
// or call LoadProgress/SaveProgress. All save fixtures exist only in memory.
public static class KaelRosterValidation
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [MenuItem("Mythwake/Validate Kael Roster and Save Migration")]
    public static void Run()
    {
        var fixtureRoot = new GameObject("Kael roster validation (inactive)");
        fixtureRoot.SetActive(false);
        try
        {
            var controller = fixtureRoot.AddComponent<IdlePrototypeController>();
            Set(controller, "backendGameplayEnabled", false);
            ValidateCatalog();
            ValidateFreshFormation(controller);
            ValidateLegacyRoundTrip(controller);
            ValidateLegacyEquipment(controller);
            ValidateSelectionAndBench(controller);
            ValidateSeventhSlotClick(controller, fixtureRoot);
            ValidateBackendSelectedStats(controller);
            Debug.Log("Kael roster validation passed: 8 unique heroes, starter Kael, 7 selected slots, 5 legacy presets, per-hero save migration, selected stats, bench assignment, seventh slot click, and EN/DE skill text. No player save was read or written.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fixtureRoot);
        }
    }

    private static void ValidateCatalog()
    {
        var definitions = (Array)GetStatic("HeroDefinitions");
        Assert(definitions.Length == 8, "Expected eight heroes.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in definitions)
            Assert(ids.Add((string)Get(definition, "heroId")), "Duplicate hero ID.");
        var kael = definitions.GetValue(7);
        Assert((string)Get(kael, "heroId") == "hero_kael", "Kael must append at index seven.");
        Assert((int)Get(kael, "baseAttack") == 18 && (int)Get(kael, "baseHealth") == 150, "Kael baseline mismatch.");
        Assert((int)Get(kael, "attackGrowth") == 5 && (int)Get(kael, "healthGrowth") == 28, "Kael growth mismatch.");
        var banner = GetStatic("HeroShardBanner");
        var foundShardReward = false;
        foreach (var rate in (Array)Get(banner, "rates"))
            if (Array.IndexOf((int[])Get(rate, "heroIndexes"), 7) >= 0) foundShardReward = true;
        Assert(foundShardReward, "Kael shards must be obtainable from the standard banner.");
        foreach (var language in new[] { MythwakeLanguage.English, MythwakeLanguage.German })
            foreach (var suffix in new[] { "name", "title", "description", "ability.name", "ability.description" })
            {
                var key = "hero.hero_kael." + suffix;
                Assert(MythwakeLocalization.Text(language, key) != key, "Missing localization: " + key);
            }
    }

    private static void ValidateFreshFormation(IdlePrototypeController controller)
    {
        Set(controller, "formationSlotHeroIndices", null);
        Set(controller, "formationPresetHeroIndices", null);
        Invoke(controller, "EnsureFormationPresets");
        var active = (int[])Invoke(controller, "GetActiveFormationHeroIndices");
        Equal(active, new[] { 7, 1, 2, 3, 4, 5, 6 }, "Fresh formation");
        Assert(((int[])Get(controller, "formationPresetHeroIndices")).Length == 35, "Preset stride must remain seven.");
    }

    private static void ValidateLegacyRoundTrip(IdlePrototypeController controller)
    {
        var saveType = typeof(IdlePrototypeController).GetNestedType("PrototypeSaveData", BindingFlags.NonPublic);
        var save = Activator.CreateInstance(saveType, true);
        Set(save, "saveVersion", 2);
        Set(save, "gold", 1234);
        Set(save, "enemyLevel", 3);
        Set(save, "heroLevels", new[] { 3, 4, 5, 6, 7, 8, 9 });
        Set(save, "heroShards", new[] { 11, 12, 13, 14, 15, 16, 17 });
        Set(save, "heroAscensions", new[] { 0, 1, 2, 3, 4, 5, 6 });
        Set(save, "heroStarLevels", new[] { 1, 2, 3, 4, 5, 1, 2 });
        Set(save, "heroWeaponLevels", new[] { 2, 3, 4, 5, 6, 7, 8 });
        Set(save, "heroArmorLevels", new[] { 3, 4, 5, 6, 7, 8, 9 });
        var formation = new[] { 6, 5, 4, 3, 2, 1, 0 };
        var presets = new int[35];
        for (var p = 0; p < 5; p++)
            for (var slot = 0; slot < 7; slot++) presets[p * 7 + slot] = (slot + p) % 7;
        Array.Copy(formation, 0, presets, 14, 7);
        Set(save, "formationSlotHeroIndices", formation);
        Set(save, "selectedFormationPresetIndex", 2);
        Set(save, "formationPresetHeroIndices", presets);
        var rarities = new int[42];
        var accessoryLevels = new int[42];
        for (var i = 0; i < 42; i++) { rarities[i] = -1; }
        rarities[6 * 6 + 5] = 0;
        accessoryLevels[6 * 6 + 5] = 2;
        Set(save, "heroEquippedAccessoryRarities", rarities);
        Set(save, "heroEquippedAccessoryLevels", accessoryLevels);
        Invoke(controller, "ApplySaveData", save);
        Equal((int[])Get(controller, "heroLevels"), new[] { 3, 4, 5, 6, 7, 8, 9, 1 }, "Legacy levels");
        Equal((int[])Get(controller, "heroShards"), new[] { 11, 12, 13, 14, 15, 16, 17, 0 }, "Legacy shards");
        Equal((int[])Get(controller, "heroWeaponLevels"), new[] { 2, 3, 4, 5, 6, 7, 8, 1 }, "Legacy weapon levels");
        Equal((int[])Get(controller, "heroArmorLevels"), new[] { 3, 4, 5, 6, 7, 8, 9, 1 }, "Legacy armor levels");
        Equal((int[])Get(controller, "formationPresetHeroIndices"), presets, "All five legacy presets");
        Equal((int[])Invoke(controller, "GetActiveFormationHeroIndices"), formation, "Legacy active formation");
        Assert((int)Invoke(controller, "FindFormationSlotForHero", 7) == -1, "Existing saves must keep Kael on the bench.");
        var expandedRarities = (int[])Get(controller, "heroEquippedAccessoryRarities");
        var expandedLevels = (int[])Get(controller, "heroEquippedAccessoryLevels");
        Assert(expandedRarities.Length == 48 && expandedRarities[41] == 0 && expandedLevels[41] == 2, "Legacy last hero accessory was lost or shifted.");
        Assert(expandedRarities[47] == -1 && expandedLevels[47] == 0, "Kael must start without inherited accessories.");
        var savedAgain = Invoke(controller, "CreateSaveData");
        var reloaded = JsonUtility.FromJson(JsonUtility.ToJson(savedAgain), saveType);
        Invoke(controller, "ApplySaveData", reloaded);
        Equal((int[])Get(controller, "formationPresetHeroIndices"), presets, "Preset JSON round trip");
        Equal((int[])Get(controller, "heroLevels"), new[] { 3, 4, 5, 6, 7, 8, 9, 1 }, "Level JSON round trip");
        Assert((int)Get(controller, "gold") == 1234, "Currency changed during migration.");
    }

    private static void ValidateLegacyEquipment(IdlePrototypeController controller)
    {
        Set(controller, "weaponLevel", 12);
        Set(controller, "armorLevel", 13);
        Set(controller, "heroWeaponLevels", null);
        Set(controller, "heroArmorLevels", null);
        Invoke(controller, "EnsureHeroEquipment");
        var weapons = (int[])Get(controller, "heroWeaponLevels");
        var armor = (int[])Get(controller, "heroArmorLevels");
        Assert(weapons[0] == 12 && weapons[6] == 12 && weapons[7] == 1, "Legacy scalar weapons must migrate only to previous heroes.");
        Assert(armor[0] == 13 && armor[6] == 13 && armor[7] == 1, "Legacy scalar armor must migrate only to previous heroes.");
    }

    private static void ValidateSelectionAndBench(IdlePrototypeController controller)
    {
        Set(controller, "formationSlotHeroIndices", new[] { 0, 1, 2, 3, 4, 5, 6 });
        var damageBefore = (int)Invoke(controller, "GetTeamDamage");
        var healthBefore = (int)Invoke(controller, "GetTeamHealth");
        ((int[])Get(controller, "heroLevels"))[7] = 90;
        Assert((int)Invoke(controller, "GetTeamDamage") == damageBefore, "A benched hero contributed damage.");
        Assert((int)Invoke(controller, "GetTeamHealth") == healthBefore, "A benched hero contributed health.");
        Assert((bool)Invoke(controller, "AssignFormationHeroToSlot", 7, 6), "Kael could not replace the seventh slot.");
        var active = (int[])Invoke(controller, "GetActiveFormationHeroIndices");
        Assert(active.Length == 7 && active[6] == 7 && new HashSet<int>(active).Count == 7, "Invalid deployed formation.");
        Assert((int)Invoke(controller, "GetTeamDamage") > damageBefore, "Selected Kael did not contribute stats.");
        Assert(((string[])Invoke(controller, "GetActiveFormationHeroIds"))[6] == "hero_kael", "ID mapping lost seventh-slot Kael.");
        Assert(!(bool)Invoke(controller, "AssignFormationHeroToSlot", 7, 7), "An eighth formation slot was accepted.");
        Invoke(controller, "SwapTeamSlots", 0, 6);
        Assert(((int[])Get(controller, "formationSlotHeroIndices"))[0] == 7, "Drag swap failed.");
        Invoke(controller, "ApplyStrongestFormation");
        active = (int[])Invoke(controller, "GetActiveFormationHeroIndices");
        Assert(active.Length == 7 && active[0] == 7 && new HashSet<int>(active).Count == 7, "Auto-set must select the strongest seven.");
    }

    private static void ValidateSeventhSlotClick(IdlePrototypeController controller, GameObject root)
    {
        var buttons = new Button[7];
        for (var i = 0; i < buttons.Length; i++)
        {
            var buttonObject = new GameObject("Slot " + i, typeof(RectTransform), typeof(Button));
            buttonObject.transform.SetParent(root.transform, false);
            buttons[i] = buttonObject.GetComponent<Button>();
        }
        Set(controller, "heroTeamSlotButtons", buttons);
        Set(controller, "selectedHeroTeamSlotIndex", -1);
        Invoke(controller, "RegisterHeroScreenControls");
        buttons[6].onClick.Invoke();
        Assert((int)Get(controller, "selectedHeroTeamSlotIndex") == 6, "Seventh team slot click was not wired.");
        Invoke(controller, "UnregisterHeroScreenControls");
    }

    private static void ValidateBackendSelectedStats(IdlePrototypeController controller)
    {
        var catalog = (Array)GetStatic("HeroDefinitions");
        var definitions = new MythwakeHeroDefinitionDto[8];
        var states = new MythwakeHeroStateDto[8];
        var attacks = new int[8];
        var health = new int[8];
        var ownedAttack = 0;
        var ownedHealth = 0;
        for (var i = 0; i < 8; i++)
        {
            definitions[i] = new MythwakeHeroDefinitionDto
            {
                heroId = (string)Get(catalog.GetValue(i), "heroId"), maxLevel = 100, maxAscension = 10,
                baseAttack = 18 + i, attackPerLevel = 5, attackPerAscension = 11,
                baseHealth = 150 + i * 10, healthPerLevel = 28, healthPerAscension = 70
            };
            states[i] = new MythwakeHeroStateDto { heroId = definitions[i].heroId, level = 3 + i, ascension = 1, starLevel = 2 };
            attacks[i] = definitions[i].baseAttack + (states[i].level - 1) * 5 + 11 + Mathf.CeilToInt(definitions[i].baseAttack * .12f) * 2;
            health[i] = definitions[i].baseHealth + (states[i].level - 1) * 28 + 70 + Mathf.CeilToInt(definitions[i].baseHealth * .09f) * 2;
            ownedAttack += attacks[i];
            ownedHealth += health[i];
        }
        Set(controller, "backendDefinitions", new MythwakeDefinitionSnapshotDto { contentHash = "isolated-catalog-fixture", heroes = definitions });
        Set(controller, "hasBackendDefinitions", true);
        Set(controller, "backendGameplayEnabled", true);
        Invoke(controller, "ApplyBackendHeroes", states, null);
        const int sharedAttack = 37;
        const int sharedHealth = 430;
        const int sharedEquipmentPower = 61;
        Set(controller, "backendTeamAttack", ownedAttack + sharedAttack);
        Set(controller, "backendTeamHealth", ownedHealth + sharedHealth);
        Set(controller, "backendTeamPower", ownedAttack + sharedAttack + (ownedHealth + sharedHealth) / 10 + sharedEquipmentPower);
        for (var i = 0; i < 8; i++)
        {
            Assert((int)Invoke(controller, "GetHeroAttack", i) == attacks[i], "Server hero attack omitted stars.");
            Assert((int)Invoke(controller, "GetHeroHealth", i) == health[i], "Server hero health omitted stars.");
        }
        Set(controller, "formationSlotHeroIndices", new[] { 0, 1, 2, 3, 4, 5, 6 });
        var expectedAttack = ownedAttack - attacks[7] + sharedAttack;
        var expectedHealth = ownedHealth - health[7] + sharedHealth;
        Assert((int)Invoke(controller, "GetTeamDamage") == expectedAttack, "Server selected attack counted shared bonuses more than once or included the bench.");
        Assert((int)Invoke(controller, "GetTeamHealth") == expectedHealth, "Server selected health counted shared bonuses more than once or included the bench.");
        Assert((int)Invoke(controller, "GetTeamPower") == expectedAttack + expectedHealth / 10 + sharedEquipmentPower, "Server selected power formula changed.");
        Invoke(controller, "AssignFormationHeroToSlot", 7, 0);
        Assert((int)Invoke(controller, "GetTeamDamage") == expectedAttack - attacks[0] + attacks[7], "Server selected stats did not follow bench replacement.");
        Assert((int)Invoke(controller, "GetTeamHealth") == expectedHealth - health[0] + health[7], "Server selected HP did not follow bench replacement.");
        Set(controller, "backendGameplayEnabled", false);
        Set(controller, "hasBackendDefinitions", false);
    }

    private static object Invoke(object target, string name, params object[] args)
    {
        var method = target.GetType().GetMethod(name, InstanceFlags);
        if (method == null) throw new InvalidOperationException("Missing method: " + name);
        return method.Invoke(target, args);
    }
    private static object Get(object target, string name) => target.GetType().GetField(name, InstanceFlags).GetValue(target);
    private static object GetStatic(string name) => typeof(IdlePrototypeController).GetField(name, StaticFlags).GetValue(null);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, InstanceFlags).SetValue(target, value);
    private static void Equal(int[] actual, int[] expected, string label)
    {
        Assert(actual != null && actual.Length == expected.Length, label + " length mismatch.");
        for (var i = 0; i < expected.Length; i++) Assert(actual[i] == expected[i], label + " changed index " + i + ".");
    }
    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
