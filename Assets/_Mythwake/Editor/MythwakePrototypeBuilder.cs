using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MythwakePrototypeBuilder
{
    private static readonly Color BackgroundColor = new Color(0.07f, 0.1f, 0.16f);
    private static readonly Color PanelColor = new Color(0.12f, 0.16f, 0.24f, 0.95f);
    private static readonly Color ButtonColor = new Color(0.17f, 0.39f, 0.72f);
    private static readonly Color UpgradeButtonColor = new Color(0.19f, 0.5f, 0.31f);
    private static readonly Color ResetButtonColor = new Color(0.35f, 0.17f, 0.22f);

    [MenuItem("Tools/Mythwake/Build Prototype UI")]
    public static void BuildPrototypeUi()
    {
        EnsureEventSystem();

        var oldRoot = GameObject.Find("Prototype UI");
        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot);
        }

        var oldController = GameObject.Find("Idle Prototype Controller");
        if (oldController != null)
        {
            Object.DestroyImmediate(oldController);
        }

        var root = new GameObject("Prototype UI");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 1f;

        root.AddComponent<GraphicRaycaster>();

        var background = CreatePanel("Background", root.transform, BackgroundColor);
        Stretch(background.rectTransform);

        var title = CreateText("Title", root.transform, "Mythwake", 72, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0, -170), new Vector2(920, 120), new Vector2(0.5f, 1f));

        var gold = CreateText("Gold Text", root.transform, "Gold: 0", 46, FontStyles.Bold);
        SetRect(gold.rectTransform, new Vector2(0, -300), new Vector2(920, 70), new Vector2(0.5f, 1f));

        var damage = CreateText("Damage Text", root.transform, "Damage: 1", 36, FontStyles.Normal);
        SetRect(damage.rectTransform, new Vector2(0, -370), new Vector2(920, 60), new Vector2(0.5f, 1f));

        var autoAttack = CreateText("Auto Attack Text", root.transform, "Auto Attack: 1.0s", 30, FontStyles.Normal);
        SetRect(autoAttack.rectTransform, new Vector2(0, -425), new Vector2(920, 46), new Vector2(0.5f, 1f));
        autoAttack.color = new Color(0.72f, 0.86f, 1f);

        var offlineReward = CreateText("Offline Reward Text", root.transform, "Offline: no reward yet", 28, FontStyles.Normal);
        SetRect(offlineReward.rectTransform, new Vector2(0, -475), new Vector2(920, 46), new Vector2(0.5f, 1f));
        offlineReward.color = new Color(0.82f, 0.76f, 0.52f);

        var enemyPanel = CreatePanel("Enemy Panel", root.transform, PanelColor);
        SetRect(enemyPanel.rectTransform, new Vector2(0, -680), new Vector2(860, 300), new Vector2(0.5f, 1f));

        var enemyName = CreateText("Enemy Text", enemyPanel.transform, "Enemy Lv. 1", 46, FontStyles.Bold);
        SetRect(enemyName.rectTransform, new Vector2(0, -70), new Vector2(780, 70), new Vector2(0.5f, 1f));

        var enemyHp = CreateText("Enemy HP Text", enemyPanel.transform, "HP: 10 / 10", 40, FontStyles.Normal);
        SetRect(enemyHp.rectTransform, new Vector2(0, -160), new Vector2(780, 70), new Vector2(0.5f, 1f));

        var fightButton = CreateButton("Fight Button", root.transform, "Fight", ButtonColor);
        SetRect(fightButton.GetComponent<RectTransform>(), new Vector2(0, -1050), new Vector2(760, 150), new Vector2(0.5f, 1f));

        var upgradeButton = CreateButton("Upgrade Button", root.transform, "Upgrade Damage (10 Gold)", UpgradeButtonColor);
        SetRect(upgradeButton.GetComponent<RectTransform>(), new Vector2(0, -1240), new Vector2(760, 135), new Vector2(0.5f, 1f));

        var resetButton = CreateButton("Reset Button", root.transform, "Reset Prototype", ResetButtonColor);
        SetRect(resetButton.GetComponent<RectTransform>(), new Vector2(0, -1400), new Vector2(560, 90), new Vector2(0.5f, 1f));
        resetButton.GetComponentInChildren<TMP_Text>().fontSize = 28;

        var hint = CreateText("Hint Text", root.transform, "Close and reopen later to claim Offline Gold.", 28, FontStyles.Normal);
        SetRect(hint.rectTransform, new Vector2(0, -1535), new Vector2(880, 90), new Vector2(0.5f, 1f));
        hint.color = new Color(0.72f, 0.78f, 0.88f);

        var controllerObject = new GameObject("Idle Prototype Controller");
        var controller = controllerObject.AddComponent<IdlePrototypeController>();
        var serializedController = new SerializedObject(controller);
        SetObject(serializedController, "titleText", title);
        SetObject(serializedController, "goldText", gold);
        SetObject(serializedController, "damageText", damage);
        SetObject(serializedController, "enemyText", enemyName);
        SetObject(serializedController, "enemyHpText", enemyHp);
        SetObject(serializedController, "autoAttackText", autoAttack);
        SetObject(serializedController, "offlineRewardText", offlineReward);
        SetObject(serializedController, "upgradeCostText", upgradeButton.GetComponentInChildren<TMP_Text>());
        SetObject(serializedController, "fightButton", fightButton);
        SetObject(serializedController, "upgradeButton", upgradeButton);
        SetObject(serializedController, "resetButton", resetButton);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Selection.activeGameObject = root;
        Debug.Log("Mythwake prototype UI was created.");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Image CreatePanel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        var image = panel.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    [MenuItem("GameObject/Mythwake/Build Prototype UI", false, 10)]
    private static void BuildPrototypeUiFromGameObjectMenu()
    {
        BuildPrototypeUi();
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var image = buttonObject.GetComponent<Image>();
        image.color = color;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        var buttonLabel = CreateText("Label", buttonObject.transform, label, 36, FontStyles.Bold);
        Stretch(buttonLabel.rectTransform, new Vector2(42, 20));

        return button;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 padding)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = padding;
        rectTransform.offsetMax = -padding;
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
    }
}
