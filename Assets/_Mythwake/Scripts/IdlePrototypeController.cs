using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdlePrototypeController : MonoBehaviour
{
    [Serializable]
    private struct StageDefinition
    {
        public string enemyName;
        public int maxHp;
        public int goldReward;

        public StageDefinition(string enemyName, int maxHp, int goldReward)
        {
            this.enemyName = enemyName;
            this.maxHp = maxHp;
            this.goldReward = goldReward;
        }
    }

    private enum AppScreen
    {
        Home,
        Battle,
        Heroes,
        Summon,
        Shop
    }

    private const string GoldKey = "Mythwake.Prototype.Gold";
    private const string DamageKey = "Mythwake.Prototype.Damage";
    private const string EnemyLevelKey = "Mythwake.Prototype.EnemyLevel";
    private const string EnemyHpKey = "Mythwake.Prototype.EnemyHp";
    private const string EnemyMaxHpKey = "Mythwake.Prototype.EnemyMaxHp";
    private const string UpgradeCostKey = "Mythwake.Prototype.UpgradeCost";
    private const string LastSeenUtcKey = "Mythwake.Prototype.LastSeenUtc";
    private const string SelectedHeroKey = "Mythwake.Prototype.SelectedHero";
    private const string HeroLevelKeyPrefix = "Mythwake.Prototype.HeroLevel.";
    private const string HeroShardKeyPrefix = "Mythwake.Prototype.HeroShard.";
    private const string HeroAscensionKeyPrefix = "Mythwake.Prototype.HeroAscension.";
    private const string SummonCountKey = "Mythwake.Prototype.SummonCount";
    private const string DailyDateKey = "Mythwake.Prototype.Daily.Date";
    private const string DailyFightCountKey = "Mythwake.Prototype.Daily.FightCount";
    private const string DailyStageClearCountKey = "Mythwake.Prototype.Daily.StageClearCount";
    private const string DailySummonCountKey = "Mythwake.Prototype.Daily.SummonCount";
    private const string DailyMissionClaimedKeyPrefix = "Mythwake.Prototype.Daily.MissionClaimed.";
    private const int HeroCount = 5;
    private const int DailyMissionCount = 3;
    private const int SummonCost = 60;

    private static readonly string[] HeroNames = { "Astra", "Borin", "Cyra", "Dante", "Elowen" };
    private static readonly string[] HeroRoles = { "Warrior", "Tank", "Mage", "Ranger", "Support" };
    private static readonly string[] HeroRarities = { "Epic", "Rare", "Epic", "Rare", "Legendary" };
    private static readonly int[] HeroBasePower = { 42, 50, 46, 38, 58 };
    private static readonly int[] HeroPowerGrowth = { 13, 10, 15, 12, 11 };
    private static readonly int[] RareHeroIndexes = { 1, 3 };
    private static readonly int[] EpicHeroIndexes = { 0, 2 };
    private static readonly int[] LegendaryHeroIndexes = { 4 };
    private static readonly string[] DailyMissionTitles = { "Battle 20 times", "Clear 3 stages", "Summon 1 hero" };
    private static readonly int[] DailyMissionTargets = { 20, 3, 1 };
    private static readonly int[] DailyMissionRewards = { 75, 120, 80 };

    [Header("Stats")]
    [SerializeField] private int gold;
    [SerializeField] private int damage = 1;
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private int enemyHp = 10;
    [SerializeField] private int enemyMaxHp = 10;
    [SerializeField] private int upgradeCost = 10;
    [SerializeField] private int selectedHeroIndex;
    [SerializeField] private int[] heroLevels = new int[HeroCount];
    [SerializeField] private int[] heroShards = new int[HeroCount];
    [SerializeField] private int[] heroAscensions = new int[HeroCount];
    [SerializeField] private int summonCount;
    [SerializeField] private int dailyFightCount;
    [SerializeField] private int dailyStageClearCount;
    [SerializeField] private int dailySummonCount;
    [SerializeField] private bool[] dailyMissionClaimed = new bool[DailyMissionCount];

    [Header("Campaign")]
    [SerializeField]
    private StageDefinition[] stages =
    {
        new StageDefinition("Fallen Scout", 50, 7),
        new StageDefinition("Hollow Guard", 75, 10),
        new StageDefinition("Ashborne Rogue", 105, 14),
        new StageDefinition("Rift Hound", 145, 19),
        new StageDefinition("Veil Shaman", 195, 25),
        new StageDefinition("Dusk Knight", 260, 33),
        new StageDefinition("Cursed Warden", 345, 43),
        new StageDefinition("Abyss Herald", 455, 56),
        new StageDefinition("Eclipse Beast", 600, 73),
        new StageDefinition("Mythfallen Tyrant", 790, 95)
    };

    [Header("Idle")]
    [SerializeField] private bool autoAttackEnabled = true;
    [SerializeField] private float autoAttackInterval = 1f;
    [SerializeField] private int maxOfflineSeconds = 8 * 60 * 60;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text homeGoldText;
    [SerializeField] private TMP_Text homeStageText;
    [SerializeField] private TMP_Text homePowerText;
    [SerializeField] private TMP_Text[] teamSlotTexts;
    [SerializeField] private TMP_Text selectedHeroText;
    [SerializeField] private TMP_Text[] heroCardTexts;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text enemyText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private TMP_Text autoAttackText;
    [SerializeField] private TMP_Text offlineRewardText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text heroUpgradeCostText;
    [SerializeField] private TMP_Text heroAscendCostText;
    [SerializeField] private TMP_Text summonCostText;
    [SerializeField] private TMP_Text summonResultText;
    [SerializeField] private TMP_Text summonRatesText;
    [SerializeField] private TMP_Text summonCountText;
    [SerializeField] private TMP_Text[] dailyMissionTexts;
    [SerializeField] private Button fightButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button heroUpgradeButton;
    [SerializeField] private Button heroAscendButton;
    [SerializeField] private Button summonButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button[] heroSelectButtons;
    [SerializeField] private Button[] dailyMissionButtons;

    [Header("Navigation")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject heroesPanel;
    [SerializeField] private GameObject summonPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button homeTabButton;
    [SerializeField] private Button battleTabButton;
    [SerializeField] private Button heroesTabButton;
    [SerializeField] private Button summonTabButton;
    [SerializeField] private Button shopTabButton;
    [SerializeField] private Color activeTabColor = new Color(0.22f, 0.48f, 0.86f);
    [SerializeField] private Color inactiveTabColor = new Color(0.11f, 0.14f, 0.2f);

    private float autoAttackTimer;
    private int lastOfflineReward;
    private int lastOfflineSeconds;
    private AppScreen activeScreen = AppScreen.Home;

    private void Awake()
    {
        LoadProgress();
        ClaimOfflineRewards();
        RegisterNavigation();
        RegisterHeroButtons();
        RegisterDailyMissionButtons();

        if (fightButton != null)
        {
            fightButton.onClick.AddListener(Fight);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(UpgradeDamage);
        }

        if (heroUpgradeButton != null)
        {
            heroUpgradeButton.onClick.AddListener(UpgradeDamage);
        }

        if (heroAscendButton != null)
        {
            heroAscendButton.onClick.AddListener(AscendSelectedHero);
        }

        if (summonButton != null)
        {
            summonButton.onClick.AddListener(SummonOnce);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetProgress);
        }

        RefreshUi();
        ShowScreen(activeScreen);
    }

    private void Update()
    {
        if (!autoAttackEnabled)
        {
            return;
        }

        autoAttackTimer += Time.deltaTime;

        if (autoAttackTimer < autoAttackInterval)
        {
            RefreshAutoAttackUi();
            return;
        }

        autoAttackTimer -= autoAttackInterval;
        Fight(saveProgress: true);
    }

    private void OnDestroy()
    {
        if (fightButton != null)
        {
            fightButton.onClick.RemoveListener(Fight);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(UpgradeDamage);
        }

        if (heroUpgradeButton != null)
        {
            heroUpgradeButton.onClick.RemoveListener(UpgradeDamage);
        }

        if (heroAscendButton != null)
        {
            heroAscendButton.onClick.RemoveListener(AscendSelectedHero);
        }

        if (summonButton != null)
        {
            summonButton.onClick.RemoveListener(SummonOnce);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetProgress);
        }

        UnregisterNavigation();
        UnregisterHeroButtons();
        UnregisterDailyMissionButtons();
    }

    public void ShowHome()
    {
        ShowScreen(AppScreen.Home);
    }

    public void ShowBattle()
    {
        ShowScreen(AppScreen.Battle);
    }

    public void ShowHeroes()
    {
        ShowScreen(AppScreen.Heroes);
    }

    public void ShowSummon()
    {
        ShowScreen(AppScreen.Summon);
    }

    public void ShowShop()
    {
        ShowScreen(AppScreen.Shop);
    }

    public void Fight()
    {
        Fight(saveProgress: true);
    }

    public void UpgradeDamage()
    {
        selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);

        if (gold < upgradeCost)
        {
            RefreshUi();
            return;
        }

        gold -= upgradeCost;
        heroLevels[selectedHeroIndex]++;
        damage = GetTeamDamage();
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);

        SaveProgress();
        RefreshUi();
    }

    public void AscendSelectedHero()
    {
        selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        EnsureHeroShards();
        EnsureHeroAscensions();

        var ascendCost = GetHeroAscensionCost(selectedHeroIndex);
        if (heroShards[selectedHeroIndex] < ascendCost)
        {
            RefreshUi();
            return;
        }

        heroShards[selectedHeroIndex] -= ascendCost;
        heroAscensions[selectedHeroIndex]++;
        damage = GetTeamDamage();

        SaveProgress();
        RefreshUi();
    }

    public void SummonOnce()
    {
        if (gold < SummonCost)
        {
            SetSummonResult($"Need {SummonCost} Gold for a summon.");
            RefreshUi();
            return;
        }

        EnsureHeroShards();

        gold -= SummonCost;
        summonCount++;
        dailySummonCount++;

        var heroIndex = RollSummonHero();
        var shards = GetSummonShardReward(heroIndex);
        heroShards[heroIndex] += shards;
        selectedHeroIndex = heroIndex;
        damage = GetTeamDamage();

        SetSummonResult($"{HeroRarities[heroIndex]} pull: {HeroNames[heroIndex]}\n+{shards} shards");
        SaveProgress();
        RefreshUi();
    }

    public void ClaimDailyBattleMission()
    {
        ClaimDailyMission(0);
    }

    public void ClaimDailyStageMission()
    {
        ClaimDailyMission(1);
    }

    public void ClaimDailySummonMission()
    {
        ClaimDailyMission(2);
    }

    public void ResetProgress()
    {
        gold = 0;
        damage = 1;
        enemyLevel = 1;
        enemyMaxHp = GetStageMaxHp(enemyLevel);
        enemyHp = enemyMaxHp;
        selectedHeroIndex = 0;
        EnsureHeroLevels();
        for (var i = 0; i < heroLevels.Length; i++)
        {
            heroLevels[i] = 1;
        }

        EnsureHeroShards();
        EnsureHeroAscensions();
        for (var i = 0; i < heroShards.Length; i++)
        {
            heroShards[i] = 0;
            heroAscensions[i] = 0;
        }

        summonCount = 0;
        dailyFightCount = 0;
        dailyStageClearCount = 0;
        dailySummonCount = 0;
        EnsureDailyMissionClaims();
        for (var i = 0; i < dailyMissionClaimed.Length; i++)
        {
            dailyMissionClaimed[i] = false;
        }

        damage = GetTeamDamage();
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);
        autoAttackTimer = 0f;
        lastOfflineReward = 0;
        lastOfflineSeconds = 0;

        SaveProgress();
        RefreshUi();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            SaveProgress();
        }
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    private void Fight(bool saveProgress)
    {
        damage = GetTeamDamage();
        enemyHp -= damage;
        dailyFightCount++;

        if (enemyHp <= 0)
        {
            gold += GetStageReward(enemyLevel);
            enemyLevel++;
            dailyStageClearCount++;
            enemyMaxHp = GetStageMaxHp(enemyLevel);
            enemyHp = enemyMaxHp;
        }

        if (saveProgress)
        {
            SaveProgress();
        }

        RefreshUi();
    }

    private void LoadProgress()
    {
        gold = PlayerPrefs.GetInt(GoldKey, gold);
        enemyLevel = Mathf.Max(1, PlayerPrefs.GetInt(EnemyLevelKey, enemyLevel));
        enemyMaxHp = Mathf.Max(GetStageMaxHp(enemyLevel), PlayerPrefs.GetInt(EnemyMaxHpKey, enemyMaxHp));
        enemyHp = Mathf.Clamp(PlayerPrefs.GetInt(EnemyHpKey, enemyHp), 1, enemyMaxHp);
        selectedHeroIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedHeroKey, selectedHeroIndex), 0, HeroCount - 1);
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();

        for (var i = 0; i < heroLevels.Length; i++)
        {
            heroLevels[i] = Mathf.Max(1, PlayerPrefs.GetInt($"{HeroLevelKeyPrefix}{i}", 1));
            heroShards[i] = Mathf.Max(0, PlayerPrefs.GetInt($"{HeroShardKeyPrefix}{i}", 0));
            heroAscensions[i] = Mathf.Max(0, PlayerPrefs.GetInt($"{HeroAscensionKeyPrefix}{i}", 0));
        }

        summonCount = Mathf.Max(0, PlayerPrefs.GetInt(SummonCountKey, summonCount));
        LoadDailyProgress();
        damage = GetTeamDamage();
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(GoldKey, gold);
        PlayerPrefs.SetInt(DamageKey, damage);
        PlayerPrefs.SetInt(EnemyLevelKey, enemyLevel);
        PlayerPrefs.SetInt(EnemyHpKey, enemyHp);
        PlayerPrefs.SetInt(EnemyMaxHpKey, enemyMaxHp);
        PlayerPrefs.SetInt(UpgradeCostKey, upgradeCost);
        PlayerPrefs.SetInt(SelectedHeroKey, selectedHeroIndex);
        PlayerPrefs.SetInt(SummonCountKey, summonCount);
        PlayerPrefs.SetString(DailyDateKey, GetDailyDateKey());
        PlayerPrefs.SetInt(DailyFightCountKey, dailyFightCount);
        PlayerPrefs.SetInt(DailyStageClearCountKey, dailyStageClearCount);
        PlayerPrefs.SetInt(DailySummonCountKey, dailySummonCount);

        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();
        EnsureDailyMissionClaims();
        for (var i = 0; i < heroLevels.Length; i++)
        {
            PlayerPrefs.SetInt($"{HeroLevelKeyPrefix}{i}", heroLevels[i]);
            PlayerPrefs.SetInt($"{HeroShardKeyPrefix}{i}", heroShards[i]);
            PlayerPrefs.SetInt($"{HeroAscensionKeyPrefix}{i}", heroAscensions[i]);
        }

        for (var i = 0; i < dailyMissionClaimed.Length; i++)
        {
            PlayerPrefs.SetInt($"{DailyMissionClaimedKeyPrefix}{i}", dailyMissionClaimed[i] ? 1 : 0);
        }

        PlayerPrefs.SetString(LastSeenUtcKey, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    private void ClaimOfflineRewards()
    {
        lastOfflineReward = 0;
        lastOfflineSeconds = 0;

        var rawTicks = PlayerPrefs.GetString(LastSeenUtcKey, string.Empty);
        if (!long.TryParse(rawTicks, out var ticks))
        {
            SaveProgress();
            return;
        }

        var lastSeenUtc = new DateTime(ticks, DateTimeKind.Utc);
        var elapsedSeconds = Mathf.FloorToInt((float)(DateTime.UtcNow - lastSeenUtc).TotalSeconds);
        lastOfflineSeconds = Mathf.Clamp(elapsedSeconds, 0, maxOfflineSeconds);

        if (lastOfflineSeconds <= 0)
        {
            SaveProgress();
            return;
        }

        lastOfflineReward = CalculateOfflineReward(lastOfflineSeconds);
        gold += lastOfflineReward;
        SaveProgress();
    }

    private int CalculateOfflineReward(int offlineSeconds)
    {
        var attacks = Mathf.FloorToInt(offlineSeconds / Mathf.Max(0.1f, autoAttackInterval));
        var enemyClearSeconds = Mathf.Max(1, Mathf.CeilToInt(enemyMaxHp / (float)Mathf.Max(1, GetTeamDamage())));
        var enemyKills = Mathf.Max(0, attacks / enemyClearSeconds);

        return enemyKills * GetStageReward(enemyLevel);
    }

    private StageDefinition GetStageDefinition(int stage)
    {
        stage = Mathf.Max(1, stage);
        EnsureStages();

        if (stage <= stages.Length)
        {
            return stages[stage - 1];
        }

        var lastStage = stages[stages.Length - 1];
        var overflow = stage - stages.Length;
        var hp = Mathf.CeilToInt(lastStage.maxHp * Mathf.Pow(1.18f, overflow));
        var reward = Mathf.CeilToInt(lastStage.goldReward * Mathf.Pow(1.12f, overflow));

        return new StageDefinition($"Rift Echo {stage}", hp, reward);
    }

    private int GetStageReward(int stage)
    {
        return Mathf.Max(1, GetStageDefinition(stage).goldReward);
    }

    private int GetStageMaxHp(int stage)
    {
        return Mathf.Max(1, GetStageDefinition(stage).maxHp);
    }

    private void RefreshUi()
    {
        EnsureHeroLevels();
        damage = GetTeamDamage();
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);

        if (titleText != null)
        {
            titleText.text = "Mythwake";
        }

        if (goldText != null)
        {
            goldText.text = $"Gold: {gold}";
        }

        if (homeGoldText != null)
        {
            homeGoldText.text = $"{gold} Gold";
        }

        if (homeStageText != null)
        {
            var stage = GetStageDefinition(enemyLevel);
            homeStageText.text = $"Campaign {enemyLevel}\n{stage.enemyName}";
        }

        if (homePowerText != null)
        {
            homePowerText.text = $"Team Power {GetTeamPower()}";
        }

        if (damageText != null)
        {
            damageText.text = $"Team Damage: {damage}";
        }

        if (enemyText != null)
        {
            var stage = GetStageDefinition(enemyLevel);
            enemyText.text = $"Stage {enemyLevel}: {stage.enemyName}\nReward {stage.goldReward} Gold";
        }

        if (enemyHpText != null)
        {
            enemyHpText.text = $"HP: {Mathf.Max(enemyHp, 0)} / {enemyMaxHp}";
        }

        RefreshAutoAttackUi();
        RefreshOfflineRewardUi();
        RefreshHeroUi();
        RefreshSummonUi();
        RefreshDailyMissionUi();

        if (upgradeCostText != null)
        {
            upgradeCostText.text = $"Upgrade {HeroNames[selectedHeroIndex]} ({upgradeCost} Gold)";
        }

        if (heroUpgradeCostText != null)
        {
            heroUpgradeCostText.text = $"Upgrade {HeroNames[selectedHeroIndex]} ({upgradeCost} Gold)";
        }

        if (heroAscendCostText != null)
        {
            heroAscendCostText.text = $"Ascend {HeroNames[selectedHeroIndex]} ({GetHeroAscensionCost(selectedHeroIndex)} Shards)";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = gold >= upgradeCost;
        }

        if (heroUpgradeButton != null)
        {
            heroUpgradeButton.interactable = gold >= upgradeCost;
        }

        if (heroAscendButton != null)
        {
            heroAscendButton.interactable = heroShards[selectedHeroIndex] >= GetHeroAscensionCost(selectedHeroIndex);
        }

        if (summonButton != null)
        {
            summonButton.interactable = gold >= SummonCost;
        }
    }

    private void RegisterHeroButtons()
    {
        if (heroSelectButtons == null || heroSelectButtons.Length == 0)
        {
            return;
        }

        if (heroSelectButtons.Length > 0 && heroSelectButtons[0] != null) heroSelectButtons[0].onClick.AddListener(SelectHero0);
        if (heroSelectButtons.Length > 1 && heroSelectButtons[1] != null) heroSelectButtons[1].onClick.AddListener(SelectHero1);
        if (heroSelectButtons.Length > 2 && heroSelectButtons[2] != null) heroSelectButtons[2].onClick.AddListener(SelectHero2);
        if (heroSelectButtons.Length > 3 && heroSelectButtons[3] != null) heroSelectButtons[3].onClick.AddListener(SelectHero3);
        if (heroSelectButtons.Length > 4 && heroSelectButtons[4] != null) heroSelectButtons[4].onClick.AddListener(SelectHero4);
    }

    private void UnregisterHeroButtons()
    {
        if (heroSelectButtons == null || heroSelectButtons.Length == 0)
        {
            return;
        }

        if (heroSelectButtons.Length > 0 && heroSelectButtons[0] != null) heroSelectButtons[0].onClick.RemoveListener(SelectHero0);
        if (heroSelectButtons.Length > 1 && heroSelectButtons[1] != null) heroSelectButtons[1].onClick.RemoveListener(SelectHero1);
        if (heroSelectButtons.Length > 2 && heroSelectButtons[2] != null) heroSelectButtons[2].onClick.RemoveListener(SelectHero2);
        if (heroSelectButtons.Length > 3 && heroSelectButtons[3] != null) heroSelectButtons[3].onClick.RemoveListener(SelectHero3);
        if (heroSelectButtons.Length > 4 && heroSelectButtons[4] != null) heroSelectButtons[4].onClick.RemoveListener(SelectHero4);
    }

    private void RegisterDailyMissionButtons()
    {
        if (dailyMissionButtons == null || dailyMissionButtons.Length == 0)
        {
            return;
        }

        if (dailyMissionButtons.Length > 0 && dailyMissionButtons[0] != null) dailyMissionButtons[0].onClick.AddListener(ClaimDailyBattleMission);
        if (dailyMissionButtons.Length > 1 && dailyMissionButtons[1] != null) dailyMissionButtons[1].onClick.AddListener(ClaimDailyStageMission);
        if (dailyMissionButtons.Length > 2 && dailyMissionButtons[2] != null) dailyMissionButtons[2].onClick.AddListener(ClaimDailySummonMission);
    }

    private void UnregisterDailyMissionButtons()
    {
        if (dailyMissionButtons == null || dailyMissionButtons.Length == 0)
        {
            return;
        }

        if (dailyMissionButtons.Length > 0 && dailyMissionButtons[0] != null) dailyMissionButtons[0].onClick.RemoveListener(ClaimDailyBattleMission);
        if (dailyMissionButtons.Length > 1 && dailyMissionButtons[1] != null) dailyMissionButtons[1].onClick.RemoveListener(ClaimDailyStageMission);
        if (dailyMissionButtons.Length > 2 && dailyMissionButtons[2] != null) dailyMissionButtons[2].onClick.RemoveListener(ClaimDailySummonMission);
    }

    private void SelectHero0() => SelectHero(0);
    private void SelectHero1() => SelectHero(1);
    private void SelectHero2() => SelectHero(2);
    private void SelectHero3() => SelectHero(3);
    private void SelectHero4() => SelectHero(4);

    private void SelectHero(int index)
    {
        selectedHeroIndex = Mathf.Clamp(index, 0, HeroCount - 1);
        SaveProgress();
        RefreshUi();
    }

    private void RegisterNavigation()
    {
        if (homeTabButton != null)
        {
            homeTabButton.onClick.AddListener(ShowHome);
        }

        if (battleTabButton != null)
        {
            battleTabButton.onClick.AddListener(ShowBattle);
        }

        if (heroesTabButton != null)
        {
            heroesTabButton.onClick.AddListener(ShowHeroes);
        }

        if (summonTabButton != null)
        {
            summonTabButton.onClick.AddListener(ShowSummon);
        }

        if (shopTabButton != null)
        {
            shopTabButton.onClick.AddListener(ShowShop);
        }
    }

    private void UnregisterNavigation()
    {
        if (homeTabButton != null)
        {
            homeTabButton.onClick.RemoveListener(ShowHome);
        }

        if (battleTabButton != null)
        {
            battleTabButton.onClick.RemoveListener(ShowBattle);
        }

        if (heroesTabButton != null)
        {
            heroesTabButton.onClick.RemoveListener(ShowHeroes);
        }

        if (summonTabButton != null)
        {
            summonTabButton.onClick.RemoveListener(ShowSummon);
        }

        if (shopTabButton != null)
        {
            shopTabButton.onClick.RemoveListener(ShowShop);
        }
    }

    private void ShowScreen(AppScreen screen)
    {
        activeScreen = screen;

        SetPanel(homePanel, screen == AppScreen.Home);
        SetPanel(battlePanel, screen == AppScreen.Battle);
        SetPanel(heroesPanel, screen == AppScreen.Heroes);
        SetPanel(summonPanel, screen == AppScreen.Summon);
        SetPanel(shopPanel, screen == AppScreen.Shop);

        SetTabState(homeTabButton, screen == AppScreen.Home);
        SetTabState(battleTabButton, screen == AppScreen.Battle);
        SetTabState(heroesTabButton, screen == AppScreen.Heroes);
        SetTabState(summonTabButton, screen == AppScreen.Summon);
        SetTabState(shopTabButton, screen == AppScreen.Shop);
    }

    private void SetPanel(GameObject panel, bool isVisible)
    {
        if (panel != null)
        {
            panel.SetActive(isVisible);
        }
    }

    private void SetTabState(Button button, bool isActive)
    {
        if (button == null || button.targetGraphic == null)
        {
            return;
        }

        button.targetGraphic.color = isActive ? activeTabColor : inactiveTabColor;
    }

    private void RefreshHeroUi()
    {
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();

        if (teamSlotTexts != null)
        {
            for (var i = 0; i < Mathf.Min(teamSlotTexts.Length, HeroCount); i++)
            {
                if (teamSlotTexts[i] != null)
                {
                    teamSlotTexts[i].text = $"{HeroNames[i]}\nLv. {heroLevels[i]}  A{heroAscensions[i]}";
                }
            }
        }

        if (selectedHeroText != null)
        {
            selectedHeroText.text = $"{HeroNames[selectedHeroIndex]}  Lv. {heroLevels[selectedHeroIndex]}  Asc. {heroAscensions[selectedHeroIndex]}\n{HeroRarities[selectedHeroIndex]} {HeroRoles[selectedHeroIndex]}\nPower {GetHeroPower(selectedHeroIndex)}  Shards {heroShards[selectedHeroIndex]}";
        }

        if (heroCardTexts != null)
        {
            for (var i = 0; i < Mathf.Min(heroCardTexts.Length, HeroCount); i++)
            {
                if (heroCardTexts[i] != null)
                {
                    var marker = i == selectedHeroIndex ? "> " : string.Empty;
                    heroCardTexts[i].text = $"{marker}{HeroNames[i]}  Lv. {heroLevels[i]}  A{heroAscensions[i]}\n{HeroRarities[i]} {HeroRoles[i]}  Power {GetHeroPower(i)}  Shards {heroShards[i]}";
                }
            }
        }
    }

    private void EnsureHeroLevels()
    {
        if (heroLevels == null || heroLevels.Length != HeroCount)
        {
            heroLevels = new int[HeroCount];
        }

        for (var i = 0; i < heroLevels.Length; i++)
        {
            if (heroLevels[i] <= 0)
            {
                heroLevels[i] = 1;
            }
        }
    }

    private void EnsureHeroShards()
    {
        if (heroShards == null || heroShards.Length != HeroCount)
        {
            heroShards = new int[HeroCount];
        }

        for (var i = 0; i < heroShards.Length; i++)
        {
            if (heroShards[i] < 0)
            {
                heroShards[i] = 0;
            }
        }
    }

    private void EnsureHeroAscensions()
    {
        if (heroAscensions == null || heroAscensions.Length != HeroCount)
        {
            heroAscensions = new int[HeroCount];
        }

        for (var i = 0; i < heroAscensions.Length; i++)
        {
            if (heroAscensions[i] < 0)
            {
                heroAscensions[i] = 0;
            }
        }
    }

    private void EnsureDailyMissionClaims()
    {
        if (dailyMissionClaimed == null || dailyMissionClaimed.Length != DailyMissionCount)
        {
            dailyMissionClaimed = new bool[DailyMissionCount];
        }
    }

    private void LoadDailyProgress()
    {
        EnsureDailyMissionClaims();

        var today = GetDailyDateKey();
        var savedDate = PlayerPrefs.GetString(DailyDateKey, string.Empty);
        if (savedDate != today)
        {
            dailyFightCount = 0;
            dailyStageClearCount = 0;
            dailySummonCount = 0;
            for (var i = 0; i < dailyMissionClaimed.Length; i++)
            {
                dailyMissionClaimed[i] = false;
            }

            return;
        }

        dailyFightCount = Mathf.Max(0, PlayerPrefs.GetInt(DailyFightCountKey, dailyFightCount));
        dailyStageClearCount = Mathf.Max(0, PlayerPrefs.GetInt(DailyStageClearCountKey, dailyStageClearCount));
        dailySummonCount = Mathf.Max(0, PlayerPrefs.GetInt(DailySummonCountKey, dailySummonCount));
        for (var i = 0; i < dailyMissionClaimed.Length; i++)
        {
            dailyMissionClaimed[i] = PlayerPrefs.GetInt($"{DailyMissionClaimedKeyPrefix}{i}", 0) == 1;
        }
    }

    private void ClaimDailyMission(int missionIndex)
    {
        missionIndex = Mathf.Clamp(missionIndex, 0, DailyMissionCount - 1);
        EnsureDailyMissionClaims();

        if (dailyMissionClaimed[missionIndex] || GetDailyMissionProgress(missionIndex) < DailyMissionTargets[missionIndex])
        {
            RefreshUi();
            return;
        }

        dailyMissionClaimed[missionIndex] = true;
        gold += DailyMissionRewards[missionIndex];

        SaveProgress();
        RefreshUi();
    }

    private int GetDailyMissionProgress(int missionIndex)
    {
        switch (missionIndex)
        {
            case 0:
                return dailyFightCount;
            case 1:
                return dailyStageClearCount;
            case 2:
                return dailySummonCount;
            default:
                return 0;
        }
    }

    private string GetDailyDateKey()
    {
        return DateTime.UtcNow.ToString("yyyyMMdd");
    }

    private void EnsureStages()
    {
        if (stages != null && stages.Length > 0)
        {
            return;
        }

        stages = new StageDefinition[]
        {
            new StageDefinition("Fallen Scout", 50, 7),
            new StageDefinition("Hollow Guard", 75, 10),
            new StageDefinition("Ashborne Rogue", 105, 14),
            new StageDefinition("Rift Hound", 145, 19),
            new StageDefinition("Veil Shaman", 195, 25)
        };
    }

    private int GetTeamPower()
    {
        var power = 0;

        for (var i = 0; i < HeroCount; i++)
        {
            power += GetHeroPower(i);
        }

        return power;
    }

    private int GetTeamDamage()
    {
        return Mathf.Max(1, Mathf.FloorToInt(GetTeamPower() / 28f));
    }

    private int GetHeroPower(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();
        return HeroBasePower[index] + (heroLevels[index] * HeroPowerGrowth[index]) + Mathf.FloorToInt(heroShards[index] * 0.5f) + (heroAscensions[index] * GetHeroAscensionPower(index));
    }

    private int GetHeroUpgradeCost(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroLevels();
        return Mathf.CeilToInt(12 * Mathf.Pow(1.32f, heroLevels[index] - 1));
    }

    private int GetHeroAscensionCost(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroAscensions();

        var baseCost = 20;
        if (HeroRarities[index] == "Epic")
        {
            baseCost = 25;
        }
        else if (HeroRarities[index] == "Legendary")
        {
            baseCost = 30;
        }

        return baseCost + (heroAscensions[index] * 15);
    }

    private int GetHeroAscensionPower(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);

        if (HeroRarities[index] == "Legendary")
        {
            return 65;
        }

        if (HeroRarities[index] == "Epic")
        {
            return 52;
        }

        return 42;
    }

    private void RefreshAutoAttackUi()
    {
        if (autoAttackText == null)
        {
            return;
        }

        if (!autoAttackEnabled)
        {
            autoAttackText.text = "Auto Attack: Off";
            return;
        }

        var remaining = Mathf.Max(0f, autoAttackInterval - autoAttackTimer);
        autoAttackText.text = $"Auto Attack: {remaining:0.0}s";
    }

    private void RefreshOfflineRewardUi()
    {
        if (offlineRewardText == null)
        {
            return;
        }

        if (lastOfflineReward <= 0)
        {
            offlineRewardText.text = "Offline: no reward yet";
            return;
        }

        offlineRewardText.text = $"Offline: +{lastOfflineReward} Gold ({FormatDuration(lastOfflineSeconds)})";
    }

    private void RefreshSummonUi()
    {
        if (summonCostText != null)
        {
            summonCostText.text = $"Cost: {SummonCost} Gold";
        }

        if (summonRatesText != null)
        {
            summonRatesText.text = "Rates\nRare 55%  Epic 35%  Legendary 10%";
        }

        if (summonCountText != null)
        {
            summonCountText.text = $"Summons: {summonCount}";
        }

        if (summonResultText != null && string.IsNullOrWhiteSpace(summonResultText.text))
        {
            summonResultText.text = "Summon heroes to collect shards and raise team power.";
        }
    }

    private void RefreshDailyMissionUi()
    {
        EnsureDailyMissionClaims();

        for (var i = 0; i < DailyMissionCount; i++)
        {
            var progress = Mathf.Min(GetDailyMissionProgress(i), DailyMissionTargets[i]);
            var isComplete = progress >= DailyMissionTargets[i];
            var isClaimed = dailyMissionClaimed[i];
            var state = isClaimed ? "Claimed" : isComplete ? "Claim" : $"{progress}/{DailyMissionTargets[i]}";
            var text = $"{DailyMissionTitles[i]}\n{state}  Reward {DailyMissionRewards[i]} Gold";

            if (dailyMissionTexts != null && i < dailyMissionTexts.Length && dailyMissionTexts[i] != null)
            {
                dailyMissionTexts[i].text = text;
            }

            if (dailyMissionButtons != null && i < dailyMissionButtons.Length && dailyMissionButtons[i] != null)
            {
                dailyMissionButtons[i].interactable = isComplete && !isClaimed;
            }
        }
    }

    private int RollSummonHero()
    {
        var roll = UnityEngine.Random.Range(0, 100);

        if (roll < 10)
        {
            return PickRandomHero(LegendaryHeroIndexes);
        }

        if (roll < 45)
        {
            return PickRandomHero(EpicHeroIndexes);
        }

        return PickRandomHero(RareHeroIndexes);
    }

    private int PickRandomHero(int[] heroIndexes)
    {
        if (heroIndexes == null || heroIndexes.Length == 0)
        {
            return 0;
        }

        return heroIndexes[UnityEngine.Random.Range(0, heroIndexes.Length)];
    }

    private int GetSummonShardReward(int heroIndex)
    {
        var rarity = HeroRarities[Mathf.Clamp(heroIndex, 0, HeroCount - 1)];

        if (rarity == "Legendary")
        {
            return 5;
        }

        if (rarity == "Epic")
        {
            return 7;
        }

        return 10;
    }

    private void SetSummonResult(string result)
    {
        if (summonResultText != null)
        {
            summonResultText.text = result;
        }
    }

    private string FormatDuration(int seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);

        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}h {time.Minutes}m";
        }

        if (time.TotalMinutes >= 1)
        {
            return $"{time.Minutes}m {time.Seconds}s";
        }

        return $"{time.Seconds}s";
    }
}
