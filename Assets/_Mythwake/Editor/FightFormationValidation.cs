using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FightFormationValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Mythwake/Validate Fight Formation UI")]
    public static void RunFightFormationValidation()
    {
        try
        {
            ValidateFightFormationUi();
            Debug.Log("Fight/Formation UI validated: formation swap, auto toggle, visible fight controls, x2, mana/ultimate cards, result popup, and dungeon focus are present.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateFightFormationUi()
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
        SetPrivateField(controller, "autoContinueFightsEnabled", false);
        SetPrivateField(controller, "fightAutoSkillsEnabled", false);
        SetPrivateField(controller, "fightDoubleSpeedEnabled", false);

        ValidateCampaignFormation(controller);
        ValidateCampaignFightControls(controller);
        ValidateFightResultFlow(controller);
        ValidateDungeonFightFocus(controller);
    }

    private static void ValidateCampaignFormation(IdlePrototypeController controller)
    {
        InvokePrivate(controller, "ShowFormationScreen");
        Canvas.ForceUpdateCanvases();

        var formationRoot = RequireObject("Campaign Formation Root", true);
        var header = RequireText(formationRoot, "Formation Header");
        var team = RequireText(formationRoot, "Formation Team Power");
        var enemy = RequireText(formationRoot, "Formation Enemy Text");
        var hint = RequireText(formationRoot, "Formation Hint");
        var confirm = RequireButton("Formation Confirm Button");
        var back = RequireButton("Formation Back Button");
        var autoToggle = RequireButton("Formation Auto Continue Toggle");
        var autoLabel = RequireText(autoToggle.gameObject, "Auto Continue Label");
        var autoMark = RequireText(autoToggle.gameObject, "Checkbox Mark");
        var enemyImage = RequireRawImageWithTexture("Formation Enemy");

        RequireCopy(header.text, "Formation", "Formation header");
        RequireCopy(team.text, "Power", "Formation team power");
        RequireCopy(enemy.text, "Damage", "Formation enemy text");
        RequireCopy(hint.text, "Confirm", "Formation hint");
        AssertTextFits(header, "Formation header");
        AssertTextFits(team, "Formation team power");
        AssertTextFits(enemy, "Formation enemy text");
        AssertTextFits(hint, "Formation hint");
        AssertTextFits(autoLabel, "Formation auto label");
        AssertMinimumSize(confirm.gameObject, 300f, 60f, "Formation confirm button");
        AssertMinimumSize(back.gameObject, 190f, 54f, "Formation back button");
        AssertMinimumSize(autoToggle.gameObject, 520f, 48f, "Formation auto toggle");
        AssertInsideParent(formationRoot, enemyImage.gameObject);
        if (!confirm.interactable || !back.interactable)
        {
            throw new InvalidOperationException("Formation confirm/back buttons should be interactable.");
        }

        var slotButtons = GetPrivateField<Button[]>(controller, "formationSlotButtons");
        var slotFrames = GetPrivateField<Image[]>(controller, "formationSlotFrames");
        var heroImages = GetPrivateField<RawImage[]>(controller, "formationHeroImages");
        var heroLabels = GetPrivateField<TMP_Text[]>(controller, "formationHeroTexts");
        var order = GetPrivateField<int[]>(controller, "formationSlotHeroIndices");
        RequireArray(slotButtons, 2, "formation slot buttons");
        RequireArray(slotFrames, 2, "formation slot frames");
        RequireArray(heroImages, 2, "formation hero images");
        RequireArray(heroLabels, 2, "formation hero labels");
        RequireArray(order, 2, "formation order");

        var firstHero = order[0];
        var secondHero = order[1];
        for (var i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null || slotFrames[i] == null || heroImages[i] == null || heroLabels[i] == null)
            {
                throw new InvalidOperationException($"Formation slot {i + 1} should have button, frame, hero art, and label.");
            }

            AssertInsideParent(formationRoot, slotButtons[i].gameObject);
            AssertTextFits(heroLabels[i], $"Formation hero label {i + 1}");
            if (heroImages[i].gameObject.activeSelf && heroImages[i].texture == null)
            {
                throw new InvalidOperationException($"Formation hero {i + 1} should have loaded art when RawImage fallback is visible.");
            }

            for (var otherIndex = i + 1; otherIndex < slotButtons.Length; otherIndex++)
            {
                AssertNoOverlap(slotButtons[i].gameObject, slotButtons[otherIndex].gameObject, 6f, "Formation mobile slot spacing");
            }
        }

        slotButtons[0].onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (GetPrivateField<int>(controller, "selectedFormationSlotIndex") != 0)
        {
            throw new InvalidOperationException("Tapping Formation slot 1 should select it for swap.");
        }

        slotButtons[1].onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        order = GetPrivateField<int[]>(controller, "formationSlotHeroIndices");
        if (order[0] != secondHero || order[1] != firstHero)
        {
            throw new InvalidOperationException("Tapping a second Formation slot should swap the two heroes.");
        }

        if (GetPrivateField<int>(controller, "selectedFormationSlotIndex") != -1)
        {
            throw new InvalidOperationException("Formation selection should clear after a successful swap.");
        }

        autoToggle.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (!GetPrivateField<bool>(controller, "autoContinueFightsEnabled") || autoMark.text != "X" || !autoLabel.text.Contains("AUTO"))
        {
            throw new InvalidOperationException("Formation auto toggle should enable auto-continue and show a checkbox mark.");
        }

        AssertTextFits(autoLabel, "Formation auto label enabled");
        autoToggle.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (GetPrivateField<bool>(controller, "autoContinueFightsEnabled") || !string.IsNullOrEmpty(autoMark.text))
        {
            throw new InvalidOperationException("Formation auto toggle should disable cleanly and clear the checkbox mark.");
        }
    }

    private static void ValidateCampaignFightControls(IdlePrototypeController controller)
    {
        SetPrivateEnumField(controller, "battleFlowMode", "Fight");
        InvokePrivate(controller, "ApplyBattleFlowVisibility");
        InvokePrivate(controller, "RefreshFightArenaBackground", false);
        InvokePrivate(controller, "PrepareFightAnimationTextures", 1, false, null);
        InvokePrivate(controller, "InitializeFightSkillState");
        Canvas.ForceUpdateCanvases();

        var fightRoot = RequireObject("Campaign Fight Root", true);
        var vs = RequireText(fightRoot, "Fight VS Text");
        var timer = RequireText(fightRoot, "Fight Timer Text");
        var status = RequireText(fightRoot, "Fight Status Text");
        var end = RequireButton("Fight End Button");
        var auto = RequireButton("Fight Auto Skill Button");
        var speed = RequireButton("Fight Speed Button");
        var autoText = GetButtonLabel(auto);
        var speedText = GetButtonLabel(speed);

        RequireCopy(vs.text, "VS", "Fight VS text");
        RequireCopy(timer.text, "00:", "Fight timer");
        RequireCopy(status.text, "Ready", "Fight status");
        AssertTextFits(vs, "Fight VS text");
        AssertTextFits(timer, "Fight timer");
        AssertTextFits(status, "Fight status");
        AssertMinimumSize(end.gameObject, 120f, 52f, "Fight End button");
        AssertMinimumSize(auto.gameObject, 88f, 52f, "Fight AUTO button");
        AssertMinimumSize(speed.gameObject, 76f, 52f, "Fight x2 button");
        RequireCopy(autoText, "AUTO", "Fight AUTO button");
        RequireCopy(speedText, "x2", "Fight x2 button");

        ValidateFightUnitArt(controller, fightRoot);
        ValidateFightSkillCards(controller, fightRoot);
        ValidateFightToggles(controller, auto, speed);
        ValidateFightUltimateQueue(controller);
    }

    private static void ValidateFightUnitArt(IdlePrototypeController controller, GameObject fightRoot)
    {
        var heroImages = GetPrivateField<RawImage[]>(controller, "fightHeroImages");
        var enemyImages = GetPrivateField<RawImage[]>(controller, "fightEnemyImages");
        var heroHpFills = GetPrivateField<Image[]>(controller, "fightHeroHpFills");
        var enemyHpFills = GetPrivateField<Image[]>(controller, "fightEnemyHpFills");
        RequireArray(heroImages, 1, "fight hero images");
        RequireArray(enemyImages, 1, "fight enemy images");
        RequireArray(heroHpFills, 1, "fight hero HP fills");
        RequireArray(enemyHpFills, 1, "fight enemy HP fills");

        for (var i = 0; i < heroImages.Length; i++)
        {
            if (heroImages[i] == null || enemyImages[i] == null || heroHpFills[i] == null || enemyHpFills[i] == null)
            {
                throw new InvalidOperationException($"Fight lane {i + 1} should have hero/enemy art and HP bars.");
            }

            AssertInsideParent(fightRoot, heroImages[i].gameObject);
            AssertInsideParent(fightRoot, enemyImages[i].gameObject);
            if (heroImages[i].gameObject.activeSelf && heroImages[i].texture == null)
            {
                throw new InvalidOperationException($"Fight hero {i + 1} should have loaded fallback art when visible.");
            }

            if (enemyImages[i].gameObject.activeSelf && enemyImages[i].texture == null)
            {
                throw new InvalidOperationException($"Fight enemy {i + 1} should have loaded art.");
            }
        }
    }

    private static void ValidateFightSkillCards(IdlePrototypeController controller, GameObject fightRoot)
    {
        var skillButtons = GetPrivateField<Button[]>(controller, "fightSkillButtons");
        var manaFills = GetPrivateField<Image[]>(controller, "fightSkillManaFills");
        var manaTexts = GetPrivateField<TMP_Text[]>(controller, "fightSkillManaTexts");
        var hpFills = GetPrivateField<Image[]>(controller, "fightSkillHpFills");
        var nameTexts = GetPrivateField<TMP_Text[]>(controller, "fightSkillNameTexts");
        var portraits = GetPrivateField<RawImage[]>(controller, "fightSkillPortraits");
        RequireArray(skillButtons, 1, "fight skill buttons");
        RequireArray(manaFills, skillButtons.Length, "fight mana fills");
        RequireArray(manaTexts, skillButtons.Length, "fight mana texts");
        RequireArray(hpFills, skillButtons.Length, "fight skill HP fills");
        RequireArray(nameTexts, skillButtons.Length, "fight skill name texts");
        RequireArray(portraits, skillButtons.Length, "fight skill portraits");

        for (var i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] == null || manaFills[i] == null || manaTexts[i] == null || hpFills[i] == null || nameTexts[i] == null || portraits[i] == null)
            {
                throw new InvalidOperationException($"Fight skill card {i + 1} should have button, portrait, HP, mana, and label.");
            }

            AssertInsideParent(fightRoot, skillButtons[i].gameObject);
            AssertMinimumSize(skillButtons[i].gameObject, 120f, 170f, $"Fight skill card {i + 1}");
            AssertTextFits(nameTexts[i], $"Fight skill card {i + 1} name");
            AssertTextFits(manaTexts[i], $"Fight skill card {i + 1} mana");
            AssertFillPercentBetween(manaFills[i], 0f, 1f, $"Fight skill card {i + 1} mana");
            AssertFillPercentBetween(hpFills[i], 0f, 1f, $"Fight skill card {i + 1} HP");
            if (portraits[i].texture == null)
            {
                throw new InvalidOperationException($"Fight skill card {i + 1} should have portrait art.");
            }
        }
    }

    private static void ValidateFightToggles(IdlePrototypeController controller, Button auto, Button speed)
    {
        auto.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (!GetPrivateField<bool>(controller, "fightAutoSkillsEnabled") || !GetButtonLabel(auto).Contains("ON"))
        {
            throw new InvalidOperationException("Fight AUTO button should enable skill auto-cast and show ON state.");
        }

        speed.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (!GetPrivateField<bool>(controller, "fightDoubleSpeedEnabled") || !GetButtonLabel(speed).Contains("ON"))
        {
            throw new InvalidOperationException("Fight x2 button should enable double-speed and show ON state.");
        }

        auto.onClick.Invoke();
        speed.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (GetPrivateField<bool>(controller, "fightAutoSkillsEnabled") || GetPrivateField<bool>(controller, "fightDoubleSpeedEnabled"))
        {
            throw new InvalidOperationException("Fight AUTO/x2 buttons should toggle off cleanly.");
        }
    }

    private static void ValidateFightUltimateQueue(IdlePrototypeController controller)
    {
        var manaValues = GetPrivateField<int[]>(controller, "fightHeroManaValues");
        var maxManaValues = GetPrivateField<int[]>(controller, "fightHeroMaxManaValues");
        var queued = GetPrivateField<bool[]>(controller, "fightHeroUltimateQueued");
        var skillButtons = GetPrivateField<Button[]>(controller, "fightSkillButtons");
        var manaTexts = GetPrivateField<TMP_Text[]>(controller, "fightSkillManaTexts");
        RequireArray(manaValues, 1, "fight mana values");
        RequireArray(maxManaValues, 1, "fight max mana values");
        RequireArray(queued, 1, "fight ultimate queued flags");
        RequireArray(skillButtons, 1, "fight skill buttons");
        RequireArray(manaTexts, 1, "fight mana texts");

        manaValues[0] = Mathf.Max(1, maxManaValues[0]);
        queued[0] = false;
        InvokePrivate(controller, "RefreshFightSkillUi", 0f);
        Canvas.ForceUpdateCanvases();

        RequireCopy(manaTexts[0].text, $"{manaValues[0]}/{maxManaValues[0]}", "Fight ready mana text");
        skillButtons[0].onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (!queued[0])
        {
            throw new InvalidOperationException("Clicking a ready Fight skill card should queue that hero's ultimate.");
        }
    }

    private static void ValidateFightResultFlow(IdlePrototypeController controller)
    {
        SetPrivateField(controller, "autoContinueFightsEnabled", false);
        InvokePrivate(controller, "ShowCampaignFightResult", false, "Defeat", "HP 0/100  Enemy HP 40/100\nATK 10  Enemy DMG 5  Dealt 60  Took 100");
        Canvas.ForceUpdateCanvases();

        var fightRoot = RequireObject("Campaign Fight Root", true);
        var resultRoot = RequireObject("Campaign Fight Result Popup", true);
        var title = RequireText(resultRoot, "Title");
        var body = RequireText(resultRoot, "Result Body");
        var continueButton = RequireButton("Fight Continue Button");
        RequireCopy(title.text, "Defeat", "Fight result title");
        RequireCopy(body.text, "Enemy HP", "Fight result body");
        RequireCopy(body.text, "Enemy DMG", "Fight result body");
        AssertInsideParent(fightRoot, resultRoot);
        AssertTextFits(title, "Fight result title");
        AssertTextFits(body, "Fight result body");
        if (!continueButton.interactable)
        {
            throw new InvalidOperationException("Fight continue button should be interactable on result popup.");
        }

        continueButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        if (resultRoot.activeInHierarchy)
        {
            throw new InvalidOperationException("Fight result popup should close after Continue returns to the previous screen.");
        }
    }

    private static void ValidateDungeonFightFocus(IdlePrototypeController controller)
    {
        InvokePrivate(controller, "ShowDungeonFormation", "gold_dungeon");
        Canvas.ForceUpdateCanvases();

        var formationRoot = RequireObject("Campaign Formation Root", true);
        var header = RequireText(formationRoot, "Formation Header");
        var topBar = RequireObject("Mythwake Top Resource Bar", false);
        var bottomNav = RequireObject("Mythwake Art Bottom Navbar", false);
        RequireCopy(header.text, "Gold", "Dungeon formation header");
        if (topBar.activeInHierarchy || bottomNav.activeInHierarchy)
        {
            throw new InvalidOperationException("Dungeon fight focus should hide top and bottom navigation chrome while Formation is active.");
        }

        var back = RequireButton("Formation Back Button");
        back.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        RequireObject("Dungeons Panel", true);
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        foreach (var component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component != null && component.gameObject.scene.IsValid())
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
        var transform = FindChild(parent.transform, name);
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

    private static Transform FindChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            var match = FindChild(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static GameObject FindSceneObject(string name)
    {
        foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform != null && transform.name == name && transform.gameObject.scene.IsValid())
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static string GetButtonLabel(Button button)
    {
        var label = button == null ? null : button.GetComponentInChildren<TMP_Text>(true);
        return label == null ? string.Empty : label.text;
    }

    private static void RequireArray(Array array, int minLength, string label)
    {
        if (array == null || array.Length < minLength)
        {
            throw new InvalidOperationException($"Missing or too-short {label}: length={(array == null ? 0 : array.Length)}, expected at least {minLength}.");
        }
    }

    private static void RequireCopy(string text, string expected, string context)
    {
        if (string.IsNullOrWhiteSpace(text) || text.IndexOf(expected, StringComparison.Ordinal) < 0)
        {
            throw new InvalidOperationException($"{context} is missing '{expected}': '{text}'");
        }
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

    private static void AssertFillPercentBetween(Image fill, float min, float max, string context)
    {
        if (fill == null)
        {
            throw new InvalidOperationException($"{context} fill is missing.");
        }

        var percent = fill.rectTransform.anchorMax.x;
        if (percent < min - 0.001f || percent > max + 0.001f)
        {
            throw new InvalidOperationException($"{context} fill percent should be between {min} and {max}, got {percent}.");
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

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private field {fieldName}.");
        }

        return (T)field.GetValue(target);
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

    private static void SetPrivateEnumField(object target, string fieldName, string enumValueName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Missing private enum field {fieldName}.");
        }

        field.SetValue(target, Enum.Parse(field.FieldType, enumValueName));
    }
}
