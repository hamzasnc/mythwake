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
            Debug.Log("Upgrade clutter validated: legacy Battle/Hero controls are hidden, Gear controls live on Gear, Hero Detail gear slots fit, and debug tools live on Shop.");
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

        ValidateHeroDetailGearLayout(heroDetailRoot.gameObject, gearSlots, expectedGearSlotCount);

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

        ValidateHeroDetailLanguageRefresh(controller, equipGearButton, removeGearButton);

        SetPrivateField(controller, "backendGameplayEnabled", false);
        InvokePrivate(controller, "SetHeroEquippedAccessory", 0, 0, -1, 0);
    }

    private static void ValidateHeroDetailLanguageRefresh(IdlePrototypeController controller, Button equipGearButton, Button removeGearButton)
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

            InvokePrivate(controller, "ShowHeroDetailGearSlot", 2);
            Canvas.ForceUpdateCanvases();

            AssertButtonLabel(equipGearButton, MythwakeLocalization.Text(MythwakeLanguage.German, "ui.common.equip_gear"), "Hero detail accessory action should refresh when language changes.");
            AssertButtonLabel(removeGearButton, MythwakeLocalization.Text(MythwakeLanguage.German, "ui.common.remove_gear"), "Hero detail remove action should stay localized after slot changes.");
        }
        finally
        {
            SetPrivateField(controller, "language", originalLanguage);
            InvokePrivate(controller, "RefreshHeroDetailUi");
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
    }

    private static string GetLocalizedText(IdlePrototypeController controller, string key)
    {
        return (string)InvokePrivate(controller, "Tr", key);
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
            if (previousOption != null)
            {
                AssertNoOverlap(previousOption, option.gameObject, 4f, "Hero Detail gear list option spacing");
            }

            previousOption = option.gameObject;
        }
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
