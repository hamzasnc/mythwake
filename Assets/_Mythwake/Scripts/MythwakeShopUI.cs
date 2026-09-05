using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime storefront for the prototype.  It deliberately only presents offers and
/// purchase intent; a native IAP provider must validate a transaction before any
/// rewards are granted.
/// </summary>
public sealed class MythwakeShopUI : MonoBehaviour
{
    private enum ShopTab
    {
        Featured,
        Crystals,
        Bundles,
        BattlePass,
        Dev
    }

    private sealed class ShopOffer
    {
        public string Id;
        public string Title;
        public string Contents;
        public string Price;
        public string Icon;
        public bool BestValue;

        public ShopOffer(string id, string title, string contents, string price, string icon, bool bestValue = false)
        {
            Id = id;
            Title = title;
            Contents = contents;
            Price = price;
            Icon = icon;
            BestValue = bestValue;
        }
    }

    private const float ContentWidth = 964f;
    private const float ContentHeight = 1950f;
    private const string RuntimeArtResourceRoot = "Mythwake/Art/Runtime/";
    private const string HomeUiResourceRoot = "Mythwake/UI/HomeScreen/Generated/";
    private const string CurrencyResourceRoot = "Mythwake/UI/icons/";
    private const string BagUiResourceRoot = "Mythwake/UI/Bag/";
    private const string ShopIconResourceRoot = "Mythwake/UI/Shop/Icons/";
    private const string BattlePassUiResourceRoot = "Mythwake/UI/Shop/BattlePass/";
    private const string ReferenceArtworkResource = "Mythwake/UI/Shop/shop_reference_featured";

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    private IdlePrototypeController controller;
    private RectTransform root;
    private RectTransform[] contentRoots;
    private Button[] tabButtons;
    private TMP_Text[] tabLabels;
    private RectTransform developerToolsRoot;
    private RectTransform battlePassContentRoot;
    private RectTransform purchaseModalRoot;
    private TMP_Text purchaseModalTitle;
    private TMP_Text purchaseModalBody;
    private TMP_Text purchaseModalPrice;
    private RectTransform referenceLayer;
    private RectTransform referenceChromeHitLayer;
    private RectTransform battlePassArtworkLayer;
    private ScrollRect battlePassRewardScrollRect;
    private RectTransform battlePassRewardContent;
    private Texture2D battlePassRewardTierHeaderTexture;
    private Texture2D battlePassRewardFreeCardTexture;
    private Texture2D battlePassRewardPremiumCardTexture;
    private RectTransform battlePassProgressFill;
    private TMP_Text battlePassProgressLabel;
    private readonly TMP_Text[] battlePassMissionProgressLabels = new TMP_Text[3];
    private readonly TMP_Text[] battlePassMissionRightProgressLabels = new TMP_Text[3];
    private readonly RectTransform[] battlePassMissionProgressFills = new RectTransform[3];
    private readonly RectTransform[] battlePassFreeRewardStatePanels = new RectTransform[11];
    private readonly TMP_Text[] battlePassFreeRewardStateLabels = new TMP_Text[11];
    private readonly int[] battlePassMissionProgress = new int[3];
    private static readonly int[] BattlePassMissionTargets = { 15, 3, 1 };
    private static readonly int[] BattlePassMissionXp = { 4, 10, 20 };
    private int battlePassXp;
    private RectTransform tabContentLayer;
    private RectTransform shopScrollViewport;
    private RectTransform shopScrollContent;
    private ScrollRect shopScrollRect;
    private RectTransform referenceTabInactiveMask;
    private RectTransform referenceTabHighlight;
    private RectTransform referenceTabTextMask;
    private TMP_Text referenceTabSelectedLabel;
    private readonly Dictionary<string, RectTransform> shopOfferRibbons = new Dictionary<string, RectTransform>();
    private CanvasGroup generatedUiGroup;
    private bool referenceArtworkLoaded;
    private bool battlePassArtworkLoaded;
    private bool battlePassArtworkSelected;
    private bool referenceTabSelected;
    private bool built;

    /// <summary>
    /// The featured storefront is supplied as the approved visual reference artwork.
    /// The transparent hit areas below keep the tabs, offers and navigation live while
    /// the artwork guarantees a pixel-stable presentation on the portrait tester build.
    /// </summary>
    public bool UsesReferenceArtwork =>
        (referenceArtworkLoaded && referenceLayer != null && referenceLayer.gameObject.activeSelf) ||
        (battlePassArtworkLoaded && battlePassArtworkLayer != null && battlePassArtworkLayer.gameObject.activeSelf);

    public bool ShouldShowReferenceArtwork => referenceArtworkLoaded;

    public void Build(IdlePrototypeController owner)
    {
        if (built)
        {
            return;
        }

        controller = owner;
        built = true;

        root = CreatePanel(transform, "Premium Shop Experience", new Vector2(0f, 25f), new Vector2(1020f, 1470f), Color.clear, false);
        generatedUiGroup = root.gameObject.AddComponent<CanvasGroup>();
        generatedUiGroup.blocksRaycasts = true;

        var title = CreateText(root, "Shop Title", "SHOP", 56, new Vector2(0f, -4f), new Vector2(700f, 70f), new Color(1f, 0.86f, 0.56f), FontStyles.Bold);
        title.textWrappingMode = TextWrappingModes.NoWrap;

        CreatePanel(root, "Shop Title Rule Left", new Vector2(-338f, -40f), new Vector2(176f, 3f), new Color(0.75f, 0.49f, 0.18f, 0.8f), false);
        CreatePanel(root, "Shop Title Rule Right", new Vector2(338f, -40f), new Vector2(176f, 3f), new Color(0.75f, 0.49f, 0.18f, 0.8f), false);
        CreatePanel(root, "Shop Title Gem", new Vector2(0f, -40f), new Vector2(12f, 12f), new Color(0.2f, 0.89f, 0.95f, 0.95f), false);

        BuildTabBar();
        BuildContentRoots();
        BuildFeaturedContent();
        BuildCrystalContent();
        BuildBundleContent();
        BuildBattlePassContent();
        BuildDeveloperContent();
        BuildPurchaseModal();
        BuildReferenceArtwork();
        BuildBattlePassArtwork();
        SelectTab(ShopTab.Featured);
    }

