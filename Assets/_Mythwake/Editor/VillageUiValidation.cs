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

        ForceBuiltVillagePlot(controller, 0, 0, 1);
        InvokePrivate(controller, "SelectVillagePlot", 0);
        Canvas.ForceUpdateCanvases();

        RequireActive("Village Building Detail Panel");
        if (!upgradeButton.interactable)
        {
            throw new InvalidOperationException("Village upgrade button should be interactable for a level 1 built plot.");
        }

        if (!demolishButton.interactable)
        {
            throw new InvalidOperationException("Village demolish button should be interactable for a built plot.");
        }

        var upgradeLabel = GetButtonLabel(upgradeButton);
        if (!upgradeLabel.Contains("Aufwerten") || !upgradeLabel.Contains("5"))
        {
            throw new InvalidOperationException($"Village upgrade button label mismatch: '{upgradeLabel}'");
        }

        var detailBody = RequireObject("Village Building Detail Body", true).GetComponent<TMP_Text>();
        if (detailBody == null || !detailBody.text.Contains("Lv. 1") || !detailBody.text.Contains("Upgrade auf Lv. 2"))
        {
            throw new InvalidOperationException($"Village building detail body missing level/upgrade copy: '{(detailBody == null ? "<missing>" : detailBody.text)}'");
        }

        if (!villagePanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Village panel should be active after ShowVillage.");
        }
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
