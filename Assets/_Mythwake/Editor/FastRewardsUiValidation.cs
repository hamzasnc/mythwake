using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FastRewardsUiValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Mythwake/Validate Fast Rewards UI")]
    public static void RunFastRewardsUiValidation()
    {
        try
        {
            ValidateFastRewardsUi();
            Debug.Log("Fast Rewards UI validated: popup controls, local copy, reward button state, and Server Mode fallback copy are present.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateFastRewardsUi()
    {
        EditorSceneManager.OpenScene(ScenePath);

        var controller = FindSceneComponent<IdlePrototypeController>();
        if (controller == null)
        {
            throw new InvalidOperationException("Missing IdlePrototypeController in SampleScene.");
        }

        InvokePrivate(controller, "EnsureRuntimeScreenLayout");
        InvokePrivate(controller, "RegisterNavigation");
        controller.ShowHome();
        Canvas.ForceUpdateCanvases();

        var popup = RequireObject("Fast Rewards Popup", false);
        var body = RequireObject("Fast Rewards Body", false);
        var redeemButton = RequireButton("Fast Rewards Redeem Button");
        var closeButton = RequireButton("Fast Rewards Close Button");

        AssertInsideParent(popup, body);
        AssertInsideParent(popup, redeemButton.gameObject);
        AssertInsideParent(popup, closeButton.gameObject);

        SetPrivateField(controller, "backendGameplayEnabled", false);
        SetPrivateField(controller, "afkRewardStoredSeconds", 7200f);
        InvokePrivate(controller, "ShowFastRewardsPopup");
        Canvas.ForceUpdateCanvases();

        RequireActive("Fast Rewards Popup");
        var localBodyText = RequireText(body, "Fast Rewards Body").text;
        RequireCopy(localBodyText, "Local Mode: continuous stored rewards");
        RequireCopy(localBodyText, "Stored:");
        RequireCopy(localBodyText, "Rate:");
        RequireCopy(localBodyText, "Village bonus:");
        RequireCopy(localBodyText, "Ready:");

        if (!redeemButton.interactable)
        {
            throw new InvalidOperationException("Fast Rewards redeem button should be interactable when local rewards are ready.");
        }

        var localButtonLabel = GetButtonLabel(redeemButton);
        if (localButtonLabel != "Redeem")
        {
            throw new InvalidOperationException($"Fast Rewards local button label mismatch: '{localButtonLabel}'");
        }

        SetPrivateField(controller, "backendGameplayEnabled", true);
        SetPrivateField(controller, "backendRequestInProgress", false);
        SetPrivateField(controller, "hasBackendDefinitions", false);
        InvokePrivate(controller, "RefreshFastRewardsPopupUi");
        Canvas.ForceUpdateCanvases();

        var serverBodyText = RequireText(body, "Fast Rewards Body").text;
        RequireCopy(serverBodyText, "Server Mode: backend-authoritative rewards");
        RequireCopy(serverBodyText, "Definitions not loaded yet.");
        RequireCopy(serverBodyText, "Local stored rewards are paused in Server Mode.");

        var serverButtonLabel = GetButtonLabel(redeemButton);
        if (serverButtonLabel != "Claim")
        {
            throw new InvalidOperationException($"Fast Rewards Server Mode button label mismatch: '{serverButtonLabel}'");
        }

        if (!popup.activeInHierarchy)
        {
            throw new InvalidOperationException("Fast Rewards popup should remain active after local and Server Mode refreshes.");
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

    private static TMP_Text RequireText(GameObject gameObject, string name)
    {
        var text = gameObject.GetComponent<TMP_Text>();
        if (text == null)
        {
            throw new InvalidOperationException($"{name} is missing TMP_Text.");
        }

        return text;
    }

    private static void RequireCopy(string text, string expected)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.Contains(expected))
        {
            throw new InvalidOperationException($"Fast Rewards copy is missing '{expected}': '{text}'");
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
