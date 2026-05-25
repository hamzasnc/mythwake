using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HomeIdleCombatValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Mythwake/Validate Home Idle Combat")]
    public static void RunHomeIdleCombatValidation()
    {
        try
        {
            ValidateHomeIdleCombatUi();
            Debug.Log("Home Idle Combat validated: campaign map, clickable stage preview, foreground patrol fight, active loot tick, and no automatic stage clear are present.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateHomeIdleCombatUi()
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
        controller.ShowHome();
        Canvas.ForceUpdateCanvases();

        var mapRoot = RequireObject("Home Campaign Map Root", true);
        var scrollRect = mapRoot.GetComponent<ScrollRect>();
        if (scrollRect == null || scrollRect.content == null || !scrollRect.vertical || scrollRect.horizontal)
        {
            throw new InvalidOperationException("Home Campaign Map Root should be a vertical-only ScrollRect with content.");
        }

        var mapContent = RequireObject("Campaign Map Content", true);
        var mapImage = RequireRawImageWithTexture("Campaign World Map Image");
        if (!mapImage.texture.name.Contains("area_map_scorched_plains"))
        {
            throw new InvalidOperationException($"Campaign map should use area_map_scorched_plains, got '{mapImage.texture.name}'.");
        }

        var preview = RequireObject("Campaign Stage Preview", true);
        var previewText = RequireText(preview, "Campaign Stage Preview Text");
        RequireCopy(previewText.text, "Abschnitt");
        RequireCopy(previewText.text, "Idle sammelt nur kleine Beute");
        AssertTextFits(previewText, "Campaign Stage Preview Text");

        var node = RequireButton("Campaign Stage Node 3");
        AssertInsideParent(mapContent, node.gameObject);
        node.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        RequireCopy(previewText.text, "Abschnitt");

        var idleRoot = RequireObject("Home Idle Combat Root", true);
        AssertInsideParent(RequireObject("Home Generated Art Root", true), idleRoot);
        AssertDoesNotOverlap(mapRoot, idleRoot);
        AssertSameWidth(mapRoot, idleRoot);
        AssertConnectedBelow(mapRoot, idleRoot);
        var battleButton = RequireButton("Home Battle Button").gameObject;
        AssertExtendsBehind(mapRoot, battleButton);
        AssertExtendsBelow(idleRoot, battleButton);
        var idleMap = RequireRawImageWithTexture("Home Idle Mini Map Background");
        if (!idleMap.texture.name.Contains("area_map_"))
        {
            throw new InvalidOperationException($"Home Idle Mini Map Background should use an area map texture, got '{idleMap.texture.name}'.");
        }

        AssertInsideParent(idleRoot, idleMap.gameObject);
        var idleText = RequireText(idleRoot, "Home Idle Combat Text");
        var rewardText = RequireText(idleRoot, "Home Idle Reward Text");
        RequireCopy(idleText.text, "Patrol");
        RequireCopy(rewardText.text, "Naechste");
        AssertTextFits(idleText, "Home Idle Combat Text");
        AssertTextFits(rewardText, "Home Idle Reward Text");

        for (var i = 1; i <= 3; i++)
        {
            RequireRawImageWithTexture($"Home Idle Hero {i}");
            RequireRawImageWithTexture($"Home Idle Enemy {i}");
        }

        SetPrivateField(controller, "homeIdleRewardTimer", 0f);
        var stageBefore = GetPrivateField<int>(controller, "enemyLevel");
        var goldBefore = GetPrivateField<int>(controller, "gold");
        var mythEssenceBefore = GetPrivateField<int>(controller, "mythEssence");
        try
        {
            InvokePrivate(controller, "TickHomeIdleCombat", 10.1f);
            Canvas.ForceUpdateCanvases();

            var stageAfter = GetPrivateField<int>(controller, "enemyLevel");
            var goldAfter = GetPrivateField<int>(controller, "gold");
            if (stageAfter != stageBefore)
            {
                throw new InvalidOperationException($"Home idle combat must not auto-clear stages. Before={stageBefore}, after={stageAfter}.");
            }

            if (goldAfter <= goldBefore)
            {
                throw new InvalidOperationException($"Home idle combat should grant a small local gold reward. Before={goldBefore}, after={goldAfter}.");
            }
        }
        finally
        {
            SetPrivateField(controller, "enemyLevel", stageBefore);
            SetPrivateField(controller, "gold", goldBefore);
            SetPrivateField(controller, "mythEssence", mythEssenceBefore);
            SetPrivateField(controller, "homeIdleRewardTimer", 0f);
            SetPrivateField(controller, "homeIdleLastRewardGold", 0);
            SetPrivateField(controller, "homeIdleLastRewardEssence", 0);
            InvokePrivate(controller, "SaveProgress");
        }
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        foreach (var component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component.gameObject.scene.IsValid())
            {
                return component;
            }
        }

        return null;
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
            throw new InvalidOperationException($"{name} is missing a Button.");
        }

        return button;
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

    private static RawImage RequireRawImageWithTexture(string name)
    {
        var gameObject = RequireObject(name, true);
        var image = gameObject.GetComponent<RawImage>();
        if (image == null)
        {
            throw new InvalidOperationException($"{name} is missing a RawImage.");
        }

        if (image.texture == null)
        {
            throw new InvalidOperationException($"{name} should have loaded texture art.");
        }

        return image;
    }

    private static GameObject FindSceneObject(string name)
    {
        foreach (var gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.name == name && gameObject.scene.IsValid())
            {
                return gameObject;
            }
        }

        return null;
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

    private static void AssertDoesNotOverlap(GameObject first, GameObject second)
    {
        var firstRect = first.GetComponent<RectTransform>();
        var secondRect = second.GetComponent<RectTransform>();
        if (firstRect == null || secondRect == null)
        {
            throw new InvalidOperationException($"{first.name} or {second.name} is missing a RectTransform.");
        }

        var firstBounds = GetAnchoredBounds(firstRect);
        var secondBounds = GetAnchoredBounds(secondRect);
        var overlaps = firstBounds.left < secondBounds.right &&
            firstBounds.right > secondBounds.left &&
            firstBounds.bottom < secondBounds.top &&
            firstBounds.top > secondBounds.bottom;

        if (overlaps)
        {
            throw new InvalidOperationException($"{first.name} should not overlap {second.name}.");
        }
    }

    private static void AssertExtendsBehind(GameObject background, GameObject foreground)
    {
        var backgroundRect = background.GetComponent<RectTransform>();
        var foregroundRect = foreground.GetComponent<RectTransform>();
        if (backgroundRect == null || foregroundRect == null)
        {
            throw new InvalidOperationException($"{background.name} or {foreground.name} is missing a RectTransform.");
        }

        var backgroundBounds = GetAnchoredBounds(backgroundRect);
        var foregroundBounds = GetAnchoredBounds(foregroundRect);
        if (backgroundBounds.top >= foregroundBounds.top && backgroundBounds.bottom <= foregroundBounds.bottom)
        {
            return;
        }

        throw new InvalidOperationException($"{background.name} should extend behind {foreground.name}: background top={backgroundBounds.top}, bottom={backgroundBounds.bottom}; foreground top={foregroundBounds.top}, bottom={foregroundBounds.bottom}.");
    }

    private static void AssertConnectedBelow(GameObject upper, GameObject lower)
    {
        var upperRect = upper.GetComponent<RectTransform>();
        var lowerRect = lower.GetComponent<RectTransform>();
        if (upperRect == null || lowerRect == null)
        {
            throw new InvalidOperationException($"{upper.name} or {lower.name} is missing a RectTransform.");
        }

        var upperBounds = GetAnchoredBounds(upperRect);
        var lowerBounds = GetAnchoredBounds(lowerRect);
        if (Mathf.Abs(lowerBounds.top - upperBounds.bottom) <= 2f)
        {
            return;
        }

        throw new InvalidOperationException($"{lower.name} should connect to {upper.name}: lower top={lowerBounds.top}, upper bottom={upperBounds.bottom}.");
    }

    private static void AssertSameWidth(GameObject first, GameObject second)
    {
        var firstRect = first.GetComponent<RectTransform>();
        var secondRect = second.GetComponent<RectTransform>();
        if (firstRect == null || secondRect == null)
        {
            throw new InvalidOperationException($"{first.name} or {second.name} is missing a RectTransform.");
        }

        if (Mathf.Abs(firstRect.rect.width - secondRect.rect.width) <= 2f)
        {
            return;
        }

        throw new InvalidOperationException($"{second.name} should match {first.name} width: first={firstRect.rect.width}, second={secondRect.rect.width}.");
    }

    private static void AssertExtendsBelow(GameObject background, GameObject foreground)
    {
        var backgroundRect = background.GetComponent<RectTransform>();
        var foregroundRect = foreground.GetComponent<RectTransform>();
        if (backgroundRect == null || foregroundRect == null)
        {
            throw new InvalidOperationException($"{background.name} or {foreground.name} is missing a RectTransform.");
        }

        var backgroundBounds = GetAnchoredBounds(backgroundRect);
        var foregroundBounds = GetAnchoredBounds(foregroundRect);
        if (backgroundBounds.bottom < foregroundBounds.bottom)
        {
            return;
        }

        throw new InvalidOperationException($"{background.name} should extend below {foreground.name}: background bottom={backgroundBounds.bottom}, foreground bottom={foregroundBounds.bottom}.");
    }

    private static (float left, float right, float top, float bottom) GetAnchoredBounds(RectTransform rectTransform)
    {
        var rect = rectTransform.rect;
        var left = rectTransform.anchoredPosition.x - rect.width * rectTransform.pivot.x;
        var right = left + rect.width;
        var top = rectTransform.anchoredPosition.y + rect.height * (1f - rectTransform.pivot.y);
        var bottom = top - rect.height;
        return (left, right, top, bottom);
    }

    private static void AssertTextFits(TMP_Text text, string label)
    {
        var rect = text.rectTransform.rect;
        var preferred = text.GetPreferredValues(text.text, rect.width, 0f);
        if (preferred.y > rect.height + 4f)
        {
            throw new InvalidOperationException($"{label} does not fit: preferred height={preferred.y}, rect height={rect.height}, text='{text.text}'.");
        }
    }

    private static void RequireCopy(string text, string expected)
    {
        if (string.IsNullOrWhiteSpace(text) || text.IndexOf(expected, StringComparison.Ordinal) < 0)
        {
            throw new InvalidOperationException($"Expected copy '{expected}' in '{text}'.");
        }
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Missing private method {methodName}.");
        }

        return method.Invoke(target, args);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field {fieldName}.");
        }

        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field {fieldName}.");
        }

        return (T)field.GetValue(target);
    }
}
