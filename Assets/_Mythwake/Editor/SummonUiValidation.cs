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
            Debug.Log("Summon UI validated: Vanguard banner, Paladin feature art, rates, carousel, and result popup are present.");
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

        RequireCopy(resultTitle.text, "Summon x10 Result");
        RequireResultSlot(resultNames, resultCounts, resultImages, 0, "Paladin", "x3", PaladinHeroId);

        if (GetButtonLabel(resultTenButton) != "x10" || GetButtonLabel(resultMaxButton) != "x300")
        {
            throw new InvalidOperationException("Summon result repeat buttons should keep x10 and x300 labels.");
        }

        if (string.IsNullOrWhiteSpace(resultTenCost.text) || string.IsNullOrWhiteSpace(resultMaxCost.text))
        {
            throw new InvalidOperationException("Summon result repeat buttons should show gem costs.");
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
