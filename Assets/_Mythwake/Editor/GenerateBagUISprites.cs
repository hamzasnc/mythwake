using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GenerateBagUISprites
{
    private const string ReferencePath = "Pictures/bag_mockup_reference.png";
    private const string SpriteDirectory = "Assets/Art/UI/BagGenerated/Sprites";
    private const string PrefabDirectory = "Assets/Art/UI/BagGenerated/Prefabs";
    private const string ResourceDirectory = "Assets/Art/UI/BagGenerated/Resources/Mythwake/UI";
    private const string CatalogPath = ResourceDirectory + "/BagGeneratedSpriteCatalog.asset";

    private struct SpriteSpec
    {
        public string name;
        public RectInt crop;
        public RectInt fill;
        public Vector4 border;
        public bool cropOnly;
        public bool transparentCenter;
        public Color tint;

        public SpriteSpec(string name, RectInt crop, RectInt fill, Vector4 border, bool cropOnly = false, bool transparentCenter = false, Color? tint = null)
        {
            this.name = name;
            this.crop = crop;
            this.fill = fill;
            this.border = border;
            this.cropOnly = cropOnly;
            this.transparentCenter = transparentCenter;
            this.tint = tint ?? Color.white;
        }
    }

    [MenuItem("Mythwake/Generate Bag UI Sprites From Mockup")]
    public static void Generate()
    {
        if (!File.Exists(ReferencePath))
        {
            throw new FileNotFoundException($"Bag mockup reference image missing at {ReferencePath}.", ReferencePath);
        }

        Directory.CreateDirectory(SpriteDirectory);
        Directory.CreateDirectory(PrefabDirectory);
        Directory.CreateDirectory(ResourceDirectory);

        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(source, File.ReadAllBytes(ReferencePath)))
        {
            throw new InvalidOperationException($"Could not decode bag mockup reference image at {ReferencePath}.");
        }

        var parchmentFill = new RectInt(704, 166, 60, 50);
        var darkFill = new RectInt(292, 292, 36, 36);
        var tealFill = new RectInt(338, 1022, 54, 36);
        var brownButtonFill = new RectInt(330, 958, 34, 24);
        var rewardFill = new RectInt(150, 1152, 70, 54);

        var specs = new[]
        {
            new SpriteSpec("bag_panel_frame", new RectInt(95, 123, 744, 654), parchmentFill, new Vector4(48, 48, 48, 48)),
            new SpriteSpec("bag_panel_background", new RectInt(166, 143, 96, 96), parchmentFill, new Vector4(18, 18, 18, 18)),
            new SpriteSpec("bag_header", new RectInt(283, 145, 374, 58), darkFill, new Vector4(44, 18, 44, 18)),
            new SpriteSpec("bag_close_button", new RectInt(727, 806, 63, 58), default, new Vector4(14, 14, 14, 14), cropOnly: true),
            new SpriteSpec("bag_tab_normal", new RectInt(238, 209, 101, 53), darkFill, new Vector4(18, 14, 18, 14)),
            new SpriteSpec("bag_tab_selected", new RectInt(136, 208, 101, 54), new RectInt(152, 222, 48, 24), new Vector4(18, 14, 18, 14)),
            new SpriteSpec("bag_tab_pressed", new RectInt(238, 209, 101, 53), darkFill, new Vector4(18, 14, 18, 14), tint: new Color(0.78f, 0.78f, 0.78f, 1f)),
            new SpriteSpec("bag_slot_empty", new RectInt(278, 285, 126, 126), darkFill, new Vector4(20, 20, 20, 20), tint: new Color(0.64f, 0.64f, 0.64f, 0.88f)),
            new SpriteSpec("bag_slot_filled", new RectInt(278, 285, 126, 126), darkFill, new Vector4(20, 20, 20, 20)),
            new SpriteSpec("bag_slot_selected", new RectInt(140, 283, 126, 126), default, new Vector4(20, 20, 20, 20), transparentCenter: true),
            new SpriteSpec("bag_slot_amount_badge", new RectInt(222, 379, 42, 30), darkFill, new Vector4(8, 8, 8, 8)),
            new SpriteSpec("bag_detail_panel", new RectInt(137, 570, 666, 180), parchmentFill, new Vector4(36, 36, 36, 36)),
            new SpriteSpec("bag_use_panel", new RectInt(126, 800, 668, 224), parchmentFill, new Vector4(36, 36, 36, 36)),
            new SpriteSpec("bag_button_normal", new RectInt(319, 951, 93, 43), brownButtonFill, new Vector4(18, 14, 18, 14)),
            new SpriteSpec("bag_button_pressed", new RectInt(319, 951, 93, 43), brownButtonFill, new Vector4(18, 14, 18, 14), tint: new Color(0.74f, 0.74f, 0.74f, 1f)),
            new SpriteSpec("bag_button_disabled", new RectInt(319, 951, 93, 43), brownButtonFill, new Vector4(18, 14, 18, 14), tint: new Color(0.42f, 0.42f, 0.42f, 0.82f)),
            new SpriteSpec("bag_reward_popup_frame", new RectInt(96, 1115, 744, 398), rewardFill, new Vector4(54, 54, 54, 54)),
            new SpriteSpec("bag_reward_slot", new RectInt(177, 1220, 105, 112), new RectInt(194, 1258, 38, 38), new Vector4(16, 16, 16, 16)),
            new SpriteSpec("bag_ok_button", new RectInt(314, 1427, 319, 73), tealFill, new Vector4(32, 24, 32, 24)),
            new SpriteSpec("bag_icon_frame", new RectInt(145, 573, 193, 184), new RectInt(174, 608, 54, 54), new Vector4(24, 24, 24, 24)),
            new SpriteSpec("bag_reward_inner", new RectInt(255, 1362, 430, 52), rewardFill, new Vector4(18, 18, 18, 18)),
        };

        for (var i = 0; i < specs.Length; i++)
        {
            var texture = specs[i].cropOnly
                ? Crop(source, specs[i].crop, specs[i].tint)
                : BuildSlicedSpriteTexture(source, specs[i]);
            SaveTexture(specs[i].name, texture);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        UnityEngine.Object.DestroyImmediate(source);
        AssetDatabase.Refresh();
        ConfigureImports(specs);
        AssetDatabase.Refresh();
        CreateCatalog(specs);
        CreatePrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Generated {specs.Length} Bag UI PNG sprites from {ReferencePath} into {SpriteDirectory}.");
    }

    private static Texture2D Crop(Texture2D source, RectInt crop, Color tint)
    {
        var texture = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false);
        for (var y = 0; y < crop.height; y++)
        {
            for (var x = 0; x < crop.width; x++)
            {
                SetPixelTop(texture, x, y, GetPixelTop(source, crop.x + x, crop.y + y) * tint);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D BuildSlicedSpriteTexture(Texture2D source, SpriteSpec spec)
    {
        return spec.name switch
        {
            "bag_panel_frame" => BuildParchmentPanel(source, spec, 48, true),
            "bag_panel_background" => BuildParchmentPanel(source, spec, 18, false),
            "bag_header" => BuildPlaque(source, spec, PlaqueStyle.Header),
            "bag_tab_normal" => BuildPlaque(source, spec, PlaqueStyle.TabNormal),
            "bag_tab_selected" => BuildPlaque(source, spec, PlaqueStyle.TabSelected),
            "bag_tab_pressed" => BuildPlaque(source, spec, PlaqueStyle.TabPressed),
            "bag_slot_empty" => BuildSlot(source, spec, false),
            "bag_slot_filled" => BuildSlot(source, spec, true),
            "bag_slot_selected" => BuildSelectedSlot(spec),
            "bag_slot_amount_badge" => BuildPlaque(source, spec, PlaqueStyle.Badge),
            "bag_detail_panel" => BuildParchmentPanel(source, spec, 36, false),
            "bag_use_panel" => BuildParchmentPanel(source, spec, 36, false),
            "bag_button_normal" => BuildPlaque(source, spec, PlaqueStyle.BrownButton),
            "bag_button_pressed" => BuildPlaque(source, spec, PlaqueStyle.BrownButton),
            "bag_button_disabled" => BuildPlaque(source, spec, PlaqueStyle.BrownButton),
            "bag_reward_popup_frame" => BuildRewardPanel(source, spec),
            "bag_reward_slot" => BuildRewardSlot(source, spec),
            "bag_ok_button" => BuildPlaque(source, spec, PlaqueStyle.TealButton),
            "bag_icon_frame" => BuildIconFrame(source, spec),
            "bag_reward_inner" => BuildRewardInner(source, spec),
            _ => BuildParchmentPanel(source, spec, Mathf.RoundToInt(spec.border.x), false),
        };
    }

    private enum PlaqueStyle
    {
        Header,
        TabNormal,
        TabSelected,
        TabPressed,
        Badge,
        BrownButton,
        TealButton,
    }

    private static Texture2D BuildParchmentPanel(Texture2D source, SpriteSpec spec, int frame, bool heavy)
    {
        var texture = CreateTransparentTexture(spec.crop.width, spec.crop.height);
        var parchment = GetAverageColorTop(source, spec.fill);
        var gold = new Color(0.78f, 0.50f, 0.20f, 1f);
        var goldLight = new Color(1f, 0.78f, 0.34f, 1f);
        var dark = new Color(0.12f, 0.065f, 0.026f, 1f);
        var shadow = new Color(0.045f, 0.026f, 0.012f, 1f);

        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var edge = Mathf.Min(Mathf.Min(x, texture.width - 1 - x), Mathf.Min(y, texture.height - 1 - y));
                var color = ParchmentNoise(parchment, spec.crop.x + x, spec.crop.y + y, 0.1f);
                if (edge < 4)
                {
                    color = shadow;
                }
                else if (edge < 8)
                {
                    color = gold;
                }
                else if (edge < 12)
                {
                    color = dark;
                }
                else if (edge < frame)
                {
                    var t = Mathf.InverseLerp(12f, frame, edge);
                    color = Color.Lerp(new Color(0.32f, 0.18f, 0.07f, 1f), ParchmentNoise(parchment, spec.crop.x + x, spec.crop.y + y, 0.06f), t);
                }

                SetPixelTop(texture, x, y, color * spec.tint);
            }
        }

        DrawInsetLine(texture, 15, goldLight, 1);
        DrawInsetLine(texture, heavy ? 31 : 25, new Color(0.44f, 0.25f, 0.09f, 0.95f), 1);
        DrawCornerOrnaments(texture, frame, goldLight, dark);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildPlaque(Texture2D source, SpriteSpec spec, PlaqueStyle style)
    {
        var texture = CreateTransparentTexture(spec.crop.width, spec.crop.height);
        var dark = GetAverageColorTop(source, spec.fill);
        var brown = GetAverageColorTop(source, new RectInt(330, 958, 34, 24));
        var teal = GetAverageColorTop(source, new RectInt(338, 1022, 54, 36));
        var gold = new Color(0.88f, 0.58f, 0.22f, 1f);
        var goldLight = new Color(1f, 0.78f, 0.36f, 1f);
        var baseColor = style switch
        {
            PlaqueStyle.Header => Color.Lerp(dark, new Color(0.22f, 0.12f, 0.05f, 1f), 0.35f),
            PlaqueStyle.TabSelected => Color.Lerp(brown, new Color(0.93f, 0.60f, 0.18f, 1f), 0.3f),
            PlaqueStyle.TabPressed => Color.Lerp(dark, Color.black, 0.08f),
            PlaqueStyle.Badge => Color.Lerp(dark, Color.black, 0.18f),
            PlaqueStyle.BrownButton => brown,
            PlaqueStyle.TealButton => teal,
            _ => dark,
        };

        var cut = style == PlaqueStyle.Header || style == PlaqueStyle.TealButton ? Mathf.RoundToInt(texture.height * 0.38f) : Mathf.RoundToInt(texture.height * 0.2f);
        var border = style == PlaqueStyle.Badge ? 5 : 7;
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                if (!InsideChamferedRect(x, y, texture.width, texture.height, cut, 0))
                {
                    continue;
                }

                var inner = InsideChamferedRect(x, y, texture.width, texture.height, cut, border);
                var color = inner
                    ? ParchmentNoise(baseColor, spec.crop.x + x, spec.crop.y + y, style == PlaqueStyle.TealButton ? 0.12f : 0.08f)
                    : gold;
                if (!inner && (x < 3 || y < 3 || x >= texture.width - 3 || y >= texture.height - 3))
                {
                    color = new Color(0.09f, 0.045f, 0.018f, 1f);
                }

                SetPixelTop(texture, x, y, color * spec.tint);
            }
        }

        if (style == PlaqueStyle.Header || style == PlaqueStyle.TealButton)
        {
            DrawHorizontalAccent(texture, texture.height / 2, goldLight);
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D BuildSlot(Texture2D source, SpriteSpec spec, bool filled)
    {
        var texture = CreateTransparentTexture(spec.crop.width, spec.crop.height);
        var dark = GetAverageColorTop(source, spec.fill);
        var gold = new Color(0.68f, 0.42f, 0.16f, 1f);
        var goldLight = new Color(0.96f, 0.68f, 0.28f, 1f);
        var outer = new Color(0.055f, 0.034f, 0.017f, 1f);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var edge = Mathf.Min(Mathf.Min(x, texture.width - 1 - x), Mathf.Min(y, texture.height - 1 - y));
                var color = ParchmentNoise(Color.Lerp(dark, Color.black, filled ? 0.04f : 0.2f), spec.crop.x + x, spec.crop.y + y, 0.14f);
                if (edge < 4)
                {
                    color = outer;
                }
                else if (edge < 8)
                {
                    color = gold;
                }
                else if (edge < 13)
                {
                    color = new Color(0.11f, 0.065f, 0.028f, 1f);
                }

                SetPixelTop(texture, x, y, color * spec.tint);
            }
        }

        DrawInsetLine(texture, 9, goldLight, 1);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildSelectedSlot(SpriteSpec spec)
    {
        var texture = CreateTransparentTexture(spec.crop.width, spec.crop.height);
        var teal = new Color(0.02f, 0.78f, 0.90f, 1f);
        var tealDark = new Color(0.01f, 0.22f, 0.26f, 0.86f);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var edge = Mathf.Min(Mathf.Min(x, texture.width - 1 - x), Mathf.Min(y, texture.height - 1 - y));
                if (edge < 5)
                {
                    SetPixelTop(texture, x, y, teal);
                }
                else if (edge < 11)
                {
                    SetPixelTop(texture, x, y, tealDark);
                }
            }
        }

        DrawInsetLine(texture, 3, new Color(0.58f, 1f, 1f, 1f), 1);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildRewardPanel(Texture2D source, SpriteSpec spec)
    {
        var texture = CreateTransparentTexture(spec.crop.width, spec.crop.height);
        var dark = GetAverageColorTop(source, spec.fill);
        var gold = new Color(0.75f, 0.45f, 0.17f, 1f);
        var goldLight = new Color(1f, 0.75f, 0.32f, 1f);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var edge = Mathf.Min(Mathf.Min(x, texture.width - 1 - x), Mathf.Min(y, texture.height - 1 - y));
                var color = ParchmentNoise(Color.Lerp(dark, Color.black, 0.38f), spec.crop.x + x, spec.crop.y + y, 0.12f);
                if (edge < 5)
                {
                    color = Color.black;
                }
                else if (edge < 10)
                {
                    color = gold;
                }
                else if (edge < 18)
                {
                    color = new Color(0.12f, 0.07f, 0.03f, 1f);
                }

                SetPixelTop(texture, x, y, color);
            }
        }

        DrawInsetLine(texture, 12, goldLight, 1);
        DrawCornerOrnaments(texture, 54, goldLight, Color.black);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildRewardSlot(Texture2D source, SpriteSpec spec)
    {
        var texture = BuildSlot(source, spec, true);
        DrawInsetLine(texture, 5, new Color(0.82f, 0.18f, 1f, 0.92f), 2);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildIconFrame(Texture2D source, SpriteSpec spec)
    {
        var texture = BuildSlot(source, spec, true);
        DrawInsetLine(texture, 7, new Color(0.02f, 0.64f, 0.72f, 0.95f), 2);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildRewardInner(Texture2D source, SpriteSpec spec)
    {
        var texture = BuildPlaque(source, spec, PlaqueStyle.TealButton);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var color = GetPixelTop(texture, x, y);
                color.a *= 0.72f;
                SetPixelTop(texture, x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D CreateTransparentTexture(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                SetPixelTop(texture, x, y, Color.clear);
            }
        }

        return texture;
    }

    private static Color ParchmentNoise(Color baseColor, int x, int y, float strength)
    {
        var noise = Mathf.PerlinNoise(x * 0.071f, y * 0.083f) - 0.5f;
        var factor = 1f + (noise * strength);
        return new Color(
            Mathf.Clamp01(baseColor.r * factor),
            Mathf.Clamp01(baseColor.g * factor),
            Mathf.Clamp01(baseColor.b * factor),
            baseColor.a);
    }

    private static bool InsideChamferedRect(int x, int y, int width, int height, int cut, int inset)
    {
        var ix = x - inset;
        var iy = y - inset;
        var iw = width - (inset * 2);
        var ih = height - (inset * 2);
        if (ix < 0 || iy < 0 || ix >= iw || iy >= ih)
        {
            return false;
        }

        var half = Mathf.Max(1f, ih * 0.5f);
        var side = Mathf.Max(0f, cut - (Mathf.Abs(iy - half) * cut / half));
        return ix >= side && ix < iw - side;
    }

    private static void DrawInsetLine(Texture2D texture, int inset, Color color, int thickness)
    {
        for (var i = 0; i < thickness; i++)
        {
            var offset = inset + i;
            for (var x = offset; x < texture.width - offset; x++)
            {
                SetPixelTop(texture, x, offset, color);
                SetPixelTop(texture, x, texture.height - 1 - offset, color);
            }

            for (var y = offset; y < texture.height - offset; y++)
            {
                SetPixelTop(texture, offset, y, color);
                SetPixelTop(texture, texture.width - 1 - offset, y, color);
            }
        }
    }

    private static void DrawHorizontalAccent(Texture2D texture, int y, Color color)
    {
        for (var x = Mathf.RoundToInt(texture.width * 0.16f); x < Mathf.RoundToInt(texture.width * 0.84f); x++)
        {
            SetPixelTop(texture, x, y, color);
            if (y + 1 < texture.height)
            {
                SetPixelTop(texture, x, y + 1, new Color(color.r, color.g, color.b, 0.42f));
            }
        }
    }

    private static void DrawCornerOrnaments(Texture2D texture, int size, Color gold, Color dark)
    {
        var ornament = Mathf.Max(18, Mathf.RoundToInt(size * 0.55f));
        for (var i = 0; i < ornament; i++)
        {
            DrawCornerPixel(texture, i, ornament - i, gold);
            DrawCornerPixel(texture, i + 6, ornament - i, dark);
            DrawCornerPixel(texture, ornament - i, i, gold);
        }
    }

    private static void DrawCornerPixel(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
        {
            return;
        }

        SetPixelTop(texture, x, y, color);
        SetPixelTop(texture, texture.width - 1 - x, y, color);
        SetPixelTop(texture, x, texture.height - 1 - y, color);
        SetPixelTop(texture, texture.width - 1 - x, texture.height - 1 - y, color);
    }

    private static Color GetAverageColorTop(Texture2D source, RectInt rect)
    {
        if (rect.width <= 0 || rect.height <= 0)
        {
            return Color.white;
        }

        var sum = Vector4.zero;
        var count = 0;
        for (var y = 0; y < rect.height; y++)
        {
            for (var x = 0; x < rect.width; x++)
            {
                var color = GetPixelTop(source, rect.x + x, rect.y + y);
                sum += new Vector4(color.r, color.g, color.b, color.a);
                count++;
            }
        }

        if (count <= 0)
        {
            return Color.white;
        }

        return new Color(sum.x / count, sum.y / count, sum.z / count, sum.w / count);
    }

    private static Color GetPixelTop(Texture2D texture, int x, int yTop)
    {
        x = Mathf.Clamp(x, 0, texture.width - 1);
        yTop = Mathf.Clamp(yTop, 0, texture.height - 1);
        return texture.GetPixel(x, texture.height - 1 - yTop);
    }

    private static void SetPixelTop(Texture2D texture, int x, int yTop, Color color)
    {
        texture.SetPixel(x, texture.height - 1 - yTop, color);
    }

    private static void SaveTexture(string name, Texture2D texture)
    {
        var path = $"{SpriteDirectory}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
    }

    private static void ConfigureImports(SpriteSpec[] specs)
    {
        for (var i = 0; i < specs.Length; i++)
        {
            var path = $"{SpriteDirectory}/{specs[i].name}.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = specs[i].border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }

    private static void CreateCatalog(SpriteSpec[] specs)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<BagGeneratedSpriteCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<BagGeneratedSpriteCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var entries = new List<BagGeneratedSpriteCatalog.Entry>();
        for (var i = 0; i < specs.Length; i++)
        {
            entries.Add(new BagGeneratedSpriteCatalog.Entry
            {
                key = specs[i].name,
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{specs[i].name}.png")
            });
        }

        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
    }

    private static void CreatePrefabs()
    {
        SavePrefab(CreateImagePrefab("BagPanel", "bag_panel_frame", new Vector2(744, 654)), $"{PrefabDirectory}/BagPanel.prefab");
        SavePrefab(CreateButtonPrefab("BagTabButton", "bag_tab_normal", "All", new Vector2(101, 54)), $"{PrefabDirectory}/BagTabButton.prefab");
        SavePrefab(CreateInventorySlotPrefab(), $"{PrefabDirectory}/BagInventorySlot.prefab");
        SavePrefab(CreateDetailPrefab(), $"{PrefabDirectory}/BagItemDetailPanel.prefab");
        SavePrefab(CreateUsePrefab(), $"{PrefabDirectory}/BagUsePanel.prefab");
        SavePrefab(CreateRewardPopupPrefab(), $"{PrefabDirectory}/BagRewardPopup.prefab");
        SavePrefab(CreateRewardEntryPrefab(), $"{PrefabDirectory}/BagRewardEntry.prefab");
    }

    private static GameObject CreateInventorySlotPrefab()
    {
        var root = CreateButtonPrefab("BagInventorySlot", "bag_slot_filled", string.Empty, new Vector2(126, 126));
        var highlight = CreateImageChild(root.transform, "Selected", "bag_slot_selected", Vector2.zero, new Vector2(126, 126));
        highlight.SetActive(false);
        CreateImageChild(root.transform, "AmountBadge", "bag_slot_amount_badge", new Vector2(43, -43), new Vector2(42, 30));
        return root;
    }

    private static GameObject CreateDetailPrefab()
    {
        var root = CreateImagePrefab("BagItemDetailPanel", "bag_detail_panel", new Vector2(666, 180));
        CreateImageChild(root.transform, "IconFrame", "bag_icon_frame", new Vector2(-236, -2), new Vector2(180, 168));
        CreateLabel(root.transform, "Title", "Hero Shard Chest", 24, new Vector2(80, 42), new Vector2(420, 36), TextAlignmentOptions.Left);
        CreateLabel(root.transform, "Owned", "Owned: 12", 18, new Vector2(80, -44), new Vector2(420, 30), TextAlignmentOptions.Left);
        return root;
    }

    private static GameObject CreateUsePrefab()
    {
        var root = CreateImagePrefab("BagUsePanel", "bag_use_panel", new Vector2(668, 224));
        CreateLabel(root.transform, "Status", "Select amount to use.", 18, new Vector2(0, 62), new Vector2(480, 34));
        CreateButtonChild(root.transform, "UseOne", "bag_button_normal", "Use 1", new Vector2(93, 43), new Vector2(-148, 6));
        CreateButtonChild(root.transform, "Minus", "bag_button_normal", "-", new Vector2(50, 43), new Vector2(-62, 6));
        CreateButtonChild(root.transform, "Amount", "bag_button_normal", "5", new Vector2(93, 43), new Vector2(20, 6));
        CreateButtonChild(root.transform, "Plus", "bag_button_normal", "+", new Vector2(50, 43), new Vector2(102, 6));
        CreateButtonChild(root.transform, "All", "bag_button_normal", "All", new Vector2(93, 43), new Vector2(188, 6));
        CreateButtonChild(root.transform, "Use", "bag_ok_button", "Use", new Vector2(286, 64), new Vector2(0, -66));
        return root;
    }

    private static GameObject CreateRewardPopupPrefab()
    {
        var root = CreateImagePrefab("BagRewardPopup", "bag_reward_popup_frame", new Vector2(744, 398));
        CreateLabel(root.transform, "Title", "Rewards", 30, new Vector2(0, 130), new Vector2(300, 40));
        CreateButtonChild(root.transform, "OK", "bag_ok_button", "OK", new Vector2(319, 73), new Vector2(0, -130));
        return root;
    }

    private static GameObject CreateRewardEntryPrefab()
    {
        var root = CreateImagePrefab("BagRewardEntry", "bag_reward_slot", new Vector2(105, 112));
        CreateLabel(root.transform, "Amount", "5", 18, new Vector2(34, -36), new Vector2(34, 24));
        return root;
    }

    private static GameObject CreateImagePrefab(string name, string spriteName, Vector2 size)
    {
        return CreateImageObject(name, spriteName, size, null);
    }

    private static GameObject CreateImageChild(Transform parent, string name, string spriteName, Vector2 position, Vector2 size)
    {
        return CreateImageObject(name, spriteName, size, parent, position);
    }

    private static GameObject CreateImageObject(string name, string spriteName, Vector2 size, Transform parent, Vector2? position = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position ?? Vector2.zero;
        var image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{spriteName}.png");
        image.type = image.sprite != null && image.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = false;
        return go;
    }

    private static GameObject CreateButtonPrefab(string name, string spriteName, string label, Vector2 size)
    {
        var root = CreateImagePrefab(name, spriteName, size);
        var image = root.GetComponent<Image>();
        image.raycastTarget = true;
        var button = root.AddComponent<Button>();
        button.targetGraphic = image;
        var spriteState = button.spriteState;
        spriteState.pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/bag_button_pressed.png");
        spriteState.disabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/bag_button_disabled.png");
        button.spriteState = spriteState;
        CreateLabel(root.transform, "Label", label, 18, Vector2.zero, size - new Vector2(16, 8));
        return root;
    }

    private static void CreateButtonChild(Transform parent, string name, string spriteName, string label, Vector2 size, Vector2 position)
    {
        var button = CreateButtonPrefab(name, spriteName, label, size);
        button.transform.SetParent(parent, false);
        button.GetComponent<RectTransform>().anchoredPosition = position;
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
        text.raycastTarget = false;
        return text;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }
}
