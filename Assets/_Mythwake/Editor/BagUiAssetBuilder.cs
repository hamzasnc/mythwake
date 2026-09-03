using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BagUiAssetBuilder
{
    private const string SpriteDirectory = "Assets/_Mythwake/Resources/Mythwake/UI/Bag";
    private const string PrefabDirectory = "Assets/_Mythwake/Prefabs/Bag";

    [MenuItem("Mythwake/Build Bag UI Assets")]
    public static void BuildBagUiAssets()
    {
        Directory.CreateDirectory(SpriteDirectory);
        Directory.CreateDirectory(PrefabDirectory);

        CreatePanelSprite("bag_panel_frame", 192, 192, new Color32(67, 32, 13, 255), new Color32(177, 116, 39, 255), new Color32(39, 18, 8, 255), new Color32(236, 185, 86, 255), 34, true);
        CreatePanelSprite("bag_parchment", 160, 160, new Color32(206, 159, 89, 255), new Color32(118, 70, 29, 255), new Color32(78, 42, 17, 255), new Color32(245, 205, 127, 255), 22, true);
        CreatePanelSprite("bag_title_plaque", 160, 72, new Color32(58, 29, 12, 255), new Color32(158, 96, 34, 255), new Color32(35, 16, 7, 255), new Color32(230, 178, 79, 255), 18, false);
        CreatePanelSprite("bag_header_ornament", 160, 32, new Color32(35, 17, 7, 0), new Color32(158, 96, 34, 255), new Color32(35, 16, 7, 255), new Color32(230, 178, 79, 255), 8, false, true);
        CreatePanelSprite("bag_tab_button", 96, 56, new Color32(56, 31, 13, 255), new Color32(111, 67, 26, 255), new Color32(29, 15, 7, 255), new Color32(182, 126, 48, 255), 14, false);
        CreatePanelSprite("bag_tab_button_active", 96, 56, new Color32(144, 76, 22, 255), new Color32(230, 157, 54, 255), new Color32(72, 32, 11, 255), new Color32(255, 217, 107, 255), 14, false);
        CreatePanelSprite("bag_tab_button_pressed", 96, 56, new Color32(41, 23, 10, 255), new Color32(154, 96, 35, 255), new Color32(18, 10, 6, 255), new Color32(226, 160, 62, 255), 14, false);
        CreatePanelSprite("bag_slot_frame", 128, 112, new Color32(21, 17, 14, 255), new Color32(112, 72, 30, 255), new Color32(9, 8, 7, 255), new Color32(190, 128, 48, 255), 16, false);
        CreatePanelSprite("bag_slot_empty", 128, 112, new Color32(18, 15, 12, 210), new Color32(76, 50, 22, 220), new Color32(7, 6, 5, 230), new Color32(128, 87, 37, 220), 16, false);
        CreatePanelSprite("bag_slot_well", 96, 72, new Color32(13, 11, 9, 235), new Color32(46, 31, 17, 255), new Color32(5, 4, 3, 255), new Color32(90, 61, 30, 255), 10, false);
        CreatePanelSprite("bag_slot_selected", 128, 112, new Color32(0, 0, 0, 0), new Color32(20, 220, 235, 255), new Color32(2, 76, 90, 255), new Color32(118, 255, 250, 255), 16, false, true);
        CreatePanelSprite("bag_icon_frame", 112, 112, new Color32(21, 18, 14, 255), new Color32(18, 176, 190, 255), new Color32(6, 66, 76, 255), new Color32(104, 242, 238, 255), 16, false);
        CreatePanelSprite("bag_icon_well", 96, 78, new Color32(38, 21, 10, 235), new Color32(78, 45, 18, 255), new Color32(12, 8, 5, 255), new Color32(142, 88, 35, 255), 10, true);
        CreatePanelSprite("bag_detail_panel", 160, 104, new Color32(216, 166, 91, 255), new Color32(118, 70, 29, 255), new Color32(70, 38, 16, 255), new Color32(244, 202, 116, 255), 20, true);
        CreatePanelSprite("bag_use_panel", 160, 104, new Color32(196, 142, 73, 255), new Color32(102, 57, 24, 255), new Color32(56, 29, 12, 255), new Color32(221, 164, 72, 255), 20, true);
        CreatePanelSprite("bag_button_blue", 160, 64, new Color32(8, 118, 139, 255), new Color32(29, 214, 232, 255), new Color32(4, 58, 68, 255), new Color32(142, 255, 248, 255), 18, false);
        CreatePanelSprite("bag_button_brown", 96, 48, new Color32(92, 54, 21, 255), new Color32(166, 103, 36, 255), new Color32(45, 24, 10, 255), new Color32(232, 177, 77, 255), 12, false);
        CreatePanelSprite("bag_button_small", 96, 48, new Color32(92, 54, 21, 255), new Color32(166, 103, 36, 255), new Color32(45, 24, 10, 255), new Color32(232, 177, 77, 255), 12, false);
        CreatePanelSprite("bag_button_small_pressed", 96, 48, new Color32(58, 33, 14, 255), new Color32(139, 84, 29, 255), new Color32(25, 14, 7, 255), new Color32(204, 144, 60, 255), 12, false);
        CreatePanelSprite("bag_button_use", 160, 64, new Color32(8, 118, 139, 255), new Color32(29, 214, 232, 255), new Color32(4, 58, 68, 255), new Color32(142, 255, 248, 255), 18, false);
        CreatePanelSprite("bag_close_button", 64, 64, new Color32(105, 48, 24, 255), new Color32(220, 158, 76, 255), new Color32(53, 22, 11, 255), new Color32(255, 215, 122, 255), 14, false);
        CreatePanelSprite("bag_reward_popup", 160, 128, new Color32(35, 20, 11, 255), new Color32(170, 106, 36, 255), new Color32(18, 10, 6, 255), new Color32(235, 183, 83, 255), 22, false);
        CreatePanelSprite("bag_reward_item", 96, 96, new Color32(38, 18, 45, 255), new Color32(171, 91, 208, 255), new Color32(24, 10, 30, 255), new Color32(242, 185, 255, 255), 14, false);
        CreatePanelSprite("bag_reward_well", 72, 72, new Color32(28, 15, 34, 235), new Color32(86, 45, 112, 255), new Color32(12, 6, 16, 255), new Color32(184, 116, 224, 255), 10, false);
        CreatePanelSprite("bag_count_badge", 64, 32, new Color32(28, 18, 10, 230), new Color32(129, 78, 27, 255), new Color32(10, 7, 5, 255), new Color32(211, 150, 58, 255), 8, false);

        AssetDatabase.Refresh();
        CreatePrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Bag UI assets generated: isolated sprites and prefabs are ready.");
    }

    private static void CreatePanelSprite(
        string name,
        int width,
        int height,
        Color32 fill,
        Color32 border,
        Color32 shadow,
        Color32 highlight,
        int borderSize,
        bool parchmentNoise,
        bool transparentCenter = false)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, transparentCenter ? new Color32(0, 0, 0, 0) : fill);
            }
        }

        DrawBorder(texture, borderSize, border, shadow, highlight, transparentCenter);
        DrawCornerCaps(texture, borderSize, highlight, shadow);

        if (parchmentNoise && !transparentCenter)
        {
            AddParchmentNoise(texture, borderSize);
        }

        AddFantasyOrnaments(texture, name, borderSize, fill, border, shadow, highlight, transparentCenter);
        texture.Apply();
        var path = $"{SpriteDirectory}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = new Vector4(borderSize, borderSize, borderSize, borderSize);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static void DrawBorder(Texture2D texture, int size, Color32 border, Color32 shadow, Color32 highlight, bool transparentCenter)
    {
        var width = texture.width;
        var height = texture.height;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var left = x;
                var right = width - 1 - x;
                var bottom = y;
                var top = height - 1 - y;
                var distance = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top));
                if (distance >= size)
                {
                    continue;
                }

                var color = distance < 3 ? shadow : border;
                if ((top < size / 3 || left < size / 3) && distance >= 3)
                {
                    color = highlight;
                }

                if (transparentCenter && distance > size - 5)
                {
                    color = new Color32(color.r, color.g, color.b, 180);
                }

                texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DrawCornerCaps(Texture2D texture, int size, Color32 highlight, Color32 shadow)
    {
        var width = texture.width;
        var height = texture.height;
        var cap = Mathf.Max(7, size / 2);
        for (var i = 0; i < cap; i++)
        {
            var alpha = (byte)Mathf.Lerp(255, 120, i / (float)cap);
            var bright = new Color32(highlight.r, highlight.g, highlight.b, alpha);
            var dark = new Color32(shadow.r, shadow.g, shadow.b, alpha);
            SafeSet(texture, i, height - 1 - cap + i, bright);
            SafeSet(texture, cap - i, height - 1 - i, bright);
            SafeSet(texture, width - 1 - i, height - 1 - cap + i, bright);
            SafeSet(texture, width - 1 - cap + i, height - 1 - i, bright);
            SafeSet(texture, i, cap - i, dark);
            SafeSet(texture, cap - i, i, dark);
            SafeSet(texture, width - 1 - i, cap - i, dark);
            SafeSet(texture, width - 1 - cap + i, i, dark);
        }
    }

    private static void AddParchmentNoise(Texture2D texture, int borderSize)
    {
        for (var y = borderSize; y < texture.height - borderSize; y++)
        {
            for (var x = borderSize; x < texture.width - borderSize; x++)
            {
                var c = texture.GetPixel(x, y);
                var noise = Mathf.PerlinNoise(x * 0.11f, y * 0.13f) - 0.5f;
                c.r = Mathf.Clamp01(c.r + noise * 0.075f);
                c.g = Mathf.Clamp01(c.g + noise * 0.055f);
                c.b = Mathf.Clamp01(c.b + noise * 0.028f);
                texture.SetPixel(x, y, c);
            }
        }
    }

    private static void AddFantasyOrnaments(
        Texture2D texture,
        string name,
        int borderSize,
        Color32 fill,
        Color32 border,
        Color32 shadow,
        Color32 highlight,
        bool transparentCenter)
    {
        var width = texture.width;
        var height = texture.height;
        var inner = Mathf.Max(4, borderSize - 7);
        var darkGold = Blend(border, shadow, 0.55f);
        var softGold = Blend(border, highlight, 0.42f);
        var centerShade = Blend(fill, shadow, 0.18f);

        if (!transparentCenter)
        {
            DrawRectOutline(texture, inner, inner, width - inner - 1, height - inner - 1, darkGold, 2);
            DrawRectOutline(texture, borderSize + 3, borderSize + 3, width - borderSize - 4, height - borderSize - 4, Blend(fill, highlight, 0.12f), 1);
        }

        if (name.Contains("button") || name.Contains("tab"))
        {
            CarveNotchedCorners(texture, Mathf.Max(6, borderSize / 2));
            DrawHorizontalBand(texture, Mathf.Max(4, borderSize / 3), height - Mathf.Max(8, borderSize), softGold, 2);
            DrawHorizontalBand(texture, Mathf.Max(4, borderSize / 3), Mathf.Max(5, borderSize / 2), Blend(shadow, border, 0.25f), 2);
            DrawDiamond(texture, width / 2, height - Mathf.Max(9, borderSize / 2), Mathf.Max(3, borderSize / 4), softGold);
            DrawDiamond(texture, width / 2, Mathf.Max(9, borderSize / 2), Mathf.Max(3, borderSize / 4), darkGold);
        }

        if (name.Contains("panel") || name.Contains("parchment") || name.Contains("popup"))
        {
            var cap = Mathf.Max(10, borderSize / 2);
            DrawCornerBracket(texture, inner + 2, inner + 2, cap, cap, softGold, true, true);
            DrawCornerBracket(texture, width - inner - 3, inner + 2, cap, cap, softGold, false, true);
            DrawCornerBracket(texture, inner + 2, height - inner - 3, cap, cap, softGold, true, false);
            DrawCornerBracket(texture, width - inner - 3, height - inner - 3, cap, cap, softGold, false, false);
            DrawRivet(texture, inner + 5, inner + 5, 3, highlight, shadow);
            DrawRivet(texture, width - inner - 6, inner + 5, 3, highlight, shadow);
            DrawRivet(texture, inner + 5, height - inner - 6, 3, highlight, shadow);
            DrawRivet(texture, width - inner - 6, height - inner - 6, 3, highlight, shadow);
        }

        if (name.Contains("title"))
        {
            CarveNotchedCorners(texture, Mathf.Max(8, borderSize / 2));
            DrawHorizontalBand(texture, borderSize, height / 2, darkGold, 2);
            DrawDiamond(texture, width / 2, height / 2, Mathf.Max(12, borderSize), softGold);
            DrawDiamond(texture, width / 2, height / 2, Mathf.Max(6, borderSize / 2), centerShade);
        }

        if (name.Contains("slot"))
        {
            CarveNotchedCorners(texture, Mathf.Max(8, borderSize / 2));
            DrawRectOutline(texture, borderSize, borderSize, width - borderSize - 1, height - borderSize - 1, darkGold, 2);
            DrawRivet(texture, borderSize - 2, borderSize - 2, 3, highlight, shadow);
            DrawRivet(texture, width - borderSize + 1, borderSize - 2, 3, highlight, shadow);
        }

        if (name.Contains("selected"))
        {
            DrawRectOutline(texture, 4, 4, width - 5, height - 5, new Color32(91, 255, 250, 230), 3);
            DrawRectOutline(texture, 10, 10, width - 11, height - 11, new Color32(7, 119, 139, 180), 2);
        }

        if (name.Contains("count_badge"))
        {
            CarveNotchedCorners(texture, 5);
            DrawRectOutline(texture, 5, 4, width - 6, height - 5, softGold, 1);
        }
    }

    private static void DrawRectOutline(Texture2D texture, int left, int bottom, int right, int top, Color32 color, int thickness)
    {
        for (var i = 0; i < thickness; i++)
        {
            for (var x = left; x <= right; x++)
            {
                SafeSet(texture, x, bottom + i, color);
                SafeSet(texture, x, top - i, color);
            }

            for (var y = bottom; y <= top; y++)
            {
                SafeSet(texture, left + i, y, color);
                SafeSet(texture, right - i, y, color);
            }
        }
    }

    private static void DrawHorizontalBand(Texture2D texture, int inset, int y, Color32 color, int thickness)
    {
        for (var t = 0; t < thickness; t++)
        {
            for (var x = inset; x < texture.width - inset; x++)
            {
                SafeSet(texture, x, y + t, color);
            }
        }
    }

    private static void DrawCornerBracket(Texture2D texture, int x, int y, int width, int height, Color32 color, bool left, bool bottom)
    {
        var xDirection = left ? 1 : -1;
        var yDirection = bottom ? 1 : -1;
        for (var i = 0; i < width; i++)
        {
            SafeSet(texture, x + i * xDirection, y, color);
            SafeSet(texture, x + i * xDirection, y + yDirection, color);
        }

        for (var i = 0; i < height; i++)
        {
            SafeSet(texture, x, y + i * yDirection, color);
            SafeSet(texture, x + xDirection, y + i * yDirection, color);
        }
    }

    private static void DrawRivet(Texture2D texture, int centerX, int centerY, int radius, Color32 highlight, Color32 shadow)
    {
        for (var y = -radius; y <= radius; y++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                if ((x * x) + (y * y) > radius * radius)
                {
                    continue;
                }

                var color = x + y > 0 ? shadow : highlight;
                SafeSet(texture, centerX + x, centerY + y, color);
            }
        }
    }

    private static void DrawDiamond(Texture2D texture, int centerX, int centerY, int radius, Color32 color)
    {
        for (var y = -radius; y <= radius; y++)
        {
            var span = radius - Mathf.Abs(y);
            for (var x = -span; x <= span; x++)
            {
                SafeSet(texture, centerX + x, centerY + y, color);
            }
        }
    }

    private static void CarveNotchedCorners(Texture2D texture, int notch)
    {
        for (var y = 0; y < notch; y++)
        {
            var width = notch - y;
            for (var x = 0; x < width; x++)
            {
                SafeSet(texture, x, y, new Color32(0, 0, 0, 0));
                SafeSet(texture, texture.width - 1 - x, y, new Color32(0, 0, 0, 0));
                SafeSet(texture, x, texture.height - 1 - y, new Color32(0, 0, 0, 0));
                SafeSet(texture, texture.width - 1 - x, texture.height - 1 - y, new Color32(0, 0, 0, 0));
            }
        }
    }

    private static Color32 Blend(Color32 first, Color32 second, float amount)
    {
        amount = Mathf.Clamp01(amount);
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(first.r, second.r, amount)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(first.g, second.g, amount)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(first.b, second.b, amount)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(first.a, second.a, amount)));
    }

    private static void SafeSet(Texture2D texture, int x, int y, Color32 color)
    {
        if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
        {
            texture.SetPixel(x, y, color);
        }
    }

    private static void CreatePrefabs()
    {
        SavePrefab(CreatePanelPrefab("BagPanel", "bag_panel_frame", new Vector2(900, 1088)), $"{PrefabDirectory}/BagPanel.prefab");
        SavePrefab(CreateButtonPrefab("InventoryTabButton", "bag_tab_button", "Tab", new Vector2(96, 54)), $"{PrefabDirectory}/InventoryTabButton.prefab");
        SavePrefab(CreateButtonPrefab("InventorySmallButton", "bag_button_small", "+", new Vector2(102, 42)), $"{PrefabDirectory}/InventorySmallButton.prefab");
        SavePrefab(CreateButtonPrefab("InventoryUseButton", "bag_button_use", "Use", new Vector2(330, 52)), $"{PrefabDirectory}/InventoryUseButton.prefab");
        SavePrefab(CreateButtonPrefab("InventoryCloseButton", "bag_close_button", "X", new Vector2(54, 54)), $"{PrefabDirectory}/InventoryCloseButton.prefab");
        SavePrefab(CreateSlotPrefab(), $"{PrefabDirectory}/InventorySlot.prefab");
        SavePrefab(CreateDetailPrefab(), $"{PrefabDirectory}/ItemDetailPanel.prefab");
        SavePrefab(CreateUsePrefab(), $"{PrefabDirectory}/UseItemPanel.prefab");
        SavePrefab(CreateRewardPopupPrefab(), $"{PrefabDirectory}/RewardPopup.prefab");
        SavePrefab(CreateRewardEntryPrefab(), $"{PrefabDirectory}/RewardItemEntry.prefab");
    }

    private static GameObject CreatePanelPrefab(string name, string spriteName, Vector2 size)
    {
        var root = CreateImageObject(name, spriteName, size);
        return root;
    }

    private static GameObject CreateButtonPrefab(string name, string spriteName, string label, Vector2 size)
    {
        var root = CreateImageObject(name, spriteName, size);
        var button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        var spriteState = button.spriteState;
        spriteState.pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{GetPressedSpriteName(spriteName)}.png");
        button.spriteState = spriteState;
        CreateLabel(root.transform, "Label", label, 20, Vector2.zero, size - new Vector2(16, 8));
        return root;
    }

    private static GameObject CreateSlotPrefab()
    {
        var root = CreateButtonPrefab("InventorySlot", "bag_slot_frame", string.Empty, new Vector2(142, 104));
        var well = CreateImageObject("Inner", "bag_slot_well", new Vector2(108, 72), root.transform);
        well.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -8);
        well.transform.SetAsFirstSibling();
        var highlight = CreateImageObject("Selected Highlight", "bag_slot_selected", new Vector2(142, 104), root.transform);
        highlight.SetActive(false);
        CreateRawImage(root.transform, "Icon", new Vector2(0, 4), new Vector2(82, 70));
        var badge = CreateImageObject("Count Back", "bag_count_badge", new Vector2(58, 28), root.transform);
        badge.GetComponent<RectTransform>().anchoredPosition = new Vector2(44, -34);
        CreateLabel(badge.transform, "Count", "12", 16, Vector2.zero, new Vector2(52, 24));
        return root;
    }

    private static GameObject CreateDetailPrefab()
    {
        var root = CreateImageObject("ItemDetailPanel", "bag_detail_panel", new Vector2(790, 208));
        var icon = CreateImageObject("Icon Frame", "bag_icon_frame", new Vector2(136, 124), root.transform);
        icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-292, -8);
        CreateImageObject("Icon Well", "bag_icon_well", new Vector2(112, 86), icon.transform);
        CreateRawImage(icon.transform, "Icon", Vector2.zero, new Vector2(100, 82));
        CreateLabel(root.transform, "Title", "Hero Shard Chest", 24, new Vector2(88, 42), new Vector2(488, 34), TextAlignmentOptions.Left);
        CreateLabel(root.transform, "Description", "Open to obtain Hero Shards.", 17, new Vector2(88, 4), new Vector2(488, 44), TextAlignmentOptions.Left);
        CreateLabel(root.transform, "Owned", "Owned: 12", 18, new Vector2(88, -48), new Vector2(488, 38), TextAlignmentOptions.Left);
        return root;
    }

    private static GameObject CreateUsePrefab()
    {
        var root = CreateImageObject("UseItemPanel", "bag_use_panel", new Vector2(790, 224));
        var icon = CreateImageObject("Use Item Frame", "bag_icon_frame", new Vector2(100, 92), root.transform);
        icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-304, 0);
        CreateImageObject("Icon Well", "bag_icon_well", new Vector2(78, 58), icon.transform);
        CreateRawImage(icon.transform, "Icon", new Vector2(0, -4), new Vector2(78, 58));
        CreateLabel(root.transform, "Title", "Use Hero Shard Chest", 22, new Vector2(54, 42), new Vector2(500, 34), TextAlignmentOptions.Left);
        CreateLabel(root.transform, "Hint", "Select amount and confirm.", 16, new Vector2(54, 10), new Vector2(500, 28), TextAlignmentOptions.Left);
        CreateButtonChild(root.transform, "UseOneButton", "bag_button_small", "Use 1", new Vector2(102, 42), new Vector2(-178, -24));
        CreateButtonChild(root.transform, "MinusButton", "bag_button_small", "-", new Vector2(52, 42), new Vector2(-56, -24));
        CreateButtonChild(root.transform, "AmountButton", "bag_button_small", "5", new Vector2(88, 42), new Vector2(20, -24));
        CreateButtonChild(root.transform, "PlusButton", "bag_button_small", "+", new Vector2(52, 42), new Vector2(96, -24));
        CreateButtonChild(root.transform, "AllButton", "bag_button_small", "All", new Vector2(102, 42), new Vector2(216, -24));
        CreateButtonChild(root.transform, "UseButton", "bag_button_use", "Use", new Vector2(330, 52), new Vector2(54, -66));
        return root;
    }

    private static GameObject CreateRewardPopupPrefab()
    {
        var root = CreateImageObject("RewardPopup", "bag_reward_popup", new Vector2(760, 350));
        CreateLabel(root.transform, "Title", "Rewards", 30, new Vector2(0, 130), new Vector2(260, 40));
        CreateButtonChild(root.transform, "Close", "bag_close_button", "X", new Vector2(48, 48), new Vector2(334, 126));
        CreateButtonChild(root.transform, "OK", "bag_button_blue", "OK", new Vector2(330, 58), new Vector2(0, -110));
        return root;
    }

    private static GameObject CreateRewardEntryPrefab()
    {
        var root = CreateImageObject("RewardItemEntry", "bag_reward_item", new Vector2(80, 88));
        var well = CreateImageObject("Inner", "bag_reward_well", new Vector2(58, 58), root.transform);
        well.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 8);
        CreateRawImage(root.transform, "Icon", new Vector2(0, 8), new Vector2(62, 62));
        CreateLabel(root.transform, "Amount", "+5", 13, new Vector2(0, -34), new Vector2(74, 20));
        return root;
    }

    private static string GetPressedSpriteName(string spriteName)
    {
        if (spriteName == "bag_tab_button")
        {
            return "bag_tab_button_pressed";
        }

        if (spriteName == "bag_button_brown" || spriteName == "bag_button_small")
        {
            return "bag_button_small_pressed";
        }

        return spriteName;
    }

    private static GameObject CreateImageObject(string name, string spriteName, Vector2 size, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{spriteName}.png");
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return go;
    }

    private static GameObject CreateButtonChild(Transform parent, string name, string spriteName, string label, Vector2 size, Vector2 position)
    {
        var button = CreateButtonPrefab(name, spriteName, label, size);
        button.transform.SetParent(parent, false);
        button.GetComponent<RectTransform>().anchoredPosition = position;
        return button;
    }

    private static RawImage CreateRawImage(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.GetComponent<RawImage>();
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateLabel(Transform parent, string name, string value, int size, Vector2 position, Vector2 rectSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = rectSize;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10;
        text.fontSizeMax = size;
        text.raycastTarget = false;
        return text;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
