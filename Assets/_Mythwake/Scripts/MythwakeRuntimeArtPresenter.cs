using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct MythwakeRuntimeArtState
{
    public int selectedHeroIndex;
    public string selectedHeroName;
    public int selectedHeroLevel;
    public int selectedHeroAscension;
    public int teamPower;
    public int teamAttack;
    public int teamHealth;
    public int campaignStage;
    public string campaignEnemyName;
    public int gold;
    public int gems;
    public int mythEssence;
    public int goldDungeonFloor;
    public int essenceDungeonFloor;
    public int gearDungeonFloor;
    public bool backendRequestInProgress;
}

public sealed class MythwakeRuntimeArtPresenter
{
    private const string ResourceRoot = "Mythwake/Art/Runtime/";

    private static readonly string[] HeroTextureNames =
    {
        "hero_astra",
        "hero_borin",
        "hero_cyra",
        "hero_dante",
        "hero_elowen"
    };

    private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    private RectTransform homeRoot;
    private RectTransform battleRoot;
    private RectTransform summonRoot;
    private RawImage battleHeroImage;
    private RawImage battleEnemyImage;
    private RawImage battleVfxImage;
    private RawImage summonHeroImage;
    private RawImage summonVfxImage;
    private TMP_Text battleTitleText;
    private TMP_Text battleMetaText;
    private TMP_Text resourceText;
    private TMP_Text dungeonText;
    private TMP_Text summonTitleText;
    private Image heroHpFill;
    private Image enemyHpFill;
    private RectTransform battleHeroRect;
    private RectTransform battleEnemyRect;
    private RectTransform battleVfxRect;
    private RectTransform summonVfxRect;

    private float combatVfxTimer;
    private float summonVfxTimer;
    private float heroHpPercent = 1f;
    private float enemyHpPercent = 1f;
    private string currentEnemyTexture = "enemy_campaign";
    private string currentVfxTexture = "vfx_slash";
    private string currentBattleTitle = "Campaign";
    private string currentBattleMeta = "Ready";
    private int currentSummonHeroIndex;

