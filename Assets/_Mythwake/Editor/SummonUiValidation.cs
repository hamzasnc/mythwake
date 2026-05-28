using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SummonUiValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PaladinHeroId = "hero_paladin";
    private const string VanguardBannerId = "hero_shard_vanguard";

    [MenuItem("Mythwake/Validate Summon UI")]
    public static void RunSummonUiValidation()
    {
        try
        {
            ValidateSummonUi();
            Debug.Log("Summon UI validated: Vanguard banner, Paladin feature art, rates, carousel, result popup, auto toggle, close flow, and repeat summon button states are present.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateSummonUi()
    {
        EditorSceneManager.OpenScene(ScenePath);

        var controller = FindSceneComponent<IdlePrototypeController>();
        if (controller == null)
        {
            throw new InvalidOperationException("Missing IdlePrototypeController in SampleScene.");
        }

        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "RegisterNavigation");
        controller.ShowSummon();
        Canvas.ForceUpdateCanvases();

        var summonPanel = RequireObject("Summon Panel", true);
        var summonOffer = RequireObject("Summon Offer Banner", true);
        var summonCarousel = RequireObject("Summon Banner Carousel", true);
        var resultPopup = RequireObject("Summon Result Popup", false);

        AssertInsideParent(summonPanel, summonOffer);
        AssertInsideParent(summonPanel, summonCarousel);
        AssertInsideParent(resultPopup, RequireObject("Result Summon Ten", false));
        AssertInsideParent(resultPopup, RequireObject("Result Summon Max", false));
        AssertInsideParent(resultPopup, RequireObject("Result Close", false));
        AssertInsideParent(resultPopup, RequireObject("Auto Summon Toggle", false));

        var paladinHeroIndex = FindHeroIndex(PaladinHeroId);
        var vanguardBannerIndex = FindBannerIndex(VanguardBannerId);

        SetPrivateField(controller, "backendGameplayEnabled", false);
        SetPrivateField(controller, "gems", 20000);
        SetPrivateField(controller, "selectedSummonBannerIndex", vanguardBannerIndex);
        InvokePrivate(controller, "RefreshSummonUi");
        Canvas.ForceUpdateCanvases();

        var offerTitle = GetPrivateField<TMP_Text>(controller, "summonOfferTitleText");
        var offerPromo = GetPrivateField<TMP_Text>(controller, "summonOfferPromoText");
        var ratesText = GetPrivateField<TMP_Text>(controller, "summonRatesText");
        var offerImages = GetPrivateField<RawImage[]>(controller, "summonOfferHeroImages");
        var carouselTitles = GetPrivateField<TMP_Text[]>(controller, "summonCarouselTitleTexts");

        RequireCopy(offerTitle.text, "Vanguard Oath");
        RequireCopy(offerPromo.text, "frontline");
        RequireCopy(ratesText.text, "Epic 52%");
        RequireCopy(ratesText.text, "Legendary 6%");

        if (offerImages == null || offerImages.Length == 0 || offerImages[0] == null || offerImages[0].texture == null || !offerImages[0].texture.name.Contains(PaladinHeroId))
        {
            throw new InvalidOperationException("Vanguard offer hero slot 1 should show Paladin runtime art.");
        }

        if (carouselTitles == null || carouselTitles.Length < 2 || carouselTitles[1] == null || carouselTitles[1].text != "Vanguard Oath")
        {
            throw new InvalidOperationException("Selected Summon carousel center card should show Vanguard Oath.");
        }

        var drawCounts = new int[GetStaticArray(typeof(IdlePrototypeController), "HeroDefinitions").Length];
        drawCounts[paladinHeroIndex] = 3;
        InvokePrivate(controller, "ShowSummonResultPopup", drawCounts, 10);
        Canvas.ForceUpdateCanvases();

        var resultRoot = GetPrivateField<RectTransform>(controller, "summonResultPopupRoot");
        if (resultRoot == null || !resultRoot.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Summon result popup should be active after showing a Paladin result.");
        }

        var resultTitle = GetPrivateField<TMP_Text>(controller, "summonResultPopupTitleText");
        var resultNames = GetPrivateField<TMP_Text[]>(controller, "summonResultHeroNameTexts");
        var resultCounts = GetPrivateField<TMP_Text[]>(controller, "summonResultHeroCountTexts");
        var resultImages = GetPrivateField<RawImage[]>(controller, "summonResultHeroImages");
        var resultTenButton = GetPrivateField<Button>(controller, "summonResultTenButton");
        var resultMaxButton = GetPrivateField<Button>(controller, "summonResultMaxButton");
        var resultTenCost = GetPrivateField<TMP_Text>(controller, "summonResultTenCostText");
        var resultMaxCost = GetPrivateField<TMP_Text>(controller, "summonResultMaxCostText");
        var autoToggleText = GetPrivateField<TMP_Text>(controller, "summonAutoToggleText");
        var autoToggleButton = GetPrivateField<Button>(controller, "summonAutoToggleButton");
        var autoToggleMark = GetPrivateField<TMP_Text>(controller, "summonAutoCheckboxMarkText");
        var resultCloseButton = GetPrivateField<Button>(controller, "summonResultCloseButton");

        RequireCopy(resultTitle.text, "Summon x10 Result");
        RequireResultSlot(resultNames, resultCounts, resultImages, 0, "Paladin", "x3", PaladinHeroId);
        ValidateSummonResultSlots(resultNames, resultCounts, resultImages, 1);
        RequireCopy(autoToggleText.text, "Auto-Summon");
        AssertTextFits(resultTitle, "Summon result title");
        AssertTextFits(autoToggleText, "Summon auto toggle label");
        ValidateSummonAutoToggle(autoToggleButton, autoToggleMark, autoToggleText);
        ValidateSummonResultMobileLayout(resultRoot.gameObject, resultTenButton, resultMaxButton, resultCloseButton, autoToggleButton, resultNames);

        if (GetButtonLabel(resultTenButton) != "x10" || GetButtonLabel(resultMaxButton) != "x300")
        {
            throw new InvalidOperationException("Summon result repeat buttons should keep x10 and x300 labels.");
        }

        AssertResultRepeatButtons(resultTenButton, resultMaxButton, resultTenCost, resultMaxCost, "315", "9450", true, true, "Summon result high-gem state");
        SetPrivateField(controller, "gems", 314);
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();
        AssertResultRepeatButtons(resultTenButton, resultMaxButton, resultTenCost, resultMaxCost, "315", "9450", false, false, "Summon result insufficient-gem state");

        SetPrivateField(controller, "gems", 315);
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();
        AssertResultRepeatButtons(resultTenButton, resultMaxButton, resultTenCost, resultMaxCost, "315", "9450", true, false, "Summon result x10-only gem state");

        resultCloseButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (resultRoot.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Summon result popup should close from its close button.");
        }
    }

    private static int FindHeroIndex(string heroId)
    {
        var heroDefinitions = GetStaticArray(typeof(IdlePrototypeController), "HeroDefinitions");
        for (var i = 0; i < heroDefinitions.Length; i++)
        {
            var hero = heroDefinitions.GetValue(i);
            if (GetInstanceField<string>(hero, "heroId") == heroId)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Missing hero definition: {heroId}");
    }

    private static int FindBannerIndex(string bannerId)
    {
        var banners = GetStaticArray(typeof(IdlePrototypeController), "LocalSummonBanners");
        for (var i = 0; i < banners.Length; i++)
        {
            var banner = banners.GetValue(i);
            if (GetInstanceField<string>(banner, "bannerId") == bannerId)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Missing summon banner: {bannerId}");
    }

    private static void RequireResultSlot(TMP_Text[] names, TMP_Text[] counts, RawImage[] images, int slotIndex, string expectedName, string expectedCount, string expectedTextureName)
    {
        if (names == null || slotIndex >= names.Length || names[slotIndex] == null || names[slotIndex].text != expectedName)
        {
            throw new InvalidOperationException($"Summon result slot {slotIndex} should show {expectedName}.");
        }

        if (counts == null || slotIndex >= counts.Length || counts[slotIndex] == null || counts[slotIndex].text != expectedCount)
        {
            throw new InvalidOperationException($"Summon result slot {slotIndex} should show draw count {expectedCount}.");
        }

        if (images == null || slotIndex >= images.Length || images[slotIndex] == null || images[slotIndex].texture == null || !images[slotIndex].texture.name.Contains(expectedTextureName))
        {
            throw new InvalidOperationException($"Summon result slot {slotIndex} should show {expectedTextureName} art.");
        }
    }

    private static void ValidateSummonResultSlots(TMP_Text[] names, TMP_Text[] counts, RawImage[] images, int visibleSlotCount)
    {
        for (var i = 0; i < visibleSlotCount; i++)
        {
            if (names == null || i >= names.Length || names[i] == null)
            {
                throw new InvalidOperationException($"Summon result slot {i} is missing its name label.");
            }

            if (counts == null || i >= counts.Length || counts[i] == null)
            {
                throw new InvalidOperationException($"Summon result slot {i} is missing its count label.");
            }

            if (images == null || i >= images.Length || images[i] == null || images[i].texture == null)
            {
                throw new InvalidOperationException($"Summon result slot {i} is missing loaded hero art.");
            }

            AssertTextFits(names[i], $"Summon result slot {i} name");
            AssertTextFits(counts[i], $"Summon result slot {i} count");
        }

        for (var i = visibleSlotCount; names != null && i < names.Length; i++)
        {
            var card = names[i] == null ? null : names[i].transform.parent;
            if (card != null && card.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException($"Summon result slot {i} should be hidden when it has no draw count.");
            }
        }
    }

    private static void ValidateSummonAutoToggle(Button autoToggleButton, TMP_Text autoToggleMark, TMP_Text autoToggleText)
    {
        if (autoToggleButton == null || autoToggleMark == null || autoToggleText == null)
        {
            throw new InvalidOperationException("Summon auto toggle controls should exist in the result popup.");
        }

        autoToggleButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (autoToggleMark.text != "X")
        {
            throw new InvalidOperationException($"Summon auto toggle should show an X mark after enabling, got '{autoToggleMark.text}'.");
        }

        RequireCopy(autoToggleText.text, "Auto-Summon");
        AssertTextFits(autoToggleText, "Summon auto toggle enabled label");

        autoToggleButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (!string.IsNullOrEmpty(autoToggleMark.text))
        {
            throw new InvalidOperationException($"Summon auto toggle should clear its mark after disabling, got '{autoToggleMark.text}'.");
        }

        RequireCopy(autoToggleText.text, "Auto-Summon");
        AssertTextFits(autoToggleText, "Summon auto toggle disabled label");
    }

    private static void AssertResultRepeatButtons(Button tenButton, Button maxButton, TMP_Text tenCost, TMP_Text maxCost, string expectedTenCost, string expectedMaxCost, bool expectedTenInteractable, bool expectedMaxInteractable, string context)
    {
        if (tenCost == null || tenCost.text != expectedTenCost)
        {
            throw new InvalidOperationException($"{context}: x10 repeat cost should be {expectedTenCost}, got '{(tenCost == null ? "<missing>" : tenCost.text)}'.");
        }

        if (maxCost == null || maxCost.text != expectedMaxCost)
        {
            throw new InvalidOperationException($"{context}: x300 repeat cost should be {expectedMaxCost}, got '{(maxCost == null ? "<missing>" : maxCost.text)}'.");
        }

        AssertButtonState(tenButton, "x10", expectedTenInteractable, context);
        AssertButtonState(maxButton, "x300", expectedMaxInteractable, context);

        AssertTextFits(tenCost, $"{context} x10 cost");
        AssertTextFits(maxCost, $"{context} x300 cost");
    }

    private static void AssertButtonState(Button button, string expectedLabel, bool expectedInteractable, string context)
    {
        if (button == null)
        {
            throw new InvalidOperationException($"{context}: {expectedLabel} button is missing.");
        }

        var label = GetButtonLabel(button);
        if (label != expectedLabel)
        {
            throw new InvalidOperationException($"{context}: expected {expectedLabel} button label, got '{label}'.");
        }

        if (button.interactable != expectedInteractable)
        {
            throw new InvalidOperationException($"{context}: {expectedLabel} button interactable mismatch. Expected {expectedInteractable}, got {button.interactable}.");
        }
    }

    private static void ValidateSummonResultMobileLayout(GameObject resultPopup, Button tenButton, Button maxButton, Button closeButton, Button autoToggleButton, TMP_Text[] resultNames)
    {
        AssertMinimumSize(tenButton.gameObject, 240f, 70f, "Summon result x10 repeat button");
        AssertMinimumSize(maxButton.gameObject, 240f, 70f, "Summon result x300 repeat button");
        AssertMinimumSize(closeButton.gameObject, 70f, 70f, "Summon result close button");
        AssertMinimumSize(autoToggleButton.gameObject, 320f, 50f, "Summon result auto toggle");
        AssertInsideParent(resultPopup, tenButton.gameObject);
        AssertInsideParent(resultPopup, maxButton.gameObject);
        AssertInsideParent(resultPopup, closeButton.gameObject);
        AssertInsideParent(resultPopup, autoToggleButton.gameObject);
        AssertNoOverlap(tenButton.gameObject, closeButton.gameObject, 10f, "Summon result repeat/close controls");
        AssertNoOverlap(maxButton.gameObject, closeButton.gameObject, 10f, "Summon result repeat/close controls");
        AssertNoOverlap(autoToggleButton.gameObject, tenButton.gameObject, 10f, "Summon result auto/repeat controls");
        AssertNoOverlap(autoToggleButton.gameObject, maxButton.gameObject, 10f, "Summon result auto/repeat controls");

        for (var i = 0; resultNames != null && i < resultNames.Length; i++)
        {
            if (resultNames[i] == null)
            {
                continue;
            }

            var card = resultNames[i].transform.parent == null ? null : resultNames[i].transform.parent.gameObject;
            if (card == null || !card.activeInHierarchy)
            {
                continue;
            }

            AssertInsideParent(resultPopup, card);
            AssertNoOverlap(card, autoToggleButton.gameObject, 12f, "Summon result card/control spacing");
        }
    }

    private static void AssertMinimumSize(GameObject gameObject, float width, float height, string context)
    {
        var rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            throw new InvalidOperationException($"{context} is missing a RectTransform.");
        }

        if (rect.rect.width < width || rect.rect.height < height)
        {
            throw new InvalidOperationException($"{context} should be at least {width}x{height}, got {rect.rect.width}x{rect.rect.height}.");
        }
    }

    private static void AssertNoOverlap(GameObject first, GameObject second, float padding, string context)
    {
        var firstBounds = GetLocalBounds(first);
        var secondBounds = GetLocalBounds(second);
        if (firstBounds.Left < secondBounds.Right + padding
            && firstBounds.Right > secondBounds.Left - padding
            && firstBounds.Top > secondBounds.Bottom - padding
            && firstBounds.Bottom < secondBounds.Top + padding)
        {
            throw new InvalidOperationException($"{context}: {first.name} overlaps {second.name}.");
        }
    }

    private static LocalBounds GetLocalBounds(GameObject gameObject)
    {
        var rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            throw new InvalidOperationException($"{gameObject.name} is missing a RectTransform.");
        }

        var left = rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
        var right = left + rect.rect.width;
        var top = rect.anchoredPosition.y + rect.rect.height * (1f - rect.pivot.y);
        var bottom = top - rect.rect.height;
        return new LocalBounds(left, right, top, bottom);
    }

    private readonly struct LocalBounds
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

    private static void RequireCopy(string text, string expected)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.Contains(expected))
        {
            throw new InvalidOperationException($"Summon copy is missing '{expected}': '{text}'");
        }
    }

    private static string GetButtonLabel(Button button)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        return label == null ? string.Empty : label.text;
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

    private static T GetInstanceField<T>(object instance, string fieldName)
    {
        var type = instance.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            throw new InvalidOperationException($"{type.Name}.{fieldName} could not be found.");
        }

        return (T)field.GetValue(instance);
    }
}