    public void BindProgression(TMP_Text[] dailyMissionTexts, Button[] dailyMissionButtons, TMP_Text battlePassProgressText, TMP_Text[] battlePassRewardTexts, Button[] battlePassRewardButtons)
    {
        if (battlePassContentRoot == null)
        {
            return;
        }

        if (battlePassProgressText != null)
        {
            MoveToContent(battlePassProgressText.transform, battlePassContentRoot, new Vector2(0f, -290f), new Vector2(830f, 42f));
            battlePassProgressText.color = new Color(0.76f, 0.91f, 1f);
            battlePassProgressText.fontStyle = FontStyles.Bold;
            ConfigureTextFit(battlePassProgressText, 15f, 21f);
            battlePassProgressText.gameObject.SetActive(true);
        }

        if (dailyMissionButtons != null)
        {
            for (var i = 0; i < dailyMissionButtons.Length; i++)
            {
                var button = dailyMissionButtons[i];
                if (button == null)
                {
                    continue;
                }

                MoveToContent(button.transform, battlePassContentRoot, new Vector2(0f, -449f - (i * 86f)), new Vector2(850f, 74f));
                button.gameObject.SetActive(true);
                StyleExistingButton(button, false);

                var missionIcon = i == 0 ? "icon_weapon" : i == 1 ? "icon_gems" : "mythic_gem";
                CreateOfferArt(button.transform, "Mission Icon", missionIcon, new Vector2(-384f, -37f), new Vector2(42f, 42f));

                if (dailyMissionTexts != null && i < dailyMissionTexts.Length && dailyMissionTexts[i] != null)
                {
                    var text = dailyMissionTexts[i];
                    MoveToContent(text.transform, button.GetComponent<RectTransform>(), new Vector2(34f, -7f), new Vector2(744f, 56f));
                    text.color = new Color(1f, 0.89f, 0.68f);
                    ConfigureTextFit(text, 15f, 21f);
                    text.alignment = TextAlignmentOptions.Left;
                    text.gameObject.SetActive(true);
                }
            }
        }

        if (battlePassRewardButtons == null)
        {
            return;
        }

        for (var i = 0; i < battlePassRewardButtons.Length; i++)
        {
            var button = battlePassRewardButtons[i];
            if (button == null)
            {
                continue;
            }

            var column = i % 2;
            var row = i / 2;
            var isLast = i == battlePassRewardButtons.Length - 1 && battlePassRewardButtons.Length % 2 == 1;
            var x = isLast ? 0f : column == 0 ? -218f : 218f;
            var width = isLast ? 850f : 414f;
            MoveToContent(button.transform, battlePassContentRoot, new Vector2(x, -765f - (row * 122f)), new Vector2(width, 108f));
            button.gameObject.SetActive(true);
            StyleExistingButton(button, false);

            var rewardIcon = i == 0 ? "icon_gold" : i == 1 ? "mythic_gem" : i == 2 ? "icon_essence" : i == 3 ? "icon_weapon" : "exp_shard";
            CreateOfferArt(button.transform, "Reward Icon", rewardIcon, new Vector2(-width * 0.38f, -54f), new Vector2(54f, 54f));

            if (battlePassRewardTexts != null && i < battlePassRewardTexts.Length && battlePassRewardTexts[i] != null)
            {
                var text = battlePassRewardTexts[i];
                var textOffset = isLast ? 38f : 35f;
                MoveToContent(text.transform, button.GetComponent<RectTransform>(), new Vector2(textOffset, -8f), new Vector2(width - 86f, 88f));
                text.color = new Color(1f, 0.89f, 0.68f);
                ConfigureTextFit(text, 14f, 20f);
                text.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Applies the server-controlled merchandising flags to the already-built
    /// storefront.  Offer artwork and copy remain local presentation assets, while
    /// the database can turn a ribbon on/off or rename it without a new client build.
    /// </summary>
    public void BindShopOffers(MythwakeShopOfferDefinitionDto[] definitions)
    {
        if (definitions == null)
        {
            return;
        }

        for (var i = 0; i < definitions.Length; i++)
        {
            var definition = definitions[i];
            var key = ShopOfferKey(definition.tab, definition.offerId);
            if (!shopOfferRibbons.TryGetValue(key, out var ribbon) || ribbon == null)
            {
                continue;
            }

            var visible = definition.topPick || !string.IsNullOrWhiteSpace(definition.badgeLabel);
            ribbon.gameObject.SetActive(visible);
            var label = ribbon.GetComponentInChildren<TMP_Text>(true);
            if (label != null && !string.IsNullOrWhiteSpace(definition.badgeLabel))
            {
                label.text = definition.badgeLabel;
            }
        }
    }

    public void BindDeveloperTools(RectTransform backendPanel, Button resetButton, params Button[] debugButtons)
    {
        if (developerToolsRoot == null)
        {
            return;
        }

        if (backendPanel != null)
        {
            MoveToContent(backendPanel, developerToolsRoot, new Vector2(0f, -142f), new Vector2(860f, 360f));
            backendPanel.gameObject.SetActive(true);
        }

        if (resetButton != null)
        {
            MoveToContent(resetButton.transform, developerToolsRoot, new Vector2(0f, -550f), new Vector2(320f, 60f));
            resetButton.gameObject.SetActive(true);
            StyleExistingButton(resetButton, false);
        }

        if (debugButtons == null)
        {
            return;
        }

        for (var i = 0; i < debugButtons.Length; i++)
        {
            var button = debugButtons[i];
            if (button == null)
            {
                continue;
            }

            var column = i % 2;
            var row = i / 2;
            MoveToContent(button.transform, developerToolsRoot, new Vector2(column == 0 ? -218f : 218f, -638f - (row * 72f)), new Vector2(414f, 56f));
            button.gameObject.SetActive(true);
            StyleExistingButton(button, false);
        }
    }

    public void ShowDeveloperTools()
    {
        SelectTab(ShopTab.Dev);
    }

    private void BuildTabBar()
    {
        var labels = new[] { "FEATURED", "CRYSTALS", "BUNDLES", "BATTLE PASS", "DEV" };
        tabButtons = new Button[labels.Length];
        tabLabels = new TMP_Text[labels.Length];
        const float tabWidth = 184f;
        const float spacing = 190f;
        const float startX = -380f;

        for (var i = 0; i < labels.Length; i++)
        {
            var tab = (ShopTab)i;
            var button = CreateButton(root, $"Shop Tab {labels[i]}", labels[i], new Vector2(startX + (i * spacing), -82f), new Vector2(tabWidth, 66f), false);
            var capturedTab = tab;
            button.onClick.AddListener(() => SelectTab(capturedTab));
            tabButtons[i] = button;
            tabLabels[i] = button.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void BuildContentRoots()
    {
        contentRoots = new RectTransform[Enum.GetValues(typeof(ShopTab)).Length];
        for (var i = 0; i < contentRoots.Length; i++)
        {
            contentRoots[i] = CreatePanel(root, $"Shop {(ShopTab)i} Content", new Vector2(0f, -130f), new Vector2(ContentWidth, ContentHeight), Color.clear, false);
        }

        battlePassContentRoot = contentRoots[(int)ShopTab.BattlePass];
        developerToolsRoot = contentRoots[(int)ShopTab.Dev];
    }

    private void BuildFeaturedContent()
    {
        var parent = contentRoots[(int)ShopTab.Featured];
        var hero = CreateFramedPanel(parent, "Featured Myth Crystal Bundle", new Vector2(0f, 0f), new Vector2(950f, 410f), new Color(0.025f, 0.055f, 0.08f, 0.98f));
        CreateRibbon(hero, "BEST VALUE", new Vector2(-335f, -20f));
        CreateOfferArt(hero, "Featured Chest Art", "home_treasure_chest_button", new Vector2(-255f, -155f), new Vector2(285f, 270f));

        var title = CreateText(hero, "Featured Title", "Myth Crystal Bundle", 36, new Vector2(130f, -42f), new Vector2(570f, 58f), new Color(1f, 0.87f, 0.59f), FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Left;
        var subtitle = CreateText(hero, "Featured Subtitle", "The fastest way to power up your journey.", 19, new Vector2(130f, -85f), new Vector2(570f, 34f), new Color(0.67f, 0.86f, 0.96f), FontStyles.Normal);
        subtitle.alignment = TextAlignmentOptions.Left;

        CreateRewardChip(hero, "Crystal Reward", "mythic_gem", "2,500", new Vector2(-35f, -142f));
        CreateRewardChip(hero, "Gold Reward", "gold_coin", "150K", new Vector2(122f, -142f));
        CreateRewardChip(hero, "Essence Reward", "icon_essence", "10", new Vector2(279f, -142f));
        CreateRewardChip(hero, "Crest Reward", "icon_weapon", "5", new Vector2(436f, -142f));
        CreatePriceButton(hero, new ShopOffer("myth_crystal_bundle", "Myth Crystal Bundle", "2,500 Myth Crystals, 150K Gold, 10 Essence and 5 Crests", "€9.99", "home_treasure_chest_button", true), new Vector2(184f, -300f), new Vector2(390f, 78f));

        var offers = new[]
        {
            new ShopOffer("starter_pack", "Starter Pack", "500 Crystals\n25K Gold · 5 Essence", "€2.99", "icon_gold"),
            new ShopOffer("crystal_cache", "Crystal Cache", "1,100 Crystals\n60K Gold · 5 Essence", "€4.99", "icon_gems"),
            new ShopOffer("adventurer_bundle", "Adventurer Bundle", "2,200 Crystals\n120K Gold · 15 Essence", "€14.99", "home_shop_button"),
            new ShopOffer("legendary_chest", "Legendary Chest", "5,000 Crystals\n250K Gold · 25 Essence", "€19.99", "home_treasure_chest_button")
        };

        BuildOfferGrid(parent, offers, 0f, -440f, "featured");
        var restore = CreateButton(parent, "Restore Purchases", "Restore purchases", new Vector2(0f, -1110f), new Vector2(350f, 50f), true);
        restore.onClick.AddListener(ShowRestorePurchasesNotice);
    }

    private void BuildCrystalContent()
    {
        var parent = contentRoots[(int)ShopTab.Crystals];
        CreateStorefrontHeaderPlate(parent, "CRYSTAL MARKET", "Instant delivery  •  secure checkout  •  bonus crystals", new Color(0.12f, 0.75f, 0.95f, 0.9f), "MYTHIC CURRENCY");
        CreateSectionHeader(parent, "MYTH CRYSTALS", "Choose a crystal cache for summons, heroes and special offers.");
        var offers = new[]
        {
            new ShopOffer("crystal_pouch", "Crystal Pouch", "100 Myth Crystals\nStarter stash", "€0.99", "shop_icon_crystal_altar"),
            new ShopOffer("crystal_pack", "Crystal Pack", "500 Myth Crystals\n+ 5% bonus", "€3.99", "shop_icon_crystal_vault"),
            new ShopOffer("crystal_cache", "Crystal Cache", "1,100 Myth Crystals\n+ 10% bonus", "€7.99", "shop_icon_crystal_altar", true),
            new ShopOffer("crystal_vault", "Crystal Vault", "2,500 Myth Crystals\n+ 25% bonus", "€14.99", "shop_icon_crystal_vault"),
            new ShopOffer("crystal_reserve", "Crystal Reserve", "5,000 Myth Crystals\n+ 35% bonus", "€24.99", "shop_icon_crystal_altar"),
            new ShopOffer("crystal_treasury", "Crystal Treasury", "12,000 Myth Crystals\n+ 45% bonus", "€49.99", "shop_icon_crystal_vault", true),
            new ShopOffer("crystal_hoard", "Crystal Hoard", "25,000 Myth Crystals\n+ 55% bonus", "€89.99", "shop_icon_crystal_altar"),
            new ShopOffer("crystal_relic", "Ancient Relic Cache", "50,000 Myth Crystals\n+ 70% bonus", "€149.99", "shop_icon_crystal_vault"),
            new ShopOffer("crystal_ascendant", "Ascendant Crystals", "100,000 Myth Crystals\n+ 90% bonus", "€249.99", "shop_icon_crystal_altar"),
            new ShopOffer("crystal_eternal", "Eternal Crystal Vault", "250,000 Myth Crystals\n+ 120% bonus", "€499.99", "shop_icon_crystal_vault")
        };

        BuildOfferGrid(parent, offers, 0f, -172f, "crystals");
        var note = CreateText(parent, "Crystal Store Note", "Crystals are delivered after the platform confirms your purchase.", 18, new Vector2(0f, -1810f), new Vector2(840f, 42f), new Color(0.62f, 0.78f, 0.88f), FontStyles.Normal);
        note.enableAutoSizing = true;
        note.fontSizeMin = 14;
        note.fontSizeMax = 18;
    }

    private void BuildBundleContent()
    {
        var parent = contentRoots[(int)ShopTab.Bundles];
        CreateStorefrontHeaderPlate(parent, "ADVENTURER MARKET", "Rotating collections  •  extra value  •  progression-ready", new Color(0.95f, 0.67f, 0.22f, 0.92f), "LIMITED COLLECTIONS");
        CreateSectionHeader(parent, "ADVENTURER BUNDLES", "Limited collections with crystals, gold and progression resources.");
        var offers = new[]
        {
            new ShopOffer("daily_deal", "Daily Deal", "250 Crystals\n15K Gold · 2 Essence", "€1.99", "shop_icon_adventurer_satchel"),
            new ShopOffer("hero_bundle", "Hero Bundle", "1,000 Crystals\nHero Shard Chest ×2", "€8.99", "shop_icon_bundle_chest"),
            new ShopOffer("adventurer_bundle", "Adventurer Bundle", "2,200 Crystals\n120K Gold · 15 Essence", "€14.99", "shop_icon_adventurer_satchel", true),
            new ShopOffer("legendary_chest", "Legendary Chest", "5,000 Crystals\n250K Gold · 25 Essence", "€19.99", "shop_icon_bundle_chest"),
            new ShopOffer("dungeon_expedition", "Dungeon Expedition", "3,500 Crystals\n150K Gold · 20 Essence", "€24.99", "shop_icon_adventurer_satchel"),
            new ShopOffer("guild_foundry", "Guild Foundry Pack", "4,500 Crystals\n200K Gold · 30 Essence", "€34.99", "shop_icon_bundle_chest"),
            new ShopOffer("royal_war_chest", "Royal War Chest", "8,000 Crystals\n400K Gold · 45 Essence", "€49.99", "shop_icon_bundle_chest", true),
            new ShopOffer("mythic_arsenal", "Mythic Arsenal", "12,000 Crystals\n650K Gold · 60 Essence", "€69.99", "shop_icon_adventurer_satchel"),
            new ShopOffer("worldbreaker_cache", "Worldbreaker Cache", "20,000 Crystals\n1M Gold · 90 Essence", "€99.99", "shop_icon_bundle_chest"),
            new ShopOffer("founder_legacy", "Founder’s Legacy", "35,000 Crystals\n2M Gold · 120 Essence", "€149.99", "shop_icon_adventurer_satchel")
        };

        BuildOfferGrid(parent, offers, 0f, -172f, "bundles");
    }

    private void BuildBattlePassContent()
    {
        var parent = battlePassContentRoot;
        CreateSectionHeader(parent, "BATTLE PASS", "Complete daily missions to unlock the reward track.");
        var seasonHero = CreateFramedPanel(parent, "Battle Pass Season Hero", new Vector2(0f, -128f), new Vector2(900f, 176f), new Color(0.018f, 0.055f, 0.095f, 0.98f));
        CreatePanel(seasonHero, "Hero Cyan Glow", new Vector2(-222f, -88f), new Vector2(300f, 4f), new Color(0.1f, 0.78f, 0.95f, 0.72f), false);
        CreateText(seasonHero, "Battle Pass Season Label", "SEASON 01  •  SPELLFORGED FRONTIER", 15, new Vector2(-168f, -38f), new Vector2(430f, 26f), new Color(0.47f, 0.87f, 1f), FontStyles.Bold);
        var heroTitle = CreateText(seasonHero, "Battle Pass Season Title", "The Arcane Trail", 30, new Vector2(-165f, -70f), new Vector2(450f, 42f), new Color(1f, 0.88f, 0.58f), FontStyles.Bold);
        heroTitle.alignment = TextAlignmentOptions.Left;
        var heroCopy = CreateText(seasonHero, "Battle Pass Season Copy", "Earn XP from daily missions and claim rewards on every tier.", 15, new Vector2(-165f, -103f), new Vector2(460f, 28f), new Color(0.7f, 0.86f, 0.96f), FontStyles.Normal);
        heroCopy.alignment = TextAlignmentOptions.Left;
        CreateOfferArt(seasonHero, "Battle Pass Season Crest", "shop_icon_battle_pass_crest", new Vector2(346f, -90f), new Vector2(154f, 142f));
        var seasonTag = CreateText(seasonHero, "Battle Pass Season Timer", "12 DAYS LEFT", 14, new Vector2(218f, -32f), new Vector2(170f, 28f), new Color(1f, 0.78f, 0.34f), FontStyles.Bold);
        seasonTag.alignment = TextAlignmentOptions.Center;
        var unlock = CreateButton(seasonHero, "Unlock Battle Pass", "UNLOCK PASS  ·  €9.99", new Vector2(32f, -122f), new Vector2(270f, 48f), false, true);
        unlock.onClick.AddListener(() => ShowPurchaseNotice(new ShopOffer("battle_pass_premium", "Premium Battle Pass", "Unlock the premium reward lane for Season 01 and claim every earned tier.", "€9.99", "shop_icon_battle_pass_crest")));

        var progressFrame = CreateFramedPanel(parent, "Battle Pass Progress Frame", new Vector2(0f, -300f), new Vector2(880f, 82f), new Color(0.035f, 0.09f, 0.12f, 0.96f));
        CreateText(progressFrame, "Battle Pass Progress Caption", "SEASON PROGRESS", 13, new Vector2(-340f, -18f), new Vector2(180f, 22f), new Color(0.48f, 0.86f, 0.96f), FontStyles.Bold);
        CreateProgressBar(progressFrame, "Battle Pass XP Bar", new Vector2(36f, -47f), new Vector2(600f, 16f), 0.21f, new Color(0.13f, 0.82f, 0.95f, 0.95f));
        CreateText(progressFrame, "Battle Pass Tier Text", "TIER 1 / 10", 14, new Vector2(350f, -42f), new Vector2(150f, 26f), new Color(1f, 0.86f, 0.54f), FontStyles.Bold);

        CreateFramedPanel(parent, "Battle Pass Missions Panel", new Vector2(0f, -420f), new Vector2(900f, 298f), new Color(0.018f, 0.042f, 0.058f, 0.98f));
        var daily = CreateText(parent, "Battle Pass Daily Label", "DAILY MISSIONS", 22, new Vector2(-350f, -400f), new Vector2(300f, 38f), new Color(1f, 0.84f, 0.5f), FontStyles.Bold);
        daily.alignment = TextAlignmentOptions.Left;
        var missionHint = CreateText(parent, "Battle Pass Mission Hint", "Complete objectives to earn +40 XP each", 14, new Vector2(225f, -400f), new Vector2(420f, 28f), new Color(0.62f, 0.81f, 0.92f), FontStyles.Italic);
        missionHint.alignment = TextAlignmentOptions.Right;

        CreateFramedPanel(parent, "Battle Pass Rewards Panel", new Vector2(0f, -805f), new Vector2(900f, 450f), new Color(0.018f, 0.042f, 0.058f, 0.98f));
        var rewards = CreateText(parent, "Battle Pass Rewards Label", "REWARD TRACK", 22, new Vector2(-350f, -700f), new Vector2(300f, 38f), new Color(1f, 0.84f, 0.5f), FontStyles.Bold);
        rewards.alignment = TextAlignmentOptions.Left;
        var rewardHint = CreateText(parent, "Battle Pass Rewards Hint", "Claim each unlocked tier before the season ends", 14, new Vector2(225f, -700f), new Vector2(420f, 28f), new Color(0.62f, 0.81f, 0.92f), FontStyles.Italic);
        rewardHint.alignment = TextAlignmentOptions.Right;
        CreateText(parent, "Battle Pass Free Lane Label", "FREE TRACK", 14, new Vector2(-220f, -744f), new Vector2(160f, 24f), new Color(0.52f, 0.86f, 0.96f), FontStyles.Bold);
        CreateText(parent, "Battle Pass Premium Lane Label", "PREMIUM TRACK", 14, new Vector2(220f, -744f), new Vector2(180f, 24f), new Color(1f, 0.77f, 0.32f), FontStyles.Bold);
        CreateText(parent, "Battle Pass Footer", "More tiers unlock as your XP grows  •  swipe to explore the full season", 14, new Vector2(0f, -1160f), new Vector2(860f, 30f), new Color(0.57f, 0.75f, 0.88f), FontStyles.Italic);
    }

    private void BuildDeveloperContent()
    {
        var parent = developerToolsRoot;
        CreateSectionHeader(parent, "DEVELOPER TOOLS", "Internal account, backend and prototype controls. Not part of the player storefront.");
        CreateFramedPanel(parent, "Developer Backend Frame", new Vector2(0f, -116f), new Vector2(900f, 392f), new Color(0.055f, 0.045f, 0.075f, 0.98f));
        var tools = CreateText(parent, "Developer Prototype Tools Label", "PROTOTYPE RESOURCES", 22, new Vector2(-300f, -505f), new Vector2(410f, 38f), new Color(1f, 0.84f, 0.5f), FontStyles.Bold);
        tools.alignment = TextAlignmentOptions.Left;
        var warning = CreateText(parent, "Developer Tool Warning", "Developer-only: changes here are never shown in the player shop.", 17, new Vector2(0f, -835f), new Vector2(820f, 34f), new Color(0.75f, 0.84f, 0.98f), FontStyles.Italic);
        warning.enableAutoSizing = true;
        warning.fontSizeMin = 13;
        warning.fontSizeMax = 17;
    }

    private void BuildPurchaseModal()
    {
        purchaseModalRoot = CreatePanel(transform, "Shop Purchase Preview", Vector2.zero, new Vector2(1080f, 1920f), new Color(0.005f, 0.008f, 0.016f, 0.78f), true);
        purchaseModalRoot.anchorMin = new Vector2(0.5f, 0.5f);
        purchaseModalRoot.anchorMax = new Vector2(0.5f, 0.5f);
        purchaseModalRoot.pivot = new Vector2(0.5f, 0.5f);
        purchaseModalRoot.anchoredPosition = Vector2.zero;
        purchaseModalRoot.sizeDelta = new Vector2(1080f, 1920f);

        var dialog = CreateFramedPanel(purchaseModalRoot, "Purchase Preview Dialog", new Vector2(0f, -520f), new Vector2(850f, 460f), new Color(0.035f, 0.065f, 0.09f, 0.99f));
        purchaseModalTitle = CreateText(dialog, "Purchase Preview Title", string.Empty, 32, new Vector2(0f, -52f), new Vector2(720f, 50f), new Color(1f, 0.87f, 0.59f), FontStyles.Bold);
        purchaseModalBody = CreateText(dialog, "Purchase Preview Body", string.Empty, 21, new Vector2(0f, -142f), new Vector2(700f, 138f), new Color(0.75f, 0.88f, 0.98f), FontStyles.Normal);
        purchaseModalBody.enableAutoSizing = true;
        purchaseModalBody.fontSizeMin = 15;
        purchaseModalBody.fontSizeMax = 21;
        purchaseModalPrice = CreateText(dialog, "Purchase Preview Price", string.Empty, 34, new Vector2(0f, -262f), new Vector2(500f, 52f), new Color(1f, 0.76f, 0.22f), FontStyles.Bold);
        var close = CreateButton(dialog, "Purchase Preview Close", "Close", new Vector2(0f, -348f), new Vector2(300f, 64f), false);
        close.onClick.AddListener(() =>
        {
            purchaseModalRoot.gameObject.SetActive(false);
            SetReferenceArtworkVisible(true);
        });
        purchaseModalRoot.gameObject.SetActive(false);
    }

    private void BuildReferenceArtwork()
    {
        var texture = Resources.Load<Texture2D>(ReferenceArtworkResource);
        if (texture == null)
        {
            Debug.LogWarning($"Shop reference artwork is missing at Resources/{ReferenceArtworkResource}. Falling back to the generated UI.");
            return;
        }

        var canvasParent = transform.parent != null ? transform.parent : transform;
        referenceLayer = new GameObject("Shop Reference Artwork Layer", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RectTransform>();
        referenceLayer.SetParent(canvasParent, false);
        referenceLayer.anchorMin = new Vector2(0.5f, 0.5f);
        referenceLayer.anchorMax = new Vector2(0.5f, 0.5f);
        referenceLayer.pivot = new Vector2(0.5f, 0.5f);
        referenceLayer.anchoredPosition = Vector2.zero;
        referenceLayer.sizeDelta = new Vector2(1080f, 1920f);

        var artwork = referenceLayer.GetComponent<RawImage>();
        artwork.texture = texture;
        artwork.color = Color.white;
        artwork.raycastTarget = false;
        referenceLayer.SetAsLastSibling();

        // The generated layout remains in the project as a functional fallback and
        // for non-artwork tabs, but the reference image is the player-facing featured
        // storefront. Its controls are replaced with transparent, working hit areas.
        generatedUiGroup.alpha = 0f;
        generatedUiGroup.interactable = false;
        generatedUiGroup.blocksRaycasts = false;
        referenceArtworkLoaded = true;
        referenceTabSelected = true;
        BuildReferenceHitAreas();
        BuildReferenceTabDecorations();
        BuildSharedTabContentLayer();
        referenceLayer.gameObject.SetActive(false);
    }

    private void BuildBattlePassArtwork()
    {
        var slices = new[]
        {
            new BattlePassSlice("battlepass_variant1_header", 0f, 315f),
            new BattlePassSlice("battlepass_variant1_season", 315f, 365f),
            new BattlePassSlice("battlepass_variant1_missions", 680f, 420f),
            new BattlePassSlice("battlepass_variant1_rewards", 1100f, 570f),
            new BattlePassSlice("battlepass_variant1_footer", 1670f, 250f)
        };

        for (var i = 0; i < slices.Length; i++)
        {
            if (Resources.Load<Texture2D>(BattlePassUiResourceRoot + slices[i].ResourceName) == null)
            {
                Debug.LogWarning($"Battle Pass variant 1 slice is missing at Resources/{BattlePassUiResourceRoot}{slices[i].ResourceName}.png");
                return;
            }
        }

        var canvasParent = referenceLayer != null && referenceLayer.parent != null
            ? referenceLayer.parent
            : transform.parent != null ? transform.parent : transform;
        battlePassArtworkLayer = new GameObject("Battle Pass Variant 1 Artwork Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        battlePassArtworkLayer.SetParent(canvasParent, false);
        battlePassArtworkLayer.anchorMin = new Vector2(0.5f, 0.5f);
        battlePassArtworkLayer.anchorMax = new Vector2(0.5f, 0.5f);
        battlePassArtworkLayer.pivot = new Vector2(0.5f, 0.5f);
        battlePassArtworkLayer.anchoredPosition = Vector2.zero;
        battlePassArtworkLayer.sizeDelta = new Vector2(1080f, 1920f);

        for (var i = 0; i < slices.Length; i++)
        {
            CreateBattlePassArtworkSlice(battlePassArtworkLayer, slices[i]);
        }

        BuildBattlePassInteractiveOverlays();
        CreateBattlePassHitAreas();
        var viewport = CreatePanel(canvasParent, "Battle Pass Shared Body Viewport", new Vector2(0, -310), new Vector2(1020, 1328), Color.clear, false);
        viewport.gameObject.AddComponent<RectMask2D>();
        battlePassArtworkLayer.SetParent(viewport, false);
        const float bodyScale = 1328f / (1670f - 310f);
        PlaceTop(battlePassArtworkLayer, new Vector2(0, 310 * bodyScale), new Vector2(1080, 1920));
        battlePassArtworkLayer.localScale = new Vector3(1, bodyScale, 1);
        battlePassArtworkLoaded = true;
        battlePassArtworkSelected = false;
        battlePassArtworkLayer.gameObject.SetActive(false);
    }

    private static void CreateBattlePassArtworkSlice(Transform parent, BattlePassSlice slice)
    {
        var texture = Resources.Load<Texture2D>(BattlePassUiResourceRoot + slice.ResourceName);
        var imageObject = new GameObject(slice.ResourceName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);
        var rect = imageObject.GetComponent<RectTransform>();
        PlaceTop(rect, new Vector2(0f, -slice.Top), new Vector2(1080f, slice.Height));
        var image = imageObject.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void BuildBattlePassInteractiveOverlays()
    {
        if (battlePassArtworkLayer == null)
        {
            return;
        }

        // Cover only the values that change at runtime.  The surrounding artwork
        // remains the approved variant-1 slice, while these small overlays make the
        // page behave like a real battle pass instead of a flat screenshot.
        CreatePanel(battlePassArtworkLayer, "Battle Pass XP Dynamic Cover", new Vector2(0f, -812f), new Vector2(900f, 86f), new Color(0.005f, 0.018f, 0.028f, 1f), false);
        battlePassProgressFill = CreateBattlePassDynamicProgressBar(battlePassArtworkLayer, new Vector2(0f, -832f), new Vector2(850f, 24f));
        battlePassProgressLabel = CreateText(battlePassArtworkLayer, "Battle Pass Dynamic XP Label", string.Empty, 22, new Vector2(0f, -861f), new Vector2(300f, 30f), new Color(0.66f, 0.92f, 1f), FontStyles.Bold);

        var missionProgressTop = new[] { 1000f, 1092f, 1185f };
        for (var i = 0; i < missionProgressTop.Length; i++)
        {
            CreatePanel(battlePassArtworkLayer, "Battle Pass Mission Progress Cover " + i, new Vector2(-286f, -missionProgressTop[i]), new Vector2(150f, 34f), new Color(0.08f, 0.075f, 0.06f, 1f), false);
            battlePassMissionProgressLabels[i] = CreateText(battlePassArtworkLayer, "Battle Pass Mission Progress " + i, string.Empty, 18, new Vector2(-286f, -(missionProgressTop[i] + 1f)), new Vector2(116f, 28f), new Color(1f, 0.89f, 0.68f), FontStyles.Bold);
            battlePassMissionProgressLabels[i].alignment = TextAlignmentOptions.Left;

            // The artwork's right-side counter sits just above its bar; cover that
            // printed value so it cannot disagree with the live mission state.
            var rightCounterTop = missionProgressTop[i] - 28f;
            CreatePanel(battlePassArtworkLayer, "Battle Pass Mission Right Progress Cover " + i, new Vector2(354f, -rightCounterTop), new Vector2(140f, 30f), new Color(0.08f, 0.075f, 0.06f, 1f), false);
            battlePassMissionRightProgressLabels[i] = CreateText(battlePassArtworkLayer, "Battle Pass Mission Right Progress " + i, string.Empty, 18, new Vector2(354f, -(rightCounterTop + 1f)), new Vector2(120f, 28f), new Color(1f, 0.89f, 0.68f), FontStyles.Bold);
            battlePassMissionRightProgressLabels[i].alignment = TextAlignmentOptions.Center;
            battlePassMissionProgressFills[i] = CreateBattlePassDynamicProgressBar(battlePassArtworkLayer, new Vector2(354f, -(missionProgressTop[i] + 2f)), new Vector2(205f, 18f));
        }

        BuildBattlePassRewardScroller();
        UpdateBattlePassInteractiveState();
    }

    private static RectTransform CreateBattlePassDynamicProgressBar(Transform parent, Vector2 position, Vector2 size)
    {
        var track = CreatePanel(parent, "Battle Pass Dynamic XP Track", position, size, new Color(0.01f, 0.025f, 0.04f, 1f), false);
        var fill = CreatePanel(track, "Battle Pass Dynamic XP Fill", new Vector2(-size.x * 0.5f + 4f, -4f), new Vector2(8f, size.y - 8f), new Color(0.12f, 0.83f, 0.97f, 1f), false);
        CreatePanel(track, "Battle Pass Dynamic XP Shine", new Vector2(-size.x * 0.5f + 4f, -6f), new Vector2(8f, 2f), new Color(1f, 1f, 1f, 0.42f), false);
        return fill;
    }

    private void BuildBattlePassRewardScroller()
    {
        var viewport = CreatePanel(battlePassArtworkLayer, "Battle Pass Reward Track Swipe Viewport", new Vector2(0f, -1250f), new Vector2(1000f, 420f), new Color(0.008f, 0.02f, 0.03f, 1f), true);
        viewport.gameObject.AddComponent<RectMask2D>();
        battlePassRewardScrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        battlePassRewardScrollRect.horizontal = true;
        battlePassRewardScrollRect.vertical = false;
        battlePassRewardScrollRect.inertia = true;
        battlePassRewardScrollRect.movementType = ScrollRect.MovementType.Clamped;
        battlePassRewardScrollRect.scrollSensitivity = 2.2f;

        battlePassRewardContent = new GameObject("Battle Pass Reward Track Swipe Content", typeof(RectTransform)).GetComponent<RectTransform>();
        battlePassRewardContent.SetParent(viewport, false);
        battlePassRewardContent.anchorMin = new Vector2(0f, 1f);
        battlePassRewardContent.anchorMax = new Vector2(0f, 1f);
        battlePassRewardContent.pivot = new Vector2(0f, 1f);
        battlePassRewardContent.anchoredPosition = Vector2.zero;
        battlePassRewardContent.sizeDelta = new Vector2(1600f, 420f);
        battlePassRewardScrollRect.viewport = viewport;
        battlePassRewardScrollRect.content = battlePassRewardContent;
        battlePassRewardScrollRect.horizontalNormalizedPosition = 0f;

        var stripTexture = Resources.Load<Texture2D>(BattlePassUiResourceRoot + "battlepass_variant1_reward_strip");
        battlePassRewardTierHeaderTexture = Resources.Load<Texture2D>(BattlePassUiResourceRoot + "battlepass_variant1_reward_tier_header");
        battlePassRewardFreeCardTexture = Resources.Load<Texture2D>(BattlePassUiResourceRoot + "battlepass_variant1_reward_free_card");
        battlePassRewardPremiumCardTexture = Resources.Load<Texture2D>(BattlePassUiResourceRoot + "battlepass_variant1_reward_premium_card");
        var stripObject = new GameObject("Battle Pass Reward Track Tiers 1 to 6", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        stripObject.transform.SetParent(battlePassRewardContent, false);
        var stripRect = stripObject.GetComponent<RectTransform>();
        PlaceTopLeft(stripRect, Vector2.zero, new Vector2(1000f, 420f));
        var stripImage = stripObject.GetComponent<RawImage>();
        stripImage.texture = stripTexture;
        stripImage.color = Color.white;
        stripImage.raycastTarget = false;

        var icons = new[] { "icon_gold", "mythic_gem", "icon_essence", "mythic_gem" };
        var premiumIcons = new[] { "shop_icon_hero_portrait", "shop_icon_bundle_chest", "mythic_gem", "icon_essence" };
        var freeAmounts = new[] { "200", "30", "2", "50" };
        var premiumAmounts = new[] { "1", "1", "75", "2" };
        for (var i = 0; i < 4; i++)
        {
            var x = 1000f + (i * 145f);
            CreateRewardTrackTier(battlePassRewardContent, i + 7, x, icons[i], freeAmounts[i], premiumIcons[i], premiumAmounts[i]);
            CreateRewardTrackHitArea(i + 7, false, new Vector2(x + 68f, -102f), new Vector2(132f, 132f));
            CreateRewardTrackHitArea(i + 7, true, new Vector2(x + 68f, -250f), new Vector2(132f, 160f));
        }

        // These positions match the tier cards inside the 1000px reward-strip slice
        // (the free/premium lane label occupies the first ~140px).
        var firstTierXs = new[] { 145f, 280f, 415f, 550f, 685f, 820f };
        for (var i = 0; i < firstTierXs.Length; i++)
        {
            CreateRewardTrackHitArea(i + 1, false, new Vector2(firstTierXs[i] + 60f, -72f), new Vector2(124f, 150f));
            CreateRewardTrackHitArea(i + 1, true, new Vector2(firstTierXs[i] + 60f, -250f), new Vector2(124f, 160f));
            CreateBattlePassRewardStateOverlay(i + 1, new Vector2(firstTierXs[i], -194f), new Vector2(136f, 32f));
        }
    }

    private void CreateRewardTrackTier(Transform parent, int tier, float x, string freeIcon, string freeAmount, string premiumIcon, string premiumAmount)
    {
        var tierPanel = CreateBattlePassRewardTierHeader(parent, tier, x, -44f);
        var tierText = CreateText(tierPanel, "Tier", tier.ToString(), 22, new Vector2(0f, -16f), new Vector2(120f, 34f), new Color(1f, 0.9f, 0.7f), FontStyles.Bold);
        tierText.textWrappingMode = TextWrappingModes.NoWrap;

        CreateRewardTrackTile(parent, tier, "Battle Pass Free Tier " + tier, new Vector2(x, -102f), freeIcon, freeAmount, false);
        CreateRewardTrackTile(parent, tier, "Battle Pass Premium Tier " + tier, new Vector2(x, -248f), premiumIcon, premiumAmount, true);
    }

    private RectTransform CreateBattlePassRewardTierHeader(Transform parent, int tier, float x, float top)
    {
        var headerObject = new GameObject("Battle Pass Tier " + tier, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        headerObject.transform.SetParent(parent, false);
        var header = headerObject.GetComponent<RectTransform>();
        PlaceTopLeft(header, new Vector2(x, top), new Vector2(136f, 62f));
        var image = headerObject.GetComponent<RawImage>();
        image.texture = battlePassRewardTierHeaderTexture;
        image.color = Color.white;
        image.raycastTarget = false;
        var cover = CreatePanelTopLeft(header, "Tier Number Cover", new Vector2(42f, -12f), new Vector2(52f, 36f), new Color(0.12f, 0.07f, 0.03f, 0.96f), false);
        cover.GetComponent<Image>().raycastTarget = false;
        return header;
    }

    private void CreateRewardTrackTile(Transform parent, int tier, string name, Vector2 topLeft, string icon, string amount, bool locked)
    {
        var tileSize = locked ? new Vector2(136f, 164f) : new Vector2(136f, 130f);
        var tileObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        tileObject.transform.SetParent(parent, false);
        var tile = tileObject.GetComponent<RectTransform>();
        PlaceTopLeft(tile, topLeft, tileSize);
        var image = tileObject.GetComponent<RawImage>();
        image.texture = locked ? battlePassRewardPremiumCardTexture : battlePassRewardFreeCardTexture;
        image.color = Color.white;
        image.raycastTarget = false;

        var interiorHeight = locked ? 146f : 110f;
        var interior = CreatePanelTopLeft(tile, "Reward Interior", new Vector2(8f, -8f), new Vector2(120f, interiorHeight), locked ? new Color(0.08f, 0.045f, 0.12f, 1f) : new Color(0.02f, 0.09f, 0.12f, 1f), false);
        interior.GetComponent<Image>().raycastTarget = false;
        CreateOfferArt(tile, "Reward Icon", icon, new Vector2(0f, locked ? -56f : -42f), new Vector2(58f, 58f));
        var amountText = CreateText(tile, "Amount", amount, 20, new Vector2(0f, locked ? -120f : -78f), new Vector2(120f, 30f), new Color(1f, 0.9f, 0.72f), FontStyles.Bold);
        amountText.textWrappingMode = TextWrappingModes.NoWrap;
        if (locked)
        {
            var lockText = CreateText(tile, "Locked", "LOCKED", 10, new Vector2(0f, -145f), new Vector2(116f, 20f), new Color(1f, 0.7f, 0.25f), FontStyles.Bold);
            lockText.textWrappingMode = TextWrappingModes.NoWrap;
        }
        else
        {
            CreateBattlePassRewardStateOverlay(tier, new Vector2(topLeft.x, topLeft.y - 102f), new Vector2(136f, 28f));
        }
    }

    private void CreateRewardTrackHitArea(int tier, bool premium, Vector2 position, Vector2 size)
    {
        var buttonObject = new GameObject("Battle Pass Reward " + tier + (premium ? " Premium" : " Free") + " Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(battlePassRewardContent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        PlaceTopLeft(rect, new Vector2(position.x - (size.x * 0.5f), -Mathf.Abs(position.y)), size);
        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => ShowRewardNotice(tier, premium));
    }

    private void CreateBattlePassRewardStateOverlay(int tier, Vector2 topLeft, Vector2 size)
    {
        if (tier < 1 || tier >= battlePassFreeRewardStatePanels.Length || battlePassRewardContent == null)
        {
            return;
        }

        // The full-width cover hides the static artwork's old claim badge. The
        // inset button leaves a dark breathing gap between adjacent reward cards.
        var cover = CreatePanelTopLeft(battlePassRewardContent, "Battle Pass Free Reward State Cover " + tier, topLeft, size, new Color(0.008f, 0.018f, 0.025f, 1f), false);
        var panel = CreatePanelTopLeft(cover, "Battle Pass Free Reward State Button " + tier, new Vector2(9f, -2f), new Vector2(size.x - 18f, size.y - 4f), new Color(0.12f, 0.06f, 0.02f, 1f), false);
        var panelImage = panel.GetComponent<Image>();
        ApplySprite(panelImage, "ui_button_brown", RuntimeArtResourceRoot, new Vector4(10f, 10f, 10f, 10f));
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.12f, 0.06f, 0.02f, 1f);
        var label = CreateText(panel, "State", "LOCKED", 13, new Vector2(0f, -2f), new Vector2(size.x - 26f, size.y - 8f), new Color(1f, 0.78f, 0.32f), FontStyles.Bold);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        battlePassFreeRewardStatePanels[tier] = panel;
        battlePassFreeRewardStateLabels[tier] = label;
    }

    private void CompleteBattlePassMission(int index)
    {
        if (index < 0 || index >= battlePassMissionProgress.Length || battlePassMissionProgress[index] >= BattlePassMissionTargets[index])
        {
            return;
        }

        battlePassMissionProgress[index]++;
        battlePassXp = Mathf.Min(100, battlePassXp + BattlePassMissionXp[index]);
        UpdateBattlePassInteractiveState();
    }

    private void UpdateBattlePassInteractiveState()
    {
        if (battlePassProgressFill != null)
        {
            var trackWidth = 850f;
            var fillWidth = Mathf.Clamp(trackWidth * (battlePassXp / 100f), 8f, trackWidth);
            battlePassProgressFill.sizeDelta = new Vector2(fillWidth, 16f);
            battlePassProgressFill.anchoredPosition = new Vector2(-trackWidth * 0.5f + (fillWidth * 0.5f), -4f);
        }

        if (battlePassProgressLabel != null)
        {
            battlePassProgressLabel.text = battlePassXp + " / 100 XP";
        }

        for (var i = 0; i < battlePassMissionProgressLabels.Length; i++)
        {
            if (battlePassMissionProgressLabels[i] != null)
            {
                battlePassMissionProgressLabels[i].text = battlePassMissionProgress[i] + "/" + BattlePassMissionTargets[i];
            }

            if (battlePassMissionRightProgressLabels[i] != null)
            {
                battlePassMissionRightProgressLabels[i].text = battlePassMissionProgress[i] + " / " + BattlePassMissionTargets[i];
            }

            if (battlePassMissionProgressFills[i] != null)
            {
                var missionTrackWidth = 205f;
                var missionFillWidth = Mathf.Clamp(missionTrackWidth * (battlePassMissionProgress[i] / (float)BattlePassMissionTargets[i]), 6f, missionTrackWidth);
                battlePassMissionProgressFills[i].sizeDelta = new Vector2(missionFillWidth, 10f);
                battlePassMissionProgressFills[i].anchoredPosition = new Vector2(-missionTrackWidth * 0.5f + (missionFillWidth * 0.5f), -4f);
            }
        }

        UpdateBattlePassRewardState();
    }

    private void UpdateBattlePassRewardState()
    {
        for (var tier = 1; tier < battlePassFreeRewardStatePanels.Length; tier++)
        {
            if (battlePassFreeRewardStatePanels[tier] == null)
            {
                continue;
            }

            var unlocked = battlePassXp >= tier * 10;
            var image = battlePassFreeRewardStatePanels[tier].GetComponent<Image>();
            if (image != null)
            {
                image.color = unlocked
                    ? new Color(0.95f, 0.62f, 0.16f, 1f)
                    : new Color(0.12f, 0.06f, 0.02f, 1f);
            }

            if (battlePassFreeRewardStateLabels[tier] != null)
            {
                battlePassFreeRewardStateLabels[tier].text = unlocked ? "CLAIM" : "LOCKED";
                battlePassFreeRewardStateLabels[tier].color = unlocked
                    ? new Color(0.16f, 0.08f, 0.02f)
                    : new Color(1f, 0.78f, 0.32f);
            }
        }
    }

    private void ShowRewardNotice(int tier, bool premium)
    {
        if (premium)
        {
            ShowPurchaseNotice(new ShopOffer(
                "battle_pass_tier_" + tier,
                "Premium Tier " + tier,
                "Unlock the premium Battle Pass lane to claim this reward.",
                "€9.99",
                "shop_icon_battle_pass_crest"));
            return;
        }

        var requiredXp = tier * 10;
        if (battlePassXp < requiredXp)
        {
            if (purchaseModalRoot == null)
            {
                return;
            }

            SetReferenceArtworkVisible(false);
            purchaseModalTitle.text = "Reward Tier " + tier + " locked";
            purchaseModalBody.text = "Earn " + requiredXp + " XP to unlock this free reward.";
            purchaseModalPrice.text = "LOCKED";
            purchaseModalRoot.gameObject.SetActive(true);
            purchaseModalRoot.SetAsLastSibling();
            return;
        }

        if (purchaseModalRoot == null)
        {
            return;
        }

        SetReferenceArtworkVisible(false);
        purchaseModalTitle.text = "Reward Tier " + tier;
        purchaseModalBody.text = "This free reward is ready to claim once the tier is unlocked.";
        purchaseModalPrice.text = "CLAIM REWARD";
        purchaseModalRoot.gameObject.SetActive(true);
        purchaseModalRoot.SetAsLastSibling();
    }

    private void CreateBattlePassHitAreas()
    {
        if (battlePassArtworkLayer == null)
        {
            return;
        }

        CreateBattlePassHitArea("Battle Pass Featured Tab", new Vector2(-401f, -226f), new Vector2(202f, 88f), () => SelectTab(ShopTab.Featured));
        CreateBattlePassHitArea("Battle Pass Crystals Tab", new Vector2(-197f, -226f), new Vector2(196f, 88f), () => SelectTab(ShopTab.Crystals));
        CreateBattlePassHitArea("Battle Pass Bundles Tab", new Vector2(8f, -226f), new Vector2(196f, 88f), () => SelectTab(ShopTab.Bundles));
        CreateBattlePassHitArea("Battle Pass Active Tab", new Vector2(211f, -226f), new Vector2(210f, 88f), () => SelectTab(ShopTab.BattlePass));
        CreateBattlePassHitArea("Battle Pass Dev Tab", new Vector2(410f, -226f), new Vector2(190f, 88f), () => SelectTab(ShopTab.Dev));

        CreateBattlePassHitArea(
            "Battle Pass Unlock Purchase",
            new Vector2(-125f, -604f),
            new Vector2(430f, 94f),
            () => ShowPurchaseNotice(new ShopOffer(
                "battle_pass_premium",
                "Premium Battle Pass",
                "Unlock the premium reward lane for Season 01 and claim every earned tier.",
                "€9.99",
                "shop_icon_battle_pass_crest")));

        var missionRows = new[] { 1000f, 1092f, 1185f };
        for (var i = 0; i < missionRows.Length; i++)
        {
            var missionIndex = i;
            CreateBattlePassHitArea(
                "Battle Pass Mission " + (i + 1),
                new Vector2(0f, -missionRows[i]),
                new Vector2(900f, 82f),
                () => CompleteBattlePassMission(missionIndex));
        }

        CreateBattlePassHitArea("Battle Pass Heroes Navigation", new Vector2(-390f, -1800f), new Vector2(220f, 180f), controller.ShowHeroes);
        CreateBattlePassHitArea("Battle Pass Village Navigation", new Vector2(-190f, -1800f), new Vector2(220f, 180f), controller.ShowVillage);
        CreateBattlePassHitArea("Battle Pass Home Navigation", new Vector2(0f, -1800f), new Vector2(220f, 180f), controller.ShowHome);
        CreateBattlePassHitArea("Battle Pass Dungeons Navigation", new Vector2(205f, -1800f), new Vector2(220f, 180f), controller.ShowDungeons);
        CreateBattlePassHitArea("Battle Pass Summon Navigation", new Vector2(405f, -1800f), new Vector2(220f, 180f), controller.ShowSummon);
    }

    private void CreateBattlePassHitArea(string name, Vector2 position, Vector2 size, Action callback)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(battlePassArtworkLayer, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        PlaceTop(rect, position, size);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        if (callback != null)
        {
            button.onClick.AddListener(() => callback());
        }
    }

    private sealed class BattlePassSlice
    {
        public readonly string ResourceName;
        public readonly float Top;
        public readonly float Height;

        public BattlePassSlice(string resourceName, float top, float height)
        {
            ResourceName = resourceName;
            Top = top;
            Height = height;
        }
    }

    private void BuildReferenceTabDecorations()
    {
        if (referenceLayer == null || referenceTabHighlight != null) return;
        // Sample the approved Featured frame, including its original gold glow.
        // A narrow interior patch removes the baked label; all labels stay live.
        var texture = Resources.Load<Texture2D>(ReferenceArtworkResource);
        var parent = referenceLayer;
        referenceTabInactiveMask = CreatePanel(parent, "Reference Featured Inactive Mask", new Vector2(-401f, -216f), new Vector2(210f, 88f), Color.white, false);
        var inactive = referenceTabInactiveMask.GetComponent<Image>();
        inactive.sprite = Sprite.Create(texture, new Rect(211, texture.height - 267, 172, 78), new Vector2(.5f, .5f));
        var inactiveCover = CreatePanel(referenceTabInactiveMask, "Inactive Label Cover", new Vector2(0, -32), new Vector2(174, 39), new Color(.085f, .067f, .047f), false);
        var inactiveLabel = CreateText(inactiveCover, "Inactive Featured Label", "FEATURED", 24, Vector2.zero, new Vector2(174, 39), new Color(.78f, .66f, .46f), FontStyles.Bold);
        if (MythwakeLoginUI.HeadingFont != null) inactiveLabel.font = MythwakeLoginUI.HeadingFont;
        referenceTabHighlight = CreatePanel(parent, "Reference Active Tab", Vector2.zero, new Vector2(210, 88), Color.white, false);
        referenceTabHighlight.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(31, texture.height - 269, 179, 80), new Vector2(.5f, .5f));
        referenceTabTextMask = CreatePanel(referenceTabHighlight, "Active Label Cover", new Vector2(0, -32), new Vector2(174, 39), new Color(.16f, .105f, .025f), false);
        referenceTabSelectedLabel = CreateText(referenceTabTextMask, "Selected Tab Label", "FEATURED", 24, Vector2.zero, new Vector2(174, 39), new Color(1f, .91f, .65f), FontStyles.Bold);
        referenceTabSelectedLabel.enableAutoSizing = true;
        if (MythwakeLoginUI.HeadingFont != null) referenceTabSelectedLabel.font = MythwakeLoginUI.HeadingFont;
        referenceTabSelectedLabel.fontSizeMin = 19;
        referenceTabSelectedLabel.fontSizeMax = 24;
        UpdateReferenceTabDecorations();
    }

    private void UpdateReferenceTabDecorations()
    {
        if (referenceTabHighlight == null) return;
        var tab = battlePassArtworkSelected ? ShopTab.BattlePass : CurrentReferenceTab;
        var centers = new[] { -401f, -197f, 8f, 211f, 410f };
        referenceTabHighlight.anchoredPosition = new Vector2(centers[(int)tab], -216f);
        referenceTabSelectedLabel.text = ReferenceTabLabel(tab);
        var visible = referenceLayer != null && referenceLayer.gameObject.activeSelf && tab != ShopTab.Featured;
        referenceTabInactiveMask.gameObject.SetActive(visible);
        referenceTabHighlight.gameObject.SetActive(visible);
        referenceTabInactiveMask.SetAsLastSibling();
        referenceTabHighlight.SetAsLastSibling();
    }

    private ShopTab CurrentReferenceTab
    {
        get
        {
            if (contentRoots != null)
            {
                for (var i = 0; i < contentRoots.Length; i++)
                {
                    if (contentRoots[i] != null && contentRoots[i].gameObject.activeSelf)
                    {
                        return (ShopTab)i;
                    }
                }
            }

            return ShopTab.Featured;
        }
    }

    private static string ReferenceTabLabel(ShopTab tab)
    {
        switch (tab)
        {
            case ShopTab.BattlePass:
                return "BATTLE PASS";
            default:
                return tab.ToString().ToUpperInvariant();
        }
    }

    private void BuildSharedTabContentLayer()
    {
        if (referenceLayer == null || tabContentLayer != null)
        {
            return;
        }

        var canvasParent = referenceLayer.parent != null ? referenceLayer.parent : transform;
        tabContentLayer = new GameObject("Shop Shared Tab Content Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        tabContentLayer.SetParent(canvasParent, false);
        tabContentLayer.anchorMin = new Vector2(0.5f, 0.5f);
        tabContentLayer.anchorMax = new Vector2(0.5f, 0.5f);
        tabContentLayer.pivot = new Vector2(0.5f, 0.5f);
        tabContentLayer.anchoredPosition = Vector2.zero;
        tabContentLayer.sizeDelta = new Vector2(1080f, 1920f);

        // Preserve the exact header/navigation from the reference artwork, while
        // giving every other tab the same ornate storefront frame for its content.
        CreateFramedPanel(tabContentLayer, "Shared Shop Tab Frame", new Vector2(0f, -300f), new Vector2(1040f, 1370f), new Color(0.012f, 0.026f, 0.04f, 0.99f));

        // The storefront body is intentionally taller than the visible frame. A
        // transparent touch viewport plus RectMask2D gives phone users the expected
        // finger-drag scroll without moving the fixed shop header or bottom nav.
        shopScrollViewport = CreatePanel(tabContentLayer, "Shop Scroll Viewport", new Vector2(0f, -300f), new Vector2(1000f, 1350f), new Color(0f, 0f, 0f, 0.001f), true);
        shopScrollViewport.gameObject.AddComponent<RectMask2D>();
        shopScrollContent = new GameObject("Shop Scroll Content", typeof(RectTransform)).GetComponent<RectTransform>();
        shopScrollContent.SetParent(shopScrollViewport, false);
        shopScrollContent.anchorMin = new Vector2(0.5f, 1f);
        shopScrollContent.anchorMax = new Vector2(0.5f, 1f);
        shopScrollContent.pivot = new Vector2(0.5f, 1f);
        shopScrollContent.anchoredPosition = Vector2.zero;
        shopScrollContent.sizeDelta = new Vector2(ContentWidth, ContentHeight);
        shopScrollRect = tabContentLayer.gameObject.AddComponent<ScrollRect>();
        shopScrollRect.viewport = shopScrollViewport;
        shopScrollRect.content = shopScrollContent;
        shopScrollRect.horizontal = false;
        shopScrollRect.vertical = true;
        shopScrollRect.inertia = true;
        shopScrollRect.movementType = ScrollRect.MovementType.Clamped;
        shopScrollRect.scrollSensitivity = 2.5f;
        shopScrollRect.verticalNormalizedPosition = 1f;

        // Reuse the functional tab content already bound to the controller, but move
        // it into the shared full-screen frame so all tabs use identical chrome.
        if (contentRoots != null)
        {
            for (var i = 0; i < contentRoots.Length; i++)
            {
                var content = contentRoots[i];
                if (content == null)
                {
                    continue;
                }

                content.SetParent(shopScrollContent, false);
                PlaceTop(content, new Vector2(0f, -60f), new Vector2(ContentWidth, ContentHeight));
                content.SetAsLastSibling();
            }
        }

        tabContentLayer.gameObject.SetActive(false);
    }

    private void BuildReferenceHitAreas()
    {
        if (referenceLayer == null || controller == null)
        {
            return;
        }

        var canvasParent = referenceLayer.parent != null ? referenceLayer.parent : transform;
        referenceChromeHitLayer = new GameObject("Shop Chrome Hit Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        referenceChromeHitLayer.SetParent(canvasParent, false);
        referenceChromeHitLayer.anchorMin = referenceChromeHitLayer.anchorMax = new Vector2(0.5f, 0.5f);
        referenceChromeHitLayer.pivot = new Vector2(0.5f, 0.5f);
        referenceChromeHitLayer.anchoredPosition = Vector2.zero;
        referenceChromeHitLayer.sizeDelta = new Vector2(1080f, 1920f);

        // Coordinates are in the same 1080x1920 portrait reference space as the
        // CanvasScaler, so the artwork and its hit areas stay aligned on Android.
        CreateReferenceChromeHitArea("Reference Featured Tab", new Vector2(-401f, -226f), new Vector2(202f, 88f), () => SelectTab(ShopTab.Featured));
        CreateReferenceChromeHitArea("Reference Crystals Tab", new Vector2(-197f, -226f), new Vector2(196f, 88f), () => SelectTab(ShopTab.Crystals));
        CreateReferenceChromeHitArea("Reference Bundles Tab", new Vector2(8f, -226f), new Vector2(196f, 88f), () => SelectTab(ShopTab.Bundles));
        CreateReferenceChromeHitArea("Reference Battle Pass Tab", new Vector2(211f, -226f), new Vector2(210f, 88f), () => SelectTab(ShopTab.BattlePass));
        CreateReferenceChromeHitArea("Reference Dev Tab", new Vector2(410f, -226f), new Vector2(190f, 88f), () => SelectTab(ShopTab.Dev));

        CreateReferenceChromeHitArea("Reference Home Crest", new Vector2(-455f, -45f), new Vector2(170f, 150f), controller.ShowHome);
        CreateReferenceChromeHitArea("Reference Gem Plus", new Vector2(100f, -52f), new Vector2(90f, 90f), () => SelectTab(ShopTab.Crystals));
        CreateReferenceChromeHitArea("Reference Gold Plus", new Vector2(380f, -52f), new Vector2(90f, 90f), () => SelectTab(ShopTab.Bundles));
        CreateReferenceChromeHitArea("Reference Management Menu", new Vector2(490f, -52f), new Vector2(100f, 100f), controller.ShowShopManagementPopup);

        // The hit areas intentionally cover the complete offer card rather than only
        // the printed price so taps remain forgiving on phones with different touch
        // density/scaling while the reference artwork stays untouched.
        CreateReferenceHitArea("Reference Featured Purchase", new Vector2(222f, -575f), new Vector2(500f, 170f), () => ShowPurchaseNotice(new ShopOffer("myth_crystal_bundle", "Myth Crystal Bundle", "2,500 Myth Crystals, 150K Gold, 10 Essence and 5 Crests", "€9.99", "home_treasure_chest_button", true)));
        CreateReferenceHitArea("Reference Starter Purchase", new Vector2(-283f, -775f), new Vector2(500f, 365f), () => ShowPurchaseNotice(new ShopOffer("starter_pack", "Starter Pack", "500 Crystals, 25K Gold and 5 Essence", "€2.99", "icon_gold")));
        CreateReferenceHitArea("Reference Crystal Cache Purchase", new Vector2(283f, -775f), new Vector2(500f, 365f), () => ShowPurchaseNotice(new ShopOffer("crystal_cache", "Crystal Cache", "1,100 Crystals, 60K Gold and 5 Essence", "€4.99", "icon_gems")));
        CreateReferenceHitArea("Reference Adventurer Purchase", new Vector2(-283f, -1160f), new Vector2(500f, 410f), () => ShowPurchaseNotice(new ShopOffer("adventurer_bundle", "Adventurer Bundle", "2,200 Crystals, 120K Gold and 15 Essence", "€14.99", "home_shop_button")));
        CreateReferenceHitArea("Reference Legendary Purchase", new Vector2(283f, -1160f), new Vector2(500f, 410f), () => ShowPurchaseNotice(new ShopOffer("legendary_chest", "Legendary Chest", "5,000 Crystals, 250K Gold and 25 Essence", "€19.99", "home_treasure_chest_button")));
        CreateReferenceHitArea("Reference Restore Purchases", new Vector2(0f, -1590f), new Vector2(420f, 86f), ShowRestorePurchasesNotice);

        CreateReferenceChromeHitArea("Reference Heroes Navigation", new Vector2(-390f, -1800f), new Vector2(220f, 180f), controller.ShowHeroes);
        CreateReferenceChromeHitArea("Reference Village Navigation", new Vector2(-190f, -1800f), new Vector2(220f, 180f), controller.ShowVillage);
        CreateReferenceChromeHitArea("Reference Home Navigation", new Vector2(0f, -1800f), new Vector2(220f, 180f), controller.ShowHome);
        CreateReferenceChromeHitArea("Reference Dungeons Navigation", new Vector2(205f, -1800f), new Vector2(220f, 180f), controller.ShowDungeons);
        CreateReferenceChromeHitArea("Reference Summon Navigation", new Vector2(405f, -1800f), new Vector2(220f, 180f), controller.ShowSummon);
    }

    private void CreateReferenceHitArea(string name, Vector2 position, Vector2 size, Action callback)
    {
        CreateHitArea(referenceLayer, name, position, size, callback);
    }

    private void CreateReferenceChromeHitArea(string name, Vector2 position, Vector2 size, Action callback)
    {
        CreateHitArea(referenceChromeHitLayer, name, position, size, callback);
    }

    private static void CreateHitArea(Transform parent, string name, Vector2 position, Vector2 size, Action callback)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        PlaceTop(rect, position, size);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        if (callback != null)
        {
            button.onClick.AddListener(() => callback());
        }
    }

    public void SetReferenceArtworkVisible(bool visible)
    {
        if (!referenceArtworkLoaded && !battlePassArtworkLoaded)
        {
            return;
        }

        var showBattlePassArtwork = visible && battlePassArtworkSelected && battlePassArtworkLayer != null;
        if (referenceLayer != null)
        {
            referenceLayer.gameObject.SetActive(visible);
        }
        if (referenceTabHighlight != null)
        {
            referenceTabHighlight.gameObject.SetActive(visible && !referenceTabSelected);
        }
        if (referenceTabInactiveMask != null)
        {
            referenceTabInactiveMask.gameObject.SetActive(visible && !referenceTabSelected);
        }
        if (battlePassArtworkLayer != null)
        {
            battlePassArtworkLayer.gameObject.SetActive(showBattlePassArtwork);
        }
        // Keep the shared tab body scoped to the shop screen as well.  Without
        // this, leaving the shop while a secondary tab is selected would leave
        // its content layer visible over the next screen.
        if (tabContentLayer != null)
        {
            tabContentLayer.gameObject.SetActive(visible && !referenceTabSelected && !showBattlePassArtwork);
        }
        if (referenceChromeHitLayer != null)
        {
            referenceChromeHitLayer.gameObject.SetActive(visible);
            if (visible)
                referenceChromeHitLayer.SetAsLastSibling();
        }
        UpdateReferenceTabDecorations();
    }

    private void BuildOfferGrid(Transform parent, ShopOffer[] offers, float offsetX, float offsetY, string tab)
    {
        for (var i = 0; i < offers.Length; i++)
        {
            var column = i % 2;
            var row = i / 2;
            var x = offsetX + (column == 0 ? -238f : 238f);
            var y = offsetY - (row * 322f);
            CreateOfferCard(parent, offers[i], new Vector2(x, y), new Vector2(450f, 294f), tab);
        }
    }

    private void CreateOfferCard(Transform parent, ShopOffer offer, Vector2 position, Vector2 size, string tab)
    {
        var card = CreateFramedPanel(parent, offer.Id + " Card", position, size, new Color(0.025f, 0.052f, 0.075f, 0.98f));
        var isCrystal = string.Equals(tab, "crystals", StringComparison.OrdinalIgnoreCase);
        var accent = isCrystal ? new Color(0.12f, 0.76f, 0.94f, 0.95f) : new Color(0.96f, 0.65f, 0.22f, 0.95f);
        CreatePanel(card, "Offer Accent", new Vector2(0f, -72f), new Vector2(size.x - 34f, 3f), accent, false);
        CreatePanel(card, "Offer Accent Glow", new Vector2(0f, -75f), new Vector2(size.x - 90f, 2f), new Color(accent.r, accent.g, accent.b, 0.22f), false);

        var category = isCrystal ? "MYTHIC CURRENCY" : "ADVENTURER SET";
        var categoryText = CreateText(card, "Category", category, 12, new Vector2(-82f, -84f), new Vector2(154f, 22f), new Color(accent.r, accent.g, accent.b, 0.95f), FontStyles.Bold);
        categoryText.alignment = TextAlignmentOptions.Left;

        var ribbon = CreateRibbon(card, isCrystal ? "BEST VALUE" : "TOP PICK", new Vector2(-121f, -35f), 18);
        ribbon.gameObject.SetActive(offer.BestValue);
        shopOfferRibbons[ShopOfferKey(tab, offer.Id)] = ribbon;

        var title = CreateText(card, "Title", offer.Title, 29, new Vector2(0f, -30f), new Vector2(size.x - 32f, 44f), new Color(1f, 0.87f, 0.59f), FontStyles.Bold);
        title.enableAutoSizing = true;
        title.fontSizeMin = 20;
        title.fontSizeMax = 29;
        title.textWrappingMode = TextWrappingModes.NoWrap;

        CreateOfferArt(card, "Offer Art", offer.Icon, new Vector2(-122f, -150f), new Vector2(174f, 154f));
        var contents = CreateText(card, "Contents", offer.Contents, 20, new Vector2(92f, -118f), new Vector2(210f, 92f), new Color(0.72f, 0.9f, 0.98f), FontStyles.Normal);
        contents.alignment = TextAlignmentOptions.MidlineLeft;
        contents.enableAutoSizing = true;
        contents.fontSizeMin = 15;
        contents.fontSizeMax = 20;

        var meta = isCrystal
            ? (offer.Contents.IndexOf("bonus", StringComparison.OrdinalIgnoreCase) >= 0 ? "BONUS CRYSTALS" : "INSTANT DELIVERY")
            : "CRYSTALS  +  GOLD  +  ESSENCE";
        var metaText = CreateText(card, "Meta", meta, 11, new Vector2(94f, -184f), new Vector2(212f, 22f), new Color(0.86f, 0.74f, 0.44f), FontStyles.Bold);
        metaText.alignment = TextAlignmentOptions.Left;

        if (isCrystal && offer.Contents.IndexOf("bonus", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            CreateBadge(card, "Bonus Badge", "+ BONUS", new Vector2(142f, -87f), new Color(0.08f, 0.35f, 0.48f, 0.96f), new Color(0.5f, 0.94f, 1f));
        }
        else if (!isCrystal)
        {
            CreateBundleMiniRewards(card, offer, new Vector2(166f, -211f));
        }

        CreatePriceButton(card, offer, new Vector2(0f, -236f), new Vector2(290f, 58f));
    }

    private void CreatePriceButton(Transform parent, ShopOffer offer, Vector2 position, Vector2 size)
    {
        var button = CreateButton(parent, offer.Id + " Purchase", offer.Price, position, size, false, true);
        button.onClick.AddListener(() => ShowPurchaseNotice(offer));
    }

    private void CreateSectionHeader(Transform parent, string title, string subtitle)
    {
        var titleText = CreateText(parent, title + " Header", title, 40, new Vector2(0f, 0f), new Vector2(860f, 54f), new Color(1f, 0.86f, 0.55f), FontStyles.Bold);
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        var subtitleText = CreateText(parent, title + " Subtitle", subtitle, 20, new Vector2(0f, -52f), new Vector2(860f, 40f), new Color(0.66f, 0.82f, 0.93f), FontStyles.Normal);
        subtitleText.enableAutoSizing = true;
        subtitleText.fontSizeMin = 16;
        subtitleText.fontSizeMax = 20;
    }

    private void CreateStorefrontHeaderPlate(Transform parent, string title, string subtitle, Color accent, string tag)
    {
        var plate = CreateFramedPanel(parent, title + " Header Plate", new Vector2(0f, -92f), new Vector2(930f, 58f), new Color(0.012f, 0.032f, 0.048f, 0.98f));
        CreatePanel(plate, "Header Accent", new Vector2(0f, -43f), new Vector2(760f, 3f), accent, false);
        CreateText(plate, "Header Tag", tag, 12, new Vector2(-285f, -16f), new Vector2(220f, 20f), accent, FontStyles.Bold);
        var callout = CreateText(plate, "Header Callout", subtitle, 13, new Vector2(108f, -16f), new Vector2(500f, 20f), new Color(0.68f, 0.84f, 0.94f), FontStyles.Normal);
        callout.enableAutoSizing = true;
        callout.fontSizeMin = 11;
        callout.fontSizeMax = 14;
    }

    private static void CreateBadge(Transform parent, string name, string label, Vector2 position, Color background, Color textColor)
    {
        var badge = CreatePanel(parent, name, position, new Vector2(112f, 24f), background, false);
        var text = CreateText(badge, "Label", label, 11, new Vector2(0f, -1f), new Vector2(106f, 20f), textColor, FontStyles.Bold);
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static void CreateBundleMiniRewards(Transform parent, ShopOffer offer, Vector2 position)
    {
        var icons = new[] { "mythic_gem", "gold_coin", "icon_essence" };
        for (var i = 0; i < icons.Length; i++)
        {
            CreateOfferArt(parent, "Bundle Reward " + i, icons[i], position + new Vector2((i - 1) * 32f, 0f), new Vector2(24f, 24f));
        }
    }

    private static void CreateProgressBar(Transform parent, string name, Vector2 position, Vector2 size, float progress, Color fillColor)
    {
        var track = CreatePanel(parent, name + " Track", position, size, new Color(0.015f, 0.025f, 0.04f, 0.98f), false);
        var fillWidth = Mathf.Max(8f, size.x * progress);
        var fill = CreatePanel(track, name + " Fill", new Vector2(-size.x * 0.5f + (fillWidth * 0.5f), -2f), new Vector2(fillWidth, size.y - 4f), fillColor, false);
        fill.GetComponent<Image>().raycastTarget = false;
        CreatePanel(track, name + " Shine", new Vector2(-size.x * 0.5f + (fillWidth * 0.5f), -4f), new Vector2(fillWidth, 2f), new Color(1f, 1f, 1f, 0.36f), false);
    }

    private void CreateRewardChip(Transform parent, string name, string icon, string amount, Vector2 position)
    {
        var chip = CreateFramedPanel(parent, name, position, new Vector2(136f, 114f), new Color(0.025f, 0.05f, 0.07f, 0.96f));
        CreateOfferArt(chip, "Icon", icon, new Vector2(0f, -35f), new Vector2(42f, 42f));
        CreateText(chip, "Amount", amount, 22, new Vector2(0f, -82f), new Vector2(112f, 30f), new Color(1f, 0.87f, 0.59f), FontStyles.Bold);
    }

    private RectTransform CreateRibbon(Transform parent, string label, Vector2 position, int fontSize = 21)
    {
        var ribbon = CreatePanel(parent, "Best Value Ribbon", position, new Vector2(190f, 42f), new Color(0.91f, 0.59f, 0.1f, 1f), false);
        var text = CreateText(ribbon, "Label", label, fontSize, new Vector2(0f, -1f), new Vector2(174f, 34f), new Color(0.16f, 0.08f, 0.02f), FontStyles.Bold);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        ribbon.localRotation = Quaternion.Euler(0f, 0f, -10f);
        return ribbon;
    }

    private static string ShopOfferKey(string tab, string offerId)
    {
        return (tab ?? string.Empty).Trim().ToLowerInvariant() + ":" + (offerId ?? string.Empty).Trim().ToLowerInvariant();
    }

    private void SelectTab(ShopTab tab)
    {
        if (contentRoots == null)
        {
            return;
        }

        for (var i = 0; i < contentRoots.Length; i++)
        {
            var selected = i == (int)tab;
            if (contentRoots[i] != null)
            {
                contentRoots[i].gameObject.SetActive(selected);
            }

            if (tabButtons != null && i < tabButtons.Length && tabButtons[i] != null)
            {
                StyleTab(tabButtons[i], selected);
            }

            if (tabLabels != null && i < tabLabels.Length && tabLabels[i] != null)
            {
                tabLabels[i].color = selected ? new Color(1f, 0.91f, 0.65f) : new Color(0.85f, 0.72f, 0.52f);
            }
        }

        if (battlePassArtworkLoaded && tab == ShopTab.BattlePass)
        {
            referenceTabSelected = false;
            battlePassArtworkSelected = true;
            generatedUiGroup.alpha = 0f;
            generatedUiGroup.interactable = false;
            generatedUiGroup.blocksRaycasts = false;
            if (tabContentLayer != null)
            {
                tabContentLayer.gameObject.SetActive(false);
            }

            if (battlePassRewardScrollRect != null)
            {
                battlePassRewardScrollRect.StopMovement();
                battlePassRewardScrollRect.horizontalNormalizedPosition = 0f;
            }

            UpdateReferenceTabDecorations();
            SetReferenceArtworkVisible(controller != null);
            if (controller != null)
            {
                controller.RefreshShopChrome();
            }
            return;
        }

        battlePassArtworkSelected = false;
        if (referenceArtworkLoaded)
        {
            referenceTabSelected = tab == ShopTab.Featured;
            // The approved reference artwork owns the header and navigation for every
            // shop tab. The generated root remains hidden; only its tab content roots
            // are shown inside the shared frame below the header.
            generatedUiGroup.alpha = 0f;
            generatedUiGroup.interactable = false;
            generatedUiGroup.blocksRaycasts = false;
            if (tabContentLayer != null)
            {
                tabContentLayer.gameObject.SetActive(!referenceTabSelected);
            }

            if (shopScrollRect != null)
            {
                shopScrollRect.StopMovement();
                shopScrollRect.verticalNormalizedPosition = 1f;
            }

            UpdateReferenceTabDecorations();

            SetReferenceArtworkVisible(controller != null);
            if (controller != null)
            {
                controller.RefreshShopChrome();
            }
        }
    }

    private void ShowPurchaseNotice(ShopOffer offer)
    {
        if (purchaseModalRoot == null)
        {
            return;
        }

        SetReferenceArtworkVisible(false);
        purchaseModalTitle.text = offer.Title;
        purchaseModalBody.text = offer.Contents + "\n\nThis storefront is ready for platform checkout. Connect Google Play / App Store IAP and server-side receipt validation before enabling live purchases.";
        purchaseModalPrice.text = offer.Price;
        purchaseModalRoot.gameObject.SetActive(true);
        purchaseModalRoot.SetAsLastSibling();
        if (controller != null)
        {
            controller.NotifyShopPurchaseRequested(offer.Id, offer.Title, offer.Price);
        }
    }

    private void ShowRestorePurchasesNotice()
    {
        if (purchaseModalRoot == null)
        {
            return;
        }

        SetReferenceArtworkVisible(false);
        purchaseModalTitle.text = "Restore purchases";
        purchaseModalBody.text = "Restore purchases is displayed in the final storefront. It will become available after the native store provider and receipt validation service are connected.";
        purchaseModalPrice.text = string.Empty;
        purchaseModalRoot.gameObject.SetActive(true);
        purchaseModalRoot.SetAsLastSibling();
        if (controller != null)
        {
            controller.NotifyShopRestoreRequested();
        }
    }

    private static RectTransform CreateFramedPanel(Transform parent, string name, Vector2 position, Vector2 size, Color innerColor)
    {
        var frame = CreatePanel(parent, name, position, size, new Color(0.54f, 0.31f, 0.11f, 1f), false);
        var frameImage = frame.GetComponent<Image>();
        ApplySprite(frameImage, "ui_panel_brown", RuntimeArtResourceRoot, new Vector4(10f, 10f, 10f, 10f));
        frameImage.color = new Color(0.43f, 0.24f, 0.09f, 1f);

        var inner = CreatePanel(frame, "Inner", new Vector2(0f, -8f), new Vector2(size.x - 18f, size.y - 18f), innerColor, false);
        inner.SetAsFirstSibling();
        CreatePanel(frame, "Top Glow", new Vector2(0f, -9f), new Vector2(size.x - 38f, 3f), new Color(0.95f, 0.65f, 0.22f, 0.62f), false);
        return frame;
    }

    private static RectTransform CreateFramedPanelTopLeft(Transform parent, string name, Vector2 topLeft, Vector2 size, Color innerColor)
    {
        var frame = CreatePanelTopLeft(parent, name, topLeft, size, new Color(0.54f, 0.31f, 0.11f, 1f), false);
        var frameImage = frame.GetComponent<Image>();
        ApplySprite(frameImage, "ui_panel_brown", RuntimeArtResourceRoot, new Vector4(10f, 10f, 10f, 10f));
        frameImage.color = new Color(0.43f, 0.24f, 0.09f, 1f);

        var inner = CreatePanel(frame, "Inner", new Vector2(0f, -8f), new Vector2(size.x - 18f, size.y - 18f), innerColor, false);
        inner.SetAsFirstSibling();
        CreatePanel(frame, "Top Glow", new Vector2(0f, -9f), new Vector2(size.x - 38f, 3f), new Color(0.95f, 0.65f, 0.22f, 0.62f), false);
        return frame;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, bool flat, bool gold = false)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        PlaceTop(rect, position, size);

        var image = buttonObject.GetComponent<Image>();
        if (!flat)
        {
            ApplySprite(image, gold ? "ui_button_brown" : "ui_button_brown", RuntimeArtResourceRoot, new Vector4(10f, 10f, 10f, 10f));
        }

        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = gold ? new Color(1f, 0.68f, 0.16f, 1f) : flat ? new Color(0f, 0f, 0f, 0f) : new Color(0.27f, 0.13f, 0.045f, 1f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.74f, 0.87f, 1f, 1f);
        colors.disabledColor = new Color(0.4f, 0.43f, 0.5f, 0.65f);
        button.colors = colors;

        var text = CreateText(buttonObject.transform, "Label", label, gold ? 30 : 20, new Vector2(0f, -2f), new Vector2(size.x - 24f, size.y - 12f), gold ? new Color(0.16f, 0.08f, 0.02f) : new Color(1f, 0.87f, 0.62f), FontStyles.Bold);
        text.enableAutoSizing = true;
        text.fontSizeMin = gold ? 19 : 12;
        text.fontSizeMax = gold ? 30 : 20;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size, Vector2 position, Vector2 rectSize, Color color, FontStyles style)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        PlaceTop(rect, position, rectSize);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool raycastTarget)
    {
        var objectRoot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objectRoot.transform.SetParent(parent, false);
        var rect = objectRoot.GetComponent<RectTransform>();
        PlaceTop(rect, position, size);
        var image = objectRoot.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return rect;
    }

    private static RectTransform CreatePanelTopLeft(Transform parent, string name, Vector2 topLeft, Vector2 size, Color color, bool raycastTarget)
    {
        var objectRoot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objectRoot.transform.SetParent(parent, false);
        var rect = objectRoot.GetComponent<RectTransform>();
        PlaceTopLeft(rect, topLeft, size);
        var image = objectRoot.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return rect;
    }

    private static void CreateOfferArt(Transform parent, string name, string iconName, Vector2 position, Vector2 size)
    {
        var sprite = LoadSprite(iconName);
        if (sprite != null)
        {
            var spriteObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            spriteObject.transform.SetParent(parent, false);
            var spriteRect = spriteObject.GetComponent<RectTransform>();
            PlaceTop(spriteRect, position, size);
            var spriteImage = spriteObject.GetComponent<Image>();
            spriteImage.sprite = sprite;
            spriteImage.preserveAspect = true;
            spriteImage.raycastTarget = false;
            spriteImage.color = Color.white;
            return;
        }

        var artObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        artObject.transform.SetParent(parent, false);
        var rect = artObject.GetComponent<RectTransform>();
        PlaceTop(rect, position, size);
        var image = artObject.GetComponent<RawImage>();
        image.texture = LoadTexture(iconName);
        if (iconName.StartsWith("home_", StringComparison.Ordinal))
        {
            // The home shortcuts include a text plaque below the illustration.
            // Shop cards intentionally show only their item artwork.
            image.uvRect = new Rect(0f, 0.25f, 1f, 0.75f);
        }
        image.raycastTarget = false;
        image.color = Color.white;
    }

    private static Sprite LoadSprite(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

        return Resources.Load<Sprite>(ShopIconResourceRoot + spriteName);
    }

    private static Texture2D LoadTexture(string textureName)
    {
        if (string.IsNullOrWhiteSpace(textureName))
        {
            return null;
        }

        var texture = Resources.Load<Texture2D>(RuntimeArtResourceRoot + textureName);
        if (texture == null)
        {
            texture = Resources.Load<Texture2D>(HomeUiResourceRoot + textureName);
        }
        if (texture == null)
        {
            texture = Resources.Load<Texture2D>(CurrencyResourceRoot + textureName);
        }
        return texture;
    }

    private static void ApplySprite(Image image, string textureName, string rootPath, Vector4 border)
    {
        if (image == null)
        {
            return;
        }

        var key = rootPath + textureName;
        Sprite sprite;
        if (!SpriteCache.TryGetValue(key, out sprite))
        {
            var texture = Resources.Load<Texture2D>(key);
            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                SpriteCache[key] = sprite;
            }
        }

        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static void MoveToContent(Transform child, RectTransform parent, Vector2 position, Vector2 size)
    {
        if (child == null || parent == null)
        {
            return;
        }

        child.SetParent(parent, false);
        var rect = child.GetComponent<RectTransform>();
        if (rect != null)
        {
            PlaceTop(rect, position, size);
        }
    }

    private static void ConfigureTextFit(TMP_Text text, float minSize, float maxSize)
    {
        if (text == null)
        {
            return;
        }

        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void StyleExistingButton(Button button, bool gold)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            ApplySprite(image, "ui_button_brown", RuntimeArtResourceRoot, new Vector4(10f, 10f, 10f, 10f));
            image.color = gold ? new Color(1f, 0.68f, 0.16f, 1f) : new Color(0.27f, 0.13f, 0.045f, 1f);
        }

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontStyle = FontStyles.Bold;
            label.color = gold ? new Color(0.16f, 0.08f, 0.02f) : new Color(1f, 0.87f, 0.62f);
        }
    }

    private static void StyleTab(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            ApplySprite(image, "ui_button_brown", RuntimeArtResourceRoot, new Vector4(10f, 10f, 10f, 10f));
            image.color = selected ? new Color(0.74f, 0.44f, 0.12f, 1f) : new Color(0.19f, 0.1f, 0.045f, 1f);
        }
    }

    private static void PlaceTop(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