    public void Ensure(GameObject homePanel, GameObject battlePanel, GameObject summonPanel, GameObject shopPanel)
    {
        if (homePanel != null && homeRoot == null)
        {
            homeRoot = CreatePanel(homePanel.transform, "Runtime Art Home Backdrop", new Vector2(0f, -280f), new Vector2(900f, 360f), new Color(0.05f, 0.08f, 0.13f, 0.9f));
            homeRoot.SetAsFirstSibling();
            CreateBackgroundLayers(homeRoot);
            resourceText = CreateText(homeRoot, "Resource Icon Row", "Gold 0   Gems 0   Essence 0", 24, new Vector2(0f, -286f), new Vector2(820f, 42f), FontStyles.Bold);
        }

        if (battlePanel != null && battleRoot == null)
        {
            battleRoot = CreatePanel(battlePanel.transform, "Runtime Art Battle Stage", new Vector2(0f, -360f), new Vector2(900f, 380f), new Color(0.05f, 0.07f, 0.12f, 0.94f));
            battleRoot.SetAsFirstSibling();
            CreateBackgroundLayers(battleRoot);

            battleTitleText = CreateText(battleRoot, "Battle Visual Title", "Campaign", 28, new Vector2(0f, -24f), new Vector2(820f, 42f), FontStyles.Bold);
            battleMetaText = CreateText(battleRoot, "Battle Visual Meta", "Ready", 20, new Vector2(0f, -322f), new Vector2(820f, 40f), FontStyles.Bold);
            battleHeroImage = CreateTextureImage(battleRoot, "Battle Hero", "hero_astra", new Vector2(-260f, -158f), new Vector2(132f, 132f));
            battleEnemyImage = CreateTextureImage(battleRoot, "Battle Enemy", "enemy_campaign", new Vector2(260f, -158f), new Vector2(132f, 132f));
            battleVfxImage = CreateTextureImage(battleRoot, "Battle VFX", "vfx_slash", new Vector2(115f, -150f), new Vector2(190f, 190f));
            battleHeroRect = battleHeroImage.GetComponent<RectTransform>();
            battleEnemyRect = battleEnemyImage.GetComponent<RectTransform>();
            battleVfxRect = battleVfxImage.GetComponent<RectTransform>();
            battleVfxImage.color = new Color(1f, 1f, 1f, 0f);

            heroHpFill = CreateHealthBar(battleRoot, "Hero HP Bar", new Vector2(-260f, -246f), new Color(0.16f, 0.77f, 0.38f));
            enemyHpFill = CreateHealthBar(battleRoot, "Enemy HP Bar", new Vector2(260f, -246f), new Color(0.86f, 0.2f, 0.24f));
            dungeonText = CreateText(battleRoot, "Dungeon Visual Summary", "Gold F1 | Essence F1 | Gear F1", 18, new Vector2(0f, -286f), new Vector2(820f, 34f), FontStyles.Bold);

            CreateTextureImage(battleRoot, "Gold Dungeon Token", "icon_gold", new Vector2(-120f, -286f), new Vector2(34f, 34f));
            CreateTextureImage(battleRoot, "Essence Dungeon Token", "icon_essence", new Vector2(32f, -286f), new Vector2(34f, 34f));
            CreateTextureImage(battleRoot, "Gear Dungeon Token", "icon_weapon", new Vector2(196f, -286f), new Vector2(34f, 34f));
        }

        if (summonPanel != null && summonRoot == null)
        {
            summonRoot = CreatePanel(summonPanel.transform, "Runtime Art Summon Portal", new Vector2(0f, -330f), new Vector2(820f, 360f), new Color(0.06f, 0.06f, 0.13f, 0.94f));
            summonRoot.SetAsFirstSibling();
            CreateBackgroundLayers(summonRoot);

            summonTitleText = CreateText(summonRoot, "Summon Visual Title", "Shard Gate", 30, new Vector2(0f, -28f), new Vector2(760f, 44f), FontStyles.Bold);
            summonHeroImage = CreateTextureImage(summonRoot, "Summon Hero", "hero_cyra", new Vector2(0f, -176f), new Vector2(150f, 150f));
            summonVfxImage = CreateTextureImage(summonRoot, "Summon VFX", "vfx_summon", new Vector2(0f, -176f), new Vector2(290f, 290f));
            summonVfxRect = summonVfxImage.GetComponent<RectTransform>();
            summonVfxImage.color = new Color(1f, 1f, 1f, 0.35f);
        }
    }

