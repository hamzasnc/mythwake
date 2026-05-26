using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CurrentSliceValidation
{
    [MenuItem("Mythwake/Validate Current Slice")]
    public static void RunCurrentSliceValidation()
    {
        try
        {
            RunPrivateValidator(typeof(VillageUiValidation), "ValidateVillageUi", "Village UI");
            RunPrivateValidator(typeof(DungeonsUiValidation), "ValidateDungeonsUi", "Dungeons UI");
            RunPrivateValidator(typeof(FastRewardsUiValidation), "ValidateFastRewardsUi", "Fast Rewards UI");
            RunPrivateValidator(typeof(MobileUxValidation), "ValidateMobileUx", "Mobile UX");
            RunPrivateValidator(typeof(SummonUiValidation), "ValidateSummonUi", "Summon UI");
            RunPrivateValidator(typeof(UpgradeClutterValidation), "ValidateUpgradeClutter", "Upgrade Clutter");
            RunPrivateValidator(typeof(HomeIdleCombatValidation), "ValidateHomeIdleCombatUi", "Home Idle Combat");
            RunPrivateValidator(typeof(FightFormationValidation), "ValidateFightFormationUi", "Fight Formation UI");
            RunValidator("Paladin Integration", PaladinSpineValidation.RunPaladinIntegrationValidation);
            RunValidator("Paladin Spine Handoff", PaladinSpineValidation.RunPaladinSpineValidation);

            Debug.Log("Current Mythwake slice validated: Village, Dungeons, Fast Rewards, Mobile UX, Summon, Upgrade Clutter, Home Idle Combat, Fight Formation UI, Paladin integration, and Paladin Spine handoff.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void RunPrivateValidator(Type type, string methodName, string label)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Missing validator method: {type.Name}.{methodName}");
        }

        RunValidator(label, () => method.Invoke(null, null));
    }

    private static void RunValidator(string label, Action action)
    {
        Debug.Log($"Running {label} validation...");
        try
        {
            action();
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw new InvalidOperationException($"{label} validation failed.", ex.InnerException);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{label} validation failed.", ex);
        }

        Debug.Log($"{label} validation passed.");
    }
}

