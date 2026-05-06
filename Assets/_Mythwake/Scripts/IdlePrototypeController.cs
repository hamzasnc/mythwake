using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdlePrototypeController : MonoBehaviour, IMythwakePlayerStateService, IMythwakeEconomyService, IMythwakeBattleService, IMythwakeSummonService, IMythwakeInventoryService
{
    public const string PrototypeVersion = "0.2.8";
    public const int CurrentSaveVersion = 2;

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

    private struct CampaignBalanceDefinition
    {
        public int baseRecommendedPower;
        public float recommendedPowerScale;
        public float recommendedPowerGrowth;
        public int baseEnemyDamage;
        public float enemyDamageScale;
        public float enemyDamageGrowth;
        public int milestoneGemBase;
        public float milestoneGemScale;
        public int milestonePassXp;
        public float overflowHpGrowth;
        public float overflowRewardGrowth;

        public CampaignBalanceDefinition(
            int baseRecommendedPower,
            float recommendedPowerScale,
            float recommendedPowerGrowth,
            int baseEnemyDamage,
            float enemyDamageScale,
            float enemyDamageGrowth,
            int milestoneGemBase,
            float milestoneGemScale,
            int milestonePassXp,
            float overflowHpGrowth,
            float overflowRewardGrowth)
        {
            this.baseRecommendedPower = baseRecommendedPower;
            this.recommendedPowerScale = recommendedPowerScale;
            this.recommendedPowerGrowth = recommendedPowerGrowth;
            this.baseEnemyDamage = baseEnemyDamage;
            this.enemyDamageScale = enemyDamageScale;
            this.enemyDamageGrowth = enemyDamageGrowth;
            this.milestoneGemBase = milestoneGemBase;
            this.milestoneGemScale = milestoneGemScale;
            this.milestonePassXp = milestonePassXp;
            this.overflowHpGrowth = overflowHpGrowth;
            this.overflowRewardGrowth = overflowRewardGrowth;
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

    private struct AccessoryDefinition
    {
        public string accessoryId;
        public string itemSlotId;
        public string rarityId;
        public int slotIndex;
        public int rarityIndex;
        public int levelCap;
        public int attackPerLevel;
        public int healthPerLevel;
        public int dropWeight;
        public string fuseTargetAccessoryId;

        public AccessoryDefinition(
            string accessoryId,
            string itemSlotId,
            string rarityId,
            int slotIndex,
            int rarityIndex,
            int levelCap,
            int attackPerLevel,
            int healthPerLevel,
            int dropWeight,
            string fuseTargetAccessoryId)
        {
            this.accessoryId = accessoryId;
            this.itemSlotId = itemSlotId;
            this.rarityId = rarityId;
            this.slotIndex = slotIndex;
            this.rarityIndex = rarityIndex;
            this.levelCap = levelCap;
            this.attackPerLevel = attackPerLevel;
            this.healthPerLevel = healthPerLevel;
            this.dropWeight = dropWeight;
            this.fuseTargetAccessoryId = fuseTargetAccessoryId;
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

    [Serializable]
    private sealed class PrototypeSaveData
    {
        public int saveVersion;
        public int gold;
        public int gems;
        public int mythEssence;
        public int damage;
        public int enemyLevel;
        public int enemyHp;
        public int enemyMaxHp;
        public int upgradeCost;
        public int goldDungeonFloor;
        public int essenceDungeonFloor;
        public int gearDungeonFloor;
        public int weaponLevel;
        public int armorLevel;
        public int selectedAccessorySlot;
        public int selectedAccessoryRarity;
        public int selectedHeroIndex;
        public int summonCount;
        public string dailyDate;
        public int dailyFightCount;
        public int dailyStageClearCount;
        public int dailySummonCount;
        public int battlePassXp;
        public string lastSeenUtcTicks;
        public int[] heroLevels;
        public int[] heroShards;
        public int[] heroAscensions;
        public int[] equippedAccessoryRarities;
        public int[] equippedAccessoryLevels;
        public int[] accessoryInventory;
        public bool[] dailyMissionClaimed;
        public bool[] battlePassRewardsClaimed;
    }

    private const string SaveJsonKey = "Mythwake.Prototype.SaveJson";
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
    private const int SummonCost = 35;
    private const int StarterGems = 35;
    private const int StarterMythEssence = 20;
    private const float OfflineGoldRewardRate = 0.5f;
    private const int MaxCombatRounds = 45;
    private const float WarriorDamageBonusRate = 0.06f;
    private const float MageDamageBonusRate = 0.1f;
    private const float TankDamageReductionRate = 0.18f;
    private const float SupportHealRate = 0.04f;
    private const float RangerExecuteThresholdRate = 0.12f;
    private const int StarterEquipmentLevel = 1;
    private const int CampaignMilestoneInterval = 5;
    private const int DungeonBonusInterval = 5;
    private const int AccessoryFuseCost = 3;
    private const int DebugGoldAmount = 500;
    private const int DebugGemAmount = 30;
    private const int DebugEssenceAmount = 250;

    private static readonly string[] SaveKeys =
    {
        SaveJsonKey,
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
        new DailyMissionDefinition("daily_battles_15", "Battle 15 times", DailyMissionProgressType.Fight, 15, new RewardDefinition("reward_daily_battles_15", 40, 5, 70, BattlePassXpPerDailyClaim)),
        new DailyMissionDefinition("daily_stage_clears_3", "Clear 3 stages", DailyMissionProgressType.StageClear, 3, new RewardDefinition("reward_daily_stage_clears_3", 70, 10, 110, BattlePassXpPerDailyClaim)),
        new DailyMissionDefinition("daily_summon_1", "Summon 1 hero", DailyMissionProgressType.Summon, 1, new RewardDefinition("reward_daily_summon_1", 35, 20, 55, BattlePassXpPerDailyClaim))
    };

    private static readonly BattlePassRewardDefinition[] BattlePassRewardDefinitions =
    {
        new BattlePassRewardDefinition("mission_track_reward_01", 40, new RewardDefinition("reward_mission_track_01", 100, 10, 0)),
        new BattlePassRewardDefinition("mission_track_reward_02", 80, new RewardDefinition("reward_mission_track_02", 125, 15, 120)),
        new BattlePassRewardDefinition("mission_track_reward_03", 120, new RewardDefinition("reward_mission_track_03", 175, 20, 0)),
        new BattlePassRewardDefinition("mission_track_reward_04", 180, new RewardDefinition("reward_mission_track_04", 225, 25, 180)),
        new BattlePassRewardDefinition("mission_track_reward_05", 240, new RewardDefinition("reward_mission_track_05", 350, 40, 300))
    };

    private static readonly EquipmentTrackDefinition WeaponTrack = new EquipmentTrackDefinition("equipment_weapon", "Weapon", "ATK", GoldCurrencyId, 8, 9, 80, 1.42f);
    private static readonly EquipmentTrackDefinition ArmorTrack = new EquipmentTrackDefinition("equipment_armor", "Armor", "HP", GoldCurrencyId, 80, 65, 75, 1.39f);

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

    private static readonly AccessoryDefinition[] AccessoryDefinitions = CreateAccessoryDefinitions();

    private static readonly DungeonDefinition GoldDungeonDefinition = new DungeonDefinition("gold_dungeon", "Gold Dungeon", GoldCurrencyId, 200, 100f, 1.2f, 22, 9f, 1.12f, 115, 50f, 1.18f, 95, 34f, 1.14f);
    private static readonly DungeonDefinition EssenceDungeonDefinition = new DungeonDefinition("essence_dungeon", "Essence Dungeon", MythEssenceCurrencyId, 210, 104f, 1.2f, 22, 9.5f, 1.12f, 118, 51f, 1.18f, 110, 40f, 1.13f);
    private static readonly DungeonDefinition GearDungeonDefinition = new DungeonDefinition("gear_dungeon", "Gear Dungeon", string.Empty, 235, 120f, 1.21f, 24, 10f, 1.14f, 130, 56f, 1.18f, 0, 0f, 1f);
    private static readonly CampaignBalanceDefinition CampaignBalance = new CampaignBalanceDefinition(90, 46f, 1.17f, 10, 5.8f, 1.16f, 12, 1.6f, 25, 1.23f, 1.15f);

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
    [SerializeField] private string lastSeenUtcTicks = string.Empty;

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
    [SerializeField] private int maxOfflineSeconds = 6 * 60 * 60;

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
        FightCampaign();
    }

    public void RunGoldDungeon()
    {
        RunDungeon(GoldDungeonDefinition.dungeonId);
    }

    public void RunEssenceDungeon()
    {
        RunDungeon(EssenceDungeonDefinition.dungeonId);
    }

    public void RunGearDungeon()
    {
        RunDungeon(GearDungeonDefinition.dungeonId);
    }

    public MythwakeActionResultDto FightCampaign()
    {
        var result = ExecuteCampaignFight();
        SaveProgress();
        RefreshUi();
        return result;
    }

    public MythwakeActionResultDto RunDungeon(string dungeonId)
    {
        if (dungeonId == GoldDungeonDefinition.dungeonId)
        {
            return ExecuteResourceDungeon(isGoldDungeon: true);
        }

        if (dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return ExecuteResourceDungeon(isGoldDungeon: false);
        }

        if (dungeonId == GearDungeonDefinition.dungeonId)
        {
            return ExecuteGearDungeon();
        }

        var result = CreateActionResult(false, "run_dungeon", "invalid_dungeon", $"Unknown dungeon: {dungeonId}");
        SetDungeonResult(result.message);
        RefreshUi();
        return result;
    }

    private MythwakeActionResultDto ExecuteGearDungeon()
    {
        var floor = Mathf.Max(1, gearDungeonFloor);
        var enemyHp = GetGearDungeonEnemyHp(floor);
        var enemyDamage = GetGearDungeonEnemyDamage(floor);
        var result = SimulateCombat(enemyHp, enemyDamage);

        if (!result.won)
        {
            var failMessage = $"Gear Dungeon Floor {floor} failed after {result.rounds} rounds\nEnemy HP {result.enemyHpRemaining}/{enemyHp}  {FormatCombatResult(result)}";
            SetDungeonResult(failMessage);
            RefreshUi();
            return CreateActionResult(false, "gear_dungeon_run", "combat_lost", failMessage);
        }

        var accessory = RollAccessoryDrop(floor);
        AddAccessoryInventory(accessory.slotIndex, accessory.rarityIndex, 1);
        gearDungeonFloor++;

        var message = $"Gear Dungeon Floor {floor} cleared in {result.rounds} rounds\nDrop: {GetAccessoryRarityName(accessory.rarityIndex)} {AccessorySlots[accessory.slotIndex].name}  HP {result.teamHpRemaining}/{GetTeamHealth()}";
        SetDungeonResult(message);
        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "gear_dungeon_run", string.Empty, message);
    }

    public void UpgradeDamage()
    {
        selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);

        if (!TrySpendCurrency(MythEssenceCurrencyId, upgradeCost))
        {
            RefreshUi();
            return;
        }

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

        if (!TrySpendCurrency(GoldCurrencyId, cost))
        {
            RefreshUi();
            return;
        }

        weaponLevel++;
        damage = GetTeamDamage();

        SaveProgress();
        RefreshUi();
    }

    public void UpgradeArmor()
    {
        armorLevel = Mathf.Max(StarterEquipmentLevel, armorLevel);
        var cost = GetArmorUpgradeCost();

        if (!TrySpendCurrency(GoldCurrencyId, cost))
        {
            RefreshUi();
            return;
        }

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
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);
        EquipAccessory(GetAccessoryDefinition(slot, rarity).accessoryId);
    }

    public MythwakeActionResultDto EquipAccessory(string accessoryId)
    {
        EnsureAccessories();
        if (!TryGetAccessoryDefinitionById(accessoryId, out var accessory))
        {
            var invalidResult = CreateActionResult(false, "accessory_equip", "invalid_accessory", $"Unknown accessory: {accessoryId}");
            RefreshUi();
            return invalidResult;
        }

        var slot = accessory.slotIndex;
        var rarity = accessory.rarityIndex;

        if (GetAccessoryInventoryCount(slot, rarity) <= 0)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_equip", "missing_item", $"No {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} copy to equip.");
        }

        if (equippedAccessoryRarities[slot] == rarity)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_equip", "already_equipped", $"{GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} is already equipped.");
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
        return CreateActionResult(true, "accessory_equip", string.Empty, $"Equipped {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name}.");
    }

    public void LevelSelectedAccessory()
    {
        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = equippedAccessoryRarities[slot];
        var accessoryId = rarity >= 0 ? GetAccessoryDefinition(slot, rarity).accessoryId : string.Empty;
        LevelAccessory(accessoryId);
    }

    public MythwakeActionResultDto LevelAccessory(string accessoryId)
    {
        EnsureAccessories();
        if (!TryGetAccessoryDefinitionById(accessoryId, out var accessory))
        {
            var invalidResult = CreateActionResult(false, "accessory_level", "invalid_accessory", $"Unknown accessory: {accessoryId}");
            RefreshUi();
            return invalidResult;
        }

        var slot = accessory.slotIndex;
        var rarity = accessory.rarityIndex;

        if (equippedAccessoryRarities[slot] != rarity)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_level", "not_equipped", $"{GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} is not equipped.");
        }

        var maxLevel = GetAccessoryMaxLevel(rarity);
        if (equippedAccessoryLevels[slot] >= maxLevel)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_level", "max_level", $"{GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} is already max level.");
        }

        var cost = GetAccessoryLevelCost(slot);
        if (!TrySpendCurrency(GoldCurrencyId, cost))
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_level", "insufficient_currency", $"Need {cost} Gold to level {AccessorySlots[slot].name}.");
        }

        equippedAccessoryLevels[slot]++;
        damage = GetTeamDamage();

        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "accessory_level", string.Empty, $"Leveled {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} to Lv. {equippedAccessoryLevels[slot]}.");
    }

    public void FuseSelectedAccessory()
    {
        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);
        FuseAccessory(GetAccessoryDefinition(slot, rarity).accessoryId);
    }

    public MythwakeActionResultDto FuseAccessory(string accessoryId)
    {
        EnsureAccessories();
        if (!TryGetAccessoryDefinitionById(accessoryId, out var accessory))
        {
            var invalidResult = CreateActionResult(false, "accessory_fuse", "invalid_accessory", $"Unknown accessory: {accessoryId}");
            RefreshUi();
            return invalidResult;
        }

        var slot = accessory.slotIndex;
        var rarity = accessory.rarityIndex;

        if (string.IsNullOrEmpty(accessory.fuseTargetAccessoryId) || GetAccessoryInventoryCount(slot, rarity) < AccessoryFuseCost)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_fuse", "missing_items", $"Need {AccessoryFuseCost}x {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} to fuse.");
        }

        var fuseTarget = GetAccessoryDefinitionById(accessory.fuseTargetAccessoryId);
        AddAccessoryInventory(slot, rarity, -AccessoryFuseCost);
        AddAccessoryInventory(fuseTarget.slotIndex, fuseTarget.rarityIndex, 1);
        selectedAccessoryRarity = fuseTarget.rarityIndex;

        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "accessory_fuse", string.Empty, $"Fused into {GetAccessoryRarityName(fuseTarget.rarityIndex)} {AccessorySlots[fuseTarget.slotIndex].name}.");
    }

    public void SummonOnce()
    {
        Pull(HeroShardBanner.bannerId);
    }

    public MythwakeActionResultDto Pull(string bannerId)
    {
        if (bannerId != HeroShardBanner.bannerId)
        {
            var invalidBanner = CreateActionResult(false, "summon_pull", "invalid_banner", $"Unknown banner: {bannerId}");
            SetSummonResult(invalidBanner.message);
            RefreshUi();
            return invalidBanner;
        }

        var summonCost = GetSummonCost();
        if (!TrySpendCurrency(GemsCurrencyId, summonCost))
        {
            var failMessage = $"Need {summonCost} Gems for a summon.";
            SetSummonResult(failMessage);
            RefreshUi();
            return CreateActionResult(false, "summon_pull", "insufficient_currency", failMessage);
        }

        EnsureHeroShards();

        summonCount++;
        dailySummonCount++;

        var heroIndex = RollSummonHero();
        var shards = GetSummonShardReward(heroIndex);
        heroShards[heroIndex] += shards;
        selectedHeroIndex = heroIndex;
        damage = GetTeamDamage();
        var hero = GetHeroDefinition(heroIndex);

        var message = $"{hero.rarityName} pull: {hero.name}\n+{shards} shards";
        SetSummonResult(message);
        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "summon_pull", string.Empty, message);
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
        ExecuteCampaignFight();

        if (saveProgress)
        {
            SaveProgress();
        }

        RefreshUi();
    }

    private MythwakeActionResultDto ExecuteCampaignFight()
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
            var winMessage = $"Campaign Stage {clearedStage} cleared in {result.rounds} rounds\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}{milestoneText}";
            SetDungeonResult(winMessage);
            return CreateActionResult(true, "campaign_fight", string.Empty, winMessage);
        }
        else
        {
            enemyMaxHp = stage.maxHp;
            enemyHp = enemyMaxHp;
            var failMessage = $"Campaign Stage {enemyLevel} failed after {result.rounds} rounds\nEnemy HP {result.enemyHpRemaining}/{stage.maxHp}  {FormatCombatResult(result)}";
            SetDungeonResult(failMessage);
            return CreateActionResult(false, "campaign_fight", "combat_lost", failMessage);
        }
    }

    private void LoadProgress()
    {
        if (TryLoadSaveJson())
        {
            return;
        }

        LoadLegacyProgress();
    }

    private bool TryLoadSaveJson()
    {
        var json = PlayerPrefs.GetString(SaveJsonKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var data = JsonUtility.FromJson<PrototypeSaveData>(json);
            if (data == null || data.saveVersion <= 0)
            {
                return false;
            }

            ApplySaveData(data);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private void LoadLegacyProgress()
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
        lastSeenUtcTicks = PlayerPrefs.GetString(LastSeenUtcKey, string.Empty);
        LoadDailyProgress();
        NormalizeLoadedState();
    }

    private void SaveProgress()
    {
        NormalizeLoadedState();
        saveVersion = CurrentSaveVersion;
        lastSeenUtcTicks = DateTime.UtcNow.Ticks.ToString();
        var saveData = CreateSaveData();

        PlayerPrefs.SetInt(SaveVersionKey, saveVersion);
        PlayerPrefs.SetString(SaveJsonKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
    }

    private PrototypeSaveData CreateSaveData()
    {
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();
        EnsureAccessories();
        EnsureDailyMissionClaims();
        EnsureBattlePassRewardClaims();

        return new PrototypeSaveData
        {
            saveVersion = CurrentSaveVersion,
            gold = gold,
            gems = gems,
            mythEssence = mythEssence,
            damage = damage,
            enemyLevel = enemyLevel,
            enemyHp = enemyHp,
            enemyMaxHp = enemyMaxHp,
            upgradeCost = upgradeCost,
            goldDungeonFloor = goldDungeonFloor,
            essenceDungeonFloor = essenceDungeonFloor,
            gearDungeonFloor = gearDungeonFloor,
            weaponLevel = weaponLevel,
            armorLevel = armorLevel,
            selectedAccessorySlot = selectedAccessorySlot,
            selectedAccessoryRarity = selectedAccessoryRarity,
            selectedHeroIndex = selectedHeroIndex,
            summonCount = summonCount,
            dailyDate = GetDailyDateKey(),
            dailyFightCount = dailyFightCount,
            dailyStageClearCount = dailyStageClearCount,
            dailySummonCount = dailySummonCount,
            battlePassXp = battlePassXp,
            lastSeenUtcTicks = lastSeenUtcTicks,
            heroLevels = CopyIntArray(heroLevels, HeroCount, 1),
            heroShards = CopyIntArray(heroShards, HeroCount, 0),
            heroAscensions = CopyIntArray(heroAscensions, HeroCount, 0),
            equippedAccessoryRarities = CopyIntArray(equippedAccessoryRarities, AccessorySlotCount, -1),
            equippedAccessoryLevels = CopyIntArray(equippedAccessoryLevels, AccessorySlotCount, 0),
            accessoryInventory = CopyIntArray(accessoryInventory, AccessorySlotCount * AccessoryRarityCount, 0),
            dailyMissionClaimed = CopyBoolArray(dailyMissionClaimed, DailyMissionCount),
            battlePassRewardsClaimed = CopyBoolArray(battlePassRewardsClaimed, BattlePassRewardCount)
        };
    }

    private void ApplySaveData(PrototypeSaveData data)
    {
        saveVersion = Mathf.Max(1, data.saveVersion);
        gold = Mathf.Max(0, data.gold);
        gems = Mathf.Max(0, data.gems);
        mythEssence = Mathf.Max(0, data.mythEssence);
        damage = Mathf.Max(1, data.damage);
        enemyLevel = Mathf.Max(1, data.enemyLevel);
        enemyMaxHp = Mathf.Max(GetStageMaxHp(enemyLevel), data.enemyMaxHp);
        enemyHp = Mathf.Clamp(data.enemyHp, 1, enemyMaxHp);
        upgradeCost = Mathf.Max(1, data.upgradeCost);
        goldDungeonFloor = Mathf.Max(1, data.goldDungeonFloor);
        essenceDungeonFloor = Mathf.Max(1, data.essenceDungeonFloor);
        gearDungeonFloor = Mathf.Max(1, data.gearDungeonFloor);
        weaponLevel = Mathf.Max(StarterEquipmentLevel, data.weaponLevel);
        armorLevel = Mathf.Max(StarterEquipmentLevel, data.armorLevel);
        selectedAccessorySlot = Mathf.Clamp(data.selectedAccessorySlot, 0, AccessorySlotCount - 1);
        selectedAccessoryRarity = Mathf.Clamp(data.selectedAccessoryRarity, 0, AccessoryRarityCount - 1);
        selectedHeroIndex = Mathf.Clamp(data.selectedHeroIndex, 0, HeroCount - 1);
        summonCount = Mathf.Max(0, data.summonCount);
        battlePassXp = Mathf.Max(0, data.battlePassXp);
        lastSeenUtcTicks = data.lastSeenUtcTicks ?? string.Empty;
        heroLevels = CopyIntArray(data.heroLevels, HeroCount, 1);
        heroShards = CopyIntArray(data.heroShards, HeroCount, 0);
        heroAscensions = CopyIntArray(data.heroAscensions, HeroCount, 0);
        equippedAccessoryRarities = CopyIntArray(data.equippedAccessoryRarities, AccessorySlotCount, -1);
        equippedAccessoryLevels = CopyIntArray(data.equippedAccessoryLevels, AccessorySlotCount, 0);
        accessoryInventory = CopyIntArray(data.accessoryInventory, AccessorySlotCount * AccessoryRarityCount, 0);
        dailyMissionClaimed = CopyBoolArray(data.dailyMissionClaimed, DailyMissionCount);
        battlePassRewardsClaimed = CopyBoolArray(data.battlePassRewardsClaimed, BattlePassRewardCount);

        if (data.dailyDate == GetDailyDateKey())
        {
            dailyFightCount = Mathf.Max(0, data.dailyFightCount);
            dailyStageClearCount = Mathf.Max(0, data.dailyStageClearCount);
            dailySummonCount = Mathf.Max(0, data.dailySummonCount);
        }
        else
        {
            dailyFightCount = 0;
            dailyStageClearCount = 0;
            dailySummonCount = 0;
            dailyMissionClaimed = new bool[DailyMissionCount];
        }

        NormalizeLoadedState();
    }

    private void NormalizeLoadedState()
    {
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();
        EnsureAccessories();
        EnsureDailyMissionClaims();
        EnsureBattlePassRewardClaims();

        gold = Mathf.Max(0, gold);
        gems = Mathf.Max(0, gems);
        mythEssence = Mathf.Max(0, mythEssence);
        enemyLevel = Mathf.Max(1, enemyLevel);
        enemyMaxHp = Mathf.Max(GetStageMaxHp(enemyLevel), enemyMaxHp);
        enemyHp = Mathf.Clamp(enemyHp, 1, enemyMaxHp);
        goldDungeonFloor = Mathf.Max(1, goldDungeonFloor);
        essenceDungeonFloor = Mathf.Max(1, essenceDungeonFloor);
        gearDungeonFloor = Mathf.Max(1, gearDungeonFloor);
        weaponLevel = Mathf.Max(StarterEquipmentLevel, weaponLevel);
        armorLevel = Mathf.Max(StarterEquipmentLevel, armorLevel);
        selectedAccessorySlot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        selectedAccessoryRarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);
        selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        summonCount = Mathf.Max(0, summonCount);
        battlePassXp = Mathf.Max(0, battlePassXp);
        damage = GetTeamDamage();
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);
    }

    private static int[] CopyIntArray(int[] source, int length, int defaultValue)
    {
        var target = new int[length];
        for (var i = 0; i < target.Length; i++)
        {
            target[i] = source != null && i < source.Length ? source[i] : defaultValue;
        }

        return target;
    }

    private static bool[] CopyBoolArray(bool[] source, int length)
    {
        var target = new bool[length];
        for (var i = 0; i < target.Length; i++)
        {
            target[i] = source != null && i < source.Length && source[i];
        }

        return target;
    }

    private void ClaimOfflineRewards()
    {
        lastOfflineReward = 0;
        lastOfflineSeconds = 0;

        if (!long.TryParse(lastSeenUtcTicks, out var ticks))
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
        GrantCurrency(GoldCurrencyId, offlineGoldReward);
        GrantCurrency(MythEssenceCurrencyId, lastOfflineReward);
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
        var hp = Mathf.CeilToInt(lastStage.maxHp * Mathf.Pow(CampaignBalance.overflowHpGrowth, overflow));
        var reward = Mathf.CeilToInt(lastStage.essenceReward * Mathf.Pow(CampaignBalance.overflowRewardGrowth, overflow));

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
        return CampaignBalance.baseRecommendedPower + Mathf.FloorToInt(CampaignBalance.recommendedPowerScale * Mathf.Pow(stage, CampaignBalance.recommendedPowerGrowth));
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

    private AccessoryDefinition RollAccessoryDrop(int floor)
    {
        floor = Mathf.Max(1, floor);
        var totalWeight = 0;

        for (var i = 0; i < AccessoryDefinitions.Length; i++)
        {
            totalWeight += GetAccessoryDropWeight(AccessoryDefinitions[i], floor);
        }

        if (totalWeight <= 0)
        {
            return AccessoryDefinitions[0];
        }

        var roll = UnityEngine.Random.Range(0, totalWeight);
        var cumulativeWeight = 0;

        for (var i = 0; i < AccessoryDefinitions.Length; i++)
        {
            cumulativeWeight += GetAccessoryDropWeight(AccessoryDefinitions[i], floor);
            if (roll < cumulativeWeight)
            {
                return AccessoryDefinitions[i];
            }
        }

        return AccessoryDefinitions[AccessoryDefinitions.Length - 1];
    }

    private MythwakeActionResultDto ExecuteResourceDungeon(bool isGoldDungeon)
    {
        var dungeon = isGoldDungeon ? GoldDungeonDefinition : EssenceDungeonDefinition;
        var floor = isGoldDungeon ? goldDungeonFloor : essenceDungeonFloor;
        var enemyHp = GetDungeonEnemyHp(dungeon, floor);
        var enemyDamage = GetDungeonEnemyDamage(dungeon, floor);
        var result = SimulateCombat(enemyHp, enemyDamage);

        if (!result.won)
        {
            var failMessage = $"{dungeon.displayName} Floor {floor} failed after {result.rounds} rounds\nEnemy HP {result.enemyHpRemaining}/{enemyHp}  {FormatCombatResult(result)}";
            SetDungeonResult(failMessage);
            RefreshUi();
            return CreateActionResult(false, $"{dungeon.dungeonId}_run", "combat_lost", failMessage);
        }

        var reward = GetDungeonReward(dungeon, floor);
        var bonusReward = GetDungeonBonusReward(isGoldDungeon, floor);
        GrantReward(bonusReward);
        var bonusText = FormatDungeonBonusReward(bonusReward);
        MythwakeRewardDto rewardDto;
        string message;
        if (isGoldDungeon)
        {
            GrantCurrency(GoldCurrencyId, reward);
            goldDungeonFloor++;
            rewardDto = new MythwakeRewardDto { rewardId = $"reward_{dungeon.dungeonId}_floor_{floor}", gold = reward + bonusReward.gold };
            message = $"{dungeon.displayName} Floor {floor} cleared in {result.rounds} rounds (+{reward} Gold)\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}{bonusText}";
        }
        else
        {
            GrantCurrency(MythEssenceCurrencyId, reward);
            essenceDungeonFloor++;
            rewardDto = new MythwakeRewardDto { rewardId = $"reward_{dungeon.dungeonId}_floor_{floor}", mythEssence = reward + bonusReward.mythEssence };
            message = $"{dungeon.displayName} Floor {floor} cleared in {result.rounds} rounds (+{reward} Essence)\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}{bonusText}";
        }

        SetDungeonResult(message);
        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, $"{dungeon.dungeonId}_run", string.Empty, message, rewardDto);
    }

    private int GetCampaignEnemyDamage(int stage)
    {
        stage = Mathf.Max(1, stage);
        return CampaignBalance.baseEnemyDamage + Mathf.FloorToInt(CampaignBalance.enemyDamageScale * Mathf.Pow(stage, CampaignBalance.enemyDamageGrowth));
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

        var rewardGems = CampaignBalance.milestoneGemBase + Mathf.FloorToInt(clearedStage * CampaignBalance.milestoneGemScale);
        var rewardPassXp = CampaignBalance.milestonePassXp;
        var reward = new RewardDefinition($"reward_campaign_milestone_{clearedStage}", 0, rewardGems, 0, rewardPassXp);
        GrantReward(reward);

        return $"  Milestone +{rewardGems} Gems +{rewardPassXp} XP";
    }

    private RewardDefinition GetDungeonBonusReward(bool isGoldDungeon, int clearedFloor)
    {
        if (clearedFloor <= 0 || clearedFloor % DungeonBonusInterval != 0)
        {
            return new RewardDefinition(string.Empty, 0, 0, 0);
        }

        if (isGoldDungeon)
        {
            var bonusGold = Mathf.CeilToInt(GetGoldDungeonReward(clearedFloor) * 0.75f);
            return new RewardDefinition($"reward_gold_dungeon_bonus_{clearedFloor}", bonusGold, 0, 0);
        }

        var bonusEssence = Mathf.CeilToInt(GetEssenceDungeonReward(clearedFloor) * 0.75f);
        return new RewardDefinition($"reward_essence_dungeon_bonus_{clearedFloor}", 0, 0, bonusEssence);
    }

    private string FormatDungeonBonusReward(RewardDefinition reward)
    {
        if (reward.gold > 0)
        {
            return $"  Bonus +{reward.gold} Gold";
        }

        if (reward.mythEssence > 0)
        {
            return $"  Bonus +{reward.mythEssence} Essence";
        }

        return string.Empty;
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
            var accessory = GetAccessoryDefinition(slot, rarity);
            var nextTier = string.IsNullOrEmpty(accessory.fuseTargetAccessoryId)
                ? "Max"
                : GetAccessoryRarityName(GetAccessoryDefinitionById(accessory.fuseTargetAccessoryId).rarityIndex);
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
            var accessory = GetAccessoryDefinition(slot, rarity);
            accessoryFuseButton.interactable = !string.IsNullOrEmpty(accessory.fuseTargetAccessoryId) && GetAccessoryInventoryCount(slot, rarity) >= AccessoryFuseCost;
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

    private static AccessoryDefinition GetAccessoryDefinition(int slot, int rarity)
    {
        return AccessoryDefinitions[GetAccessoryDefinitionIndex(slot, rarity)];
    }

    private static AccessoryDefinition GetAccessoryDefinitionById(string accessoryId)
    {
        return TryGetAccessoryDefinitionById(accessoryId, out var definition) ? definition : AccessoryDefinitions[0];
    }

    private static bool TryGetAccessoryDefinitionById(string accessoryId, out AccessoryDefinition definition)
    {
        for (var i = 0; i < AccessoryDefinitions.Length; i++)
        {
            if (AccessoryDefinitions[i].accessoryId == accessoryId)
            {
                definition = AccessoryDefinitions[i];
                return true;
            }
        }

        definition = AccessoryDefinitions[0];
        return false;
    }

    private static int GetAccessoryDefinitionIndex(int slot, int rarity)
    {
        slot = Mathf.Clamp(slot, 0, AccessorySlotCount - 1);
        rarity = Mathf.Clamp(rarity, 0, AccessoryRarityCount - 1);
        return (slot * AccessoryRarityCount) + rarity;
    }

    private static string GetAccessoryRarityName(int rarity)
    {
        return GetAccessoryRarityDefinition(rarity).displayName;
    }

    private static AccessoryDefinition[] CreateAccessoryDefinitions()
    {
        var definitions = new AccessoryDefinition[AccessorySlotCount * AccessoryRarityCount];

        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            for (var rarity = 0; rarity < AccessoryRarityCount; rarity++)
            {
                var slotDefinition = AccessorySlots[slot];
                var rarityDefinition = AccessoryRarities[rarity];
                var fuseTargetAccessoryId = rarity >= AccessoryRarityCount - 1
                    ? string.Empty
                    : CreateAccessoryId(slotDefinition, AccessoryRarities[rarity + 1]);

                definitions[GetAccessoryDefinitionIndex(slot, rarity)] = new AccessoryDefinition(
                    CreateAccessoryId(slotDefinition, rarityDefinition),
                    slotDefinition.itemSlotId,
                    rarityDefinition.rarityId,
                    slot,
                    rarity,
                    rarityDefinition.maxLevel,
                    slotDefinition.attackPerLevel * rarityDefinition.statMultiplier,
                    slotDefinition.healthPerLevel * rarityDefinition.statMultiplier,
                    GetAccessoryBaseDropWeight(rarity),
                    fuseTargetAccessoryId);
            }
        }

        return definitions;
    }

    private static string CreateAccessoryId(AccessorySlotDefinition slot, AccessoryRarityDefinition rarity)
    {
        return $"{slot.itemSlotId.Replace("item_slot_", "accessory_")}_{rarity.displayName.ToLowerInvariant()}";
    }

    private static int GetAccessoryBaseDropWeight(int rarity)
    {
        switch (Mathf.Clamp(rarity, 0, AccessoryRarityCount - 1))
        {
            case 0:
                return 455;
            case 1:
                return 335;
            case 2:
                return 160;
            case 3:
                return 45;
            default:
                return 5;
        }
    }

    private static int GetAccessoryDropWeight(AccessoryDefinition definition, int floor)
    {
        floor = Mathf.Max(1, floor);
        var tier = Mathf.Clamp(definition.rarityIndex, 0, AccessoryRarityCount - 1);

        switch (tier)
        {
            case 0:
                return Mathf.Max(95, definition.dropWeight - Mathf.Min(230, floor * 7));
            case 1:
                return definition.dropWeight + Mathf.Min(90, floor * 2);
            case 2:
                return definition.dropWeight + Mathf.Min(120, floor * 4);
            case 3:
                return definition.dropWeight + Mathf.Min(150, floor * 5);
            default:
                return definition.dropWeight + Mathf.Min(35, floor);
        }
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
        GrantCurrency(GoldCurrencyId, reward.gold);
        GrantCurrency(GemsCurrencyId, reward.gems);
        GrantCurrency(MythEssenceCurrencyId, reward.mythEssence);
        GrantCurrency(PassXpCurrencyId, reward.passXp);
    }

    private bool TrySpendCurrency(string currencyId, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount == 0)
        {
            return true;
        }

        var currentAmount = GetCurrencyAmount(currencyId);
        if (currentAmount < amount)
        {
            return false;
        }

        SetCurrencyAmount(currencyId, currentAmount - amount);
        return true;
    }

    bool IMythwakeEconomyService.TrySpendCurrency(string currencyId, int amount)
    {
        return TrySpendCurrency(currencyId, amount);
    }

    private void GrantCurrency(string currencyId, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0)
        {
            return;
        }

        SetCurrencyAmount(currencyId, GetCurrencyAmount(currencyId) + amount);
    }

    void IMythwakeEconomyService.GrantCurrency(string currencyId, int amount)
    {
        GrantCurrency(currencyId, amount);
    }

    MythwakeRewardDto IMythwakeEconomyService.GrantReward(MythwakeRewardDto reward)
    {
        GrantReward(new RewardDefinition(reward.rewardId, reward.gold, reward.gems, reward.mythEssence, reward.passXp));
        return reward;
    }

    private int GetCurrencyAmount(string currencyId)
    {
        switch (currencyId)
        {
            case GoldCurrencyId:
                return gold;
            case GemsCurrencyId:
                return gems;
            case MythEssenceCurrencyId:
                return mythEssence;
            case PassXpCurrencyId:
                return battlePassXp;
            default:
                return 0;
        }
    }

    private void SetCurrencyAmount(string currencyId, int amount)
    {
        amount = Mathf.Max(0, amount);
        switch (currencyId)
        {
            case GoldCurrencyId:
                gold = amount;
                break;
            case GemsCurrencyId:
                gems = amount;
                break;
            case MythEssenceCurrencyId:
                mythEssence = amount;
                break;
            case PassXpCurrencyId:
                battlePassXp = amount;
                break;
        }
    }

    public MythwakePlayerStateDto GetPlayerState()
    {
        NormalizeLoadedState();
        return new MythwakePlayerStateDto
        {
            saveVersion = saveVersion,
            gold = gold,
            gems = gems,
            mythEssence = mythEssence,
            passXp = battlePassXp,
            campaignStage = enemyLevel,
            goldDungeonFloor = goldDungeonFloor,
            essenceDungeonFloor = essenceDungeonFloor,
            gearDungeonFloor = gearDungeonFloor,
            teamPower = GetTeamPower(),
            teamAttack = GetTeamDamage(),
            teamHealth = GetTeamHealth()
        };
    }

    private MythwakeActionResultDto CreateActionResult(bool success, string actionId, string errorCode, string message)
    {
        return CreateActionResult(success, actionId, errorCode, message, new MythwakeRewardDto());
    }

    private MythwakeActionResultDto CreateActionResult(bool success, string actionId, string errorCode, string message, MythwakeRewardDto reward)
    {
        return new MythwakeActionResultDto
        {
            success = success,
            actionId = actionId,
            errorCode = errorCode,
            message = message,
            playerState = GetPlayerState(),
            reward = reward
        };
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

        var definition = GetAccessoryDefinition(slot, rarity);
        return definition.attackPerLevel * level;
    }

    private int GetAccessoryHealthFor(int slot, int rarity, int level)
    {
        if (rarity < 0 || level <= 0)
        {
            return 0;
        }

        var definition = GetAccessoryDefinition(slot, rarity);
        return definition.healthPerLevel * level;
    }

    private int GetAccessoryMaxLevel(int rarity)
    {
        return GetAccessoryDefinition(0, rarity).levelCap;
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
        return GetAccessoryDefinitionIndex(slot, rarity);
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
        return Mathf.CeilToInt(14 * Mathf.Pow(1.34f, heroLevels[index] - 1));
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
        GrantCurrency(GoldCurrencyId, goldAmount);
        GrantCurrency(GemsCurrencyId, gemAmount);
        GrantCurrency(MythEssenceCurrencyId, essenceAmount);

        SaveProgress();
        RefreshUi();
        SetDungeonResult($"Debug: +{FormatReward(goldAmount, gemAmount, essenceAmount)}.");
    }

    private void ClearPrototypePlayerPrefs()
    {
        foreach (var key in SaveKeys)
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
