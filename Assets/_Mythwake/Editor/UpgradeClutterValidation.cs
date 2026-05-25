using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UpgradeClutterValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Mythwake/Validate Upgrade Clutter")]
    public static void RunUpgradeClutterValidation()
    {
        try
        {
            ValidateUpgradeClutter();
            Debug.Log("Upgrade clutter validated: legacy Battle/Hero controls are hidden, Gear controls live on Gear, Hero Detail gear slots/art fit, and debug tools live on Shop.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateUpgradeClutter()
    {
        EditorSceneManager.OpenScene(ScenePath);

        var controller = FindSceneComponent<IdlePrototypeController>();
        if (controller == null)
        {
            throw new InvalidOperationException("Missing IdlePrototypeController in SampleScene.");
        }

        InvokePrivate(controller, "EnsureRuntimeDebugUi");
        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "RegisterNavigation");
        Canvas.ForceUpdateCanvases();

        ValidateBattleScreen(controller);
        ValidateHeroesScreen(controller);
        ValidateGearScreen(controller);
        ValidateShopTools(controller);
    }

    private static void ValidateBattleScreen(IdlePrototypeController controller)
    {
        controller.ShowBattle();
        Canvas.ForceUpdateCanvases();

        var battlePanel = RequireObjectField<GameObject>(controller, "battlePanel");
        if (!battlePanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Battle panel should be active after ShowBattle.");
        }

        RequireInactive(RequireButtonField(controller, "upgradeButton"), "Legacy Battle upgrade button");

        var debugPanel = FindSceneObject("Debug Resource Panel");
        if (debugPanel != null && debugPanel.GetComponentsInChildren<Button>(true).Length > 0)
        {
            throw new InvalidOperationException("Debug Resource Panel should not keep debug buttons under Battle after layout.");
        }

        RequireNotUnderPanel(RequireButtonField(controller, "debugGoldButton"), battlePanel, "Debug Gold Button");
        RequireNotUnderPanel(RequireButtonField(controller, "debugEssenceButton"), battlePanel, "Debug Essence Button");
        RequireNotUnderPanel(RequireButtonField(controller, "debugGemsButton"), battlePanel, "Debug Gems Button");
        RequireNotUnderPanel(RequireButtonField(controller, "debugAccessoryButton"), battlePanel, "Debug Accessory Button");
    }

    private static void ValidateHeroesScreen(IdlePrototypeController controller)
    {
        controller.ShowHeroes();
        Canvas.ForceUpdateCanvases();

        var heroesPanel = RequireObjectField<GameObject>(controller, "heroesPanel");
        if (!heroesPanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Heroes panel should be active after ShowHeroes.");
        }

        RequireInactive(RequireButtonField(controller, "heroUpgradeButton"), "Legacy hero upgrade button");
        RequireInactive(RequireButtonField(controller, "heroAscendButton"), "Legacy hero ascend button");

        InvokePrivate(controller, "ShowHeroDetail", 0);
        Canvas.ForceUpdateCanvases();

        var heroDetailRoot = RequireObjectField<RectTransform>(controller, "heroDetailRoot");
        if (!heroDetailRoot.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Hero detail window should be active after opening a hero card.");
        }

        var levelButton = RequireButtonField(controller, "heroDetailLevelButton");
        RequireInsidePanel(heroDetailRoot.gameObject, levelButton.gameObject);
        var equipGearButton = RequireButtonField(controller, "heroDetailEquipGearButton");
        RequireInsidePanel(heroDetailRoot.gameObject, equipGearButton.gameObject);
        var removeGearButton = RequireButtonField(controller, "heroDetailRemoveGearButton");
        RequireInsidePanel(heroDetailRoot.gameObject, removeGearButton.gameObject);
        AssertButtonLabel(levelButton, GetLocalizedText(controller, "ui.common.level_up"), "Hero detail level action should use localized text.");

        var gearSlots = RequireField<Button[]>(controller, "heroDetailGearSlotButtons");
        var expectedGearSlotCount = 2 + GetStaticArray(typeof(IdlePrototypeController), "AccessorySlots").Length;
        if (gearSlots.Length < expectedGearSlotCount)
        {
            throw new InvalidOperationException($"Hero detail should expose {expectedGearSlotCount} clickable gear slots.");
        }

        for (var i = 0; i < expectedGearSlotCount; i++)
        {
            if (gearSlots[i] == null)
            {
                throw new InvalidOperationException($"Hero detail gear slot {i + 1} is missing its button.");
            }

            RequireInsidePanel(heroDetailRoot.gameObject, gearSlots[i].gameObject);
        }

        ValidateHeroDetailEquipmentArt(controller, heroDetailRoot.gameObject, gearSlots, expectedGearSlotCount);
        ValidateHeroDetailGearLayout(heroDetailRoot.gameObject, gearSlots, expectedGearSlotCount);
        ValidateHeroDetailEmptyAccessoryInventoryHint(controller, gearSlots);

        InvokePrivate(controller, "OpenSelectedHeroDetailGearOptions");
        Canvas.ForceUpdateCanvases();
        var gearListRoot = RequireObjectField<RectTransform>(controller, "heroDetailGearListRoot");
        if (!gearListRoot.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Hero detail Equip Gear should open the selected gear list instead of leaving the Heroes screen.");
        }

        var gearListCloseButton = RequireButtonField(controller, "heroDetailGearListCloseButton");
        var gearOptionButtons = RequireField<Button[]>(controller, "heroDetailGearOptionButtons");
        ValidateHeroDetailGearListLayout(heroDetailRoot.gameObject, gearListRoot.gameObject, gearListCloseButton, gearOptionButtons);
        ValidateHeroDetailEquipmentGearList(controller, heroDetailRoot, gearListRoot, equipGearButton, gearOptionButtons);

        SetPrivateField(controller, "backendGameplayEnabled", false);
        InvokePrivate(controller, "SetHeroEquippedAccessory", 0, 0, 0, 1);
        InvokePrivate(controller, "ShowHeroDetailGearSlot", 2);
        Canvas.ForceUpdateCanvases();
        ValidateHeroDetailAccessoryGearList(gearListRoot.gameObject, gearOptionButtons);
        ValidateHeroDetailAccessoryOwnedRowsFirst(controller, gearOptionButtons);
        AssertButtonLabel(equipGearButton, GetLocalizedText(controller, "ui.common.equip_gear"), "Hero detail accessory action should keep the Equip Gear label.");
        AssertButtonLabel(removeGearButton, GetLocalizedText(controller, "ui.common.remove_gear"), "Hero detail remove action should use localized text.");
        if (!(bool)InvokePrivate(controller, "CanRemoveSelectedHeroDetailAccessory") || !removeGearButton.interactable)
        {
            throw new InvalidOperationException("Hero detail Remove Gear should be available for a locally equipped accessory slot.");
        }

        SetPrivateField(controller, "backendGameplayEnabled", true);
        if (!(bool)InvokePrivate(controller, "CanRemoveSelectedHeroDetailAccessory"))
        {
            throw new InvalidOperationException("Hero detail Remove Gear should stay available in Server Mode when a backend-equipped accessory is selected.");
        }

        ValidateHeroDetailLanguageRefresh(controller, gearListRoot, equipGearButton, removeGearButton, gearOptionButtons);

        SetPrivateField(controller, "backendGameplayEnabled", false);
        InvokePrivate(controller, "SetHeroEquippedAccessory", 0, 0, -1, 0);
    }

    private static void ValidateHeroDetailEmptyAccessoryInventoryHint(IdlePrototypeController controller, Button[] gearSlots)
    {
        const int heroIndex = 0;
        const int accessorySlot = 1;
        const int gearSlotIndex = accessorySlot + 2;
        const int rarity = 3;
        const int addedCopies = 2;

        var originalRarity = (int)InvokePrivate(controller, "GetHeroEquippedAccessoryRarity", heroIndex, accessorySlot);
        var originalLevel = (int)InvokePrivate(controller, "GetHeroEquippedAccessoryLevel", heroIndex, accessorySlot);
        var originalCopies = (int)InvokePrivate(controller, "GetAccessoryInventoryCount", accessorySlot, rarity);

        try
        {
            InvokePrivate(controller, "SetHeroEquippedAccessory", heroIndex, accessorySlot, -1, 0);
            InvokePrivate(controller, "AddAccessoryInventory", accessorySlot, rarity, addedCopies);

            var emptyColor = new Color(0.36f, 0.22f, 0.13f, 0.9f);
            var baseFrameColor = (Color)InvokePrivate(controller, "GetHeroDetailGearSlotColor", gearSlotIndex);
            if (ApproximatelySameColor(baseFrameColor, emptyColor))
            {
                throw new InvalidOperationException("Hero detail accessory slot with bag copies should use a rarity-tinted base highlight.");
            }

            InvokePrivate(controller, "ShowHeroDetailGearSlot", gearSlotIndex);
            Canvas.ForceUpdateCanvases();

            var slotText = gearSlots[gearSlotIndex].GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (slotText == null)
            {
                throw new InvalidOperationException("Hero detail accessory slot should keep a visible label.");
            }

            var expectedCopies = originalCopies + addedCopies;
            if (!slotText.text.Contains(GetLocalizedText(controller, "ui.common.bag")) || !slotText.text.Contains("R3") || !slotText.text.Contains($"x{expectedCopies}"))
            {
                throw new InvalidOperationException($"Hero detail empty accessory slot should show the best bag copy hint. Got '{slotText.text}'.");
            }

            AssertTextFits(slotText, gearSlots[gearSlotIndex].name, "Hero detail empty accessory bag hint");

            var frame = gearSlots[gearSlotIndex].GetComponent<Image>();
            if (frame == null)
            {
                throw new InvalidOperationException("Hero detail accessory slot should keep a visible frame image.");
            }

            if (ApproximatelySameColor(frame.color, emptyColor))
            {
                throw new InvalidOperationException("Hero detail accessory slot with bag copies should be visually highlighted.");
            }
        }
        finally
        {
            InvokePrivate(controller, "AddAccessoryInventory", accessorySlot, rarity, -addedCopies);
            InvokePrivate(controller, "SetHeroEquippedAccessory", heroIndex, accessorySlot, originalRarity, originalLevel);
            InvokePrivate(controller, "RefreshHeroDetailUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static bool ApproximatelySameColor(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f
            && Mathf.Abs(a.g - b.g) < 0.01f
            && Mathf.Abs(a.b - b.b) < 0.01f
            && Mathf.Abs(a.a - b.a) < 0.01f;
    }

    private static void ValidateHeroDetailLanguageRefresh(IdlePrototypeController controller, RectTransform gearListRoot, Button equipGearButton, Button removeGearButton, Button[] gearOptionButtons)
    {
        var originalLanguage = RequireField<MythwakeLanguage>(controller, "language");
        try
        {
            SetPrivateField(controller, "language", MythwakeLanguage.German);
            InvokePrivate(controller, "RefreshHeroDetailUi");
            InvokePrivate(controller, "ShowHeroDetailGearSlot", 0);
            Canvas.ForceUpdateCanvases();

            AssertButtonLabel(equipGearButton, MythwakeLocalization.Text(MythwakeLanguage.German, "ui.common.open_gear_short"), "Hero detail equipment action should refresh when language changes.");
            AssertButtonLabel(removeGearButton, MythwakeLocalization.Text(MythwakeLanguage.German, "ui.common.remove_gear"), "Hero detail remove action should refresh when language changes.");
            AssertHeroDetailGearListTextFits(gearListRoot.gameObject, gearOptionButtons, "Hero detail German equipment list");

            InvokePrivate(controller, "ShowHeroDetailGearSlot", 2);
            Canvas.ForceUpdateCanvases();

            ValidateHeroDetailAccessoryGearList(gearListRoot.gameObject, gearOptionButtons);
            AssertButtonLabel(equipGearButton, MythwakeLocalization.Text(MythwakeLanguage.German, "ui.common.equip_gear"), "Hero detail accessory action should refresh when language changes.");
            AssertButtonLabel(removeGearButton, MythwakeLocalization.Text(MythwakeLanguage.German, "ui.common.remove_gear"), "Hero detail remove action should stay localized after slot changes.");
            AssertHeroDetailGearListTextFits(gearListRoot.gameObject, gearOptionButtons, "Hero detail German accessory list");
        }
        finally
        {
            SetPrivateField(controller, "language", originalLanguage);
            InvokePrivate(controller, "RefreshHeroDetailUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ValidateHeroDetailAccessoryGearList(GameObject gearListRoot, Button[] gearOptionButtons)
    {
        if (!gearListRoot.activeInHierarchy)
        {
            throw new InvalidOperationException("Hero detail accessory gear list should be active.");
        }

        for (var i = 0; i < gearOptionButtons.Length; i++)
        {
            var option = gearOptionButtons[i];
            if (option == null)
            {
                throw new InvalidOperationException($"Hero detail accessory option {i + 1} is missing its button.");
            }

            if (!option.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException($"Hero detail accessory list should show rarity option row {i + 1}.");
            }
        }

        if (gearOptionButtons.Length < 2)
        {
            throw new InvalidOperationException("Hero detail accessory list should expose multiple rarity option rows.");
        }

        var lowestRarityRect = gearOptionButtons[0].GetComponent<RectTransform>();
        var highestRarityRect = gearOptionButtons[gearOptionButtons.Length - 1].GetComponent<RectTransform>();
        if (lowestRarityRect == null || highestRarityRect == null)
        {
            throw new InvalidOperationException("Hero detail accessory option rows should keep RectTransforms.");
        }

        if (highestRarityRect.anchoredPosition.y <= lowestRarityRect.anchoredPosition.y)
        {
            throw new InvalidOperationException("Hero detail accessory list should display higher rarity rows above lower rarity rows.");
        }

        if (gearOptionButtons.Length > 0 && gearOptionButtons[0].interactable)
        {
            throw new InvalidOperationException("Hero detail equipped accessory option row should not be clickable.");
        }
    }

    private static void ValidateHeroDetailAccessoryOwnedRowsFirst(IdlePrototypeController controller, Button[] gearOptionButtons)
    {
        const int heroIndex = 0;
        const int accessorySlot = 1;
        const int gearSlotIndex = accessorySlot + 2;
        const int lowerOwnedRarity = 1;
        const int higherOwnedRarity = 3;
        const int emptyRarity = 4;

        if (gearOptionButtons.Length <= emptyRarity)
        {
            throw new InvalidOperationException("Hero detail accessory list should expose every rarity row for owned-row ordering.");
        }

        var originalRarity = (int)InvokePrivate(controller, "GetHeroEquippedAccessoryRarity", heroIndex, accessorySlot);
        var originalLevel = (int)InvokePrivate(controller, "GetHeroEquippedAccessoryLevel", heroIndex, accessorySlot);
        var originalCopies = new int[gearOptionButtons.Length];
        for (var rarity = 0; rarity < originalCopies.Length; rarity++)
        {
            originalCopies[rarity] = (int)InvokePrivate(controller, "GetAccessoryInventoryCount", accessorySlot, rarity);
        }

        try
        {
            InvokePrivate(controller, "SetHeroEquippedAccessory", heroIndex, accessorySlot, -1, 0);
            for (var rarity = 0; rarity < originalCopies.Length; rarity++)
            {
                InvokePrivate(controller, "AddAccessoryInventory", accessorySlot, rarity, -originalCopies[rarity]);
            }

            InvokePrivate(controller, "AddAccessoryInventory", accessorySlot, lowerOwnedRarity, 1);
            InvokePrivate(controller, "AddAccessoryInventory", accessorySlot, higherOwnedRarity, 1);
            InvokePrivate(controller, "ShowHeroDetailGearSlot", gearSlotIndex);
            Canvas.ForceUpdateCanvases();

            if (!gearOptionButtons[higherOwnedRarity].interactable || !gearOptionButtons[lowerOwnedRarity].interactable)
            {
                throw new InvalidOperationException("Hero detail accessory rows with owned copies should stay clickable.");
            }

            var higherOwnedRect = RequireRectTransform(gearOptionButtons[higherOwnedRarity].gameObject);
            var lowerOwnedRect = RequireRectTransform(gearOptionButtons[lowerOwnedRarity].gameObject);
            var emptyRect = RequireRectTransform(gearOptionButtons[emptyRarity].gameObject);
            if (higherOwnedRect.anchoredPosition.y <= lowerOwnedRect.anchoredPosition.y)
            {
                throw new InvalidOperationException("Hero detail owned accessory rows should keep higher rarity above lower rarity.");
            }

            if (lowerOwnedRect.anchoredPosition.y <= emptyRect.anchoredPosition.y)
            {
                throw new InvalidOperationException("Hero detail owned accessory rows should appear above empty rarity rows.");
            }
        }
        finally
        {
            for (var rarity = 0; rarity < originalCopies.Length; rarity++)
            {
                var currentCopies = (int)InvokePrivate(controller, "GetAccessoryInventoryCount", accessorySlot, rarity);
                InvokePrivate(controller, "AddAccessoryInventory", accessorySlot, rarity, -currentCopies);
                InvokePrivate(controller, "AddAccessoryInventory", accessorySlot, rarity, originalCopies[rarity]);
            }

            InvokePrivate(controller, "SetHeroEquippedAccessory", heroIndex, accessorySlot, originalRarity, originalLevel);
            InvokePrivate(controller, "ShowHeroDetailGearSlot", 2);
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void ValidateHeroDetailEquipmentGearList(IdlePrototypeController controller, RectTransform heroDetailRoot, RectTransform gearListRoot, Button equipGearButton, Button[] gearOptionButtons)
    {
        InvokePrivate(controller, "HideHeroDetailGearList");
        InvokePrivate(controller, "ShowHeroDetailGearSlot", 0);
        InvokePrivate(controller, "OpenSelectedHeroDetailGearOptions");
        Canvas.ForceUpdateCanvases();
        AssertButtonLabel(equipGearButton, GetLocalizedText(controller, "ui.common.open_gear_short"), "Hero detail equipment action should use the Open Gear label.");

        if (!heroDetailRoot.gameObject.activeInHierarchy || !gearListRoot.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Hero detail Equip Gear should open the selected equipment slot list before navigating away.");
        }

        AssertHeroDetailGearListTextFits(gearListRoot.gameObject, gearOptionButtons, "Hero detail equipment list");

        var gearPanel = RequireObjectField<GameObject>(controller, "gearPanel");
        if (gearPanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Hero detail equipment slot list should not immediately leave for the Gear screen.");
        }

        for (var i = 0; i < gearOptionButtons.Length; i++)
        {
            var option = gearOptionButtons[i];
            if (option == null)
            {
                throw new InvalidOperationException($"Hero detail gear option {i + 1} is missing its button.");
            }

            if (i < 2 && !option.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException($"Hero detail equipment option {i + 1} should be visible.");
            }

            if (i >= 2 && option.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException($"Hero detail equipment list should hide unused option row {i + 1}.");
            }
        }

        if (gearOptionButtons[0].interactable)
        {
            throw new InvalidOperationException("Hero detail equipped equipment summary row should not be clickable.");
        }

        if (!gearOptionButtons[1].interactable)
        {
            throw new InvalidOperationException("Hero detail equipment list should expose an Open Gear action row.");
        }

        InvokePrivate(controller, "EquipHeroDetailGearOption", 1);
        Canvas.ForceUpdateCanvases();

        if (!gearPanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Hero detail equipment Open Gear row should navigate to the Gear screen.");
        }

        controller.ShowHeroes();
        InvokePrivate(controller, "ShowHeroDetail", 0);
        Canvas.ForceUpdateCanvases();
    }

    private static void AssertButtonLabel(Button button, string expected, string message)
    {
        var label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (label == null)
        {
            throw new InvalidOperationException($"{button.name} is missing its label text.");
        }

        if (!string.Equals(label.text, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{message} Expected '{expected}', got '{label.text}'.");
        }

        AssertButtonLabelFits(button, label, message);
    }

    private static void AssertButtonLabelFits(Button button, TMP_Text label, string message)
    {
        AssertTextFits(label, button.name, message);
    }

    private static void AssertButtonTextFits(Button button, string message)
    {
        var label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (label == null)
        {
            throw new InvalidOperationException($"{button.name} is missing its label text.");
        }

        AssertTextFits(label, button.name, message);
    }

    private static void AssertHeroDetailGearListTextFits(GameObject gearListRoot, Button[] gearOptionButtons, string message)
    {
        var title = gearListRoot.transform.Find("Title")?.GetComponent<TMP_Text>();
        if (title == null)
        {
            throw new InvalidOperationException($"{gearListRoot.name} is missing its title text.");
        }

        AssertTextFits(title, $"{gearListRoot.name} Title", message);

        for (var i = 0; i < gearOptionButtons.Length; i++)
        {
            var option = gearOptionButtons[i];
            if (option != null && option.gameObject.activeInHierarchy)
            {
                AssertButtonTextFits(option, $"{message} option {i + 1}");
            }
        }
    }

    private static void AssertTextFits(TMP_Text label, string ownerName, string message)
    {
        label.ForceMeshUpdate();
        if (!label.isTextOverflowing)
        {
            return;
        }

        var rect = label.rectTransform.rect;
        throw new InvalidOperationException($"{message}: '{label.text}' overflows {ownerName}: labelWidth={rect.width}, labelHeight={rect.height}, fontSize={label.fontSize}.");
    }

    private static string GetLocalizedText(IdlePrototypeController controller, string key)
    {
        return (string)InvokePrivate(controller, "Tr", key);
    }

    private static void ValidateHeroDetailEquipmentArt(IdlePrototypeController controller, GameObject heroDetailRoot, Button[] gearSlots, int expectedGearSlotCount)
    {
        var background = RequireSceneObject("Hero Detail Armory Background");
        RequireInsidePanel(heroDetailRoot, background);

        var backgroundImage = background.GetComponent<RawImage>();
        if (backgroundImage == null || backgroundImage.texture == null)
        {
            throw new InvalidOperationException("Hero detail armory background should render the equipment art texture.");
        }

        if (backgroundImage.raycastTarget)
        {
            throw new InvalidOperationException("Hero detail armory background should not intercept gear slot input.");
        }

        var slotIcons = RequireField<RawImage[]>(controller, "heroDetailGearSlotIcons");
        if (slotIcons.Length < expectedGearSlotCount)
        {
            throw new InvalidOperationException($"Hero detail should expose {expectedGearSlotCount} gear slot icons.");
        }

        for (var i = 0; i < expectedGearSlotCount; i++)
        {
            var icon = slotIcons[i];
            if (icon == null)
            {
                throw new InvalidOperationException($"Hero detail gear slot {i + 1} is missing its icon.");
            }

            if (icon.texture == null)
            {
                throw new InvalidOperationException($"Hero detail gear slot {i + 1} should render equipment icon art.");
            }

            if (icon.raycastTarget)
            {
                throw new InvalidOperationException($"Hero detail gear slot {i + 1} icon should not intercept button input.");
            }

            if (!icon.transform.IsChildOf(gearSlots[i].transform))
            {
                throw new InvalidOperationException($"Hero detail gear slot {i + 1} icon should stay inside its slot.");
            }

            RequireInsidePanel(gearSlots[i].gameObject, icon.gameObject);
            var iconRect = RequireRectTransform(icon.gameObject);
            if (iconRect.rect.width <= 0f || iconRect.rect.height <= 0f || iconRect.rect.width > 84.5f || iconRect.rect.height > 56.5f)
            {
                throw new InvalidOperationException($"Hero detail gear slot {i + 1} icon should fit within the icon frame. width={iconRect.rect.width}, height={iconRect.rect.height}.");
            }

            var label = gearSlots[i].GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (label != null)
            {
                AssertNoOverlap(icon.gameObject, label.gameObject, 0f, "Hero Detail gear slot icon layout");
            }
        }
    }

    private static void ValidateHeroDetailGearLayout(GameObject heroDetailRoot, Button[] gearSlots, int expectedGearSlotCount)
    {
        var guardedObjects = new[]
        {
            RequireSceneObject("Hero Detail Stage"),
            RequireSceneObject("Hero Detail Portrait"),
            RequireSceneObject("Hero Detail Stat Backplate"),
            RequireSceneObject("Hero Detail Stats"),
            RequireSceneObject("Hero Detail Resources"),
            RequireSceneObject("Hero Detail Remove Gear Button"),
            RequireSceneObject("Hero Detail Level Button"),
            RequireSceneObject("Hero Detail Equip Gear Button"),
        };

        for (var i = 0; i < expectedGearSlotCount; i++)
        {
            var gearSlot = gearSlots[i].gameObject;
            if (!gearSlot.transform.IsChildOf(heroDetailRoot.transform))
            {
                throw new InvalidOperationException($"{gearSlot.name} should stay directly inside {heroDetailRoot.name}.");
            }

            for (var otherIndex = i + 1; otherIndex < expectedGearSlotCount; otherIndex++)
            {
                AssertNoOverlap(gearSlot, gearSlots[otherIndex].gameObject, 4f, "Hero Detail gear slot spacing");
            }

            for (var guardedIndex = 0; guardedIndex < guardedObjects.Length; guardedIndex++)
            {
                AssertNoOverlap(gearSlot, guardedObjects[guardedIndex], 4f, "Hero Detail gear layout");
            }
        }
    }

    private static void ValidateHeroDetailGearListLayout(GameObject heroDetailRoot, GameObject gearListRoot, Button gearListCloseButton, Button[] gearOptionButtons)
    {
        RequireInsidePanel(heroDetailRoot, gearListRoot);
        RequireInsidePanel(gearListRoot, gearListCloseButton.gameObject);

        GameObject previousOption = null;
        for (var i = 0; i < gearOptionButtons.Length; i++)
        {
            var option = gearOptionButtons[i];
            if (option == null || !option.gameObject.activeInHierarchy)
            {
                continue;
            }

            RequireInsidePanel(gearListRoot, option.gameObject);
            AssertNoOverlap(gearListCloseButton.gameObject, option.gameObject, 4f, "Hero Detail gear list layout");
            AssertButtonTextFits(option, "Hero Detail gear list option text");
            if (previousOption != null)
            {
                AssertNoOverlap(previousOption, option.gameObject, 4f, "Hero Detail gear list option spacing");
            }

            previousOption = option.gameObject;
        }

        AssertHeroDetailGearListTextFits(gearListRoot, gearOptionButtons, "Hero Detail gear list");
    }

    private static void ValidateGearScreen(IdlePrototypeController controller)
    {
        controller.ShowGear();
        Canvas.ForceUpdateCanvases();

        var gearPanel = RequireObjectField<GameObject>(controller, "gearPanel");
        if (!gearPanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Gear panel should be active after ShowGear.");
        }

        RequireToolButtonInPanel(controller, "weaponUpgradeButton", gearPanel);
        RequireToolButtonInPanel(controller, "armorUpgradeButton", gearPanel);
        RequireToolButtonInPanel(controller, "accessoryEquipButton", gearPanel);
        RequireToolButtonInPanel(controller, "accessoryLevelButton", gearPanel);
        RequireToolButtonInPanel(controller, "accessoryFuseButton", gearPanel);
    }

    private static void ValidateShopTools(IdlePrototypeController controller)
    {
        controller.ShowShop();
        Canvas.ForceUpdateCanvases();

        var shopPanel = RequireObjectField<GameObject>(controller, "shopPanel");
        if (!shopPanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Shop panel should be active after ShowShop.");
        }

        RequireToolButtonInPanel(controller, "debugGoldButton", shopPanel);
        RequireToolButtonInPanel(controller, "debugEssenceButton", shopPanel);
        RequireToolButtonInPanel(controller, "debugGemsButton", shopPanel);
        RequireToolButtonInPanel(controller, "debugAccessoryButton", shopPanel);
    }

    private static void RequireToolButtonInPanel(IdlePrototypeController controller, string fieldName, GameObject panel)
    {
        var button = RequireButtonField(controller, fieldName);
        if (!button.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException($"{button.name} should be active while {panel.name} is open.");
        }

        RequireInsidePanel(panel, button.gameObject);
    }

    private static void RequireInsidePanel(GameObject panel, GameObject child)
    {
        if (!child.transform.IsChildOf(panel.transform))
        {
            throw new InvalidOperationException($"{child.name} should be parented under {panel.name}.");
        }

        AssertInsideParent(panel, child);
    }

    private static void RequireNotUnderPanel(Button button, GameObject panel, string label)
    {
        if (button.transform.IsChildOf(panel.transform))
        {
            throw new InvalidOperationException($"{label} should not be under {panel.name}.");
        }
    }

    private static void RequireInactive(Button button, string label)
    {
        if (button.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException($"{label} should be hidden.");
        }
    }

    private static void AssertInsideParent(GameObject parent, GameObject child)
    {
        var parentRect = parent.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            throw new InvalidOperationException($"{parent.name} is missing a RectTransform.");
        }

        var parentWidth = parentRect.rect.width;
        var parentHeight = parentRect.rect.height;
        var childBounds = GetLocalBounds(child);

        if (childBounds.Left < -parentWidth * 0.5f || childBounds.Right > parentWidth * 0.5f || childBounds.Top > 0f || childBounds.Bottom < -parentHeight)
        {
            throw new InvalidOperationException($"{child.name} is outside {parent.name}: left={childBounds.Left}, right={childBounds.Right}, top={childBounds.Top}, bottom={childBounds.Bottom}.");
        }
    }

    private static void AssertNoOverlap(GameObject first, GameObject second, float padding, string context)
    {
        var firstRect = RequireRectTransform(first);
        var secondRect = RequireRectTransform(second);
        if (firstRect.parent != secondRect.parent)
        {
            throw new InvalidOperationException($"{context}: {first.name} and {second.name} must share a parent for layout comparison.");
        }

        var firstBounds = GetLocalBounds(first);
        var secondBounds = GetLocalBounds(second);
        if (BoundsOverlap(firstBounds, secondBounds, padding))
        {
            throw new InvalidOperationException($"{context}: {first.name} overlaps {second.name}. First left={firstBounds.Left}, right={firstBounds.Right}, top={firstBounds.Top}, bottom={firstBounds.Bottom}; second left={secondBounds.Left}, right={secondBounds.Right}, top={secondBounds.Top}, bottom={secondBounds.Bottom}.");
        }
    }

    private static RectTransform RequireRectTransform(GameObject gameObject)
    {
        var rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            throw new InvalidOperationException($"{gameObject.name} is missing a RectTransform.");
        }

        return rect;
    }

    private static LocalBounds GetLocalBounds(GameObject gameObject)
    {
        var rect = RequireRectTransform(gameObject);
        var left = rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
        var right = left + rect.rect.width;
        var top = rect.anchoredPosition.y + rect.rect.height * (1f - rect.pivot.y);
        var bottom = top - rect.rect.height;
        return new LocalBounds(left, right, top, bottom);
    }

    private static bool BoundsOverlap(LocalBounds first, LocalBounds second, float padding)
    {
        return first.Left < second.Right + padding
            && first.Right > second.Left - padding
            && first.Bottom < second.Top + padding
            && first.Top > second.Bottom - padding;
    }

    private struct LocalBounds
    {
        public LocalBounds(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public float Left { get; }
        public float Right { get; }
        public float Top { get; }
        public float Bottom { get; }
    }

    private static Button RequireButtonField(object target, string fieldName)
    {
        var button = RequireObjectField<Button>(target, fieldName);
        if (button == null)
        {
            throw new InvalidOperationException($"Missing button field: {fieldName}");
        }

        return button;
    }

    private static T RequireObjectField<T>(object target, string fieldName) where T : UnityEngine.Object
    {
        var value = GetPrivateField<T>(target, fieldName);
        if (value == null)
        {
            throw new InvalidOperationException($"Missing object field: {fieldName}");
        }

        return value;
    }

    private static T RequireField<T>(object target, string fieldName)
    {
        var value = GetPrivateField<T>(target, fieldName);
        if (value == null)
        {
            throw new InvalidOperationException($"Missing field: {fieldName}");
        }

        return value;
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

    private static GameObject FindSceneObject(string name)
    {
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (var i = 0; i < transforms.Length; i++)
        {
            var transform = transforms[i];
            if (transform != null && transform.name == name && transform.gameObject.scene.IsValid())
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static GameObject RequireSceneObject(string name)
    {
        var sceneObject = FindSceneObject(name);
        if (sceneObject == null)
        {
            throw new InvalidOperationException($"Missing scene object: {name}");
        }

        return sceneObject;
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

    private static Array GetStaticArray(Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null)
        {
            throw new InvalidOperationException($"{type.Name}.{fieldName} could not be found.");
        }

        var value = field.GetValue(null);
        if (value is Array array)
        {
            return array;
        }

        throw new InvalidOperationException($"{type.Name}.{fieldName} is not an array.");
    }
}
