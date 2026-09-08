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
            var output = Path.GetFullPath("Builds/Validation/core-screens");
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
            Capture(controller, canvas, output, "heroes-team", () => { controller.ShowHeroes(); Call(controller, "ShowHeroesSetTeamTab"); });
            Capture(controller, canvas, output, "hero-detail", () => { controller.ShowHeroes(); Call(controller, "ShowHeroesRosterTab"); Call(controller, "ShowHeroDetail", 0); });
            Capture(controller, canvas, output, "hero-equipment", () => { Call(controller, "ShowHeroDetailGearSlot", 7); });
            foreach (var field in new[] { "heroDetailRoot", "heroDetailGearListRoot" })
            {
                var page = Field<RectTransform>(controller, field);
                var fill = page.Find("Core Inset Fill")?.GetComponent<Image>();
                if (fill == null || fill.color.a < .5f || !fill.raycastTarget || page.rect.width < 1000)
                    throw new InvalidOperationException(field + " must be a visible, input-blocking content page.");
            }
            var emptyGearState = Field<RectTransform>(controller, "heroDetailGearEmptyRoot");
            var emptyGearMessage = Field<TMPro.TMP_Text>(controller, "heroDetailGearEmptyMessageText");
            if (emptyGearState == null || !emptyGearState.gameObject.activeInHierarchy || emptyGearMessage == null || string.IsNullOrWhiteSpace(emptyGearMessage.text))
                throw new InvalidOperationException("Empty accessory gear state is missing or has no message.");
            Call(controller, "HideHeroDetailGearList");
            if (!Field<RectTransform>(controller, "heroDetailRoot").gameObject.activeSelf)
                throw new InvalidOperationException("Closing equipment must return to the hero.");
            Call(controller, "HideHeroDetail");
            if (!Field<RectTransform>(controller, "heroSubTabRoot").gameObject.activeSelf)
                throw new InvalidOperationException("Closing the hero must restore roster navigation.");
            Capture(controller, canvas, output, "village-build", () => { controller.ShowVillage(); Call(controller, "SelectVillagePlot", 0); });
            Capture(controller, canvas, output, "summon-result", () => { controller.ShowSummon(); Call(controller, "ShowSummonResultPopup", new[] { 1, 0, 0, 0, 0, 0, 0 }, 1); });
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
        var panel = Field<GameObject>(controller, name == "campaign" ? "homePanel" : name.StartsWith("hero") ? "heroesPanel" : name.StartsWith("village") ? "villagePanel" : name == "dungeons" ? "dungeonsPanel" : "summonPanel");
        var lines = new System.Collections.Generic.List<string>();
        foreach (var rect in panel.GetComponentsInChildren<RectTransform>(true))
            lines.Add(rect.name + " | active=" + rect.gameObject.activeSelf + " | parent=" + rect.parent.name + " | " + rect.anchoredPosition + " | " + rect.rect.size);
        File.WriteAllLines(Path.Combine(output, name + "-hierarchy.txt"), lines);
        typeof(PortraitScreenshotAutomation).GetMethod("CaptureCanvas", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { canvas, Path.Combine(output, name + ".png") });
        if (name == "campaign" || name.StartsWith("hero") || name == "village" || name == "dungeons" || name == "summon")
            CapturePhone(controller, canvas, Path.Combine(output, name + "-phone.png"));
        if (name == "hero-detail" || name == "hero-equipment")
        {
            CapturePhone(controller, canvas, Path.Combine(output, name + "-tall.png"), 540, 1170);
            CapturePhone(controller, canvas, Path.Combine(output, name + "-short.png"), 540, 864);
        }
        if (name == "heroes")
        {
            var cards = Field<Button[]>(controller, "heroSelectButtons");
            for (var i = 0; i < cards.Length; i++)
            {
                var rect = cards[i].GetComponent<RectTransform>();
                if (!cards[i].gameObject.activeInHierarchy || rect.rect.width < 280 || rect.rect.height < 360)
                    throw new InvalidOperationException("Roster card is hidden or too small: " + i);
                for (var j = i + 1; j < cards.Length; j++)
                    if (Bounds(rect).Overlaps(Bounds(cards[j].GetComponent<RectTransform>())))
                        throw new InvalidOperationException("Roster cards overlap: " + i + "/" + j);
            }
            var equipment = panel.transform.Find("Equipment Panel");
            if (equipment != null && equipment.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Legacy equipment panel is still visible.");
        }
    }

    private static Rect Bounds(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x + .1f, corners[0].y + .1f, corners[2].x - .1f, corners[2].y - .1f);
    }

    private static void CapturePhone(IdlePrototypeController controller, Canvas canvas, string path, int width = 540, int height = 960)
    {
        var oldCamera = canvas.worldCamera;
        var oldMode = canvas.renderMode;
        var oldScale = canvas.scaleFactor;
        var oldActive = RenderTexture.active;
        var cameraObject = new GameObject("Core UI Phone Preview Camera");
        var camera = cameraObject.AddComponent<Camera>();
        var target = new RenderTexture(width, height, 24);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.orthographic = true;
            camera.orthographicSize = height / 2f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10;
            canvas.scaleFactor = .5f;
            Canvas.ForceUpdateCanvases();
            Call(controller, "RefreshUi");
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = oldActive;
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.scaleFactor = oldScale;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Canvas.ForceUpdateCanvases();
        }
    }

    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Flags).Invoke(owner, args);
}
