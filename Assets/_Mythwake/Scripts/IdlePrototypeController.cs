using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdlePrototypeController : MonoBehaviour
{
    public const string PrototypeVersion = "0.2.1";
    public const int CurrentSaveVersion = 1;

    [Serializable]
    private struct StageDefinition
    {
        public string stageId;
        public int stageNumber;
        public string enemyName;
        public int maxHp;
        public int essenceReward;

        public StageDefinition(int stageNumber, string enemyName, int maxHp, int essenceReward)
        {
            this.stageNumber = stageNumber;
            this.stageId = $"campaign_stage_{stageNumber:000}";
            this.enemyName = enemyName;
            this.maxHp = maxHp;
            this.essenceReward = essenceReward;
        }
    }

    private struct CurrencyDefinition
    {
        public string currencyId;
        public string displayName;
        public string saveKey;
        public int starterAmount;

        public CurrencyDefinition(string currencyId, string displayName, string saveKey, int starterAmount)
        {
            this.currencyId = currencyId;
            this.displayName = displayName;
            this.saveKey = saveKey;
            this.starterAmount = starterAmount;
        }
    }

    private struct RewardDefinition
    {
        public string rewardId;
        public int gold;
        public int gems;
        public int mythEssence;
        public int passXp;

        public RewardDefinition(string rewardId, int gold, int gems, int mythEssence, int passXp = 0)
        {
            this.rewardId = rewardId;
            this.gold = gold;
            this.gems = gems;
            this.mythEssence = mythEssence;
            this.passXp = passXp;
        }
    }

    private enum DailyMissionProgressType
    {
        Fight,
        StageClear,
        Summon
    }

    private struct HeroDefinition
    {
        public string heroId;
        public string name;
        public string roleId;
        public string roleName;
        public string rarityId;
        public string rarityName;
        public int baseAttack;
        public int attackGrowth;
        public int baseHealth;
        public int healthGrowth;
        public int summonShardReward;
        public int ascensionBaseCost;
        public int ascensionCostGrowth;
        public int ascensionAttack;
        public int ascensionHealth;

        public HeroDefinition(
            string heroId,
            string name,
            string roleId,
            string roleName,
            string rarityId,
            string rarityName,
            int baseAttack,
            int attackGrowth,
            int baseHealth,
            int healthGrowth,
            int summonShardReward,
            int ascensionBaseCost,
            int ascensionCostGrowth,
            int ascensionAttack,
            int ascensionHealth)
        {
            this.heroId = heroId;
            this.name = name;
            this.roleId = roleId;
            this.roleName = roleName;
            this.rarityId = rarityId;
            this.rarityName = rarityName;
            this.baseAttack = baseAttack;
            this.attackGrowth = attackGrowth;
            this.baseHealth = baseHealth;
            this.healthGrowth = healthGrowth;
            this.summonShardReward = summonShardReward;
            this.ascensionBaseCost = ascensionBaseCost;
            this.ascensionCostGrowth = ascensionCostGrowth;
            this.ascensionAttack = ascensionAttack;
            this.ascensionHealth = ascensionHealth;
        }
    }

    private struct DailyMissionDefinition
    {
        public string missionId;
        public string title;
        public DailyMissionProgressType progressType;
        public int target;
        public RewardDefinition reward;

        public DailyMissionDefinition(string missionId, string title, DailyMissionProgressType progressType, int target, RewardDefinition reward)
        {
            this.missionId = missionId;
            this.title = title;
            this.progressType = progressType;
            this.target = target;
            this.reward = reward;
        }
    }

    private struct BattlePassRewardDefinition
    {
        public string rewardId;
        public int requiredXp;
        public RewardDefinition reward;

        public BattlePassRewardDefinition(string rewardId, int requiredXp, RewardDefinition reward)
        {
            this.rewardId = rewardId;
            this.requiredXp = requiredXp;
            this.reward = reward;
        }
    }

    private struct DungeonDefinition
    {
        public string dungeonId;
        public string displayName;
        public string rewardCurrencyId;
        public int baseEnemyHp;
        public float enemyHpScale;
        public float enemyHpGrowth;
        public int baseEnemyDamage;
        public float enemyDamageScale;
        public float enemyDamageGrowth;
        public int baseRecommendedPower;
        public float recommendedPowerScale;
        public float recommendedPowerGrowth;
        public int baseReward;
        public float rewardScale;
        public float rewardGrowth;

        public DungeonDefinition(
            string dungeonId,
            string displayName,
            string rewardCurrencyId,
            int baseEnemyHp,
            float enemyHpScale,
            float enemyHpGrowth,
            int baseEnemyDamage,
            float enemyDamageScale,
            float enemyDamageGrowth,
            int baseRecommendedPower,
            float recommendedPowerScale,
            float recommendedPowerGrowth,
            int baseReward,
            float rewardScale,
            float rewardGrowth)
        {
            this.dungeonId = dungeonId;
            this.displayName = displayName;
            this.rewardCurrencyId = rewardCurrencyId;
            this.baseEnemyHp = baseEnemyHp;
            this.enemyHpScale = enemyHpScale;
            this.enemyHpGrowth = enemyHpGrowth;
            this.baseEnemyDamage = baseEnemyDamage;
            this.enemyDamageScale = enemyDamageScale;
            this.enemyDamageGrowth = enemyDamageGrowth;
            this.baseRecommendedPower = baseRecommendedPower;
            this.recommendedPowerScale = recommendedPowerScale;
            this.recommendedPowerGrowth = recommendedPowerGrowth;
            this.baseReward = baseReward;
            this.rewardScale = rewardScale;
            this.rewardGrowth = rewardGrowth;
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
        public string equipmentId;
        public string name;
        public string statLabel;
        public string currencyId;
        public int baseBonus;
        public int bonusPerLevel;
        public int baseCost;
        public float costGrowth;

        public EquipmentTrackDefinition(string equipmentId, string name, string statLabel, string currencyId, int baseBonus, int bonusPerLevel, int baseCost, float costGrowth)
        {
            this.equipmentId = equipmentId;
            this.name = name;
            this.statLabel = statLabel;
            this.currencyId = currencyId;
            this.baseBonus = baseBonus;
            this.bonusPerLevel = bonusPerLevel;
            this.baseCost = baseCost;
            this.costGrowth = costGrowth;
        }
    }

    private struct AccessorySlotDefinition
    {
        public string itemSlotId;
        public string name;
        public int attackPerLevel;
        public int healthPerLevel;

        public AccessorySlotDefinition(string itemSlotId, string name, int attackPerLevel, int healthPerLevel)
        {
            this.itemSlotId = itemSlotId;
            this.name = name;
            this.attackPerLevel = attackPerLevel;
            this.healthPerLevel = healthPerLevel;
        }
    }

    private struct AccessoryRarityDefinition
    {
        public string rarityId;
        public string displayName;
        public int tier;
        public int maxLevel;
        public int statMultiplier;
        public int levelCostBase;
        public float levelCostGrowth;
        public string fuseTargetRarityId;

        public AccessoryRarityDefinition(string rarityId, string displayName, int tier, int maxLevel, int statMultiplier, int levelCostBase, float levelCostGrowth, string fuseTargetRarityId)
        {
            this.rarityId = rarityId;
            this.displayName = displayName;
            this.tier = tier;
            this.maxLevel = maxLevel;
            this.statMultiplier = statMultiplier;
            this.levelCostBase = levelCostBase;
            this.levelCostGrowth = levelCostGrowth;
            this.fuseTargetRarityId = fuseTargetRarityId;
        }
    }

    private struct SummonRateDefinition
    {
        public string rarityId;
        public int cumulativeChance;
        public int[] heroIndexes;

        public SummonRateDefinition(string rarityId, int cumulativeChance, int[] heroIndexes)
        {
            this.rarityId = rarityId;
            this.cumulativeChance = cumulativeChance;
            this.heroIndexes = heroIndexes;
        }
    }

    private struct SummonBannerDefinition
    {
        public string bannerId;
        public string displayName;
        public string costCurrencyId;
        public int costAmount;
        public SummonRateDefinition[] rates;

        public SummonBannerDefinition(string bannerId, string displayName, string costCurrencyId, int costAmount, SummonRateDefinition[] rates)
        {
            this.bannerId = bannerId;
            this.displayName = displayName;
            this.costCurrencyId = costCurrencyId;
            this.costAmount = costAmount;
            this.rates = rates;
        }
    }

    private enum AppScreen
    {
        Home,
        Battle,
        Heroes,
        Gear,
        Summon,
        Shop
    }

    private const string SaveVersionKey = "Mythwake.Prototype.SaveVersion";
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
    private const string GearDungeonFloorKey = "Mythwake.Prototype.Dungeon.GearFloor";
    private const string WeaponLevelKey = "Mythwake.Prototype.Equipment.WeaponLevel";
    private const string ArmorLevelKey = "Mythwake.Prototype.Equipment.ArmorLevel";
    private const string SelectedAccessorySlotKey = "Mythwake.Prototype.Accessory.SelectedSlot";
    private const string SelectedAccessoryRarityKey = "Mythwake.Prototype.Accessory.SelectedRarity";
    private const string EquippedAccessoryRarityKeyPrefix = "Mythwake.Prototype.Accessory.EquippedRarity.";
    private const string EquippedAccessoryLevelKeyPrefix = "Mythwake.Prototype.Accessory.EquippedLevel.";
    private const string AccessoryInventoryKeyPrefix = "Mythwake.Prototype.Accessory.Inventory.";
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
    private const string GoldCurrencyId = "gold";
    private const string GemsCurrencyId = "gems";
    private const string MythEssenceCurrencyId = "myth_essence";
    private const string PassXpCurrencyId = "pass_xp";
    private const string WarriorRoleId = "warrior";
    private const string TankRoleId = "tank";
    private const string MageRoleId = "mage";
    private const string RangerRoleId = "ranger";
    private const string SupportRoleId = "support";
    private const string RareRarityId = "rare";
    private const string EpicRarityId = "epic";
    private const string LegendaryRarityId = "legendary";
    private const int HeroCount = 5;
    private const int DailyMissionCount = 3;
    private const int BattlePassRewardCount = 5;
    private const int AccessorySlotCount = 5;
    private const int AccessoryRarityCount = 5;
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
    private const int CampaignMilestoneInterval = 5;
    private const int DungeonBonusInterval = 5;
    private const int AccessoryFuseCost = 3;
    private const int DebugGoldAmount = 500;
    private const int DebugGemAmount = 30;
    private const int DebugEssenceAmount = 250;

    private static readonly string[] ScalarSaveKeys =
    {
        SaveVersionKey,
        GoldKey,
        GemsKey,
        MythEssenceKey,
        DamageKey,
        EnemyLevelKey,
        EnemyHpKey,
        EnemyMaxHpKey,
        UpgradeCostKey,
        GoldDungeonFloorKey,
        EssenceDungeonFloorKey,
        GearDungeonFloorKey,
        WeaponLevelKey,
        ArmorLevelKey,
        SelectedAccessorySlotKey,
        SelectedAccessoryRarityKey,
        LastSeenUtcKey,
        SelectedHeroKey,
        SummonCountKey,
        DailyDateKey,
        DailyFightCountKey,
        DailyStageClearCountKey,
        DailySummonCountKey,
        BattlePassXpKey
    };

    private static readonly CurrencyDefinition[] CurrencyDefinitions =
    {
        new CurrencyDefinition(GoldCurrencyId, "Gold", GoldKey, 0),
        new CurrencyDefinition(GemsCurrencyId, "Gems", GemsKey, StarterGems),
        new CurrencyDefinition(MythEssenceCurrencyId, "Myth Essence", MythEssenceKey, StarterMythEssence),
        new CurrencyDefinition(PassXpCurrencyId, "Pass XP", BattlePassXpKey, 0)
    };

    private static readonly HeroDefinition[] HeroDefinitions =
    {
        new HeroDefinition("hero_astra", "Astra", WarriorRoleId, "Warrior", EpicRarityId, "Epic", 18, 5, 150, 28, 7, 25, 15, 11, 70),
        new HeroDefinition("hero_borin", "Borin", TankRoleId, "Tank", RareRarityId, "Rare", 10, 3, 230, 42, 10, 20, 15, 8, 55),
        new HeroDefinition("hero_cyra", "Cyra", MageRoleId, "Mage", EpicRarityId, "Epic", 22, 7, 110, 20, 7, 25, 15, 11, 70),
        new HeroDefinition("hero_dante", "Dante", RangerRoleId, "Ranger", RareRarityId, "Rare", 20, 6, 125, 23, 10, 20, 15, 8, 55),
        new HeroDefinition("hero_elowen", "Elowen", SupportRoleId, "Support", LegendaryRarityId, "Legendary", 12, 4, 165, 34, 5, 30, 15, 14, 90)
    };

    private static readonly DailyMissionDefinition[] DailyMissionDefinitions =
    {
        new DailyMissionDefinition("daily_battles_20", "Battle 20 times", DailyMissionProgressType.Fight, 20, new RewardDefinition("reward_daily_battles_20", 25, 5, 80, BattlePassXpPerDailyClaim)),
        new DailyMissionDefinition("daily_stage_clears_3", "Clear 3 stages", DailyMissionProgressType.StageClear, 3, new RewardDefinition("reward_daily_stage_clears_3", 50, 10, 120, BattlePassXpPerDailyClaim)),
        new DailyMissionDefinition("daily_summon_1", "Summon 1 hero", DailyMissionProgressType.Summon, 1, new RewardDefinition("reward_daily_summon_1", 25, 15, 60, BattlePassXpPerDailyClaim))
    };

    private static readonly BattlePassRewardDefinition[] BattlePassRewardDefinitions =
    {
        new BattlePassRewardDefinition("mission_track_reward_01", 40, new RewardDefinition("reward_mission_track_01", 100, 10, 0)),
        new BattlePassRewardDefinition("mission_track_reward_02", 80, new RewardDefinition("reward_mission_track_02", 125, 15, 120)),
        new BattlePassRewardDefinition("mission_track_reward_03", 120, new RewardDefinition("reward_mission_track_03", 175, 20, 0)),
        new BattlePassRewardDefinition("mission_track_reward_04", 180, new RewardDefinition("reward_mission_track_04", 225, 25, 180)),
        new BattlePassRewardDefinition("mission_track_reward_05", 240, new RewardDefinition("reward_mission_track_05", 350, 40, 300))
    };

    private static readonly EquipmentTrackDefinition WeaponTrack = new EquipmentTrackDefinition("equipment_weapon", "Weapon", "ATK", GoldCurrencyId, 8, 9, 80, 1.45f);
    private static readonly EquipmentTrackDefinition ArmorTrack = new EquipmentTrackDefinition("equipment_armor", "Armor", "HP", GoldCurrencyId, 80, 65, 75, 1.42f);

    private static readonly AccessoryRarityDefinition[] AccessoryRarities =
    {
        new AccessoryRarityDefinition("accessory_r0", "R0", 0, 20, 1, 35, 1.18f, "accessory_r1"),
        new AccessoryRarityDefinition("accessory_r1", "R1", 1, 30, 2, 70, 1.195f, "accessory_r2"),
        new AccessoryRarityDefinition("accessory_r2", "R2", 2, 40, 3, 105, 1.21f, "accessory_r3"),
        new AccessoryRarityDefinition("accessory_r3", "R3", 3, 50, 4, 140, 1.225f, "accessory_r4"),
        new AccessoryRarityDefinition("accessory_r4", "R4", 4, 60, 5, 175, 1.24f, string.Empty)
    };

    private static readonly AccessorySlotDefinition[] AccessorySlots =
    {
        new AccessorySlotDefinition("item_slot_earrings", "Ohrringe", 3, 4),
        new AccessorySlotDefinition("item_slot_necklace", "Kette", 1, 18),
        new AccessorySlotDefinition("item_slot_bracelet", "Armband", 2, 10),
        new AccessorySlotDefinition("item_slot_gloves", "Handschuhe", 4, 2),
        new AccessorySlotDefinition("item_slot_shoes", "Schuhe", 1, 15)
    };

    private static readonly DungeonDefinition GoldDungeonDefinition = new DungeonDefinition("gold_dungeon", "Gold Dungeon", GoldCurrencyId, 220, 110f, 1.22f, 24, 10f, 1.15f, 125, 54f, 1.2f, 80, 30f, 1.15f);
    private static readonly DungeonDefinition EssenceDungeonDefinition = new DungeonDefinition("essence_dungeon", "Essence Dungeon", MythEssenceCurrencyId, 220, 110f, 1.22f, 24, 10f, 1.15f, 125, 54f, 1.2f, 100, 36f, 1.15f);
    private static readonly DungeonDefinition GearDungeonDefinition = new DungeonDefinition("gear_dungeon", "Gear Dungeon", string.Empty, 260, 135f, 1.23f, 28, 12f, 1.16f, 145, 62f, 1.2f, 0, 0f, 1f);

    private static readonly SummonBannerDefinition HeroShardBanner = new SummonBannerDefinition(
        "hero_shard_standard",
        "Awaken Heroes",
        GemsCurrencyId,
        SummonCost,
        new[]
        {
            new SummonRateDefinition(LegendaryRarityId, 10, new[] { 4 }),
            new SummonRateDefinition(EpicRarityId, 45, new[] { 0, 2 }),
            new SummonRateDefinition(RareRarityId, 100, new[] { 1, 3 })
        });

    private static readonly StageDefinition[] StarterStageDefinitions =
    {
        new StageDefinition(1, "Fallen Scout", 50, 7),
        new StageDefinition(2, "Hollow Guard", 110, 11),
        new StageDefinition(3, "Ashborne Rogue", 165, 16),
        new StageDefinition(4, "Rift Hound", 240, 23),
        new StageDefinition(5, "Veil Shaman", 340, 31),
        new StageDefinition(6, "Dusk Knight", 480, 42),
        new StageDefinition(7, "Cursed Warden", 675, 56),
        new StageDefinition(8, "Abyss Herald", 930, 74),
        new StageDefinition(9, "Eclipse Beast", 1275, 97),
        new StageDefinition(10, "Mythfallen Tyrant", 1725, 125)
    };

    [Header("Stats")]
    [SerializeField] private int saveVersion = CurrentSaveVersion;
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
    [SerializeField] private int gearDungeonFloor = 1;
    [SerializeField] private int weaponLevel = StarterEquipmentLevel;
    [SerializeField] private int armorLevel = StarterEquipmentLevel;
    [SerializeField] private int selectedAccessorySlot;
    [SerializeField] private int selectedAccessoryRarity;
    [SerializeField] private int[] equippedAccessoryRarities = new int[AccessorySlotCount];
    [SerializeField] private int[] equippedAccessoryLevels = new int[AccessorySlotCount];
    [SerializeField] private int[] accessoryInventory = new int[AccessorySlotCount * AccessoryRarityCount];
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
        new StageDefinition(1, "Fallen Scout", 50, 7),
        new StageDefinition(2, "Hollow Guard", 110, 11),
        new StageDefinition(3, "Ashborne Rogue", 165, 16),
        new StageDefinition(4, "Rift Hound", 240, 23),
        new StageDefinition(5, "Veil Shaman", 340, 31),
        new StageDefinition(6, "Dusk Knight", 480, 42),
        new StageDefinition(7, "Cursed Warden", 675, 56),
        new StageDefinition(8, "Abyss Herald", 930, 74),
        new StageDefinition(9, "Eclipse Beast", 1275, 97),
        new StageDefinition(10, "Mythfallen Tyrant", 1725, 125)
    };

    [Header("Idle")]
    [SerializeField] private bool autoAttackEnabled = true;
    [SerializeField] private float autoAttackInterval = 1f;
    [SerializeField] private int maxOfflineSeconds = 8 * 60 * 60;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text homeGoldText;
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private TMP_Text mythEssenceText;
    [SerializeField] private TMP_Text homeStageText;
    [SerializeField] private TMP_Text homePowerText;
    [SerializeField] private TMP_Text nextGoalText;
    [SerializeField] private TMP_Text[] teamSlotTexts;
    [SerializeField] private TMP_Text selectedHeroText;
    [SerializeField] private TMP_Text[] heroCardTexts;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text enemyText;
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private TMP_Text dungeonResultText;
    [SerializeField] private TMP_Text goldDungeonText;
    [SerializeField] private TMP_Text essenceDungeonText;
    [SerializeField] private TMP_Text gearDungeonText;
    [SerializeField] private TMP_Text autoAttackText;
    [SerializeField] private TMP_Text offlineRewardText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text heroUpgradeCostText;
    [SerializeField] private TMP_Text heroAscendCostText;
    [SerializeField] private TMP_Text equipmentSummaryText;
    [SerializeField] private TMP_Text weaponUpgradeCostText;
    [SerializeField] private TMP_Text armorUpgradeCostText;
    [SerializeField] private TMP_Text accessorySummaryText;
    [SerializeField] private TMP_Text accessorySelectedText;
    [SerializeField] private TMP_Text accessoryInventoryText;
    [SerializeField] private TMP_Text accessoryEquipText;
    [SerializeField] private TMP_Text accessoryLevelText;
    [SerializeField] private TMP_Text accessoryFuseText;
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
    [SerializeField] private Button gearDungeonButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button heroUpgradeButton;
    [SerializeField] private Button heroAscendButton;
    [SerializeField] private Button weaponUpgradeButton;
    [SerializeField] private Button armorUpgradeButton;
    [SerializeField] private Button accessoryPreviousSlotButton;
    [SerializeField] private Button accessoryNextSlotButton;
    [SerializeField] private Button accessoryPreviousRarityButton;
    [SerializeField] private Button accessoryNextRarityButton;
    [SerializeField] private Button accessoryEquipButton;
    [SerializeField] private Button accessoryLevelButton;
    [SerializeField] private Button accessoryFuseButton;
    [SerializeField] private Button summonButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button debugGoldButton;
    [SerializeField] private Button debugEssenceButton;
    [SerializeField] private Button debugGemsButton;
    [SerializeField] private Button debugAccessoryButton;
    [SerializeField] private Button[] heroSelectButtons;
    [SerializeField] private Button[] dailyMissionButtons;
    [SerializeField] private Button[] battlePassRewardButtons;

    [Header("Navigation")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject heroesPanel;
    [SerializeField] private GameObject gearPanel;
    [SerializeField] private GameObject summonPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button homeTabButton;
    [SerializeField] private Button battleTabButton;
    [SerializeField] private Button heroesTabButton;
    [SerializeField] private Button gearTabButton;
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
        EnsureRuntimeDebugUi();
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

        if (gearDungeonButton != null)
        {
            gearDungeonButton.onClick.AddListener(RunGearDungeon);
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

        if (accessoryPreviousSlotButton != null)
        {
            accessoryPreviousSlotButton.onClick.AddListener(PreviousAccessorySlot);
        }

        if (accessoryNextSlotButton != null)
        {
            accessoryNextSlotButton.onClick.AddListener(NextAccessorySlot);
        }

        if (accessoryPreviousRarityButton != null)
        {
            accessoryPreviousRarityButton.onClick.AddListener(PreviousAccessoryRarity);
        }

        if (accessoryNextRarityButton != null)
        {
            accessoryNextRarityButton.onClick.AddListener(NextAccessoryRarity);
        }

        if (accessoryEquipButton != null)
        {
            accessoryEquipButton.onClick.AddListener(EquipSelectedAccessory);
        }

        if (accessoryLevelButton != null)
        {
            accessoryLevelButton.onClick.AddListener(LevelSelectedAccessory);
        }

        if (accessoryFuseButton != null)
        {
            accessoryFuseButton.onClick.AddListener(FuseSelectedAccessory);
        }

        if (summonButton != null)
        {
            summonButton.onClick.AddListener(SummonOnce);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetProgress);
        }

        if (debugGoldButton != null)
        {
            debugGoldButton.onClick.AddListener(AddDebugGold);
        }

        if (debugEssenceButton != null)
        {
            debugEssenceButton.onClick.AddListener(AddDebugEssence);
        }

        if (debugGemsButton != null)
        {
            debugGemsButton.onClick.AddListener(AddDebugGems);
        }

        if (debugAccessoryButton != null)
        {
            debugAccessoryButton.onClick.AddListener(AddDebugAccessoryCopy);
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

        if (gearDungeonButton != null)
        {
            gearDungeonButton.onClick.RemoveListener(RunGearDungeon);
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

        if (accessoryPreviousSlotButton != null)
        {
            accessoryPreviousSlotButton.onClick.RemoveListener(PreviousAccessorySlot);
        }

        if (accessoryNextSlotButton != null)
        {
            accessoryNextSlotButton.onClick.RemoveListener(NextAccessorySlot);
        }

        if (accessoryPreviousRarityButton != null)
        {
            accessoryPreviousRarityButton.onClick.RemoveListener(PreviousAccessoryRarity);
        }

        if (accessoryNextRarityButton != null)
        {
            accessoryNextRarityButton.onClick.RemoveListener(NextAccessoryRarity);
        }

        if (accessoryEquipButton != null)
        {
            accessoryEquipButton.onClick.RemoveListener(EquipSelectedAccessory);
        }

        if (accessoryLevelButton != null)
        {
            accessoryLevelButton.onClick.RemoveListener(LevelSelectedAccessory);
        }

        if (accessoryFuseButton != null)
        {
            accessoryFuseButton.onClick.RemoveListener(FuseSelectedAccessory);
        }

        if (summonButton != null)
        {
            summonButton.onClick.RemoveListener(SummonOnce);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetProgress);
        }

        if (debugGoldButton != null)
        {
            debugGoldButton.onClick.RemoveListener(AddDebugGold);
        }

        if (debugEssenceButton != null)
        {
            debugEssenceButton.onClick.RemoveListener(AddDebugEssence);
        }

        if (debugGemsButton != null)
        {
            debugGemsButton.onClick.RemoveListener(AddDebugGems);
        }

        if (debugAccessoryButton != null)
        {
            debugAccessoryButton.onClick.RemoveListener(AddDebugAccessoryCopy);
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

    public void ShowGear()
    {
        ShowScreen(AppScreen.Gear);
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

    public void RunGearDungeon()
    {
        var floor = Mathf.Max(1, gearDungeonFloor);
        var enemyHp = GetGearDungeonEnemyHp(floor);
        var enemyDamage = GetGearDungeonEnemyDamage(floor);
        var result = SimulateCombat(enemyHp, enemyDamage);

        if (!result.won)
        {
            SetDungeonResult($"Gear Dungeon Floor {floor} failed after {result.rounds} rounds\nEnemy HP {result.enemyHpRemaining}/{enemyHp}  {FormatCombatResult(result)}");
            RefreshUi();
            return;
        }

        var slot = UnityEngine.Random.Range(0, AccessorySlotCount);
        var rarity = RollAccessoryRarity(floor);
        AddAccessoryInventory(slot, rarity, 1);
        gearDungeonFloor++;

        SetDungeonResult($"Gear Dungeon Floor {floor} cleared in {result.rounds} rounds\nDrop: {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name}  HP {result.teamHpRemaining}/{GetTeamHealth()}");
        SaveProgress();
        RefreshUi();
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

    public void PreviousAccessorySlot()
    {
        selectedAccessorySlot = (selectedAccessorySlot + AccessorySlotCount - 1) % AccessorySlotCount;
        SaveProgress();
        RefreshUi();
    }

    public void NextAccessorySlot()
    {
        selectedAccessorySlot = (selectedAccessorySlot + 1) % AccessorySlotCount;
        SaveProgress();
        RefreshUi();
    }

    public void PreviousAccessoryRarity()
    {
        selectedAccessoryRarity = (selectedAccessoryRarity + AccessoryRarityCount - 1) % AccessoryRarityCount;
        SaveProgress();
        RefreshUi();
    }

    public void NextAccessoryRarity()
    {
        selectedAccessoryRarity = (selectedAccessoryRarity + 1) % AccessoryRarityCount;
        SaveProgress();
        RefreshUi();
    }

    public void EquipSelectedAccessory()
    {
        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);

        if (GetAccessoryInventoryCount(slot, rarity) <= 0)
        {
            RefreshUi();
            return;
        }

        if (equippedAccessoryRarities[slot] == rarity)
        {
            RefreshUi();
            return;
        }

        if (equippedAccessoryRarities[slot] >= 0)
        {
            AddAccessoryInventory(slot, equippedAccessoryRarities[slot], 1);
        }

        AddAccessoryInventory(slot, rarity, -1);
        equippedAccessoryRarities[slot] = rarity;
        equippedAccessoryLevels[slot] = 1;

        SaveProgress();
        RefreshUi();
    }

    public void LevelSelectedAccessory()
    {
        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = equippedAccessoryRarities[slot];

        if (rarity < 0)
        {
            RefreshUi();
            return;
        }

        var maxLevel = GetAccessoryMaxLevel(rarity);
        if (equippedAccessoryLevels[slot] >= maxLevel)
        {
            RefreshUi();
            return;
        }

        var cost = GetAccessoryLevelCost(slot);
        if (gold < cost)
        {
            RefreshUi();
            return;
        }

        gold -= cost;
        equippedAccessoryLevels[slot]++;
        damage = GetTeamDamage();

        SaveProgress();
        RefreshUi();
    }

    public void FuseSelectedAccessory()
    {
        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);

        if (rarity >= AccessoryRarityCount - 1 || GetAccessoryInventoryCount(slot, rarity) < AccessoryFuseCost)
        {
            RefreshUi();
            return;
        }

        AddAccessoryInventory(slot, rarity, -AccessoryFuseCost);
        AddAccessoryInventory(slot, rarity + 1, 1);
        selectedAccessoryRarity = rarity + 1;

        SaveProgress();
        RefreshUi();
    }

    public void SummonOnce()
    {
        var summonCost = GetSummonCost();
        if (gems < summonCost)
        {
            SetSummonResult($"Need {summonCost} Gems for a summon.");
            RefreshUi();
            return;
        }

        EnsureHeroShards();

        gems -= summonCost;
        summonCount++;
        dailySummonCount++;

        var heroIndex = RollSummonHero();
        var shards = GetSummonShardReward(heroIndex);
        heroShards[heroIndex] += shards;
        selectedHeroIndex = heroIndex;
        damage = GetTeamDamage();
        var hero = GetHeroDefinition(heroIndex);

        SetSummonResult($"{hero.rarityName} pull: {hero.name}\n+{shards} shards");
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

    public void AddDebugGold()
    {
        AddDebugResources(DebugGoldAmount, 0, 0);
    }

    public void AddDebugEssence()
    {
        AddDebugResources(0, 0, DebugEssenceAmount);
    }

    public void AddDebugGems()
    {
        AddDebugResources(0, DebugGemAmount, 0);
    }

    public void AddDebugAccessoryCopy()
    {
        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);
        AddAccessoryInventory(slot, rarity, 1);

        SaveProgress();
        RefreshUi();
        SetDungeonResult($"Debug: +1 {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} copy.");
    }

    public void ResetProgress()
    {
        ClearPrototypePlayerPrefs();
        saveVersion = CurrentSaveVersion;
        gold = GetCurrencyDefinition(GoldCurrencyId).starterAmount;
        gems = GetCurrencyDefinition(GemsCurrencyId).starterAmount;
        mythEssence = GetCurrencyDefinition(MythEssenceCurrencyId).starterAmount;
        damage = 1;
        enemyLevel = 1;
        goldDungeonFloor = 1;
        essenceDungeonFloor = 1;
        gearDungeonFloor = 1;
        weaponLevel = StarterEquipmentLevel;
        armorLevel = StarterEquipmentLevel;
        selectedAccessorySlot = 0;
        selectedAccessoryRarity = 0;
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
        EnsureAccessories();
        for (var i = 0; i < heroShards.Length; i++)
        {
            heroShards[i] = 0;
            heroAscensions[i] = 0;
        }

        for (var i = 0; i < AccessorySlotCount; i++)
        {
            equippedAccessoryRarities[i] = -1;
            equippedAccessoryLevels[i] = 0;
        }

        for (var i = 0; i < accessoryInventory.Length; i++)
        {
            accessoryInventory[i] = 0;
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
        SetDungeonResult("Prototype reset to fresh save.\nCurrencies, heroes, gear, missions cleared.");
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
            var clearedStage = enemyLevel;
            var milestoneText = GrantCampaignMilestoneReward(clearedStage);
            enemyLevel++;
            dailyStageClearCount++;
            enemyMaxHp = GetStageMaxHp(enemyLevel);
            enemyHp = enemyMaxHp;
            SetDungeonResult($"Campaign Stage {clearedStage} cleared in {result.rounds} rounds\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}{milestoneText}");
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
        saveVersion = Mathf.Max(1, PlayerPrefs.GetInt(SaveVersionKey, CurrentSaveVersion));
        gold = PlayerPrefs.GetInt(GoldKey, gold);
        gems = PlayerPrefs.GetInt(GemsKey, gems);
        mythEssence = PlayerPrefs.GetInt(MythEssenceKey, mythEssence);
        if (!PlayerPrefs.HasKey(GemsKey))
        {
            gems = GetCurrencyDefinition(GemsCurrencyId).starterAmount;
        }

        if (!PlayerPrefs.HasKey(MythEssenceKey))
        {
            mythEssence = Mathf.Max(GetCurrencyDefinition(MythEssenceCurrencyId).starterAmount, gold);
        }

        goldDungeonFloor = Mathf.Max(1, PlayerPrefs.GetInt(GoldDungeonFloorKey, goldDungeonFloor));
        essenceDungeonFloor = Mathf.Max(1, PlayerPrefs.GetInt(EssenceDungeonFloorKey, essenceDungeonFloor));
        gearDungeonFloor = Mathf.Max(1, PlayerPrefs.GetInt(GearDungeonFloorKey, gearDungeonFloor));
        weaponLevel = Mathf.Max(StarterEquipmentLevel, PlayerPrefs.GetInt(WeaponLevelKey, weaponLevel));
        armorLevel = Mathf.Max(StarterEquipmentLevel, PlayerPrefs.GetInt(ArmorLevelKey, armorLevel));
        selectedAccessorySlot = Mathf.Clamp(PlayerPrefs.GetInt(SelectedAccessorySlotKey, selectedAccessorySlot), 0, AccessorySlotCount - 1);
        selectedAccessoryRarity = Mathf.Clamp(PlayerPrefs.GetInt(SelectedAccessoryRarityKey, selectedAccessoryRarity), 0, AccessoryRarityCount - 1);
        enemyLevel = Mathf.Max(1, PlayerPrefs.GetInt(EnemyLevelKey, enemyLevel));
        enemyMaxHp = Mathf.Max(GetStageMaxHp(enemyLevel), PlayerPrefs.GetInt(EnemyMaxHpKey, enemyMaxHp));
        enemyHp = Mathf.Clamp(PlayerPrefs.GetInt(EnemyHpKey, enemyHp), 1, enemyMaxHp);
        selectedHeroIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedHeroKey, selectedHeroIndex), 0, HeroCount - 1);
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();
        EnsureAccessories();

        for (var i = 0; i < heroLevels.Length; i++)
        {
            heroLevels[i] = Mathf.Max(1, PlayerPrefs.GetInt($"{HeroLevelKeyPrefix}{i}", 1));
            heroShards[i] = Mathf.Max(0, PlayerPrefs.GetInt($"{HeroShardKeyPrefix}{i}", 0));
            heroAscensions[i] = Mathf.Max(0, PlayerPrefs.GetInt($"{HeroAscensionKeyPrefix}{i}", 0));
        }

        for (var i = 0; i < AccessorySlotCount; i++)
        {
            equippedAccessoryRarities[i] = Mathf.Clamp(PlayerPrefs.GetInt($"{EquippedAccessoryRarityKeyPrefix}{i}", -1), -1, AccessoryRarityCount - 1);
            equippedAccessoryLevels[i] = Mathf.Clamp(PlayerPrefs.GetInt($"{EquippedAccessoryLevelKeyPrefix}{i}", 0), 0, GetAccessoryMaxLevel(Mathf.Max(0, equippedAccessoryRarities[i])));
            if (equippedAccessoryRarities[i] < 0)
            {
                equippedAccessoryLevels[i] = 0;
            }
        }

        for (var i = 0; i < accessoryInventory.Length; i++)
        {
            accessoryInventory[i] = Mathf.Max(0, PlayerPrefs.GetInt($"{AccessoryInventoryKeyPrefix}{i}", accessoryInventory[i]));
        }

        summonCount = Mathf.Max(0, PlayerPrefs.GetInt(SummonCountKey, summonCount));
        LoadDailyProgress();
        damage = GetTeamDamage();
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);
    }

    private void SaveProgress()
    {
        saveVersion = CurrentSaveVersion;
        PlayerPrefs.SetInt(SaveVersionKey, saveVersion);
        PlayerPrefs.SetInt(GoldKey, gold);
        PlayerPrefs.SetInt(GemsKey, gems);
        PlayerPrefs.SetInt(MythEssenceKey, mythEssence);
        PlayerPrefs.SetInt(DamageKey, damage);
        PlayerPrefs.SetInt(GoldDungeonFloorKey, goldDungeonFloor);
        PlayerPrefs.SetInt(EssenceDungeonFloorKey, essenceDungeonFloor);
        PlayerPrefs.SetInt(GearDungeonFloorKey, gearDungeonFloor);
        PlayerPrefs.SetInt(WeaponLevelKey, weaponLevel);
        PlayerPrefs.SetInt(ArmorLevelKey, armorLevel);
        PlayerPrefs.SetInt(SelectedAccessorySlotKey, selectedAccessorySlot);
        PlayerPrefs.SetInt(SelectedAccessoryRarityKey, selectedAccessoryRarity);
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
        EnsureAccessories();
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

        for (var i = 0; i < AccessorySlotCount; i++)
        {
            PlayerPrefs.SetInt($"{EquippedAccessoryRarityKeyPrefix}{i}", equippedAccessoryRarities[i]);
            PlayerPrefs.SetInt($"{EquippedAccessoryLevelKeyPrefix}{i}", equippedAccessoryLevels[i]);
        }

        for (var i = 0; i < accessoryInventory.Length; i++)
        {
            PlayerPrefs.SetInt($"{AccessoryInventoryKeyPrefix}{i}", accessoryInventory[i]);
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

        return new StageDefinition(stage, $"Rift Echo {stage}", hp, reward);
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
        return GetDungeonRecommendedPower(GoldDungeonDefinition, floor);
    }

    private int GetGearDungeonEnemyHp(int floor)
    {
        return GetDungeonEnemyHp(GearDungeonDefinition, floor);
    }

    private int GetGearDungeonEnemyDamage(int floor)
    {
        return GetDungeonEnemyDamage(GearDungeonDefinition, floor);
    }

    private int GetGearDungeonRecommendedPower(int floor)
    {
        return GetDungeonRecommendedPower(GearDungeonDefinition, floor);
    }

    private void RunDungeon(bool isGoldDungeon)
    {
        var dungeon = isGoldDungeon ? GoldDungeonDefinition : EssenceDungeonDefinition;
        var floor = isGoldDungeon ? goldDungeonFloor : essenceDungeonFloor;
        var enemyHp = GetDungeonEnemyHp(dungeon, floor);
        var enemyDamage = GetDungeonEnemyDamage(dungeon, floor);
        var result = SimulateCombat(enemyHp, enemyDamage);

        if (!result.won)
        {
            SetDungeonResult($"{dungeon.displayName} Floor {floor} failed after {result.rounds} rounds\nEnemy HP {result.enemyHpRemaining}/{enemyHp}  {FormatCombatResult(result)}");
            RefreshUi();
            return;
        }

        var reward = GetDungeonReward(dungeon, floor);
        var bonusText = GrantDungeonBonusReward(isGoldDungeon, floor);
        if (isGoldDungeon)
        {
            gold += reward;
            goldDungeonFloor++;
            SetDungeonResult($"{dungeon.displayName} Floor {floor} cleared in {result.rounds} rounds (+{reward} Gold)\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}{bonusText}");
        }
        else
        {
            mythEssence += reward;
            essenceDungeonFloor++;
            SetDungeonResult($"{dungeon.displayName} Floor {floor} cleared in {result.rounds} rounds (+{reward} Essence)\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}{bonusText}");
        }

        SaveProgress();
        RefreshUi();
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
        return $"DMG {result.damageDealt}  Took {result.damageTaken}  Heal {result.healingDone}{executeText}";
    }

    private int GetGoldDungeonReward(int floor)
    {
        return GetDungeonReward(GoldDungeonDefinition, floor);
    }

    private int GetEssenceDungeonReward(int floor)
    {
        return GetDungeonReward(EssenceDungeonDefinition, floor);
    }

    private string GrantCampaignMilestoneReward(int clearedStage)
    {
        if (clearedStage <= 0 || clearedStage % CampaignMilestoneInterval != 0)
        {
            return string.Empty;
        }

        var rewardGems = 10 + Mathf.FloorToInt(clearedStage * 1.5f);
        var rewardPassXp = 20;
        var reward = new RewardDefinition($"reward_campaign_milestone_{clearedStage}", 0, rewardGems, 0, rewardPassXp);
        GrantReward(reward);

        return $"  Milestone +{rewardGems} Gems +{rewardPassXp} XP";
    }

    private string GrantDungeonBonusReward(bool isGoldDungeon, int clearedFloor)
    {
        if (clearedFloor <= 0 || clearedFloor % DungeonBonusInterval != 0)
        {
            return string.Empty;
        }

        if (isGoldDungeon)
        {
            var bonusGold = Mathf.CeilToInt(GetGoldDungeonReward(clearedFloor) * 0.75f);
            GrantReward(new RewardDefinition($"reward_gold_dungeon_bonus_{clearedFloor}", bonusGold, 0, 0));
            return $"  Bonus +{bonusGold} Gold";
        }

        var bonusEssence = Mathf.CeilToInt(GetEssenceDungeonReward(clearedFloor) * 0.75f);
        GrantReward(new RewardDefinition($"reward_essence_dungeon_bonus_{clearedFloor}", 0, 0, bonusEssence));
        return $"  Bonus +{bonusEssence} Essence";
    }

    private void SetDungeonResult(string result)
    {
        if (dungeonResultText != null)
        {
            dungeonResultText.enableAutoSizing = true;
            dungeonResultText.fontSizeMin = 18;
            dungeonResultText.fontSizeMax = 24;
            dungeonResultText.fontSize = 24;
            dungeonResultText.textWrappingMode = TextWrappingModes.Normal;
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

        if (versionText != null)
        {
            versionText.text = GetVersionLabel();
        }

        if (goldText != null)
        {
            var resourceText = $"Gold {gold}   Gems {gems}   Essence {mythEssence}";
            goldText.fontSize = versionText == null ? 30 : 36;
            goldText.text = versionText == null ? $"{resourceText}\n{GetVersionLabel()}" : resourceText;
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

        RefreshNextGoalUi();

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
        RefreshAccessoryUi();
        RefreshSummonUi();
        RefreshDailyMissionUi();
        RefreshBattlePassUi();

        if (upgradeCostText != null)
        {
            upgradeCostText.text = $"Upgrade {GetHeroDefinition(selectedHeroIndex).name} ({upgradeCost} Essence)";
        }

        if (heroUpgradeCostText != null)
        {
            heroUpgradeCostText.text = $"Upgrade {GetHeroDefinition(selectedHeroIndex).name} ({upgradeCost} Essence)";
        }

        if (heroAscendCostText != null)
        {
            heroAscendCostText.text = $"Ascend {GetHeroDefinition(selectedHeroIndex).name} ({GetHeroAscensionCost(selectedHeroIndex)} Shards)";
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

        RefreshAccessoryButtonStates();

        if (summonButton != null)
        {
            summonButton.interactable = gems >= GetSummonCost();
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

        if (gearTabButton != null)
        {
            gearTabButton.onClick.AddListener(ShowGear);
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

        if (gearTabButton != null)
        {
            gearTabButton.onClick.RemoveListener(ShowGear);
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
        SetPanel(gearPanel, screen == AppScreen.Gear);
        SetPanel(summonPanel, screen == AppScreen.Summon);
        SetPanel(shopPanel, screen == AppScreen.Shop);

        SetTabState(homeTabButton, screen == AppScreen.Home);
        SetTabState(battleTabButton, screen == AppScreen.Battle);
        SetTabState(heroesTabButton, screen == AppScreen.Heroes);
        SetTabState(gearTabButton, screen == AppScreen.Gear);
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
                    teamSlotTexts[i].text = $"{GetHeroDefinition(i).name}\nATK {GetHeroAttack(i)}\nHP {GetHeroHealth(i)}";
                }
            }
        }

        if (selectedHeroText != null)
        {
            var hero = GetHeroDefinition(selectedHeroIndex);
            selectedHeroText.text = $"{hero.name}  Lv. {heroLevels[selectedHeroIndex]}  Asc. {heroAscensions[selectedHeroIndex]}\n{hero.rarityName} {hero.roleName}  Power {GetHeroPower(selectedHeroIndex)}\nATK {GetHeroAttack(selectedHeroIndex)}  HP {GetHeroHealth(selectedHeroIndex)}  Shards {heroShards[selectedHeroIndex]}";
        }

        if (heroCardTexts != null)
        {
            for (var i = 0; i < Mathf.Min(heroCardTexts.Length, HeroCount); i++)
            {
                if (heroCardTexts[i] != null)
                {
                    var hero = GetHeroDefinition(i);
                    var marker = i == selectedHeroIndex ? "> " : string.Empty;
                    heroCardTexts[i].text = $"{marker}{hero.name}  Lv. {heroLevels[i]}  A{heroAscensions[i]}  Shards {heroShards[i]}\n{hero.rarityName} {hero.roleName}  ATK {GetHeroAttack(i)}  HP {GetHeroHealth(i)}";
                }
            }
        }
    }

    private void RefreshNextGoalUi()
    {
        if (nextGoalText == null)
        {
            return;
        }

        nextGoalText.fontSize = 26;
        nextGoalText.text = $"Next Goal\n{GetNextGoalText()}";
    }

    private string GetNextGoalText()
    {
        var summonCost = GetSummonCost();
        if (gems >= summonCost)
        {
            return $"Summon x1 to gain shards ({gems}/{summonCost} Gems)";
        }

        var weaponCost = GetWeaponUpgradeCost();
        if (gold >= weaponCost)
        {
            return $"Upgrade Weapon for more ATK ({gold}/{weaponCost} Gold)";
        }

        var armorCost = GetArmorUpgradeCost();
        if (gold >= armorCost)
        {
            return $"Upgrade Armor for more HP ({gold}/{armorCost} Gold)";
        }

        if (mythEssence >= upgradeCost)
        {
            return $"Level {GetHeroDefinition(selectedHeroIndex).name} with Myth Essence";
        }

        if (HasAccessoryCopiesToEquip())
        {
            return "Open Gear tab and equip new accessory drops";
        }

        var accessorySlot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        if (equippedAccessoryRarities[accessorySlot] >= 0 && gold >= GetAccessoryLevelCost(accessorySlot))
        {
            return "Level equipped accessories for extra stats";
        }

        if (GetTeamPower() >= GetStageRecommendedPower(enemyLevel))
        {
            return $"Push Campaign Stage {enemyLevel}";
        }

        var goldGap = Mathf.Max(0, Mathf.Min(weaponCost, armorCost) - gold);
        var essenceGap = Mathf.Max(0, upgradeCost - mythEssence);
        return $"Farm dungeons: need {goldGap} Gold, {essenceGap} Essence, or Gear drops";
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

    private void RefreshAccessoryUi()
    {
        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);

        if (accessorySummaryText != null)
        {
            accessorySummaryText.text = $"Accessories\nATK +{GetAccessoryAttackBonus()}  HP +{GetAccessoryHealthBonus()}\nGear Dungeon Floor {gearDungeonFloor}";
        }

        if (accessorySelectedText != null)
        {
            var equippedText = GetEquippedAccessoryText(slot);
            accessorySelectedText.text = $"{AccessorySlots[slot].name}\nEquipped: {equippedText}\nSelected Fuse Tier: {GetAccessoryRarityName(rarity)}";
        }

        if (accessoryInventoryText != null)
        {
            accessoryInventoryText.text = GetAccessoryInventoryText(slot);
        }

        if (accessoryEquipText != null)
        {
            accessoryEquipText.text = $"Equip {GetAccessoryRarityName(rarity)}\nCopies {GetAccessoryInventoryCount(slot, rarity)}";
        }

        if (accessoryLevelText != null)
        {
            var equippedRarity = equippedAccessoryRarities[slot];
            accessoryLevelText.text = equippedRarity < 0
                ? "Level Equipped\nNo item"
                : $"Level Equipped\n{GetAccessoryLevelCost(slot)} Gold";
        }

        if (accessoryFuseText != null)
        {
            var nextTier = rarity >= AccessoryRarityCount - 1 ? "Max" : GetAccessoryRarityName(rarity + 1);
            accessoryFuseText.text = $"Fuse {AccessoryFuseCost}x {GetAccessoryRarityName(rarity)}\nInto {nextTier}";
        }
    }

    private void RefreshAccessoryButtonStates()
    {
        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);
        var equippedRarity = equippedAccessoryRarities[slot];
        var canLevel = equippedRarity >= 0 && equippedAccessoryLevels[slot] < GetAccessoryMaxLevel(equippedRarity) && gold >= GetAccessoryLevelCost(slot);

        if (accessoryEquipButton != null)
        {
            accessoryEquipButton.interactable = equippedAccessoryRarities[slot] != rarity && GetAccessoryInventoryCount(slot, rarity) > 0;
        }

        if (accessoryLevelButton != null)
        {
            accessoryLevelButton.interactable = canLevel;
        }

        if (accessoryFuseButton != null)
        {
            accessoryFuseButton.interactable = rarity < AccessoryRarityCount - 1 && GetAccessoryInventoryCount(slot, rarity) >= AccessoryFuseCost;
        }
    }

    private string GetEquippedAccessoryText(int slot)
    {
        var rarity = equippedAccessoryRarities[slot];
        if (rarity < 0)
        {
            return "None";
        }

        var level = equippedAccessoryLevels[slot];
        return $"{GetAccessoryRarityName(rarity)} Lv. {level}/{GetAccessoryMaxLevel(rarity)} (+{GetAccessoryAttackFor(slot, rarity, level)} ATK, +{GetAccessoryHealthFor(slot, rarity, level)} HP)";
    }

    private string GetAccessoryInventoryText(int slot)
    {
        var text = "Inventory Copies";
        for (var rarity = 0; rarity < AccessoryRarityCount; rarity++)
        {
            text += $"\n{GetAccessoryRarityName(rarity)}: {GetAccessoryInventoryCount(slot, rarity)}";
        }

        return text;
    }

    private bool HasAccessoryCopiesToEquip()
    {
        EnsureAccessories();
        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            for (var rarity = 0; rarity < AccessoryRarityCount; rarity++)
            {
                if (accessoryInventory[GetAccessoryInventoryIndex(slot, rarity)] > 0 && equippedAccessoryRarities[slot] != rarity)
                {
                    return true;
                }
            }
        }

        return false;
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

    private void EnsureAccessories()
    {
        if (equippedAccessoryRarities == null || equippedAccessoryRarities.Length != AccessorySlotCount)
        {
            equippedAccessoryRarities = new int[AccessorySlotCount];
            for (var i = 0; i < equippedAccessoryRarities.Length; i++)
            {
                equippedAccessoryRarities[i] = -1;
            }
        }

        if (equippedAccessoryLevels == null || equippedAccessoryLevels.Length != AccessorySlotCount)
        {
            equippedAccessoryLevels = new int[AccessorySlotCount];
        }

        if (accessoryInventory == null || accessoryInventory.Length != AccessorySlotCount * AccessoryRarityCount)
        {
            accessoryInventory = new int[AccessorySlotCount * AccessoryRarityCount];
        }

        for (var i = 0; i < AccessorySlotCount; i++)
        {
            equippedAccessoryRarities[i] = Mathf.Clamp(equippedAccessoryRarities[i], -1, AccessoryRarityCount - 1);
            if (equippedAccessoryRarities[i] < 0)
            {
                equippedAccessoryLevels[i] = 0;
            }
            else
            {
                equippedAccessoryLevels[i] = Mathf.Clamp(equippedAccessoryLevels[i], 1, GetAccessoryMaxLevel(equippedAccessoryRarities[i]));
            }
        }

        for (var i = 0; i < accessoryInventory.Length; i++)
        {
            accessoryInventory[i] = Mathf.Max(0, accessoryInventory[i]);
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
        var mission = GetDailyMissionDefinition(missionIndex);

        if (dailyMissionClaimed[missionIndex] || GetDailyMissionProgress(missionIndex) < mission.target)
        {
            RefreshUi();
            return;
        }

        dailyMissionClaimed[missionIndex] = true;
        GrantReward(mission.reward);

        SaveProgress();
        RefreshUi();
    }

    private void ClaimBattlePassReward(int rewardIndex)
    {
        rewardIndex = Mathf.Clamp(rewardIndex, 0, BattlePassRewardCount - 1);
        EnsureBattlePassRewardClaims();
        var rewardDefinition = GetBattlePassRewardDefinition(rewardIndex);

        if (battlePassRewardsClaimed[rewardIndex] || battlePassXp < rewardDefinition.requiredXp)
        {
            RefreshUi();
            return;
        }

        battlePassRewardsClaimed[rewardIndex] = true;
        GrantReward(rewardDefinition.reward);

        SaveProgress();
        RefreshUi();
    }

    private int GetDailyMissionProgress(int missionIndex)
    {
        switch (GetDailyMissionDefinition(missionIndex).progressType)
        {
            case DailyMissionProgressType.Fight:
                return dailyFightCount;
            case DailyMissionProgressType.StageClear:
                return dailyStageClearCount;
            case DailyMissionProgressType.Summon:
                return dailySummonCount;
            default:
                return 0;
        }
    }

    private string GetDailyDateKey()
    {
        return DateTime.UtcNow.ToString("yyyyMMdd");
    }

    private static CurrencyDefinition GetCurrencyDefinition(string currencyId)
    {
        for (var i = 0; i < CurrencyDefinitions.Length; i++)
        {
            if (CurrencyDefinitions[i].currencyId == currencyId)
            {
                return CurrencyDefinitions[i];
            }
        }

        return CurrencyDefinitions[0];
    }

    private static HeroDefinition GetHeroDefinition(int index)
    {
        index = Mathf.Clamp(index, 0, HeroDefinitions.Length - 1);
        return HeroDefinitions[index];
    }

    private static DailyMissionDefinition GetDailyMissionDefinition(int index)
    {
        index = Mathf.Clamp(index, 0, DailyMissionDefinitions.Length - 1);
        return DailyMissionDefinitions[index];
    }

    private static BattlePassRewardDefinition GetBattlePassRewardDefinition(int index)
    {
        index = Mathf.Clamp(index, 0, BattlePassRewardDefinitions.Length - 1);
        return BattlePassRewardDefinitions[index];
    }

    private static AccessoryRarityDefinition GetAccessoryRarityDefinition(int rarity)
    {
        rarity = Mathf.Clamp(rarity, 0, AccessoryRarities.Length - 1);
        return AccessoryRarities[rarity];
    }

    private static string GetAccessoryRarityName(int rarity)
    {
        return GetAccessoryRarityDefinition(rarity).displayName;
    }

    private static string GetHeroRarityName(string rarityId)
    {
        for (var i = 0; i < HeroDefinitions.Length; i++)
        {
            if (HeroDefinitions[i].rarityId == rarityId)
            {
                return HeroDefinitions[i].rarityName;
            }
        }

        return rarityId;
    }

    private static int GetSummonCost()
    {
        return HeroShardBanner.costAmount;
    }

    private static string GetSummonRatesText()
    {
        var text = "Rates";

        if (HeroShardBanner.rates == null || HeroShardBanner.rates.Length == 0)
        {
            return text;
        }

        for (var i = HeroShardBanner.rates.Length - 1; i >= 0; i--)
        {
            var lowerBound = i > 0 ? HeroShardBanner.rates[i - 1].cumulativeChance : 0;
            var chance = Mathf.Max(0, HeroShardBanner.rates[i].cumulativeChance - lowerBound);
            text += i == HeroShardBanner.rates.Length - 1 ? "\n" : "  ";
            text += $"{GetHeroRarityName(HeroShardBanner.rates[i].rarityId)} {chance}%";
        }

        return text;
    }

    private static int GetDungeonEnemyHp(DungeonDefinition dungeon, int floor)
    {
        return GetScaledDefinitionValue(dungeon.baseEnemyHp, dungeon.enemyHpScale, dungeon.enemyHpGrowth, floor);
    }

    private static int GetDungeonEnemyDamage(DungeonDefinition dungeon, int floor)
    {
        return GetScaledDefinitionValue(dungeon.baseEnemyDamage, dungeon.enemyDamageScale, dungeon.enemyDamageGrowth, floor);
    }

    private static int GetDungeonRecommendedPower(DungeonDefinition dungeon, int floor)
    {
        return GetScaledDefinitionValue(dungeon.baseRecommendedPower, dungeon.recommendedPowerScale, dungeon.recommendedPowerGrowth, floor);
    }

    private static int GetDungeonReward(DungeonDefinition dungeon, int floor)
    {
        return GetScaledDefinitionValue(dungeon.baseReward, dungeon.rewardScale, dungeon.rewardGrowth, floor);
    }

    private static int GetScaledDefinitionValue(int baseValue, float scale, float growth, int index)
    {
        index = Mathf.Max(1, index);
        return baseValue + Mathf.FloorToInt(scale * Mathf.Pow(index, growth));
    }

    private void GrantReward(RewardDefinition reward)
    {
        gold += reward.gold;
        gems += reward.gems;
        mythEssence += reward.mythEssence;
        battlePassXp += reward.passXp;
    }

    private void EnsureStages()
    {
        if (stages != null && stages.Length > 0 && !string.IsNullOrWhiteSpace(stages[0].stageId))
        {
            return;
        }

        stages = StarterStageDefinitions;
    }

    private int GetTeamPower()
    {
        var power = 0;

        for (var i = 0; i < HeroCount; i++)
        {
            power += GetHeroPower(i);
        }

        return power + GetEquipmentPower() + GetAccessoryPower();
    }

    private int GetTeamDamage()
    {
        var multiplier = 1f
            + (CountHeroesWithRole(WarriorRoleId) * WarriorDamageBonusRate)
            + (CountHeroesWithRole(MageRoleId) * MageDamageBonusRate);

        return Mathf.Max(1, Mathf.FloorToInt((GetTeamBaseAttack() + GetEquipmentAttackBonus() + GetAccessoryAttackBonus()) * multiplier));
    }

    private int GetTeamHealth()
    {
        var health = 0;

        for (var i = 0; i < HeroCount; i++)
        {
            health += GetHeroHealth(i);
        }

        return Mathf.Max(1, health + GetEquipmentHealthBonus() + GetAccessoryHealthBonus());
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

    private int GetAccessoryPower()
    {
        return GetAccessoryAttackBonus() + Mathf.FloorToInt(GetAccessoryHealthBonus() / 8f);
    }

    private int GetEquipmentAttackBonus()
    {
        return GetEquipmentBonus(WeaponTrack, weaponLevel);
    }

    private int GetEquipmentHealthBonus()
    {
        return GetEquipmentBonus(ArmorTrack, armorLevel);
    }

    private int GetAccessoryAttackBonus()
    {
        EnsureAccessories();
        var attack = 0;

        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            attack += GetAccessoryAttackFor(slot, equippedAccessoryRarities[slot], equippedAccessoryLevels[slot]);
        }

        return attack;
    }

    private int GetAccessoryHealthBonus()
    {
        EnsureAccessories();
        var health = 0;

        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            health += GetAccessoryHealthFor(slot, equippedAccessoryRarities[slot], equippedAccessoryLevels[slot]);
        }

        return health;
    }

    private int GetAccessoryAttackFor(int slot, int rarity, int level)
    {
        if (rarity < 0 || level <= 0)
        {
            return 0;
        }

        var rarityDefinition = GetAccessoryRarityDefinition(rarity);
        return AccessorySlots[slot].attackPerLevel * level * rarityDefinition.statMultiplier;
    }

    private int GetAccessoryHealthFor(int slot, int rarity, int level)
    {
        if (rarity < 0 || level <= 0)
        {
            return 0;
        }

        var rarityDefinition = GetAccessoryRarityDefinition(rarity);
        return AccessorySlots[slot].healthPerLevel * level * rarityDefinition.statMultiplier;
    }

    private int GetAccessoryMaxLevel(int rarity)
    {
        return GetAccessoryRarityDefinition(rarity).maxLevel;
    }

    private int GetAccessoryLevelCost(int slot)
    {
        EnsureAccessories();
        var rarity = equippedAccessoryRarities[slot];
        if (rarity < 0)
        {
            return 0;
        }

        var level = Mathf.Max(1, equippedAccessoryLevels[slot]);
        var rarityDefinition = GetAccessoryRarityDefinition(rarity);
        return Mathf.CeilToInt(rarityDefinition.levelCostBase * Mathf.Pow(rarityDefinition.levelCostGrowth, level - 1));
    }

    private int GetAccessoryInventoryIndex(int slot, int rarity)
    {
        slot = Mathf.Clamp(slot, 0, AccessorySlotCount - 1);
        rarity = Mathf.Clamp(rarity, 0, AccessoryRarityCount - 1);
        return (slot * AccessoryRarityCount) + rarity;
    }

    private int GetAccessoryInventoryCount(int slot, int rarity)
    {
        EnsureAccessories();
        return accessoryInventory[GetAccessoryInventoryIndex(slot, rarity)];
    }

    private void AddAccessoryInventory(int slot, int rarity, int amount)
    {
        EnsureAccessories();
        var index = GetAccessoryInventoryIndex(slot, rarity);
        accessoryInventory[index] = Mathf.Max(0, accessoryInventory[index] + amount);
    }

    private int RollAccessoryRarity(int floor)
    {
        floor = Mathf.Max(1, floor);
        var roll = UnityEngine.Random.Range(0, 1000);
        var r4Threshold = Mathf.Min(45, Mathf.FloorToInt(floor * 1.1f));
        var r3Threshold = 75 + Mathf.Min(120, Mathf.FloorToInt(floor * 2.5f));
        var r2Threshold = 230 + Mathf.Min(160, Mathf.FloorToInt(floor * 3.2f));
        var r1Threshold = 560 + Mathf.Min(120, Mathf.FloorToInt(floor * 2.2f));

        if (roll < r4Threshold)
        {
            return 4;
        }

        if (roll < r3Threshold)
        {
            return 3;
        }

        if (roll < r2Threshold)
        {
            return 2;
        }

        if (roll < r1Threshold)
        {
            return 1;
        }

        return 0;
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
        var hero = GetHeroDefinition(index);

        return hero.baseAttack
            + (heroLevels[index] * hero.attackGrowth)
            + Mathf.FloorToInt(heroShards[index] * 0.25f)
            + (heroAscensions[index] * hero.ascensionAttack);
    }

    private int GetHeroHealth(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();
        var hero = GetHeroDefinition(index);

        return hero.baseHealth
            + (heroLevels[index] * hero.healthGrowth)
            + Mathf.FloorToInt(heroShards[index] * 1.2f)
            + (heroAscensions[index] * hero.ascensionHealth);
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
        var hero = GetHeroDefinition(index);
        return hero.ascensionBaseCost + (heroAscensions[index] * hero.ascensionCostGrowth);
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

    private int CountHeroesWithRole(string roleId)
    {
        var count = 0;

        for (var i = 0; i < HeroCount; i++)
        {
            if (GetHeroDefinition(i).roleId == roleId)
            {
                count++;
            }
        }

        return count;
    }

    private bool ShouldExecuteEnemy(int enemyHpRemaining, int enemyMaxHp)
    {
        if (CountHeroesWithRole(RangerRoleId) <= 0)
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
        var supports = CountHeroesWithRole(SupportRoleId);
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
            goldDungeonText.text = $"{GoldDungeonDefinition.displayName} F{goldDungeonFloor}  Rec {GetDungeonRecommendedPower(goldDungeonFloor)}\n+{GetGoldDungeonReward(goldDungeonFloor)} {GetCurrencyDefinition(GoldDungeonDefinition.rewardCurrencyId).displayName}";
        }

        if (essenceDungeonText != null)
        {
            essenceDungeonText.text = $"{EssenceDungeonDefinition.displayName} F{essenceDungeonFloor}  Rec {GetDungeonRecommendedPower(essenceDungeonFloor)}\n+{GetEssenceDungeonReward(essenceDungeonFloor)} {GetCurrencyDefinition(EssenceDungeonDefinition.rewardCurrencyId).displayName}";
        }

        if (gearDungeonText != null)
        {
            gearDungeonText.text = $"{GearDungeonDefinition.displayName} F{gearDungeonFloor}  Rec {GetGearDungeonRecommendedPower(gearDungeonFloor)}\nRandom accessory drop";
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
            summonCostText.text = $"Cost: {GetSummonCost()} Gems";
        }

        if (summonRatesText != null)
        {
            summonRatesText.text = GetSummonRatesText();
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
            var mission = GetDailyMissionDefinition(i);
            var progress = Mathf.Min(GetDailyMissionProgress(i), mission.target);
            var isComplete = progress >= mission.target;
            var isClaimed = dailyMissionClaimed[i];
            var state = isClaimed ? "Claimed" : isComplete ? "Claim" : $"{progress}/{mission.target}";
            var text = $"{mission.title}\n{state}  Reward {FormatReward(mission.reward)}";

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
            var rewardDefinition = GetBattlePassRewardDefinition(i);
            var isReady = battlePassXp >= rewardDefinition.requiredXp;
            var isClaimed = battlePassRewardsClaimed[i];
            var state = isClaimed ? "Claimed" : isReady ? "Claim" : $"{battlePassXp}/{rewardDefinition.requiredXp} XP";
            var text = $"Level {i + 1}  {state}\nReward {FormatReward(rewardDefinition.reward)}";

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

        if (HeroShardBanner.rates != null)
        {
            for (var i = 0; i < HeroShardBanner.rates.Length; i++)
            {
                if (roll < HeroShardBanner.rates[i].cumulativeChance)
                {
                    return PickRandomHero(HeroShardBanner.rates[i].heroIndexes);
                }
            }
        }

        return 0;
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
        return GetHeroDefinition(heroIndex).summonShardReward;
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

    private string FormatReward(RewardDefinition reward)
    {
        return FormatReward(reward.gold, reward.gems, reward.mythEssence);
    }

    private void AddDebugResources(int goldAmount, int gemAmount, int essenceAmount)
    {
        gold += Mathf.Max(0, goldAmount);
        gems += Mathf.Max(0, gemAmount);
        mythEssence += Mathf.Max(0, essenceAmount);

        SaveProgress();
        RefreshUi();
        SetDungeonResult($"Debug: +{FormatReward(goldAmount, gemAmount, essenceAmount)}.");
    }

    private void ClearPrototypePlayerPrefs()
    {
        foreach (var key in ScalarSaveKeys)
        {
            PlayerPrefs.DeleteKey(key);
        }

        for (var i = 0; i < HeroCount; i++)
        {
            PlayerPrefs.DeleteKey($"{HeroLevelKeyPrefix}{i}");
            PlayerPrefs.DeleteKey($"{HeroShardKeyPrefix}{i}");
            PlayerPrefs.DeleteKey($"{HeroAscensionKeyPrefix}{i}");
        }

        for (var i = 0; i < DailyMissionCount; i++)
        {
            PlayerPrefs.DeleteKey($"{DailyMissionClaimedKeyPrefix}{i}");
        }

        for (var i = 0; i < BattlePassRewardCount; i++)
        {
            PlayerPrefs.DeleteKey($"{BattlePassClaimedKeyPrefix}{i}");
        }

        for (var i = 0; i < AccessorySlotCount; i++)
        {
            PlayerPrefs.DeleteKey($"{EquippedAccessoryRarityKeyPrefix}{i}");
            PlayerPrefs.DeleteKey($"{EquippedAccessoryLevelKeyPrefix}{i}");
        }

        for (var i = 0; i < AccessorySlotCount * AccessoryRarityCount; i++)
        {
            PlayerPrefs.DeleteKey($"{AccessoryInventoryKeyPrefix}{i}");
        }

        PlayerPrefs.Save();
    }

    private string GetVersionLabel()
    {
        return $"Prototype v{PrototypeVersion}  Save v{saveVersion}";
    }

    private void EnsureRuntimeDebugUi()
    {
        if (battlePanel == null || debugGoldButton != null)
        {
            return;
        }

        var panelObject = new GameObject("Debug Resource Panel", typeof(RectTransform));
        panelObject.transform.SetParent(battlePanel.transform, false);
        SetRuntimeRect(panelObject.GetComponent<RectTransform>(), new Vector2(0, -1430), new Vector2(860, 72), new Vector2(0.5f, 1f));

        debugGoldButton = CreateRuntimeDebugButton(panelObject.transform, "Debug Gold Button", "+Gold", -315);
        debugEssenceButton = CreateRuntimeDebugButton(panelObject.transform, "Debug Essence Button", "+Essence", -105);
        debugGemsButton = CreateRuntimeDebugButton(panelObject.transform, "Debug Gems Button", "+Gems", 105);
        debugAccessoryButton = CreateRuntimeDebugButton(panelObject.transform, "Debug Accessory Button", "+Gear", 315);
    }

    private static Button CreateRuntimeDebugButton(Transform parent, string name, string label, float xPosition)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRuntimeRect(buttonObject.GetComponent<RectTransform>(), new Vector2(xPosition, 0), new Vector2(190, 64), new Vector2(0.5f, 0.5f));

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.11f, 0.14f, 0.2f, 0.98f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 24;
        text.fontSizeMin = 16;
        text.fontSizeMax = 24;
        text.enableAutoSizing = true;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        StretchRuntime(text.rectTransform, new Vector2(18, 10));

        return button;
    }

    private static void SetRuntimeRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static void StretchRuntime(RectTransform rectTransform, Vector2 padding)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = padding;
        rectTransform.offsetMax = -padding;
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
