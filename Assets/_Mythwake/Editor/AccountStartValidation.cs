using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AccountStartValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    [MenuItem("Mythwake/Validate Account Start")]
    public static void RunAccountStartValidation()
    {
        try
        {
            ValidateAccountStart();
            Debug.Log("Account Start validated: start overlay, Continue/Guest/Email/Register actions, masked password input, Email panel labels, Google-later hint, EN/DE text fit, and no reset trap are present.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateAccountStart()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var controller = FindSceneComponent<IdlePrototypeController>();
        if (controller == null)
        {
            throw new InvalidOperationException("Missing IdlePrototypeController in SampleScene.");
        }

        SetPrivateField(controller, "language", MythwakeLanguage.English);
        InvokePrivate(controller, "EnsureRuntimeInputStack");
        InvokePrivate(controller, "EnsureRuntimeBackendClient");
        InvokePrivate(controller, "EnsureRuntimeBackendUi");
        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "EnsureRuntimeAccountStartScreen");
        InvokePrivate(controller, "RegisterAccountStartButtons");
        InvokePrivate(controller, "ShowAccountStartScreen", (object)null);
        Canvas.ForceUpdateCanvases();

        var root = RequireObjectField<RectTransform>(controller, "accountStartRoot");
        if (!root.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Account Start Screen should be active after ShowAccountStartScreen.");
        }

        AssertFullScreen(root, "Account Start Screen");
        AssertTextField(controller, "accountStartTitleText", "Mythwake");
        AssertTextContains(controller, "accountStartStatusText", "Local Save");
        AssertTextContains(controller, "accountStartStatusText", "Server Session");
        AssertTextContains(controller, "accountStartStatusText", "Mode:");
        AssertTextContains(controller, "accountStartGoogleHintText", "Google Login via Play Games comes later.");

        var continueButton = AssertButton(controller, "accountStartContinueButton", "Continue");
        var guestButton = AssertButton(controller, "accountStartGuestButton", "Play as Guest");
        var loginButton = AssertButton(controller, "accountStartEmailLoginButton", "Email Login");
        var registerButton = AssertButton(controller, "accountStartEmailRegisterButton", "Register");
        AssertButtonHasRaycastableTarget(continueButton, "Continue");
        AssertButtonHasRaycastableTarget(guestButton, "Guest");
        AssertButtonHasRaycastableTarget(loginButton, "Email Login");
        AssertButtonHasRaycastableTarget(registerButton, "Register");
        AssertNoGoogleButton(root);
        AssertNoResetButton(root);

        var emailPanel = RequireObjectField<RectTransform>(controller, "accountStartEmailPanelRoot");
        if (emailPanel.gameObject.activeSelf)
        {
            throw new InvalidOperationException("Email panel should start hidden.");
        }

        loginButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (!emailPanel.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Email Login should open the email panel.");
        }

        AssertTextField(controller, "accountStartEmailTitleText", "Email Login");
        AssertButton(controller, "accountStartEmailSubmitButton", "Login");
        var backButton = AssertButton(controller, "accountStartEmailBackButton", "Back");
        var emailInput = RequireObjectField<TMP_InputField>(controller, "accountStartEmailInput");
        var passwordInput = RequireObjectField<TMP_InputField>(controller, "accountStartPasswordInput");
        if (emailInput.contentType != TMP_InputField.ContentType.EmailAddress)
        {
            throw new InvalidOperationException("Account Start email input should use EmailAddress content type.");
        }
        if (passwordInput.contentType != TMP_InputField.ContentType.Password)
        {
            throw new InvalidOperationException("Account Start password input should mask text.");
        }
        AssertTextContains(controller, "accountStartEmailStatusText", "Enter email");
        AssertButtonHasRaycastableTarget(backButton, "Email Back");
        backButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (emailPanel.gameObject.activeSelf)
        {
            throw new InvalidOperationException("Back should hide the email panel.");
        }

        registerButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (!emailPanel.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Register should open the email panel.");
        }

        AssertTextField(controller, "accountStartEmailTitleText", "Register");
        AssertButton(controller, "accountStartEmailSubmitButton", "Register");

        AssertTextFit(root, "Account Start Screen");

        SetPrivateField(controller, "language", MythwakeLanguage.German);
        InvokePrivate(controller, "RefreshAccountStartUi");
        Canvas.ForceUpdateCanvases();
        AssertButton(controller, "accountStartContinueButton", "Weiter");
        AssertButton(controller, "accountStartGuestButton", "Als Gast spielen");
        AssertButton(controller, "accountStartEmailRegisterButton", "Registrieren");
        AssertTextContains(controller, "accountStartGoogleHintText", "Google Login ueber Play Games kommt spaeter.");
        AssertTextFit(root, "Account Start Screen DE");
    }

    private static Button AssertButton(object controller, string fieldName, string expectedLabel)
    {
        var button = RequireObjectField<Button>(controller, fieldName);
        var label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (label == null)
        {
            throw new InvalidOperationException($"{fieldName} is missing a TMP label.");
        }

        if (!string.Equals(label.text, expectedLabel, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{fieldName} label should be '{expectedLabel}', got '{label.text}'.");
        }

        AssertTextFits(label, fieldName);
        return button;
    }

    private static void AssertTextField(object controller, string fieldName, string expected)
    {
        var text = RequireObjectField<TMP_Text>(controller, fieldName);
        if (!string.Equals(text.text, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{fieldName} should be '{expected}', got '{text.text}'.");
        }

        AssertTextFits(text, fieldName);
    }

    private static void AssertTextContains(object controller, string fieldName, string expectedFragment)
    {
        var text = RequireObjectField<TMP_Text>(controller, fieldName);
        if (string.IsNullOrWhiteSpace(text.text) || !text.text.Contains(expectedFragment))
        {
            throw new InvalidOperationException($"{fieldName} should contain '{expectedFragment}', got '{text.text}'.");
        }

        AssertTextFits(text, fieldName);
    }

    private static void AssertNoGoogleButton(RectTransform root)
    {
        var buttons = root.GetComponentsInChildren<Button>(includeInactive: true);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button != null && button.name.IndexOf("google", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException("Account Start should show a Google-later hint, not a Google login button.");
            }
        }
    }

    private static void AssertNoResetButton(RectTransform root)
    {
        var buttons = root.GetComponentsInChildren<Button>(includeInactive: true);
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button != null && button.name.IndexOf("reset", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException("Account Start should not expose reset/dev reset in the main flow.");
            }
        }
    }

    private static void AssertFullScreen(RectTransform rect, string context)
    {
        const float tolerance = 0.001f;
        if (Vector2.Distance(rect.anchorMin, Vector2.zero) > tolerance || Vector2.Distance(rect.anchorMax, Vector2.one) > tolerance)
        {
            throw new InvalidOperationException($"{context} should stretch across the full root canvas.");
        }

        if (Vector2.Distance(rect.offsetMin, Vector2.zero) > tolerance || Vector2.Distance(rect.offsetMax, Vector2.zero) > tolerance)
        {
            throw new InvalidOperationException($"{context} should not leave uncovered canvas offsets.");
        }
    }

    private static void AssertButtonHasRaycastableTarget(Button button, string context)
    {
        var target = button.targetGraphic;
        if (target == null)
        {
            throw new InvalidOperationException($"{context} button should have a target graphic.");
        }

        if (!target.raycastTarget)
        {
            throw new InvalidOperationException($"{context} button target should accept raycasts.");
        }

        if (target.color.a <= 0f)
        {
            throw new InvalidOperationException($"{context} button target should be visible for pointer hits.");
        }
    }

    private static void AssertTextFit(RectTransform root, string context)
    {
        var texts = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
        for (var i = 0; i < texts.Length; i++)
        {
            AssertTextFits(texts[i], $"{context} text {texts[i].name}");
        }
    }

    private static void AssertTextFits(TMP_Text label, string context)
    {
        if (label == null)
        {
            return;
        }

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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field: {fieldName}");
        }

        field.SetValue(target, value);
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
