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
    private const float DungeonMapMinZoom = 0.8f;
    private const float DungeonMapMaxZoom = 1.8f;
    private const float FloatTolerance = 0.001f;

    [MenuItem("Mythwake/Validate Dungeons UI")]
    public static void RunDungeonsUiValidation()
    {
        try
        {
            ValidateDungeonsUi();
            Debug.Log("Dungeons UI validated: selector cards, detail panel, floor list, locked future dungeons, and Formation back flows are present.");
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

        var selectorPanel = RequireObject("Dungeon Selector Panel", true);
        var selectorCards = RequireObject("Dungeon Selector Cards", true);
        ValidateDungeonSelectorCard(selectorCards, "Gold Dungeon Selector Card", "Gold Vault", "Floor", "Reward", "gold_dungeon_set_banner", "dungeon_portal");
        ValidateDungeonSelectorCard(selectorCards, "Essence Dungeon Selector Card", "Essence Grove", "Floor", "Reward", "essence_dungeon_set_banner", "dungeon_essence");
        ValidateDungeonSelectorCard(selectorCards, "Gear Dungeon Selector Card", "Gear Forge", "Floor", "Reward", "gear_dungeon_set_banner", "dungeon_fire");
        ValidateLockedDungeonCard(selectorCards, "Shard Rift Dungeon Selector Card", "Shard Rift");
        ValidateLockedDungeonCard(selectorCards, "Ancient Tower Dungeon Selector Card", "Ancient Tower");
        ValidateDungeonSelectorSpacing();
        ValidateDungeonDetail(selectorPanel, "Gold Vault", "gold_dungeon_set_banner");
        ValidateFloorList(selectorPanel);

        var oldWorldMap = FindSceneObject("Mythwake World Map Image");
        if (oldWorldMap != null && oldWorldMap.activeInHierarchy)
        {
            throw new InvalidOperationException("Dungeon screen should not use the generic Mythwake world map as its main dungeon view.");
        }

        var resultText = RequireText(dungeonsPanel, "Dungeon Result Text");
        AssertTextFits(resultText, "Dungeon Result Text");
        var flowHintRoot = RequireObject("Dungeon Flow Hint", true);
        var flowHint = RequireText(flowHintRoot, "Dungeon Flow Hint Text");
        RequireCopy(flowHint.text, "Formation", "Dungeon flow hint");
        AssertTextFits(flowHint, "Dungeon flow hint");
        ValidateDungeonsLanguageRefresh(controller);

        ValidateDungeonFormationEntry(controller, "Gold Dungeon Selector Card", "gold_dungeon", "Gold Vault", "Gold");
        ValidateDungeonFormationEntry(controller, "Essence Dungeon Selector Card", "essence_dungeon", "Essence Grove", "Essence");
        ValidateDungeonFormationEntry(controller, "Gear Dungeon Selector Card", "gear_dungeon", "Gear Forge", "Gear");

        controller.ShowDungeons();
        Canvas.ForceUpdateCanvases();
        if (!dungeonsPanel.activeInHierarchy)
        {
            throw new InvalidOperationException("Dungeons panel should remain reachable after validating Formation entry flows.");
        }
    }

    private static void ValidateDungeonsLanguageRefresh(IdlePrototypeController controller)
    {
        SetPrivateField(controller, "language", MythwakeLanguage.German);
        InvokePrivate(controller, "RefreshUi");
        controller.ShowDungeons();
        Canvas.ForceUpdateCanvases();

        var dungeonsPanel = RequireObject("Dungeons Panel", true);
        var resultText = RequireText(dungeonsPanel, "Dungeon Result Text");
        var flowHintRoot = RequireObject("Dungeon Flow Hint", true);
        var flowHint = RequireText(flowHintRoot, "Dungeon Flow Hint Text");
        RequireCopy(resultText.text, "Dungeons sind", "German Dungeon Result Text");
        RequireCopy(flowHint.text, "Formation", "German Dungeon flow hint");
        if (flowHint.text.Contains("Select"))
        {
            throw new InvalidOperationException("German Dungeon flow hint should not keep the old English Select copy.");
        }

        AssertTextFits(resultText, "German Dungeon Result Text");
        AssertTextFits(flowHint, "German Dungeon flow hint");
        SetPrivateField(controller, "language", MythwakeLanguage.English);
        InvokePrivate(controller, "RefreshUi");
        controller.ShowDungeons();
        Canvas.ForceUpdateCanvases();
    }

    private static void ValidateDungeonSelectorCard(GameObject selectorCards, string cardName, string expectedTitle, string expectedProgress, string expectedDetail, string expectedBannerTexture, string expectedIconTexture)
    {
        var card = RequireButton(cardName);
        AssertInsideParent(selectorCards, card.gameObject);
        if (!card.interactable)
        {
            throw new InvalidOperationException($"{cardName} should be selectable.");
        }

        var banner = RequireChildRawImageWithTexture(card.transform, "Selector Banner", cardName);
        if (!banner.texture.name.Contains(expectedBannerTexture))
        {
            throw new InvalidOperationException($"{cardName} should use {expectedBannerTexture} banner art, got '{banner.texture.name}'.");
        }

        var icon = RequireChildRawImageWithTexture(card.transform, "Dungeon Selector Icon", cardName);
        if (!icon.texture.name.Contains(expectedIconTexture))
        {
            throw new InvalidOperationException($"{cardName} should use {expectedIconTexture} icon art, got '{icon.texture.name}'.");
        }

        var title = RequireChildText(card.transform, "Dungeon Set Title", cardName);
        var progress = RequireChildText(card.transform, "Dungeon Set Progress", cardName);
        var detail = RequireChildText(card.transform, "Dungeon Set Detail", cardName);
        RequireCopy(title.text, expectedTitle, $"{cardName} title");
        RequireCopy(progress.text, expectedProgress, $"{cardName} progress");
        RequireCopy(detail.text, expectedDetail, $"{cardName} detail");
        AssertTextFits(title, $"{cardName} title");
        AssertTextFits(progress, $"{cardName} progress");
        AssertTextFits(detail, $"{cardName} detail");
    }

    private static void ValidateLockedDungeonCard(GameObject selectorCards, string cardName, string expectedTitle)
    {
        var card = RequireButton(cardName);
        AssertInsideParent(selectorCards, card.gameObject);
        if (card.interactable)
        {
            throw new InvalidOperationException($"{cardName} should be visible but locked/non-interactable.");
        }

        var title = RequireChildText(card.transform, "Dungeon Set Title", cardName);
        var progress = RequireChildText(card.transform, "Dungeon Set Progress", cardName);
        var detail = RequireChildText(card.transform, "Dungeon Set Detail", cardName);
        RequireCopy(title.text, expectedTitle, $"{cardName} title");
        RequireCopy(progress.text, "Locked", $"{cardName} locked state");
        RequireCopy(detail.text, "Future", $"{cardName} future state");
        AssertTextFits(title, $"{cardName} title");
        AssertTextFits(progress, $"{cardName} locked state");
        AssertTextFits(detail, $"{cardName} future state");
    }

    private static void ValidateDungeonSelectorSpacing()
    {
        var cards = new[]
        {
            RequireButton("Gold Dungeon Selector Card").gameObject,
            RequireButton("Essence Dungeon Selector Card").gameObject,
            RequireButton("Gear Dungeon Selector Card").gameObject,
            RequireButton("Shard Rift Dungeon Selector Card").gameObject,
            RequireButton("Ancient Tower Dungeon Selector Card").gameObject,
        };

        for (var i = 0; i < cards.Length; i++)
        {
            for (var j = i + 1; j < cards.Length; j++)
            {
                AssertNoRectOverlap(cards[i], cards[j], $"{cards[i].name} and {cards[j].name}");
            }
        }
    }

    private static void ValidateDungeonDetail(GameObject selectorPanel, string expectedTitle, string expectedBannerTexture)
    {
        var detail = RequireObject("Dungeon Detail Panel", true);
        AssertInsideParent(selectorPanel, detail);

        var banner = RequireRawImageWithTexture("Dungeon Detail Banner");
        AssertInsideParent(detail, banner.gameObject);
        if (!banner.texture.name.Contains(expectedBannerTexture))
        {
            throw new InvalidOperationException($"Dungeon detail should use {expectedBannerTexture}, got '{banner.texture.name}'.");
        }

        var boss = RequireRawImageWithTexture("Dungeon Detail Boss Preview");
        AssertInsideParent(detail, boss.gameObject);

        var title = RequireText(detail, "Dungeon Detail Title");
        var meta = RequireText(detail, "Dungeon Detail Meta");
        var rewards = RequireText(detail, "Dungeon Detail Rewards");
        RequireCopy(title.text, expectedTitle, "Dungeon Detail title");
        RequireCopy(meta.text, "Rec", "Dungeon Detail meta");
        RequireCopy(rewards.text, "Reward", "Dungeon Detail rewards");
        AssertTextFits(title, "Dungeon Detail title");
        AssertTextFits(meta, "Dungeon Detail meta");
        AssertTextFits(rewards, "Dungeon Detail rewards");

        var runButton = RequireButton("Dungeon Run Button");
        AssertInsideParent(detail, runButton.gameObject);
        if (!runButton.interactable)
        {
            throw new InvalidOperationException("Dungeon Run Button should be interactable for playable dungeons.");
        }
    }

    private static void ValidateFloorList(GameObject selectorPanel)
    {
        var floorList = RequireObject("Dungeon Floor List", true);
        AssertInsideParent(selectorPanel, floorList);

        for (var i = 1; i <= 4; i++)
        {
            var entry = RequireButton($"Dungeon Floor Entry {i}");
            AssertInsideParent(floorList, entry.gameObject);
            var title = RequireChildText(entry.transform, "Floor Title", entry.name);
            var status = RequireChildText(entry.transform, "Floor Status", entry.name);
            var action = RequireChildText(entry.transform, "Floor Action", entry.name);
            RequireCopy(title.text, "Floor", $"{entry.name} title");
            if (i == 1)
            {
                RequireCopy(status.text, "Ready", $"{entry.name} status");
                RequireCopy(action.text, "Enter", $"{entry.name} action");
                if (!entry.interactable)
                {
                    throw new InvalidOperationException($"{entry.name} should enter the ready dungeon floor.");
                }
            }
            else
            {
                RequireCopy(status.text, "Locked", $"{entry.name} status");
                if (entry.interactable)
                {
                    throw new InvalidOperationException($"{entry.name} should be locked until earlier floors are cleared.");
                }
            }

            AssertTextFits(title, $"{entry.name} title");
            AssertTextFits(status, $"{entry.name} status");
            AssertTextFits(action, $"{entry.name} action");
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

        for (var i = 0; i < 20; i++)
        {
            zoomIn.onClick.Invoke();
        }

        Canvas.ForceUpdateCanvases();
        var maxZoom = GetPrivateField<float>(controller, "dungeonMapZoom");
        AssertApproximately(maxZoom, DungeonMapMaxZoom, "Dungeon zoom max clamp");

        for (var i = 0; i < 30; i++)
        {
            zoomOut.onClick.Invoke();
        }

        Canvas.ForceUpdateCanvases();
        var minZoom = GetPrivateField<float>(controller, "dungeonMapZoom");
        AssertApproximately(minZoom, DungeonMapMinZoom, "Dungeon zoom min clamp");

        InvokePrivate(controller, "SetDungeonMapZoom", 1f);
        Canvas.ForceUpdateCanvases();
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

    private static void ValidateDungeonMarkerSpacing()
    {
        var goldMarker = RequireButton("Gold Dungeon Map Marker").gameObject;
        var essenceMarker = RequireButton("Essence Dungeon Map Marker").gameObject;
        var gearMarker = RequireButton("Gear Dungeon Map Marker").gameObject;

        AssertNoRectOverlap(goldMarker, essenceMarker, "Gold and Essence dungeon markers");
        AssertNoRectOverlap(goldMarker, gearMarker, "Gold and Gear dungeon markers");
        AssertNoRectOverlap(essenceMarker, gearMarker, "Essence and Gear dungeon markers");
    }

    private static void ValidateDungeonFormationEntry(IdlePrototypeController controller, string selectorName, string expectedDungeonId, string expectedDetailTitle, string expectedHeader)
    {
        controller.ShowDungeons();
        Canvas.ForceUpdateCanvases();

        RequireButton(selectorName).onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        var selectedDungeonId = GetPrivateField<string>(controller, "selectedDungeonId");
        if (selectedDungeonId != expectedDungeonId)
        {
            throw new InvalidOperationException($"{selectorName} should select {expectedDungeonId}, got '{selectedDungeonId}'.");
        }

        var detail = RequireObject("Dungeon Detail Panel", true);
        var detailTitle = RequireText(detail, "Dungeon Detail Title");
        RequireCopy(detailTitle.text, expectedDetailTitle, $"{selectorName} detail title");
        AssertTextFits(detailTitle, $"{selectorName} detail title");

        RequireButton("Dungeon Run Button").onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        var formationRoot = RequireObject("Campaign Formation Root", true);
        var formationHeader = RequireText(formationRoot, "Formation Header");
        var formationStage = RequireText(formationRoot, "Formation Stage Text");
        var formationEnemy = RequireText(formationRoot, "Formation Enemy Text");
        var confirmButton = RequireButton("Formation Confirm Button");

        RequireCopy(formationHeader.text, "VS", $"{selectorName} Formation header");
        RequireCopy(formationStage.text, expectedHeader, $"{selectorName} Formation stage");
        RequireCopy(formationEnemy.text, "Enemy", $"{selectorName} Formation enemy");
        AssertTextFits(formationHeader, $"{selectorName} Formation header");
        AssertTextFits(formationStage, $"{selectorName} Formation stage");
        AssertTextFits(formationEnemy, $"{selectorName} Formation enemy");

        if (!confirmButton.interactable)
        {
            throw new InvalidOperationException($"{selectorName} Formation confirm button should be interactable.");
        }

        var backButton = RequireButton("Formation Back Button");
        if (!backButton.interactable)
        {
            throw new InvalidOperationException($"{selectorName} Formation back button should be interactable.");
        }

        backButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        if (formationRoot.activeInHierarchy)
        {
            throw new InvalidOperationException($"{selectorName} Formation root should close after using Back.");
        }

        RequireObject("Dungeons Panel", true);
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

    private static RawImage RequireChildRawImageWithTexture(Transform parent, string childName, string context)
    {
        var child = FindChild(parent, childName);
        if (child == null)
        {
            throw new InvalidOperationException($"{context} is missing child {childName}.");
        }

        var image = child.GetComponent<RawImage>();
        if (image == null)
        {
            throw new InvalidOperationException($"{context} child {childName} is missing RawImage.");
        }

        if (image.texture == null)
        {
            throw new InvalidOperationException($"{context} child {childName} is missing a loaded texture.");
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

    private static void AssertNoRectOverlap(GameObject first, GameObject second, string context)
    {
        if (first.transform.parent != second.transform.parent)
        {
            throw new InvalidOperationException($"{context} should share the same map parent.");
        }

        var firstRect = GetAnchoredRect(first.GetComponent<RectTransform>());
        var secondRect = GetAnchoredRect(second.GetComponent<RectTransform>());
        if (firstRect.Overlaps(secondRect))
        {
            throw new InvalidOperationException($"{context} overlap: first={firstRect}, second={secondRect}.");
        }
    }

    private static Rect GetAnchoredRect(RectTransform rect)
    {
        if (rect == null)
        {
            throw new InvalidOperationException("Missing RectTransform while checking dungeon marker overlap.");
        }

        var width = rect.rect.width;
        var height = rect.rect.height;
        var left = rect.anchoredPosition.x - width * rect.pivot.x;
        var top = rect.anchoredPosition.y + height * (1f - rect.pivot.y);
        return new Rect(left, top - height, width, height);
    }

    private static void AssertApproximately(float actual, float expected, string context)
    {
        if (Mathf.Abs(actual - expected) <= FloatTolerance)
        {
            return;
        }

        throw new InvalidOperationException($"{context} expected {expected}, got {actual}.");
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
