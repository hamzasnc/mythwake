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
            Debug.Log("Village UI validated: map, all build plots, build panel, built-building detail panel, upgrade, demolish, and close controls are present.");
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

        SetPrivateField(controller, "backendGameplayEnabled", false);
        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "RegisterNavigation");
        controller.ShowVillage();
        Canvas.ForceUpdateCanvases();

        var villagePanel = RequireObject("Village Panel", true);
        var mapViewport = RequireObject("Village Map Viewport", true);
        var mapContent = RequireObject("Village Map Content", true);
        var mapImage = RequireRawImageWithTexture("Village Empty Map");
        ValidateVillageScrollRect(mapViewport, mapContent);
        AssertInsideParent(mapContent, mapImage.gameObject);
        ValidateVillageMapPlots(mapContent);
        ValidateVillageDefinitionCatalog();
        AssertVillageBonusHint("Lokal:", "Village empty bonus hint");
        AssertVillageBonusHint("keine Village Boni", "Village empty bonus hint");

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
        ValidateVillageBuildPanelOptions(buildPanel);
        if (!buildButton.interactable)
        {
            throw new InvalidOperationException("Village build button should be interactable for a free selected plot.");
        }

        buildCloseButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (buildPanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Village build panel should close from its close button.");
        }

        AssertSelectedVillagePlot(controller, -1, "Village build close");

        AssertVillageBuiltPlotDetail(controller, 0, 0, 1, "Team HP", "Upgrade auf Lv. 2", "Aufwerten (5)", true, upgradeButton, demolishButton);
        AssertVillageBuiltPlotDetail(controller, 1, 0, 2, "Team ATK", "Upgrade auf Lv. 3", "Aufwerten (10)", true, upgradeButton, demolishButton);
        AssertVillageBuiltPlotDetail(controller, 2, 0, 3, "Essence/s Fast Rewards", "Upgrade auf Lv. 4", "Aufwerten (15)", true, upgradeButton, demolishButton);
        AssertVillageBuiltPlotDetail(controller, 5, 0, 4, "Gold/s Fast Rewards", "Upgrade auf Lv. 5", "Aufwerten (20)", true, upgradeButton, demolishButton);
        AssertVillageBuiltPlotDetail(controller, 0, 0, 20, "Team HP", "Max Level erreicht.", "Max", false, upgradeButton, demolishButton);
        ValidateVillageDetailClose(controller, 3, 0, 1, detailCloseButton, detailPanel);
        ValidateVillageServerModeHint(controller);

        if (!villagePanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Village panel should be active after ShowVillage.");
        }
    }

    private static void ValidateVillageMapPlots(GameObject mapContent)
    {
        for (var i = 0; i < 12; i++)
        {
            var plot = RequireButton($"Village Build Plot {i + 1}");
            AssertInsideParent(mapContent, plot.gameObject);

            var buildMark = FindChild(plot.transform, "Build Mark");
            if (buildMark == null || buildMark.GetComponent<TMP_Text>() == null)
            {
                throw new InvalidOperationException($"Village Build Plot {i + 1} is missing its build mark label.");
            }
        }
    }

    private static void ValidateVillageDefinitionCatalog()
    {
        for (var plot = 0; plot < 12; plot++)
        {
            var expectedBonusToken = GetExpectedDefinitionBonusToken(plot);
            for (var option = 0; option < 3; option++)
            {
                var id = (string)InvokePrivateStatic("GetVillageBuildingId", plot, option);
                var expectedId = $"village_building_{plot + 1:D2}_option_{option + 1:D2}";
                if (id != expectedId)
                {
                    throw new InvalidOperationException($"Village definition ID mismatch for plot {plot + 1} option {option + 1}: expected {expectedId}, got {id}.");
                }

                var buildCost = (int)InvokePrivateStatic("GetVillagePlotBuildCost", plot, option);
                var expectedBuildCost = 5 + (option * 2);
                if (buildCost != expectedBuildCost)
                {
                    throw new InvalidOperationException($"Village definition build-cost mismatch for {id}: expected {expectedBuildCost}, got {buildCost}.");
                }

                var maxLevel = (int)InvokePrivateStatic("GetVillageBuildingMaxLevel", plot, option);
                if (maxLevel != 20)
                {
                    throw new InvalidOperationException($"Village definition max-level mismatch for {id}: expected 20, got {maxLevel}.");
                }

                var textureName = (string)InvokePrivateStatic("GetVillageBuildingTextureName", plot, option);
                var texture = (Texture2D)InvokePrivateStatic("LoadVillageBuildingTexture", plot, option);
                if (string.IsNullOrWhiteSpace(textureName) || texture == null)
                {
                    throw new InvalidOperationException($"Village definition {id} has missing texture '{textureName}'.");
                }

                var bonusText = (string)InvokePrivateStatic("GetVillageBuildingBonusValueText", plot, option, 1);
                RequireCopy(bonusText, expectedBonusToken, $"Village definition {id} bonus");
            }
        }
    }

    private static string GetExpectedDefinitionBonusToken(int plotIndex)
    {
        switch (plotIndex)
        {
            case 0:
            case 7:
            case 8:
                return "Team HP";
            case 1:
            case 3:
            case 11:
                return "Team ATK";
            case 5:
            case 6:
            case 10:
                return "Gold/s Fast Rewards";
            default:
                return "Essence/s Fast Rewards";
        }
    }

    private static void ValidateVillageScrollRect(GameObject mapViewport, GameObject mapContent)
    {
        var scrollRect = mapViewport.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            throw new InvalidOperationException("Village Map Viewport is missing its ScrollRect.");
        }

        if (scrollRect.content == null || scrollRect.content.gameObject != mapContent)
        {
            throw new InvalidOperationException("Village Map Viewport ScrollRect should use Village Map Content as content.");
        }

        if (!scrollRect.vertical || scrollRect.horizontal)
        {
            throw new InvalidOperationException("Village Map Viewport should scroll vertically only.");
        }

        var viewportRect = mapViewport.GetComponent<RectTransform>();
        var contentRect = mapContent.GetComponent<RectTransform>();
        if (viewportRect == null || contentRect == null || contentRect.rect.height <= viewportRect.rect.height)
        {
            throw new InvalidOperationException("Village Map Content should be taller than the viewport so the village map can scroll.");
        }
    }

    private static void ValidateVillageBuildPanelOptions(GameObject buildPanel)
    {
        for (var i = 0; i < 3; i++)
        {
            var option = RequireButton($"Village Build Option {i + 1}");
            AssertInsideParent(buildPanel, option.gameObject);

            var icon = FindChild(option.transform, "Icon")?.GetComponent<RawImage>();
            if (icon == null || icon.texture == null)
            {
                throw new InvalidOperationException($"Village Build Option {i + 1} is missing loaded building art.");
            }

            var label = FindChild(option.transform, "Label")?.GetComponent<TMP_Text>();
            if (label == null)
            {
                throw new InvalidOperationException($"Village Build Option {i + 1} is missing its label.");
            }

            AssertTextFits(label, $"Village Build Option {i + 1} label");
        }
    }

    private static void ValidateVillageDetailClose(IdlePrototypeController controller, int plotIndex, int buildingOptionIndex, int level, Button detailCloseButton, GameObject detailPanel)
    {
        ForceBuiltVillagePlot(controller, plotIndex, buildingOptionIndex, level);
        InvokePrivate(controller, "SelectVillagePlot", plotIndex);
        Canvas.ForceUpdateCanvases();

        RequireActive("Village Building Detail Panel");
        detailCloseButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        if (detailPanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Village building detail panel should close from its close button.");
        }

        AssertSelectedVillagePlot(controller, -1, "Village detail close");
    }

    private static void AssertVillageBuiltPlotDetail(IdlePrototypeController controller, int plotIndex, int buildingOptionIndex, int level, string expectedBonus, string expectedProgressionCopy, string expectedUpgradeLabel, bool expectUpgradeInteractable, Button upgradeButton, Button demolishButton)
    {
        ForceBuiltVillagePlot(controller, plotIndex, buildingOptionIndex, level);
        InvokePrivate(controller, "SelectVillagePlot", plotIndex);
        Canvas.ForceUpdateCanvases();

        RequireActive("Village Building Detail Panel");
        ValidateBuiltVillagePlotArt(plotIndex);

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
        RequireCopy(detailBody.text, expectUpgradeInteractable ? "Naechster Bonus:" : "Max Bonus:", $"Village plot {plotIndex + 1} detail");
        RequireCopy(detailBody.text, expectedProgressionCopy, $"Village plot {plotIndex + 1} detail");
        RequireCopy(detailBody.text, "Vorhanden:", $"Village plot {plotIndex + 1} detail");
        AssertTextFits(detailBody, $"Village plot {plotIndex + 1} detail body");
        AssertVillageBonusHint("Lokal:", $"Village plot {plotIndex + 1} bonus hint");
        AssertVillageBonusHint(GetVillageHintToken(expectedBonus), $"Village plot {plotIndex + 1} bonus hint");
    }

    private static void ValidateVillageServerModeHint(IdlePrototypeController controller)
    {
        var backendBefore = GetPrivateField<bool>(controller, "backendGameplayEnabled");
        try
        {
            SetPrivateField(controller, "backendGameplayEnabled", true);
            InvokePrivate(controller, "RefreshVillageUi");
            Canvas.ForceUpdateCanvases();

            AssertVillageBonusHint("Server Mode:", "Village server-mode bonus hint");
            AssertVillageBonusHint("lokal pausiert", "Village server-mode bonus hint");
            AssertVillageBonusHint("Server Snapshot", "Village server-mode bonus hint");
        }
        finally
        {
            SetPrivateField(controller, "backendGameplayEnabled", backendBefore);
            InvokePrivate(controller, "RefreshVillageUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static void AssertVillageBonusHint(string expected, string context)
    {
        var hintText = RequireObject("Village Hint", true).GetComponent<TMP_Text>();
        if (hintText == null)
        {
            throw new InvalidOperationException("Village Hint is missing TMP_Text.");
        }

        RequireCopy(hintText.text, expected, context);
        AssertTextFits(hintText, context);
    }

    private static string GetVillageHintToken(string expectedBonus)
    {
        if (expectedBonus.Contains("HP"))
        {
            return "HP";
        }

        if (expectedBonus.Contains("ATK"))
        {
            return "ATK";
        }

        if (expectedBonus.Contains("Gold/s"))
        {
            return "Gold/s";
        }

        return "Essence/s";
    }

    private static void ValidateBuiltVillagePlotArt(int plotIndex)
    {
        var plot = RequireObject($"Village Build Plot {plotIndex + 1}", true);
        var building = FindChild(plot.transform, "Building")?.GetComponent<RawImage>();
        if (building == null)
        {
            throw new InvalidOperationException($"Village Build Plot {plotIndex + 1} is missing its building image.");
        }

        if (!building.gameObject.activeInHierarchy || building.texture == null)
        {
            throw new InvalidOperationException($"Village Build Plot {plotIndex + 1} should show loaded building art after being forced built.");
        }

        var buildMark = FindChild(plot.transform, "Build Mark");
        if (buildMark != null && buildMark.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException($"Village Build Plot {plotIndex + 1} should hide its build mark after being built.");
        }
    }

    private static void AssertSelectedVillagePlot(IdlePrototypeController controller, int expectedIndex, string context)
    {
        var selectedIndex = GetPrivateField<int>(controller, "selectedVillagePlotIndex");
        if (selectedIndex != expectedIndex)
        {
            throw new InvalidOperationException($"{context} selected plot mismatch: expected {expectedIndex}, got {selectedIndex}.");
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

    private static RawImage RequireRawImageWithTexture(string name)
    {
        var gameObject = RequireObject(name, true);
        var image = gameObject.GetComponent<RawImage>();
        if (image == null)
        {
            throw new InvalidOperationException($"{name} is missing a RawImage component.");
        }

        if (image.texture == null)
        {
            throw new InvalidOperationException($"{name} is missing a loaded texture.");
        }

        return image;
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            var match = FindChild(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
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

    private static object InvokePrivateStatic(string methodName, params object[] args)
    {
        var method = typeof(IdlePrototypeController).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Missing private static method: {methodName}");
        }

        return method.Invoke(null, args);
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
