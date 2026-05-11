using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdlePrototypeController : MonoBehaviour, IMythwakePlayerStateService, IMythwakePlayerSnapshotService, IMythwakeDefinitionService, IMythwakeEconomyService, IMythwakeBattleService, IMythwakeSummonService, IMythwakeInventoryService, IMythwakeProgressionService, IMythwakeMissionService
{
    public const string PrototypeVersion = "0.2.34";
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
        public int elapsedSeconds;
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
        Dungeons,
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
    private const string BackendGameplayEnabledKey = "Mythwake.Backend.GameplayEnabled";
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
    private const int DefaultCombatDurationSeconds = 30;
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
    [NonSerialized] private int backendWeaponLevel;
    [NonSerialized] private int backendArmorLevel;
    [NonSerialized] private int backendTeamPower;
    [NonSerialized] private int backendTeamAttack;
    [NonSerialized] private int backendTeamHealth;
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

    [Header("Backend")]
    [SerializeField] private MythwakeBackendClient backendClient;
    [SerializeField] private bool backendGameplayEnabled;
    [SerializeField] private TMP_Text backendStatusText;
    [SerializeField] private Button backendHealthButton;
    [SerializeField] private Button backendLoginButton;
    [SerializeField] private Button backendSyncButton;
    [SerializeField] private Button backendAfkButton;
    [SerializeField] private Button backendClockButton;
    [SerializeField] private Button backendDefinitionsButton;
    [SerializeField] private Button backendSmokeButton;
    [SerializeField] private Button backendResetButton;
    [SerializeField] private Button backendModeButton;
    [SerializeField] private TMP_Text backendModeText;

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
    [SerializeField] private GameObject dungeonsPanel;
    [SerializeField] private GameObject heroesPanel;
    [SerializeField] private GameObject gearPanel;
    [SerializeField] private GameObject summonPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button homeTabButton;
    [SerializeField] private Button battleTabButton;
    [SerializeField] private Button dungeonsTabButton;
    [SerializeField] private Button heroesTabButton;
    [SerializeField] private Button gearTabButton;
    [SerializeField] private Button summonTabButton;
    [SerializeField] private Button shopTabButton;
    [SerializeField] private Color activeTabColor = new Color(0.22f, 0.48f, 0.86f);
    [SerializeField] private Color inactiveTabColor = new Color(0.11f, 0.14f, 0.2f);

    [Header("Home Navbar Art")]
    [SerializeField] private Texture2D homeNavbarTexture;
    [SerializeField] private Texture2D homeNavbarVillageTexture;
    [SerializeField] private Texture2D homeNavbarDungeonsTexture;
    [SerializeField] private Texture2D homeNavbarHeroesTexture;
    [SerializeField] private Texture2D homeNavbarSummonTexture;

    private float autoAttackTimer;
    private int lastOfflineGoldReward;
    private int lastOfflineReward;
    private int lastOfflineSeconds;
    private bool lastOfflineRewardIsServer;
    private AppScreen activeScreen = AppScreen.Home;
    private bool backendRequestInProgress;
    private bool backendLifecycleFlushInProgress;
    private string backendStatus = "Backend: local prototype mode";
    private long backendStateRevision;
    private MythwakeDefinitionSnapshotDto backendDefinitions;
    private bool hasBackendDefinitions;
    private MythwakeRuntimeArtPresenter runtimeArt;
    private TMP_Text dungeonsHeaderText;
    private TMP_Text runtimeDungeonResultText;
    private RectTransform topBarRoot;
    private RectTransform bottomNavRoot;
    private TMP_Text topProfileText;
    private TMP_Text topPowerText;
    private TMP_Text topCurrencyText;
    private RawImage topAvatarImage;
    private Button homeBeginButton;
    private TMP_Text menuHeaderText;
    private RawImage[] heroCardPortraits;
    private RectTransform artBottomNavRoot;
    private RawImage villageNavImage;
    private RawImage dungeonsNavImage;
    private RawImage heroesNavImage;
    private RawImage summonNavImage;
    private Button villageNavButton;
    private Button campaignNavButton;
    private Button dungeonsNavButton;
    private Button heroesNavButton;
    private Button summonNavButton;

    private void Awake()
    {
        LoadProgress();
        ClaimOfflineRewards();
        EnsureRuntimeBackendClient();
        LoadBackendGameplayPreference();
        EnsureRuntimeDebugUi();
        EnsureRuntimeBackendUi();
        EnsureRuntimeScreenLayout();
        EnsureRuntimeArtUi();
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

        if (backendHealthButton != null)
        {
            backendHealthButton.onClick.AddListener(PingBackend);
        }

        if (backendLoginButton != null)
        {
            backendLoginButton.onClick.AddListener(LoginBackend);
        }

        if (backendSyncButton != null)
        {
            backendSyncButton.onClick.AddListener(SyncBackendState);
        }

        if (backendAfkButton != null)
        {
            backendAfkButton.onClick.AddListener(ClaimBackendOfflineRewards);
        }

        if (backendClockButton != null)
        {
            backendClockButton.onClick.AddListener(SyncBackendClock);
        }

        if (backendDefinitionsButton != null)
        {
            backendDefinitionsButton.onClick.AddListener(SyncBackendDefinitions);
        }

        if (backendSmokeButton != null)
        {
            backendSmokeButton.onClick.AddListener(RunBackendSmokeTest);
        }

        if (backendResetButton != null)
        {
            backendResetButton.onClick.AddListener(ResetBackendPlayer);
        }

        if (backendModeButton != null)
        {
            backendModeButton.onClick.AddListener(ToggleBackendGameplayMode);
        }

        RefreshUi();
        ShowScreen(activeScreen);
        PreparePersistedBackendGameplayMode();
    }

    private void Update()
    {
        if (runtimeArt != null)
        {
            runtimeArt.Tick(Time.unscaledDeltaTime);
        }

        if (!autoAttackEnabled)
        {
            return;
        }

        if (backendGameplayEnabled)
        {
            RefreshAutoAttackUi();
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

        if (backendHealthButton != null)
        {
            backendHealthButton.onClick.RemoveListener(PingBackend);
        }

        if (backendLoginButton != null)
        {
            backendLoginButton.onClick.RemoveListener(LoginBackend);
        }

        if (backendSyncButton != null)
        {
            backendSyncButton.onClick.RemoveListener(SyncBackendState);
        }

        if (backendAfkButton != null)
        {
            backendAfkButton.onClick.RemoveListener(ClaimBackendOfflineRewards);
        }

        if (backendClockButton != null)
        {
            backendClockButton.onClick.RemoveListener(SyncBackendClock);
        }

        if (backendDefinitionsButton != null)
        {
            backendDefinitionsButton.onClick.RemoveListener(SyncBackendDefinitions);
        }

        if (backendSmokeButton != null)
        {
            backendSmokeButton.onClick.RemoveListener(RunBackendSmokeTest);
        }

        if (backendResetButton != null)
        {
            backendResetButton.onClick.RemoveListener(ResetBackendPlayer);
        }

        if (backendModeButton != null)
        {
            backendModeButton.onClick.RemoveListener(ToggleBackendGameplayMode);
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

    public void ShowDungeons()
    {
        ShowScreen(AppScreen.Dungeons);
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
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: campaign fight..."))
            {
                StartCoroutine(backendClient.FightCampaign(OnBackendGameplayAction));
            }

            return;
        }

        FightCampaign();
    }

    public void RunGoldDungeon()
    {
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: gold dungeon..."))
            {
                StartCoroutine(backendClient.RunDungeon(GoldDungeonDefinition.dungeonId, OnBackendGameplayAction));
            }

            return;
        }

        RunDungeon(GoldDungeonDefinition.dungeonId);
    }

    public void RunEssenceDungeon()
    {
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: essence dungeon..."))
            {
                StartCoroutine(backendClient.RunDungeon(EssenceDungeonDefinition.dungeonId, OnBackendGameplayAction));
            }

            return;
        }

        RunDungeon(EssenceDungeonDefinition.dungeonId);
    }

    public void RunGearDungeon()
    {
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: gear dungeon..."))
            {
                StartCoroutine(backendClient.RunDungeon(GearDungeonDefinition.dungeonId, OnBackendGameplayAction));
            }

            return;
        }

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
            var failMessage = $"Gear Dungeon Floor {floor} failed after {result.elapsedSeconds}s\nEnemy HP {result.enemyHpRemaining}/{enemyHp}  {FormatCombatResult(result)}";
            PlayCombatVisual(GearDungeonDefinition.dungeonId, $"Gear Dungeon F{floor}", result, enemyHp);
            SetDungeonResult(failMessage);
            RefreshUi();
            return CreateActionResult(false, "gear_dungeon_run", "combat_lost", failMessage);
        }

        var accessory = RollAccessoryDrop(floor);
        AddAccessoryInventory(accessory.slotIndex, accessory.rarityIndex, 1);
        gearDungeonFloor++;

        var message = $"Gear Dungeon Floor {floor} cleared in {result.elapsedSeconds}s\nDrop: {GetAccessoryRarityName(accessory.rarityIndex)} {AccessorySlots[accessory.slotIndex].name}  HP {result.teamHpRemaining}/{GetTeamHealth()}";
        PlayCombatVisual(GearDungeonDefinition.dungeonId, $"Gear Dungeon F{floor}", result, enemyHp);
        SetDungeonResult(message);
        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "gear_dungeon_run", string.Empty, message);
    }

    public void UpgradeDamage()
    {
        selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest($"Server: leveling {GetHeroDefinition(selectedHeroIndex).name}..."))
            {
                StartCoroutine(backendClient.LevelHero(GetHeroDefinition(selectedHeroIndex).heroId, OnBackendGameplayAction));
            }

            return;
        }

        LevelHero(GetHeroDefinition(selectedHeroIndex).heroId);
    }

    public void AscendSelectedHero()
    {
        selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest($"Server: ascending {GetHeroDefinition(selectedHeroIndex).name}..."))
            {
                StartCoroutine(backendClient.AscendHero(GetHeroDefinition(selectedHeroIndex).heroId, OnBackendGameplayAction));
            }

            return;
        }

        AscendHero(GetHeroDefinition(selectedHeroIndex).heroId);
    }

    public void UpgradeWeapon()
    {
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: leveling weapon..."))
            {
                StartCoroutine(backendClient.LevelEquipment(WeaponTrack.equipmentId, OnBackendGameplayAction));
            }

            return;
        }

        LevelEquipment(WeaponTrack.equipmentId);
    }

    public void UpgradeArmor()
    {
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: leveling armor..."))
            {
                StartCoroutine(backendClient.LevelEquipment(ArmorTrack.equipmentId, OnBackendGameplayAction));
            }

            return;
        }

        LevelEquipment(ArmorTrack.equipmentId);
    }

    public MythwakeActionResultDto LevelHero(string heroId)
    {
        if (!TryGetHeroIndexById(heroId, out var heroIndex))
        {
            var invalidResult = CreateActionResult(false, "hero_level", "invalid_hero", $"Unknown hero: {heroId}");
            RefreshUi();
            return invalidResult;
        }

        selectedHeroIndex = heroIndex;
        if (IsHeroLevelMax(heroIndex))
        {
            var maxResult = CreateActionResult(false, "hero_level", "max_level", $"{GetHeroDefinition(heroIndex).name} is already max level.");
            RefreshUi();
            return maxResult;
        }

        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);
        if (!TrySpendCurrency(MythEssenceCurrencyId, upgradeCost))
        {
            var failMessage = $"Need {upgradeCost} Essence to level {GetHeroDefinition(heroIndex).name}.";
            RefreshUi();
            return CreateActionResult(false, "hero_level", "insufficient_currency", failMessage);
        }

        heroLevels[selectedHeroIndex]++;
        damage = GetTeamDamage();
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);

        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "hero_level", string.Empty, $"{GetHeroDefinition(heroIndex).name} reached Lv. {heroLevels[heroIndex]}.");
    }

    public MythwakeActionResultDto AscendHero(string heroId)
    {
        if (!TryGetHeroIndexById(heroId, out var heroIndex))
        {
            var invalidResult = CreateActionResult(false, "hero_ascend", "invalid_hero", $"Unknown hero: {heroId}");
            RefreshUi();
            return invalidResult;
        }

        selectedHeroIndex = heroIndex;
        EnsureHeroShards();
        EnsureHeroAscensions();
        if (IsHeroAscensionMax(heroIndex))
        {
            var maxResult = CreateActionResult(false, "hero_ascend", "max_ascension", $"{GetHeroDefinition(heroIndex).name} is already max ascension.");
            RefreshUi();
            return maxResult;
        }

        var ascendCost = GetHeroAscensionCost(selectedHeroIndex);
        if (heroShards[selectedHeroIndex] < ascendCost)
        {
            var failMessage = $"Need {ascendCost} shards to ascend {GetHeroDefinition(heroIndex).name}.";
            RefreshUi();
            return CreateActionResult(false, "hero_ascend", "insufficient_shards", failMessage);
        }

        heroShards[selectedHeroIndex] -= ascendCost;
        heroAscensions[selectedHeroIndex]++;
        damage = GetTeamDamage();

        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "hero_ascend", string.Empty, $"{GetHeroDefinition(heroIndex).name} ascended to +{heroAscensions[heroIndex]}.");
    }

    public MythwakeActionResultDto LevelEquipment(string equipmentId)
    {
        if (equipmentId == WeaponTrack.equipmentId)
        {
            return LevelEquipmentTrack(WeaponTrack, isWeapon: true);
        }

        if (equipmentId == ArmorTrack.equipmentId)
        {
            return LevelEquipmentTrack(ArmorTrack, isWeapon: false);
        }

        var invalidResult = CreateActionResult(false, "equipment_level", "invalid_equipment", $"Unknown equipment: {equipmentId}");
        RefreshUi();
        return invalidResult;
    }

    private MythwakeActionResultDto LevelEquipmentTrack(EquipmentTrackDefinition track, bool isWeapon)
    {
        var currentLevel = isWeapon ? weaponLevel : armorLevel;
        currentLevel = Mathf.Max(StarterEquipmentLevel, currentLevel);
        if (IsEquipmentLevelMax(track, currentLevel))
        {
            var maxResult = CreateActionResult(false, "equipment_level", "max_level", $"{track.name} is already max level.");
            RefreshUi();
            return maxResult;
        }

        var cost = GetEquipmentUpgradeCost(track, currentLevel);

        if (!TrySpendCurrency(GoldCurrencyId, cost))
        {
            var failMessage = $"Need {cost} Gold to level {track.name}.";
            RefreshUi();
            return CreateActionResult(false, "equipment_level", "insufficient_currency", failMessage);
        }

        if (isWeapon)
        {
            weaponLevel = currentLevel + 1;
            damage = GetTeamDamage();
        }
        else
        {
            armorLevel = currentLevel + 1;
        }

        SaveProgress();
        RefreshUi();
        var newLevel = isWeapon ? weaponLevel : armorLevel;
        return CreateActionResult(true, "equipment_level", string.Empty, $"{track.name} reached Lv. {newLevel}.");
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
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: equipping accessory..."))
            {
                StartCoroutine(backendClient.EquipAccessory(GetAccessoryDefinition(slot, rarity).accessoryId, OnBackendGameplayAction));
            }

            return;
        }

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
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: leveling accessory..."))
            {
                StartCoroutine(backendClient.LevelAccessory(accessoryId, OnBackendGameplayAction));
            }

            return;
        }

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
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: fusing accessory..."))
            {
                StartCoroutine(backendClient.FuseAccessory(GetAccessoryDefinition(slot, rarity).accessoryId, OnBackendGameplayAction));
            }

            return;
        }

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
        var fuseCost = GetAccessoryFuseCost(rarity);

        if (string.IsNullOrEmpty(accessory.fuseTargetAccessoryId) || GetAccessoryInventoryCount(slot, rarity) < fuseCost)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_fuse", "missing_items", $"Need {fuseCost}x {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} to fuse.");
        }

        var fuseTarget = GetAccessoryDefinitionById(accessory.fuseTargetAccessoryId);
        AddAccessoryInventory(slot, rarity, -fuseCost);
        AddAccessoryInventory(fuseTarget.slotIndex, fuseTarget.rarityIndex, 1);
        selectedAccessoryRarity = fuseTarget.rarityIndex;

        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "accessory_fuse", string.Empty, $"Fused into {GetAccessoryRarityName(fuseTarget.rarityIndex)} {AccessorySlots[fuseTarget.slotIndex].name}.");
    }

    public void SummonOnce()
    {
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: summon pull..."))
            {
                StartCoroutine(backendClient.PullSummon(HeroShardBanner.bannerId, OnBackendSummonAction));
            }

            return;
        }

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
        PlaySummonVisual(heroIndex, $"{hero.rarityName} {hero.name}");
        SetSummonResult(message);
        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "summon_pull", string.Empty, message);
    }

    public void ClaimDailyBattleMission()
    {
        if (backendGameplayEnabled)
        {
            ClaimDailyMissionOnBackend(0);
            return;
        }

        ClaimDailyMission(0);
    }

    public void ClaimDailyStageMission()
    {
        if (backendGameplayEnabled)
        {
            ClaimDailyMissionOnBackend(1);
            return;
        }

        ClaimDailyMission(1);
    }

    public void ClaimDailySummonMission()
    {
        if (backendGameplayEnabled)
        {
            ClaimDailyMissionOnBackend(2);
            return;
        }

        ClaimDailyMission(2);
    }

    public void ClaimBattlePassReward1()
    {
        if (backendGameplayEnabled)
        {
            ClaimBattlePassRewardOnBackend(0);
            return;
        }

        ClaimBattlePassReward(0);
    }

    public void ClaimBattlePassReward2()
    {
        if (backendGameplayEnabled)
        {
            ClaimBattlePassRewardOnBackend(1);
            return;
        }

        ClaimBattlePassReward(1);
    }

    public void ClaimBattlePassReward3()
    {
        if (backendGameplayEnabled)
        {
            ClaimBattlePassRewardOnBackend(2);
            return;
        }

        ClaimBattlePassReward(2);
    }

    public void ClaimBattlePassReward4()
    {
        if (backendGameplayEnabled)
        {
            ClaimBattlePassRewardOnBackend(3);
            return;
        }

        ClaimBattlePassReward(3);
    }

    public void ClaimBattlePassReward5()
    {
        if (backendGameplayEnabled)
        {
            ClaimBattlePassRewardOnBackend(4);
            return;
        }

        ClaimBattlePassReward(4);
    }

    public void AddDebugGold()
    {
        if (RejectDebugActionInBackendMode())
        {
            return;
        }

        AddDebugResources(DebugGoldAmount, 0, 0);
    }

    public void AddDebugEssence()
    {
        if (RejectDebugActionInBackendMode())
        {
            return;
        }

        AddDebugResources(0, 0, DebugEssenceAmount);
    }

    public void AddDebugGems()
    {
        if (RejectDebugActionInBackendMode())
        {
            return;
        }

        AddDebugResources(0, DebugGemAmount, 0);
    }

    public void AddDebugAccessoryCopy()
    {
        if (RejectDebugActionInBackendMode())
        {
            return;
        }

        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);
        AddAccessoryInventory(slot, rarity, 1);

        SaveProgress();
        RefreshUi();
        SetDungeonResult($"Debug: +1 {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} copy.");
    }

    public void PingBackend()
    {
        if (!TryStartBackendRequest("Backend: pinging..."))
        {
            return;
        }

        StartCoroutine(PingBackendRoutine());
    }

    private IEnumerator PingBackendRoutine()
    {
        var healthSuccess = false;
        var healthError = string.Empty;
        var health = default(MythwakeHealthDto);
        yield return backendClient.GetHealth((success, error, response) =>
        {
            healthSuccess = success;
            healthError = error;
            health = response;
        });

        if (!healthSuccess)
        {
            FinishBackendRequest($"Backend offline: {healthError}");
            yield break;
        }

        var clockSuccess = false;
        var clock = default(MythwakeServerClockDto);
        yield return backendClient.GetServerClock((success, _, response) =>
        {
            clockSuccess = success;
            clock = response;
        });

        var healthHeadline = FormatBackendHealthHeadline(health);
        var cacheSummary = FormatBackendCacheSummary(health);
        if (clockSuccess)
        {
            FinishBackendRequest($"Backend: {health.status}  {healthHeadline}  {cacheSummary}  v{health.version}  Daily {FormatResetCountdown(clock.secondsUntilDailyReset)}");
            yield break;
        }

        FinishBackendRequest($"Backend: {health.status}  {healthHeadline}  {cacheSummary}  v{health.version}");
    }

    public void LoginBackend()
    {
        if (!TryStartBackendRequest("Backend: guest login..."))
        {
            return;
        }

        StartCoroutine(backendClient.GuestAuth(OnBackendLogin));
    }

    public void SyncBackendState()
    {
        if (!TryStartBackendRequest("Backend: syncing player snapshot..."))
        {
            return;
        }

        StartCoroutine(backendClient.GetPlayerSnapshot(OnBackendSnapshot));
    }

    public void ResetBackendPlayer()
    {
        if (!TryStartBackendRequest("Backend: resetting dev player..."))
        {
            return;
        }

        StartCoroutine(backendClient.ResetDevPlayer(OnBackendReset));
    }

    public void SyncBackendDefinitions()
    {
        if (!TryStartBackendRequest("Backend: syncing definitions..."))
        {
            return;
        }

        StartCoroutine(backendClient.GetDefinitions(OnBackendDefinitions));
    }

    public void SyncBackendClock()
    {
        if (!TryStartBackendRequest("Backend: syncing clock..."))
        {
            return;
        }

        StartCoroutine(backendClient.GetServerClock(OnBackendClock));
    }

    public void ClaimBackendOfflineRewards()
    {
        ClaimBackendOfflineRewards("manual");
    }

    private void ClaimBackendOfflineRewards(string reason)
    {
        var status = reason == "resume" ? "Server: checking AFK rewards..." : "Server: claiming AFK rewards...";
        if (!TryStartBackendRequest(status))
        {
            return;
        }

        StartCoroutine(backendClient.ClaimOfflineRewards(OnBackendOfflineClaim));
    }

    public void RunBackendSmokeTest()
    {
        SetBackendGameplayEnabled(true);
        if (!TryStartBackendRequest("Server smoke: bootstrapping..."))
        {
            return;
        }

        StartCoroutine(BackendSmokeTestRoutine());
    }

    private IEnumerator BackendSmokeTestRoutine()
    {
        var summary = "Server smoke";
        var bootstrapSuccess = false;
        var bootstrapError = string.Empty;
        var bootstrap = default(MythwakeClientBootstrapDto);
        yield return backendClient.GetClientBootstrap((success, error, response) =>
        {
            bootstrapSuccess = success;
            bootstrapError = error;
            bootstrap = response;
        });

        if (!bootstrapSuccess)
        {
            SetBackendGameplayEnabled(false);
            var failed = $"Server smoke bootstrap failed: {bootstrapError}";
            SetDungeonResult(failed);
            FinishBackendRequest(failed);
            yield break;
        }

        backendDefinitions = bootstrap.definitions;
        hasBackendDefinitions = !string.IsNullOrWhiteSpace(bootstrap.definitions.contentHash);
        ApplyBackendSnapshot(bootstrap.playerSnapshot);
        summary = $"{summary}\nBootstrap: Stage {enemyLevel}  Rev {backendStateRevision}  Defs {ShortHash(backendDefinitions.contentHash)}";

        var transportFailed = false;
        yield return RunBackendSmokeAction("Campaign", callback => backendClient.FightCampaign(callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during Campaign");
            yield break;
        }

        yield return RunBackendSmokeAction("Gold Dungeon", callback => backendClient.RunDungeon(GoldDungeonDefinition.dungeonId, callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during Gold Dungeon");
            yield break;
        }

        yield return RunBackendSmokeAction("Essence Dungeon", callback => backendClient.RunDungeon(EssenceDungeonDefinition.dungeonId, callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during Essence Dungeon");
            yield break;
        }

        yield return RunBackendSmokeAction("Gear Dungeon", callback => backendClient.RunDungeon(GearDungeonDefinition.dungeonId, callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during Gear Dungeon");
            yield break;
        }

        if (TryGetFirstOwnedAccessoryId(out var smokeAccessoryId))
        {
            yield return RunBackendSmokeAction("Accessory Equip", callback => backendClient.EquipAccessory(smokeAccessoryId, callback), (ok, line) =>
            {
                summary = $"{summary}\n{line}";
                transportFailed = !ok;
            });
            if (transportFailed)
            {
                SetDungeonResult(summary);
                FinishBackendRequest("Server smoke failed during Accessory Equip");
                yield break;
            }

            yield return RunBackendSmokeAction("Accessory Level", callback => backendClient.LevelAccessory(smokeAccessoryId, callback), (ok, line) =>
            {
                summary = $"{summary}\n{line}";
                transportFailed = !ok;
            });
            if (transportFailed)
            {
                SetDungeonResult(summary);
                FinishBackendRequest("Server smoke failed during Accessory Level");
                yield break;
            }
        }
        else
        {
            summary = $"{summary}\nAccessory Equip: skipped no copy";
        }

        if (TryGetFuseCandidateAccessoryId(out var smokeFuseAccessoryId))
        {
            yield return RunBackendSmokeAction("Accessory Fuse", callback => backendClient.FuseAccessory(smokeFuseAccessoryId, callback), (ok, line) =>
            {
                summary = $"{summary}\n{line}";
                transportFailed = !ok;
            });
            if (transportFailed)
            {
                SetDungeonResult(summary);
                FinishBackendRequest("Server smoke failed during Accessory Fuse");
                yield break;
            }
        }
        else
        {
            summary = $"{summary}\nAccessory Fuse: skipped need 3 copies";
        }

        yield return RunBackendSmokeAction("Hero Level", callback => backendClient.LevelHero(GetHeroDefinition(selectedHeroIndex).heroId, callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during Hero Level");
            yield break;
        }

        yield return RunBackendSmokeAction("Weapon Level", callback => backendClient.LevelEquipment(WeaponTrack.equipmentId, callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during Weapon Level");
            yield break;
        }

        yield return RunBackendSmokeAction("Summon", callback => backendClient.PullSummon(HeroShardBanner.bannerId, callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during Summon");
            yield break;
        }

        yield return RunBackendSmokeAction("Daily Summon Claim", callback => backendClient.ClaimDailyMission(GetBackendDailyMissionId(2), callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during Daily Claim");
            yield break;
        }

        yield return RunBackendSmokeAction("Mission Track Claim", callback => backendClient.ClaimBattlePassReward(GetBackendBattlePassRewardId(0), callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during Mission Track Claim");
            yield break;
        }

        yield return RunBackendSmokeAction("AFK Claim", callback => backendClient.ClaimOfflineRewards(callback), (ok, line) =>
        {
            summary = $"{summary}\n{line}";
            transportFailed = !ok;
        });
        if (transportFailed)
        {
            SetDungeonResult(summary);
            FinishBackendRequest("Server smoke failed during AFK Claim");
            yield break;
        }

        var flushSuccess = false;
        var flushError = string.Empty;
        yield return backendClient.FlushPlayerState((success, error) =>
        {
            flushSuccess = success;
            flushError = error;
        });

        summary = flushSuccess ? $"{summary}\nFlush: ok" : $"{summary}\nFlush failed: {flushError}";
        SetDungeonResult(summary);
        FinishBackendRequest(flushSuccess ? "Server smoke complete" : $"Server smoke flush failed: {flushError}");
    }

    private IEnumerator RunBackendSmokeAction(string label, Func<Action<bool, string, MythwakeActionResultDto>, IEnumerator> action, Action<bool, string> completed)
    {
        var success = false;
        var error = string.Empty;
        var result = default(MythwakeActionResultDto);
        yield return action((actionSuccess, actionError, actionResult) =>
        {
            success = actionSuccess;
            error = actionError;
            result = actionResult;
        });

        if (!success)
        {
            completed?.Invoke(false, $"{label}: transport failed  {error}");
            yield break;
        }

        if (result.playerSnapshot.state.campaignStage > 0)
        {
            ApplyBackendSnapshot(result.playerSnapshot);
        }

        var outcome = FormatBackendActionOutcome(result);
        var revision = FormatBackendRevisionSuffix(result).Trim();
        var reward = FormatServerReward(result.reward);
        var note = !result.success && !string.IsNullOrWhiteSpace(result.message) ? $"  {result.message}" : string.Empty;
        var rewardSuffix = string.IsNullOrWhiteSpace(reward) ? string.Empty : $"  {reward}";
        completed?.Invoke(true, string.IsNullOrWhiteSpace(revision) ? $"{label}: {outcome}{rewardSuffix}{note}" : $"{label}: {outcome}  {revision}{rewardSuffix}{note}");
    }

    public void ToggleBackendGameplayMode()
    {
        SetBackendGameplayEnabled(!backendGameplayEnabled);
        if (!backendGameplayEnabled)
        {
            SetBackendStatus("Gameplay mode: Local");
            RefreshUi();
            return;
        }

        if (TryStartBackendRequest("Server: preparing gameplay mode..."))
        {
            StartCoroutine(PrepareBackendGameplayModeRoutine());
            return;
        }

        RefreshUi();
    }

    private IEnumerator PrepareBackendGameplayModeRoutine()
    {
        var bootstrapSuccess = false;
        var bootstrapError = string.Empty;
        var bootstrap = default(MythwakeClientBootstrapDto);
        yield return backendClient.GetClientBootstrap((success, error, response) =>
        {
            bootstrapSuccess = success;
            bootstrapError = error;
            bootstrap = response;
        });

        if (!bootstrapSuccess)
        {
            SetBackendGameplayEnabled(false);
            FinishBackendRequest($"Bootstrap failed: {bootstrapError}");
            yield break;
        }

        backendDefinitions = bootstrap.definitions;
        hasBackendDefinitions = !string.IsNullOrWhiteSpace(bootstrap.definitions.contentHash);
        ApplyBackendSnapshot(bootstrap.playerSnapshot);
        SetBackendStatus($"Bootstrap: v{bootstrap.definitions.apiVersion}  {ShortHash(bootstrap.definitions.contentHash)}");
        RefreshUi();

        var claimSuccess = false;
        var claimError = string.Empty;
        var claimResult = default(MythwakeActionResultDto);
        yield return backendClient.ClaimOfflineRewards((success, error, result) =>
        {
            claimSuccess = success;
            claimError = error;
            claimResult = result;
        });

        CompleteBackendOfflineClaim(claimSuccess, claimError, claimResult, "Server mode");
    }

    private void ClaimDailyMissionOnBackend(int missionIndex)
    {
        missionIndex = Mathf.Clamp(missionIndex, 0, DailyMissionCount - 1);
        var missionId = GetBackendDailyMissionId(missionIndex);

        if (TryStartBackendRequest("Server: claiming daily mission..."))
        {
            StartCoroutine(backendClient.ClaimDailyMission(missionId, OnBackendGameplayAction));
        }
    }

    private void ClaimBattlePassRewardOnBackend(int rewardIndex)
    {
        rewardIndex = Mathf.Clamp(rewardIndex, 0, BattlePassRewardCount - 1);
        var rewardId = GetBackendBattlePassRewardId(rewardIndex);

        if (TryStartBackendRequest("Server: claiming mission track reward..."))
        {
            StartCoroutine(backendClient.ClaimBattlePassReward(rewardId, OnBackendGameplayAction));
        }
    }

    private string GetBackendDailyMissionId(int missionIndex)
    {
        missionIndex = Mathf.Clamp(missionIndex, 0, DailyMissionCount - 1);
        if (TryGetBackendDailyMissionDefinition(missionIndex, out var backendMission) && !string.IsNullOrWhiteSpace(backendMission.missionId))
        {
            return backendMission.missionId;
        }

        return GetDailyMissionDefinition(missionIndex).missionId;
    }

    private string GetBackendBattlePassRewardId(int rewardIndex)
    {
        rewardIndex = Mathf.Clamp(rewardIndex, 0, BattlePassRewardCount - 1);
        if (TryGetBackendBattlePassRewardDefinition(rewardIndex, out var backendReward) && !string.IsNullOrWhiteSpace(backendReward.rewardId))
        {
            return backendReward.rewardId;
        }

        return GetBattlePassRewardDefinition(rewardIndex).rewardId;
    }

    public void ResetProgress()
    {
        if (backendGameplayEnabled)
        {
            SetDungeonResult("Local reset is disabled in Server Mode.\nUse Backend Reset so PostgreSQL stays authoritative.");
            SetBackendStatus("Server mode: local reset blocked");
            return;
        }

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
        lastOfflineGoldReward = 0;
        lastOfflineReward = 0;
        lastOfflineSeconds = 0;
        lastOfflineRewardIsServer = false;

        SaveProgress();
        RefreshUi();
        SetDungeonResult("Prototype reset to fresh save.\nCurrencies, heroes, gear, missions cleared.");
    }

    private void LoadBackendGameplayPreference()
    {
        backendGameplayEnabled = PlayerPrefs.GetInt(BackendGameplayEnabledKey, backendGameplayEnabled ? 1 : 0) == 1;
        if (backendGameplayEnabled)
        {
            backendStatus = "Server mode: saved preference";
        }
    }

    private void PreparePersistedBackendGameplayMode()
    {
        if (!backendGameplayEnabled)
        {
            return;
        }

        if (TryStartBackendRequest("Server: restoring gameplay mode..."))
        {
            StartCoroutine(PrepareBackendGameplayModeRoutine());
        }
    }

    private void SetBackendGameplayEnabled(bool enabled)
    {
        backendGameplayEnabled = enabled;
        PlayerPrefs.SetInt(BackendGameplayEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private bool RejectDebugActionInBackendMode()
    {
        if (!backendGameplayEnabled)
        {
            return false;
        }

        SetDungeonResult("Debug shortcuts are disabled in Server Mode.\nUse backend actions/reset so PostgreSQL stays authoritative.");
        SetBackendStatus("Server mode: local debug grant blocked");
        return true;
    }

    private bool TryStartBackendRequest(string status)
    {
        EnsureRuntimeBackendClient();
        if (backendClient == null || backendRequestInProgress)
        {
            return false;
        }

        backendRequestInProgress = true;
        SetBackendStatus(status);
        RefreshBackendUi();
        RefreshGameplayInteractivity();
        return true;
    }

    private void OnBackendHealth(bool success, string error, MythwakeHealthDto health)
    {
        if (success)
        {
            FinishBackendRequest($"Backend: {health.status}  {FormatBackendHealthHeadline(health)}  {FormatBackendCacheSummary(health)}  v{health.version}");
            return;
        }

        FinishBackendRequest($"Backend offline: {error}");
    }

    private void OnBackendLogin(bool success, string error, MythwakeGuestAuthResponseDto response)
    {
        if (success)
        {
            ApplyBackendSnapshot(response.playerSnapshot);
            var playerId = string.IsNullOrWhiteSpace(response.playerId) ? backendClient.PlayerId : response.playerId;
            FinishBackendRequest($"Backend login: {playerId}  Session active");
            return;
        }

        FinishBackendRequest($"Backend login failed: {error}");
    }

    private void OnBackendSnapshot(bool success, string error, MythwakePlayerSnapshotDto snapshot)
    {
        if (success)
        {
            ApplyBackendSnapshot(snapshot);
            FinishBackendRequest($"Backend sync: {snapshot.playerId}  Stage {enemyLevel}");
            return;
        }

        FinishBackendRequest($"Backend sync failed: {error}");
    }

    private void OnBackendReset(bool success, string error, MythwakeDevResetResponseDto response)
    {
        if (success)
        {
            ApplyBackendSnapshot(response.playerSnapshot);
            SetDungeonResult("Server dev player reset to fresh progression.");
            FinishBackendRequest($"Backend reset: {response.playerId}");
            return;
        }

        FinishBackendRequest($"Backend reset failed: {error}");
    }

    private void OnBackendDefinitions(bool success, string error, MythwakeDefinitionSnapshotDto definitions, bool fromCache)
    {
        if (success)
        {
            backendDefinitions = definitions;
            hasBackendDefinitions = true;
            RefreshUi();

            var source = fromCache ? "cache" : "server";
            FinishBackendRequest($"Definitions: {source}  v{definitions.apiVersion}  {ShortHash(definitions.contentHash)}");
            return;
        }

        FinishBackendRequest($"Definitions sync failed: {error}");
    }

    private void OnBackendClock(bool success, string error, MythwakeServerClockDto clock)
    {
        if (success)
        {
            FinishBackendRequest($"Clock: {FormatClockTime(clock.serverTimeUtc)}  Daily {FormatResetCountdown(clock.secondsUntilDailyReset)}  Weekly {FormatResetCountdown(clock.secondsUntilWeeklyReset)}");
            return;
        }

        FinishBackendRequest($"Clock sync failed: {error}");
    }

    private static string FormatBackendCacheSummary(MythwakeHealthDto health)
    {
        var dirty = string.IsNullOrWhiteSpace(health.state_cache_dirty) ? "0" : health.state_cache_dirty;
        var queued = string.IsNullOrWhiteSpace(health.state_cache_queued) ? "0" : health.state_cache_queued;
        var failed = string.IsNullOrWhiteSpace(health.state_cache_failed) ? "0" : health.state_cache_failed;
        var loadedPlayers = string.IsNullOrWhiteSpace(health.loaded_players) ? "0" : health.loaded_players;
        if (!string.IsNullOrWhiteSpace(health.state_cache_error))
        {
            return $"Dirty {dirty}  Q {queued}  Failed {failed}  Hot {loadedPlayers}";
        }

        return $"Dirty {dirty}  Q {queued}  Hot {loadedPlayers}";
    }

    private static string FormatBackendHealthHeadline(MythwakeHealthDto health)
    {
        var database = string.IsNullOrWhiteSpace(health.database) ? "unknown" : health.database;
        var redis = string.IsNullOrWhiteSpace(health.redis) ? "disabled" : health.redis;
        var catalog = string.IsNullOrWhiteSpace(health.balance_catalog) ? "unknown" : health.balance_catalog;
        var writeMode = string.IsNullOrWhiteSpace(health.state_write_mode) ? "unknown" : health.state_write_mode;
        var lockStore = string.IsNullOrWhiteSpace(health.player_lock_store) ? "unknown" : health.player_lock_store;
        return $"DB {database}  Redis {redis}  Catalog {catalog}  {writeMode}  Lock {lockStore}";
    }

    private void OnBackendGameplayAction(bool success, string error, MythwakeActionResultDto result)
    {
        CompleteBackendAction(success, error, result, showInSummonPanel: false);
    }

    private void OnBackendSummonAction(bool success, string error, MythwakeActionResultDto result)
    {
        CompleteBackendAction(success, error, result, showInSummonPanel: true);
    }

    private void OnBackendOfflineClaim(bool success, string error, MythwakeActionResultDto result)
    {
        CompleteBackendOfflineClaim(success, error, result, "Server AFK");
    }

    private void CompleteBackendOfflineClaim(bool success, string error, MythwakeActionResultDto result, string source)
    {
        if (!success)
        {
            var failedMessage = $"Server AFK failed: {error}";
            SetDungeonResult(failedMessage);
            FinishBackendRequest(failedMessage);
            return;
        }

        ApplyBackendSnapshot(result.playerSnapshot);
        UpdateServerOfflineRewardUi(result);

        var message = string.IsNullOrWhiteSpace(result.message) ? "Server AFK checked." : result.message;
        SetDungeonResult(message);

        var outcome = FormatBackendActionOutcome(result);
        FinishBackendRequest($"{source}: {outcome}{FormatBackendRevisionSuffix(result)}");
    }

    private void CompleteBackendAction(bool success, string error, MythwakeActionResultDto result, bool showInSummonPanel)
    {
        if (!success)
        {
            var failedMessage = $"Server request failed: {error}";
            if (showInSummonPanel)
            {
                SetSummonResult(failedMessage);
            }
            else
            {
                SetDungeonResult(failedMessage);
            }

            FinishBackendRequest($"Server request failed: {error}");
            return;
        }

        ApplyBackendSnapshot(result.playerSnapshot);
        var message = string.IsNullOrWhiteSpace(result.message) ? FormatBackendActionOutcome(result) : result.message;
        if (!showInSummonPanel && HasServerCombatResult(result))
        {
            message = FormatServerCombatMessage(result);
            PlayServerCombatVisual(result.combat);
        }
        else if (!result.success)
        {
            message = $"{FormatBackendActionOutcome(result)}\n{message}";
        }
        else if (showInSummonPanel)
        {
            PlaySummonVisual(selectedHeroIndex, "Server Summon");
        }

        if (showInSummonPanel)
        {
            SetSummonResult(message);
        }
        else
        {
            SetDungeonResult(message);
        }

        var outcome = FormatBackendActionOutcome(result);
        FinishBackendRequest($"Server action: {outcome}  {result.actionId}{FormatBackendRevisionSuffix(result)}");
    }

    private static string FormatBackendActionOutcome(MythwakeActionResultDto result)
    {
        var outcome = result.success ? "ok" : HumanizeActionErrorCode(result.errorCode);
        if (result.replay)
        {
            outcome = $"{outcome} replay";
        }

        return outcome;
    }

    private static string HumanizeActionErrorCode(string errorCode)
    {
        switch (errorCode)
        {
            case "combat_lost":
                return "combat lost";
            case "stale_player_state":
                return "stale state - sync";
            case "afk_not_ready":
                return "AFK not ready";
            case "idempotency_conflict":
                return "idempotency conflict";
            case "persistence_failed":
                return "persistence failed";
            case "insufficient_currency":
                return "not enough currency";
            case "insufficient_shards":
                return "not enough shards";
            case "missing_item":
                return "missing item";
            case "missing_items":
                return "missing copies";
            case "max_level":
                return "max level";
            case "max_ascension":
                return "max ascension";
            case "max_rarity":
                return "max rarity";
            case "already_claimed":
                return "already claimed";
            case "not_complete":
                return "not complete";
            case "not_unlocked":
                return "not unlocked";
            case "invalid_banner":
                return "invalid banner";
            case "invalid_mission":
                return "invalid mission";
            case "invalid_reward":
                return "invalid reward";
            case "invalid_hero":
                return "invalid hero";
            case "invalid_equipment":
                return "invalid equipment";
            case "invalid_accessory":
                return "invalid accessory";
            case "invalid_dungeon":
                return "invalid dungeon";
            case "invalid_currency":
                return "invalid currency";
            default:
                return string.IsNullOrWhiteSpace(errorCode) ? "failed" : errorCode.Replace('_', ' ');
        }
    }

    private static string FormatBackendRevisionSuffix(MythwakeActionResultDto result)
    {
        var revision = Math.Max(result.receipt.stateRevision, result.playerSnapshot.revision);
        return revision > 0 ? $"  Rev {revision}" : string.Empty;
    }

    private static bool HasServerCombatResult(MythwakeActionResultDto result)
    {
        return result.combat.maxSeconds > 0 && result.combat.enemyMaxHp > 0;
    }

    private string FormatServerCombatMessage(MythwakeActionResultDto result)
    {
        var combat = result.combat;
        var status = combat.won ? "cleared" : "failed";
        var reward = FormatServerReward(result.reward);
        var rewardLine = string.IsNullOrWhiteSpace(reward) ? string.Empty : $"\nReward: {reward}";
        return $"{GetServerCombatLabel(combat)} {status} in {combat.elapsedSeconds}/{combat.maxSeconds}s" +
               $"\nHP {combat.teamHpRemaining}/{combat.teamMaxHp}  Enemy HP {combat.enemyHpRemaining}/{combat.enemyMaxHp}" +
               $"\nATK {combat.teamAttack}  Enemy DMG {combat.enemyDamage}  Dealt {combat.damageDealt}  Took {combat.damageTaken}" +
               rewardLine;
    }

    private string GetServerCombatLabel(MythwakeCombatResultDto combat)
    {
        if (combat.mode == "campaign")
        {
            return $"Campaign Stage {combat.targetLevel}";
        }

        if (combat.mode == "dungeon")
        {
            if (UseBackendDefinitionView() && TryGetBackendDungeonDefinition(combat.targetId, out var definition))
            {
                return $"{definition.displayName} F{combat.targetLevel}";
            }

            if (combat.targetId == GoldDungeonDefinition.dungeonId)
            {
                return $"{GoldDungeonDefinition.displayName} F{combat.targetLevel}";
            }

            if (combat.targetId == EssenceDungeonDefinition.dungeonId)
            {
                return $"{EssenceDungeonDefinition.displayName} F{combat.targetLevel}";
            }

            if (combat.targetId == GearDungeonDefinition.dungeonId)
            {
                return $"{GearDungeonDefinition.displayName} F{combat.targetLevel}";
            }
        }

        return string.IsNullOrWhiteSpace(combat.targetId) ? "Server Combat" : $"{combat.targetId} Lv {combat.targetLevel}";
    }

    private string FormatServerReward(MythwakeRewardDto reward)
    {
        var text = FormatReward(reward.gold, reward.gems, reward.mythEssence);
        if (reward.passXp <= 0)
        {
            return text == "None" ? string.Empty : text;
        }

        return text == "None" ? $"{reward.passXp} Pass XP" : $"{text}, {reward.passXp} Pass XP";
    }

    private void FinishBackendRequest(string status)
    {
        backendRequestInProgress = false;
        SetBackendStatus(status);
        SetBackendButtonsInteractable(true);
        RefreshUi();
    }

    private void FlushBackendLifecycle(string reason)
    {
        EnsureRuntimeBackendClient();
        if (backendClient == null || !backendClient.HasSession || backendLifecycleFlushInProgress)
        {
            return;
        }

        backendLifecycleFlushInProgress = true;
        StartCoroutine(backendClient.FlushPlayerState((success, error) =>
        {
            backendLifecycleFlushInProgress = false;
            if (success)
            {
                SetBackendStatus($"Backend flush: {reason}");
                return;
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                SetBackendStatus($"Backend flush failed: {error}");
            }
        }));
    }

    private void ApplyBackendSnapshot(MythwakePlayerSnapshotDto snapshot)
    {
        if (snapshot.state.campaignStage <= 0)
        {
            return;
        }

        backendStateRevision = Math.Max(0, snapshot.revision);

        var state = snapshot.state;
        saveVersion = CurrentSaveVersion;
        gold = Mathf.Max(0, state.gold);
        gems = Mathf.Max(0, state.gems);
        mythEssence = Mathf.Max(0, state.mythEssence);
        battlePassXp = Mathf.Max(0, state.passXp);
        enemyLevel = Mathf.Max(1, state.campaignStage);
        goldDungeonFloor = Mathf.Max(1, state.goldDungeonFloor);
        essenceDungeonFloor = Mathf.Max(1, state.essenceDungeonFloor);
        gearDungeonFloor = Mathf.Max(1, state.gearDungeonFloor);
        backendTeamPower = Mathf.Max(0, state.teamPower);
        backendTeamAttack = Mathf.Max(0, state.teamAttack);
        backendTeamHealth = Mathf.Max(0, state.teamHealth);

        ApplyBackendHeroes(snapshot.heroes, snapshot.heroShards);
        ApplyBackendEquipment(snapshot.equipment);
        ApplyBackendAccessories(snapshot.accessories, snapshot.equippedAccessories);
        ApplyBackendDailyProgress(snapshot.dailyProgress);
        ApplyBackendClaims(snapshot.dailyClaims, snapshot.battlePassClaims);

        summonCount = Mathf.Max(0, snapshot.summonCount);
        enemyMaxHp = GetStageMaxHp(enemyLevel);
        enemyHp = enemyMaxHp;
        damage = GetTeamDamage();
        upgradeCost = GetHeroUpgradeCost(selectedHeroIndex);

        SaveProgress();
        RefreshUi();
    }

    private void ApplyBackendHeroes(MythwakeHeroStateDto[] heroes, MythwakeHeroShardStateDto[] shards)
    {
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();

        for (var i = 0; i < HeroCount; i++)
        {
            heroLevels[i] = 1;
            heroShards[i] = 0;
            heroAscensions[i] = 0;
        }

        if (heroes != null)
        {
            for (var i = 0; i < heroes.Length; i++)
            {
                if (!TryGetHeroIndexById(heroes[i].heroId, out var heroIndex))
                {
                    continue;
                }

                heroLevels[heroIndex] = Mathf.Max(1, heroes[i].level);
                heroAscensions[heroIndex] = Mathf.Max(0, heroes[i].ascension);
            }
        }

        if (shards != null)
        {
            for (var i = 0; i < shards.Length; i++)
            {
                if (TryGetHeroIndexById(shards[i].heroId, out var heroIndex))
                {
                    heroShards[heroIndex] = Mathf.Max(0, shards[i].shards);
                }
            }
        }
    }

    private void ApplyBackendEquipment(MythwakeEquipmentStateDto[] equipment)
    {
        weaponLevel = StarterEquipmentLevel;
        armorLevel = StarterEquipmentLevel;
        backendWeaponLevel = 0;
        backendArmorLevel = 0;

        if (equipment == null)
        {
            return;
        }

        for (var i = 0; i < equipment.Length; i++)
        {
            if (equipment[i].equipmentId == WeaponTrack.equipmentId)
            {
                backendWeaponLevel = Mathf.Max(0, equipment[i].level);
                weaponLevel = Mathf.Max(StarterEquipmentLevel, backendWeaponLevel);
            }
            else if (equipment[i].equipmentId == ArmorTrack.equipmentId)
            {
                backendArmorLevel = Mathf.Max(0, equipment[i].level);
                armorLevel = Mathf.Max(StarterEquipmentLevel, backendArmorLevel);
            }
        }
    }

    private void ApplyBackendAccessories(MythwakeAccessoryStateDto[] accessories, MythwakeEquippedAccessoryDto[] equipped)
    {
        EnsureAccessories();

        for (var i = 0; i < accessoryInventory.Length; i++)
        {
            accessoryInventory[i] = 0;
        }

        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            equippedAccessoryRarities[slot] = -1;
            equippedAccessoryLevels[slot] = 0;
        }

        if (accessories != null)
        {
            for (var i = 0; i < accessories.Length; i++)
            {
                if (!TryGetAccessoryDefinitionById(accessories[i].accessoryId, out var definition))
                {
                    continue;
                }

                accessoryInventory[GetAccessoryInventoryIndex(definition.slotIndex, definition.rarityIndex)] = Mathf.Max(0, accessories[i].copies);
            }
        }

        if (equipped == null)
        {
            return;
        }

        for (var i = 0; i < equipped.Length; i++)
        {
            if (!TryGetAccessoryDefinitionById(equipped[i].accessoryId, out var definition))
            {
                continue;
            }

            equippedAccessoryRarities[definition.slotIndex] = definition.rarityIndex;
            equippedAccessoryLevels[definition.slotIndex] = Mathf.Clamp(GetBackendAccessoryLevel(accessories, equipped[i].accessoryId), 1, GetAccessoryMaxLevel(definition.rarityIndex));
        }
    }

    private void ApplyBackendClaims(MythwakeClaimStateDto[] dailyClaims, MythwakeClaimStateDto[] battlePassClaims)
    {
        EnsureDailyMissionClaims();
        EnsureBattlePassRewardClaims();

        for (var i = 0; i < dailyMissionClaimed.Length; i++)
        {
            dailyMissionClaimed[i] = GetClaimed(dailyClaims, GetDailyMissionDefinition(i).missionId);
        }

        for (var i = 0; i < battlePassRewardsClaimed.Length; i++)
        {
            battlePassRewardsClaimed[i] = GetClaimed(battlePassClaims, GetBattlePassRewardDefinition(i).rewardId);
        }
    }

    private void ApplyBackendDailyProgress(MythwakeDailyProgressDto[] dailyProgress)
    {
        if (dailyProgress == null)
        {
            return;
        }

        dailyFightCount = 0;
        dailyStageClearCount = 0;
        dailySummonCount = 0;
        EnsureDailyMissionClaims();

        for (var i = 0; i < dailyProgress.Length; i++)
        {
            if (!TryGetDailyMissionIndexById(dailyProgress[i].missionId, out var missionIndex))
            {
                continue;
            }

            dailyMissionClaimed[missionIndex] = dailyProgress[i].claimed;
            switch (GetDailyMissionDefinition(missionIndex).progressType)
            {
                case DailyMissionProgressType.Fight:
                    dailyFightCount = Mathf.Max(dailyFightCount, dailyProgress[i].progress);
                    break;
                case DailyMissionProgressType.StageClear:
                    dailyStageClearCount = Mathf.Max(dailyStageClearCount, dailyProgress[i].progress);
                    break;
                case DailyMissionProgressType.Summon:
                    dailySummonCount = Mathf.Max(dailySummonCount, dailyProgress[i].progress);
                    break;
            }
        }
    }

    private static int GetBackendAccessoryLevel(MythwakeAccessoryStateDto[] accessories, string accessoryId)
    {
        if (accessories == null)
        {
            return 1;
        }

        for (var i = 0; i < accessories.Length; i++)
        {
            if (accessories[i].accessoryId == accessoryId)
            {
                return Mathf.Max(1, accessories[i].level);
            }
        }

        return 1;
    }

    private static bool GetClaimed(MythwakeClaimStateDto[] claims, string claimId)
    {
        if (claims == null)
        {
            return false;
        }

        for (var i = 0; i < claims.Length; i++)
        {
            if (claims[i].claimId == claimId)
            {
                return claims[i].claimed;
            }
        }

        return false;
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            SaveProgress();
            FlushBackendLifecycle("pause");
            return;
        }

        ClaimBackendOfflineRewardsOnResume();
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
        FlushBackendLifecycle("quit");
    }

    private void ClaimBackendOfflineRewardsOnResume()
    {
        EnsureRuntimeBackendClient();
        if (!backendGameplayEnabled || backendClient == null || !backendClient.HasSession || backendRequestInProgress || backendLifecycleFlushInProgress)
        {
            return;
        }

        ClaimBackendOfflineRewards("resume");
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
            var winMessage = $"Campaign Stage {clearedStage} cleared in {result.elapsedSeconds}s\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}{milestoneText}";
            PlayCombatVisual("campaign", $"Campaign Stage {clearedStage}", result, stage.maxHp);
            SetDungeonResult(winMessage);
            return CreateActionResult(true, "campaign_fight", string.Empty, winMessage);
        }
        else
        {
            enemyMaxHp = stage.maxHp;
            enemyHp = enemyMaxHp;
            var failMessage = $"Campaign Stage {enemyLevel} failed after {result.elapsedSeconds}s\nEnemy HP {result.enemyHpRemaining}/{stage.maxHp}  {FormatCombatResult(result)}";
            PlayCombatVisual("campaign", $"Campaign Stage {enemyLevel}", result, stage.maxHp);
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
        lastOfflineGoldReward = 0;
        lastOfflineReward = 0;
        lastOfflineSeconds = 0;
        lastOfflineRewardIsServer = false;

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
        lastOfflineGoldReward = offlineGoldReward;
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
            var failMessage = $"{dungeon.displayName} Floor {floor} failed after {result.elapsedSeconds}s\nEnemy HP {result.enemyHpRemaining}/{enemyHp}  {FormatCombatResult(result)}";
            PlayCombatVisual(dungeon.dungeonId, $"{dungeon.displayName} F{floor}", result, enemyHp);
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
            message = $"{dungeon.displayName} Floor {floor} cleared in {result.elapsedSeconds}s (+{reward} Gold)\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}{bonusText}";
        }
        else
        {
            GrantCurrency(MythEssenceCurrencyId, reward);
            essenceDungeonFloor++;
            rewardDto = new MythwakeRewardDto { rewardId = $"reward_{dungeon.dungeonId}_floor_{floor}", mythEssence = reward + bonusReward.mythEssence };
            message = $"{dungeon.displayName} Floor {floor} cleared in {result.elapsedSeconds}s (+{reward} Essence)\nHP {result.teamHpRemaining}/{GetTeamHealth()}  {FormatCombatResult(result)}{bonusText}";
        }

        PlayCombatVisual(dungeon.dungeonId, $"{dungeon.displayName} F{floor}", result, enemyHp);
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
        var supportHeal = GetSupportHealPerSecond(maxTeamHp);
        enemyDamage = Mathf.Max(1, enemyDamage);

        for (var second = 1; second <= DefaultCombatDurationSeconds; second++)
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
                result.elapsedSeconds = second;
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
                result.elapsedSeconds = second;
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
        result.elapsedSeconds = DefaultCombatDurationSeconds;
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

    private void PlayCombatVisual(string mode, string label, CombatResult result, int enemyMaxHp)
    {
        if (runtimeArt == null)
        {
            return;
        }

        var maxTeamHp = Mathf.Max(1, GetTeamHealth());
        runtimeArt.PlayCombatResult(mode, label, new CombatVisualResult
        {
            won = result.won,
            elapsedSeconds = result.elapsedSeconds,
            heroHpPercent = result.teamHpRemaining / (float)maxTeamHp,
            enemyHpPercent = result.enemyHpRemaining / (float)Mathf.Max(1, enemyMaxHp)
        });
    }

    private void PlayServerCombatVisual(MythwakeCombatResultDto combat)
    {
        if (runtimeArt == null)
        {
            return;
        }

        var mode = combat.mode == "dungeon" ? combat.targetId : "campaign";
        runtimeArt.PlayCombatResult(mode, GetServerCombatLabel(combat), new CombatVisualResult
        {
            won = combat.won,
            elapsedSeconds = combat.elapsedSeconds,
            heroHpPercent = combat.teamHpRemaining / (float)Mathf.Max(1, combat.teamMaxHp),
            enemyHpPercent = combat.enemyHpRemaining / (float)Mathf.Max(1, combat.enemyMaxHp)
        });
    }

    private void PlaySummonVisual(int heroIndex, string label)
    {
        if (runtimeArt != null)
        {
            runtimeArt.PlaySummonResult(heroIndex, label);
        }
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

        if (runtimeDungeonResultText != null)
        {
            runtimeDungeonResultText.text = result;
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
            damageText.text = $"ATK {damage}   HP {GetTeamHealth()}   Guard -{Mathf.RoundToInt(GetTankDamageReductionRate() * 100f)}%   Heal/s {GetSupportHealPerSecond(GetTeamHealth())}";
        }

        RefreshDungeonUi();

        if (enemyText != null)
        {
            if (UseBackendDefinitionView() && TryGetBackendCampaignStageDefinition(enemyLevel, out var backendStage))
            {
                enemyText.text = $"Stage {enemyLevel}: {backendStage.displayName}\nRecommended Power {backendStage.requiredPower}";
            }
            else
            {
                var stage = GetStageDefinition(enemyLevel);
                enemyText.text = $"Stage {enemyLevel}: {stage.enemyName}\nRecommended Power {GetStageRecommendedPower(enemyLevel)}";
            }
        }

        if (enemyHpText != null)
        {
            if (UseBackendDefinitionView() && TryGetBackendCampaignStageDefinition(enemyLevel, out var backendStage))
            {
                enemyHpText.text = $"Enemy HP: {backendStage.enemyMaxHp}   Enemy Damage: {backendStage.enemyDamage}";
            }
            else
            {
                enemyHpText.text = $"Enemy HP: {enemyMaxHp}   Enemy Damage: {GetCampaignEnemyDamage(enemyLevel)}";
            }
        }

        RefreshAutoAttackUi();
        RefreshOfflineRewardUi();
        RefreshHeroUi();
        RefreshEquipmentUi();
        RefreshAccessoryUi();
        RefreshSummonUi();
        RefreshDailyMissionUi();
        RefreshBattlePassUi();
        RefreshBackendUi();
        RefreshRuntimeArtUi();
        RefreshTopBarUi();

        var heroLevelMax = IsHeroLevelMax(selectedHeroIndex);
        var heroAscensionMax = IsHeroAscensionMax(selectedHeroIndex);

        if (upgradeCostText != null)
        {
            upgradeCostText.text = heroLevelMax
                ? $"{GetHeroDefinition(selectedHeroIndex).name} Max Lv. {GetHeroLevelCap(selectedHeroIndex)}"
                : $"Upgrade {GetHeroDefinition(selectedHeroIndex).name} ({upgradeCost} Essence)";
        }

        if (heroUpgradeCostText != null)
        {
            heroUpgradeCostText.text = heroLevelMax
                ? $"{GetHeroDefinition(selectedHeroIndex).name} Max Lv. {GetHeroLevelCap(selectedHeroIndex)}"
                : $"Upgrade {GetHeroDefinition(selectedHeroIndex).name} ({upgradeCost} Essence)";
        }

        if (heroAscendCostText != null)
        {
            heroAscendCostText.text = heroAscensionMax
                ? $"{GetHeroDefinition(selectedHeroIndex).name} Max Asc. {GetHeroAscensionCap(selectedHeroIndex)}"
                : $"Ascend {GetHeroDefinition(selectedHeroIndex).name} ({GetHeroAscensionCost(selectedHeroIndex)} Shards)";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = !heroLevelMax && mythEssence >= upgradeCost;
        }

        if (heroUpgradeButton != null)
        {
            heroUpgradeButton.interactable = !heroLevelMax && mythEssence >= upgradeCost;
        }

        if (heroAscendButton != null)
        {
            heroAscendButton.interactable = !heroAscensionMax && heroShards[selectedHeroIndex] >= GetHeroAscensionCost(selectedHeroIndex);
        }

        if (weaponUpgradeButton != null)
        {
            weaponUpgradeButton.interactable = !IsEquipmentLevelMax(WeaponTrack, weaponLevel) && gold >= GetWeaponUpgradeCost();
        }

        if (armorUpgradeButton != null)
        {
            armorUpgradeButton.interactable = !IsEquipmentLevelMax(ArmorTrack, armorLevel) && gold >= GetArmorUpgradeCost();
        }

        RefreshAccessoryButtonStates();

        if (summonButton != null)
        {
            summonButton.interactable = gems >= GetSummonCost();
        }

        RefreshGameplayInteractivity();
    }

    private void RefreshRuntimeArtUi()
    {
        if (runtimeArt == null)
        {
            return;
        }

        var hero = GetHeroDefinition(selectedHeroIndex);
        var stage = GetStageDefinition(enemyLevel);
        var enemyName = stage.enemyName;
        if (UseBackendDefinitionView() && TryGetBackendCampaignStageDefinition(enemyLevel, out var backendStage))
        {
            enemyName = backendStage.displayName;
        }

        runtimeArt.Refresh(new MythwakeRuntimeArtState
        {
            selectedHeroIndex = selectedHeroIndex,
            selectedHeroName = hero.name,
            selectedHeroLevel = heroLevels[selectedHeroIndex],
            selectedHeroAscension = heroAscensions[selectedHeroIndex],
            teamPower = GetTeamPower(),
            teamAttack = GetTeamDamage(),
            teamHealth = GetTeamHealth(),
            campaignStage = enemyLevel,
            campaignEnemyName = enemyName,
            gold = gold,
            gems = gems,
            mythEssence = mythEssence,
            goldDungeonFloor = goldDungeonFloor,
            essenceDungeonFloor = essenceDungeonFloor,
            gearDungeonFloor = gearDungeonFloor,
            backendRequestInProgress = backendRequestInProgress
        });
    }

    private void RefreshGameplayInteractivity()
    {
        var canInteract = !backendRequestInProgress && !backendLifecycleFlushInProgress;

        SetButtonInteractable(fightButton, canInteract);
        SetButtonInteractable(goldDungeonButton, canInteract);
        SetButtonInteractable(essenceDungeonButton, canInteract);
        SetButtonInteractable(gearDungeonButton, canInteract);
        GateButton(upgradeButton, canInteract);
        GateButton(heroUpgradeButton, canInteract);
        GateButton(heroAscendButton, canInteract);
        GateButton(weaponUpgradeButton, canInteract);
        GateButton(armorUpgradeButton, canInteract);
        SetButtonInteractable(accessoryPreviousSlotButton, canInteract);
        SetButtonInteractable(accessoryNextSlotButton, canInteract);
        SetButtonInteractable(accessoryPreviousRarityButton, canInteract);
        SetButtonInteractable(accessoryNextRarityButton, canInteract);
        GateButton(accessoryEquipButton, canInteract);
        GateButton(accessoryLevelButton, canInteract);
        GateButton(accessoryFuseButton, canInteract);
        GateButton(summonButton, canInteract);
        SetButtonInteractable(resetButton, canInteract && !backendGameplayEnabled);
        SetButtonInteractable(debugGoldButton, canInteract && !backendGameplayEnabled);
        SetButtonInteractable(debugEssenceButton, canInteract && !backendGameplayEnabled);
        SetButtonInteractable(debugGemsButton, canInteract && !backendGameplayEnabled);
        SetButtonInteractable(debugAccessoryButton, canInteract && !backendGameplayEnabled);
        SetButtonsInteractable(heroSelectButtons, canInteract);
        GateButtons(dailyMissionButtons, canInteract);
        GateButtons(battlePassRewardButtons, canInteract);
    }

    private static void SetButtonsInteractable(Button[] buttons, bool interactable)
    {
        if (buttons == null)
        {
            return;
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            SetButtonInteractable(buttons[i], interactable);
        }
    }

    private static void GateButtons(Button[] buttons, bool canInteract)
    {
        if (buttons == null)
        {
            return;
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            GateButton(buttons[i], canInteract);
        }
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private static void GateButton(Button button, bool canInteract)
    {
        if (button != null)
        {
            button.interactable = button.interactable && canInteract;
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

        if (homeBeginButton != null)
        {
            homeBeginButton.onClick.AddListener(ShowBattle);
        }

        if (villageNavButton != null)
        {
            villageNavButton.onClick.AddListener(ShowHome);
        }

        if (campaignNavButton != null)
        {
            campaignNavButton.onClick.AddListener(ShowBattle);
        }

        if (dungeonsNavButton != null)
        {
            dungeonsNavButton.onClick.AddListener(ShowDungeons);
        }

        if (heroesNavButton != null)
        {
            heroesNavButton.onClick.AddListener(ShowHeroes);
        }

        if (summonNavButton != null)
        {
            summonNavButton.onClick.AddListener(ShowSummon);
        }

        if (battleTabButton != null)
        {
            battleTabButton.onClick.AddListener(ShowBattle);
        }

        if (dungeonsTabButton != null)
        {
            dungeonsTabButton.onClick.AddListener(ShowDungeons);
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

        if (homeBeginButton != null)
        {
            homeBeginButton.onClick.RemoveListener(ShowBattle);
        }

        if (villageNavButton != null)
        {
            villageNavButton.onClick.RemoveListener(ShowHome);
        }

        if (campaignNavButton != null)
        {
            campaignNavButton.onClick.RemoveListener(ShowBattle);
        }

        if (dungeonsNavButton != null)
        {
            dungeonsNavButton.onClick.RemoveListener(ShowDungeons);
        }

        if (heroesNavButton != null)
        {
            heroesNavButton.onClick.RemoveListener(ShowHeroes);
        }

        if (summonNavButton != null)
        {
            summonNavButton.onClick.RemoveListener(ShowSummon);
        }

        if (battleTabButton != null)
        {
            battleTabButton.onClick.RemoveListener(ShowBattle);
        }

        if (dungeonsTabButton != null)
        {
            dungeonsTabButton.onClick.RemoveListener(ShowDungeons);
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
        SetPanel(dungeonsPanel, screen == AppScreen.Dungeons);
        SetPanel(heroesPanel, screen == AppScreen.Heroes);
        SetPanel(gearPanel, screen == AppScreen.Gear);
        SetPanel(summonPanel, screen == AppScreen.Summon);
        SetPanel(shopPanel, screen == AppScreen.Shop);

        SetTabState(homeTabButton, screen == AppScreen.Home);
        SetTabState(battleTabButton, screen == AppScreen.Battle);
        SetTabState(dungeonsTabButton, screen == AppScreen.Dungeons);
        SetTabState(heroesTabButton, screen == AppScreen.Heroes);
        SetTabState(gearTabButton, screen == AppScreen.Gear);
        SetTabState(summonTabButton, screen == AppScreen.Summon);
        SetTabState(shopTabButton, screen == AppScreen.Shop);

        SetArtNavState(villageNavImage, villageNavButton, screen == AppScreen.Home);
        SetArtNavState(heroesNavImage, heroesNavButton, screen == AppScreen.Heroes);
        SetArtNavState(dungeonsNavImage, dungeonsNavButton, screen == AppScreen.Dungeons);
        SetArtNavState(summonNavImage, summonNavButton, screen == AppScreen.Summon);
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

    private static void SetArtNavState(RawImage image, Button button, bool isActive)
    {
        if (image == null)
        {
            return;
        }

        image.color = isActive ? Color.white : new Color(0.78f, 0.78f, 0.78f, 0.96f);

        if (button == null)
        {
            return;
        }

        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.8f, 0.95f, 1f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
        button.colors = colors;
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
            selectedHeroText.text = $"{hero.name}  Lv. {FormatCappedValue(heroLevels[selectedHeroIndex], GetHeroLevelCap(selectedHeroIndex))}  Asc. {FormatCappedValue(heroAscensions[selectedHeroIndex], GetHeroAscensionCap(selectedHeroIndex))}\n{hero.rarityName} {hero.roleName}  Power {GetHeroPower(selectedHeroIndex)}\nATK {GetHeroAttack(selectedHeroIndex)}  HP {GetHeroHealth(selectedHeroIndex)}  Shards {heroShards[selectedHeroIndex]}";
        }

        if (heroCardTexts != null)
        {
            for (var i = 0; i < Mathf.Min(heroCardTexts.Length, HeroCount); i++)
            {
                if (heroCardTexts[i] != null)
                {
                    var hero = GetHeroDefinition(i);
                    var marker = i == selectedHeroIndex ? "> " : string.Empty;
                    heroCardTexts[i].text = $"{marker}{hero.name}\nLv {FormatCappedValue(heroLevels[i], GetHeroLevelCap(i))}  A{FormatCappedValue(heroAscensions[i], GetHeroAscensionCap(i))}\nPower {GetHeroPower(i)}";
                }
            }
        }

        RefreshHeroCardVisuals();
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
        var displayedWeaponLevel = GetEquipmentDisplayLevel(WeaponTrack, weaponLevel);
        var displayedArmorLevel = GetEquipmentDisplayLevel(ArmorTrack, armorLevel);

        if (equipmentSummaryText != null)
        {
            equipmentSummaryText.text = $"Equipment\n{WeaponTrack.name} Lv. {FormatCappedValue(displayedWeaponLevel, GetEquipmentLevelCap(WeaponTrack))}  +{GetEquipmentAttackBonus()} {WeaponTrack.statLabel}\n{ArmorTrack.name} Lv. {FormatCappedValue(displayedArmorLevel, GetEquipmentLevelCap(ArmorTrack))}  +{GetEquipmentHealthBonus()} {ArmorTrack.statLabel}";
        }

        if (weaponUpgradeCostText != null)
        {
            weaponUpgradeCostText.text = IsEquipmentLevelMax(WeaponTrack, weaponLevel)
                ? $"{WeaponTrack.name}\nMax Lv. {GetEquipmentLevelCap(WeaponTrack)}"
                : $"{WeaponTrack.name} +1\n{GetWeaponUpgradeCost()} Gold";
        }

        if (armorUpgradeCostText != null)
        {
            armorUpgradeCostText.text = IsEquipmentLevelMax(ArmorTrack, armorLevel)
                ? $"{ArmorTrack.name}\nMax Lv. {GetEquipmentLevelCap(ArmorTrack)}"
                : $"{ArmorTrack.name} +1\n{GetArmorUpgradeCost()} Gold";
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
            var fuseCost = GetAccessoryFuseCost(rarity);
            var nextTier = string.IsNullOrEmpty(accessory.fuseTargetAccessoryId)
                ? "Max"
                : GetAccessoryRarityName(GetAccessoryDefinitionById(accessory.fuseTargetAccessoryId).rarityIndex);
            accessoryFuseText.text = $"Fuse {fuseCost}x {GetAccessoryRarityName(rarity)}\nInto {nextTier}";
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
            accessoryFuseButton.interactable = !string.IsNullOrEmpty(accessory.fuseTargetAccessoryId) && GetAccessoryInventoryCount(slot, rarity) >= GetAccessoryFuseCost(rarity);
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
        ClaimDailyMission(GetDailyMissionDefinition(missionIndex).missionId);
    }

    public MythwakeActionResultDto ClaimDailyMission(string missionId)
    {
        EnsureDailyMissionClaims();
        if (!TryGetDailyMissionIndexById(missionId, out var missionIndex))
        {
            var invalidResult = CreateActionResult(false, "daily_mission_claim", "invalid_mission", $"Unknown daily mission: {missionId}");
            RefreshUi();
            return invalidResult;
        }

        var mission = GetDailyMissionDefinition(missionIndex);

        if (dailyMissionClaimed[missionIndex])
        {
            RefreshUi();
            return CreateActionResult(false, "daily_mission_claim", "already_claimed", $"{mission.title} already claimed.");
        }

        if (GetDailyMissionProgress(missionIndex) < mission.target)
        {
            RefreshUi();
            return CreateActionResult(false, "daily_mission_claim", "not_complete", $"{mission.title} is not complete yet.");
        }

        dailyMissionClaimed[missionIndex] = true;
        GrantReward(mission.reward);

        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "daily_mission_claim", string.Empty, $"Claimed {mission.title}.", ToRewardDto(mission.reward));
    }

    private void ClaimBattlePassReward(int rewardIndex)
    {
        rewardIndex = Mathf.Clamp(rewardIndex, 0, BattlePassRewardCount - 1);
        ClaimBattlePassReward(GetBattlePassRewardDefinition(rewardIndex).rewardId);
    }

    public MythwakeActionResultDto ClaimBattlePassReward(string rewardId)
    {
        EnsureBattlePassRewardClaims();
        if (!TryGetBattlePassRewardIndexById(rewardId, out var rewardIndex))
        {
            var invalidResult = CreateActionResult(false, "battle_pass_claim", "invalid_reward", $"Unknown mission track reward: {rewardId}");
            RefreshUi();
            return invalidResult;
        }

        var rewardDefinition = GetBattlePassRewardDefinition(rewardIndex);

        if (battlePassRewardsClaimed[rewardIndex])
        {
            RefreshUi();
            return CreateActionResult(false, "battle_pass_claim", "already_claimed", $"Mission Track reward {rewardIndex + 1} already claimed.");
        }

        if (battlePassXp < rewardDefinition.requiredXp)
        {
            RefreshUi();
            return CreateActionResult(false, "battle_pass_claim", "not_unlocked", $"Need {rewardDefinition.requiredXp} Mission Track XP.");
        }

        battlePassRewardsClaimed[rewardIndex] = true;
        GrantReward(rewardDefinition.reward);

        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "battle_pass_claim", string.Empty, $"Claimed Mission Track reward {rewardIndex + 1}.", ToRewardDto(rewardDefinition.reward));
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

    private static bool TryGetHeroIndexById(string heroId, out int heroIndex)
    {
        for (var i = 0; i < HeroDefinitions.Length; i++)
        {
            if (HeroDefinitions[i].heroId == heroId)
            {
                heroIndex = i;
                return true;
            }
        }

        heroIndex = 0;
        return false;
    }

    private static DailyMissionDefinition GetDailyMissionDefinition(int index)
    {
        index = Mathf.Clamp(index, 0, DailyMissionDefinitions.Length - 1);
        return DailyMissionDefinitions[index];
    }

    private static bool TryGetDailyMissionIndexById(string missionId, out int missionIndex)
    {
        for (var i = 0; i < DailyMissionDefinitions.Length; i++)
        {
            if (DailyMissionDefinitions[i].missionId == missionId)
            {
                missionIndex = i;
                return true;
            }
        }

        missionIndex = 0;
        return false;
    }

    private static BattlePassRewardDefinition GetBattlePassRewardDefinition(int index)
    {
        index = Mathf.Clamp(index, 0, BattlePassRewardDefinitions.Length - 1);
        return BattlePassRewardDefinitions[index];
    }

    private static bool TryGetBattlePassRewardIndexById(string rewardId, out int rewardIndex)
    {
        for (var i = 0; i < BattlePassRewardDefinitions.Length; i++)
        {
            if (BattlePassRewardDefinitions[i].rewardId == rewardId)
            {
                rewardIndex = i;
                return true;
            }
        }

        rewardIndex = 0;
        return false;
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

    private int GetSummonCost()
    {
        if (TryGetBackendSummonBannerDefinition(HeroShardBanner.bannerId, out var backendBanner))
        {
            return Mathf.Max(0, backendBanner.costAmount);
        }

        return HeroShardBanner.costAmount;
    }

    private string GetSummonRatesText()
    {
        if (TryGetBackendSummonBannerDefinition(HeroShardBanner.bannerId, out var backendBanner))
        {
            var serverText = $"{backendBanner.displayName}\nCost {backendBanner.costAmount} {GetCurrencyDefinition(backendBanner.costCurrencyId).displayName}";
            if (backendBanner.shardDrops == null || backendBanner.shardDrops.Length == 0)
            {
                return serverText;
            }

            serverText += "\nRotation";
            for (var i = 0; i < backendBanner.shardDrops.Length; i++)
            {
                serverText += i == 0 ? " " : "  ";
                var heroName = backendBanner.shardDrops[i].heroId;
                if (TryGetHeroIndexById(backendBanner.shardDrops[i].heroId, out var heroIndex))
                {
                    heroName = GetHeroDefinition(heroIndex).name;
                }

                serverText += $"{heroName} x{backendBanner.shardDrops[i].shards}";
            }

            return serverText;
        }

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

    private static MythwakeRewardDto ToRewardDto(RewardDefinition reward)
    {
        return new MythwakeRewardDto
        {
            rewardId = reward.rewardId,
            gold = reward.gold,
            gems = reward.gems,
            mythEssence = reward.mythEssence,
            passXp = reward.passXp
        };
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

    public MythwakePlayerSnapshotDto GetPlayerSnapshot()
    {
        NormalizeLoadedState();
        return new MythwakePlayerSnapshotDto
        {
            playerId = "local-player",
            state = GetPlayerState(),
            heroes = CreateHeroSnapshot(),
            heroShards = CreateHeroShardSnapshot(),
            equipment = CreateEquipmentSnapshot(),
            accessories = CreateAccessorySnapshot(),
            equippedAccessories = CreateEquippedAccessorySnapshot(),
            dailyClaims = CreateDailyClaimSnapshot(),
            battlePassClaims = CreateBattlePassClaimSnapshot(),
            summonCount = summonCount
        };
    }

    public bool TryGetDefinitions(out MythwakeDefinitionSnapshotDto definitions)
    {
        definitions = backendDefinitions;
        return hasBackendDefinitions && !string.IsNullOrWhiteSpace(backendDefinitions.contentHash);
    }

    private bool UseBackendDefinitionView()
    {
        MythwakeDefinitionSnapshotDto definitions;
        return backendGameplayEnabled && TryGetDefinitions(out definitions);
    }

    private bool TryGetBackendDungeonDefinition(string dungeonId, out MythwakeDungeonDefinitionDto definition)
    {
        definition = default(MythwakeDungeonDefinitionDto);
        if (!TryGetDefinitions(out var definitions) || definitions.dungeons == null)
        {
            return false;
        }

        for (var i = 0; i < definitions.dungeons.Length; i++)
        {
            if (definitions.dungeons[i].dungeonId == dungeonId)
            {
                definition = definitions.dungeons[i];
                return true;
            }
        }

        return false;
    }

    private bool TryGetBackendCampaignStageDefinition(int stageNumber, out MythwakeCampaignStageDefinitionDto definition)
    {
        definition = default(MythwakeCampaignStageDefinitionDto);
        if (!TryGetDefinitions(out var definitions) || definitions.campaignStages == null)
        {
            return false;
        }

        stageNumber = Mathf.Max(1, stageNumber);
        for (var i = 0; i < definitions.campaignStages.Length; i++)
        {
            if (definitions.campaignStages[i].stageNumber == stageNumber)
            {
                definition = definitions.campaignStages[i];
                return definition.enemyMaxHp > 0 && definition.enemyDamage > 0;
            }
        }

        return false;
    }

    private bool TryGetBackendHeroDefinition(string heroId, out MythwakeHeroDefinitionDto definition)
    {
        definition = default(MythwakeHeroDefinitionDto);
        if (!UseBackendDefinitionView() || !TryGetDefinitions(out var definitions) || definitions.heroes == null)
        {
            return false;
        }

        for (var i = 0; i < definitions.heroes.Length; i++)
        {
            if (definitions.heroes[i].heroId == heroId)
            {
                definition = definitions.heroes[i];
                return !string.IsNullOrWhiteSpace(definition.heroId);
            }
        }

        return false;
    }

    private bool TryGetBackendEquipmentDefinition(string equipmentId, out MythwakeEquipmentDefinitionDto definition)
    {
        definition = default(MythwakeEquipmentDefinitionDto);
        if (!UseBackendDefinitionView() || !TryGetDefinitions(out var definitions) || definitions.equipment == null)
        {
            return false;
        }

        for (var i = 0; i < definitions.equipment.Length; i++)
        {
            if (definitions.equipment[i].equipmentId == equipmentId)
            {
                definition = definitions.equipment[i];
                return !string.IsNullOrWhiteSpace(definition.equipmentId);
            }
        }

        return false;
    }

    private bool TryGetBackendAccessoryDefinition(string accessoryId, out MythwakeAccessoryDefinitionDto definition)
    {
        definition = default(MythwakeAccessoryDefinitionDto);
        if (!UseBackendDefinitionView() || !TryGetDefinitions(out var definitions) || definitions.accessories == null)
        {
            return false;
        }

        for (var i = 0; i < definitions.accessories.Length; i++)
        {
            if (definitions.accessories[i].accessoryId == accessoryId)
            {
                definition = definitions.accessories[i];
                return !string.IsNullOrWhiteSpace(definition.accessoryId);
            }
        }

        return false;
    }

    private bool TryGetBackendAccessoryRarityDefinition(string rarityId, out MythwakeAccessoryRarityDefinitionDto definition)
    {
        definition = default(MythwakeAccessoryRarityDefinitionDto);
        if (!UseBackendDefinitionView() || !TryGetDefinitions(out var definitions) || definitions.accessoryRarities == null)
        {
            return false;
        }

        for (var i = 0; i < definitions.accessoryRarities.Length; i++)
        {
            if (definitions.accessoryRarities[i].rarityId == rarityId)
            {
                definition = definitions.accessoryRarities[i];
                return !string.IsNullOrWhiteSpace(definition.rarityId);
            }
        }

        return false;
    }

    private bool TryGetBackendAccessoryRarityDefinition(int rarity, out MythwakeAccessoryRarityDefinitionDto definition)
    {
        definition = default(MythwakeAccessoryRarityDefinitionDto);
        var localDefinition = GetAccessoryDefinition(0, rarity);
        if (!TryGetBackendAccessoryDefinition(localDefinition.accessoryId, out var backendAccessory))
        {
            return false;
        }

        return TryGetBackendAccessoryRarityDefinition(backendAccessory.rarityId, out definition);
    }

    private bool TryGetBackendAfkRewardDefinition(out MythwakeAfkRewardDefinitionDto definition)
    {
        definition = default(MythwakeAfkRewardDefinitionDto);
        if (!UseBackendDefinitionView() || !TryGetDefinitions(out var definitions) || definitions.afkRewards == null || definitions.afkRewards.Length == 0)
        {
            return false;
        }

        definition = definitions.afkRewards[0];
        return !string.IsNullOrWhiteSpace(definition.rewardId);
    }

    private bool TryGetBackendProgressionCostAmount(string costId, string targetId, int progressValue, out int amount, out string currencyId)
    {
        amount = 0;
        currencyId = string.Empty;
        if (!UseBackendDefinitionView() || !TryGetDefinitions(out var definitions) || definitions.progressionCosts == null)
        {
            return false;
        }

        for (var i = 0; i < definitions.progressionCosts.Length; i++)
        {
            var definition = definitions.progressionCosts[i];
            if (definition.costId != costId)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(definition.targetId) && definition.targetId != "*" && definition.targetId != targetId)
            {
                continue;
            }

            amount = Mathf.Max(0, definition.baseAmount + (Mathf.Max(0, progressValue) * definition.amountPerLevel));
            currencyId = definition.costCurrencyId;
            return true;
        }

        return false;
    }

    private bool TryGetBackendSummonBannerDefinition(string bannerId, out MythwakeSummonBannerDefinitionDto definition)
    {
        definition = default(MythwakeSummonBannerDefinitionDto);
        if (!UseBackendDefinitionView() || !TryGetDefinitions(out var definitions) || definitions.summonBanners == null)
        {
            return false;
        }

        for (var i = 0; i < definitions.summonBanners.Length; i++)
        {
            if (definitions.summonBanners[i].bannerId == bannerId)
            {
                definition = definitions.summonBanners[i];
                return true;
            }
        }

        return false;
    }

    private bool TryGetBackendDailyMissionDefinition(int missionIndex, out MythwakeDailyMissionDefinitionDto definition)
    {
        definition = default(MythwakeDailyMissionDefinitionDto);
        if (!UseBackendDefinitionView() || !TryGetDefinitions(out var definitions) || definitions.dailyMissions == null || definitions.dailyMissions.Length == 0)
        {
            return false;
        }

        missionIndex = Mathf.Clamp(missionIndex, 0, definitions.dailyMissions.Length - 1);
        if (missionIndex < 0 || missionIndex >= definitions.dailyMissions.Length)
        {
            return false;
        }

        definition = definitions.dailyMissions[missionIndex];
        return !string.IsNullOrWhiteSpace(definition.missionId);
    }

    private bool TryGetBackendBattlePassRewardDefinition(int rewardIndex, out MythwakeBattlePassRewardDefinitionDto definition)
    {
        definition = default(MythwakeBattlePassRewardDefinitionDto);
        if (!UseBackendDefinitionView() || !TryGetDefinitions(out var definitions) || definitions.battlePassRewards == null || definitions.battlePassRewards.Length == 0)
        {
            return false;
        }

        rewardIndex = Mathf.Clamp(rewardIndex, 0, definitions.battlePassRewards.Length - 1);
        if (rewardIndex < 0 || rewardIndex >= definitions.battlePassRewards.Length)
        {
            return false;
        }

        definition = definitions.battlePassRewards[rewardIndex];
        return !string.IsNullOrWhiteSpace(definition.rewardId);
    }

    private static int GetBackendDungeonRequiredPower(MythwakeDungeonDefinitionDto definition, int floor)
    {
        floor = Mathf.Max(1, floor);
        return Mathf.Max(1, definition.baseRequiredPower + (floor * definition.requiredPowerPerFloor));
    }

    private static int GetBackendDungeonRewardAmount(MythwakeDungeonDefinitionDto definition, int floor)
    {
        floor = Mathf.Max(1, floor);
        return Mathf.Max(0, definition.baseRewardAmount + (floor * definition.rewardPerFloor));
    }

    private static int GetBackendDungeonEnemyHp(MythwakeDungeonDefinitionDto definition, int floor)
    {
        floor = Mathf.Max(1, floor);
        var requiredPower = GetBackendDungeonRequiredPower(definition, floor);
        var baseHp = definition.enemyBaseHp > 0 ? definition.enemyBaseHp : 220;
        var hpPerPower = definition.enemyHpPerPower > 0 ? definition.enemyHpPerPower : 2;
        var hpPerFloor = definition.enemyHpPerFloor > 0 ? definition.enemyHpPerFloor : 95;
        return Mathf.Max(1, baseHp + (requiredPower * hpPerPower) + (floor * hpPerFloor));
    }

    private static int GetBackendDungeonEnemyDamage(MythwakeDungeonDefinitionDto definition, int floor)
    {
        floor = Mathf.Max(1, floor);
        var requiredPower = GetBackendDungeonRequiredPower(definition, floor);
        var baseDamage = definition.enemyBaseDamage > 0 ? definition.enemyBaseDamage : 26;
        var damagePerFloor = definition.enemyDamagePerFloor > 0 ? definition.enemyDamagePerFloor : definition.dungeonId == GearDungeonDefinition.dungeonId ? 4 : 3;
        var damagePowerDivisor = definition.enemyDamagePowerDivisor > 0 ? definition.enemyDamagePowerDivisor : 48;
        return Mathf.Max(1, baseDamage + (floor * damagePerFloor) + (requiredPower / damagePowerDivisor));
    }

    private static string ShortHash(string contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
        {
            return "no-hash";
        }

        return contentHash.Length <= 8 ? contentHash : contentHash.Substring(0, 8);
    }

    private static bool HasFiniteCap(int cap)
    {
        return cap >= 0 && cap < int.MaxValue / 2;
    }

    private static string FormatCappedValue(int value, int cap)
    {
        return HasFiniteCap(cap) ? $"{value}/{cap}" : value.ToString();
    }

    private static string FormatClockTime(string serverTimeUtc)
    {
        if (!DateTime.TryParse(serverTimeUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return "synced";
        }

        return $"{parsed.ToUniversalTime():HH:mm} UTC";
    }

    private static string FormatResetCountdown(long seconds)
    {
        seconds = Math.Max(0, seconds);
        var time = TimeSpan.FromSeconds(seconds);

        if (time.TotalDays >= 1)
        {
            return $"{(int)time.TotalDays}d {time.Hours}h";
        }

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

    private MythwakeHeroStateDto[] CreateHeroSnapshot()
    {
        EnsureHeroLevels();
        EnsureHeroAscensions();
        var snapshot = new MythwakeHeroStateDto[HeroCount];
        for (var i = 0; i < snapshot.Length; i++)
        {
            snapshot[i] = new MythwakeHeroStateDto
            {
                heroId = GetHeroDefinition(i).heroId,
                level = heroLevels[i],
                ascension = heroAscensions[i]
            };
        }

        return snapshot;
    }

    private MythwakeHeroShardStateDto[] CreateHeroShardSnapshot()
    {
        EnsureHeroShards();
        var snapshot = new MythwakeHeroShardStateDto[HeroCount];
        for (var i = 0; i < snapshot.Length; i++)
        {
            snapshot[i] = new MythwakeHeroShardStateDto
            {
                heroId = GetHeroDefinition(i).heroId,
                shards = heroShards[i]
            };
        }

        return snapshot;
    }

    private MythwakeEquipmentStateDto[] CreateEquipmentSnapshot()
    {
        return new[]
        {
            new MythwakeEquipmentStateDto { equipmentId = WeaponTrack.equipmentId, level = weaponLevel },
            new MythwakeEquipmentStateDto { equipmentId = ArmorTrack.equipmentId, level = armorLevel }
        };
    }

    private MythwakeAccessoryStateDto[] CreateAccessorySnapshot()
    {
        EnsureAccessories();
        var snapshot = new MythwakeAccessoryStateDto[AccessorySlotCount * AccessoryRarityCount];

        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            for (var rarity = 0; rarity < AccessoryRarityCount; rarity++)
            {
                var definition = GetAccessoryDefinition(slot, rarity);
                snapshot[GetAccessoryDefinitionIndex(slot, rarity)] = new MythwakeAccessoryStateDto
                {
                    accessoryId = definition.accessoryId,
                    copies = GetAccessoryInventoryCount(slot, rarity),
                    level = GetAccessorySnapshotLevel(slot, rarity)
                };
            }
        }

        return snapshot;
    }

    private MythwakeEquippedAccessoryDto[] CreateEquippedAccessorySnapshot()
    {
        EnsureAccessories();
        var equippedCount = 0;
        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            if (equippedAccessoryRarities[slot] >= 0)
            {
                equippedCount++;
            }
        }

        var snapshot = new MythwakeEquippedAccessoryDto[equippedCount];
        var index = 0;
        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            var rarity = equippedAccessoryRarities[slot];
            if (rarity < 0)
            {
                continue;
            }

            snapshot[index] = new MythwakeEquippedAccessoryDto
            {
                slotId = AccessorySlots[slot].itemSlotId,
                accessoryId = GetAccessoryDefinition(slot, rarity).accessoryId
            };
            index++;
        }

        return snapshot;
    }

    private MythwakeClaimStateDto[] CreateDailyClaimSnapshot()
    {
        EnsureDailyMissionClaims();
        var snapshot = new MythwakeClaimStateDto[DailyMissionCount];
        for (var i = 0; i < snapshot.Length; i++)
        {
            snapshot[i] = new MythwakeClaimStateDto
            {
                claimId = GetDailyMissionDefinition(i).missionId,
                claimed = dailyMissionClaimed[i]
            };
        }

        return snapshot;
    }

    private MythwakeClaimStateDto[] CreateBattlePassClaimSnapshot()
    {
        EnsureBattlePassRewardClaims();
        var snapshot = new MythwakeClaimStateDto[BattlePassRewardCount];
        for (var i = 0; i < snapshot.Length; i++)
        {
            snapshot[i] = new MythwakeClaimStateDto
            {
                claimId = GetBattlePassRewardDefinition(i).rewardId,
                claimed = battlePassRewardsClaimed[i]
            };
        }

        return snapshot;
    }

    private int GetAccessorySnapshotLevel(int slot, int rarity)
    {
        if (equippedAccessoryRarities[slot] != rarity)
        {
            return 0;
        }

        return Mathf.Max(1, equippedAccessoryLevels[slot]);
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
            playerSnapshot = GetPlayerSnapshot(),
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
        if (backendGameplayEnabled && backendTeamPower > 0)
        {
            return backendTeamPower;
        }

        var power = 0;

        for (var i = 0; i < HeroCount; i++)
        {
            power += GetHeroPower(i);
        }

        return power + GetEquipmentPower() + GetAccessoryPower();
    }

    private int GetTeamDamage()
    {
        if (backendGameplayEnabled && backendTeamAttack > 0)
        {
            return backendTeamAttack;
        }

        var multiplier = 1f
            + (CountHeroesWithRole(WarriorRoleId) * WarriorDamageBonusRate)
            + (CountHeroesWithRole(MageRoleId) * MageDamageBonusRate);

        return Mathf.Max(1, Mathf.FloorToInt((GetTeamBaseAttack() + GetEquipmentAttackBonus() + GetAccessoryAttackBonus()) * multiplier));
    }

    private int GetTeamHealth()
    {
        if (backendGameplayEnabled && backendTeamHealth > 0)
        {
            return backendTeamHealth;
        }

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
        if (TryGetBackendEquipmentDefinition(WeaponTrack.equipmentId, out var definition))
        {
            var level = Mathf.Clamp(GetEquipmentDisplayLevel(WeaponTrack, weaponLevel), 0, Mathf.Max(1, definition.maxLevel));
            return Mathf.Max(0, definition.attackPerLevel) * level;
        }

        return GetEquipmentBonus(WeaponTrack, weaponLevel);
    }

    private int GetEquipmentHealthBonus()
    {
        if (TryGetBackendEquipmentDefinition(ArmorTrack.equipmentId, out var definition))
        {
            var level = Mathf.Clamp(GetEquipmentDisplayLevel(ArmorTrack, armorLevel), 0, Mathf.Max(1, definition.maxLevel));
            return Mathf.Max(0, definition.healthPerLevel) * level;
        }

        return GetEquipmentBonus(ArmorTrack, armorLevel);
    }

    private int GetEquipmentDisplayLevel(EquipmentTrackDefinition track, int fallbackLevel)
    {
        if (!UseBackendDefinitionView())
        {
            return Mathf.Max(StarterEquipmentLevel, fallbackLevel);
        }

        if (track.equipmentId == WeaponTrack.equipmentId)
        {
            return Mathf.Max(0, backendWeaponLevel);
        }

        if (track.equipmentId == ArmorTrack.equipmentId)
        {
            return Mathf.Max(0, backendArmorLevel);
        }

        return Mathf.Max(0, fallbackLevel);
    }

    private int GetEquipmentLevelCap(EquipmentTrackDefinition track)
    {
        if (TryGetBackendEquipmentDefinition(track.equipmentId, out var definition))
        {
            return Mathf.Max(1, definition.maxLevel);
        }

        return int.MaxValue;
    }

    private bool IsEquipmentLevelMax(EquipmentTrackDefinition track, int fallbackLevel)
    {
        var cap = GetEquipmentLevelCap(track);
        if (!HasFiniteCap(cap))
        {
            return false;
        }

        return GetEquipmentDisplayLevel(track, fallbackLevel) >= cap;
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
        if (TryGetBackendAccessoryDefinition(definition.accessoryId, out var backendDefinition))
        {
            return Mathf.Max(0, backendDefinition.attackPerLevel) * level;
        }

        return definition.attackPerLevel * level;
    }

    private int GetAccessoryHealthFor(int slot, int rarity, int level)
    {
        if (rarity < 0 || level <= 0)
        {
            return 0;
        }

        var definition = GetAccessoryDefinition(slot, rarity);
        if (TryGetBackendAccessoryDefinition(definition.accessoryId, out var backendDefinition))
        {
            return Mathf.Max(0, backendDefinition.healthPerLevel) * level;
        }

        return definition.healthPerLevel * level;
    }

    private int GetAccessoryMaxLevel(int rarity)
    {
        if (TryGetBackendAccessoryRarityDefinition(rarity, out var backendRarity))
        {
            return Mathf.Max(1, backendRarity.maxLevel);
        }

        return GetAccessoryDefinition(0, rarity).levelCap;
    }

    private int GetAccessoryFuseCost(int rarity)
    {
        if (TryGetBackendAccessoryRarityDefinition(rarity, out var backendRarity))
        {
            return Mathf.Max(1, backendRarity.fuseCopyCost);
        }

        return AccessoryFuseCost;
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
        if (TryGetBackendProgressionCostAmount("accessory_level_any", "*", level, out var backendCost, out _))
        {
            return backendCost;
        }

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

    private bool TryGetFirstOwnedAccessoryId(out string accessoryId)
    {
        EnsureAccessories();
        for (var rarity = AccessoryRarityCount - 1; rarity >= 0; rarity--)
        {
            for (var slot = 0; slot < AccessorySlotCount; slot++)
            {
                if (GetAccessoryInventoryCount(slot, rarity) > 0)
                {
                    accessoryId = GetAccessoryDefinition(slot, rarity).accessoryId;
                    return true;
                }
            }
        }

        accessoryId = string.Empty;
        return false;
    }

    private bool TryGetFuseCandidateAccessoryId(out string accessoryId)
    {
        EnsureAccessories();
        for (var rarity = 0; rarity < AccessoryRarityCount - 1; rarity++)
        {
            for (var slot = 0; slot < AccessorySlotCount; slot++)
            {
                var definition = GetAccessoryDefinition(slot, rarity);
                if (!string.IsNullOrWhiteSpace(definition.fuseTargetAccessoryId) && GetAccessoryInventoryCount(slot, rarity) >= GetAccessoryFuseCost(rarity))
                {
                    accessoryId = definition.accessoryId;
                    return true;
                }
            }
        }

        accessoryId = string.Empty;
        return false;
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

        if (TryGetBackendHeroDefinition(hero.heroId, out var backendHero))
        {
            var level = Mathf.Clamp(heroLevels[index], 1, Mathf.Max(1, backendHero.maxLevel));
            var ascension = Mathf.Clamp(heroAscensions[index], 0, Mathf.Max(0, backendHero.maxAscension));
            return Mathf.Max(1, backendHero.baseAttack + ((level - 1) * backendHero.attackPerLevel) + (ascension * backendHero.attackPerAscension));
        }

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

        if (TryGetBackendHeroDefinition(hero.heroId, out var backendHero))
        {
            var level = Mathf.Clamp(heroLevels[index], 1, Mathf.Max(1, backendHero.maxLevel));
            var ascension = Mathf.Clamp(heroAscensions[index], 0, Mathf.Max(0, backendHero.maxAscension));
            return Mathf.Max(1, backendHero.baseHealth + ((level - 1) * backendHero.healthPerLevel) + (ascension * backendHero.healthPerAscension));
        }

        return hero.baseHealth
            + (heroLevels[index] * hero.healthGrowth)
            + Mathf.FloorToInt(heroShards[index] * 1.2f)
            + (heroAscensions[index] * hero.ascensionHealth);
    }

    private int GetHeroUpgradeCost(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroLevels();
        if (TryGetBackendProgressionCostAmount("hero_level_any", "*", heroLevels[index], out var backendCost, out _))
        {
            return backendCost;
        }

        return Mathf.CeilToInt(14 * Mathf.Pow(1.34f, heroLevels[index] - 1));
    }

    private int GetHeroLevelCap(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        var hero = GetHeroDefinition(index);
        if (TryGetBackendHeroDefinition(hero.heroId, out var backendHero))
        {
            return Mathf.Max(1, backendHero.maxLevel);
        }

        return int.MaxValue;
    }

    private bool IsHeroLevelMax(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        var cap = GetHeroLevelCap(index);
        return HasFiniteCap(cap) && heroLevels[index] >= cap;
    }

    private int GetHeroAscensionCost(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroAscensions();
        if (TryGetBackendProgressionCostAmount("hero_ascension_any", "*", heroAscensions[index], out var backendCost, out _))
        {
            return backendCost;
        }

        var hero = GetHeroDefinition(index);
        return hero.ascensionBaseCost + (heroAscensions[index] * hero.ascensionCostGrowth);
    }

    private int GetHeroAscensionCap(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        var hero = GetHeroDefinition(index);
        if (TryGetBackendHeroDefinition(hero.heroId, out var backendHero))
        {
            return Mathf.Max(0, backendHero.maxAscension);
        }

        return int.MaxValue;
    }

    private bool IsHeroAscensionMax(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        var cap = GetHeroAscensionCap(index);
        return HasFiniteCap(cap) && heroAscensions[index] >= cap;
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
        var costId = track.equipmentId == WeaponTrack.equipmentId ? "equipment_weapon_level" : "equipment_armor_level";
        var serverLevel = track.equipmentId == WeaponTrack.equipmentId ? backendWeaponLevel : backendArmorLevel;
        if (TryGetBackendProgressionCostAmount(costId, track.equipmentId, serverLevel, out var backendCost, out _))
        {
            return backendCost;
        }

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

    private int GetSupportHealPerSecond(int maxTeamHealth)
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

        if (backendGameplayEnabled)
        {
            autoAttackText.text = "Auto Attack: Off in Server Mode";
            return;
        }

        var remaining = Mathf.Max(0f, autoAttackInterval - autoAttackTimer);
        autoAttackText.text = $"Auto Attack: {remaining:0.0}s";
    }

    private void RefreshDungeonUi()
    {
        if (goldDungeonText != null)
        {
            goldDungeonText.text = FormatDungeonPreview(GoldDungeonDefinition, goldDungeonFloor);
        }

        if (essenceDungeonText != null)
        {
            essenceDungeonText.text = FormatDungeonPreview(EssenceDungeonDefinition, essenceDungeonFloor);
        }

        if (gearDungeonText != null)
        {
            gearDungeonText.text = FormatDungeonPreview(GearDungeonDefinition, gearDungeonFloor);
        }

        if (dungeonResultText != null && string.IsNullOrWhiteSpace(dungeonResultText.text))
        {
            dungeonResultText.text = "Dungeons are the active resource source.";
        }
    }

    private string FormatDungeonPreview(DungeonDefinition localDefinition, int floor)
    {
        if (UseBackendDefinitionView() && TryGetBackendDungeonDefinition(localDefinition.dungeonId, out var backendDefinition))
        {
            var requiredPower = GetBackendDungeonRequiredPower(backendDefinition, floor);
            var enemyHp = GetBackendDungeonEnemyHp(backendDefinition, floor);
            var enemyDamage = GetBackendDungeonEnemyDamage(backendDefinition, floor);
            if (string.IsNullOrWhiteSpace(backendDefinition.rewardCurrencyId))
            {
                return $"{backendDefinition.displayName} F{floor}  Rec {requiredPower}\nEnemy HP {enemyHp}  DMG {enemyDamage}  Drop";
            }

            var rewardAmount = GetBackendDungeonRewardAmount(backendDefinition, floor);
            var currencyName = GetCurrencyDefinition(backendDefinition.rewardCurrencyId).displayName;
            return $"{backendDefinition.displayName} F{floor}  Rec {requiredPower}\nEnemy HP {enemyHp}  DMG {enemyDamage}  +{rewardAmount} {currencyName}";
        }

        if (localDefinition.dungeonId == GoldDungeonDefinition.dungeonId)
        {
            return $"{localDefinition.displayName} F{floor}  Rec {GetDungeonRecommendedPower(floor)}\n+{GetGoldDungeonReward(floor)} {GetCurrencyDefinition(localDefinition.rewardCurrencyId).displayName}";
        }

        if (localDefinition.dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return $"{localDefinition.displayName} F{floor}  Rec {GetDungeonRecommendedPower(floor)}\n+{GetEssenceDungeonReward(floor)} {GetCurrencyDefinition(localDefinition.rewardCurrencyId).displayName}";
        }

        return $"{localDefinition.displayName} F{floor}  Rec {GetGearDungeonRecommendedPower(floor)}\nRandom accessory drop";
    }

    private void RefreshOfflineRewardUi()
    {
        if (offlineRewardText == null)
        {
            return;
        }

        if (backendGameplayEnabled || lastOfflineRewardIsServer)
        {
            if (lastOfflineGoldReward > 0 || lastOfflineReward > 0)
            {
                offlineRewardText.text = $"Server AFK: +{lastOfflineGoldReward} Gold, +{lastOfflineReward} Essence";
                return;
            }

            if (TryGetBackendAfkRewardDefinition(out var afkDefinition))
            {
                offlineRewardText.text = $"Server AFK: min {FormatDuration(afkDefinition.minClaimSeconds)}, cap {FormatDuration(afkDefinition.maxClaimSeconds)}";
                return;
            }

            offlineRewardText.text = "Server AFK: sync definitions first";
            return;
        }

        if (lastOfflineGoldReward <= 0 && lastOfflineReward <= 0)
        {
            offlineRewardText.text = "Offline: no reward yet";
            return;
        }

        offlineRewardText.text = $"Offline: +{lastOfflineGoldReward} Gold, +{lastOfflineReward} Essence ({FormatDuration(lastOfflineSeconds)})";
    }

    private void UpdateServerOfflineRewardUi(MythwakeActionResultDto result)
    {
        lastOfflineGoldReward = Mathf.Max(0, result.reward.gold);
        lastOfflineReward = Mathf.Max(0, result.reward.mythEssence);
        lastOfflineSeconds = 0;
        lastOfflineRewardIsServer = true;

        if (offlineRewardText == null)
        {
            return;
        }

        if (lastOfflineGoldReward > 0 || lastOfflineReward > 0)
        {
            offlineRewardText.text = $"Server AFK: +{lastOfflineGoldReward} Gold, +{lastOfflineReward} Essence";
            return;
        }

        var message = string.IsNullOrWhiteSpace(result.message) ? "no reward yet" : result.message;
        offlineRewardText.text = $"Server AFK: {message}";
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
            var title = mission.title;
            var target = mission.target;
            var rewardText = FormatReward(mission.reward);
            if (TryGetBackendDailyMissionDefinition(i, out var backendMission))
            {
                title = backendMission.displayName;
                target = Mathf.Max(1, backendMission.target);
                rewardText = FormatServerReward(backendMission.reward);
            }

            var progress = Mathf.Min(GetDailyMissionProgress(i), target);
            var isComplete = progress >= target;
            var isClaimed = dailyMissionClaimed[i];
            var state = isClaimed ? "Claimed" : isComplete ? "Claim" : $"{progress}/{target}";
            var text = $"{title}\n{state}  Reward {rewardText}";

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
            var dailyPassXp = BattlePassXpPerDailyClaim;
            if (TryGetBackendDailyMissionDefinition(0, out var backendDailyMission) && backendDailyMission.reward.passXp > 0)
            {
                dailyPassXp = backendDailyMission.reward.passXp;
            }

            battlePassProgressText.text = $"Mission Track XP: {battlePassXp}\nDaily claims give +{dailyPassXp} XP";
        }

        for (var i = 0; i < BattlePassRewardCount; i++)
        {
            var rewardDefinition = GetBattlePassRewardDefinition(i);
            var requiredXp = rewardDefinition.requiredXp;
            var rewardText = FormatReward(rewardDefinition.reward);
            if (TryGetBackendBattlePassRewardDefinition(i, out var backendReward))
            {
                requiredXp = Mathf.Max(0, backendReward.requiredPassXp);
                rewardText = FormatServerReward(backendReward.reward);
            }

            var isReady = battlePassXp >= requiredXp;
            var isClaimed = battlePassRewardsClaimed[i];
            var state = isClaimed ? "Claimed" : isReady ? "Claim" : $"{battlePassXp}/{requiredXp} XP";
            var text = $"Level {i + 1}  {state}\nReward {rewardText}";

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

    private void EnsureRuntimeBackendClient()
    {
        if (backendClient != null)
        {
            return;
        }

        backendClient = GetComponent<MythwakeBackendClient>();
        if (backendClient == null)
        {
            backendClient = gameObject.AddComponent<MythwakeBackendClient>();
        }
    }

    private void EnsureRuntimeBackendUi()
    {
        if (shopPanel == null || backendStatusText != null)
        {
            return;
        }

        var panelObject = new GameObject("Backend Sync Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(shopPanel.transform, false);
        SetRuntimeRect(panelObject.GetComponent<RectTransform>(), new Vector2(0, -1215), new Vector2(860, 210), new Vector2(0.5f, 1f));

        var panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.1f, 0.13f, 0.2f, 0.96f);

        var header = CreateRuntimeText(panelObject.transform, "Backend Header", "Backend", 30, new Vector2(0, -32), new Vector2(790, 42));
        header.fontStyle = FontStyles.Bold;

        backendStatusText = CreateRuntimeText(panelObject.transform, "Backend Status Text", backendStatus, 22, new Vector2(0, -78), new Vector2(790, 62));
        backendStatusText.color = new Color(0.72f, 0.86f, 1f);
        backendStatusText.enableAutoSizing = true;
        backendStatusText.fontSizeMin = 16;
        backendStatusText.fontSizeMax = 22;

        backendHealthButton = CreateRuntimeButton(panelObject.transform, "Backend Health Button", "Ping", -384, -154, 86, 54);
        backendLoginButton = CreateRuntimeButton(panelObject.transform, "Backend Login Button", "Login", -288, -154, 86, 54);
        backendSyncButton = CreateRuntimeButton(panelObject.transform, "Backend Sync Button", "Sync", -192, -154, 86, 54);
        backendAfkButton = CreateRuntimeButton(panelObject.transform, "Backend AFK Button", "AFK", -96, -154, 86, 54);
        backendClockButton = CreateRuntimeButton(panelObject.transform, "Backend Clock Button", "Clock", 0, -154, 86, 54);
        backendDefinitionsButton = CreateRuntimeButton(panelObject.transform, "Backend Definitions Button", "Defs", 96, -154, 86, 54);
        backendSmokeButton = CreateRuntimeButton(panelObject.transform, "Backend Smoke Button", "Smoke", 192, -154, 86, 54);
        backendResetButton = CreateRuntimeButton(panelObject.transform, "Backend Reset Button", "Reset", 288, -154, 86, 54);
        backendModeButton = CreateRuntimeButton(panelObject.transform, "Backend Mode Button", "Local", 384, -154, 86, 54);
        backendModeText = backendModeButton.GetComponentInChildren<TMP_Text>();
    }

    private void EnsureRuntimeScreenLayout()
    {
        EnsureRuntimeTopBar();
        EnsureRuntimeDungeonsPanel();
        EnsureRuntimeDungeonsTab();
        EnsureRuntimeScreenBackdrops();
        EnsureRuntimeHomeActions();
        EnsureRuntimeMenuHeader();
        EnsureRuntimeHeroCardArt();
        EnsureRuntimeBottomNavbarArt();
        LayoutBottomNavigation();
        LayoutHomeScreen();
        LayoutBattleScreen();
        LayoutDungeonsScreen();
        LayoutHeroesScreen();
        LayoutGearScreen();
        LayoutSummonScreen();
        LayoutShopScreen();
        LayoutPrototypeTools();
        ApplyAfkInspiredTextSkin();
    }

    private void EnsureRuntimeTopBar()
    {
        if (topBarRoot != null)
        {
            return;
        }

        var parent = homePanel != null && homePanel.transform.parent != null ? homePanel.transform.parent : transform;
        var topBarObject = new GameObject("Mythwake Top Resource Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topBarObject.transform.SetParent(parent, false);
        topBarRoot = topBarObject.GetComponent<RectTransform>();
        SetRuntimeRect(topBarRoot, new Vector2(0, 0), new Vector2(900, 116), new Vector2(0.5f, 1f));
        topBarRoot.SetAsLastSibling();

        var topBarImage = topBarObject.GetComponent<Image>();
        topBarImage.color = new Color(0.24f, 0.12f, 0.055f, 0.98f);
        topBarImage.raycastTarget = false;

        CreateRuntimePanel(topBarObject.transform, "Top Avatar Ring", new Vector2(-390, -58), new Vector2(92, 92), new Color(0.86f, 0.61f, 0.22f, 0.32f));
        topAvatarImage = CreateRuntimeRawImage(topBarObject.transform, "Top Avatar", "hero_elowen", new Vector2(-390, -58), new Vector2(82, 82));

        topProfileText = CreateRuntimeText(topBarObject.transform, "Top Profile Text", "Mythwake", 24, new Vector2(-260, -20), new Vector2(260, 34));
        topProfileText.alignment = TextAlignmentOptions.Left;
        topProfileText.fontStyle = FontStyles.Bold;

        topPowerText = CreateRuntimeText(topBarObject.transform, "Top Power Text", "Power 0", 23, new Vector2(-260, -62), new Vector2(260, 40));
        topPowerText.alignment = TextAlignmentOptions.Left;
        topPowerText.color = new Color(1f, 0.86f, 0.36f);
        topPowerText.fontStyle = FontStyles.Bold;

        topCurrencyText = CreateRuntimeText(topBarObject.transform, "Top Currency Text", "Gold 0   Gems 0   Essence 0", 24, new Vector2(170, -38), new Vector2(520, 46));
        topCurrencyText.alignment = TextAlignmentOptions.Right;
        topCurrencyText.fontStyle = FontStyles.Bold;

        SetComponentActive(titleText, false);
        SetComponentActive(versionText, false);
        SetComponentActive(goldText, false);
        SetComponentActive(homeGoldText, false);
        SetComponentActive(gemsText, false);
        SetComponentActive(mythEssenceText, false);
    }

    private void EnsureRuntimeScreenBackdrops()
    {
        EnsureParchmentBackdrop(heroesPanel, "Heroes Parchment Backdrop");
        EnsureParchmentBackdrop(gearPanel, "Gear Parchment Backdrop");
        EnsureParchmentBackdrop(summonPanel, "Summon Parchment Backdrop");
        EnsureParchmentBackdrop(shopPanel, "Shop Parchment Backdrop");
    }

    private void EnsureParchmentBackdrop(GameObject panel, string name)
    {
        if (panel == null || panel.transform.Find(name) != null)
        {
            return;
        }

        var backdrop = CreateRuntimePanel(panel.transform, name, new Vector2(0, -548), new Vector2(840, 820), new Color(0.87f, 0.68f, 0.38f, 0.95f));
        backdrop.SetAsFirstSibling();

        var inner = CreateRuntimePanel(backdrop, "Inner Scroll Tint", new Vector2(0, -404), new Vector2(800, 780), new Color(1f, 0.85f, 0.55f, 0.68f));
        inner.SetAsFirstSibling();
    }

    private void EnsureRuntimeHomeActions()
    {
        if (homePanel == null || homeBeginButton != null)
        {
            return;
        }

        homeBeginButton = CreateRuntimeButton(homePanel.transform, "Home Begin Button", "Begin", 0, -805, 360, 76);
    }

    private void EnsureRuntimeMenuHeader()
    {
        if (shopPanel == null || menuHeaderText != null)
        {
            return;
        }

        menuHeaderText = CreateRuntimeText(shopPanel.transform, "Menu Header", "Quests & Systems", 34, new Vector2(0, -145), new Vector2(790, 48));
        menuHeaderText.fontStyle = FontStyles.Bold;
        menuHeaderText.color = new Color(0.28f, 0.14f, 0.055f);
    }

    private void EnsureRuntimeHeroCardArt()
    {
        if (heroSelectButtons == null || heroCardPortraits != null)
        {
            return;
        }

        heroCardPortraits = new RawImage[heroSelectButtons.Length];
        for (var i = 0; i < heroSelectButtons.Length; i++)
        {
            var button = heroSelectButtons[i];
            if (button == null)
            {
                continue;
            }

            var existing = button.transform.Find("Runtime Hero Portrait");
            if (existing != null)
            {
                heroCardPortraits[i] = existing.GetComponent<RawImage>();
                continue;
            }

            var portraitObject = new GameObject("Runtime Hero Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            portraitObject.transform.SetParent(button.transform, false);
            var portraitRect = portraitObject.GetComponent<RectTransform>();
            SetRuntimeRect(portraitRect, new Vector2(0, -14), new Vector2(78, 78), new Vector2(0.5f, 1f));
            portraitObject.transform.SetAsFirstSibling();

            var portrait = portraitObject.GetComponent<RawImage>();
            portrait.raycastTarget = false;
            portrait.color = Color.white;
            heroCardPortraits[i] = portrait;
        }
    }

    private void EnsureRuntimeBottomNavbarArt()
    {
        if (artBottomNavRoot != null)
        {
            return;
        }

        var navbarTexture = GetHomeNavbarTexture("navbar");
        if (navbarTexture == null)
        {
            return;
        }

        var tabParent = homeTabButton != null && homeTabButton.transform.parent != null ? homeTabButton.transform.parent : null;
        if (tabParent == null)
        {
            return;
        }

        var artObject = new GameObject("Mythwake Art Bottom Navbar", typeof(RectTransform));
        artObject.transform.SetParent(tabParent, false);
        artBottomNavRoot = artObject.GetComponent<RectTransform>();
        SetRuntimeRect(artBottomNavRoot, Vector2.zero, new Vector2(1080, 256), new Vector2(0.5f, 0.5f));
        artBottomNavRoot.SetAsLastSibling();

        CreateRuntimeRawImage(artBottomNavRoot, "Navbar Backplate", navbarTexture, Vector2.zero, new Vector2(1080, 256), new Vector2(0.5f, 0.5f));
        heroesNavButton = CreateNavbarButton(artBottomNavRoot, "Heroes Navbar Button", GetHomeNavbarTexture("heroes"), new Vector2(-390, 18), new Vector2(174, 194), out heroesNavImage);
        villageNavButton = CreateNavbarButton(artBottomNavRoot, "Village Navbar Button", GetHomeNavbarTexture("village"), new Vector2(-218, 12), new Vector2(170, 186), out villageNavImage);
        campaignNavButton = CreateTransparentNavbarButton(artBottomNavRoot, "Campaign Navbar Button", new Vector2(0, 18), new Vector2(286, 170));
        dungeonsNavButton = CreateNavbarButton(artBottomNavRoot, "Dungeons Navbar Button", GetHomeNavbarTexture("dungeons"), new Vector2(220, 12), new Vector2(171, 187), out dungeonsNavImage);
        summonNavButton = CreateNavbarButton(artBottomNavRoot, "Summon Navbar Button", GetHomeNavbarTexture("summon"), new Vector2(398, 18), new Vector2(183, 194), out summonNavImage);
    }

    private void EnsureRuntimeDungeonsPanel()
    {
        if (dungeonsPanel != null)
        {
            return;
        }

        var parent = battlePanel != null && battlePanel.transform.parent != null ? battlePanel.transform.parent : transform;
        dungeonsPanel = new GameObject("Dungeons Panel", typeof(RectTransform));
        dungeonsPanel.transform.SetParent(parent, false);

        var dungeonsRect = dungeonsPanel.GetComponent<RectTransform>();
        var sourceRect = battlePanel != null ? battlePanel.GetComponent<RectTransform>() : null;
        if (sourceRect != null)
        {
            dungeonsRect.anchorMin = sourceRect.anchorMin;
            dungeonsRect.anchorMax = sourceRect.anchorMax;
            dungeonsRect.pivot = sourceRect.pivot;
            dungeonsRect.anchoredPosition = sourceRect.anchoredPosition;
            dungeonsRect.sizeDelta = sourceRect.sizeDelta;
            dungeonsRect.offsetMin = sourceRect.offsetMin;
            dungeonsRect.offsetMax = sourceRect.offsetMax;
        }
        else
        {
            StretchRuntime(dungeonsRect, Vector2.zero);
        }

        dungeonsHeaderText = CreateRuntimeText(dungeonsPanel.transform, "Dungeons Header", "Dungeons", 36, new Vector2(0, -132), new Vector2(860, 54));
        dungeonsHeaderText.fontStyle = FontStyles.Bold;

        runtimeDungeonResultText = CreateRuntimeText(dungeonsPanel.transform, "Dungeon Result Text", "Dungeons are the active resource source.", 22, new Vector2(0, -475), new Vector2(760, 52));
        runtimeDungeonResultText.color = new Color(0.72f, 0.86f, 1f);
        runtimeDungeonResultText.enableAutoSizing = true;
        runtimeDungeonResultText.fontSizeMin = 16;
        runtimeDungeonResultText.fontSizeMax = 22;
    }

    private void EnsureRuntimeDungeonsTab()
    {
        if (dungeonsTabButton != null)
        {
            return;
        }

        var tabParent = homeTabButton != null && homeTabButton.transform.parent != null ? homeTabButton.transform.parent : null;
        if (tabParent == null)
        {
            return;
        }

        GameObject tabObject;
        if (shopTabButton != null)
        {
            tabObject = Instantiate(shopTabButton.gameObject, tabParent, false);
        }
        else
        {
            tabObject = new GameObject("Dungeons Tab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tabObject.transform.SetParent(tabParent, false);
        }

        tabObject.name = "Dungeons Tab";
        dungeonsTabButton = tabObject.GetComponent<Button>();
        dungeonsTabButton.onClick.RemoveAllListeners();
        SetButtonLabel(dungeonsTabButton, "Dungeons");
    }

    private void LayoutBottomNavigation()
    {
        var tabParent = homeTabButton != null && homeTabButton.transform.parent != null ? homeTabButton.transform.parent : null;
        if (tabParent != null)
        {
            bottomNavRoot = tabParent.GetComponent<RectTransform>();
            if (bottomNavRoot != null)
            {
                bottomNavRoot.sizeDelta = artBottomNavRoot != null ? new Vector2(1080, 256) : new Vector2(860, 118);
            }

            var navImage = tabParent.GetComponent<Image>();
            if (navImage != null)
            {
                navImage.color = artBottomNavRoot != null ? Color.clear : new Color(0.18f, 0.08f, 0.035f, 0.98f);
                navImage.raycastTarget = artBottomNavRoot == null;
            }
        }

        if (artBottomNavRoot != null)
        {
            SetRuntimeRect(artBottomNavRoot, Vector2.zero, new Vector2(1080, 256), new Vector2(0.5f, 0.5f));
            HideLegacyTab(homeTabButton);
            HideLegacyTab(battleTabButton);
            HideLegacyTab(dungeonsTabButton);
            HideLegacyTab(heroesTabButton);
            HideLegacyTab(gearTabButton);
            HideLegacyTab(summonTabButton);
            HideLegacyTab(shopTabButton);
            return;
        }

        var tabs = new[] { homeTabButton, battleTabButton, dungeonsTabButton, heroesTabButton, gearTabButton, summonTabButton, shopTabButton };
        var labels = new[] { "Map", "Battle", "Dungeon", "Heroes", "Gear", "Summon", "Menu" };
        const float spacing = 116f;
        const float startX = -348f;

        for (var i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null)
            {
                continue;
            }

            SetRuntimeRect(tabs[i].GetComponent<RectTransform>(), new Vector2(startX + spacing * i, 0), new Vector2(106, 92), new Vector2(0.5f, 0.5f));
            SetButtonLabel(tabs[i], labels[i]);
        }
    }

    private static void HideLegacyTab(Button button)
    {
        if (button != null)
        {
            button.gameObject.SetActive(false);
        }
    }

    private void LayoutHomeScreen()
    {
        MoveUiElement(homeStageText, homePanel, new Vector2(-270, -180), new Vector2(300, 76));
        MoveUiElement(homePowerText, homePanel, new Vector2(-270, -258), new Vector2(300, 44));
        MoveUiElement(nextGoalText, homePanel, new Vector2(0, -675), new Vector2(720, 86));
        MoveUiElement(offlineRewardText, homePanel, new Vector2(255, -760), new Vector2(300, 64));
        MoveUiElement(homeBeginButton, homePanel, new Vector2(0, -825), new Vector2(360, 76));
    }

    private void LayoutBattleScreen()
    {
        SetComponentActive(upgradeButton, false);

        MoveUiElement(damageText, battlePanel, new Vector2(0, -130), new Vector2(760, 34));
        MoveUiElement(autoAttackText, battlePanel, new Vector2(0, -166), new Vector2(760, 28));
        MoveUiElement(enemyText, battlePanel, new Vector2(0, -210), new Vector2(760, 64));
        MoveUiElement(enemyHpText, battlePanel, new Vector2(0, -270), new Vector2(760, 32));
        MoveUiElement(fightButton, battlePanel, new Vector2(0, -690), new Vector2(420, 76));
        MoveUiElement(dungeonResultText, battlePanel, new Vector2(0, -782), new Vector2(760, 72));
    }

    private void LayoutDungeonsScreen()
    {
        if (dungeonsPanel == null)
        {
            return;
        }

        MoveUiElement(goldDungeonButton, dungeonsPanel, new Vector2(0, -560), new Vector2(660, 76));
        MoveUiElement(essenceDungeonButton, dungeonsPanel, new Vector2(0, -653), new Vector2(660, 76));
        MoveUiElement(gearDungeonButton, dungeonsPanel, new Vector2(0, -746), new Vector2(660, 76));
    }

    private void LayoutHeroesScreen()
    {
        MoveUiElement(selectedHeroText, heroesPanel, new Vector2(0, -155), new Vector2(760, 90));
        LayoutHeroCards();
        SetTextArrayActive(teamSlotTexts, false);
        MoveUiElement(heroUpgradeButton, heroesPanel, new Vector2(-205, -845), new Vector2(330, 72));
        MoveUiElement(heroAscendButton, heroesPanel, new Vector2(205, -845), new Vector2(330, 72));
    }

    private void LayoutHeroCards()
    {
        if (heroSelectButtons == null)
        {
            return;
        }

        const float cardWidth = 132f;
        const float cardHeight = 128f;
        var xPositions = new[] { -304f, -152f, 0f, 152f, 304f };
        var yPositions = new[] { -292f, -435f, -578f };

        for (var i = 0; i < heroSelectButtons.Length; i++)
        {
            var x = xPositions[i % xPositions.Length];
            var y = yPositions[Mathf.Min(yPositions.Length - 1, i / xPositions.Length)];
            MoveUiElement(heroSelectButtons[i], heroesPanel, new Vector2(x, y), new Vector2(cardWidth, cardHeight));

            if (heroCardTexts != null && i < heroCardTexts.Length)
            {
                MoveUiElement(heroCardTexts[i], heroSelectButtons[i] != null ? heroSelectButtons[i].gameObject : null, new Vector2(0, -86), new Vector2(122, 36));
            }
        }
    }

    private void LayoutGearScreen()
    {
        MoveUiElement(equipmentSummaryText, gearPanel, new Vector2(0, -485), new Vector2(760, 88));
        MoveUiElement(weaponUpgradeButton, gearPanel, new Vector2(-210, -575), new Vector2(320, 68));
        MoveUiElement(armorUpgradeButton, gearPanel, new Vector2(210, -575), new Vector2(320, 68));
        MoveUiElement(accessorySummaryText, gearPanel, new Vector2(0, -665), new Vector2(760, 62));
        MoveUiElement(accessorySelectedText, gearPanel, new Vector2(0, -735), new Vector2(760, 70));
        MoveUiElement(accessoryInventoryText, gearPanel, new Vector2(0, -805), new Vector2(760, 64));
        MoveUiElement(accessoryPreviousSlotButton, gearPanel, new Vector2(-320, -878), new Vector2(130, 54));
        MoveUiElement(accessoryNextSlotButton, gearPanel, new Vector2(320, -878), new Vector2(130, 54));
        MoveUiElement(accessoryPreviousRarityButton, gearPanel, new Vector2(-320, -940), new Vector2(130, 54));
        MoveUiElement(accessoryNextRarityButton, gearPanel, new Vector2(320, -940), new Vector2(130, 54));
        MoveUiElement(accessoryEquipButton, gearPanel, new Vector2(-215, -1010), new Vector2(205, 58));
        MoveUiElement(accessoryLevelButton, gearPanel, new Vector2(0, -1010), new Vector2(205, 58));
        MoveUiElement(accessoryFuseButton, gearPanel, new Vector2(215, -1010), new Vector2(205, 58));
    }

    private void LayoutSummonScreen()
    {
        MoveUiElement(summonCostText, summonPanel, new Vector2(0, -690), new Vector2(760, 42));
        MoveUiElement(summonRatesText, summonPanel, new Vector2(0, -748), new Vector2(760, 70));
        MoveUiElement(summonResultText, summonPanel, new Vector2(0, -828), new Vector2(760, 72));
        MoveUiElement(summonCountText, summonPanel, new Vector2(0, -905), new Vector2(760, 36));
        MoveUiElement(summonButton, summonPanel, new Vector2(0, -960), new Vector2(360, 66));
    }

    private void LayoutShopScreen()
    {
        MoveUiElement(menuHeaderText, shopPanel, new Vector2(0, -145), new Vector2(790, 48));

        if (dailyMissionButtons != null)
        {
            for (var i = 0; i < dailyMissionButtons.Length; i++)
            {
                MoveUiElement(dailyMissionButtons[i], shopPanel, new Vector2(0, -230 - i * 82), new Vector2(720, 70));
                if (dailyMissionTexts != null && i < dailyMissionTexts.Length)
                {
                    MoveUiElement(dailyMissionTexts[i], dailyMissionButtons[i] != null ? dailyMissionButtons[i].gameObject : null, new Vector2(0, -8), new Vector2(690, 52));
                }
            }
        }

        MoveUiElement(battlePassProgressText, shopPanel, new Vector2(0, -505), new Vector2(720, 64));
        if (battlePassRewardButtons != null)
        {
            const float spacing = 142f;
            const float startX = -284f;
            for (var i = 0; i < battlePassRewardButtons.Length; i++)
            {
                MoveUiElement(battlePassRewardButtons[i], shopPanel, new Vector2(startX + i * spacing, -600), new Vector2(126, 86));
                if (battlePassRewardTexts != null && i < battlePassRewardTexts.Length)
                {
                    MoveUiElement(battlePassRewardTexts[i], battlePassRewardButtons[i] != null ? battlePassRewardButtons[i].gameObject : null, new Vector2(0, -8), new Vector2(112, 68));
                }
            }
        }

        var backendPanel = backendStatusText != null && backendStatusText.transform.parent != null
            ? backendStatusText.transform.parent.GetComponent<RectTransform>()
            : null;
        MoveUiElement(backendPanel, shopPanel, new Vector2(0, -735), new Vector2(820, 200));
    }

    private void LayoutPrototypeTools()
    {
        MoveUiElement(resetButton, shopPanel, new Vector2(0, -965), new Vector2(300, 58));
        MoveUiElement(debugGoldButton, shopPanel, new Vector2(-315, -1036), new Vector2(150, 50));
        MoveUiElement(debugEssenceButton, shopPanel, new Vector2(-105, -1036), new Vector2(150, 50));
        MoveUiElement(debugGemsButton, shopPanel, new Vector2(105, -1036), new Vector2(150, 50));
        MoveUiElement(debugAccessoryButton, shopPanel, new Vector2(315, -1036), new Vector2(150, 50));
    }

    private void ApplyAfkInspiredTextSkin()
    {
        activeTabColor = new Color(0.75f, 0.42f, 0.16f, 1f);
        inactiveTabColor = new Color(0.16f, 0.08f, 0.035f, 1f);

        SetTextColor(homeGoldText, new Color(0.23f, 0.12f, 0.045f));
        SetTextColor(gemsText, new Color(0.23f, 0.12f, 0.045f));
        SetTextColor(mythEssenceText, new Color(0.23f, 0.12f, 0.045f));
        SetTextColor(homeStageText, new Color(0.23f, 0.12f, 0.045f));
        SetTextColor(homePowerText, new Color(0.23f, 0.12f, 0.045f));
        SetTextColor(nextGoalText, new Color(0.23f, 0.12f, 0.045f));
        SetTextColor(offlineRewardText, new Color(0.23f, 0.12f, 0.045f));
        SetTextColor(selectedHeroText, new Color(0.24f, 0.13f, 0.055f));
        SetTextColor(equipmentSummaryText, new Color(0.24f, 0.13f, 0.055f));
        SetTextColor(accessorySummaryText, new Color(0.24f, 0.13f, 0.055f));
        SetTextColor(accessorySelectedText, new Color(0.24f, 0.13f, 0.055f));
        SetTextColor(accessoryInventoryText, new Color(0.24f, 0.13f, 0.055f));
        SetTextColor(battlePassProgressText, new Color(0.24f, 0.13f, 0.055f));
        SetTextArrayColor(heroCardTexts, Color.white);
        SetTextArrayColor(dailyMissionTexts, new Color(0.24f, 0.13f, 0.055f));
        SetTextArrayColor(battlePassRewardTexts, new Color(0.24f, 0.13f, 0.055f));
    }

    private static void MoveUiElement(Component component, GameObject parent, Vector2 anchoredPosition, Vector2 size)
    {
        if (component == null || parent == null)
        {
            return;
        }

        component.transform.SetParent(parent.transform, false);
        var rectTransform = component.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetRuntimeRect(rectTransform, anchoredPosition, size, new Vector2(0.5f, 1f));
        }
    }

    private static void SetComponentActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
        }
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        var text = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (text == null)
        {
            return;
        }

        text.text = label;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10;
        text.fontSizeMax = 20;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void RefreshHeroCardVisuals()
    {
        if (heroSelectButtons == null)
        {
            return;
        }

        for (var i = 0; i < heroSelectButtons.Length; i++)
        {
            var button = heroSelectButtons[i];
            if (button == null)
            {
                continue;
            }

            var frame = button.GetComponent<Image>();
            if (frame != null)
            {
                frame.color = i == selectedHeroIndex
                    ? new Color(1f, 0.78f, 0.25f, 1f)
                    : new Color(0.72f, 0.42f, 0.18f, 1f);
            }

            if (heroCardPortraits == null || i >= heroCardPortraits.Length || heroCardPortraits[i] == null)
            {
                continue;
            }

            var hero = GetHeroDefinition(Mathf.Clamp(i, 0, HeroCount - 1));
            heroCardPortraits[i].texture = LoadRuntimeTexture($"hero_{hero.name.ToLowerInvariant()}");
            heroCardPortraits[i].color = Color.white;
        }
    }

    private void RefreshTopBarUi()
    {
        if (topBarRoot == null)
        {
            return;
        }

        topBarRoot.SetAsLastSibling();

        var hero = GetHeroDefinition(selectedHeroIndex);
        if (topProfileText != null)
        {
            topProfileText.text = $"Mythwake  Lv {heroLevels[selectedHeroIndex]}";
        }

        if (topPowerText != null)
        {
            topPowerText.text = $"Power {GetTeamPower()}";
        }

        if (topCurrencyText != null)
        {
            topCurrencyText.text = $"Gold {FormatCompactNumber(gold)}    Gems {FormatCompactNumber(gems)}    Essence {FormatCompactNumber(mythEssence)}";
        }

        if (topAvatarImage != null)
        {
            topAvatarImage.texture = LoadRuntimeTexture($"hero_{hero.name.ToLowerInvariant()}");
        }
    }

    private static string FormatCompactNumber(int value)
    {
        if (value >= 1000000000)
        {
            return $"{value / 1000000000f:0.#}B";
        }

        if (value >= 1000000)
        {
            return $"{value / 1000000f:0.#}M";
        }

        if (value >= 1000)
        {
            return $"{value / 1000f:0.#}K";
        }

        return value.ToString();
    }

    private static void SetTextArrayActive(TMP_Text[] texts, bool active)
    {
        if (texts == null)
        {
            return;
        }

        for (var i = 0; i < texts.Length; i++)
        {
            SetComponentActive(texts[i], active);
        }
    }

    private static void SetTextColor(TMP_Text text, Color color)
    {
        if (text != null)
        {
            text.color = color;
        }
    }

    private static void SetTextArrayColor(TMP_Text[] texts, Color color)
    {
        if (texts == null)
        {
            return;
        }

        for (var i = 0; i < texts.Length; i++)
        {
            SetTextColor(texts[i], color);
        }
    }

    private void EnsureRuntimeArtUi()
    {
        if (runtimeArt == null)
        {
            runtimeArt = new MythwakeRuntimeArtPresenter();
        }

        runtimeArt.Ensure(homePanel, battlePanel, dungeonsPanel, heroesPanel, gearPanel, summonPanel, shopPanel);
        runtimeArt.ApplyButtonStyle(
            fightButton,
            goldDungeonButton,
            essenceDungeonButton,
            gearDungeonButton,
            upgradeButton,
            heroUpgradeButton,
            heroAscendButton,
            weaponUpgradeButton,
            armorUpgradeButton,
            accessoryEquipButton,
            accessoryLevelButton,
            accessoryFuseButton,
            summonButton,
            resetButton,
            homeTabButton,
            battleTabButton,
            dungeonsTabButton,
            heroesTabButton,
            gearTabButton,
            summonTabButton,
            shopTabButton,
            backendHealthButton,
            backendLoginButton,
            backendSyncButton,
            backendAfkButton,
            backendClockButton,
            backendDefinitionsButton,
            backendSmokeButton,
            backendResetButton,
            backendModeButton,
            debugGoldButton,
            debugEssenceButton,
            debugGemsButton,
            debugAccessoryButton,
            homeBeginButton);
        runtimeArt.ApplyButtonStyle(heroSelectButtons);
        runtimeArt.ApplyButtonStyle(dailyMissionButtons);
        runtimeArt.ApplyButtonStyle(battlePassRewardButtons);
        runtimeArt.ApplyButtonStyle("ui_button_blue", fightButton, summonButton, homeBeginButton);
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

    private static RectTransform CreateRuntimePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 rectSize, Color color)
    {
        var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        var rectTransform = panelObject.GetComponent<RectTransform>();
        SetRuntimeRect(rectTransform, anchoredPosition, rectSize, new Vector2(0.5f, 1f));

        var image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rectTransform;
    }

    private static RawImage CreateRuntimeRawImage(Transform parent, string name, string textureName, Vector2 anchoredPosition, Vector2 rectSize)
    {
        return CreateRuntimeRawImage(parent, name, LoadRuntimeTexture(textureName), anchoredPosition, rectSize, new Vector2(0.5f, 1f));
    }

    private static RawImage CreateRuntimeRawImage(Transform parent, string name, Texture texture, Vector2 anchoredPosition, Vector2 rectSize, Vector2 anchor)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);
        var rectTransform = imageObject.GetComponent<RectTransform>();
        SetRuntimeRect(rectTransform, anchoredPosition, rectSize, anchor);

        var rawImage = imageObject.GetComponent<RawImage>();
        rawImage.texture = texture;
        rawImage.raycastTarget = false;
        rawImage.color = Color.white;
        return rawImage;
    }

    private static Button CreateNavbarButton(Transform parent, string name, Texture2D texture, Vector2 anchoredPosition, Vector2 rectSize, out RawImage image)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRuntimeRect(buttonObject.GetComponent<RectTransform>(), anchoredPosition, rectSize, new Vector2(0.5f, 0.5f));

        image = buttonObject.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = true;
        image.color = Color.white;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Button CreateTransparentNavbarButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 rectSize)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRuntimeRect(buttonObject.GetComponent<RectTransform>(), anchoredPosition, rectSize, new Vector2(0.5f, 0.5f));

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Texture2D LoadRuntimeTexture(string textureName)
    {
        if (string.IsNullOrWhiteSpace(textureName))
        {
            return null;
        }

        var texture = Resources.Load<Texture2D>($"Mythwake/Art/Runtime/{textureName}");
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        var sprite = Resources.Load<Sprite>($"Mythwake/Art/Runtime/{textureName}");
        if (sprite == null)
        {
            return null;
        }

        sprite.texture.filterMode = FilterMode.Point;
        return sprite.texture;
    }

    private Texture2D GetHomeNavbarTexture(string textureName)
    {
        Texture2D texture = textureName switch
        {
            "navbar" => homeNavbarTexture,
            "village" => homeNavbarVillageTexture,
            "dungeons" => homeNavbarDungeonsTexture,
            "heroes" => homeNavbarHeroesTexture,
            "summon" => homeNavbarSummonTexture,
            _ => null
        };

        if (texture != null)
        {
            return texture;
        }

        var resourcesTexture = Resources.Load<Texture2D>($"Mythwake/UI/HomeScreen/BottomNavbar/{textureName}");
        if (resourcesTexture != null)
        {
            resourcesTexture.filterMode = FilterMode.Bilinear;
            return resourcesTexture;
        }

#if UNITY_EDITOR
        var fileName = textureName switch
        {
            "navbar" => "navbar.png",
            "village" => "village_btn.png",
            "dungeons" => "dungeons_btn.png",
            "heroes" => "heroes_btn.png",
            "summon" => "summon_btn.png",
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(fileName))
        {
            var editorTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Mythwake/UI/Home Screen/bottom_navbar/{fileName}");
            if (editorTexture != null)
            {
                editorTexture.filterMode = FilterMode.Bilinear;
                return editorTexture;
            }
        }
#endif

        return null;
    }

    private void RefreshBackendUi()
    {
        if (backendStatusText != null)
        {
            backendStatusText.text = $"{backendStatus}\n{BackendSessionLabel()}";
        }

        if (backendModeText != null)
        {
            backendModeText.text = backendGameplayEnabled ? "Server" : "Local";
        }

        SetBackendButtonsInteractable(!backendRequestInProgress);
    }

    private void SetBackendStatus(string status)
    {
        backendStatus = string.IsNullOrWhiteSpace(status) ? "Backend: no status" : status;
        if (backendStatusText != null)
        {
            backendStatusText.text = $"{backendStatus}\n{BackendSessionLabel()}";
        }
    }

    private string BackendSessionLabel()
    {
        var mode = backendGameplayEnabled ? "Server" : "Local";
        var sessionLabel = backendClient != null && backendClient.HasSession ? $"Mode {mode}  Logged: {backendClient.PlayerId}" : $"Mode {mode}  Logged: no";
        if (backendStateRevision > 0)
        {
            sessionLabel = $"{sessionLabel}  Rev {backendStateRevision}";
        }

        if (hasBackendDefinitions)
        {
            sessionLabel = $"{sessionLabel}  Defs {ShortHash(backendDefinitions.contentHash)}";
        }

        return sessionLabel;
    }

    private void SetBackendButtonsInteractable(bool interactable)
    {
        if (backendHealthButton != null)
        {
            backendHealthButton.interactable = interactable;
        }

        if (backendLoginButton != null)
        {
            backendLoginButton.interactable = interactable;
        }

        if (backendSyncButton != null)
        {
            backendSyncButton.interactable = interactable;
        }

        if (backendAfkButton != null)
        {
            backendAfkButton.interactable = interactable && backendGameplayEnabled;
        }

        if (backendClockButton != null)
        {
            backendClockButton.interactable = interactable;
        }

        if (backendDefinitionsButton != null)
        {
            backendDefinitionsButton.interactable = interactable;
        }

        if (backendSmokeButton != null)
        {
            backendSmokeButton.interactable = interactable;
        }

        if (backendResetButton != null)
        {
            backendResetButton.interactable = interactable;
        }

        if (backendModeButton != null)
        {
            backendModeButton.interactable = interactable;
        }
    }

    private static TMP_Text CreateRuntimeText(Transform parent, string name, string value, int size, Vector2 anchoredPosition, Vector2 rectSize)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        SetRuntimeRect(textObject.GetComponent<RectTransform>(), anchoredPosition, rectSize, new Vector2(0.5f, 1f));

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static Button CreateRuntimeButton(Transform parent, string name, string label, float xPosition, float yPosition, float width, float height)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRuntimeRect(buttonObject.GetComponent<RectTransform>(), new Vector2(xPosition, yPosition), new Vector2(width, height), new Vector2(0.5f, 1f));

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.17f, 0.39f, 0.72f, 0.98f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 22;
        text.fontSizeMin = 14;
        text.fontSizeMax = 22;
        text.enableAutoSizing = true;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        StretchRuntime(text.rectTransform, new Vector2(16, 8));

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
