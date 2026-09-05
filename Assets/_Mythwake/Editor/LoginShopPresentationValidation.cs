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
    private static void AssertLoginButtonArtwork(object controller, string fieldName)
    {
        var button = Field<Button>(controller, fieldName);
        var artwork = button.transform.Find("Login Button Artwork")?.GetComponent<RawImage>();
        if (artwork == null || artwork.texture == null || button.targetGraphic != artwork)
            throw new InvalidOperationException(fieldName + " should use the packed login artwork as its live target graphic.");
    }
    private static void Capture(Canvas canvas, string path)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var scaler in canvas.GetComponentsInChildren<LoginDesignScale>(true))
            scaler.SendMessage("LateUpdate");
        typeof(PortraitScreenshotAutomation).GetMethod("CaptureCanvas", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { canvas, path });
    }
}
