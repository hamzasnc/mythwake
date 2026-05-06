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
        public int essenceReward;

        public StageDefinition(string enemyName, int maxHp, int essenceReward)
        {
            this.enemyName = enemyName;
            this.maxHp = maxHp;
            this.essenceReward = essenceReward;
        }
    }

    private struct CombatResult
    {
        public bool won;
        public bool executed;
        public int rounds;
        public int teamHpRemaining;
        public int enemyHpRemaining;
        public int damageDealt;
        public int damageTaken;
        public int healingDone;
    }

    private struct EquipmentTrackDefinition
    {
        public string name;
        public string statLabel;
        public int baseBonus;
        public int bonusPerLevel;
        public int baseCost;
        public float costGrowth;

        public EquipmentTrackDefinition(string name, string statLabel, int baseBonus, int bonusPerLevel, int baseCost, float costGrowth)
        {
            this.name = name;
            this.statLabel = statLabel;
            this.baseBonus = baseBonus;
            this.bonusPerLevel = bonusPerLevel;
            this.baseCost = baseCost;
            this.costGrowth = costGrowth;
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
    private const string GemsKey = "Mythwake.Prototype.Gems";
    private const string MythEssenceKey = "Mythwake.Prototype.MythEssence";
    private const string DamageKey = "Mythwake.Prototype.Damage";
    private const string EnemyLevelKey = "Mythwake.Prototype.EnemyLevel";
    private const string EnemyHpKey = "Mythwake.Prototype.EnemyHp";
    private const string EnemyMaxHpKey = "Mythwake.Prototype.EnemyMaxHp";
    private const string UpgradeCostKey = "Mythwake.Prototype.UpgradeCost";
    private const string GoldDungeonFloorKey = "Mythwake.Prototype.Dungeon.GoldFloor";
    private const string EssenceDungeonFloorKey = "Mythwake.Prototype.Dungeon.EssenceFloor";
    private const string WeaponLevelKey = "Mythwake.Prototype.Equipment.WeaponLevel";
    private const string ArmorLevelKey = "Mythwake.Prototype.Equipment.ArmorLevel";
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
    private const string BattlePassXpKey = "Mythwake.Prototype.BattlePass.Xp";
    private const string BattlePassClaimedKeyPrefix = "Mythwake.Prototype.BattlePass.Claimed.";
    private const int HeroCount = 5;
    private const int DailyMissionCount = 3;
    private const int BattlePassRewardCount = 5;
    private const int BattlePassXpPerDailyClaim = 40;
    private const int SummonCost = 30;
    private const int StarterGems = 30;
    private const int StarterMythEssence = 20;
    private const float OfflineGoldRewardRate = 0.65f;
    private const int MaxCombatRounds = 45;
    private const float WarriorDamageBonusRate = 0.06f;
    private const float MageDamageBonusRate = 0.1f;
    private const float TankDamageReductionRate = 0.18f;
    private const float SupportHealRate = 0.04f;
    private const float RangerExecuteThresholdRate = 0.12f;
    private const int StarterEquipmentLevel = 1;
    private const float CampaignOverflowHpGrowth = 1.25f;
    private const float CampaignOverflowRewardGrowth = 1.14f;

    private static readonly string[] HeroNames = { "Astra", "Borin", "Cyra", "Dante", "Elowen" };
    private static readonly string[] HeroRoles = { "Warrior", "Tank", "Mage", "Ranger", "Support" };
    private static readonly string[] HeroRarities = { "Epic", "Rare", "Epic", "Rare", "Legendary" };
    private static readonly int[] HeroBaseAttack = { 18, 10, 22, 20, 12 };
    private static readonly int[] HeroAttackGrowth = { 5, 3, 7, 6, 4 };
    private static readonly int[] HeroBaseHealth = { 150, 230, 110, 125, 165 };
    private static readonly int[] HeroHealthGrowth = { 28, 42, 20, 23, 34 };
    private static readonly int[] RareHeroIndexes = { 1, 3 };
    private static readonly int[] EpicHeroIndexes = { 0, 2 };
    private static readonly int[] LegendaryHeroIndexes = { 4 };
    private static readonly string[] DailyMissionTitles = { "Battle 20 times", "Clear 3 stages", "Summon 1 hero" };
    private static readonly int[] DailyMissionTargets = { 20, 3, 1 };
    private static readonly int[] DailyMissionGoldRewards = { 25, 50, 25 };
    private static readonly int[] DailyMissionGemRewards = { 5, 10, 15 };
    private static readonly int[] DailyMissionEssenceRewards = { 80, 120, 60 };
    private static readonly int[] BattlePassRewardXp = { 40, 80, 120, 180, 240 };
    private static readonly int[] BattlePassGoldRewards = { 100, 125, 175, 225, 350 };
    private static readonly int[] BattlePassGemRewards = { 10, 15, 20, 25, 40 };
    private static readonly int[] BattlePassEssenceRewards = { 0, 120, 0, 180, 300 };
    private static readonly EquipmentTrackDefinition WeaponTrack = new EquipmentTrackDefinition("Weapon", "ATK", 8, 9, 80, 1.45f);
    private static readonly EquipmentTrackDefinition ArmorTrack = new EquipmentTrackDefinition("Armor", "HP", 80, 65, 75, 1.42f);

    [Header("Stats")]
    [SerializeField] private int gold;
    [SerializeField] private int gems;
    [SerializeField] private int mythEssence;
    [SerializeField] private int damage = 1;
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private int enemyHp = 10;
    [SerializeField] private int enemyMaxHp = 10;
    [SerializeField] private int upgradeCost = 10;
    [SerializeField] private int goldDungeonFloor = 1;
    [SerializeField] private int essenceDungeonFloor = 1;
    [SerializeField] private int weaponLevel = StarterEquipmentLevel;
    [SerializeField] private int armorLevel = StarterEquipmentLevel;
    [SerializeField] private int selectedHeroIndex;
    [SerializeField] private int[] heroLevels = new int[HeroCount];
    [SerializeField] private int[] heroShards = new int[HeroCount];
    [SerializeField] private int[] heroAscensions = new int[HeroCount];
    [SerializeField] private int summonCount;
    [SerializeField] private int dailyFightCount;
    [SerializeField] private int dailyStageClearCount;
    [SerializeField] private int dailySummonCount;
    [SerializeField] private bool[] dailyMissionClaimed = new bool[DailyMissionCount];
    [SerializeField] private int battlePassXp;
    [SerializeField] private bool[] battlePassRewardsClaimed = new bool[BattlePassRewardCount];

    [Header("Campaign")]
    [SerializeField]
    private StageDefinition[] stages =
    {
        new StageDefinition("Fallen Scout", 50, 7),
        new StageDefinition("Hollow Guard", 110, 11),
        new StageDefinition("Ashborne Rogue", 165, 16),
        new StageDefinition("Rift Hound", 240, 23),
        new StageDefinition("Veil Shaman", 340, 31),
        new StageDefinition("Dusk Knight", 480, 42),
        new StageDefinition("Cursed Warden", 675, 56),
        new StageDefinition("Abyss Herald", 930, 74),
        new StageDefinition("Eclipse Beast", 1275, 97),
        new StageDefinition("Mythfallen Tyrant", 1725, 125)
    };

    [Header("Idle")]
    [SerializeField] private bool autoAttackEnabled = true;
    [SerializeField] private float autoAttackInterval = 1f;
    [SerializeField] private int maxOfflineSeconds = 8 * 60 * 60;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text homeGoldText;
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private TMP_Text mythEssenceText;
    [SerializeField] private TMP_Text homeStageText;
    [SerializeField] private TMP_Text homePowerText;
    [SerializeField] private TMP_Text[] teamSlotTexts;
    [SerializeField] private TMP_Text selectedHeroText;
    [SerializeField] private TMP_Text[] heroCardTexts;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text enemyText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private TMP_Text dungeonResultText;
    [SerializeField] private TMP_Text goldDungeonText;
    [SerializeField] private TMP_Text essenceDungeonText;
    [SerializeField] private TMP_Text autoAttackText;
    [SerializeField] private TMP_Text offlineRewardText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text heroUpgradeCostText;
    [SerializeField] private TMP_Text heroAscendCostText;
    [SerializeField] private TMP_Text equipmentSummaryText;
    [SerializeField] private TMP_Text weaponUpgradeCostText;
    [SerializeField] private TMP_Text armorUpgradeCostText;
    [SerializeField] private TMP_Text summonCostText;
    [SerializeField] private TMP_Text summonResultText;
    [SerializeField] private TMP_Text summonRatesText;
    [SerializeField] private TMP_Text summonCountText;
    [SerializeField] private TMP_Text[] dailyMissionTexts;
    [SerializeField] private TMP_Text battlePassProgressText;
    [SerializeField] private TMP_Text[] battlePassRewardTexts;
    [SerializeField] private Button fightButton;
    [SerializeField] private Button goldDungeonButton;
    [SerializeField] private Button essenceDungeonButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button heroUpgradeButton;
    [SerializeField] private Button heroAscendButton;
    [SerializeField] private Button weaponUpgradeButton;
    [SerializeField] private Button armorUpgradeButton;
    [SerializeField] private Button summonButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button[] heroSelectButtons;
    [SerializeField] private Button[] dailyMissionButtons;
    [SerializeField] private Button[] battlePassRewardButtons;

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
        RegisterBattlePassRewardButtons();

        if (fightButton != null)
        {
            fightButton.onClick.AddListener(Fight);
        }

        if (goldDungeonButton != null)
        {
            goldDungeonButton.onClick.AddListener(RunGoldDungeon);
        }

        if (essenceDungeonButton != null)
        {
            essenceDungeonButton.onClick.AddListener(RunEssenceDungeon);
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

        if (weaponUpgradeButton != null)
        {
            weaponUpgradeButton.onClick.AddListener(UpgradeWeapon);
        }

        if (armorUpgradeButton != null)
        {
            armorUpgradeButton.onClick.AddListener(UpgradeArmor);
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

        if (goldDungeonButton != null)
        {
            goldDungeonButton.onClick.RemoveListener(RunGoldDungeon);
        }

        if (essenceDungeonButton != null)
        {
            essenceDungeonButton.onClick.RemoveListener(RunEssenceDungeon);
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

        if (weaponUpgradeButton != null)
        {
            weaponUpgradeButton.onClick.RemoveListener(UpgradeWeapon);
        }

        if (armorUpgradeButton != null)
        {
            armorUpgradeButton.onClick.RemoveListener(UpgradeArmor);
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
        UnregisterBattlePassRewardButtons();
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

    public void RunGoldDungeon()
    {
        RunDungeon(isGoldDungeon: true);
    }

    public void RunEssenceDungeon()
    {
        RunDungeon(isGoldDungeon: false);
    }

    public void UpgradeDamage()
    {
        selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);

        if (mythEssence < upgradeCost)
        {
            RefreshUi();
            return;
        }

        mythEssence -= upgradeCost;
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

    public void UpgradeWeapon()
    {
        weaponLevel = Mathf.Max(StarterEquipmentLevel, weaponLevel);
        var cost = GetWeaponUpgradeCost();

        if (gold < cost)
        {
            RefreshUi();
            return;
        }

        gold -= cost;
        weaponLevel++;
        damage = GetTeamDamage();

        SaveProgress();
        RefreshUi();
    }

    public void UpgradeArmor()
    {
        armorLevel = Mathf.Max(StarterEquipmentLevel, armorLevel);
        var cost = GetArmorUpgradeCost();

        if (gold < cost)
        {
            RefreshUi();
            return;
        }

        gold -= cost;
        armorLevel++;

        SaveProgress();
        RefreshUi();
    }

    public void SummonOnce()
    {
        if (gems < SummonCost)
        {
            SetSummonResult($"Need {SummonCost} Gems for a summon.");
            RefreshUi();
            return;
        }

        EnsureHeroShards();

        gems -= SummonCost;
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

    public void ClaimBattlePassReward1()
    {
        ClaimBattlePassReward(0);
    }

    public void ClaimBattlePassReward2()
    {
        ClaimBattlePassReward(1);
    }

    public void ClaimBattlePassReward3()
    {
        ClaimBattlePassReward(2);
    }

    public void ClaimBattlePassReward4()
    {
        ClaimBattlePassReward(3);
    }

    public void ClaimBattlePassReward5()
    {
        ClaimBattlePassReward(4);
    }

    public void ResetProgress()
    {
        gold = 0;
        gems = StarterGems;
        mythEssence = StarterMythEssence;
        damage = 1;
        enemyLevel = 1;
        goldDungeonFloor = 1;
        essenceDungeonFloor = 1;
        weaponLevel = StarterEquipmentLevel;
        armorLevel = StarterEquipmentLevel;
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

        battlePassXp = 0;
        EnsureBattlePassRewardClaims();
        for (var i = 0; i < battlePassRewardsClaimed.Length; i++)
        {
            battlePassRewardsClaimed[i] = false;
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
        dailyFightCount++;
        var stage = GetStageDefinition(enemyLevel);
        var result = SimulateCombat(stage.maxHp, GetCampaignEnemyDamage(enemyLevel));

        if (result.won)
        {
            enemyLevel++;
            dailyStageClearCount++;
            enemyMaxHp = GetStageMaxHp(enemyLevel);
            enemyHp = enemyMaxHp;
            SetDungeonResult($"Campaign Stage {enemyLevel - 1} cleared in {result.rounds} rounds\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}");
        }
        else
        {
            enemyMaxHp = stage.maxHp;
            enemyHp = enemyMaxHp;
            SetDungeonResult($"Campaign Stage {enemyLevel} failed after {result.rounds} rounds\nEnemy HP {result.enemyHpRemaining}/{stage.maxHp}  {FormatCombatResult(result)}");
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
        gems = PlayerPrefs.GetInt(GemsKey, gems);
        mythEssence = PlayerPrefs.GetInt(MythEssenceKey, mythEssence);
        if (!PlayerPrefs.HasKey(GemsKey))
        {
            gems = StarterGems;
        }

        if (!PlayerPrefs.HasKey(MythEssenceKey))
        {
            mythEssence = Mathf.Max(StarterMythEssence, gold);
        }

        goldDungeonFloor = Mathf.Max(1, PlayerPrefs.GetInt(GoldDungeonFloorKey, goldDungeonFloor));
        essenceDungeonFloor = Mathf.Max(1, PlayerPrefs.GetInt(EssenceDungeonFloorKey, essenceDungeonFloor));
        weaponLevel = Mathf.Max(StarterEquipmentLevel, PlayerPrefs.GetInt(WeaponLevelKey, weaponLevel));
        armorLevel = Mathf.Max(StarterEquipmentLevel, PlayerPrefs.GetInt(ArmorLevelKey, armorLevel));
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
        PlayerPrefs.SetInt(GemsKey, gems);
        PlayerPrefs.SetInt(MythEssenceKey, mythEssence);
        PlayerPrefs.SetInt(DamageKey, damage);
        PlayerPrefs.SetInt(GoldDungeonFloorKey, goldDungeonFloor);
        PlayerPrefs.SetInt(EssenceDungeonFloorKey, essenceDungeonFloor);
        PlayerPrefs.SetInt(WeaponLevelKey, weaponLevel);
        PlayerPrefs.SetInt(ArmorLevelKey, armorLevel);
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
        PlayerPrefs.SetInt(BattlePassXpKey, battlePassXp);

        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();
        EnsureDailyMissionClaims();
        EnsureBattlePassRewardClaims();
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

        for (var i = 0; i < battlePassRewardsClaimed.Length; i++)
        {
            PlayerPrefs.SetInt($"{BattlePassClaimedKeyPrefix}{i}", battlePassRewardsClaimed[i] ? 1 : 0);
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
        var offlineGoldReward = CalculateOfflineGoldReward(lastOfflineReward);
        gold += offlineGoldReward;
        mythEssence += lastOfflineReward;
        SaveProgress();
    }

    private int CalculateOfflineReward(int offlineSeconds)
    {
        var attacks = Mathf.FloorToInt(offlineSeconds / Mathf.Max(0.1f, autoAttackInterval));
        var enemyClearSeconds = Mathf.Max(1, Mathf.CeilToInt(enemyMaxHp / (float)Mathf.Max(1, GetTeamDamage())));
        var enemyKills = Mathf.Max(0, attacks / enemyClearSeconds);

        return enemyKills * GetStageReward(enemyLevel);
    }

    private int CalculateOfflineGoldReward(int offlineEssenceReward)
    {
        return Mathf.Max(0, Mathf.FloorToInt(offlineEssenceReward * OfflineGoldRewardRate));
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
        var hp = Mathf.CeilToInt(lastStage.maxHp * Mathf.Pow(CampaignOverflowHpGrowth, overflow));
        var reward = Mathf.CeilToInt(lastStage.essenceReward * Mathf.Pow(CampaignOverflowRewardGrowth, overflow));

        return new StageDefinition($"Rift Echo {stage}", hp, reward);
    }

    private int GetStageReward(int stage)
    {
        return Mathf.Max(1, GetStageDefinition(stage).essenceReward);
    }

    private int GetStageMaxHp(int stage)
    {
        return Mathf.Max(1, GetStageDefinition(stage).maxHp);
    }

    private int GetStageRecommendedPower(int stage)
    {
        stage = Mathf.Max(1, stage);
        return 95 + Mathf.FloorToInt(48 * Mathf.Pow(stage, 1.18f));
    }

    private int GetDungeonRecommendedPower(int floor)
    {
        floor = Mathf.Max(1, floor);
        return 125 + Mathf.FloorToInt(54 * Mathf.Pow(floor, 1.2f));
    }

    private void RunDungeon(bool isGoldDungeon)
    {
        var floor = isGoldDungeon ? goldDungeonFloor : essenceDungeonFloor;
        var enemyHp = GetDungeonEnemyHp(floor);
        var enemyDamage = GetDungeonEnemyDamage(floor);
        var result = SimulateCombat(enemyHp, enemyDamage);

        if (!result.won)
        {
            SetDungeonResult($"{(isGoldDungeon ? "Gold" : "Essence")} Dungeon Floor {floor} failed after {result.rounds} rounds\nEnemy HP {result.enemyHpRemaining}/{enemyHp}  {FormatCombatResult(result)}");
            RefreshUi();
            return;
        }

        var reward = isGoldDungeon ? GetGoldDungeonReward(floor) : GetEssenceDungeonReward(floor);
        if (isGoldDungeon)
        {
            gold += reward;
            goldDungeonFloor++;
            SetDungeonResult($"Gold Dungeon Floor {floor} cleared in {result.rounds} rounds (+{reward} Gold)\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}");
        }
        else
        {
            mythEssence += reward;
            essenceDungeonFloor++;
            SetDungeonResult($"Essence Dungeon Floor {floor} cleared in {result.rounds} rounds (+{reward} Essence)\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}");
        }

        SaveProgress();
        RefreshUi();
    }

    private int GetDungeonEnemyHp(int floor)
    {
        floor = Mathf.Max(1, floor);
        return 220 + Mathf.FloorToInt(110 * Mathf.Pow(floor, 1.22f));
    }

    private int GetDungeonEnemyDamage(int floor)
    {
        floor = Mathf.Max(1, floor);
        return 24 + Mathf.FloorToInt(10 * Mathf.Pow(floor, 1.15f));
    }

    private int GetCampaignEnemyDamage(int stage)
    {
        stage = Mathf.Max(1, stage);
        return 12 + Mathf.FloorToInt(6.5f * Mathf.Pow(stage, 1.18f));
    }

    private CombatResult SimulateCombat(int targetEnemyHp, int enemyDamage)
    {
        var result = new CombatResult();
        var maxTeamHp = GetTeamHealth();
        var teamHp = maxTeamHp;
        var enemyHpValue = Mathf.Max(1, targetEnemyHp);
        var teamDamage = GetTeamDamage();
        var supportHeal = GetSupportHealPerRound(maxTeamHp);
        enemyDamage = Mathf.Max(1, enemyDamage);

        for (var round = 1; round <= MaxCombatRounds; round++)
        {
            var enemyHpBeforeAttack = enemyHpValue;
            enemyHpValue -= teamDamage;
            result.damageDealt += Mathf.Min(enemyHpBeforeAttack, teamDamage);

            if (enemyHpValue > 0 && ShouldExecuteEnemy(enemyHpValue, targetEnemyHp))
            {
                result.executed = true;
                result.damageDealt += enemyHpValue;
                enemyHpValue = 0;
            }

            if (enemyHpValue <= 0)
            {
                result.won = true;
                result.rounds = round;
                result.teamHpRemaining = Mathf.Max(0, teamHp);
                result.enemyHpRemaining = 0;
                return result;
            }

            var mitigatedDamage = GetMitigatedEnemyDamage(enemyDamage);
            result.damageTaken += Mathf.Min(teamHp, mitigatedDamage);
            teamHp -= mitigatedDamage;
            if (teamHp <= 0)
            {
                result.won = false;
                result.rounds = round;
                result.teamHpRemaining = 0;
                result.enemyHpRemaining = Mathf.Max(0, enemyHpValue);
                return result;
            }

            if (supportHeal > 0 && teamHp < maxTeamHp)
            {
                var actualHeal = Mathf.Min(supportHeal, maxTeamHp - teamHp);
                teamHp += actualHeal;
                result.healingDone += actualHeal;
            }
        }

        result.won = false;
        result.rounds = MaxCombatRounds;
        result.teamHpRemaining = Mathf.Max(0, teamHp);
        result.enemyHpRemaining = Mathf.Max(0, enemyHpValue);
        return result;
    }

    private string FormatCombatResult(CombatResult result)
    {
        var executeText = result.executed ? "  Execute" : string.Empty;
        return $"Dealt {result.damageDealt}  Took {result.damageTaken}  Healed {result.healingDone}{executeText}";
    }

    private int GetGoldDungeonReward(int floor)
    {
        floor = Mathf.Max(1, floor);
        return 80 + Mathf.FloorToInt(30 * Mathf.Pow(floor, 1.15f));
    }

    private int GetEssenceDungeonReward(int floor)
    {
        floor = Mathf.Max(1, floor);
        return 100 + Mathf.FloorToInt(36 * Mathf.Pow(floor, 1.15f));
    }

    private void SetDungeonResult(string result)
    {
        if (dungeonResultText != null)
        {
            dungeonResultText.fontSize = 26;
            dungeonResultText.text = result;
        }
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
            goldText.text = $"Gold {gold}   Gems {gems}   Essence {mythEssence}";
        }

        if (homeGoldText != null)
        {
            homeGoldText.text = $"{gold} Gold";
        }

        if (gemsText != null)
        {
            gemsText.text = $"{gems} Gems";
        }

        if (mythEssenceText != null)
        {
            mythEssenceText.text = $"{mythEssence} Myth Essence";
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
            damageText.fontSize = 32;
            damageText.text = $"ATK {damage}   HP {GetTeamHealth()}   Guard -{Mathf.RoundToInt(GetTankDamageReductionRate() * 100f)}%   Heal {GetSupportHealPerRound(GetTeamHealth())}";
        }

        RefreshDungeonUi();

        if (enemyText != null)
        {
            var stage = GetStageDefinition(enemyLevel);
            enemyText.text = $"Stage {enemyLevel}: {stage.enemyName}\nRecommended Power {GetStageRecommendedPower(enemyLevel)}";
        }

        if (enemyHpText != null)
        {
            enemyHpText.text = $"Enemy HP: {enemyMaxHp}   Enemy Damage: {GetCampaignEnemyDamage(enemyLevel)}";
        }

        RefreshAutoAttackUi();
        RefreshOfflineRewardUi();
        RefreshHeroUi();
        RefreshEquipmentUi();
        RefreshSummonUi();
        RefreshDailyMissionUi();
        RefreshBattlePassUi();

        if (upgradeCostText != null)
        {
            upgradeCostText.text = $"Upgrade {HeroNames[selectedHeroIndex]} ({upgradeCost} Essence)";
        }

        if (heroUpgradeCostText != null)
        {
            heroUpgradeCostText.text = $"Upgrade {HeroNames[selectedHeroIndex]} ({upgradeCost} Essence)";
        }

        if (heroAscendCostText != null)
        {
            heroAscendCostText.text = $"Ascend {HeroNames[selectedHeroIndex]} ({GetHeroAscensionCost(selectedHeroIndex)} Shards)";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = mythEssence >= upgradeCost;
        }

        if (heroUpgradeButton != null)
        {
            heroUpgradeButton.interactable = mythEssence >= upgradeCost;
        }

        if (heroAscendButton != null)
        {
            heroAscendButton.interactable = heroShards[selectedHeroIndex] >= GetHeroAscensionCost(selectedHeroIndex);
        }

        if (weaponUpgradeButton != null)
        {
            weaponUpgradeButton.interactable = gold >= GetWeaponUpgradeCost();
        }

        if (armorUpgradeButton != null)
        {
            armorUpgradeButton.interactable = gold >= GetArmorUpgradeCost();
        }

        if (summonButton != null)
        {
            summonButton.interactable = gems >= SummonCost;
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

    private void RegisterBattlePassRewardButtons()
    {
        if (battlePassRewardButtons == null || battlePassRewardButtons.Length == 0)
        {
            return;
        }

        if (battlePassRewardButtons.Length > 0 && battlePassRewardButtons[0] != null) battlePassRewardButtons[0].onClick.AddListener(ClaimBattlePassReward1);
        if (battlePassRewardButtons.Length > 1 && battlePassRewardButtons[1] != null) battlePassRewardButtons[1].onClick.AddListener(ClaimBattlePassReward2);
        if (battlePassRewardButtons.Length > 2 && battlePassRewardButtons[2] != null) battlePassRewardButtons[2].onClick.AddListener(ClaimBattlePassReward3);
        if (battlePassRewardButtons.Length > 3 && battlePassRewardButtons[3] != null) battlePassRewardButtons[3].onClick.AddListener(ClaimBattlePassReward4);
        if (battlePassRewardButtons.Length > 4 && battlePassRewardButtons[4] != null) battlePassRewardButtons[4].onClick.AddListener(ClaimBattlePassReward5);
    }

    private void UnregisterBattlePassRewardButtons()
    {
        if (battlePassRewardButtons == null || battlePassRewardButtons.Length == 0)
        {
            return;
        }

        if (battlePassRewardButtons.Length > 0 && battlePassRewardButtons[0] != null) battlePassRewardButtons[0].onClick.RemoveListener(ClaimBattlePassReward1);
        if (battlePassRewardButtons.Length > 1 && battlePassRewardButtons[1] != null) battlePassRewardButtons[1].onClick.RemoveListener(ClaimBattlePassReward2);
        if (battlePassRewardButtons.Length > 2 && battlePassRewardButtons[2] != null) battlePassRewardButtons[2].onClick.RemoveListener(ClaimBattlePassReward3);
        if (battlePassRewardButtons.Length > 3 && battlePassRewardButtons[3] != null) battlePassRewardButtons[3].onClick.RemoveListener(ClaimBattlePassReward4);
        if (battlePassRewardButtons.Length > 4 && battlePassRewardButtons[4] != null) battlePassRewardButtons[4].onClick.RemoveListener(ClaimBattlePassReward5);
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
                    teamSlotTexts[i].text = $"{HeroNames[i]}\nATK {GetHeroAttack(i)}\nHP {GetHeroHealth(i)}";
                }
            }
        }

        if (selectedHeroText != null)
        {
            selectedHeroText.text = $"{HeroNames[selectedHeroIndex]}  Lv. {heroLevels[selectedHeroIndex]}  Asc. {heroAscensions[selectedHeroIndex]}\n{HeroRarities[selectedHeroIndex]} {HeroRoles[selectedHeroIndex]}  Power {GetHeroPower(selectedHeroIndex)}\nATK {GetHeroAttack(selectedHeroIndex)}  HP {GetHeroHealth(selectedHeroIndex)}  Shards {heroShards[selectedHeroIndex]}";
        }

        if (heroCardTexts != null)
        {
            for (var i = 0; i < Mathf.Min(heroCardTexts.Length, HeroCount); i++)
            {
                if (heroCardTexts[i] != null)
                {
                    var marker = i == selectedHeroIndex ? "> " : string.Empty;
                    heroCardTexts[i].text = $"{marker}{HeroNames[i]}  Lv. {heroLevels[i]}  A{heroAscensions[i]}  Shards {heroShards[i]}\n{HeroRarities[i]} {HeroRoles[i]}  ATK {GetHeroAttack(i)}  HP {GetHeroHealth(i)}";
                }
            }
        }
    }

    private void RefreshEquipmentUi()
    {
        weaponLevel = Mathf.Max(StarterEquipmentLevel, weaponLevel);
        armorLevel = Mathf.Max(StarterEquipmentLevel, armorLevel);

        if (equipmentSummaryText != null)
        {
            equipmentSummaryText.text = $"Equipment\n{WeaponTrack.name} Lv. {weaponLevel}  +{GetEquipmentAttackBonus()} {WeaponTrack.statLabel}\n{ArmorTrack.name} Lv. {armorLevel}  +{GetEquipmentHealthBonus()} {ArmorTrack.statLabel}";
        }

        if (weaponUpgradeCostText != null)
        {
            weaponUpgradeCostText.text = $"{WeaponTrack.name} +1\n{GetWeaponUpgradeCost()} Gold";
        }

        if (armorUpgradeCostText != null)
        {
            armorUpgradeCostText.text = $"{ArmorTrack.name} +1\n{GetArmorUpgradeCost()} Gold";
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

    private void EnsureBattlePassRewardClaims()
    {
        if (battlePassRewardsClaimed == null || battlePassRewardsClaimed.Length != BattlePassRewardCount)
        {
            battlePassRewardsClaimed = new bool[BattlePassRewardCount];
        }
    }

    private void LoadDailyProgress()
    {
        EnsureDailyMissionClaims();
        LoadBattlePassProgress();

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

    private void LoadBattlePassProgress()
    {
        EnsureBattlePassRewardClaims();

        battlePassXp = Mathf.Max(0, PlayerPrefs.GetInt(BattlePassXpKey, battlePassXp));
        for (var i = 0; i < battlePassRewardsClaimed.Length; i++)
        {
            battlePassRewardsClaimed[i] = PlayerPrefs.GetInt($"{BattlePassClaimedKeyPrefix}{i}", 0) == 1;
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
        gold += DailyMissionGoldRewards[missionIndex];
        gems += DailyMissionGemRewards[missionIndex];
        mythEssence += DailyMissionEssenceRewards[missionIndex];
        battlePassXp += BattlePassXpPerDailyClaim;

        SaveProgress();
        RefreshUi();
    }

    private void ClaimBattlePassReward(int rewardIndex)
    {
        rewardIndex = Mathf.Clamp(rewardIndex, 0, BattlePassRewardCount - 1);
        EnsureBattlePassRewardClaims();

        if (battlePassRewardsClaimed[rewardIndex] || battlePassXp < BattlePassRewardXp[rewardIndex])
        {
            RefreshUi();
            return;
        }

        battlePassRewardsClaimed[rewardIndex] = true;
        gold += BattlePassGoldRewards[rewardIndex];
        gems += BattlePassGemRewards[rewardIndex];
        mythEssence += BattlePassEssenceRewards[rewardIndex];

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
            new StageDefinition("Hollow Guard", 110, 11),
            new StageDefinition("Ashborne Rogue", 165, 16),
            new StageDefinition("Rift Hound", 240, 23),
            new StageDefinition("Veil Shaman", 340, 31),
            new StageDefinition("Dusk Knight", 480, 42),
            new StageDefinition("Cursed Warden", 675, 56),
            new StageDefinition("Abyss Herald", 930, 74),
            new StageDefinition("Eclipse Beast", 1275, 97),
            new StageDefinition("Mythfallen Tyrant", 1725, 125)
        };
    }

    private int GetTeamPower()
    {
        var power = 0;

        for (var i = 0; i < HeroCount; i++)
        {
            power += GetHeroPower(i);
        }

        return power + GetEquipmentPower();
    }

    private int GetTeamDamage()
    {
        var multiplier = 1f
            + (CountHeroesWithRole("Warrior") * WarriorDamageBonusRate)
            + (CountHeroesWithRole("Mage") * MageDamageBonusRate);

        return Mathf.Max(1, Mathf.FloorToInt((GetTeamBaseAttack() + GetEquipmentAttackBonus()) * multiplier));
    }

    private int GetTeamHealth()
    {
        var health = 0;

        for (var i = 0; i < HeroCount; i++)
        {
            health += GetHeroHealth(i);
        }

        return Mathf.Max(1, health + GetEquipmentHealthBonus());
    }

    private int GetTeamBaseAttack()
    {
        var attack = 0;

        for (var i = 0; i < HeroCount; i++)
        {
            attack += GetHeroAttack(i);
        }

        return attack;
    }

    private int GetHeroPower(int index)
    {
        return GetHeroAttack(index) + Mathf.FloorToInt(GetHeroHealth(index) / 8f);
    }

    private int GetEquipmentPower()
    {
        return GetEquipmentAttackBonus() + Mathf.FloorToInt(GetEquipmentHealthBonus() / 8f);
    }

    private int GetEquipmentAttackBonus()
    {
        return GetEquipmentBonus(WeaponTrack, weaponLevel);
    }

    private int GetEquipmentHealthBonus()
    {
        return GetEquipmentBonus(ArmorTrack, armorLevel);
    }

    private int GetEquipmentBonus(EquipmentTrackDefinition track, int level)
    {
        level = Mathf.Max(StarterEquipmentLevel, level);
        return track.baseBonus + ((level - 1) * track.bonusPerLevel);
    }

    private int GetHeroAttack(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();

        return HeroBaseAttack[index]
            + (heroLevels[index] * HeroAttackGrowth[index])
            + Mathf.FloorToInt(heroShards[index] * 0.25f)
            + (heroAscensions[index] * GetHeroAscensionAttack(index));
    }

    private int GetHeroHealth(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();

        return HeroBaseHealth[index]
            + (heroLevels[index] * HeroHealthGrowth[index])
            + Mathf.FloorToInt(heroShards[index] * 1.2f)
            + (heroAscensions[index] * GetHeroAscensionHealth(index));
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

    private int GetWeaponUpgradeCost()
    {
        return GetEquipmentUpgradeCost(WeaponTrack, weaponLevel);
    }

    private int GetArmorUpgradeCost()
    {
        return GetEquipmentUpgradeCost(ArmorTrack, armorLevel);
    }

    private int GetEquipmentUpgradeCost(EquipmentTrackDefinition track, int level)
    {
        level = Mathf.Max(StarterEquipmentLevel, level);
        return Mathf.CeilToInt(track.baseCost * Mathf.Pow(track.costGrowth, level - 1));
    }

    private int GetHeroAscensionAttack(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);

        if (HeroRarities[index] == "Legendary")
        {
            return 14;
        }

        if (HeroRarities[index] == "Epic")
        {
            return 11;
        }

        return 8;
    }

    private int GetHeroAscensionHealth(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);

        if (HeroRarities[index] == "Legendary")
        {
            return 90;
        }

        if (HeroRarities[index] == "Epic")
        {
            return 70;
        }

        return 55;
    }

    private int CountHeroesWithRole(string role)
    {
        var count = 0;

        for (var i = 0; i < HeroCount; i++)
        {
            if (HeroRoles[i] == role)
            {
                count++;
            }
        }

        return count;
    }

    private bool ShouldExecuteEnemy(int enemyHpRemaining, int enemyMaxHp)
    {
        if (CountHeroesWithRole("Ranger") <= 0)
        {
            return false;
        }

        return enemyHpRemaining <= Mathf.FloorToInt(Mathf.Max(1, enemyMaxHp) * RangerExecuteThresholdRate);
    }

    private int GetMitigatedEnemyDamage(int enemyDamage)
    {
        return Mathf.Max(1, Mathf.CeilToInt(enemyDamage * (1f - GetTankDamageReductionRate())));
    }

    private float GetTankDamageReductionRate()
    {
        return Mathf.Min(0.5f, CountHeroesWithRole("Tank") * TankDamageReductionRate);
    }

    private int GetSupportHealPerRound(int maxTeamHealth)
    {
        var supports = CountHeroesWithRole("Support");
        if (supports <= 0)
        {
            return 0;
        }

        return Mathf.Max(1, Mathf.FloorToInt(maxTeamHealth * SupportHealRate * supports));
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

    private void RefreshDungeonUi()
    {
        if (goldDungeonText != null)
        {
            goldDungeonText.text = $"Gold Dungeon\nFloor {goldDungeonFloor}  Rec. Power {GetDungeonRecommendedPower(goldDungeonFloor)}\nReward {GetGoldDungeonReward(goldDungeonFloor)} Gold";
        }

        if (essenceDungeonText != null)
        {
            essenceDungeonText.text = $"Essence Dungeon\nFloor {essenceDungeonFloor}  Rec. Power {GetDungeonRecommendedPower(essenceDungeonFloor)}\nReward {GetEssenceDungeonReward(essenceDungeonFloor)} Essence";
        }

        if (dungeonResultText != null && string.IsNullOrWhiteSpace(dungeonResultText.text))
        {
            dungeonResultText.text = "Dungeons are the active resource source.";
        }
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

        offlineRewardText.text = $"Offline: +{CalculateOfflineGoldReward(lastOfflineReward)} Gold, +{lastOfflineReward} Essence ({FormatDuration(lastOfflineSeconds)})";
    }

    private void RefreshSummonUi()
    {
        if (summonCostText != null)
        {
            summonCostText.text = $"Cost: {SummonCost} Gems";
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
            var text = $"{DailyMissionTitles[i]}\n{state}  Reward {FormatReward(DailyMissionGoldRewards[i], DailyMissionGemRewards[i], DailyMissionEssenceRewards[i])}";

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

    private void RefreshBattlePassUi()
    {
        EnsureBattlePassRewardClaims();

        if (battlePassProgressText != null)
        {
            battlePassProgressText.text = $"Mission Track XP: {battlePassXp}\nDaily claims give +{BattlePassXpPerDailyClaim} XP";
        }

        for (var i = 0; i < BattlePassRewardCount; i++)
        {
            var isReady = battlePassXp >= BattlePassRewardXp[i];
            var isClaimed = battlePassRewardsClaimed[i];
            var state = isClaimed ? "Claimed" : isReady ? "Claim" : $"{battlePassXp}/{BattlePassRewardXp[i]} XP";
            var text = $"Level {i + 1}  {state}\nReward {FormatReward(BattlePassGoldRewards[i], BattlePassGemRewards[i], BattlePassEssenceRewards[i])}";

            if (battlePassRewardTexts != null && i < battlePassRewardTexts.Length && battlePassRewardTexts[i] != null)
            {
                battlePassRewardTexts[i].text = text;
            }

            if (battlePassRewardButtons != null && i < battlePassRewardButtons.Length && battlePassRewardButtons[i] != null)
            {
                battlePassRewardButtons[i].interactable = isReady && !isClaimed;
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

    private string FormatReward(int goldReward, int gemReward, int essenceReward)
    {
        var reward = string.Empty;

        if (goldReward > 0)
        {
            reward = $"{goldReward} Gold";
        }

        if (gemReward > 0)
        {
            reward = string.IsNullOrEmpty(reward) ? $"{gemReward} Gems" : $"{reward}, {gemReward} Gems";
        }

        if (essenceReward > 0)
        {
            reward = string.IsNullOrEmpty(reward) ? $"{essenceReward} Essence" : $"{reward}, {essenceReward} Essence";
        }

        return string.IsNullOrEmpty(reward) ? "None" : reward;
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
