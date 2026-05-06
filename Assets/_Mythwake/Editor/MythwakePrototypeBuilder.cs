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
    private static readonly Color NavButtonColor = new Color(0.11f, 0.14f, 0.2f);

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
        SetRect(title.rectTransform, new Vector2(0, -70), new Vector2(920, 100), new Vector2(0.5f, 1f));

        var gold = CreateText("Gold Text", root.transform, "Gold 0   Gems 0   Essence 0", 36, FontStyles.Bold);
        SetRect(gold.rectTransform, new Vector2(0, -155), new Vector2(920, 70), new Vector2(0.5f, 1f));

        var homePanel = CreateScreen("Home Panel", root.transform);
        var battlePanel = CreateScreen("Battle Panel", root.transform);
        var heroesPanel = CreateScreen("Heroes Panel", root.transform);
        var gearPanel = CreateScreen("Gear Panel", root.transform);
        var summonPanel = CreateScreen("Summon Panel", root.transform);
        var shopPanel = CreateScreen("Shop Panel", root.transform);

        var homeHeader = CreateText("Home Header", homePanel.transform, "Campaign", 42, FontStyles.Bold);
        SetRect(homeHeader.rectTransform, new Vector2(0, -30), new Vector2(860, 60), new Vector2(0.5f, 1f));

        var homeCard = CreatePanel("Idle Chest Panel", homePanel.transform, PanelColor);
        SetRect(homeCard.rectTransform, new Vector2(0, -230), new Vector2(860, 300), new Vector2(0.5f, 1f));

        var homeStage = CreateText("Home Stage Text", homeCard.transform, "Campaign 1", 44, FontStyles.Bold);
        SetRect(homeStage.rectTransform, new Vector2(0, -45), new Vector2(780, 60), new Vector2(0.5f, 1f));

        var homeGold = CreateText("Home Gold Text", homeCard.transform, "0 Gold", 34, FontStyles.Normal);
        SetRect(homeGold.rectTransform, new Vector2(0, -115), new Vector2(780, 45), new Vector2(0.5f, 1f));

        var homeGems = CreateText("Home Gems Text", homeCard.transform, "0 Gems", 34, FontStyles.Normal);
        SetRect(homeGems.rectTransform, new Vector2(0, -165), new Vector2(780, 45), new Vector2(0.5f, 1f));
        homeGems.color = new Color(0.72f, 0.86f, 1f);

        var homeEssence = CreateText("Home Myth Essence Text", homeCard.transform, "0 Myth Essence", 34, FontStyles.Normal);
        SetRect(homeEssence.rectTransform, new Vector2(0, -215), new Vector2(780, 45), new Vector2(0.5f, 1f));
        homeEssence.color = new Color(0.82f, 0.76f, 0.52f);

        var homePower = CreateText("Home Power Text", homeCard.transform, "Power 14", 34, FontStyles.Normal);
        SetRect(homePower.rectTransform, new Vector2(0, -265), new Vector2(780, 45), new Vector2(0.5f, 1f));

        var offlineReward = CreateText("Offline Reward Text", homePanel.transform, "Offline: no reward yet", 32, FontStyles.Bold);
        SetRect(offlineReward.rectTransform, new Vector2(0, -440), new Vector2(860, 80), new Vector2(0.5f, 1f));
        offlineReward.color = new Color(0.82f, 0.76f, 0.52f);

        var nextGoal = CreateText("Next Goal Text", homePanel.transform, "Next Goal\nPush Campaign Stage 1", 26, FontStyles.Bold);
        SetRect(nextGoal.rectTransform, new Vector2(0, -540), new Vector2(860, 90), new Vector2(0.5f, 1f));
        nextGoal.color = new Color(0.72f, 0.86f, 1f);

        var teamSlotTexts = CreateHeroSlotRow(homePanel.transform, -645, "Team Preview");

        var dailyHeader = CreateText("Daily Missions Header", homePanel.transform, "Daily Missions", 34, FontStyles.Bold);
        SetRect(dailyHeader.rectTransform, new Vector2(0, -900), new Vector2(860, 50), new Vector2(0.5f, 1f));

        var dailyMissionTexts = new TMP_Text[3];
        var dailyMissionButtons = new Button[3];
        for (var i = 0; i < dailyMissionButtons.Length; i++)
        {
            dailyMissionButtons[i] = CreateButton($"Daily Mission Button {i + 1}", homePanel.transform, "Daily Mission", new Color(0.1f, 0.13f, 0.2f, 0.95f));
            SetRect(dailyMissionButtons[i].GetComponent<RectTransform>(), new Vector2(0, -990 - (i * 115)), new Vector2(860, 92), new Vector2(0.5f, 1f));

            dailyMissionTexts[i] = dailyMissionButtons[i].GetComponentInChildren<TMP_Text>();
            dailyMissionTexts[i].fontSize = 24;
            dailyMissionTexts[i].alignment = TextAlignmentOptions.Left;
        }

        var battleHeader = CreateText("Battle Header", battlePanel.transform, "Battle", 42, FontStyles.Bold);
        SetRect(battleHeader.rectTransform, new Vector2(0, -30), new Vector2(860, 60), new Vector2(0.5f, 1f));

        var damage = CreateText("Damage Text", battlePanel.transform, "ATK 1   HP 1   Guard -0%   Heal 0", 32, FontStyles.Normal);
        SetRect(damage.rectTransform, new Vector2(0, -105), new Vector2(920, 60), new Vector2(0.5f, 1f));

        var autoAttack = CreateText("Auto Attack Text", battlePanel.transform, "Auto Attack: 1.0s", 30, FontStyles.Normal);
        SetRect(autoAttack.rectTransform, new Vector2(0, -155), new Vector2(920, 46), new Vector2(0.5f, 1f));
        autoAttack.color = new Color(0.72f, 0.86f, 1f);

        var enemyPanel = CreatePanel("Enemy Panel", battlePanel.transform, PanelColor);
        SetRect(enemyPanel.rectTransform, new Vector2(0, -330), new Vector2(860, 230), new Vector2(0.5f, 1f));

        var enemyName = CreateText("Enemy Text", enemyPanel.transform, "Enemy Lv. 1", 46, FontStyles.Bold);
        SetRect(enemyName.rectTransform, new Vector2(0, -60), new Vector2(780, 80), new Vector2(0.5f, 1f));

        var enemyHp = CreateText("Enemy HP Text", enemyPanel.transform, "HP: 10 / 10", 40, FontStyles.Normal);
        SetRect(enemyHp.rectTransform, new Vector2(0, -150), new Vector2(780, 60), new Vector2(0.5f, 1f));

        var fightButton = CreateButton("Fight Button", battlePanel.transform, "Fight", ButtonColor);
        SetRect(fightButton.GetComponent<RectTransform>(), new Vector2(0, -560), new Vector2(760, 110), new Vector2(0.5f, 1f));

        var dungeonResult = CreateText("Dungeon Result Text", battlePanel.transform, "Dungeons are the active resource source.", 26, FontStyles.Bold);
        SetRect(dungeonResult.rectTransform, new Vector2(0, -695), new Vector2(860, 120), new Vector2(0.5f, 1f));
        dungeonResult.color = new Color(0.72f, 0.86f, 1f);

        var goldDungeonButton = CreateButton("Gold Dungeon Button", battlePanel.transform, "Gold Dungeon", new Color(0.45f, 0.34f, 0.12f));
        SetRect(goldDungeonButton.GetComponent<RectTransform>(), new Vector2(0, -825), new Vector2(860, 105), new Vector2(0.5f, 1f));
        var goldDungeonText = goldDungeonButton.GetComponentInChildren<TMP_Text>();
        goldDungeonText.fontSize = 24;
        goldDungeonText.alignment = TextAlignmentOptions.Left;

        var essenceDungeonButton = CreateButton("Essence Dungeon Button", battlePanel.transform, "Essence Dungeon", UpgradeButtonColor);
        SetRect(essenceDungeonButton.GetComponent<RectTransform>(), new Vector2(0, -950), new Vector2(860, 105), new Vector2(0.5f, 1f));
        var essenceDungeonText = essenceDungeonButton.GetComponentInChildren<TMP_Text>();
        essenceDungeonText.fontSize = 24;
        essenceDungeonText.alignment = TextAlignmentOptions.Left;

        var gearDungeonButton = CreateButton("Gear Dungeon Button", battlePanel.transform, "Gear Dungeon", new Color(0.24f, 0.29f, 0.52f));
        SetRect(gearDungeonButton.GetComponent<RectTransform>(), new Vector2(0, -1075), new Vector2(860, 105), new Vector2(0.5f, 1f));
        var gearDungeonText = gearDungeonButton.GetComponentInChildren<TMP_Text>();
        gearDungeonText.fontSize = 24;
        gearDungeonText.alignment = TextAlignmentOptions.Left;

        var upgradeButton = CreateButton("Upgrade Button", battlePanel.transform, "Upgrade Astra (12 Essence)", UpgradeButtonColor);
        SetRect(upgradeButton.GetComponent<RectTransform>(), new Vector2(0, -1205), new Vector2(760, 95), new Vector2(0.5f, 1f));
        upgradeButton.GetComponentInChildren<TMP_Text>().fontSize = 30;

        var resetButton = CreateButton("Reset Button", battlePanel.transform, "Reset Prototype", ResetButtonColor);
        SetRect(resetButton.GetComponent<RectTransform>(), new Vector2(0, -1320), new Vector2(560, 80), new Vector2(0.5f, 1f));
        resetButton.GetComponentInChildren<TMP_Text>().fontSize = 26;

        var heroHeader = CreateText("Hero Header", heroesPanel.transform, "Heroes", 42, FontStyles.Bold);
        SetRect(heroHeader.rectTransform, new Vector2(0, -30), new Vector2(860, 60), new Vector2(0.5f, 1f));

        var selectedHeroCard = CreatePanel("Selected Hero Card", heroesPanel.transform, PanelColor);
        SetRect(selectedHeroCard.rectTransform, new Vector2(0, -190), new Vector2(860, 230), new Vector2(0.5f, 1f));

        var selectedHeroText = CreateText("Selected Hero Text", selectedHeroCard.transform, "Astra  Lv. 1\nEpic Warrior\nATK 23  HP 178", 32, FontStyles.Bold);
        SetRect(selectedHeroText.rectTransform, new Vector2(0, -55), new Vector2(780, 150), new Vector2(0.5f, 1f));

        var heroUpgradeButton = CreateButton("Hero Upgrade Button", heroesPanel.transform, "Upgrade Astra (12 Essence)", UpgradeButtonColor);
        SetRect(heroUpgradeButton.GetComponent<RectTransform>(), new Vector2(0, -355), new Vector2(760, 100), new Vector2(0.5f, 1f));

        var heroAscendButton = CreateButton("Hero Ascend Button", heroesPanel.transform, "Ascend Astra (20 Shards)", ButtonColor);
        SetRect(heroAscendButton.GetComponent<RectTransform>(), new Vector2(0, -475), new Vector2(760, 100), new Vector2(0.5f, 1f));

        var equipmentPanel = CreatePanel("Equipment Panel", heroesPanel.transform, PanelColor);
        SetRect(equipmentPanel.rectTransform, new Vector2(0, -655), new Vector2(860, 230), new Vector2(0.5f, 1f));

        var equipmentSummary = CreateText("Equipment Summary Text", equipmentPanel.transform, "Equipment\nWeapon Lv. 1  +8 ATK\nArmor Lv. 1  +80 HP", 28, FontStyles.Bold);
        SetRect(equipmentSummary.rectTransform, new Vector2(0, -68), new Vector2(780, 105), new Vector2(0.5f, 1f));

        var weaponUpgradeButton = CreateButton("Weapon Upgrade Button", equipmentPanel.transform, "Weapon +1\n80 Gold", UpgradeButtonColor);
        SetRect(weaponUpgradeButton.GetComponent<RectTransform>(), new Vector2(-210, -165), new Vector2(360, 78), new Vector2(0.5f, 1f));
        weaponUpgradeButton.GetComponentInChildren<TMP_Text>().fontSize = 24;

        var armorUpgradeButton = CreateButton("Armor Upgrade Button", equipmentPanel.transform, "Armor +1\n75 Gold", UpgradeButtonColor);
        SetRect(armorUpgradeButton.GetComponent<RectTransform>(), new Vector2(210, -165), new Vector2(360, 78), new Vector2(0.5f, 1f));
        armorUpgradeButton.GetComponentInChildren<TMP_Text>().fontSize = 24;

        var heroCardTexts = new TMP_Text[5];
        var heroButtons = new Button[5];
        for (var i = 0; i < heroButtons.Length; i++)
        {
            heroButtons[i] = CreateButton($"Hero Card Button {i + 1}", heroesPanel.transform, "Hero", new Color(0.1f, 0.13f, 0.2f, 0.95f));
            SetRect(heroButtons[i].GetComponent<RectTransform>(), new Vector2(0, -825 - (i * 112)), new Vector2(860, 92), new Vector2(0.5f, 1f));

            heroCardTexts[i] = heroButtons[i].GetComponentInChildren<TMP_Text>();
            heroCardTexts[i].fontSize = 24;
            heroCardTexts[i].alignment = TextAlignmentOptions.Left;
        }

        var gearHeader = CreateText("Gear Header", gearPanel.transform, "Gear", 42, FontStyles.Bold);
        SetRect(gearHeader.rectTransform, new Vector2(0, -30), new Vector2(860, 60), new Vector2(0.5f, 1f));

        var accessorySummary = CreateText("Accessory Summary Text", gearPanel.transform, "Accessories\nATK +0  HP +0\nGear Dungeon Floor 1", 34, FontStyles.Bold);
        SetRect(accessorySummary.rectTransform, new Vector2(0, -145), new Vector2(860, 145), new Vector2(0.5f, 1f));

        var accessoryCard = CreatePanel("Accessory Card", gearPanel.transform, PanelColor);
        SetRect(accessoryCard.rectTransform, new Vector2(0, -410), new Vector2(860, 300), new Vector2(0.5f, 1f));

        var accessorySelected = CreateText("Accessory Selected Text", accessoryCard.transform, "Ohrringe\nEquipped: None\nSelected Fuse Tier: R0", 31, FontStyles.Bold);
        SetRect(accessorySelected.rectTransform, new Vector2(0, -85), new Vector2(780, 145), new Vector2(0.5f, 1f));

        var accessoryInventory = CreateText("Accessory Inventory Text", accessoryCard.transform, "Inventory Copies\nR0: 0\nR1: 0\nR2: 0\nR3: 0\nR4: 0", 25, FontStyles.Normal);
        SetRect(accessoryInventory.rectTransform, new Vector2(0, -215), new Vector2(780, 125), new Vector2(0.5f, 1f));
        accessoryInventory.alignment = TextAlignmentOptions.Left;

        var previousSlotButton = CreateButton("Accessory Previous Slot Button", gearPanel.transform, "Prev Slot", ButtonColor);
        SetRect(previousSlotButton.GetComponent<RectTransform>(), new Vector2(-290, -610), new Vector2(250, 76), new Vector2(0.5f, 1f));
        previousSlotButton.GetComponentInChildren<TMP_Text>().fontSize = 24;

        var nextSlotButton = CreateButton("Accessory Next Slot Button", gearPanel.transform, "Next Slot", ButtonColor);
        SetRect(nextSlotButton.GetComponent<RectTransform>(), new Vector2(0, -610), new Vector2(250, 76), new Vector2(0.5f, 1f));
        nextSlotButton.GetComponentInChildren<TMP_Text>().fontSize = 24;

        var equipAccessoryButton = CreateButton("Accessory Equip Button", gearPanel.transform, "Equip Selected", UpgradeButtonColor);
        SetRect(equipAccessoryButton.GetComponent<RectTransform>(), new Vector2(290, -610), new Vector2(250, 76), new Vector2(0.5f, 1f));
        equipAccessoryButton.GetComponentInChildren<TMP_Text>().fontSize = 23;

        var previousRarityButton = CreateButton("Accessory Previous Rarity Button", gearPanel.transform, "Prev Rarity", ButtonColor);
        SetRect(previousRarityButton.GetComponent<RectTransform>(), new Vector2(-290, -720), new Vector2(250, 76), new Vector2(0.5f, 1f));
        previousRarityButton.GetComponentInChildren<TMP_Text>().fontSize = 23;

        var nextRarityButton = CreateButton("Accessory Next Rarity Button", gearPanel.transform, "Next Rarity", ButtonColor);
        SetRect(nextRarityButton.GetComponent<RectTransform>(), new Vector2(0, -720), new Vector2(250, 76), new Vector2(0.5f, 1f));
        nextRarityButton.GetComponentInChildren<TMP_Text>().fontSize = 23;

        var fuseAccessoryButton = CreateButton("Accessory Fuse Button", gearPanel.transform, "Fuse 3 Copies", UpgradeButtonColor);
        SetRect(fuseAccessoryButton.GetComponent<RectTransform>(), new Vector2(290, -720), new Vector2(250, 76), new Vector2(0.5f, 1f));
        fuseAccessoryButton.GetComponentInChildren<TMP_Text>().fontSize = 23;

        var levelAccessoryButton = CreateButton("Accessory Level Button", gearPanel.transform, "Level Equipped", UpgradeButtonColor);
        SetRect(levelAccessoryButton.GetComponent<RectTransform>(), new Vector2(0, -840), new Vector2(760, 95), new Vector2(0.5f, 1f));
        levelAccessoryButton.GetComponentInChildren<TMP_Text>().fontSize = 28;

        var summonHeader = CreateText("Summon Header", summonPanel.transform, "Summon", 42, FontStyles.Bold);
        SetRect(summonHeader.rectTransform, new Vector2(0, -30), new Vector2(860, 60), new Vector2(0.5f, 1f));

        var summonBanner = CreatePanel("Summon Banner", summonPanel.transform, PanelColor);
        SetRect(summonBanner.rectTransform, new Vector2(0, -245), new Vector2(860, 310), new Vector2(0.5f, 1f));

        var summonTitle = CreateText("Summon Title", summonBanner.transform, "Awaken Heroes", 44, FontStyles.Bold);
        SetRect(summonTitle.rectTransform, new Vector2(0, -55), new Vector2(780, 60), new Vector2(0.5f, 1f));

        var summonCost = CreateText("Summon Cost Text", summonBanner.transform, "Cost: 30 Gems", 34, FontStyles.Normal);
        SetRect(summonCost.rectTransform, new Vector2(0, -125), new Vector2(780, 50), new Vector2(0.5f, 1f));
        summonCost.color = new Color(0.82f, 0.76f, 0.52f);

        var summonCount = CreateText("Summon Count Text", summonBanner.transform, "Summons: 0", 30, FontStyles.Normal);
        SetRect(summonCount.rectTransform, new Vector2(0, -180), new Vector2(780, 45), new Vector2(0.5f, 1f));

        var summonResult = CreateText("Summon Result Text", summonBanner.transform, "Summon heroes to collect shards and raise team power.", 30, FontStyles.Bold);
        SetRect(summonResult.rectTransform, new Vector2(0, -245), new Vector2(780, 80), new Vector2(0.5f, 1f));
        summonResult.color = new Color(0.72f, 0.86f, 1f);

        var summonButton = CreateButton("Summon Button", summonPanel.transform, "Summon x1", ButtonColor);
        SetRect(summonButton.GetComponent<RectTransform>(), new Vector2(0, -560), new Vector2(760, 135), new Vector2(0.5f, 1f));

        var summonRatesCard = CreatePanel("Summon Rates Card", summonPanel.transform, PanelColor);
        SetRect(summonRatesCard.rectTransform, new Vector2(0, -780), new Vector2(860, 175), new Vector2(0.5f, 1f));

        var summonRates = CreateText("Summon Rates Text", summonRatesCard.transform, "Rates\nRare 55%  Epic 35%  Legendary 10%", 31, FontStyles.Normal);
        SetRect(summonRates.rectTransform, new Vector2(0, -88), new Vector2(780, 110), new Vector2(0.5f, 1f));

        var shopHeader = CreateText("Shop Header", shopPanel.transform, "Shop", 42, FontStyles.Bold);
        SetRect(shopHeader.rectTransform, new Vector2(0, -30), new Vector2(860, 60), new Vector2(0.5f, 1f));

        var battlePassPanel = CreatePanel("Mission Track Panel", shopPanel.transform, PanelColor);
        SetRect(battlePassPanel.rectTransform, new Vector2(0, -260), new Vector2(860, 330), new Vector2(0.5f, 1f));

        var battlePassTitle = CreateText("Mission Track Title", battlePassPanel.transform, "Mission Track", 44, FontStyles.Bold);
        SetRect(battlePassTitle.rectTransform, new Vector2(0, -60), new Vector2(780, 60), new Vector2(0.5f, 1f));

        var battlePassProgress = CreateText("Mission Track Progress Text", battlePassPanel.transform, "Mission Track XP: 0", 32, FontStyles.Normal);
        SetRect(battlePassProgress.rectTransform, new Vector2(0, -150), new Vector2(780, 90), new Vector2(0.5f, 1f));
        battlePassProgress.color = new Color(0.82f, 0.76f, 0.52f);

        var shopInfo = CreateText("Shop Info Text", battlePassPanel.transform, "Claim daily missions to unlock this reward track.", 28, FontStyles.Normal);
        SetRect(shopInfo.rectTransform, new Vector2(0, -250), new Vector2(780, 60), new Vector2(0.5f, 1f));
        shopInfo.color = new Color(0.72f, 0.78f, 0.88f);

        var battlePassRewardTexts = new TMP_Text[5];
        var battlePassRewardButtons = new Button[5];
        for (var i = 0; i < battlePassRewardButtons.Length; i++)
        {
            battlePassRewardButtons[i] = CreateButton($"Mission Track Reward Button {i + 1}", shopPanel.transform, "Reward", new Color(0.1f, 0.13f, 0.2f, 0.95f));
            SetRect(battlePassRewardButtons[i].GetComponent<RectTransform>(), new Vector2(0, -510 - (i * 115)), new Vector2(860, 92), new Vector2(0.5f, 1f));

            battlePassRewardTexts[i] = battlePassRewardButtons[i].GetComponentInChildren<TMP_Text>();
            battlePassRewardTexts[i].fontSize = 24;
            battlePassRewardTexts[i].alignment = TextAlignmentOptions.Left;
        }

        var navPanel = CreatePanel("Bottom Navigation", root.transform, new Color(0.06f, 0.08f, 0.12f, 0.98f));
        SetRect(navPanel.rectTransform, new Vector2(0, 0), new Vector2(1080, 190), new Vector2(0.5f, 0f));

        var homeTab = CreateNavButton("Home Tab", navPanel.transform, "Home", -450);
        var battleTab = CreateNavButton("Battle Tab", navPanel.transform, "Battle", -270);
        var heroesTab = CreateNavButton("Heroes Tab", navPanel.transform, "Heroes", -90);
        var gearTab = CreateNavButton("Gear Tab", navPanel.transform, "Gear", 90);
        var summonTab = CreateNavButton("Summon Tab", navPanel.transform, "Summon", 270);
        var shopTab = CreateNavButton("Shop Tab", navPanel.transform, "Shop", 450);

        var controllerObject = new GameObject("Idle Prototype Controller");
        var controller = controllerObject.AddComponent<IdlePrototypeController>();
        var serializedController = new SerializedObject(controller);
        SetObject(serializedController, "titleText", title);
        SetObject(serializedController, "goldText", gold);
        SetObject(serializedController, "homeGoldText", homeGold);
        SetObject(serializedController, "gemsText", homeGems);
        SetObject(serializedController, "mythEssenceText", homeEssence);
        SetObject(serializedController, "homeStageText", homeStage);
        SetObject(serializedController, "homePowerText", homePower);
        SetObject(serializedController, "nextGoalText", nextGoal);
        SetObjectArray(serializedController, "teamSlotTexts", teamSlotTexts);
        SetObjectArray(serializedController, "dailyMissionTexts", dailyMissionTexts);
        SetObject(serializedController, "selectedHeroText", selectedHeroText);
        SetObjectArray(serializedController, "heroCardTexts", heroCardTexts);
        SetObject(serializedController, "damageText", damage);
        SetObject(serializedController, "enemyText", enemyName);
        SetObject(serializedController, "enemyHpText", enemyHp);
        SetObject(serializedController, "dungeonResultText", dungeonResult);
        SetObject(serializedController, "goldDungeonText", goldDungeonText);
        SetObject(serializedController, "essenceDungeonText", essenceDungeonText);
        SetObject(serializedController, "gearDungeonText", gearDungeonText);
        SetObject(serializedController, "autoAttackText", autoAttack);
        SetObject(serializedController, "offlineRewardText", offlineReward);
        SetObject(serializedController, "upgradeCostText", upgradeButton.GetComponentInChildren<TMP_Text>());
        SetObject(serializedController, "heroUpgradeCostText", heroUpgradeButton.GetComponentInChildren<TMP_Text>());
        SetObject(serializedController, "heroAscendCostText", heroAscendButton.GetComponentInChildren<TMP_Text>());
        SetObject(serializedController, "equipmentSummaryText", equipmentSummary);
        SetObject(serializedController, "weaponUpgradeCostText", weaponUpgradeButton.GetComponentInChildren<TMP_Text>());
        SetObject(serializedController, "armorUpgradeCostText", armorUpgradeButton.GetComponentInChildren<TMP_Text>());
        SetObject(serializedController, "accessorySummaryText", accessorySummary);
        SetObject(serializedController, "accessorySelectedText", accessorySelected);
        SetObject(serializedController, "accessoryInventoryText", accessoryInventory);
        SetObject(serializedController, "accessoryEquipText", equipAccessoryButton.GetComponentInChildren<TMP_Text>());
        SetObject(serializedController, "accessoryLevelText", levelAccessoryButton.GetComponentInChildren<TMP_Text>());
        SetObject(serializedController, "accessoryFuseText", fuseAccessoryButton.GetComponentInChildren<TMP_Text>());
        SetObject(serializedController, "summonCostText", summonCost);
        SetObject(serializedController, "summonResultText", summonResult);
        SetObject(serializedController, "summonRatesText", summonRates);
        SetObject(serializedController, "summonCountText", summonCount);
        SetObject(serializedController, "battlePassProgressText", battlePassProgress);
        SetObjectArray(serializedController, "battlePassRewardTexts", battlePassRewardTexts);
        SetObject(serializedController, "fightButton", fightButton);
        SetObject(serializedController, "goldDungeonButton", goldDungeonButton);
        SetObject(serializedController, "essenceDungeonButton", essenceDungeonButton);
        SetObject(serializedController, "gearDungeonButton", gearDungeonButton);
        SetObject(serializedController, "upgradeButton", upgradeButton);
        SetObject(serializedController, "heroUpgradeButton", heroUpgradeButton);
        SetObject(serializedController, "heroAscendButton", heroAscendButton);
        SetObject(serializedController, "weaponUpgradeButton", weaponUpgradeButton);
        SetObject(serializedController, "armorUpgradeButton", armorUpgradeButton);
        SetObject(serializedController, "accessoryPreviousSlotButton", previousSlotButton);
        SetObject(serializedController, "accessoryNextSlotButton", nextSlotButton);
        SetObject(serializedController, "accessoryPreviousRarityButton", previousRarityButton);
        SetObject(serializedController, "accessoryNextRarityButton", nextRarityButton);
        SetObject(serializedController, "accessoryEquipButton", equipAccessoryButton);
        SetObject(serializedController, "accessoryLevelButton", levelAccessoryButton);
        SetObject(serializedController, "accessoryFuseButton", fuseAccessoryButton);
        SetObject(serializedController, "summonButton", summonButton);
        SetObject(serializedController, "resetButton", resetButton);
        SetObjectArray(serializedController, "heroSelectButtons", heroButtons);
        SetObjectArray(serializedController, "dailyMissionButtons", dailyMissionButtons);
        SetObjectArray(serializedController, "battlePassRewardButtons", battlePassRewardButtons);
        SetObject(serializedController, "homePanel", homePanel);
        SetObject(serializedController, "battlePanel", battlePanel);
        SetObject(serializedController, "heroesPanel", heroesPanel);
        SetObject(serializedController, "gearPanel", gearPanel);
        SetObject(serializedController, "summonPanel", summonPanel);
        SetObject(serializedController, "shopPanel", shopPanel);
        SetObject(serializedController, "homeTabButton", homeTab);
        SetObject(serializedController, "battleTabButton", battleTab);
        SetObject(serializedController, "heroesTabButton", heroesTab);
        SetObject(serializedController, "gearTabButton", gearTab);
        SetObject(serializedController, "summonTabButton", summonTab);
        SetObject(serializedController, "shopTabButton", shopTab);
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

    private static GameObject CreateScreen(string name, Transform parent)
    {
        var screen = new GameObject(name, typeof(RectTransform));
        screen.transform.SetParent(parent, false);

        var rectTransform = screen.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.offsetMin = new Vector2(0f, 190f);
        rectTransform.offsetMax = new Vector2(0f, -220f);

        return screen;
    }

    private static TMP_Text[] CreateHeroSlotRow(Transform parent, float topOffset, string heading)
    {
        var labels = new TMP_Text[5];
        var header = CreateText($"{heading} Header", parent, heading, 34, FontStyles.Bold);
        SetRect(header.rectTransform, new Vector2(0, topOffset), new Vector2(860, 60), new Vector2(0.5f, 1f));

        for (var i = 0; i < 5; i++)
        {
            var slot = CreatePanel($"Hero Slot {i + 1}", parent, new Color(0.1f, 0.13f, 0.2f, 0.95f));
            SetRect(slot.rectTransform, new Vector2(-344 + (i * 172), topOffset - 145), new Vector2(145, 160), new Vector2(0.5f, 1f));

            var label = CreateText($"Hero Slot {i + 1} Label", slot.transform, i == 0 ? "Hero" : "Empty", 24, FontStyles.Bold);
            SetRect(label.rectTransform, new Vector2(0, -88), new Vector2(130, 115), new Vector2(0.5f, 1f));
            labels[i] = label;
        }

        return labels;
    }

    private static void CreatePlaceholderCard(Transform parent, string title, string body, float topOffset)
    {
        var card = CreatePanel($"{title} Card", parent, PanelColor);
        SetRect(card.rectTransform, new Vector2(0, topOffset), new Vector2(860, 220), new Vector2(0.5f, 1f));

        var titleText = CreateText($"{title} Title", card.transform, title, 42, FontStyles.Bold);
        SetRect(titleText.rectTransform, new Vector2(0, -55), new Vector2(780, 60), new Vector2(0.5f, 1f));

        var bodyText = CreateText($"{title} Body", card.transform, body, 30, FontStyles.Normal);
        SetRect(bodyText.rectTransform, new Vector2(0, -135), new Vector2(780, 70), new Vector2(0.5f, 1f));
        bodyText.color = new Color(0.72f, 0.78f, 0.88f);
    }

    private static Button CreateNavButton(string name, Transform parent, string label, float xPosition)
    {
        var button = CreateButton(name, parent, label, NavButtonColor);
        SetRect(button.GetComponent<RectTransform>(), new Vector2(xPosition, 0), new Vector2(160, 130), new Vector2(0.5f, 0.5f));

        var text = button.GetComponentInChildren<TMP_Text>();
        text.fontSize = 24;

        return button;
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

    private static void SetObjectArray(SerializedObject serializedObject, string propertyName, Object[] values)
    {
        var property = serializedObject.FindProperty(propertyName);
        property.arraySize = values.Length;

        for (var i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