    public void ApplyButtonStyle(params Button[] buttons)
    {
        if (buttons == null)
        {
            return;
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button == null)
            {
                continue;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = GetSprite("ui_button_blue", new Vector4(10f, 10f, 10f, 10f));
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.72f, 0.84f, 1f, 1f);
            colors.disabledColor = new Color(0.42f, 0.45f, 0.52f, 0.65f);
            button.colors = colors;
        }
    }

    public void Refresh(MythwakeRuntimeArtState state)
    {
        var heroTexture = GetHeroTextureName(state.selectedHeroIndex);
        SetTexture(battleHeroImage, heroTexture);
        SetTexture(summonHeroImage, GetHeroTextureName(currentSummonHeroIndex));
        SetTexture(battleEnemyImage, currentEnemyTexture);
        SetTexture(battleVfxImage, currentVfxTexture);

        if (battleTitleText != null)
        {
            battleTitleText.text = currentBattleTitle;
        }

        if (battleMetaText != null)
        {
            var mode = state.backendRequestInProgress ? "Server resolving..." : currentBattleMeta;
            battleMetaText.text = $"{mode} | {state.selectedHeroName} Lv {state.selectedHeroLevel}+{state.selectedHeroAscension} | Power {state.teamPower}";
        }

        if (resourceText != null)
        {
            resourceText.text = $"Gold {state.gold}   Gems {state.gems}   Essence {state.mythEssence}";
        }

        if (dungeonText != null)
        {
            dungeonText.text = $"Gold F{state.goldDungeonFloor}     Essence F{state.essenceDungeonFloor}     Gear F{state.gearDungeonFloor}";
        }

        if (summonTitleText != null)
        {
            summonTitleText.text = "Shard Gate";
        }

        SetFillPercent(heroHpFill, heroHpPercent);
        SetFillPercent(enemyHpFill, enemyHpPercent);
    }

    public void PlayCombatResult(string mode, string label, CombatVisualResult result)
    {
        currentBattleTitle = label;
        currentEnemyTexture = GetEnemyTextureName(mode);
        currentVfxTexture = result.won ? "vfx_slash" : "vfx_magic";
        currentBattleMeta = result.won ? $"Cleared in {result.elapsedSeconds}s" : $"Failed after {result.elapsedSeconds}s";
        heroHpPercent = Mathf.Clamp01(result.heroHpPercent);
        enemyHpPercent = Mathf.Clamp01(result.enemyHpPercent);
        combatVfxTimer = 0.75f;
    }

    public void PlaySummonResult(int heroIndex, string label)
    {
        currentSummonHeroIndex = Mathf.Clamp(heroIndex, 0, HeroTextureNames.Length - 1);
        if (summonTitleText != null)
        {
            summonTitleText.text = label;
        }

        summonVfxTimer = 1.1f;
    }

    public void Tick(float deltaTime)
    {
        AnimateTransform(battleHeroRect, -260f, -158f, 0.8f, 3.2f);
        AnimateTransform(battleEnemyRect, 260f, -158f, 1.2f, 2.8f);

        if (combatVfxTimer > 0f)
        {
            combatVfxTimer = Mathf.Max(0f, combatVfxTimer - deltaTime);
        }

        if (summonVfxTimer > 0f)
        {
            summonVfxTimer = Mathf.Max(0f, summonVfxTimer - deltaTime);
        }

        AnimateVfx(battleVfxImage, battleVfxRect, combatVfxTimer, 190f);
        AnimateVfx(summonVfxImage, summonVfxRect, Mathf.Max(0.22f, summonVfxTimer), 290f);
    }

    private void CreateBackgroundLayers(RectTransform parent)
    {
        var sky = CreateTextureImage(parent, "Sky Layer", "bg_sky", Vector2.zero, new Vector2(900f, 380f));
        Stretch(sky.rectTransform, Vector2.zero);
        sky.color = new Color(0.55f, 0.75f, 1f, 0.62f);

        var mountains = CreateTextureImage(parent, "Mountain Layer", "bg_mountains", new Vector2(0f, -152f), new Vector2(760f, 118f));
        mountains.color = new Color(0.58f, 0.74f, 0.92f, 0.8f);

        var hills = CreateTextureImage(parent, "Hill Layer", "bg_hills", new Vector2(0f, -205f), new Vector2(760f, 104f));
        hills.color = new Color(0.58f, 0.75f, 0.62f, 0.88f);

        var clouds = CreateTextureImage(parent, "Cloud Layer", "bg_clouds", new Vector2(0f, -82f), new Vector2(760f, 70f));
        clouds.color = new Color(1f, 1f, 1f, 0.55f);

        CreateTextureImage(parent, "Castle Accent", "bg_castle", new Vector2(316f, -202f), new Vector2(86f, 86f));
        CreateTextureImage(parent, "Tree Accent", "bg_tree", new Vector2(-330f, -210f), new Vector2(88f, 112f));
    }

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        SetRect(rect, position, size, new Vector2(0.5f, 1f));

        var image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private RawImage CreateTextureImage(Transform parent, string name, string textureName, Vector2 position, Vector2 size)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);
        var rect = imageObject.GetComponent<RectTransform>();
        SetRect(rect, position, size, new Vector2(0.5f, 1f));

        var rawImage = imageObject.GetComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.texture = GetTexture(textureName);
        rawImage.color = Color.white;
        return rawImage;
    }

    private Image CreateHealthBar(Transform parent, string name, Vector2 position, Color fillColor)
    {
        var backObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backObject.transform.SetParent(parent, false);
        SetRect(backObject.GetComponent<RectTransform>(), position, new Vector2(170f, 14f), new Vector2(0.5f, 1f));

        var back = backObject.GetComponent<Image>();
        back.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
        back.raycastTarget = false;

        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(backObject.transform, false);
        var fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fill = fillObject.GetComponent<Image>();
        fill.color = fillColor;
        fill.raycastTarget = false;
        return fill;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, int size, Vector2 position, Vector2 rectSize, FontStyles style)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        SetRect(textObject.GetComponent<RectTransform>(), position, rectSize, new Vector2(0.5f, 1f));

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontSizeMin = 14;
        text.fontSizeMax = size;
        text.enableAutoSizing = true;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private Texture2D GetTexture(string textureName)
    {
        if (string.IsNullOrWhiteSpace(textureName))
        {
            return null;
        }

        if (textureCache.TryGetValue(textureName, out var cachedTexture))
        {
            return cachedTexture;
        }

        var texture = Resources.Load<Texture2D>(ResourceRoot + textureName);
        if (texture == null)
        {
            var sprite = Resources.Load<Sprite>(ResourceRoot + textureName);
            if (sprite != null)
            {
                texture = sprite.texture;
            }
        }

        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
        }

        textureCache[textureName] = texture;
        return texture;
    }

    private Sprite GetSprite(string textureName, Vector4 border)
    {
        var cacheKey = $"{textureName}:{border}";
        if (spriteCache.TryGetValue(cacheKey, out var cachedSprite))
        {
            return cachedSprite;
        }

        var texture = GetTexture(textureName);
        if (texture == null)
        {
            return null;
        }

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        spriteCache[cacheKey] = sprite;
        return sprite;
    }

    private void SetTexture(RawImage image, string textureName)
    {
        if (image != null)
        {
            image.texture = GetTexture(textureName);
        }
    }

    private static void SetFillPercent(Image fill, float percent)
    {
        if (fill == null)
        {
            return;
        }

        var rect = fill.GetComponent<RectTransform>();
        rect.anchorMax = new Vector2(Mathf.Clamp01(percent), 1f);
        rect.offsetMax = Vector2.zero;
    }

    private static void AnimateTransform(RectTransform rect, float baseX, float baseY, float phase, float amplitude)
    {
        if (rect == null)
        {
            return;
        }

        var offset = Mathf.Sin(Time.unscaledTime * 2.2f + phase) * amplitude;
        rect.anchoredPosition = new Vector2(baseX, baseY + offset);
    }

    private static void AnimateVfx(RawImage image, RectTransform rect, float timer, float baseSize)
    {
        if (image == null || rect == null)
        {
            return;
        }

        var visible = timer > 0f;
        var pulse = 0.55f + Mathf.Sin(Time.unscaledTime * 11f) * 0.22f;
        var alpha = visible ? Mathf.Clamp01(pulse) : 0f;
        var scale = visible ? 1f + (1f - Mathf.Clamp01(timer)) * 0.16f : 1f;
        image.color = new Color(1f, 1f, 1f, alpha);
        rect.sizeDelta = new Vector2(baseSize * scale, baseSize * scale);
    }

    private static void SetRect(RectTransform rectTransform, Vector2 position, Vector2 size, Vector2 anchor)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 padding)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = padding;
        rectTransform.offsetMax = -padding;
    }

    private static string GetHeroTextureName(int heroIndex)
    {
        return HeroTextureNames[Mathf.Clamp(heroIndex, 0, HeroTextureNames.Length - 1)];
    }

    private static string GetEnemyTextureName(string mode)
    {
        switch (mode)
        {
            case "gold_dungeon":
                return "enemy_gold";
            case "essence_dungeon":
                return "enemy_essence";
            case "gear_dungeon":
                return "enemy_gear";
            default:
                return "enemy_campaign";
        }
    }
}

public struct CombatVisualResult
{
    public bool won;
    public int elapsedSeconds;
    public float heroHpPercent;
    public float enemyHpPercent;
}