public static class MobileUxValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;

    [MenuItem("Mythwake/Validate Mobile UX")]
    public static void RunMobileUxValidation()
    {
        try
        {
            ValidateMobileUx();
            Debug.Log("Mobile UX validated: Android portrait settings, safe-area rendering, portrait CanvasScaler, bottom navigation targets, version label, and core screen navigation are present.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateMobileUx()
    {
        ValidateAndroidPlayerSettings();

        EditorSceneManager.OpenScene(ScenePath);
        var controller = FindSceneComponent<IdlePrototypeController>();
        if (controller == null)
        {
            throw new InvalidOperationException("Missing IdlePrototypeController in SampleScene.");
        }

        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "RegisterNavigation");
        Canvas.ForceUpdateCanvases();

        var canvasRect = ValidatePortraitCanvas(controller);
        ValidateMobileChrome(controller, canvasRect);
        ValidateScreenNavigation(controller);
    }

    private static void ValidateAndroidPlayerSettings()
    {
        var projectSettings = File.ReadAllText(ProjectSettingsPath);
        RequireProjectSetting(projectSettings, "defaultScreenOrientation", "1", "Android should launch in portrait for the mobile-first prototype.");
        RequireProjectSetting(projectSettings, "allowedAutorotateToPortrait", "1", "Portrait autorotation should stay enabled.");
        RequireProjectSetting(projectSettings, "allowedAutorotateToPortraitUpsideDown", "0", "Upside-down portrait should stay disabled for tester builds.");
        RequireProjectSetting(projectSettings, "allowedAutorotateToLandscapeRight", "0", "Landscape-right autorotation should stay disabled.");
        RequireProjectSetting(projectSettings, "allowedAutorotateToLandscapeLeft", "0", "Landscape-left autorotation should stay disabled.");
        RequireProjectSetting(projectSettings, "useOSAutorotation", "0", "OS autorotation should not override the portrait test layout.");
        RequireProjectSetting(projectSettings, "androidRenderOutsideSafeArea", "0", "Android should not render outside the safe area until runtime safe-area padding exists.");
        RequireProjectSetting(projectSettings, "defaultScreenWidth", "1080", "Default Game View width should match the portrait reference canvas.");
        RequireProjectSetting(projectSettings, "defaultScreenHeight", "1920", "Default Game View height should match the portrait reference canvas.");
        RequireProjectSetting(projectSettings, "androidDefaultWindowWidth", "1080", "Android freeform width should match the portrait reference canvas.");
        RequireProjectSetting(projectSettings, "androidDefaultWindowHeight", "1920", "Android freeform height should match the portrait reference canvas.");
    }

    private static void RequireProjectSetting(string source, string key, string value, string message)
    {
        var expected = $"  {key}: {value}";
        if (!source.Contains(expected))
        {
            throw new InvalidOperationException($"{message} Expected '{expected.Trim()}'.");
        }
    }

    private static RectTransform ValidatePortraitCanvas(IdlePrototypeController controller)
    {
        var topBarRoot = RequireObjectField<RectTransform>(controller, "topBarRoot");
        var rootCanvas = topBarRoot.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            throw new InvalidOperationException("Top bar should live under the portrait Prototype UI canvas.");
        }

        var scaler = rootCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            throw new InvalidOperationException($"{rootCanvas.name} is missing CanvasScaler.");
        }

        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            throw new InvalidOperationException($"{rootCanvas.name} should use Scale With Screen Size.");
        }

        if (Mathf.Abs(scaler.referenceResolution.x - ReferenceWidth) > 0.1f || Mathf.Abs(scaler.referenceResolution.y - ReferenceHeight) > 0.1f)
        {
            throw new InvalidOperationException($"{rootCanvas.name} should use 1080x1920 reference resolution, got {scaler.referenceResolution}.");
        }

        if (scaler.matchWidthOrHeight < 0.95f)
        {
            throw new InvalidOperationException($"{rootCanvas.name} should match height for portrait layouts, got {scaler.matchWidthOrHeight:0.###}.");
        }

        return rootCanvas.GetComponent<RectTransform>();
    }

    private static void ValidateMobileChrome(IdlePrototypeController controller, RectTransform canvasRect)
    {
        controller.ShowHome();
        Canvas.ForceUpdateCanvases();

        var versionText = RequireObjectField<TMP_Text>(controller, "versionText");
        if (string.IsNullOrWhiteSpace(versionText.text) || !versionText.text.Contains("Prototype v"))
        {
            throw new InvalidOperationException($"Version label should show prototype/save version, got '{versionText.text}'.");
        }

        AssertTextFits(versionText, "Version label");

        var topBarRoot = RequireObjectField<RectTransform>(controller, "topBarRoot");
        var bottomNavRoot = RequireObjectField<RectTransform>(controller, "bottomNavRoot");
        AssertInsideCanvas(canvasRect, topBarRoot, "Top bar");
        AssertInsideCanvas(canvasRect, bottomNavRoot, "Bottom navigation");

        AssertButtonTarget(controller, "homeTabButton", 84f, 56f);
        AssertButtonTarget(controller, "battleTabButton", 84f, 56f);
        AssertButtonTarget(controller, "dungeonsTabButton", 84f, 56f);
        AssertButtonTarget(controller, "heroesTabButton", 84f, 56f);
        AssertButtonTarget(controller, "gearTabButton", 84f, 56f);
        AssertButtonTarget(controller, "summonTabButton", 84f, 56f);
        AssertButtonTarget(controller, "shopTabButton", 84f, 56f);
        AssertButtonTarget(controller, "campaignNavButton", 160f, 120f);
        AssertButtonTarget(controller, "villageNavButton", 120f, 120f);
        AssertButtonTarget(controller, "dungeonsNavButton", 120f, 120f);
        AssertButtonTarget(controller, "heroesNavButton", 120f, 120f);
        AssertButtonTarget(controller, "summonNavButton", 120f, 120f);
    }

    private static void ValidateScreenNavigation(IdlePrototypeController controller)
    {
        ValidateScreen(controller, "Home", controller.ShowHome, "homePanel");
        ValidateScreen(controller, "Village", controller.ShowVillage, "villagePanel");
        ValidateScreen(controller, "Dungeons", controller.ShowDungeons, "dungeonsPanel");
        ValidateScreen(controller, "Heroes", controller.ShowHeroes, "heroesPanel");
        ValidateScreen(controller, "Gear", controller.ShowGear, "gearPanel");
        ValidateScreen(controller, "Summon", controller.ShowSummon, "summonPanel");
        ValidateScreen(controller, "Shop", controller.ShowShop, "shopPanel");
        ValidateScreen(controller, "Battle", controller.ShowBattle, "battlePanel");
    }

    private static void ValidateScreen(IdlePrototypeController controller, string label, Action showAction, string panelField)
    {
        showAction();
        Canvas.ForceUpdateCanvases();

        var panel = RequireObjectField<GameObject>(controller, panelField);
        if (!panel.activeInHierarchy)
        {
            throw new InvalidOperationException($"{label} panel should be active after navigation.");
        }
    }

    private static void AssertButtonTarget(IdlePrototypeController controller, string fieldName, float minWidth, float minHeight)
    {
        var button = RequireObjectField<Button>(controller, fieldName);
        var rect = button.GetComponent<RectTransform>();
        if (rect == null)
        {
            throw new InvalidOperationException($"{fieldName} is missing RectTransform.");
        }

        if (rect.rect.width < minWidth || rect.rect.height < minHeight)
        {
            throw new InvalidOperationException($"{fieldName} touch target too small: {rect.rect.width:0.#}x{rect.rect.height:0.#}, expected at least {minWidth:0.#}x{minHeight:0.#}.");
        }
    }

    private static void AssertInsideCanvas(RectTransform canvasRect, RectTransform child, string context)
    {
        if (canvasRect == null || child == null)
        {
            throw new InvalidOperationException($"{context} is missing RectTransform context.");
        }

        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, child);
        var halfWidth = ReferenceWidth * 0.5f;
        var halfHeight = ReferenceHeight * 0.5f;
        const float tolerance = 16f;
        if (bounds.min.x < -halfWidth - tolerance || bounds.max.x > halfWidth + tolerance || bounds.min.y < -halfHeight - tolerance || bounds.max.y > halfHeight + tolerance)
        {
            throw new InvalidOperationException($"{context} should stay inside the portrait canvas. Bounds min={bounds.min}, max={bounds.max}.");
        }
    }

    private static void AssertTextFits(TMP_Text label, string context)
    {
        label.ForceMeshUpdate();
        if (label.isTextOverflowing)
        {
            var rect = label.rectTransform.rect;
            throw new InvalidOperationException($"{context} overflows: '{label.text}' width={rect.width}, height={rect.height}, fontSize={label.fontSize}.");
        }
    }

    private static T RequireObjectField<T>(object target, string fieldName) where T : UnityEngine.Object
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field: {fieldName}");
        }

        var value = field.GetValue(target) as T;
        if (value == null)
        {
            throw new InvalidOperationException($"{fieldName} should not be null.");
        }

        return value;
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
}
