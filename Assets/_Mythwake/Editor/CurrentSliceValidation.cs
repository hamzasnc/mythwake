using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
            RunPrivateValidator(typeof(EarlyGameLoopValidation), "ValidateEarlyGameLoop", "Early Game Loop");
            RunValidator("Paladin Integration", PaladinSpineValidation.RunPaladinIntegrationValidation);
            RunValidator("Paladin Spine Handoff", PaladinSpineValidation.RunPaladinSpineValidation);

            Debug.Log("Current Mythwake slice validated: Village, Dungeons, Fast Rewards, Mobile UX, Summon, Upgrade Clutter, Home Idle Combat, Fight Formation UI, Early Game Loop, Paladin integration, and Paladin Spine handoff.");
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
    private const string ControllerPath = "Assets/_Mythwake/Scripts/IdlePrototypeController.cs";
    private const string AndroidManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
    private const string AndroidFullscreenStylesPath = "Assets/Plugins/Android/MythwakeFullscreen.androidlib/res/values/styles.xml";
    private const string AndroidFullscreenHelperPath = "Assets/Plugins/Android/MythwakeFullscreen.androidlib/src/main/java/com/mythwake/fullscreen/MythwakeFullscreen.java";
    private const string AndroidLauncherIconPath = "Assets/_Mythwake/Branding/Mythwake_icon_launcher.png";
    private const string AndroidLauncherIconGuid = "e4a4f8593c1e42d0ba29e5183e83e026";
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;

    [MenuItem("Mythwake/Validate Mobile UX")]
    public static void RunMobileUxValidation()
    {
        try
        {
            ValidateMobileUx();
            Debug.Log("Mobile UX validated: Android portrait settings, app icon, safe-area rendering, width-matched portrait CanvasScaler, EventSystem UI stack, non-blocking FPS overlay, bottom navigation targets/raycasts, version label, and core screen navigation are present.");
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

        InvokePrivate(controller, "EnsureRuntimeInputStack");
        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "EnsureRuntimeInputStack");
        InvokePrivate(controller, "EnsureRuntimePerformanceOverlay");
        InvokePrivate(controller, "RegisterNavigation");
        Canvas.ForceUpdateCanvases();

        var canvasRect = ValidatePortraitCanvas(controller);
        ValidateRuntimeInputStack(controller, canvasRect);
        ValidateMobileChrome(controller, canvasRect);
        ValidateScreenNavigation(controller);
    }

    private static void ValidateAndroidPlayerSettings()
    {
        var projectSettings = File.ReadAllText(ProjectSettingsPath);
        RequireProjectSetting(projectSettings, "defaultScreenOrientation", "0", "Android should use autorotation constrained to normal portrait for the mobile-first prototype.");
        RequireProjectSetting(projectSettings, "allowedAutorotateToPortrait", "1", "Portrait autorotation should stay enabled.");
        RequireProjectSetting(projectSettings, "allowedAutorotateToPortraitUpsideDown", "0", "Upside-down portrait should stay disabled for tester builds.");
        RequireProjectSetting(projectSettings, "allowedAutorotateToLandscapeRight", "0", "Landscape-right autorotation should stay disabled.");
        RequireProjectSetting(projectSettings, "allowedAutorotateToLandscapeLeft", "0", "Landscape-left autorotation should stay disabled.");
        RequireProjectSetting(projectSettings, "useOSAutorotation", "1", "OS autorotation should be constrained by the allowed portrait-only settings.");
        RequireProjectSetting(projectSettings, "androidRenderOutsideSafeArea", "0", "Android should keep rendering inside the safe viewport so MuMu mouse coordinates match visible UI.");
        RequireProjectSetting(projectSettings, "androidStartInFullscreen", "1", "Android should request fullscreen at launch so emulator mouse coordinates match the Unity viewport.");
        RequireProjectSetting(projectSettings, "androidFullscreenMode", "1", "Android fullscreen mode should stay enabled for the mobile test build.");
        RequireProjectSetting(projectSettings, "uIRequiresFullScreen", "1", "Android/iOS should require fullscreen for stable tester input mapping.");
        RequireProjectSetting(projectSettings, "uIStatusBarHidden", "1", "Android status bar should be hidden for stable tester input mapping.");
        RequireProjectSetting(projectSettings, "defaultScreenWidth", "1080", "Default Game View width should match the portrait reference canvas.");
        RequireProjectSetting(projectSettings, "defaultScreenHeight", "1920", "Default Game View height should match the portrait reference canvas.");
        RequireProjectSetting(projectSettings, "androidDefaultWindowWidth", "1080", "Android freeform width should match the portrait reference canvas.");
        RequireProjectSetting(projectSettings, "androidDefaultWindowHeight", "1920", "Android freeform height should match the portrait reference canvas.");
        ValidateAndroidAppIcon(projectSettings);
        ValidateAndroidFullscreenManifest();
        ValidateAndroidImmersiveModeHook();
    }

    private static void ValidateAndroidAppIcon(string projectSettings)
    {
        if (!File.Exists(AndroidLauncherIconPath))
        {
            throw new InvalidOperationException($"Android launcher icon asset is missing: {AndroidLauncherIconPath}.");
        }

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AndroidLauncherIconPath);
        if (icon == null)
        {
            throw new InvalidOperationException($"Android launcher icon asset could not be loaded: {AndroidLauncherIconPath}.");
        }

        if (icon.width != icon.height)
        {
            throw new InvalidOperationException($"Android launcher icon should be square, got {icon.width}x{icon.height}.");
        }

        var guid = AssetDatabase.AssetPathToGUID(AndroidLauncherIconPath);
        if (!string.Equals(guid, AndroidLauncherIconGuid, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Android launcher icon GUID changed. Expected {AndroidLauncherIconGuid}, got {guid}.");
        }

        if (!projectSettings.Contains("m_BuildTarget: Android") || CountOccurrences(projectSettings, AndroidLauncherIconGuid) < 18)
        {
            throw new InvalidOperationException("Android PlayerSettings should reference the Mythwake launcher icon for every generated Android icon size.");
        }
    }

    private static void ValidateAndroidFullscreenManifest()
    {
        if (!File.Exists(AndroidManifestPath))
        {
            throw new InvalidOperationException("Android fullscreen manifest override is missing, so MuMu/system bars can shift emulator pointer coordinates.");
        }

        var manifest = File.ReadAllText(AndroidManifestPath);
        RequireSourceFragment(manifest, "UnityPlayerGameActivity", "Android manifest should target Unity GameActivity.");
        RequireSourceFragment(manifest, "android:immersive=\"true\"", "Android GameActivity should request immersive behavior before Unity renders.");
        RequireSourceFragment(manifest, "android:screenOrientation=\"portrait\"", "Android GameActivity should explicitly lock normal portrait so phones do not launch upside down.");
        RequireSourceFragment(manifest, "android.intent.action.MAIN", "Android GameActivity should remain launchable from MuMu's launcher.");
        RequireSourceFragment(manifest, "android.intent.category.LAUNCHER", "Android GameActivity should expose a launcher icon in MuMu.");
        RequireSourceFragment(manifest, "@style/MythwakeFullscreenGameActivityTheme", "Android GameActivity should start with the Mythwake fullscreen AppCompat theme before Unity renders.");
        RequireSourceFragment(manifest, "tools:replace=\"android:screenOrientation,android:theme\"", "Android manifest should explicitly replace Unity's generated reverse-portrait orientation and default GameActivity theme.");

        if (!File.Exists(AndroidFullscreenStylesPath))
        {
            throw new InvalidOperationException("Android fullscreen theme library is missing, so MuMu/system bars can shift emulator pointer coordinates.");
        }

        var styles = File.ReadAllText(AndroidFullscreenStylesPath);
        RequireSourceFragment(styles, "MythwakeFullscreenGameActivityTheme", "Android library should define the fullscreen GameActivity theme.");
        RequireSourceFragment(styles, "BaseUnityGameActivityTheme", "Android fullscreen theme should inherit Unity's AppCompat GameActivity base theme.");
        RequireSourceFragment(styles, "android:windowFullscreen", "Android fullscreen theme should request hidden status bars before Unity renders.");
        RequireMissingSourceFragment(styles, "windowLayoutInDisplayCutoutMode", "Android theme should not force cutout rendering because MuMu mouse coordinates can drift from the visible UI.");
        ValidateAndroidFullscreenHelper();
    }

    private static void ValidateAndroidFullscreenHelper()
    {
        if (!File.Exists(AndroidFullscreenHelperPath))
        {
            throw new InvalidOperationException("Android fullscreen helper is missing, so MuMu/system bars can restore shifted pointer coordinates after launch.");
        }

        var helper = File.ReadAllText(AndroidFullscreenHelperPath);
        RequireSourceFragment(helper, "SYSTEM_UI_FLAG_IMMERSIVE_STICKY", "Android fullscreen helper should request sticky immersive mode.");
        RequireSourceFragment(helper, "setDecorFitsSystemWindows(true)", "Android fullscreen helper should keep the Unity view inside MuMu's safe input viewport.");
        RequireSourceFragment(helper, "WindowInsetsController", "Android fullscreen helper should hide API 30+ system bars through WindowInsetsController.");
        RequireSourceFragment(helper, "setOnSystemUiVisibilityChangeListener", "Android fullscreen helper should re-apply fullscreen when MuMu restores system UI.");
        RequireSourceFragment(helper, "setOnApplyWindowInsetsListener(null)", "Android fullscreen helper should avoid an inset reapply loop that can keep MuMu from opening cleanly.");
        RequireSourceFragment(helper, "postDelayed", "Android fullscreen helper should retry after launch because GameActivity/MuMu can restore bars during startup.");
        RequireMissingSourceFragment(helper, "SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN", "Android fullscreen helper should not lay Unity behind system bars because MuMu can keep stale mouse coordinates.");
        RequireMissingSourceFragment(helper, "SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION", "Android fullscreen helper should not lay Unity behind the navigation bar because MuMu can keep stale mouse coordinates.");
        RequireMissingSourceFragment(helper, "LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES", "Android fullscreen helper should not force cutout rendering in MuMu.");
    }

    private static void ValidateAndroidImmersiveModeHook()
    {
        var controllerSource = File.ReadAllText(ControllerPath);
        RequireSourceFragment(controllerSource, "EnsureAndroidImmersiveMode();", "IdlePrototypeController should apply Android immersive fullscreen at startup/resume/focus.");
        RequireSourceFragment(controllerSource, "MythwakeFullscreen", "IdlePrototypeController should call the Android fullscreen helper so MuMu/system bars do not shift pointer coordinates.");
        RequireSourceFragment(controllerSource, "OnApplicationFocus", "Android immersive fullscreen should be re-applied when the app regains focus.");
        RequireSourceFragment(controllerSource, "ReapplyAndroidImmersiveModeRoutine", "Android immersive fullscreen should be re-applied after first frame because emulators can restore system bars during launch.");
    }

    private static void RequireSourceFragment(string source, string fragment, string message)
    {
        if (!source.Contains(fragment))
        {
            throw new InvalidOperationException($"{message} Missing source fragment '{fragment}'.");
        }
    }

    private static void RequireMissingSourceFragment(string source, string fragment, string message)
    {
        if (source.Contains(fragment))
        {
            throw new InvalidOperationException($"{message} Unexpected source fragment '{fragment}'.");
        }
    }

    private static void RequireProjectSetting(string source, string key, string value, string message)
    {
        var expected = $"  {key}: {value}";
        if (!source.Contains(expected))
        {
            throw new InvalidOperationException($"{message} Expected '{expected.Trim()}'.");
        }
    }

    private static int CountOccurrences(string source, string fragment)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(fragment, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += fragment.Length;
        }

        return count;
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

        if (scaler.matchWidthOrHeight > 0.05f)
        {
            throw new InvalidOperationException($"{rootCanvas.name} should match width for narrow/tall Android phones so side chrome is not cropped, got {scaler.matchWidthOrHeight:0.###}.");
        }

        return rootCanvas.GetComponent<RectTransform>();
    }

    private static void ValidateRuntimeInputStack(IdlePrototypeController controller, RectTransform canvasRect)
    {
        var rootCanvas = canvasRect != null ? canvasRect.GetComponent<Canvas>() : null;
        if (rootCanvas == null)
        {
            throw new InvalidOperationException("Runtime UI should have a root Canvas for Android input.");
        }

        if (rootCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            throw new InvalidOperationException($"{rootCanvas.name} is missing GraphicRaycaster, so Android UI buttons cannot receive pointer hits.");
        }

        var eventSystem = FindSceneComponent<EventSystem>();
        if (eventSystem == null)
        {
            throw new InvalidOperationException("Scene is missing an EventSystem for Android UI input.");
        }

        var activeModules = 0;
        var hasInputSystemModule = false;
        var modules = eventSystem.GetComponents<BaseInputModule>();
        for (var i = 0; i < modules.Length; i++)
        {
            var module = modules[i];
            if (module == null || !module.enabled)
            {
                continue;
            }

            activeModules++;
            var typeName = module.GetType().FullName ?? module.GetType().Name;
            hasInputSystemModule |= typeName.Contains("InputSystemUIInputModule");
            if (typeName.Contains("InputSystemUIInputModule"))
            {
                var actionsProperty = module.GetType().GetProperty("actionsAsset");
                var actionsAsset = actionsProperty != null ? actionsProperty.GetValue(module) as UnityEngine.Object : null;
                if (actionsAsset == null)
                {
                    throw new InvalidOperationException("InputSystemUIInputModule should have an actions asset/default actions assigned for Android touch.");
                }
            }
        }

        if (activeModules == 0)
        {
            throw new InvalidOperationException("EventSystem has no enabled UI input module.");
        }

        var projectSettings = File.ReadAllText(ProjectSettingsPath);
        if (!projectSettings.Contains("  activeInputHandler: 1"))
        {
            throw new InvalidOperationException("Project should use the Input System backend so Android/MuMu host mouse and touch share Unity's standard pointer path.");
        }

        if (!hasInputSystemModule)
        {
            throw new InvalidOperationException("EventSystem should keep an InputSystemUIInputModule for editor/default tooling.");
        }

        var controllerSource = File.ReadAllText(ControllerPath);
        RequireSourceFragment(controllerSource, "inputSystemModule.enabled = true", "Runtime input setup should enable Unity's standard InputSystemUIInputModule for Android/MuMu pointer input.");
        RequireSourceFragment(controllerSource, "inputSystemModule.AssignDefaultActions()", "Runtime input setup should assign default UI actions so Android/MuMu pointer devices can drive buttons.");
        RequireMissingSourceFragment(controllerSource, "androidStandaloneModule", "Runtime input setup should not force a separate Android UI input module when the real issue is reverse portrait orientation.");
        RequireMissingSourceFragment(controllerSource, "MythwakeMuMuInputModule", "Runtime input setup should not use a hand-rolled MuMu pointer transform because it can route visible buttons to neighboring controls.");
        RequireMissingSourceFragment(controllerSource, "MythwakeAndroidPointerInputModule", "Runtime input setup should not mirror pointer coordinates because the Android activity must launch in normal portrait.");

        var performanceOverlay = RequireObjectField<TMP_Text>(controller, "performanceOverlayText");
        if (performanceOverlay.raycastTarget)
        {
            throw new InvalidOperationException("Runtime FPS overlay must not intercept Android button touches.");
        }

        AssertInsideCanvas(canvasRect, performanceOverlay.rectTransform, "Runtime FPS overlay");
        AssertTextFits(performanceOverlay, "Runtime FPS overlay");
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
        AssertBottomAnchored(bottomNavRoot, "Bottom navigation");

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

        ValidateNavigationButtonClick(controller, "homeTabButton", "Home tab", "homePanel");
        ValidateNavigationButtonClick(controller, "battleTabButton", "Battle tab", "battlePanel");
        ValidateNavigationButtonClick(controller, "dungeonsTabButton", "Dungeons tab", "dungeonsPanel");
        ValidateNavigationButtonClick(controller, "heroesTabButton", "Heroes tab", "heroesPanel");
        ValidateNavigationButtonClick(controller, "gearTabButton", "Gear tab", "gearPanel");
        ValidateNavigationButtonClick(controller, "summonTabButton", "Summon tab", "summonPanel");
        ValidateNavigationButtonClick(controller, "shopTabButton", "Shop tab", "shopPanel");
        ValidateNavigationButtonClick(controller, "campaignNavButton", "Campaign nav", "homePanel", requireActive: true);
        ValidateNavigationButtonClick(controller, "villageNavButton", "Village nav", "villagePanel", requireActive: true);
        ValidateNavigationButtonClick(controller, "dungeonsNavButton", "Dungeons nav", "dungeonsPanel", requireActive: true);
        ValidateNavigationButtonClick(controller, "heroesNavButton", "Heroes nav", "heroesPanel", requireActive: true);
        ValidateNavigationButtonClick(controller, "summonNavButton", "Summon nav", "summonPanel", requireActive: true);
    }

    private static void ValidateNavigationButtonClick(IdlePrototypeController controller, string buttonField, string label, string panelField, bool requireActive = false)
    {
        controller.ShowHome();
        Canvas.ForceUpdateCanvases();

        var button = RequireObjectField<Button>(controller, buttonField);
        if (!button.gameObject.activeInHierarchy)
        {
            if (requireActive)
            {
                throw new InvalidOperationException($"{label} should be active for mobile navigation.");
            }

            return;
        }

        button.onClick.Invoke();
        Canvas.ForceUpdateCanvases();

        var panel = RequireObjectField<GameObject>(controller, panelField);
        if (!panel.activeInHierarchy)
        {
            throw new InvalidOperationException($"{label} should open {panelField}.");
        }
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

        AssertButtonCenterRaycast(button, fieldName);
    }

    private static void AssertButtonCenterRaycast(Button button, string context)
    {
        if (button == null || !button.gameObject.activeInHierarchy)
        {
            return;
        }

        var eventSystem = EventSystem.current ?? FindSceneComponent<EventSystem>();
        if (eventSystem == null)
        {
            throw new InvalidOperationException($"{context} cannot be raycast-tested because the scene has no EventSystem.");
        }

        var rect = button.GetComponent<RectTransform>();
        var canvas = button.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var screenPosition = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
        var results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);
        if (results.Count == 0)
        {
            AssertButtonHasRaycastableTarget(button, context);
            return;
        }

        Button firstButton = null;
        for (var i = 0; i < results.Count; i++)
        {
            var hitButton = results[i].gameObject.GetComponentInParent<Button>();
            if (hitButton == null)
            {
                continue;
            }

            if (firstButton == null)
            {
                firstButton = hitButton;
            }

            if (hitButton == button)
            {
                return;
            }
        }

        var firstHitName = results.Count > 0 ? results[0].gameObject.name : "<none>";
        var firstButtonName = firstButton != null ? firstButton.name : "<none>";
        throw new InvalidOperationException($"{context} center raycast should hit its own button. First hit={firstHitName}, first button={firstButtonName}.");
    }

    private static void AssertButtonHasRaycastableTarget(Button button, string context)
    {
        var target = button.targetGraphic;
        if (target == null)
        {
            throw new InvalidOperationException($"{context} should have a target graphic for pointer hits.");
        }

        if (!target.raycastTarget)
        {
            throw new InvalidOperationException($"{context} target graphic should accept pointer raycasts.");
        }

        if (target.color.a <= 0f)
        {
            throw new InvalidOperationException($"{context} target graphic is fully transparent and can be culled before raycasts.");
        }

        var buttonRect = button.GetComponent<RectTransform>();
        var targetRect = target.GetComponent<RectTransform>();
        if (buttonRect == null || targetRect == null)
        {
            throw new InvalidOperationException($"{context} is missing RectTransform data for pointer-hit validation.");
        }

        var center = buttonRect.TransformPoint(buttonRect.rect.center);
        if (!WorldPointInsideRect(targetRect, center))
        {
            throw new InvalidOperationException($"{context} target graphic should cover the button center.");
        }
    }

    private static bool WorldPointInsideRect(RectTransform rect, Vector3 worldPoint)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        var maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        var minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        var maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        return worldPoint.x >= minX && worldPoint.x <= maxX && worldPoint.y >= minY && worldPoint.y <= maxY;
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

    private static void AssertBottomAnchored(RectTransform rect, string context)
    {
        if (rect == null)
        {
            throw new InvalidOperationException($"{context} is missing RectTransform.");
        }

        const float tolerance = 0.01f;
        if (Mathf.Abs(rect.anchorMin.x - 0.5f) > tolerance || Mathf.Abs(rect.anchorMax.x - 0.5f) > tolerance ||
            Mathf.Abs(rect.anchorMin.y) > tolerance || Mathf.Abs(rect.anchorMax.y) > tolerance ||
            Mathf.Abs(rect.pivot.x - 0.5f) > tolerance || Mathf.Abs(rect.pivot.y) > tolerance ||
            Mathf.Abs(rect.anchoredPosition.x) > tolerance || Mathf.Abs(rect.anchoredPosition.y) > tolerance)
        {
            throw new InvalidOperationException($"{context} should be hard-anchored to the bottom center of the portrait canvas.");
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

public static class AndroidBuildAutomation
{
    private const string DefaultScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Mythwake/Build Android APK")]
    public static void BuildAndroidApk()
    {
        try
        {
            var outputPath = GetBuildOutputPath();
            var report = BuildAndroidApk(outputPath);
            var summary = report.summary;
            Debug.Log($"Android APK build succeeded: {summary.outputPath} ({summary.totalSize} bytes, {summary.totalTime}).");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static BuildReport BuildAndroidApk(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException($"Invalid Android output path: {outputPath}");
        }

        Directory.CreateDirectory(directory);
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;

        var scenes = GetEnabledScenes();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        Debug.Log($"Building Android APK to {outputPath} with {scenes.Length} scene(s).");
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Android APK build failed: {summary.result} after {summary.totalTime}. Errors={summary.totalErrors}, warnings={summary.totalWarnings}.");
        }

        return report;
    }

    private static string[] GetEnabledScenes()
    {
        var configuredScenes = EditorBuildSettings.scenes;
        var scenes = new System.Collections.Generic.List<string>();
        for (var i = 0; i < configuredScenes.Length; i++)
        {
            var scene = configuredScenes[i];
            if (scene != null && scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            {
                scenes.Add(scene.path);
            }
        }

        if (scenes.Count == 0)
        {
            scenes.Add(DefaultScenePath);
        }

        return scenes.ToArray();
    }

    private static string GetBuildOutputPath()
    {
        var outputPath = GetCommandLineValue("-mythwakeAndroidOutput");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine("Builds", "Android", $"Mythwake-{IdlePrototypeController.PrototypeVersion}.apk");
        }

        if (Path.IsPathRooted(outputPath))
        {
            return outputPath;
        }

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new InvalidOperationException("Could not resolve Unity project root.");
        }

        return Path.GetFullPath(Path.Combine(projectRoot, outputPath));
    }

    private static string GetCommandLineValue(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}

public static class PortraitScreenshotAutomation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const int ScreenshotWidth = 1080;
    private const int ScreenshotHeight = 1920;

    [MenuItem("Mythwake/Capture Portrait Screenshot Set")]
    public static void CapturePortraitScreenshotSet()
    {
        try
        {
            var outputDirectory = GetOutputDirectory();
            Directory.CreateDirectory(outputDirectory);

            EditorSceneManager.OpenScene(ScenePath);
            var controller = FindSceneComponent<IdlePrototypeController>();
            if (controller == null)
            {
                throw new InvalidOperationException("Missing IdlePrototypeController in SampleScene.");
            }

            InvokePrivate(controller, "EnsureRuntimeScreenLayout");
            InvokePrivate(controller, "RegisterNavigation");
            Canvas.ForceUpdateCanvases();

            var canvas = RequireObjectField<RectTransform>(controller, "topBarRoot").GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                throw new InvalidOperationException("Could not find runtime UI canvas for portrait screenshots.");
            }

            CaptureState(outputDirectory, "01-home", controller, () => controller.ShowHome(), canvas);
            CaptureState(outputDirectory, "02-home-stage-detail", controller, () =>
            {
                controller.ShowHome();
                SetObjectField(controller, "selectedCampaignStage", GetObjectField<int>(controller, "enemyLevel"));
                InvokePrivate(controller, "SetCampaignStageDetailPopupVisible", true);
            }, canvas);
            CaptureState(outputDirectory, "03-home-patrol-info", controller, () =>
            {
                controller.ShowHome();
                InvokePrivate(controller, "SetHomeIdleInfoPopupVisible", true);
            }, canvas);
            CaptureState(outputDirectory, "04-village", controller, () => controller.ShowVillage(), canvas);
            CaptureState(outputDirectory, "05-fast-rewards", controller, () =>
            {
                controller.ShowHome();
                InvokePrivate(controller, "SetFastRewardsPopupVisible", true);
            }, canvas);
            CaptureState(outputDirectory, "06-hero-detail", controller, () =>
            {
                controller.ShowHeroes();
                InvokePrivate(controller, "ShowHeroDetail", 0);
            }, canvas);
            CaptureState(outputDirectory, "07-gear", controller, () => controller.ShowGear(), canvas);
            CaptureState(outputDirectory, "08-summon", controller, () => controller.ShowSummon(), canvas);
            CaptureState(outputDirectory, "09-summon-result", controller, () =>
            {
                controller.ShowSummon();
                InvokePrivate(controller, "ShowSummonResultPopup", new[] { 1, 0, 0, 0, 0 }, 1);
            }, canvas);
            CaptureState(outputDirectory, "10-fight-formation", controller, () => controller.ShowBattle(), canvas);
            CaptureState(outputDirectory, "11-fight-visible", controller, () =>
            {
                controller.ShowBattle();
                SetPrivateEnumField(controller, "battleFlowMode", "Fight");
                InvokePrivate(controller, "ApplyBattleFlowVisibility");
                InvokePrivate(controller, "RefreshFightArenaBackground", false);
                InvokePrivate(controller, "PrepareFightAnimationTextures", 1, false, null);
                InvokePrivate(controller, "InitializeFightSkillState");
            }, canvas);

            Debug.Log($"Portrait screenshot set captured to {outputDirectory}.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void CaptureState(string outputDirectory, string fileName, IdlePrototypeController controller, Action setup, Canvas canvas)
    {
        setup();
        InvokePrivate(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();
        CaptureCanvas(canvas, Path.Combine(outputDirectory, $"{fileName}.png"));
    }

    private static void CaptureCanvas(Canvas canvas, string path)
    {
        var oldRenderMode = canvas.renderMode;
        var oldWorldCamera = canvas.worldCamera;
        var oldPlaneDistance = canvas.planeDistance;
        var oldActive = RenderTexture.active;

        var cameraObject = new GameObject("Mythwake Portrait Screenshot Camera");
        var camera = cameraObject.AddComponent<Camera>();
        var renderTexture = new RenderTexture(ScreenshotWidth, ScreenshotHeight, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(ScreenshotWidth, ScreenshotHeight, TextureFormat.RGB24, false);

        try
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = ScreenshotHeight * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.targetTexture = renderTexture;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            Canvas.ForceUpdateCanvases();

            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, ScreenshotWidth, ScreenshotHeight), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log($"Captured portrait screenshot: {path}");
        }
        finally
        {
            canvas.renderMode = oldRenderMode;
            canvas.worldCamera = oldWorldCamera;
            canvas.planeDistance = oldPlaneDistance;
            RenderTexture.active = oldActive;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static string GetOutputDirectory()
    {
        var outputDirectory = GetCommandLineValue("-mythwakeScreenshotOutput");
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = Path.Combine("Temp", "android-fallback-screenshots");
        }

        if (Path.IsPathRooted(outputDirectory))
        {
            return outputDirectory;
        }

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new InvalidOperationException("Could not resolve Unity project root.");
        }

        return Path.GetFullPath(Path.Combine(projectRoot, outputDirectory));
    }

    private static string GetCommandLineValue(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static T RequireObjectField<T>(object target, string fieldName) where T : UnityEngine.Object
    {
        var value = GetObjectField<T>(target, fieldName);
        if (value == null)
        {
            throw new InvalidOperationException($"{fieldName} should not be null.");
        }

        return value;
    }

    private static T GetObjectField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field: {fieldName}");
        }

        return (T)field.GetValue(target);
    }

    private static void SetObjectField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field: {fieldName}");
        }

        field.SetValue(target, value);
    }

    private static void SetPrivateEnumField(object target, string fieldName, string enumName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field: {fieldName}");
        }

        field.SetValue(target, Enum.Parse(field.FieldType, enumName));
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
