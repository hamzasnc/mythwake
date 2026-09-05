using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LoginShopPresentationValidation
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Mythwake/Validate Login and Shop Presentation")]
    public static void Run()
    {
        try
        {
            AccountStartValidation.RunAccountStartValidation();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var controller = UnityEngine.Object.FindAnyObjectByType<IdlePrototypeController>();
            Call(controller, "EnsureRuntimeDebugUi");
            Call(controller, "EnsureRuntimeBackendUi");
            Call(controller, "EnsureRuntimeScreenLayout");
            Call(controller, "RegisterNavigation");
            Call(controller, "EnsureRuntimeAccountStartScreen");
            Call(controller, "RegisterAccountStartButtons");
            var canvas = Field<RectTransform>(controller, "topBarRoot").GetComponentInParent<Canvas>();
            var output = Path.GetFullPath("docs/screenshots/login-shop");
            Directory.CreateDirectory(output);
            Call(controller, "ShowAccountStartScreen", new object[] { null });
            // Reproduce the player initialization order that previously restored
            // the brown global button skin behind the login artwork.
            Call(controller, "EnsureRuntimeArtUi");
            AssertLoginButtonArtwork(controller, "accountStartContinueButton");
            AssertLoginButtonArtwork(controller, "accountStartEmailLoginButton");
            AssertLoginButtonArtwork(controller, "accountStartEmailRegisterButton");
            AssertLoginButtonArtwork(controller, "accountStartGuestButton");
            Capture(canvas, Path.Combine(output, "login.png"));
            Field<Button>(controller, "accountStartEmailLoginButton").onClick.Invoke();
            Capture(canvas, Path.Combine(output, "email-login.png"));
            Call(controller, "CloseAccountStartEmailPanel");
            Field<RectTransform>(controller, "accountStartRoot").gameObject.SetActive(false);
            controller.ShowShop();
            var shop = UnityEngine.Object.FindAnyObjectByType<MythwakeShopUI>();
            var layer = Field<RectTransform>(shop, "referenceLayer");
            var size = layer.sizeDelta;
            var position = layer.anchoredPosition;
            var scale = layer.localScale;
            var tabType = typeof(MythwakeShopUI).GetNestedType("ShopTab", BindingFlags.NonPublic);
            Call(shop, "SelectTab", Enum.Parse(tabType, "Crystals"));
            ClickShopChrome(shop, "Reference Gem Plus");
            AssertShopTabContent(shop, 1, "Gem plus should select Crystals.");
            ClickShopChrome(shop, "Reference Gold Plus");
            AssertShopTabContent(shop, 2, "Gold plus should select Bundles.");
            ClickShopChrome(shop, "Reference Management Menu");
            if (!Field<RectTransform>(controller, "managementPopupRoot").gameObject.activeInHierarchy)
                throw new InvalidOperationException("Shop management menu should open from a secondary tab.");
            if (Field<RectTransform>(controller, "topBarRoot").gameObject.activeSelf)
                throw new InvalidOperationException("Opening the shop menu must not reveal the legacy resource bar.");
            if (!layer.gameObject.activeInHierarchy)
                throw new InvalidOperationException("The reference shop chrome should remain visible behind its menu.");
            Capture(canvas, Path.Combine(output, "shop-management-menu.png"));
            Call(controller, "HideManagementPopup");

            var navigationButtons = new[]
            {
                "Reference Heroes Navigation", "Reference Village Navigation", "Reference Home Navigation",
                "Reference Dungeons Navigation", "Reference Summon Navigation"
            };
            var navigationPanels = new[] { "heroesPanel", "villagePanel", "homePanel", "dungeonsPanel", "summonPanel" };
            for (var i = 0; i < navigationButtons.Length; i++)
            {
                controller.ShowShop();
                Call(shop, "SelectTab", Enum.Parse(tabType, "Crystals"));
                ClickShopChrome(shop, navigationButtons[i]);
                if (!Field<GameObject>(controller, navigationPanels[i]).activeInHierarchy)
                    throw new InvalidOperationException(navigationButtons[i] + " should work from a secondary shop tab.");
            }
            controller.ShowShop();
            foreach (var tab in new[] { "Featured", "Crystals", "Bundles", "BattlePass", "Featured" })
            {
                Call(shop, "SelectTab", Enum.Parse(tabType, tab));
                Canvas.ForceUpdateCanvases();
                if (!layer.gameObject.activeInHierarchy || layer.sizeDelta != size || layer.anchoredPosition != position || layer.localScale != scale)
                    throw new InvalidOperationException("Shop chrome changed on " + tab);
                Capture(canvas, Path.Combine(output, "shop-" + tab + ".png"));
            }
            controller.ShowHome();
            if (layer.gameObject.activeInHierarchy || Field<RectTransform>(shop, "referenceTabHighlight").gameObject.activeInHierarchy)
                throw new InvalidOperationException("Shop decoration leaked onto Home.");
            Debug.Log("LOGIN_SHOP_VALIDATED: login actions and shared shop geometry passed. Screenshots: " + output);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Flags).Invoke(owner, args);
    private static void ClickShopChrome(MythwakeShopUI shop, string name)
    {
        var layer = Field<RectTransform>(shop, "referenceChromeHitLayer");
        var button = layer.Find(name)?.GetComponent<Button>();
        if (button == null || !button.gameObject.activeInHierarchy)
            throw new InvalidOperationException(name + " should be active on top of secondary shop content.");
        button.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
    }
    private static void AssertShopTabContent(MythwakeShopUI shop, int index, string error)
    {
        var roots = Field<RectTransform[]>(shop, "contentRoots");
        if (roots == null || index >= roots.Length || !roots[index].gameObject.activeSelf)
            throw new InvalidOperationException(error);
    }
    private static void AssertLoginButtonArtwork(object controller, string fieldName)
    {
        var button = Field<Button>(controller, fieldName);
        var artwork = button.transform.Find("Login Button Artwork")?.GetComponent<RawImage>();
        if (artwork == null || artwork.texture == null || button.targetGraphic != artwork)
            throw new InvalidOperationException(fieldName + " should use the packed login artwork as its live target graphic.");
        var legacyImage = button.GetComponent<Image>();
        if (legacyImage != null && legacyImage.enabled)
            throw new InvalidOperationException(fieldName + " should keep its legacy button image disabled.");
    }
    private static void Capture(Canvas canvas, string path)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var scaler in canvas.GetComponentsInChildren<LoginDesignScale>(true))
            scaler.SendMessage("LateUpdate");
        typeof(PortraitScreenshotAutomation).GetMethod("CaptureCanvas", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { canvas, path });
    }
}
