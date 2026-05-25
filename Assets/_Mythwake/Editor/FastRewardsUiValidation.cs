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
            Debug.Log("Fast Rewards UI validated: popup controls, local/capped copy, cap-left copy, local redeem reset, popup exclusivity, close flow, reward button state, and Server Mode claim timing copy are present.");
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
        var bodyText = RequireText(body, "Fast Rewards Body");
        var localBodyText = bodyText.text;
        RequireCopy(localBodyText, "Local Mode: continuous stored rewards");
        RequireCopy(localBodyText, "Stored:");
        RequireCopy(localBodyText, "Cap left: 22h 0m");
        RequireCopy(localBodyText, "Rate:");
        RequireCopy(localBodyText, "Village bonus:");
        RequireCopy(localBodyText, "Ready:");
        AssertTextFits(bodyText, "Fast Rewards local body");

        if (!redeemButton.interactable)
        {
            throw new InvalidOperationException("Fast Rewards redeem button should be interactable when local rewards are ready.");
        }

        var localButtonLabel = GetButtonLabel(redeemButton);
        if (localButtonLabel != "Redeem")
        {
            throw new InvalidOperationException($"Fast Rewards local button label mismatch: '{localButtonLabel}'");
        }

        ValidateLocalFastRewardsRedeemFlow(controller, popup, bodyText, redeemButton);
        ValidateFastRewardsPopupExclusivity(controller, popup);

        AssertLocalFastRewardsState(controller, bodyText, redeemButton, 0f, "Stored: 0s / 24h 0m", "Cap left: 24h 0m", "Ready: +0 Gold   +0 Essence", false);
        AssertLocalFastRewardsState(controller, bodyText, redeemButton, 24f * 60f * 60f, "Stored: 24h 0m / 24h 0m", "Cap left: capped", "Ready:", true);

        SetPrivateField(controller, "backendGameplayEnabled", true);
        SetPrivateField(controller, "backendClient", null);
        SetPrivateField(controller, "backendRequestInProgress", false);
        SetPrivateField(controller, "hasBackendDefinitions", false);
        InvokePrivate(controller, "RefreshFastRewardsPopupUi");
        Canvas.ForceUpdateCanvases();

        var serverBodyText = bodyText.text;
        RequireCopy(serverBodyText, "Server Mode: backend-authoritative rewards");
        RequireCopy(serverBodyText, "Definitions not loaded yet.");
        RequireCopy(serverBodyText, "Local stored rewards are paused in Server Mode.");
        AssertTextFits(bodyText, "Fast Rewards Server Mode fallback body");

        var serverButtonLabel = GetButtonLabel(redeemButton);
        if (serverButtonLabel != "Claim")
        {
            throw new InvalidOperationException($"Fast Rewards Server Mode button label mismatch: '{serverButtonLabel}'");
        }

        if (redeemButton.interactable)
        {
            throw new InvalidOperationException("Fast Rewards Server Mode fallback Claim button should stay disabled without a backend session.");
        }

        SetPrivateField(controller, "backendDefinitions", CreateServerAfkDefinitions());
        SetPrivateField(controller, "hasBackendDefinitions", true);
        SetPrivateField(controller, "backendLastAfkClaimUtc", DateTime.UtcNow.ToString("o"));
        InvokePrivate(controller, "RefreshFastRewardsPopupUi");
        Canvas.ForceUpdateCanvases();

        RequireCopy(bodyText.text, "Claim status: wait");
        AssertTextFits(bodyText, "Fast Rewards Server Mode waiting body");
        if (redeemButton.interactable)
        {
            throw new InvalidOperationException("Fast Rewards Server Mode Claim button should stay disabled until the backend min claim time is reached.");
        }

        SetPrivateField(controller, "backendLastAfkClaimUtc", DateTime.UtcNow.AddSeconds(-660).ToString("o"));
        InvokePrivate(controller, "RefreshFastRewardsPopupUi");
        Canvas.ForceUpdateCanvases();

        RequireCopy(bodyText.text, "Claim status: ready");
        RequireCopy(bodyText.text, "Ready estimate:");
        AssertTextFits(bodyText, "Fast Rewards Server Mode ready body");
        if (redeemButton.interactable)
        {
            throw new InvalidOperationException("Fast Rewards Server Mode Claim button should still require a backend session even after the min claim time is reached.");
        }

        if (!popup.activeInHierarchy)
        {
            throw new InvalidOperationException("Fast Rewards popup should remain active after local and Server Mode refreshes.");
        }

        closeButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (popup.activeInHierarchy)
        {
            throw new InvalidOperationException("Fast Rewards popup should close from its close button.");
        }
    }

    private static void ValidateFastRewardsPopupExclusivity(IdlePrototypeController controller, GameObject popup)
    {
        InvokePrivate(controller, "ShowHomeIdleInfoPopup");
        Canvas.ForceUpdateCanvases();
        RequireActive("Home Idle Info Popup");

        InvokePrivate(controller, "ShowFastRewardsPopup");
        Canvas.ForceUpdateCanvases();

        if (!popup.activeInHierarchy)
        {
            throw new InvalidOperationException("Fast Rewards popup should be active after reopening it.");
        }

        RequireInactive("Home Idle Info Popup", "Fast Rewards should close the Home idle info popup.");
    }

    private static void AssertLocalFastRewardsState(IdlePrototypeController controller, TMP_Text bodyText, Button redeemButton, float storedSeconds, string expectedStoredLine, string expectedCapLine, string expectedReadyLine, bool expectRedeemInteractable)
    {
        SetPrivateField(controller, "backendGameplayEnabled", false);
        SetPrivateField(controller, "afkRewardStoredSeconds", storedSeconds);
        InvokePrivate(controller, "RefreshFastRewardsPopupUi");
        Canvas.ForceUpdateCanvases();

        RequireCopy(bodyText.text, expectedStoredLine);
        RequireCopy(bodyText.text, expectedCapLine);
        RequireCopy(bodyText.text, expectedReadyLine);
        AssertTextFits(bodyText, $"Fast Rewards local {expectedStoredLine}");

        if (redeemButton.interactable != expectRedeemInteractable)
        {
            throw new InvalidOperationException($"Fast Rewards redeem button state mismatch for '{expectedStoredLine}'. Expected {expectRedeemInteractable}, got {redeemButton.interactable}.");
        }

        var buttonLabel = GetButtonLabel(redeemButton);
        if (buttonLabel != "Redeem")
        {
            throw new InvalidOperationException($"Fast Rewards local button label should stay Redeem for '{expectedStoredLine}', got '{buttonLabel}'.");
        }
    }

    private static void ValidateLocalFastRewardsRedeemFlow(IdlePrototypeController controller, GameObject popup, TMP_Text bodyText, Button redeemButton)
    {
        var backendBefore = GetPrivateField<bool>(controller, "backendGameplayEnabled");
        var storedBefore = GetPrivateField<float>(controller, "afkRewardStoredSeconds");
        var goldBefore = GetPrivateField<int>(controller, "gold");
        var essenceBefore = GetPrivateField<int>(controller, "mythEssence");
        var lastGoldBefore = GetPrivateField<int>(controller, "lastOfflineGoldReward");
        var lastEssenceBefore = GetPrivateField<int>(controller, "lastOfflineReward");
        var lastSecondsBefore = GetPrivateField<int>(controller, "lastOfflineSeconds");
        var lastServerBefore = GetPrivateField<bool>(controller, "lastOfflineRewardIsServer");

        try
        {
            SetPrivateField(controller, "backendGameplayEnabled", false);
            SetPrivateField(controller, "afkRewardStoredSeconds", 7200f);
            InvokePrivate(controller, "RefreshFastRewardsPopupUi");
            Canvas.ForceUpdateCanvases();

            if (!redeemButton.interactable)
            {
                throw new InvalidOperationException("Fast Rewards local redeem flow should start with an interactable Redeem button.");
            }

            redeemButton.onClick.Invoke();
            Canvas.ForceUpdateCanvases();

            if (!popup.activeInHierarchy)
            {
                throw new InvalidOperationException("Fast Rewards popup should remain open after a local redeem.");
            }

            if (GetPrivateField<float>(controller, "afkRewardStoredSeconds") > 0.01f)
            {
                throw new InvalidOperationException("Fast Rewards local redeem should reset stored AFK seconds to zero.");
            }

            if (GetPrivateField<int>(controller, "gold") <= goldBefore || GetPrivateField<int>(controller, "mythEssence") <= essenceBefore)
            {
                throw new InvalidOperationException("Fast Rewards local redeem should grant both Gold and Essence.");
            }

            RequireCopy(bodyText.text, "Stored: 0s / 24h 0m");
            RequireCopy(bodyText.text, "Cap left: 24h 0m");
            RequireCopy(bodyText.text, "Ready: +0 Gold   +0 Essence");
            AssertTextFits(bodyText, "Fast Rewards local redeemed body");

            if (redeemButton.interactable)
            {
                throw new InvalidOperationException("Fast Rewards local Redeem button should be disabled after rewards are claimed.");
            }
        }
        finally
        {
            SetPrivateField(controller, "backendGameplayEnabled", backendBefore);
            SetPrivateField(controller, "afkRewardStoredSeconds", storedBefore);
            SetPrivateField(controller, "gold", goldBefore);
            SetPrivateField(controller, "mythEssence", essenceBefore);
            SetPrivateField(controller, "lastOfflineGoldReward", lastGoldBefore);
            SetPrivateField(controller, "lastOfflineReward", lastEssenceBefore);
            SetPrivateField(controller, "lastOfflineSeconds", lastSecondsBefore);
            SetPrivateField(controller, "lastOfflineRewardIsServer", lastServerBefore);
            InvokePrivate(controller, "SaveProgress");
            InvokePrivate(controller, "RefreshFastRewardsPopupUi");
            Canvas.ForceUpdateCanvases();
        }
    }

    private static MythwakeDefinitionSnapshotDto CreateServerAfkDefinitions()
    {
        return new MythwakeDefinitionSnapshotDto
        {
            contentHash = "fast-rewards-validator",
            afkRewards = new[]
            {
                new MythwakeAfkRewardDefinitionDto
                {
                    afkRewardId = "afk_main",
                    rewardId = "reward_afk_main",
                    minClaimSeconds = 600,
                    maxClaimSeconds = 86400,
                    tickSeconds = 60,
                    baseMythEssencePerTick = 10,
                    mythEssencePerStage = 1,
                    goldPerMythEssenceDivisor = 1
                }
            }
        };
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

    private static void RequireInactive(string name, string context)
    {
        var gameObject = RequireObject(name, false);
        if (gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException($"{context} {name} is still active.");
        }
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field: {fieldName}");
        }

        field.SetValue(target, value);
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
}
