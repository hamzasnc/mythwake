using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class DungeonsUiValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Mythwake/Validate Dungeons UI")]
    public static void RunDungeonsUiValidation()
    {
        try
        {
            ValidateDungeonsUi();
            Debug.Log("Dungeons UI validated: map viewport, zoom controls, dungeon markers, and Formation entry flows are present.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateDungeonsUi()
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
        controller.ShowDungeons();
        Canvas.ForceUpdateCanvases();

        var dungeonsPanel = RequireObject("Dungeons Panel", true);
        var header = RequireText(dungeonsPanel, "Dungeons Header");
        RequireCopy(header.text, "Dungeons", "Dungeons header");
        AssertTextFits(header, "Dungeons header");

        var mapViewport = RequireObject("Dungeon World Map Viewport", true);
        var mapContent = RequireObject("Dungeon World Map Content", true);
        ValidateDungeonMapViewport(mapViewport, mapContent);
        ValidateDungeonMapArt(mapContent);
        ValidateDungeonZoom(controller, dungeonsPanel);

        ValidateDungeonMarker(mapContent, "Gold Dungeon Map Marker", "Gold", "Gold", "dungeon_portal");
        ValidateDungeonMarker(mapContent, "Essence Dungeon Map Marker", "Essence", "Essence", "dungeon_essence");
        ValidateDungeonMarker(mapContent, "Gear Dungeon Map Marker", "Iron", "Drop", "dungeon_fire");

        var resultText = RequireText(dungeonsPanel, "Dungeon Result Text");
        AssertTextFits(resultText, "Dungeon Result Text");

        ValidateDungeonFormationEntry(controller, "Gold Dungeon Map Marker", "gold_dungeon", "Gold");
        ValidateDungeonFormationEntry(controller, "Essence Dungeon Map Marker", "essence_dungeon", "Essence");
        ValidateDungeonFormationEntry(controller, "Gear Dungeon Map Marker", "gear_dungeon", "Gear");

        controller.ShowDungeons();
        Canvas.ForceUpdateCanvases();
        if (!dungeonsPanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Dungeons panel should remain reachable after validating Formation entry flows.");
        }
    }

    private static void ValidateDungeonMapViewport(GameObject mapViewport, GameObject mapContent)
    {
        AssertInsideParent(RequireObject("Dungeons Panel", true), mapViewport);

        var viewportRect = mapViewport.GetComponent<RectTransform>();
        var contentRect = mapContent.GetComponent<RectTransform>();
        if (viewportRect == null || contentRect == null)
        {
            throw new InvalidOperationException("Dungeon map viewport/content should both have RectTransform components.");
        }

        if (contentRect.rect.width <= viewportRect.rect.width || contentRect.rect.height <= viewportRect.rect.height)
        {
            throw new InvalidOperationException("Dungeon world map content should be larger than the viewport so it can pan.");
        }

        if (mapViewport.GetComponent<RectMask2D>() == null)
        {
            throw new InvalidOperationException("Dungeon World Map Viewport should clip with RectMask2D.");
        }

        var trigger = mapViewport.GetComponent<EventTrigger>();
        if (trigger == null || trigger.triggers == null || trigger.triggers.Count < 3)
        {
            throw new InvalidOperationException("Dungeon World Map Viewport should register pointer, drag, and scroll handlers.");
        }
    }

    private static void ValidateDungeonMapArt(GameObject mapContent)
    {
        var mapImage = RequireRawImageWithTexture("Mythwake World Map Image");
        AssertInsideParent(mapContent, mapImage.gameObject);
        if (!mapImage.texture.name.Contains("mythwake_map"))
        {
            throw new InvalidOperationException($"Dungeon world map should use mythwake_map, got '{mapImage.texture.name}'.");
        }
    }

    private static void ValidateDungeonZoom(IdlePrototypeController controller, GameObject dungeonsPanel)
    {
        var zoomIn = RequireButton("Dungeon Map Zoom In");
        var zoomOut = RequireButton("Dungeon Map Zoom Out");
        AssertInsideParent(dungeonsPanel, zoomIn.gameObject);
        AssertInsideParent(dungeonsPanel, zoomOut.gameObject);

        var beforeZoom = GetPrivateField<float>(controller, "dungeonMapZoom");
        zoomIn.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        var zoomedIn = GetPrivateField<float>(controller, "dungeonMapZoom");
        if (zoomedIn <= beforeZoom)
        {
            throw new InvalidOperationException($"Dungeon zoom-in button should increase zoom. Before={beforeZoom}, after={zoomedIn}.");
        }

        zoomOut.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        var zoomedOut = GetPrivateField<float>(controller, "dungeonMapZoom");
        if (zoomedOut >= zoomedIn)
        {
            throw new InvalidOperationException($"Dungeon zoom-out button should decrease zoom. Before={zoomedIn}, after={zoomedOut}.");
        }
    }

    private static void ValidateDungeonMarker(GameObject mapContent, string markerName, string expectedTitle, string expectedDetail, string expectedIconTexture)
    {
        var marker = RequireButton(markerName);
        AssertInsideParent(mapContent, marker.gameObject);

        var icon = FindChild(marker.transform, "Dungeon Map Icon")?.GetComponent<RawImage>();
        if (icon == null || icon.texture == null || !icon.texture.name.Contains(expectedIconTexture))
        {
            throw new InvalidOperationException($"{markerName} should show loaded {expectedIconTexture} marker art.");
        }

        var title = RequireChildText(marker.transform, "Dungeon Set Title", markerName);
        var progress = RequireChildText(marker.transform, "Dungeon Set Progress", markerName);
        var detail = RequireChildText(marker.transform, "Dungeon Set Detail", markerName);
        RequireCopy(title.text, expectedTitle, $"{markerName} title");
        RequireCopy(progress.text, "Floor", $"{markerName} progress");
        RequireCopy(detail.text, expectedDetail, $"{markerName} detail");
        AssertTextFits(title, $"{markerName} title");
        AssertTextFits(progress, $"{markerName} progress");
        AssertTextFits(detail, $"{markerName} detail");
    }

    private static void ValidateDungeonFormationEntry(IdlePrototypeController controller, string markerName, string expectedDungeonId, string expectedHeader)
    {
        controller.ShowDungeons();
        Canvas.ForceUpdateCanvases();

        RequireButton(markerName).onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        var selectedDungeonId = GetPrivateField<string>(controller, "selectedDungeonId");
        if (selectedDungeonId != expectedDungeonId)
        {
            throw new InvalidOperationException($"{markerName} should select {expectedDungeonId}, got '{selectedDungeonId}'.");
        }

        var formationRoot = RequireObject("Campaign Formation Root", true);
        var formationHeader = RequireText(formationRoot, "Formation Header");
        var formationEnemy = RequireText(formationRoot, "Formation Enemy Text");
        var confirmButton = RequireButton("Formation Confirm Button");

        RequireCopy(formationHeader.text, expectedHeader, $"{markerName} Formation header");
        RequireCopy(formationEnemy.text, "Boss", $"{markerName} Formation enemy");
        RequireCopy(formationEnemy.text, "Damage", $"{markerName} Formation enemy");
        AssertTextFits(formationHeader, $"{markerName} Formation header");
        AssertTextFits(formationEnemy, $"{markerName} Formation enemy");

        if (!confirmButton.interactable)
        {
            throw new InvalidOperationException($"{markerName} Formation confirm button should be interactable.");
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

    private static Button RequireButton(string name)
    {
        var gameObject = RequireObject(name, true);
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

    private static TMP_Text RequireText(GameObject parent, string name)
    {
        var transform = parent.transform.Find(name);
        if (transform == null)
        {
            throw new InvalidOperationException($"{parent.name} is missing text child {name}.");
        }

        var text = transform.GetComponent<TMP_Text>();
        if (text == null)
        {
            throw new InvalidOperationException($"{name} is missing TMP_Text.");
        }

        return text;
    }

    private static TMP_Text RequireChildText(Transform parent, string childName, string context)
    {
        var child = FindChild(parent, childName);
        if (child == null)
        {
            throw new InvalidOperationException($"{context} is missing child {childName}.");
        }

        var text = child.GetComponent<TMP_Text>();
        if (text == null)
        {
            throw new InvalidOperationException($"{context} child {childName} is missing TMP_Text.");
        }

        return text;
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

    private static void RequireCopy(string text, string expected, string context)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.Contains(expected))
        {
            throw new InvalidOperationException($"{context} is missing '{expected}': '{text}'");
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
