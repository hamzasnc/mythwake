using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CoreScreenPresentationValidation
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Mythwake/Validate Core Screen Presentation")]
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var controller = UnityEngine.Object.FindAnyObjectByType<IdlePrototypeController>();
            Call(controller, "EnsureRuntimeDebugUi");
            Call(controller, "EnsureRuntimeBackendUi");
            Call(controller, "EnsureRuntimeScreenLayout");
            Call(controller, "RegisterNavigation");
            var canvas = Field<RectTransform>(controller, "topBarRoot").GetComponentInParent<Canvas>();
            var output = Path.GetFullPath("docs/screenshots/core-screens");
            Directory.CreateDirectory(output);

            ValidateButtonSprite(controller, "heroSortToggleButton", "ui_action_button");
            ValidateButtonSprite(controller, "heroRosterTabButton", "ui_action_button");
            ValidateButtonSprite(controller, "summonButton", "ui_action_button");
            ValidateButtonSprite(controller, "dungeonDetailRunButton", "ui_action_button");
            ValidateCardSprites(controller);
            ValidateMapNodeSprites(controller, "campaignStageButtons");
            ValidateMapNodeSprites(controller, "villagePlotButtons");

            Capture(controller, canvas, output, "campaign", controller.ShowHome);
            Capture(controller, canvas, output, "heroes", controller.ShowHeroes);
            Capture(controller, canvas, output, "village", controller.ShowVillage);
            Capture(controller, canvas, output, "dungeons", controller.ShowDungeons);
            Capture(controller, canvas, output, "summon", controller.ShowSummon);
            Debug.Log("CORE_SCREEN_PRESENTATION_VALIDATED: panels, cards, map nodes and buttons are live. Screenshots: " + output);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateButtonSprite(object controller, string fieldName, string expectedName)
    {
        var button = Field<Button>(controller, fieldName);
        var image = button != null ? button.GetComponent<Image>() : null;
        if (image == null || !image.enabled || image.sprite == null || image.sprite.name != expectedName || image.type != Image.Type.Simple)
            throw new InvalidOperationException(fieldName + " does not use the Mythwake button artwork.");
    }

    private static void ValidateCardSprites(object controller)
    {
        var cards = Field<Button[]>(controller, "heroSelectButtons");
        if (cards == null || cards.Length == 0)
            throw new InvalidOperationException("Hero cards are missing.");
        foreach (var card in cards)
        {
            var image = card != null ? card.GetComponent<Image>() : null;
            if (image == null || image.sprite == null || image.sprite.name != "ui_hero_card_frame")
                throw new InvalidOperationException("A hero card does not use the new portrait frame.");
        }
    }

    private static void ValidateMapNodeSprites(object controller, string fieldName)
    {
        var nodes = Field<Button[]>(controller, fieldName);
        if (nodes == null || nodes.Length == 0)
            throw new InvalidOperationException(fieldName + " are missing.");
        foreach (var node in nodes)
        {
            var image = node != null ? node.GetComponent<Image>() : null;
            if (image == null || !image.enabled || image.sprite == null || image.sprite.name != "ui_map_node")
                throw new InvalidOperationException(fieldName + " do not use the new map node artwork.");
        }
    }

    private static void Capture(IdlePrototypeController controller, Canvas canvas, string output, string name, Action show)
    {
        show();
        Call(controller, "RefreshUi");
        Canvas.ForceUpdateCanvases();
        typeof(PortraitScreenshotAutomation).GetMethod("CaptureCanvas", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { canvas, Path.Combine(output, name + ".png") });
    }

    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Flags).Invoke(owner, args);
}
