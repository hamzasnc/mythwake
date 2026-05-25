using System;
using System.Reflection;
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
            Debug.Log("Upgrade clutter validated: legacy Battle/Hero controls are hidden, Gear controls live on Gear, and debug tools live on Shop.");
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

        RequireInsidePanel(heroDetailRoot.gameObject, RequireButtonField(controller, "heroDetailLevelButton").gameObject);
        RequireInsidePanel(heroDetailRoot.gameObject, RequireButtonField(controller, "heroDetailEquipGearButton").gameObject);
        RequireInsidePanel(heroDetailRoot.gameObject, RequireButtonField(controller, "heroDetailRemoveGearButton").gameObject);

        var gearSlots = RequireField<Button[]>(controller, "heroDetailGearSlotButtons");
        if (gearSlots.Length < 6)
        {
            throw new InvalidOperationException("Hero detail should expose six clickable gear slots.");
        }

        for (var i = 0; i < 6; i++)
        {
            if (gearSlots[i] == null)
            {
                throw new InvalidOperationException($"Hero detail gear slot {i + 1} is missing its button.");
            }

            RequireInsidePanel(heroDetailRoot.gameObject, gearSlots[i].gameObject);
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
        var childRect = child.GetComponent<RectTransform>();
        if (parentRect == null || childRect == null)
        {
            throw new InvalidOperationException($"{child.name} or {parent.name} is missing a RectTransform.");
        }

        var parentWidth = parentRect.rect.width;
        var parentHeight = parentRect.rect.height;
        var childWidth = childRect.rect.width;
        var childHeight = childRect.rect.height;
        var left = childRect.anchoredPosition.x - childWidth * childRect.pivot.x;
        var right = left + childWidth;
        var top = childRect.anchoredPosition.y + childHeight * (1f - childRect.pivot.y);
        var bottom = top - childHeight;

        if (left < -parentWidth * 0.5f || right > parentWidth * 0.5f || top > 0f || bottom < -parentHeight)
        {
            throw new InvalidOperationException($"{child.name} is outside {parent.name}: left={left}, right={right}, top={top}, bottom={bottom}.");
        }
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
}
