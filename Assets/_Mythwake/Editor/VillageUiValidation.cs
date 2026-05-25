using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VillageUiValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Mythwake/Validate Village UI")]
    public static void RunVillageUiValidation()
    {
        try
        {
            ValidateVillageUi();
            Debug.Log("Village UI validated: map, build panel, built-building detail panel, upgrade, demolish, and close controls are present.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateVillageUi()
    {
        EditorSceneManager.OpenScene(ScenePath);

        var controller = FindSceneComponent<IdlePrototypeController>();
        if (controller == null)
        {
            throw new InvalidOperationException("Missing IdlePrototypeController in SampleScene.");
        }

        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "RegisterNavigation");
        controller.ShowVillage();
        Canvas.ForceUpdateCanvases();

        var villagePanel = RequireObject("Village Panel", true);
        RequireObject("Village Map Viewport", true);
        RequireObject("Village Build Plot 1", true);

        var buildPanel = RequireObject("Village Build Panel", false);
        var detailPanel = RequireObject("Village Building Detail Panel", false);
        var buildButton = RequireButton("Village Build Button");
        var buildCloseButton = RequireButton("Village Build Close");
        var upgradeButton = RequireButton("Village Upgrade Button");
        var demolishButton = RequireButton("Village Demolish Button");
        var detailCloseButton = RequireButton("Village Demolish Close");

        AssertInsideParent(buildPanel, buildButton.gameObject);
        AssertInsideParent(buildPanel, buildCloseButton.gameObject);
        AssertInsideParent(detailPanel, upgradeButton.gameObject);
        AssertInsideParent(detailPanel, demolishButton.gameObject);
        AssertInsideParent(detailPanel, detailCloseButton.gameObject);
        AssertInsideParent(detailPanel, RequireObject("Village Building Detail Body", false));

        InvokePrivate(controller, "SelectVillagePlot", 0);
        Canvas.ForceUpdateCanvases();

        RequireActive("Village Build Panel");
        if (!buildButton.interactable)
        {
            throw new InvalidOperationException("Village build button should be interactable for a free selected plot.");
        }

        AssertVillageBuiltPlotDetail(controller, 0, 0, 1, "Team HP", "Upgrade auf Lv. 2", "Aufwerten (5)", true, upgradeButton, demolishButton);
        AssertVillageBuiltPlotDetail(controller, 1, 0, 2, "Team ATK", "Upgrade auf Lv. 3", "Aufwerten (10)", true, upgradeButton, demolishButton);
        AssertVillageBuiltPlotDetail(controller, 2, 0, 3, "Essence/s Fast Rewards", "Upgrade auf Lv. 4", "Aufwerten (15)", true, upgradeButton, demolishButton);
        AssertVillageBuiltPlotDetail(controller, 5, 0, 4, "Gold/s Fast Rewards", "Upgrade auf Lv. 5", "Aufwerten (20)", true, upgradeButton, demolishButton);
        AssertVillageBuiltPlotDetail(controller, 0, 0, 20, "Team HP", "Max Level erreicht.", "Max", false, upgradeButton, demolishButton);

        if (!villagePanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Village panel should be active after ShowVillage.");
        }
    }

    private static void AssertVillageBuiltPlotDetail(IdlePrototypeController controller, int plotIndex, int buildingOptionIndex, int level, string expectedBonus, string expectedProgressionCopy, string expectedUpgradeLabel, bool expectUpgradeInteractable, Button upgradeButton, Button demolishButton)
    {
        ForceBuiltVillagePlot(controller, plotIndex, buildingOptionIndex, level);
        InvokePrivate(controller, "SelectVillagePlot", plotIndex);
        Canvas.ForceUpdateCanvases();

        RequireActive("Village Building Detail Panel");

        if (upgradeButton.interactable != expectUpgradeInteractable)
        {
            throw new InvalidOperationException($"Village upgrade button interactable mismatch for plot {plotIndex + 1} level {level}. Expected {expectUpgradeInteractable}, got {upgradeButton.interactable}.");
        }

        if (!demolishButton.interactable)
        {
            throw new InvalidOperationException($"Village demolish button should be interactable for built plot {plotIndex + 1}.");
        }

        var upgradeLabel = GetButtonLabel(upgradeButton);
        if (upgradeLabel != expectedUpgradeLabel)
        {
            throw new InvalidOperationException($"Village upgrade button label mismatch for plot {plotIndex + 1} level {level}: expected '{expectedUpgradeLabel}', got '{upgradeLabel}'.");
        }

        var detailBody = RequireObject("Village Building Detail Body", true).GetComponent<TMP_Text>();
        if (detailBody == null)
        {
            throw new InvalidOperationException("Village building detail body is missing TMP_Text.");
        }

        RequireCopy(detailBody.text, $"Lv. {level}", $"Village plot {plotIndex + 1} detail");
        RequireCopy(detailBody.text, "Bonus:", $"Village plot {plotIndex + 1} detail");
        RequireCopy(detailBody.text, expectedBonus, $"Village plot {plotIndex + 1} detail");
        RequireCopy(detailBody.text, expectedProgressionCopy, $"Village plot {plotIndex + 1} detail");
        RequireCopy(detailBody.text, "Vorhanden:", $"Village plot {plotIndex + 1} detail");
        AssertTextFits(detailBody, $"Village plot {plotIndex + 1} detail body");
    }

    private static void ForceBuiltVillagePlot(IdlePrototypeController controller, int plotIndex, int buildingOptionIndex, int level)
    {
        var builtStates = GetPrivateField<bool[]>(controller, "villagePlotBuiltStates");
        var buildingSelections = GetPrivateField<int[]>(controller, "villagePlotBuildingSelections");
        var buildingLevels = GetPrivateField<int[]>(controller, "villagePlotBuildingLevels");

        if (builtStates == null || buildingSelections == null || buildingLevels == null ||
            builtStates.Length <= plotIndex || buildingSelections.Length <= plotIndex || buildingLevels.Length <= plotIndex)
        {
            throw new InvalidOperationException("Village state arrays were not initialized.");
        }

        builtStates[plotIndex] = true;
        buildingSelections[plotIndex] = buildingOptionIndex;
        buildingLevels[plotIndex] = level;
        SetPrivateField(controller, "selectedVillagePlotIndex", -1);
        SetPrivateField(controller, "selectedVillageBuildingOptionIndex", buildingOptionIndex);
        SetPrivateField(controller, "villageBuildFeedbackMessage", string.Empty);
        InvokePrivate(controller, "RefreshVillageUi");
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

    private static GameObject RequireObject(string name, bool activeInHierarchy)
    {
        var gameObject = FindSceneObject(name);
        if (gameObject == null)
        {
            throw new InvalidOperationException($"Missing scene object: {name}");
        }

        if (activeInHierarchy && !gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException($"{name} should be active.");
        }

        return gameObject;
    }

    private static void RequireActive(string name)
    {
        RequireObject(name, true);
    }

    private static Button RequireButton(string name)
    {
        var gameObject = RequireObject(name, false);
        var button = gameObject.GetComponent<Button>();
        if (button == null)
        {
            throw new InvalidOperationException($"{name} is missing a Button component.");
        }

        return button;
    }

    private static string GetButtonLabel(Button button)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        return label == null ? string.Empty : label.text;
    }

    private static void RequireCopy(string text, string expected, string context)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.Contains(expected))
        {
            throw new InvalidOperationException($"{context} is missing '{expected}': '{text}'");
        }
    }

    private static void AssertTextFits(TMP_Text label, string context)
    {
        label.ForceMeshUpdate();
        if (!label.isTextOverflowing)
        {
            return;
        }

        var rect = label.rectTransform.rect;
        throw new InvalidOperationException($"{context} overflows: '{label.text}' width={rect.width}, height={rect.height}, fontSize={label.fontSize}.");
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field: {fieldName}");
        }

        field.SetValue(target, value);
    }
}
