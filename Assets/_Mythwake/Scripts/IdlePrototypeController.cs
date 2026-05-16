using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IdlePrototypeController : MonoBehaviour, IMythwakePlayerStateService, IMythwakePlayerSnapshotService, IMythwakeDefinitionService, IMythwakeEconomyService, IMythwakeBattleService, IMythwakeSummonService, IMythwakeInventoryService, IMythwakeProgressionService, IMythwakeMissionService
{
    public const string PrototypeVersion = "0.2.74";
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
        public int critChancePercent;
        public int accuracyPercent;
        public int defense;

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
            int ascensionHealth,
            int critChancePercent,
            int accuracyPercent,
            int defense)
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
            this.critChancePercent = critChancePercent;
            this.accuracyPercent = accuracyPercent;
            this.defense = defense;
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
        public int criticalHits;
        public int missedHits;
    }

    private struct FightVisualUnitState
    {
        public Vector2 position;
        public Vector2 lockedMeleePosition;
        public int targetIndex;
        public int lockedMeleeTargetIndex;
        public float nextAttackTime;
        public float attackStartedAt;
        public bool hasLockedMeleePosition;
        public bool isMoving;
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
        public string promoText;
        public string costCurrencyId;
        public int costAmount;
        public int[] featuredHeroIndexes;
        public SummonRateDefinition[] rates;

        public SummonBannerDefinition(string bannerId, string displayName, string promoText, string costCurrencyId, int costAmount, int[] featuredHeroIndexes, SummonRateDefinition[] rates)
        {
            this.bannerId = bannerId;
            this.displayName = displayName;
            this.promoText = promoText;
            this.costCurrencyId = costCurrencyId;
            this.costAmount = costAmount;
            this.featuredHeroIndexes = featuredHeroIndexes;
            this.rates = rates;
        }
    }

    private struct InventoryItemViewData
    {
        public string displayName;
        public string detail;
        public string description;
        public string statsText;
        public string countText;
        public string iconTextureName;
        public Color frameColor;

        public InventoryItemViewData(string displayName, string detail, string description, string statsText, string countText, string iconTextureName, Color frameColor)
        {
            this.displayName = displayName;
            this.detail = detail;
            this.description = description;
            this.statsText = statsText;
            this.countText = countText;
            this.iconTextureName = iconTextureName;
            this.frameColor = frameColor;
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

    private enum BattleFlowMode
    {
        Formation,
        Fight,
        Result
    }

    private enum BattleTargetMode
    {
        Campaign,
        Dungeon
    }

    private enum InventoryTabMode
    {
        Misc,
        Gear,
        All
    }

    private enum HeroesTabMode
    {
        Hero,
        SetTeam
    }

    private enum HeroSortDirection
    {
        Descending,
        Ascending
    }

    private enum HeroAttackTypeFilter
    {
        All,
        Melee,
        Ranged
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
        public float afkRewardStoredSeconds;
        public int[] heroLevels;
        public int[] heroShards;
        public int[] heroAscensions;
        public int[] formationSlotHeroIndices;
        public bool autoContinueFightsEnabled;
        public int[] heroWeaponLevels;
        public int[] heroArmorLevels;
        public int[] heroEquippedAccessoryRarities;
        public int[] heroEquippedAccessoryLevels;
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
    private const string HeroWeaponLevelKeyPrefix = "Mythwake.Prototype.HeroEquipment.WeaponLevel.";
    private const string HeroArmorLevelKeyPrefix = "Mythwake.Prototype.HeroEquipment.ArmorLevel.";
    private const string HeroEquippedAccessoryRarityKeyPrefix = "Mythwake.Prototype.HeroAccessory.EquippedRarity.";
    private const string HeroEquippedAccessoryLevelKeyPrefix = "Mythwake.Prototype.HeroAccessory.EquippedLevel.";
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
    private const int HeroCount = 6;
    private const int DailyMissionCount = 3;
    private const int BattlePassRewardCount = 5;
    private const int AccessorySlotCount = 5;
    private const int AccessoryRarityCount = 5;
    private const int SummonCarouselCardCount = 3;
    private const int SummonFeaturedHeroCount = 3;
    private const int SummonCarouselHeroSlotsPerCard = 2;
    private const int InventoryGridSlotCount = 30;
    private const int MaxSummonPullCount = 300;
    private const int SummonAutoStepCount = 10;
    private const int BattlePassXpPerDailyClaim = 40;
    private const int SummonCost = 35;
    private const int StarterGems = 35;
    private const int StarterMythEssence = 20;
    private const float OfflineGoldRewardRate = 0.5f;
    private const int AfkRewardMaxSeconds = 24 * 60 * 60;
    private const float AfkRewardAutosaveSeconds = 30f;
    private const int DefaultCombatDurationSeconds = 30;
    private const float FightUltimateCinematicSeconds = 0.9f;
    private const float FightUltimateWorldSlowScale = 0.18f;
    private const int FightAutoAttackManaGain = 2;
    private const float WarriorDamageBonusRate = 0.06f;
    private const float MageDamageBonusRate = 0.1f;
    private const float TankDamageReductionRate = 0.18f;
    private const float SupportHealRate = 0.04f;
    private const float RangerExecuteThresholdRate = 0.12f;
    private const float CritDamageMultiplier = 1.5f;
    private const int StarterEquipmentLevel = 1;
    private const int CampaignMilestoneInterval = 5;
    private const int DungeonBonusInterval = 5;
    private const int DungeonSetProgressGoal = 9;
    private const float DungeonBossHpMultiplier = 1.8f;
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
        new HeroDefinition("hero_astra", "Astra", WarriorRoleId, "Warrior", EpicRarityId, "Epic", 18, 5, 150, 28, 7, 25, 15, 11, 70, 12, 92, 8),
        new HeroDefinition("hero_borin", "Borin", TankRoleId, "Tank", RareRarityId, "Rare", 10, 3, 230, 42, 10, 20, 15, 8, 55, 5, 88, 24),
        new HeroDefinition("hero_cyra", "Cyra", MageRoleId, "Mage", EpicRarityId, "Epic", 22, 7, 110, 20, 7, 25, 15, 11, 70, 15, 90, 6),
        new HeroDefinition("hero_dante", "Dante", RangerRoleId, "Ranger", RareRarityId, "Rare", 20, 6, 125, 23, 10, 20, 15, 8, 55, 18, 95, 8),
        new HeroDefinition("hero_elowen", "Elowen", SupportRoleId, "Support", LegendaryRarityId, "Legendary", 12, 4, 165, 34, 5, 30, 15, 14, 90, 8, 90, 14),
        new HeroDefinition("hero_ravik", "Ravik", MageRoleId, "Mage", EpicRarityId, "Epic", 24, 7, 118, 22, 1, 25, 15, 12, 70, 17, 91, 7)
    };

    private static readonly string[] CampaignEnemyCombatTextureNames =
    {
        "enemy_rat",
        "enemy_bat",
        "enemy_slime",
        "enemy_canine",
        "enemy_golem",
        "enemy_dragon"
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
    private static readonly string[] GoldDungeonBattleMapTextureNames = { "gold_dungeon_battle_01", "gold_dungeon_battle_02" };
    private static readonly string[] EssenceDungeonBattleMapTextureNames = { "essence_dungeon_battle_01", "essence_dungeon_battle_02" };
    private static readonly string[] GearDungeonBattleMapTextureNames = { "equipment_dungeon_battle_01", "equipment_dungeon_battle_02" };
    private static readonly CampaignBalanceDefinition CampaignBalance = new CampaignBalanceDefinition(90, 46f, 1.17f, 10, 5.8f, 1.16f, 12, 1.6f, 25, 1.23f, 1.15f);

    private static readonly SummonBannerDefinition HeroShardBanner = new SummonBannerDefinition(
        "hero_shard_standard",
        "Awaken Heroes",
        "First x10 pack has a 10% discount",
        GemsCurrencyId,
        SummonCost,
        new[] { 4, 2, 0 },
        new[]
        {
            new SummonRateDefinition(LegendaryRarityId, 10, new[] { 4 }),
            new SummonRateDefinition(EpicRarityId, 45, new[] { 0, 2, 5 }),
            new SummonRateDefinition(RareRarityId, 100, new[] { 1, 3 })
        });

    private static readonly SummonBannerDefinition[] LocalSummonBanners =
    {
        HeroShardBanner,
        new SummonBannerDefinition(
            "hero_shard_vanguard",
            "Vanguard Oath",
            "Higher Epic chance for frontline teams",
            GemsCurrencyId,
            SummonCost,
            new[] { 0, 3, 1 },
            new[]
            {
                new SummonRateDefinition(LegendaryRarityId, 6, new[] { 4 }),
                new SummonRateDefinition(EpicRarityId, 58, new[] { 0 }),
                new SummonRateDefinition(RareRarityId, 100, new[] { 1, 3 })
            }),
        new SummonBannerDefinition(
            "hero_shard_mystic",
            "Mystic Bloom",
            "Better Legendary odds for support and mages",
            GemsCurrencyId,
            SummonCost,
            new[] { 4, 2, 0 },
            new[]
            {
                new SummonRateDefinition(LegendaryRarityId, 15, new[] { 4 }),
                new SummonRateDefinition(EpicRarityId, 50, new[] { 2, 0, 5 }),
                new SummonRateDefinition(RareRarityId, 100, new[] { 1, 3 })
            })
    };

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
    [SerializeField] private int[] heroWeaponLevels = new int[HeroCount];
    [SerializeField] private int[] heroArmorLevels = new int[HeroCount];
    [NonSerialized] private int backendWeaponLevel;
    [NonSerialized] private int backendArmorLevel;
    [NonSerialized] private int backendTeamPower;
    [NonSerialized] private int backendTeamAttack;
    [NonSerialized] private int backendTeamHealth;
    [SerializeField] private int selectedAccessorySlot;
    [SerializeField] private int selectedAccessoryRarity;
    [SerializeField] private int[] equippedAccessoryRarities = new int[AccessorySlotCount];
    [SerializeField] private int[] equippedAccessoryLevels = new int[AccessorySlotCount];
    [SerializeField] private int[] heroEquippedAccessoryRarities = new int[HeroCount * AccessorySlotCount];
    [SerializeField] private int[] heroEquippedAccessoryLevels = new int[HeroCount * AccessorySlotCount];
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
    [SerializeField] private bool autoAttackEnabled;
    [SerializeField] private float autoAttackInterval = 1f;
    [SerializeField] private float afkRewardStoredSeconds;

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
    [SerializeField] private string playerDisplayName = "Player";
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

    [Header("Currency Icons")]
    [SerializeField] private Texture2D expShardIconTexture;
    [SerializeField] private Texture2D goldCoinIconTexture;
    [SerializeField] private Texture2D mythicGemIconTexture;

    [Header("Home Screen Art")]
    [SerializeField] private Texture2D homeTopbarFrameTexture;
    [SerializeField] private Texture2D homeBattleButtonTexture;
    [SerializeField] private Texture2D homeQuestButtonTexture;
    [SerializeField] private Texture2D homeRewardsButtonTexture;
    [SerializeField] private Texture2D homeFastRewardsButtonTexture;
    [SerializeField] private Texture2D homeTreasureChestButtonTexture;
    [SerializeField] private Texture2D homeShopButtonTexture;
    [SerializeField] private Texture2D homeStageLevelBadgeTexture;
    [SerializeField] private Texture2D homeStageModeBadgeTexture;
    [SerializeField] private Texture2D homeStageExtraBadgeTexture;
    [SerializeField] private Texture2D homeWorldMapButtonTexture;
    [SerializeField] private Texture2D homeChatButtonTexture;
    [SerializeField] private Texture2D homePowerIconTexture;

    private float autoAttackTimer;
    private int lastOfflineGoldReward;
    private int lastOfflineReward;
    private int lastOfflineSeconds;
    private bool lastOfflineRewardIsServer;
    private float afkRewardAutosaveTimer;
    private AppScreen activeScreen = AppScreen.Home;
    private bool backendRequestInProgress;
    private bool backendLifecycleFlushInProgress;
    private bool campaignFightInProgress;
    private int selectedCampaignStage = 1;
    private BattleFlowMode battleFlowMode = BattleFlowMode.Formation;
    private BattleTargetMode battleTargetMode = BattleTargetMode.Campaign;
    private InventoryTabMode selectedInventoryTab = InventoryTabMode.All;
    private int selectedInventoryItemIndex = -1;
    private string selectedDungeonId = GoldDungeonDefinition.dungeonId;
    private string selectedDungeonBattleMapTextureName;
    private string backendStatus = "Backend: local prototype mode";
    private long backendStateRevision;
    private MythwakeDefinitionSnapshotDto backendDefinitions;
    private bool hasBackendDefinitions;
    private MythwakeRuntimeArtPresenter runtimeArt;
    private TMP_Text dungeonsHeaderText;
    private TMP_Text runtimeDungeonResultText;
    private RawImage goldDungeonBannerImage;
    private RawImage essenceDungeonBannerImage;
    private RawImage gearDungeonBannerImage;
    private RawImage goldDungeonBossImage;
    private RawImage essenceDungeonBossImage;
    private RawImage gearDungeonBossImage;
    private TMP_Text goldDungeonTitleText;
    private TMP_Text essenceDungeonTitleText;
    private TMP_Text gearDungeonTitleText;
    private TMP_Text goldDungeonProgressText;
    private TMP_Text essenceDungeonProgressText;
    private TMP_Text gearDungeonProgressText;
    private RectTransform topBarRoot;
    private RectTransform bottomNavRoot;
    private TMP_Text topGemAmountText;
    private TMP_Text topGoldAmountText;
    private TMP_Text topPlayerNameText;
    private TMP_Text topPowerText;
    private TMP_Text heroEssenceAmountText;
    private RawImage topGemIconImage;
    private RawImage topGoldIconImage;
    private RawImage topPowerIconImage;
    private RawImage heroEssenceIconImage;
    private RawImage topbarFrameImage;
    private RectTransform homeActionRoot;
    private TMP_Text homeStageLevelBadgeText;
    private TMP_Text homeStageModeBadgeText;
    private Button homeBeginButton;
    private Button homeQuestButton;
    private Button homeRewardsButton;
    private Button homeTreasureChestButton;
    private Button homeShopButton;
    private Button homeWorldMapButton;
    private Button homeChatButton;
    private Button topGemPlusButton;
    private Button homeShortcutToggleButton;
    private Button homeLeftShortcutToggleButton;
    private TMP_Text homeShortcutToggleText;
    private TMP_Text homeLeftShortcutToggleText;
    private RectTransform homeCampaignMapRoot;
    private RectTransform campaignStagePreviewRoot;
    private TMP_Text campaignStagePreviewText;
    private Button[] campaignStageButtons;
    private TMP_Text[] campaignStageButtonTexts;
    private RawImage[] campaignStageButtonIcons;
    private Image[] campaignStageButtonFrames;
    private RectTransform chatPopupRoot;
    private Button chatCloseButton;
    private RectTransform homeLeftShortcutShadow;
    private RectTransform homeRightShortcutShadow;
    private bool homeShortcutsExpanded;
    private RectTransform inventoryPopupRoot;
    private RectTransform fastRewardsPopupRoot;
    private RectTransform inventoryGridRoot;
    private RectTransform inventoryDetailRoot;
    private TMP_Text inventoryPopupText;
    private TMP_Text inventoryDetailTitleText;
    private TMP_Text inventoryDetailDescriptionText;
    private TMP_Text inventoryDetailStatsText;
    private TMP_Text fastRewardsPopupText;
    private Button inventoryCloseButton;
    private Button inventoryDetailCloseButton;
    private Button inventoryMiscTabButton;
    private Button inventoryGearTabButton;
    private Button inventoryAllTabButton;
    private Button fastRewardsCloseButton;
    private Button fastRewardsRedeemButton;
    private RectTransform[] inventorySlotRoots;
    private Button[] inventorySlotButtons;
    private Image[] inventorySlotFrames;
    private RawImage[] inventorySlotIcons;
    private RawImage inventoryDetailIcon;
    private Image inventoryDetailFrame;
    private TMP_Text[] inventorySlotCountTexts;
    private TMP_Text[] inventorySlotNameTexts;
    private TMP_Text[] inventorySlotDetailTexts;
    private TMP_Text menuHeaderText;
    private HeroesTabMode heroesTabMode = HeroesTabMode.Hero;
    private HeroSortDirection heroSortDirection = HeroSortDirection.Descending;
    private HeroAttackTypeFilter heroAttackTypeFilter = HeroAttackTypeFilter.All;
    private Button heroRosterTabButton;
    private Button heroSetTeamTabButton;
    private TMP_Text heroRosterTabText;
    private TMP_Text heroSetTeamTabText;
    private Button heroSortToggleButton;
    private TMP_Text heroSortToggleText;
    private Button heroAttackTypeFilterButton;
    private TMP_Text heroAttackTypeFilterText;
    private RectTransform heroCleanBackdropRoot;
    private RectTransform heroRosterFilterRoot;
    private TMP_Text heroRosterCountText;
    private RectTransform heroSubTabRoot;
    private Button heroAutoSetTeamButton;
    private TMP_Text heroTeamHintText;
    private RectTransform heroTeamRoot;
    private RawImage[] heroTeamSlotPortraits;
    private TMP_Text[] heroTeamSlotTexts;
    private Image[] heroTeamSlotFrames;
    private Button[] heroTeamSlotButtons;
    private int selectedHeroTeamSlotIndex = -1;
    private int draggedHeroCardIndex = -1;
    private int draggedHeroTeamSlotIndex = -1;
    private int[] heroCardDisplayIndices;
    private RawImage[] heroCardPortraits;
    private TMP_Text[] heroCardLevelTexts;
    private TMP_Text[] heroCardStarTexts;
    private TMP_Text[] heroCardShardTexts;
    private TMP_Text[] heroCardRoleBadgeTexts;
    private TMP_Text[] heroCardTeamBadgeTexts;
    private Image[] heroCardShardFills;
    private RectTransform heroDetailRoot;
    private RawImage heroDetailPortrait;
    private TMP_Text heroDetailRarityText;
    private TMP_Text heroDetailTitleText;
    private TMP_Text heroDetailNameText;
    private TMP_Text heroDetailPowerText;
    private TMP_Text heroDetailStatsText;
    private TMP_Text heroDetailResourceText;
    private TMP_Text[] heroDetailGearSlotTexts;
    private Image[] heroDetailGearSlotFrames;
    private Button[] heroDetailGearSlotButtons;
    private RectTransform heroDetailGearListRoot;
    private TMP_Text heroDetailGearListTitleText;
    private TMP_Text[] heroDetailGearOptionTexts;
    private Button[] heroDetailGearOptionButtons;
    private Button heroDetailGearListCloseButton;
    private Button heroDetailCloseButton;
    private Button heroDetailPreviousButton;
    private Button heroDetailNextButton;
    private Button heroDetailLevelButton;
    private Button heroDetailEquipGearButton;
    private Button heroDetailRemoveGearButton;
    private int selectedHeroDetailGearSlotIndex = -1;
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
    private RectTransform summonOfferRoot;
    private TMP_Text summonOfferTitleText;
    private TMP_Text summonOfferPromoText;
    private TMP_Text summonSingleCostText;
    private TMP_Text summonTenCostText;
    private Button summonTenButton;
    private RawImage[] summonOfferHeroImages;
    private RectTransform summonResultBoxRoot;
    private RectTransform summonResultPopupRoot;
    private TMP_Text summonResultPopupTitleText;
    private TMP_Text summonAutoToggleText;
    private Image summonAutoCheckboxImage;
    private TMP_Text summonAutoCheckboxMarkText;
    private Button summonResultCloseButton;
    private Button summonResultTenButton;
    private Button summonResultMaxButton;
    private Button summonAutoToggleButton;
    private RawImage[] summonResultHeroImages;
    private TMP_Text[] summonResultHeroNameTexts;
    private TMP_Text[] summonResultHeroCountTexts;
    private Image[] summonResultHeroFrames;
    private TMP_Text summonResultTenCostText;
    private TMP_Text summonResultMaxCostText;
    private RectTransform summonCountChipRoot;
    private RectTransform summonRatesBoxRoot;
    private RectTransform summonCarouselRoot;
    private Button summonCarouselPreviousButton;
    private Button summonCarouselNextButton;
    private Button[] summonCarouselButtons;
    private Image[] summonCarouselFrames;
    private TMP_Text[] summonCarouselTitleTexts;
    private TMP_Text[] summonCarouselRateTexts;
    private RawImage[] summonCarouselHeroImages;
    private int[] summonCarouselCardBannerIndices;
    private int selectedSummonBannerIndex;
    private float summonCarouselDragStartX;
    private bool summonAutoEnabled;
    private bool summonAutoRunning;
    private int summonAutoRemainingPulls;
    private int pendingBackendSummonCount = 1;
    private Coroutine summonAutoCoroutine;
    private RectTransform formationRoot;
    private RectTransform fightRoot;
    private RectTransform fightResultRoot;
    private TMP_Text formationHeaderText;
    private TMP_Text formationEnemyText;
    private TMP_Text formationTeamText;
    private TMP_Text formationHintText;
    private RawImage formationArenaBackgroundImage;
    private RawImage formationEnemyImage;
    private Button[] formationSlotButtons;
    private Image[] formationSlotFrames;
    private RawImage[] formationHeroImages;
    private RavikSkeletalCombatView[] formationHeroSkeletalViews;
    private TMP_Text[] formationHeroTexts;
    private Button formationAutoContinueButton;
    private Image formationAutoContinueBox;
    private TMP_Text formationAutoContinueMarkText;
    private TMP_Text formationAutoContinueText;
    private Button formationConfirmButton;
    private Button formationBackButton;
    private int[] formationSlotHeroIndices;
    private int selectedFormationSlotIndex = -1;
    private bool autoContinueFightsEnabled;
    private Coroutine autoContinueFightCoroutine;
    private RawImage fightArenaBackgroundImage;
    private TMP_Text fightVsText;
    private TMP_Text fightTimerText;
    private TMP_Text fightStatusText;
    private RawImage[] fightHeroImages;
    private RawImage[] fightEnemyImages;
    private RavikSkeletalCombatView[] fightHeroSkeletalViews;
    private RectTransform[] fightHeroRects;
    private RectTransform[] fightEnemyRects;
    private Image[] fightHeroHpFills;
    private Image[] fightEnemyHpFills;
    private TMP_Text[] fightEnemyHpPercentTexts;
    private Image fightBossHpFill;
    private TMP_Text fightBossHpText;
    private Texture2D[][] fightHeroIdleFrames;
    private Texture2D[][] fightHeroRunFrames;
    private Texture2D[][] fightHeroAttackFrames;
    private Texture2D[][] fightHeroUltimateFrames;
    private Texture2D[][] fightHeroAttackFxFrames;
    private Texture2D[][] fightHeroUltimateFxFrames;
    private Texture2D[][] fightEnemyIdleFrames;
    private Texture2D[][] fightEnemyRunFrames;
    private Texture2D[][] fightEnemyAttackFrames;
    private string[] fightEnemyTextureNames;
    private Image[] fightHeroProjectileImages;
    private Image[] fightEnemyProjectileImages;
    private RectTransform[] fightHeroProjectileRects;
    private RectTransform[] fightEnemyProjectileRects;
    private RawImage[] fightHeroFxImages;
    private RectTransform[] fightHeroFxRects;
    private Button[] fightSkillButtons;
    private Image[] fightSkillBackplates;
    private Image[] fightSkillHpFills;
    private TMP_Text[] fightSkillHpTexts;
    private Image[] fightSkillManaFills;
    private RawImage[] fightSkillPortraits;
    private TMP_Text[] fightSkillNameTexts;
    private TMP_Text[] fightSkillManaTexts;
    private Button fightAutoSkillButton;
    private TMP_Text fightAutoSkillButtonText;
    private Button fightSpeedButton;
    private TMP_Text fightSpeedButtonText;
    private int[] fightHeroManaValues;
    private int[] fightHeroMaxManaValues;
    private bool[] fightHeroUltimateQueued;
    private float[] fightHeroUltimateStartedAt;
    private float[] fightHeroVisibleHpPercents;
    private bool fightAutoSkillsEnabled;
    private bool fightDoubleSpeedEnabled;
    private TMP_Text[] fightFloatingTexts;
    private TMP_Text fightResultTitleText;
    private TMP_Text fightResultBodyText;
    private Button fightContinueButton;
    private Button fightEndButton;
    private bool fightCancelRequested;
    private Coroutine activeFightCoroutine;

    private void Awake()
    {
        LoadProgress();
        autoAttackEnabled = false;
        selectedCampaignStage = Mathf.Max(1, enemyLevel);
        EnsureFormationOrder();
        BankAfkElapsedSinceLastSeen();
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

        if (heroDetailCloseButton != null)
        {
            heroDetailCloseButton.onClick.AddListener(HideHeroDetail);
        }

        if (heroDetailPreviousButton != null)
        {
            heroDetailPreviousButton.onClick.AddListener(ShowPreviousHeroDetail);
        }

        if (heroDetailNextButton != null)
        {
            heroDetailNextButton.onClick.AddListener(ShowNextHeroDetail);
        }

        if (heroDetailLevelButton != null)
        {
            heroDetailLevelButton.onClick.AddListener(UpgradeDamage);
        }

        if (heroDetailEquipGearButton != null)
        {
            heroDetailEquipGearButton.onClick.AddListener(ShowGear);
        }

        if (heroDetailRemoveGearButton != null)
        {
            heroDetailRemoveGearButton.onClick.AddListener(ShowGear);
        }

        RegisterHeroDetailGearButtons();

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

        if (summonTenButton != null)
        {
            summonTenButton.onClick.AddListener(SummonTen);
        }

        RegisterSummonCarouselButtons();
        RegisterSummonResultButtons();

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

        TickAfkRewards(Time.deltaTime);

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

        if (heroDetailCloseButton != null)
        {
            heroDetailCloseButton.onClick.RemoveListener(HideHeroDetail);
        }

        if (heroDetailPreviousButton != null)
        {
            heroDetailPreviousButton.onClick.RemoveListener(ShowPreviousHeroDetail);
        }

        if (heroDetailNextButton != null)
        {
            heroDetailNextButton.onClick.RemoveListener(ShowNextHeroDetail);
        }

        if (heroDetailLevelButton != null)
        {
            heroDetailLevelButton.onClick.RemoveListener(UpgradeDamage);
        }

        if (heroDetailEquipGearButton != null)
        {
            heroDetailEquipGearButton.onClick.RemoveListener(ShowGear);
        }

        if (heroDetailRemoveGearButton != null)
        {
            heroDetailRemoveGearButton.onClick.RemoveListener(ShowGear);
        }

        UnregisterHeroDetailGearButtons();

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

        if (summonTenButton != null)
        {
            summonTenButton.onClick.RemoveListener(SummonTen);
        }

        UnregisterSummonCarouselButtons();
        UnregisterSummonResultButtons();

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
        if (ShouldResumeCampaignFightFromVillage())
        {
            ShowScreen(AppScreen.Battle);
            return;
        }

        ShowScreen(AppScreen.Home);
    }

    public void ShowBattle()
    {
        if (HasVisibleBattleToResume())
        {
            ShowScreen(AppScreen.Battle);
            return;
        }

        ShowFormationScreen();
    }

    public void ShowDungeons()
    {
        if (IsDungeonBattleFocusLocked())
        {
            ShowScreen(AppScreen.Battle);
            return;
        }

        ShowScreen(AppScreen.Dungeons);
    }

    public void ShowHeroes()
    {
        if (IsDungeonBattleFocusLocked())
        {
            ShowScreen(AppScreen.Battle);
            return;
        }

        ShowScreen(AppScreen.Heroes);
    }

    public void ShowGear()
    {
        if (IsDungeonBattleFocusLocked())
        {
            ShowScreen(AppScreen.Battle);
            return;
        }

        ShowScreen(AppScreen.Gear);
    }

    public void ShowSummon()
    {
        if (IsDungeonBattleFocusLocked())
        {
            ShowScreen(AppScreen.Battle);
            return;
        }

        ShowScreen(AppScreen.Summon);
    }

    public void ShowShop()
    {
        if (IsDungeonBattleFocusLocked())
        {
            ShowScreen(AppScreen.Battle);
            return;
        }

        ShowScreen(AppScreen.Shop);
    }

    private bool ShouldResumeCampaignFightFromVillage()
    {
        return battleTargetMode == BattleTargetMode.Campaign && HasVisibleBattleToResume();
    }

    private bool HasVisibleBattleToResume()
    {
        return battleFlowMode != BattleFlowMode.Formation && (campaignFightInProgress || battleFlowMode == BattleFlowMode.Result);
    }

    private bool IsDungeonBattleFocusLocked()
    {
        return battleTargetMode == BattleTargetMode.Dungeon && HasVisibleBattleToResume();
    }

    private void ShowInventoryPopup()
    {
        SetInventoryPopupVisible(true);
    }

    private void HideInventoryPopup()
    {
        SetInventoryPopupVisible(false);
    }

    private void ShowInventoryMiscTab()
    {
        SelectInventoryTab(InventoryTabMode.Misc);
    }

    private void ShowInventoryGearTab()
    {
        SelectInventoryTab(InventoryTabMode.Gear);
    }

    private void ShowInventoryAllTab()
    {
        SelectInventoryTab(InventoryTabMode.All);
    }

    private void SelectInventoryTab(InventoryTabMode tab)
    {
        selectedInventoryTab = tab;
        selectedInventoryItemIndex = -1;
        SetComponentActive(inventoryDetailRoot, false);
        RefreshInventoryPopupUi();
    }

    private void SelectInventoryItem(int itemIndex)
    {
        selectedInventoryItemIndex = Mathf.Max(0, itemIndex);
        RefreshInventoryPopupUi();
    }

    private void HideInventoryItemDetails()
    {
        selectedInventoryItemIndex = -1;
        SetComponentActive(inventoryDetailRoot, false);
        RefreshInventoryPopupUi();
    }

    private void ShowFastRewardsPopup()
    {
        SetFastRewardsPopupVisible(true);
    }

    private void HideFastRewardsPopup()
    {
        SetFastRewardsPopupVisible(false);
    }

    private void ShowChatPopup()
    {
        SetChatPopupVisible(true);
    }

    private void HideChatPopup()
    {
        SetChatPopupVisible(false);
    }

    private void ShowFormationScreen()
    {
        if (campaignFightInProgress)
        {
            return;
        }

        battleTargetMode = BattleTargetMode.Campaign;
        if (selectedCampaignStage != enemyLevel)
        {
            selectedCampaignStage = Mathf.Max(1, enemyLevel);
        }

        selectedFormationSlotIndex = -1;
        SetBattleFlowMode(BattleFlowMode.Formation);
        ShowScreen(AppScreen.Battle);
        RefreshUi();
    }

    private void ShowDungeonFormation(string dungeonId)
    {
        if (campaignFightInProgress)
        {
            return;
        }

        selectedDungeonId = ResolveDungeonDefinition(dungeonId).dungeonId;
        SelectRandomDungeonBattleMap(selectedDungeonId);
        battleTargetMode = BattleTargetMode.Dungeon;
        selectedFormationSlotIndex = -1;
        SetBattleFlowMode(BattleFlowMode.Formation);
        ShowScreen(AppScreen.Battle);
        RefreshUi();
    }

    private void BackToCampaignMap()
    {
        if (campaignFightInProgress)
        {
            return;
        }

        selectedFormationSlotIndex = -1;
        SetBattleFlowMode(BattleFlowMode.Formation);
        ShowScreen(battleTargetMode == BattleTargetMode.Dungeon ? AppScreen.Dungeons : AppScreen.Home);
    }

    private void ContinueAfterCampaignFight()
    {
        if (autoContinueFightCoroutine != null)
        {
            StopCoroutine(autoContinueFightCoroutine);
            autoContinueFightCoroutine = null;
        }

        fightCancelRequested = false;
        campaignFightInProgress = false;
        if (battleTargetMode == BattleTargetMode.Campaign)
        {
            selectedCampaignStage = Mathf.Max(1, enemyLevel);
        }

        selectedFormationSlotIndex = -1;
        SetBattleFlowMode(BattleFlowMode.Formation);
        ShowScreen(battleTargetMode == BattleTargetMode.Dungeon ? AppScreen.Dungeons : AppScreen.Home);
        RefreshUi();
    }

    private void EndCurrentFight()
    {
        if (!campaignFightInProgress && battleFlowMode != BattleFlowMode.Fight)
        {
            return;
        }

        fightCancelRequested = true;
        autoContinueFightsEnabled = false;
        fightAutoSkillsEnabled = false;
        if (autoContinueFightCoroutine != null)
        {
            StopCoroutine(autoContinueFightCoroutine);
            autoContinueFightCoroutine = null;
        }

        SetProjectilesVisible(fightHeroProjectileImages, false);
        SetProjectilesVisible(fightEnemyProjectileImages, false);
        SetRawImagesVisible(fightHeroFxImages, false);
        HideRavikSkeletalViews(fightHeroSkeletalViews);
        RefreshFormationAutoContinueToggle();
        RefreshFightAutoSkillButton();
        SaveProgress();
        ShowCampaignFightResult(false, "Fight Ended", "Auto battle stopped. No rewards were claimed for this cancelled fight.");
    }

    private void StartTrackedFightCoroutine(IEnumerator routine)
    {
        if (routine == null)
        {
            return;
        }

        if (activeFightCoroutine != null)
        {
            StopCoroutine(activeFightCoroutine);
            activeFightCoroutine = null;
        }

        activeFightCoroutine = StartCoroutine(TrackFightCoroutine(routine));
    }

    private IEnumerator TrackFightCoroutine(IEnumerator routine)
    {
        yield return routine;
        activeFightCoroutine = null;
    }

    private bool ConsumeFightCancelRequest()
    {
        if (!fightCancelRequested)
        {
            return false;
        }

        fightCancelRequested = false;
        return true;
    }

    private void StartCampaignFightFromFormation()
    {
        if (campaignFightInProgress || backendRequestInProgress || backendLifecycleFlushInProgress)
        {
            return;
        }

        if (battleTargetMode == BattleTargetMode.Dungeon)
        {
            StartDungeonFightFromFormation();
            return;
        }

        if (selectedCampaignStage != enemyLevel)
        {
            selectedCampaignStage = Mathf.Max(1, enemyLevel);
            RefreshUi();
            return;
        }

        selectedFormationSlotIndex = -1;
        fightAutoSkillsEnabled = autoContinueFightsEnabled;
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: campaign fight..."))
            {
                campaignFightInProgress = true;
                SetBattleFlowMode(BattleFlowMode.Fight);
                StartCoroutine(backendClient.FightCampaign(OnBackendCampaignFightVisual));
            }

            return;
        }

        StartTrackedFightCoroutine(PlayLocalCampaignFightRoutine());
    }

    private void StartDungeonFightFromFormation()
    {
        selectedDungeonId = ResolveDungeonDefinition(selectedDungeonId).dungeonId;
        EnsureSelectedDungeonBattleMap();
        selectedFormationSlotIndex = -1;
        fightAutoSkillsEnabled = autoContinueFightsEnabled;
        if (backendGameplayEnabled)
        {
            var dungeon = ResolveDungeonDefinition(selectedDungeonId);
            if (TryStartBackendRequest($"Server: {dungeon.displayName.ToLowerInvariant()} boss..."))
            {
                campaignFightInProgress = true;
                SetBattleFlowMode(BattleFlowMode.Fight);
                StartCoroutine(backendClient.RunDungeon(selectedDungeonId, OnBackendDungeonFightVisual));
            }

            return;
        }

        StartTrackedFightCoroutine(PlayLocalDungeonFightRoutine());
    }

    private void ToggleFormationAutoContinue()
    {
        autoContinueFightsEnabled = !autoContinueFightsEnabled;
        fightAutoSkillsEnabled = autoContinueFightsEnabled;
        if (!autoContinueFightsEnabled && autoContinueFightCoroutine != null)
        {
            StopCoroutine(autoContinueFightCoroutine);
            autoContinueFightCoroutine = null;
        }

        RefreshFormationAutoContinueToggle();
        RefreshFightAutoSkillButton();
        SaveProgress();
    }

    private void RefreshFormationAutoContinueToggle()
    {
        if (formationAutoContinueBox != null)
        {
            formationAutoContinueBox.color = autoContinueFightsEnabled
                ? new Color(0.12f, 0.82f, 0.58f, 0.98f)
                : new Color(0.04f, 0.055f, 0.08f, 0.96f);
        }

        if (formationAutoContinueMarkText != null)
        {
            formationAutoContinueMarkText.text = autoContinueFightsEnabled ? "X" : string.Empty;
            formationAutoContinueMarkText.color = autoContinueFightsEnabled ? new Color(0.02f, 0.06f, 0.05f) : Color.white;
        }

        if (formationAutoContinueText != null)
        {
            formationAutoContinueText.text = "Auto next after win (skills AUTO)";
            formationAutoContinueText.color = autoContinueFightsEnabled
                ? new Color(0.84f, 1f, 0.9f)
                : new Color(0.78f, 0.84f, 0.92f);
        }
    }

    private void RedeemFastRewards()
    {
        if (backendGameplayEnabled && backendClient != null && backendClient.HasSession)
        {
            ClaimBackendOfflineRewards("fast_rewards");
            return;
        }

        var rewardSeconds = Mathf.FloorToInt(afkRewardStoredSeconds);
        var pendingGold = CalculateAfkGoldReward(afkRewardStoredSeconds);
        var pendingEssence = CalculateAfkEssenceReward(afkRewardStoredSeconds);
        if (pendingGold <= 0 && pendingEssence <= 0)
        {
            RefreshFastRewardsPopupUi();
            return;
        }

        lastOfflineGoldReward = pendingGold;
        lastOfflineReward = pendingEssence;
        lastOfflineSeconds = rewardSeconds;
        lastOfflineRewardIsServer = false;

        GrantCurrency(GoldCurrencyId, pendingGold);
        GrantCurrency(MythEssenceCurrencyId, pendingEssence);
        afkRewardStoredSeconds = 0f;
        afkRewardAutosaveTimer = 0f;
        SaveProgress();
        RefreshUi();
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
        ShowDungeonFormation(GoldDungeonDefinition.dungeonId);
    }

    public void RunEssenceDungeon()
    {
        ShowDungeonFormation(EssenceDungeonDefinition.dungeonId);
    }

    public void RunGearDungeon()
    {
        ShowDungeonFormation(GearDungeonDefinition.dungeonId);
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
        var actionResult = ApplyGearDungeonResult(floor, result, enemyHp);
        SaveProgress();
        RefreshUi();
        return actionResult;
    }

    private MythwakeActionResultDto ApplyGearDungeonResult(int floor, CombatResult result, int enemyHp)
    {
        if (!result.won)
        {
            var failMessage = $"Gear Dungeon Floor {floor} failed after {result.elapsedSeconds}s\nEnemy HP {result.enemyHpRemaining}/{enemyHp}  {FormatCombatResult(result)}";
            PlayCombatVisual(GearDungeonDefinition.dungeonId, $"Gear Dungeon F{floor}", result, enemyHp);
            SetDungeonResult(failMessage);
            return CreateActionResult(false, "gear_dungeon_run", "combat_lost", failMessage);
        }

        var accessory = RollAccessoryDrop(floor);
        AddAccessoryInventory(accessory.slotIndex, accessory.rarityIndex, 1);
        gearDungeonFloor++;

        var message = $"Gear Dungeon Floor {floor} cleared in {result.elapsedSeconds}s\nDrop: {GetAccessoryRarityName(accessory.rarityIndex)} {AccessorySlots[accessory.slotIndex].name}  HP {result.teamHpRemaining}/{GetTeamHealth()}";
        PlayCombatVisual(GearDungeonDefinition.dungeonId, $"Gear Dungeon F{floor}", result, enemyHp);
        SetDungeonResult(message);
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
        var heroIndex = GetSelectedHeroIndex();
        var currentLevel = GetHeroEquipmentLevel(heroIndex, isWeapon);
        currentLevel = Mathf.Max(StarterEquipmentLevel, currentLevel);
        if (IsEquipmentLevelMax(track, currentLevel))
        {
            var maxResult = CreateActionResult(false, "equipment_level", "max_level", $"{GetHeroDefinition(heroIndex).name}'s {track.name} is already max level.");
            RefreshUi();
            return maxResult;
        }

        var cost = GetEquipmentUpgradeCost(track, currentLevel);

        if (!TrySpendCurrency(GoldCurrencyId, cost))
        {
            var failMessage = $"Need {cost} Gold to level {GetHeroDefinition(heroIndex).name}'s {track.name}.";
            RefreshUi();
            return CreateActionResult(false, "equipment_level", "insufficient_currency", failMessage);
        }

        SetHeroEquipmentLevel(heroIndex, isWeapon, currentLevel + 1);
        damage = GetTeamDamage();

        SaveProgress();
        RefreshUi();
        var newLevel = GetHeroEquipmentLevel(heroIndex, isWeapon);
        return CreateActionResult(true, "equipment_level", string.Empty, $"{GetHeroDefinition(heroIndex).name}'s {track.name} reached Lv. {newLevel}.");
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
        var heroIndex = GetSelectedHeroIndex();

        if (GetAccessoryInventoryCount(slot, rarity) <= 0)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_equip", "missing_item", $"No {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} copy to equip.");
        }

        if (GetHeroEquippedAccessoryRarity(heroIndex, slot) == rarity)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_equip", "already_equipped", $"{GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} is already equipped on {GetHeroDefinition(heroIndex).name}.");
        }

        var previousRarity = GetHeroEquippedAccessoryRarity(heroIndex, slot);
        if (previousRarity >= 0)
        {
            AddAccessoryInventory(slot, previousRarity, 1);
        }

        AddAccessoryInventory(slot, rarity, -1);
        SetHeroEquippedAccessory(heroIndex, slot, rarity, 1);

        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "accessory_equip", string.Empty, $"Equipped {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} on {GetHeroDefinition(heroIndex).name}.");
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
        var heroIndex = GetSelectedHeroIndex();

        if (GetHeroEquippedAccessoryRarity(heroIndex, slot) != rarity)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_level", "not_equipped", $"{GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} is not equipped on {GetHeroDefinition(heroIndex).name}.");
        }

        var maxLevel = GetAccessoryMaxLevel(rarity);
        var currentLevel = GetHeroEquippedAccessoryLevel(heroIndex, slot);
        if (currentLevel >= maxLevel)
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_level", "max_level", $"{GetHeroDefinition(heroIndex).name}'s {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} is already max level.");
        }

        var cost = GetAccessoryLevelCost(slot);
        if (!TrySpendCurrency(GoldCurrencyId, cost))
        {
            RefreshUi();
            return CreateActionResult(false, "accessory_level", "insufficient_currency", $"Need {cost} Gold to level {AccessorySlots[slot].name}.");
        }

        SetHeroEquippedAccessory(heroIndex, slot, rarity, currentLevel + 1);
        damage = GetTeamDamage();

        SaveProgress();
        RefreshUi();
        return CreateActionResult(true, "accessory_level", string.Empty, $"Leveled {GetHeroDefinition(heroIndex).name}'s {GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name} to Lv. {GetHeroEquippedAccessoryLevel(heroIndex, slot)}.");
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
        SummonMany(1);
    }

    public void SummonTen()
    {
        SummonMany(10);
    }

    private void ShowPreviousSummonBanner()
    {
        SelectSummonBanner(selectedSummonBannerIndex - 1);
    }

    private void ShowNextSummonBanner()
    {
        SelectSummonBanner(selectedSummonBannerIndex + 1);
    }

    private void SelectSummonCarouselCard0()
    {
        SelectSummonCarouselCard(0);
    }

    private void SelectSummonCarouselCard1()
    {
        SelectSummonCarouselCard(1);
    }

    private void SelectSummonCarouselCard2()
    {
        SelectSummonCarouselCard(2);
    }

    private void SelectSummonCarouselCard(int cardIndex)
    {
        if (summonCarouselCardBannerIndices == null || cardIndex < 0 || cardIndex >= summonCarouselCardBannerIndices.Length)
        {
            return;
        }

        SelectSummonBanner(summonCarouselCardBannerIndices[cardIndex]);
    }

    private void SelectSummonBanner(int bannerIndex)
    {
        var wrappedIndex = WrapSummonBannerIndex(bannerIndex);
        if (wrappedIndex == selectedSummonBannerIndex)
        {
            return;
        }

        selectedSummonBannerIndex = wrappedIndex;
        RefreshSummonUi();
    }

    private void BeginSummonCarouselDrag(BaseEventData eventData)
    {
        if (eventData is PointerEventData pointerEvent)
        {
            summonCarouselDragStartX = pointerEvent.position.x;
        }
    }

    private void EndSummonCarouselDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData pointerEvent))
        {
            return;
        }

        var deltaX = pointerEvent.position.x - summonCarouselDragStartX;
        if (Mathf.Abs(deltaX) < 70f)
        {
            return;
        }

        if (deltaX < 0f)
        {
            ShowNextSummonBanner();
        }
        else
        {
            ShowPreviousSummonBanner();
        }
    }

    private void SummonMany(int count)
    {
        var activeBanner = GetActiveSummonBanner();
        if (backendGameplayEnabled)
        {
            var backendCount = Mathf.Clamp(count, 1, MaxSummonPullCount);
            if (TryStartBackendRequest(backendCount == 1 ? "Server: summon pull..." : $"Server: summon x{backendCount}..."))
            {
                var backendBannerId = activeBanner.bannerId == HeroShardBanner.bannerId ? activeBanner.bannerId : HeroShardBanner.bannerId;
                pendingBackendSummonCount = backendCount;
                StartCoroutine(backendClient.PullSummon(backendBannerId, backendCount, OnBackendSummonAction));
            }

            return;
        }

        PullMany(activeBanner.bannerId, count);
    }

    public MythwakeActionResultDto Pull(string bannerId)
    {
        return PullMany(bannerId, 1);
    }

    public MythwakeActionResultDto PullMany(string bannerId, int count)
    {
        return PullManyInternal(bannerId, count, true, true);
    }

    private MythwakeActionResultDto PullManyInternal(string bannerId, int count, bool showResultPopup, bool allowAutoContinue)
    {
        if (!TryGetLocalSummonBanner(bannerId, out var banner))
        {
            var invalidBanner = CreateActionResult(false, "summon_pull", "invalid_banner", $"Unknown banner: {bannerId}");
            SetSummonResult(invalidBanner.message);
            RefreshUi();
            return invalidBanner;
        }

        count = Mathf.Clamp(count, 1, MaxSummonPullCount);
        var summonCost = GetSummonPackCost(count, banner);
        if (!TrySpendCurrency(GemsCurrencyId, summonCost))
        {
            var failMessage = count == 1
                ? $"Need {summonCost} Gems for a summon."
                : $"Need {summonCost} Gems for Summon x{count}.";
            SetSummonResult(failMessage);
            RefreshUi();
            return CreateActionResult(false, "summon_pull", "insufficient_currency", failMessage);
        }

        EnsureHeroShards();

        var shardTotals = new int[HeroDefinitions.Length];
        var drawCounts = new int[HeroDefinitions.Length];
        var lastHeroIndex = 0;
        var totalShards = 0;
        for (var i = 0; i < count; i++)
        {
            summonCount++;
            dailySummonCount++;

            var heroIndex = RollSummonHero(banner);
            var shards = GetSummonShardReward(heroIndex);
            heroShards[heroIndex] += shards;
            shardTotals[heroIndex] += shards;
            drawCounts[heroIndex]++;
            totalShards += shards;
            lastHeroIndex = heroIndex;
        }

        selectedHeroIndex = lastHeroIndex;
        damage = GetTeamDamage();
        var hero = GetHeroDefinition(lastHeroIndex);

        var message = count == 1
            ? $"{hero.rarityName} pull: {hero.name}\n+{shardTotals[lastHeroIndex]} shards"
            : BuildSummonPackResultMessage(count, totalShards, shardTotals);
        PlaySummonVisual(lastHeroIndex, count == 1 ? $"{hero.rarityName} {hero.name}" : $"Summon x{count}");
        SetSummonResult(message);
        if (showResultPopup)
        {
            ShowSummonResultPopup(drawCounts, count);
        }

        SaveProgress();
        RefreshUi();
        if (allowAutoContinue && summonAutoEnabled && count < MaxSummonPullCount)
        {
            StartSummonAutoRoutine(MaxSummonPullCount - count);
        }

        return CreateActionResult(true, "summon_pull", string.Empty, message);
    }

    private void SummonResultTen()
    {
        SummonMany(SummonAutoStepCount);
    }

    private void SummonResultMax()
    {
        SummonMany(MaxSummonPullCount);
    }

    private void ToggleSummonAuto()
    {
        summonAutoEnabled = !summonAutoEnabled;
        if (!summonAutoEnabled && summonAutoCoroutine != null)
        {
            StopCoroutine(summonAutoCoroutine);
            summonAutoCoroutine = null;
            summonAutoRunning = false;
            summonAutoRemainingPulls = 0;
        }

        RefreshSummonAutoToggle();
        RefreshUi();
    }

    private void StartSummonAutoRoutine(int remainingPulls)
    {
        if (!summonAutoEnabled || remainingPulls <= 0)
        {
            return;
        }

        if (summonAutoCoroutine != null)
        {
            StopCoroutine(summonAutoCoroutine);
        }

        summonAutoRemainingPulls = Mathf.Clamp(remainingPulls, 0, MaxSummonPullCount);
        summonAutoCoroutine = StartCoroutine(SummonAutoRoutine());
    }

    private IEnumerator SummonAutoRoutine()
    {
        summonAutoRunning = true;
        RefreshSummonAutoToggle();
        RefreshUi();

        while (summonAutoEnabled && summonAutoRemainingPulls > 0)
        {
            yield return new WaitForSecondsRealtime(0.16f);

            var activeBanner = GetActiveSummonBanner();
            var nextPulls = Mathf.Min(SummonAutoStepCount, summonAutoRemainingPulls);
            if (gems < GetSummonPackCost(nextPulls, activeBanner))
            {
                break;
            }

            var result = PullManyInternal(activeBanner.bannerId, nextPulls, true, false);
            if (!result.success)
            {
                break;
            }

            summonAutoRemainingPulls -= nextPulls;
            RefreshSummonAutoToggle();
        }

        summonAutoRunning = false;
        summonAutoRemainingPulls = 0;
        summonAutoCoroutine = null;
        RefreshSummonAutoToggle();
        RefreshUi();
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
        heroWeaponLevels = CreateFilledIntArray(HeroCount, StarterEquipmentLevel);
        heroArmorLevels = CreateFilledIntArray(HeroCount, StarterEquipmentLevel);
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

        equippedAccessoryRarities = CreateFilledIntArray(AccessorySlotCount, -1);
        equippedAccessoryLevels = new int[AccessorySlotCount];
        heroEquippedAccessoryRarities = CreateFilledIntArray(HeroCount * AccessorySlotCount, -1);
        heroEquippedAccessoryLevels = new int[HeroCount * AccessorySlotCount];

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

    private void OnBackendCampaignFightVisual(bool success, string error, MythwakeActionResultDto result)
    {
        if (!success)
        {
            campaignFightInProgress = false;
            SetBattleFlowMode(BattleFlowMode.Formation);
            SetDungeonResult($"Server request failed: {error}");
            FinishBackendRequest($"Server request failed: {error}");
            return;
        }

        if (fightCancelRequested)
        {
            ApplyBackendSnapshot(result.playerSnapshot);
            fightCancelRequested = false;
            campaignFightInProgress = false;
            autoContinueFightsEnabled = false;
            fightAutoSkillsEnabled = false;
            SetBattleFlowMode(BattleFlowMode.Result);
            ShowCampaignFightResult(false, "Fight Ended", "Auto battle stopped. Server result was received, but no auto-continue will run.");
            FinishBackendRequest($"Server action finished after fight cancellation.  {result.actionId}{FormatBackendRevisionSuffix(result)}");
            return;
        }

        ApplyBackendSnapshot(result.playerSnapshot);
        var message = string.IsNullOrWhiteSpace(result.message) ? FormatBackendActionOutcome(result) : result.message;
        if (HasServerCombatResult(result))
        {
            message = FormatServerCombatMessage(result);
            StartTrackedFightCoroutine(PlayServerCampaignFightRoutine(result));
        }
        else
        {
            campaignFightInProgress = false;
            SetBattleFlowMode(BattleFlowMode.Result);
            ShowCampaignFightResult(result.success, "Campaign Result", message);
        }

        SetDungeonResult(message);
        FinishBackendRequest($"Server action: {FormatBackendActionOutcome(result)}  {result.actionId}{FormatBackendRevisionSuffix(result)}");
    }

    private void OnBackendDungeonFightVisual(bool success, string error, MythwakeActionResultDto result)
    {
        if (!success)
        {
            campaignFightInProgress = false;
            SetBattleFlowMode(BattleFlowMode.Formation);
            SetDungeonResult($"Server request failed: {error}");
            FinishBackendRequest($"Server request failed: {error}");
            return;
        }

        if (fightCancelRequested)
        {
            ApplyBackendSnapshot(result.playerSnapshot);
            fightCancelRequested = false;
            campaignFightInProgress = false;
            autoContinueFightsEnabled = false;
            fightAutoSkillsEnabled = false;
            SetBattleFlowMode(BattleFlowMode.Result);
            ShowCampaignFightResult(false, "Fight Ended", "Auto battle stopped. Server result was received, but no auto-continue will run.");
            FinishBackendRequest($"Server action finished after fight cancellation.  {result.actionId}{FormatBackendRevisionSuffix(result)}");
            return;
        }

        battleTargetMode = BattleTargetMode.Dungeon;
        ApplyBackendSnapshot(result.playerSnapshot);
        var message = string.IsNullOrWhiteSpace(result.message) ? FormatBackendActionOutcome(result) : result.message;
        if (HasServerCombatResult(result))
        {
            message = FormatServerCombatMessage(result);
            selectedDungeonId = string.IsNullOrWhiteSpace(result.combat.targetId) ? selectedDungeonId : result.combat.targetId;
            StartTrackedFightCoroutine(PlayServerDungeonFightRoutine(result));
        }
        else
        {
            campaignFightInProgress = false;
            SetBattleFlowMode(BattleFlowMode.Result);
            ShowCampaignFightResult(result.success, "Dungeon Result", message);
        }

        SetDungeonResult(message);
        FinishBackendRequest($"Server action: {FormatBackendActionOutcome(result)}  {result.actionId}{FormatBackendRevisionSuffix(result)}");
    }

    private void OnBackendSummonAction(bool success, string error, MythwakeActionResultDto result)
    {
        CompleteBackendAction(success, error, result, showInSummonPanel: true);
        if (success && result.success)
        {
            ShowBackendSummonResultPopup(result.message, pendingBackendSummonCount);
        }

        pendingBackendSummonCount = 1;
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
        selectedCampaignStage = Mathf.Max(1, enemyLevel);
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

    private IEnumerator PlayLocalCampaignFightRoutine()
    {
        campaignFightInProgress = true;
        battleTargetMode = BattleTargetMode.Campaign;
        SetBattleFlowMode(BattleFlowMode.Fight);
        RefreshUi();

        damage = GetTeamDamage();
        dailyFightCount++;
        var stageNumber = enemyLevel;
        var stage = GetStageDefinition(stageNumber);
        var result = SimulateCombat(stage.maxHp, GetCampaignEnemyDamage(stageNumber));
        yield return PlayCampaignFightVisualRoutine(
            result.won,
            stageNumber,
            stage.enemyName,
            result.elapsedSeconds,
            GetTeamHealth(),
            result.teamHpRemaining,
            stage.maxHp,
            result.enemyHpRemaining,
            result.damageDealt,
            result.damageTaken);
        if (ConsumeFightCancelRequest())
        {
            yield break;
        }

        var actionResult = ApplyCampaignFightResult(stageNumber, stage, result);
        SaveProgress();
        RefreshUi();
        ShowCampaignFightResult(actionResult.success, actionResult.success ? "Victory" : "Defeat", actionResult.message);
    }

    private IEnumerator PlayLocalDungeonFightRoutine()
    {
        campaignFightInProgress = true;
        battleTargetMode = BattleTargetMode.Dungeon;
        SetBattleFlowMode(BattleFlowMode.Fight);
        RefreshUi();

        damage = GetTeamDamage();
        dailyFightCount++;
        var dungeon = ResolveDungeonDefinition(selectedDungeonId);
        var floor = GetDungeonFloor(dungeon.dungeonId);
        var enemyHp = GetDungeonEnemyHp(dungeon, floor);
        var enemyDamage = GetDungeonEnemyDamage(dungeon, floor);
        var result = SimulateCombat(enemyHp, enemyDamage);
        yield return PlayCampaignFightVisualRoutine(
            result.won,
            floor,
            $"{dungeon.displayName} F{floor}  VS  {GetDungeonBossName(dungeon.dungeonId)}",
            result.elapsedSeconds,
            GetTeamHealth(),
            result.teamHpRemaining,
            enemyHp,
            result.enemyHpRemaining,
            result.damageDealt,
            result.damageTaken,
            singleBoss: true,
            bossTextureName: GetDungeonBossTextureName(dungeon.dungeonId),
            enemyDamage: enemyDamage);
        if (ConsumeFightCancelRequest())
        {
            yield break;
        }

        var actionResult = ApplyDungeonFightResult(dungeon.dungeonId, floor, result, enemyHp);
        SaveProgress();
        RefreshUi();
        ShowCampaignFightResult(actionResult.success, actionResult.success ? "Boss Cleared" : "Boss Failed", actionResult.message);
    }

    private IEnumerator PlayServerCampaignFightRoutine(MythwakeActionResultDto result)
    {
        var combat = result.combat;
        battleTargetMode = BattleTargetMode.Campaign;
        yield return PlayCampaignFightVisualRoutine(
            combat.won,
            combat.targetLevel,
            GetServerCombatLabel(combat),
            combat.elapsedSeconds,
            combat.teamMaxHp,
            combat.teamHpRemaining,
            combat.enemyMaxHp,
            combat.enemyHpRemaining,
            combat.damageDealt,
            combat.damageTaken);
        if (ConsumeFightCancelRequest())
        {
            yield break;
        }

        var message = HasServerCombatResult(result) ? FormatServerCombatMessage(result) : result.message;
        ShowCampaignFightResult(result.success, result.success ? "Victory" : "Defeat", message);
    }

    private IEnumerator PlayServerDungeonFightRoutine(MythwakeActionResultDto result)
    {
        var combat = result.combat;
        battleTargetMode = BattleTargetMode.Dungeon;
        selectedDungeonId = string.IsNullOrWhiteSpace(combat.targetId) ? selectedDungeonId : combat.targetId;
        EnsureSelectedDungeonBattleMap();
        yield return PlayCampaignFightVisualRoutine(
            combat.won,
            combat.targetLevel,
            $"{GetServerCombatLabel(combat)}  VS  {GetDungeonBossName(selectedDungeonId)}",
            combat.elapsedSeconds,
            combat.teamMaxHp,
            combat.teamHpRemaining,
            combat.enemyMaxHp,
            combat.enemyHpRemaining,
            combat.damageDealt,
            combat.damageTaken,
            singleBoss: true,
            bossTextureName: GetDungeonBossTextureName(selectedDungeonId),
            enemyDamage: combat.enemyDamage);
        if (ConsumeFightCancelRequest())
        {
            yield break;
        }

        var message = HasServerCombatResult(result) ? FormatServerCombatMessage(result) : result.message;
        ShowCampaignFightResult(result.success, result.success ? "Boss Cleared" : "Boss Failed", message);
    }

    private MythwakeActionResultDto ExecuteCampaignFight()
    {
        damage = GetTeamDamage();
        dailyFightCount++;
        var stageNumber = enemyLevel;
        var stage = GetStageDefinition(stageNumber);
        var result = SimulateCombat(stage.maxHp, GetCampaignEnemyDamage(stageNumber));
        return ApplyCampaignFightResult(stageNumber, stage, result);
    }

    private MythwakeActionResultDto ApplyCampaignFightResult(int stageNumber, StageDefinition stage, CombatResult result)
    {
        if (result.won)
        {
            var clearedStage = stageNumber;
            var milestoneText = GrantCampaignMilestoneReward(clearedStage);
            enemyLevel = Mathf.Max(enemyLevel, clearedStage + 1);
            selectedCampaignStage = Mathf.Max(1, enemyLevel);
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
            var failMessage = $"Campaign Stage {stageNumber} failed after {result.elapsedSeconds}s\nEnemy HP {result.enemyHpRemaining}/{stage.maxHp}  {FormatCombatResult(result)}";
            PlayCombatVisual("campaign", $"Campaign Stage {stageNumber}", result, stage.maxHp);
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
        heroWeaponLevels = new int[HeroCount];
        heroArmorLevels = new int[HeroCount];
        for (var i = 0; i < HeroCount; i++)
        {
            heroWeaponLevels[i] = Mathf.Max(StarterEquipmentLevel, PlayerPrefs.GetInt($"{HeroWeaponLevelKeyPrefix}{i}", weaponLevel));
            heroArmorLevels[i] = Mathf.Max(StarterEquipmentLevel, PlayerPrefs.GetInt($"{HeroArmorLevelKeyPrefix}{i}", armorLevel));
        }

        selectedAccessorySlot = Mathf.Clamp(PlayerPrefs.GetInt(SelectedAccessorySlotKey, selectedAccessorySlot), 0, AccessorySlotCount - 1);
        selectedAccessoryRarity = Mathf.Clamp(PlayerPrefs.GetInt(SelectedAccessoryRarityKey, selectedAccessoryRarity), 0, AccessoryRarityCount - 1);
        enemyLevel = Mathf.Max(1, PlayerPrefs.GetInt(EnemyLevelKey, enemyLevel));
        enemyMaxHp = Mathf.Max(GetStageMaxHp(enemyLevel), PlayerPrefs.GetInt(EnemyMaxHpKey, enemyMaxHp));
        enemyHp = Mathf.Clamp(PlayerPrefs.GetInt(EnemyHpKey, enemyHp), 1, enemyMaxHp);
        selectedHeroIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedHeroKey, selectedHeroIndex), 0, HeroCount - 1);
        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();
        EnsureHeroEquipment();
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

        heroEquippedAccessoryRarities = CreateFilledIntArray(HeroCount * AccessorySlotCount, -1);
        heroEquippedAccessoryLevels = new int[HeroCount * AccessorySlotCount];
        var hasHeroAccessoryPrefs = false;
        for (var heroIndex = 0; heroIndex < HeroCount; heroIndex++)
        {
            for (var slot = 0; slot < AccessorySlotCount; slot++)
            {
                var index = GetHeroAccessoryIndex(heroIndex, slot);
                var rarityKey = $"{HeroEquippedAccessoryRarityKeyPrefix}{index}";
                if (!PlayerPrefs.HasKey(rarityKey))
                {
                    continue;
                }

                hasHeroAccessoryPrefs = true;
                heroEquippedAccessoryRarities[index] = Mathf.Clamp(PlayerPrefs.GetInt(rarityKey, -1), -1, AccessoryRarityCount - 1);
                heroEquippedAccessoryLevels[index] = Mathf.Clamp(PlayerPrefs.GetInt($"{HeroEquippedAccessoryLevelKeyPrefix}{index}", 0), 0, GetAccessoryMaxLevel(Mathf.Max(0, heroEquippedAccessoryRarities[index])));
            }
        }

        if (!hasHeroAccessoryPrefs)
        {
            var heroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
            for (var slot = 0; slot < AccessorySlotCount; slot++)
            {
                if (equippedAccessoryRarities[slot] >= 0)
                {
                    var index = GetHeroAccessoryIndex(heroIndex, slot);
                    heroEquippedAccessoryRarities[index] = equippedAccessoryRarities[slot];
                    heroEquippedAccessoryLevels[index] = equippedAccessoryLevels[slot];
                }
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
        EnsureHeroEquipment();
        EnsureAccessories();
        EnsureDailyMissionClaims();
        EnsureBattlePassRewardClaims();
        EnsureFormationOrder();

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
            afkRewardStoredSeconds = afkRewardStoredSeconds,
            heroLevels = CopyIntArray(heroLevels, HeroCount, 1),
            heroShards = CopyIntArray(heroShards, HeroCount, 0),
            heroAscensions = CopyIntArray(heroAscensions, HeroCount, 0),
            formationSlotHeroIndices = CopyIntArray(this.formationSlotHeroIndices, HeroCount, -1),
            autoContinueFightsEnabled = this.autoContinueFightsEnabled,
            heroWeaponLevels = CopyIntArray(heroWeaponLevels, HeroCount, StarterEquipmentLevel),
            heroArmorLevels = CopyIntArray(heroArmorLevels, HeroCount, StarterEquipmentLevel),
            heroEquippedAccessoryRarities = CopyIntArray(heroEquippedAccessoryRarities, HeroCount * AccessorySlotCount, -1),
            heroEquippedAccessoryLevels = CopyIntArray(heroEquippedAccessoryLevels, HeroCount * AccessorySlotCount, 0),
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
        afkRewardStoredSeconds = Mathf.Clamp(data.afkRewardStoredSeconds, 0f, GetAfkRewardMaxSeconds());
        heroLevels = CopyIntArray(data.heroLevels, HeroCount, 1);
        heroShards = CopyIntArray(data.heroShards, HeroCount, 0);
        heroAscensions = CopyIntArray(data.heroAscensions, HeroCount, 0);
        formationSlotHeroIndices = CopyIntArray(data.formationSlotHeroIndices, HeroCount, -1);
        autoContinueFightsEnabled = data.autoContinueFightsEnabled;
        fightAutoSkillsEnabled = autoContinueFightsEnabled;
        heroWeaponLevels = data.heroWeaponLevels == null ? null : CopyIntArray(data.heroWeaponLevels, HeroCount, StarterEquipmentLevel);
        heroArmorLevels = data.heroArmorLevels == null ? null : CopyIntArray(data.heroArmorLevels, HeroCount, StarterEquipmentLevel);
        heroEquippedAccessoryRarities = data.heroEquippedAccessoryRarities == null ? null : CopyIntArray(data.heroEquippedAccessoryRarities, HeroCount * AccessorySlotCount, -1);
        heroEquippedAccessoryLevels = data.heroEquippedAccessoryLevels == null ? null : CopyIntArray(data.heroEquippedAccessoryLevels, HeroCount * AccessorySlotCount, 0);

        EnsureFormationOrder();
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
        EnsureFormationOrder();

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
        afkRewardStoredSeconds = Mathf.Clamp(afkRewardStoredSeconds, 0f, GetAfkRewardMaxSeconds());
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

    private void BankAfkElapsedSinceLastSeen()
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
        lastOfflineSeconds = Mathf.Clamp(elapsedSeconds, 0, GetAfkRewardMaxSeconds());

        if (lastOfflineSeconds <= 0)
        {
            SaveProgress();
            return;
        }

        afkRewardStoredSeconds = Mathf.Clamp(afkRewardStoredSeconds + lastOfflineSeconds, 0f, GetAfkRewardMaxSeconds());
        SaveProgress();
    }

    private void TickAfkRewards(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || backendGameplayEnabled)
        {
            return;
        }

        var before = Mathf.FloorToInt(afkRewardStoredSeconds);
        afkRewardStoredSeconds = Mathf.Clamp(afkRewardStoredSeconds + deltaSeconds, 0f, GetAfkRewardMaxSeconds());
        var after = Mathf.FloorToInt(afkRewardStoredSeconds);

        if (after != before)
        {
            RefreshFastRewardsPopupUi();
            RefreshOfflineRewardUi();
        }

        afkRewardAutosaveTimer += deltaSeconds;
        if (afkRewardAutosaveTimer >= AfkRewardAutosaveSeconds)
        {
            afkRewardAutosaveTimer = 0f;
            SaveProgress();
        }
    }

    private int GetAfkRewardMaxSeconds()
    {
        return AfkRewardMaxSeconds;
    }

    private float GetAfkEssencePerSecond()
    {
        return Mathf.Max(0.05f, GetStageReward(enemyLevel) / (float)DefaultCombatDurationSeconds);
    }

    private float GetAfkGoldPerSecond()
    {
        return Mathf.Max(0.05f, GetAfkEssencePerSecond() * OfflineGoldRewardRate);
    }

    private int CalculateAfkEssenceReward(float rewardSeconds)
    {
        return Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, rewardSeconds) * GetAfkEssencePerSecond()));
    }

    private int CalculateAfkGoldReward(float rewardSeconds)
    {
        return Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, rewardSeconds) * GetAfkGoldPerSecond()));
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
        var actionResult = ApplyResourceDungeonResult(isGoldDungeon, floor, result, enemyHp);
        SaveProgress();
        RefreshUi();
        return actionResult;
    }

    private MythwakeActionResultDto ApplyResourceDungeonResult(bool isGoldDungeon, int floor, CombatResult result, int enemyHp)
    {
        var dungeon = isGoldDungeon ? GoldDungeonDefinition : EssenceDungeonDefinition;
        if (!result.won)
        {
            var failMessage = $"{dungeon.displayName} Floor {floor} failed after {result.elapsedSeconds}s\nEnemy HP {result.enemyHpRemaining}/{enemyHp}  {FormatCombatResult(result)}";
            PlayCombatVisual(dungeon.dungeonId, $"{dungeon.displayName} F{floor}", result, enemyHp);
            SetDungeonResult(failMessage);
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
        return CreateActionResult(true, $"{dungeon.dungeonId}_run", string.Empty, message, rewardDto);
    }

    private MythwakeActionResultDto ApplyDungeonFightResult(string dungeonId, int floor, CombatResult result, int enemyHp)
    {
        if (dungeonId == GoldDungeonDefinition.dungeonId)
        {
            return ApplyResourceDungeonResult(isGoldDungeon: true, floor, result, enemyHp);
        }

        if (dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return ApplyResourceDungeonResult(isGoldDungeon: false, floor, result, enemyHp);
        }

        if (dungeonId == GearDungeonDefinition.dungeonId)
        {
            return ApplyGearDungeonResult(floor, result, enemyHp);
        }

        var message = $"Unknown dungeon: {dungeonId}";
        SetDungeonResult(message);
        return CreateActionResult(false, "run_dungeon", "invalid_dungeon", message);
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
        var enemyHpValue = Mathf.Max(1, targetEnemyHp);
        var heroHpValues = CreateHeroCombatHealthValues();
        var nextHeroAttackTimes = new float[HeroCount];
        for (var i = 0; i < nextHeroAttackTimes.Length; i++)
        {
            nextHeroAttackTimes[i] = 0.25f + (i * 0.17f);
        }

        var enemyNextAttackTime = 0.9f;
        enemyDamage = Mathf.Max(1, enemyDamage);

        const float simulationStep = 0.1f;
        for (var timer = 0f; timer <= DefaultCombatDurationSeconds + 0.001f; timer += simulationStep)
        {
            for (var heroIndex = 0; heroIndex < HeroCount; heroIndex++)
            {
                if (heroHpValues[heroIndex] <= 0 || timer + 0.001f < nextHeroAttackTimes[heroIndex])
                {
                    continue;
                }

                nextHeroAttackTimes[heroIndex] = timer + GetFightVisualAttackInterval(true, heroIndex) * UnityEngine.Random.Range(0.94f, 1.08f);
                if (!RollPercentChance(GetHeroAccuracyPercent(heroIndex)))
                {
                    result.missedHits++;
                    continue;
                }

                var hitDamage = Mathf.Max(1, GetHeroEffectiveAttack(heroIndex));
                if (RollPercentChance(GetHeroCritChancePercent(heroIndex)))
                {
                    hitDamage = Mathf.Max(1, Mathf.RoundToInt(hitDamage * CritDamageMultiplier));
                    result.criticalHits++;
                }

                hitDamage = Mathf.Min(hitDamage, enemyHpValue);
                enemyHpValue -= hitDamage;
                result.damageDealt += hitDamage;

                if (GetHeroTextureName(heroIndex) == "hero_elowen" && RollPercentChance(25))
                {
                    result.healingDone += HealHeroCombatHealthValues(heroHpValues, Mathf.Max(1, Mathf.FloorToInt(maxTeamHp * 0.035f)));
                }

                if (enemyHpValue <= 0)
                {
                    result.won = true;
                    result.elapsedSeconds = Mathf.Clamp(Mathf.CeilToInt(timer), 1, DefaultCombatDurationSeconds);
                    result.teamHpRemaining = GetHeroCombatHealthTotal(heroHpValues);
                    result.enemyHpRemaining = 0;
                    return result;
                }
            }

            if (enemyHpValue > 0 && ShouldExecuteEnemy(enemyHpValue, targetEnemyHp))
            {
                result.executed = true;
                result.damageDealt += enemyHpValue;
                enemyHpValue = 0;
            }

            if (enemyHpValue <= 0)
            {
                result.won = true;
                result.elapsedSeconds = Mathf.Clamp(Mathf.CeilToInt(timer), 1, DefaultCombatDurationSeconds);
                result.teamHpRemaining = GetHeroCombatHealthTotal(heroHpValues);
                result.enemyHpRemaining = 0;
                return result;
            }

            if (timer + 0.001f >= enemyNextAttackTime)
            {
                var targetHero = PickLocalCombatEnemyTarget(heroHpValues);
                if (targetHero >= 0)
                {
                    var mitigatedDamage = GetMitigatedEnemyDamageAgainstHero(enemyDamage, targetHero);
                    var damageTaken = Mathf.Min(heroHpValues[targetHero], mitigatedDamage);
                    heroHpValues[targetHero] -= damageTaken;
                    result.damageTaken += damageTaken;
                }

                enemyNextAttackTime = timer + 1.45f;
                if (GetHeroCombatHealthTotal(heroHpValues) <= 0)
                {
                    result.won = false;
                    result.elapsedSeconds = Mathf.Clamp(Mathf.CeilToInt(timer), 1, DefaultCombatDurationSeconds);
                    result.teamHpRemaining = 0;
                    result.enemyHpRemaining = Mathf.Max(0, enemyHpValue);
                    return result;
                }
            }
        }

        result.won = false;
        result.elapsedSeconds = DefaultCombatDurationSeconds;
        result.teamHpRemaining = GetHeroCombatHealthTotal(heroHpValues);
        result.enemyHpRemaining = Mathf.Max(0, enemyHpValue);
        return result;
    }

    private int[] CreateHeroCombatHealthValues()
    {
        var values = new int[HeroCount];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = GetHeroCombatMaxHealth(i);
        }

        return values;
    }

    private int GetHeroCombatMaxHealth(int heroIndex)
    {
        heroIndex = Mathf.Clamp(heroIndex, 0, HeroCount - 1);
        return Mathf.Max(1, GetHeroHealth(heroIndex) + GetHeroGearHealthBonus(heroIndex));
    }

    private static int GetHeroCombatHealthTotal(int[] heroHpValues)
    {
        if (heroHpValues == null)
        {
            return 0;
        }

        var total = 0;
        for (var i = 0; i < heroHpValues.Length; i++)
        {
            total += Mathf.Max(0, heroHpValues[i]);
        }

        return total;
    }

    private int HealHeroCombatHealthValues(int[] heroHpValues, int totalHeal)
    {
        if (heroHpValues == null || totalHeal <= 0)
        {
            return 0;
        }

        var healed = 0;
        var share = Mathf.Max(1, Mathf.CeilToInt(totalHeal / (float)Mathf.Max(1, heroHpValues.Length)));
        for (var i = 0; i < heroHpValues.Length; i++)
        {
            if (heroHpValues[i] <= 0)
            {
                continue;
            }

            var maxHealth = GetHeroCombatMaxHealth(i);
            var amount = Mathf.Min(share, maxHealth - heroHpValues[i]);
            if (amount <= 0)
            {
                continue;
            }

            heroHpValues[i] += amount;
            healed += amount;
        }

        return healed;
    }

    private int PickLocalCombatEnemyTarget(int[] heroHpValues)
    {
        if (heroHpValues == null || heroHpValues.Length == 0)
        {
            return -1;
        }

        EnsureFormationOrder();
        if (formationSlotHeroIndices != null)
        {
            for (var slot = 0; slot < formationSlotHeroIndices.Length; slot++)
            {
                var heroIndex = Mathf.Clamp(formationSlotHeroIndices[slot], 0, HeroCount - 1);
                if (heroIndex < heroHpValues.Length && heroHpValues[heroIndex] > 0)
                {
                    return heroIndex;
                }
            }
        }

        for (var i = 0; i < heroHpValues.Length; i++)
        {
            if (heroHpValues[i] > 0)
            {
                return i;
            }
        }

        return -1;
    }

    private string FormatCombatResult(CombatResult result)
    {
        var executeText = result.executed ? "  Execute" : string.Empty;
        return $"DMG {result.damageDealt}  Took {result.damageTaken}  Heal {result.healingDone}  Crit {result.criticalHits}  Miss {result.missedHits}{executeText}";
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

    private IEnumerator PlayCampaignFightVisualRoutine(
        bool won,
        int stageNumber,
        string enemyLabel,
        int elapsedSeconds,
        int teamMaxHp,
        int teamHpRemaining,
        int enemyMaxHp,
        int enemyHpRemaining,
        int damageDealt,
        int damageTaken,
        bool singleBoss = false,
        string bossTextureName = null,
        int enemyDamage = 0)
    {
        SetBattleFlowMode(BattleFlowMode.Fight);
        ApplyBattleFlowVisibility();
        HideFightFloatingTexts();
        RefreshFightArenaBackground(singleBoss);
        PrepareFightAnimationTextures(stageNumber, singleBoss, bossTextureName);
        ConfigureFightEnemyPresentation(singleBoss);

        teamMaxHp = Mathf.Max(1, teamMaxHp);
        enemyMaxHp = Mathf.Max(1, enemyMaxHp);
        elapsedSeconds = Mathf.Clamp(elapsedSeconds, 1, DefaultCombatDurationSeconds);
        var teamEndPercent = Mathf.Clamp01(teamHpRemaining / (float)teamMaxHp);
        var enemyEndPercent = Mathf.Clamp01(enemyHpRemaining / (float)enemyMaxHp);
        var visualDuration = Mathf.Clamp(elapsedSeconds, 1f, DefaultCombatDurationSeconds);
        var timer = 0f;
        var activeEnemyCount = singleBoss ? 1 : HeroCount;
        var heroHpPercents = CreateCombatHealthPercents(HeroCount);
        var enemyHpPercents = CreateCombatHealthPercents(activeEnemyCount);
        var heroDamageUnit = Mathf.Max(0.0025f, (1f - enemyEndPercent) * activeEnemyCount / GetExpectedHeroVisualAttackWeight(visualDuration));
        var enemyDamageUnit = Mathf.Max(0.0025f, (1f - teamEndPercent) * HeroCount / GetExpectedEnemyVisualAttackWeight(visualDuration, activeEnemyCount));
        var floatingIndex = 0;
        var heroBasePositions = GetCurrentFightHeroPositions();
        var enemyBasePositions = singleBoss ? GetFightBossEnemyPositions() : GetFightEnemyPositions();
        var heroStates = CreateFightVisualUnitStates(heroBasePositions, HeroCount, 0.2f);
        var enemyStates = CreateFightVisualUnitStates(enemyBasePositions, activeEnemyCount, 0.45f);
        var animationTimer = 0f;
        var slowedWorldAnimationTimer = 0f;
        var ultimateCinematicHeroIndex = -1;
        var ultimateCinematicRemaining = 0f;
        InitializeFightSkillState();
        RefreshFightSkillHealthUi(heroHpPercents);

        if (fightResultRoot != null)
        {
            fightResultRoot.gameObject.SetActive(false);
        }

        if (fightVsText != null)
        {
            fightVsText.text = singleBoss ? enemyLabel : $"Stage {stageNumber}  VS  {enemyLabel}";
        }

        var maxVisualDuration = visualDuration;
        while (!fightCancelRequested && ShouldContinueFightVisual(timer, visualDuration, maxVisualDuration, won, heroHpPercents, HeroCount, teamEndPercent, enemyHpPercents, activeEnemyCount, enemyEndPercent))
        {
            var realDeltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            var ultimateCinematicActive = ultimateCinematicHeroIndex >= 0 && ultimateCinematicRemaining > 0f;
            var scaledDeltaTime = realDeltaTime * GetFightTimeScale();
            var combatDeltaTime = ultimateCinematicActive ? 0f : scaledDeltaTime;
            var worldDeltaTime = ultimateCinematicActive ? realDeltaTime * FightUltimateWorldSlowScale : scaledDeltaTime;
            var animationDeltaTime = ultimateCinematicActive ? realDeltaTime : scaledDeltaTime;
            var simulationDeltaTime = Mathf.Min(worldDeltaTime, 0.05f);
            timer += combatDeltaTime;
            animationTimer += animationDeltaTime;
            slowedWorldAnimationTimer += worldDeltaTime;

            if (!ultimateCinematicActive)
            {
                slowedWorldAnimationTimer = animationTimer;
            }

            var progress = Mathf.Clamp01(timer / visualDuration);
            var smooth = Mathf.SmoothStep(0f, 1f, progress);
            var shownSecond = Mathf.Clamp(Mathf.FloorToInt(timer), 0, DefaultCombatDurationSeconds);
            var remainingSeconds = Mathf.Max(0, DefaultCombatDurationSeconds - shownSecond);

            if (fightTimerText != null)
            {
                fightTimerText.text = FormatFightTimer(remainingSeconds);
            }

            if (fightStatusText != null)
            {
                fightStatusText.text = ultimateCinematicActive
                    ? $"{GetHeroDefinition(ultimateCinematicHeroIndex).name} unleashes Ultimate"
                    : $"Dealt {FormatCompactNumber(Mathf.RoundToInt(damageDealt * smooth))}   Took {FormatCompactNumber(Mathf.RoundToInt(damageTaken * smooth))}";
            }

            UpdateFightVisualMovement(heroStates, HeroCount, heroHpPercents, enemyStates, activeEnemyCount, enemyHpPercents, true, simulationDeltaTime);
            UpdateFightVisualMovement(enemyStates, activeEnemyCount, enemyHpPercents, heroStates, HeroCount, heroHpPercents, false, simulationDeltaTime);
            var finishScale = timer > visualDuration ? 1.85f : 1f;
            if (!ultimateCinematicActive)
            {
                ResolveFightVisualAttacks(heroStates, HeroCount, heroHpPercents, enemyStates, activeEnemyCount, enemyHpPercents, true, timer, animationTimer, enemyEndPercent, heroDamageUnit * finishScale, stageNumber, enemyDamage, ref floatingIndex);
                ResolveFightVisualAttacks(enemyStates, activeEnemyCount, enemyHpPercents, heroStates, HeroCount, heroHpPercents, false, timer, animationTimer, teamEndPercent, enemyDamageUnit * finishScale, stageNumber, enemyDamage, ref floatingIndex);
                var ultimateHeroIndex = ResolveFightHeroUltimates(heroStates, heroHpPercents, enemyStates, activeEnemyCount, enemyHpPercents, timer, animationTimer, enemyEndPercent, heroDamageUnit * finishScale, ref floatingIndex);
                if (ultimateHeroIndex >= 0)
                {
                    ultimateCinematicHeroIndex = ultimateHeroIndex;
                    ultimateCinematicRemaining = FightUltimateCinematicSeconds;
                }
            }

            SetFillValues(fightHeroHpFills, heroHpPercents, HeroCount);
            SetFillValues(fightEnemyHpFills, enemyHpPercents, activeEnemyCount);
            SetHpPercentTexts(fightEnemyHpPercentTexts, enemyHpPercents, activeEnemyCount);
            RefreshFightBossHpUi(singleBoss, enemyHpPercents);
            RefreshFightSkillHealthUi(heroHpPercents);
            RefreshFightSkillUi(animationTimer);
            AnimateFightUnitsWithState(
                heroStates,
                enemyStates,
                heroHpPercents,
                enemyHpPercents,
                activeEnemyCount,
                animationTimer,
                slowedWorldAnimationTimer,
                singleBoss,
                ultimateCinematicHeroIndex,
                ultimateCinematicRemaining);

            if (ultimateCinematicHeroIndex >= 0)
            {
                ultimateCinematicRemaining = Mathf.Max(0f, ultimateCinematicRemaining - realDeltaTime);
                if (ultimateCinematicRemaining <= 0f)
                {
                    ultimateCinematicHeroIndex = -1;
                    slowedWorldAnimationTimer = animationTimer;
                }
            }

            yield return null;
        }

        if (fightCancelRequested)
        {
            SetProjectilesVisible(fightHeroProjectileImages, false);
            SetProjectilesVisible(fightEnemyProjectileImages, false);
            SetRawImagesVisible(fightHeroFxImages, false);
            HideRavikSkeletalViews(fightHeroSkeletalViews);
            if (fightTimerText != null)
            {
                fightTimerText.text = "Ended";
            }

            if (fightStatusText != null)
            {
                fightStatusText.text = "Fight stopped.";
            }

            yield break;
        }

        ReduceCombatHealthToAverage(heroHpPercents, HeroCount, teamEndPercent);
        ReduceCombatHealthToAverage(enemyHpPercents, activeEnemyCount, enemyEndPercent);
        SetFillValues(fightHeroHpFills, heroHpPercents, HeroCount);
        SetFillValues(fightEnemyHpFills, enemyHpPercents, activeEnemyCount);
        SetHpPercentTexts(fightEnemyHpPercentTexts, enemyHpPercents, activeEnemyCount);
        RefreshFightBossHpUi(singleBoss, enemyHpPercents);
        SetProjectilesVisible(fightHeroProjectileImages, false);
        SetProjectilesVisible(fightEnemyProjectileImages, false);
        SetRawImagesVisible(fightHeroFxImages, false);
        HideRavikSkeletalViews(fightHeroSkeletalViews);
        RefreshFightSkillHealthUi(heroHpPercents);
        RefreshFightSkillUi(animationTimer);
        if (fightTimerText != null)
        {
            fightTimerText.text = won ? "Clear" : "00:00";
        }

        if (fightStatusText != null)
        {
            fightStatusText.text = won ? "Enemy team defeated." : "Your team ran out of time or HP.";
        }
    }

    private void ShowCampaignFightResult(bool success, string title, string body)
    {
        campaignFightInProgress = false;
        SetBattleFlowMode(BattleFlowMode.Result);

        if (fightResultTitleText != null)
        {
            fightResultTitleText.text = title;
            fightResultTitleText.color = success ? new Color(1f, 0.86f, 0.34f) : new Color(1f, 0.42f, 0.36f);
        }

        if (fightResultBodyText != null)
        {
            fightResultBodyText.text = body;
        }

        if (fightContinueButton != null)
        {
            fightContinueButton.interactable = true;
        }

        RefreshGameplayInteractivity();
        QueueAutoContinueFight(success);
    }

    private void QueueAutoContinueFight(bool success)
    {
        if (!success || !autoContinueFightsEnabled || fightCancelRequested)
        {
            return;
        }

        if (autoContinueFightCoroutine != null)
        {
            StopCoroutine(autoContinueFightCoroutine);
        }

        autoContinueFightCoroutine = StartCoroutine(AutoContinueFightAfterVictoryRoutine());
    }

    private IEnumerator AutoContinueFightAfterVictoryRoutine()
    {
        yield return new WaitForSeconds(0.75f);
        autoContinueFightCoroutine = null;

        if (!autoContinueFightsEnabled || campaignFightInProgress || backendRequestInProgress || backendLifecycleFlushInProgress || activeScreen != AppScreen.Battle)
        {
            yield break;
        }

        selectedFormationSlotIndex = -1;
        fightAutoSkillsEnabled = true;
        if (battleTargetMode == BattleTargetMode.Campaign)
        {
            selectedCampaignStage = Mathf.Max(1, enemyLevel);
        }

        if (battleTargetMode == BattleTargetMode.Dungeon)
        {
            SelectRandomDungeonBattleMap(selectedDungeonId);
            StartDungeonFightFromFormation();
        }
        else
        {
            StartCampaignFightFromFormation();
        }
    }

    private void InitializeFightSkillState()
    {
        fightHeroManaValues = new int[HeroCount];
        fightHeroMaxManaValues = new int[HeroCount];
        fightHeroUltimateQueued = new bool[HeroCount];
        fightHeroUltimateStartedAt = new float[HeroCount];
        fightHeroVisibleHpPercents = CreateCombatHealthPercents(HeroCount);
        for (var i = 0; i < HeroCount; i++)
        {
            fightHeroMaxManaValues[i] = GetHeroMaxMana(i);
            fightHeroUltimateStartedAt[i] = -99f;
        }

        if (autoContinueFightsEnabled)
        {
            fightAutoSkillsEnabled = true;
        }

        RefreshFightSkillUi(0f);
        RefreshFightSkillHealthUi(fightHeroVisibleHpPercents);
        RefreshFightAutoSkillButton();
        RefreshFightSpeedButton();
    }

    private void ToggleFightAutoSkills()
    {
        fightAutoSkillsEnabled = !fightAutoSkillsEnabled;
        RefreshFightAutoSkillButton();
        RefreshFightSkillUi(0f);
    }

    private void RefreshFightAutoSkillButton()
    {
        if (fightAutoSkillButton == null)
        {
            return;
        }

        var image = fightAutoSkillButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = fightAutoSkillsEnabled
                ? new Color(1f, 0.68f, 0.2f, 0.98f)
                : new Color(0.12f, 0.13f, 0.17f, 0.9f);
        }

        if (fightAutoSkillButtonText != null)
        {
            fightAutoSkillButtonText.text = fightAutoSkillsEnabled ? "AUTO\nON" : "AUTO";
            fightAutoSkillButtonText.color = fightAutoSkillsEnabled ? new Color(0.18f, 0.09f, 0.02f) : Color.white;
        }
    }

    private void ToggleFightSpeed()
    {
        fightDoubleSpeedEnabled = !fightDoubleSpeedEnabled;
        RefreshFightSpeedButton();
    }

    private float GetFightTimeScale()
    {
        return fightDoubleSpeedEnabled ? 2f : 1f;
    }

    private void RefreshFightSpeedButton()
    {
        if (fightSpeedButton == null)
        {
            return;
        }

        var image = fightSpeedButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = fightDoubleSpeedEnabled
                ? new Color(0.26f, 0.76f, 1f, 0.98f)
                : new Color(0.12f, 0.13f, 0.17f, 0.9f);
        }

        if (fightSpeedButtonText != null)
        {
            fightSpeedButtonText.text = fightDoubleSpeedEnabled ? "x2\nON" : "x2";
            fightSpeedButtonText.color = fightDoubleSpeedEnabled ? new Color(0.02f, 0.08f, 0.14f) : Color.white;
        }
    }

    private void QueueFightHeroUltimate(int heroIndex)
    {
        if (fightHeroManaValues == null || fightHeroMaxManaValues == null || fightHeroUltimateQueued == null)
        {
            return;
        }

        if (heroIndex < 0
            || heroIndex >= HeroCount
            || fightHeroManaValues[heroIndex] < fightHeroMaxManaValues[heroIndex]
            || (fightHeroVisibleHpPercents != null && heroIndex < fightHeroVisibleHpPercents.Length && fightHeroVisibleHpPercents[heroIndex] <= 0.001f))
        {
            return;
        }

        fightHeroUltimateQueued[heroIndex] = true;
        RefreshFightSkillUi(0f);
    }

    private void GainFightHeroMana(int heroIndex, int amount)
    {
        if (fightHeroManaValues == null || fightHeroMaxManaValues == null || heroIndex < 0 || heroIndex >= HeroCount)
        {
            return;
        }

        fightHeroManaValues[heroIndex] = Mathf.Clamp(fightHeroManaValues[heroIndex] + Mathf.Max(0, amount), 0, Mathf.Max(1, fightHeroMaxManaValues[heroIndex]));
    }

    private void TryApplyHeroPassiveOnHit(int heroIndex, float[] heroHpPercents, int heroCount, Vector2 targetPosition, ref int floatingIndex)
    {
        var heroId = GetHeroTextureName(heroIndex);
        if (heroId == "hero_elowen" && UnityEngine.Random.value <= 0.25f)
        {
            var healed = HealCombatHealth(heroHpPercents, heroCount, 0.035f);
            if (healed > 0.001f)
            {
                ShowFightFloatingText(floatingIndex++, "+Heal", targetPosition + new Vector2(0, -128), new Color(0.45f, 1f, 0.58f));
            }
        }

        if (heroId == "hero_ravik" && UnityEngine.Random.value <= 0.22f)
        {
            GainFightHeroMana(heroIndex, 2);
            ShowFightFloatingText(floatingIndex++, "Cinder", targetPosition + new Vector2(0, -128), new Color(1f, 0.48f, 0.18f));
        }
    }

    private int ResolveFightHeroUltimates(
        FightVisualUnitState[] heroStates,
        float[] heroHpPercents,
        FightVisualUnitState[] enemyStates,
        int activeEnemyCount,
        float[] enemyHpPercents,
        float timer,
        float animationTimer,
        float enemyEndPercent,
        float heroDamageUnit,
        ref int floatingIndex)
    {
        if (heroStates == null || enemyStates == null || heroHpPercents == null || enemyHpPercents == null || fightHeroManaValues == null || fightHeroMaxManaValues == null)
        {
            return -1;
        }

        for (var i = 0; i < HeroCount && i < heroStates.Length && i < heroHpPercents.Length; i++)
        {
            if (heroHpPercents[i] <= 0.001f || fightHeroManaValues[i] < fightHeroMaxManaValues[i])
            {
                continue;
            }

            if (!fightAutoSkillsEnabled && (fightHeroUltimateQueued == null || !fightHeroUltimateQueued[i]))
            {
                continue;
            }

            var state = heroStates[i];
            if (!IsFightTargetAlive(state.targetIndex, enemyHpPercents, activeEnemyCount))
            {
                state.targetIndex = FindNearestLivingVisualTarget(state.position, enemyStates, enemyHpPercents, activeEnemyCount);
            }

            if (state.targetIndex < 0 || !IsFightVisualInRange(state, enemyStates[state.targetIndex].position, true, i))
            {
                heroStates[i] = state;
                continue;
            }

            fightHeroManaValues[i] = 0;
            if (fightHeroUltimateQueued != null)
            {
                fightHeroUltimateQueued[i] = false;
            }

            if (fightHeroUltimateStartedAt != null && i < fightHeroUltimateStartedAt.Length)
            {
                fightHeroUltimateStartedAt[i] = animationTimer;
            }

            state.attackStartedAt = animationTimer;
            state.nextAttackTime = Mathf.Max(state.nextAttackTime, timer + 0.45f);
            heroStates[i] = state;

            var heroId = GetHeroTextureName(i);
            var targetPosition = enemyStates[state.targetIndex].position;
            if (heroId == "hero_elowen")
            {
                HealCombatHealth(heroHpPercents, HeroCount, 0.18f);
                ApplyTargetDamageTowardAverage(
                    enemyHpPercents,
                    state.targetIndex,
                    activeEnemyCount,
                    enemyEndPercent,
                    heroDamageUnit * GetFightVisualDamageWeight(true, i) * GetHeroUltimateDamageMultiplier(i));
                ShowFightFloatingText(floatingIndex++, "Wild Bloom", state.position + new Vector2(0, -120), new Color(0.55f, 1f, 0.62f));
                return i;
            }

            if (heroId == "hero_borin")
            {
                HealCombatHealth(heroHpPercents, HeroCount, 0.06f);
            }

            if (heroId == "hero_ravik")
            {
                var mainDamage = ApplyTargetDamageTowardAverage(
                    enemyHpPercents,
                    state.targetIndex,
                    activeEnemyCount,
                    enemyEndPercent,
                    heroDamageUnit * GetFightVisualDamageWeight(true, i) * GetHeroUltimateDamageMultiplier(i));

                for (var enemyIndex = 0; enemyIndex < activeEnemyCount; enemyIndex++)
                {
                    if (enemyIndex == state.targetIndex || !IsFightTargetAlive(enemyIndex, enemyHpPercents, activeEnemyCount))
                    {
                        continue;
                    }

                    ApplyTargetDamageTowardAverage(
                        enemyHpPercents,
                        enemyIndex,
                        activeEnemyCount,
                        enemyEndPercent,
                        heroDamageUnit * GetFightVisualDamageWeight(true, i) * GetHeroUltimateDamageMultiplier(i) * 0.38f);
                }

                if (mainDamage > 0.001f)
                {
                    ShowFightFloatingText(floatingIndex++, "Dragonflame", targetPosition + new Vector2(0, -118), new Color(1f, 0.42f, 0.14f));
                }

                return i;
            }

            var damage = ApplyTargetDamageTowardAverage(
                enemyHpPercents,
                state.targetIndex,
                activeEnemyCount,
                enemyEndPercent,
                heroDamageUnit * GetFightVisualDamageWeight(true, i) * GetHeroUltimateDamageMultiplier(i));

            if (damage > 0.001f)
            {
                ShowFightFloatingText(floatingIndex++, "ULT", targetPosition + new Vector2(0, -118), new Color(1f, 0.82f, 0.24f));
            }

            return i;
        }

        return -1;
    }

    private void RefreshFightSkillUi(float timer)
    {
        if (fightSkillButtons == null || fightSkillManaFills == null || fightHeroManaValues == null || fightHeroMaxManaValues == null)
        {
            return;
        }

        for (var i = 0; i < HeroCount; i++)
        {
            var maxMana = i < fightHeroMaxManaValues.Length ? Mathf.Max(1, fightHeroMaxManaValues[i]) : GetHeroMaxMana(i);
            var mana = i < fightHeroManaValues.Length ? Mathf.Clamp(fightHeroManaValues[i], 0, maxMana) : 0;
            var alive = fightHeroVisibleHpPercents == null || i >= fightHeroVisibleHpPercents.Length || fightHeroVisibleHpPercents[i] > 0.001f;
            var ready = alive && mana >= maxMana;
            var queued = alive && fightHeroUltimateQueued != null && i < fightHeroUltimateQueued.Length && fightHeroUltimateQueued[i];
            var activeAge = fightHeroUltimateStartedAt != null && i < fightHeroUltimateStartedAt.Length ? timer - fightHeroUltimateStartedAt[i] : 99f;
            var pulse = ready ? (0.5f + Mathf.Sin(Time.unscaledTime * 7.5f) * 0.5f) : 0f;
            var activePulse = IsFightActionActive(activeAge) ? Mathf.Sin(Mathf.Clamp01(activeAge / 0.72f) * Mathf.PI) : 0f;

            if (fightSkillManaFills[i] != null)
            {
                SetRuntimeFillPercent(fightSkillManaFills[i], mana / (float)maxMana);
                fightSkillManaFills[i].color = ready
                    ? new Color(1f, 0.68f, 0.18f, 0.98f)
                    : new Color(0.14f, 0.66f, 1f, 0.94f);
            }

            if (fightSkillBackplates != null && i < fightSkillBackplates.Length && fightSkillBackplates[i] != null)
            {
                fightSkillBackplates[i].color = queued
                    ? new Color(1f, 0.52f, 0.14f, 1f)
                    : ready
                        ? Color.Lerp(new Color(0.24f, 0.14f, 0.04f, 0.98f), new Color(1f, 0.77f, 0.2f, 1f), pulse)
                        : new Color(0.07f, 0.09f, 0.13f, 0.96f);
            }

            if (fightSkillPortraits != null && i < fightSkillPortraits.Length && fightSkillPortraits[i] != null)
            {
                fightSkillPortraits[i].color = !alive
                    ? new Color(0.38f, 0.38f, 0.42f, 0.72f)
                    : ready ? Color.white : new Color(0.72f, 0.78f, 0.86f, 0.94f);
                var scale = 1f + (ready ? 0.04f * pulse : 0f) + (activePulse * 0.12f);
                fightSkillPortraits[i].rectTransform.localScale = new Vector3(GetHeroFacingScale(i) * scale, scale, 1f);
            }

            if (fightSkillNameTexts != null && i < fightSkillNameTexts.Length && fightSkillNameTexts[i] != null)
            {
                fightSkillNameTexts[i].color = !alive
                    ? new Color(0.72f, 0.72f, 0.76f)
                    : ready ? new Color(1f, 0.86f, 0.3f) : Color.white;
            }

            if (fightSkillManaTexts != null && i < fightSkillManaTexts.Length && fightSkillManaTexts[i] != null)
            {
                fightSkillManaTexts[i].text = $"{mana}/{maxMana}";
                fightSkillManaTexts[i].color = ready ? new Color(1f, 0.82f, 0.25f) : new Color(0.74f, 0.9f, 1f);
            }
        }
    }

    private void RefreshFightSkillHealthUi(float[] heroHpPercents)
    {
        if (heroHpPercents == null)
        {
            return;
        }

        if (fightHeroVisibleHpPercents == null || fightHeroVisibleHpPercents.Length != HeroCount)
        {
            fightHeroVisibleHpPercents = new float[HeroCount];
        }

        for (var i = 0; i < HeroCount; i++)
        {
            var percent = i < heroHpPercents.Length ? Mathf.Clamp01(heroHpPercents[i]) : 0f;
            fightHeroVisibleHpPercents[i] = percent;

            if (fightSkillHpFills != null && i < fightSkillHpFills.Length && fightSkillHpFills[i] != null)
            {
                SetRuntimeFillPercent(fightSkillHpFills[i], percent);
                fightSkillHpFills[i].color = percent <= 0.001f
                    ? new Color(0.34f, 0.34f, 0.36f, 0.95f)
                    : Color.Lerp(new Color(0.92f, 0.26f, 0.18f, 0.96f), new Color(0.16f, 0.78f, 0.33f, 0.96f), Mathf.Clamp01(percent * 1.4f));
            }

            if (fightSkillHpTexts != null && i < fightSkillHpTexts.Length && fightSkillHpTexts[i] != null)
            {
                fightSkillHpTexts[i].text = percent <= 0.001f ? "KO" : FormatPercent(percent);
                fightSkillHpTexts[i].color = percent <= 0.001f ? new Color(0.86f, 0.86f, 0.9f) : Color.white;
            }
        }
    }

    private static float HealCombatHealth(float[] healthPercents, int activeCount, float healPercent)
    {
        if (healthPercents == null || healPercent <= 0f)
        {
            return 0f;
        }

        activeCount = Mathf.Clamp(activeCount, 0, healthPercents.Length);
        if (activeCount <= 0)
        {
            return 0f;
        }

        var totalHealed = 0f;
        var share = healPercent / activeCount;
        for (var i = 0; i < activeCount; i++)
        {
            var before = Mathf.Clamp01(healthPercents[i]);
            healthPercents[i] = Mathf.Min(1f, before + share);
            totalHealed += healthPercents[i] - before;
        }

        return totalHealed;
    }

    private static void SetFillArray(Image[] fills, float percent)
    {
        SetFillArray(fills, percent, fills == null ? 0 : fills.Length);
    }

    private static void SetFillArray(Image[] fills, float percent, int activeCount)
    {
        if (fills == null)
        {
            return;
        }

        activeCount = Mathf.Clamp(activeCount, 0, fills.Length);
        for (var i = 0; i < activeCount; i++)
        {
            SetRuntimeFillPercent(fills[i], percent);
        }
    }

    private static float[] CreateCombatHealthPercents(int count)
    {
        count = Mathf.Max(0, count);
        var values = new float[count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = 1f;
        }

        return values;
    }

    private static void SetFillValues(Image[] fills, float[] values, int activeCount)
    {
        if (fills == null || values == null)
        {
            return;
        }

        activeCount = Mathf.Clamp(activeCount, 0, Mathf.Min(fills.Length, values.Length));
        for (var i = 0; i < activeCount; i++)
        {
            SetRuntimeFillPercent(fills[i], values[i]);
        }
    }

    private static void SetHpPercentTexts(TMP_Text[] texts, float[] values, int activeCount)
    {
        if (texts == null || values == null)
        {
            return;
        }

        activeCount = Mathf.Clamp(activeCount, 0, Mathf.Min(texts.Length, values.Length));
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
            {
                continue;
            }

            var visible = i < activeCount && values[i] > 0.001f;
            texts[i].gameObject.SetActive(visible);
            if (visible)
            {
                texts[i].text = FormatPercent(values[i]);
            }
        }
    }

    private void RefreshFightBossHpUi(bool singleBoss, float[] enemyHpPercents)
    {
        var bossHpRoot = fightBossHpFill == null || fightBossHpFill.transform.parent == null ? null : fightBossHpFill.transform.parent.gameObject;
        if (bossHpRoot != null)
        {
            bossHpRoot.SetActive(singleBoss);
        }

        if (!singleBoss || enemyHpPercents == null || enemyHpPercents.Length == 0)
        {
            return;
        }

        var percent = Mathf.Clamp01(enemyHpPercents[0]);
        SetRuntimeFillPercent(fightBossHpFill, percent);
        if (fightBossHpText != null)
        {
            fightBossHpText.text = $"Boss HP {FormatPercent(percent)}";
        }
    }

    private static string FormatPercent(float value)
    {
        return $"{Mathf.CeilToInt(Mathf.Clamp01(value) * 100f)}%";
    }

    private static float GetAverageCombatHealth(float[] values, int activeCount)
    {
        if (values == null)
        {
            return 0f;
        }

        activeCount = Mathf.Clamp(activeCount, 0, values.Length);
        if (activeCount <= 0)
        {
            return 0f;
        }

        var total = 0f;
        for (var i = 0; i < activeCount; i++)
        {
            total += Mathf.Clamp01(values[i]);
        }

        return total / activeCount;
    }

    private static int PickNextLivingCombatant(float[] healthPercents, int startIndex, int activeCount)
    {
        if (healthPercents == null)
        {
            return -1;
        }

        activeCount = Mathf.Clamp(activeCount, 0, healthPercents.Length);
        for (var offset = 0; offset < activeCount; offset++)
        {
            var index = (Mathf.Max(0, startIndex) + offset) % activeCount;
            if (healthPercents[index] > 0.001f)
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindNearestLivingCombatant(Vector2 source, Vector2[] targetPositions, float[] targetHealthPercents, int activeCount)
    {
        if (targetPositions == null || targetHealthPercents == null)
        {
            return -1;
        }

        activeCount = Mathf.Clamp(activeCount, 0, Mathf.Min(targetPositions.Length, targetHealthPercents.Length));
        var closestIndex = -1;
        var closestDistance = float.MaxValue;
        for (var i = 0; i < activeCount; i++)
        {
            if (targetHealthPercents[i] <= 0.001f)
            {
                continue;
            }

            var distance = (targetPositions[i] - source).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private static float ApplyTargetDamageTowardAverage(float[] healthPercents, int targetIndex, int activeCount, float targetAverage, float requestedDamagePercent)
    {
        if (healthPercents == null || targetIndex < 0 || targetIndex >= healthPercents.Length)
        {
            return 0f;
        }

        activeCount = Mathf.Clamp(activeCount, 0, healthPercents.Length);
        if (activeCount <= 0)
        {
            return 0f;
        }

        targetAverage = Mathf.Clamp01(targetAverage);
        requestedDamagePercent = Mathf.Max(0f, requestedDamagePercent);
        var remainingTotalDamage = Mathf.Max(0f, (GetAverageCombatHealth(healthPercents, activeCount) - targetAverage) * activeCount);
        var actualDamage = Mathf.Min(requestedDamagePercent, remainingTotalDamage, Mathf.Clamp01(healthPercents[targetIndex]));
        healthPercents[targetIndex] = Mathf.Max(0f, healthPercents[targetIndex] - actualDamage);
        return actualDamage;
    }

    private static void ReduceCombatHealthToAverage(float[] healthPercents, int activeCount, float targetAverage)
    {
        if (healthPercents == null)
        {
            return;
        }

        activeCount = Mathf.Clamp(activeCount, 0, healthPercents.Length);
        if (activeCount <= 0)
        {
            return;
        }

        targetAverage = Mathf.Clamp01(targetAverage);
        var excess = Mathf.Max(0f, (GetAverageCombatHealth(healthPercents, activeCount) - targetAverage) * activeCount);
        var guard = 0;
        while (excess > 0.001f && guard++ < activeCount * 2)
        {
            var reducibleCount = 0;
            for (var i = 0; i < activeCount; i++)
            {
                if (healthPercents[i] > targetAverage + 0.001f)
                {
                    reducibleCount++;
                }
            }

            if (reducibleCount <= 0)
            {
                break;
            }

            var share = excess / reducibleCount;
            var changed = false;
            for (var i = 0; i < activeCount && excess > 0.001f; i++)
            {
                if (healthPercents[i] <= targetAverage + 0.001f)
                {
                    continue;
                }

                var reduction = Mathf.Min(share, healthPercents[i] - targetAverage, excess);
                if (reduction <= 0.001f)
                {
                    continue;
                }

                healthPercents[i] = Mathf.Max(0f, healthPercents[i] - reduction);
                excess -= reduction;
                changed = true;
            }

            if (!changed)
            {
                break;
            }
        }
    }

    private static Vector2 GetIndexedPosition(Vector2[] positions, int index)
    {
        return positions != null && index >= 0 && index < positions.Length ? positions[index] : Vector2.zero;
    }

    private static FightVisualUnitState[] CreateFightVisualUnitStates(Vector2[] positions, int count, float firstAttackOffset)
    {
        count = Mathf.Clamp(count, 0, positions == null ? 0 : positions.Length);
        var states = new FightVisualUnitState[count];
        for (var i = 0; i < count; i++)
        {
            states[i] = new FightVisualUnitState
            {
                position = ClampFightArenaPosition(GetIndexedPosition(positions, i)),
                lockedMeleePosition = Vector2.zero,
                targetIndex = -1,
                lockedMeleeTargetIndex = -1,
                nextAttackTime = firstAttackOffset + (i * 0.16f),
                attackStartedAt = -99f,
                hasLockedMeleePosition = false,
                isMoving = false
            };
        }

        return states;
    }

    private static bool IsFightTargetAlive(int targetIndex, float[] targetHealthPercents, int activeTargetCount)
    {
        return targetHealthPercents != null
            && targetIndex >= 0
            && targetIndex < activeTargetCount
            && targetIndex < targetHealthPercents.Length
            && targetHealthPercents[targetIndex] > 0.001f;
    }

    private static int FindNearestLivingVisualTarget(Vector2 source, FightVisualUnitState[] targetStates, float[] targetHealthPercents, int activeTargetCount)
    {
        if (targetStates == null || targetHealthPercents == null)
        {
            return -1;
        }

        activeTargetCount = Mathf.Clamp(activeTargetCount, 0, Mathf.Min(targetStates.Length, targetHealthPercents.Length));
        var closestIndex = -1;
        var closestDistance = float.MaxValue;
        for (var i = 0; i < activeTargetCount; i++)
        {
            if (targetHealthPercents[i] <= 0.001f)
            {
                continue;
            }

            var distance = (targetStates[i].position - source).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private static bool ShouldContinueFightVisual(
        float timer,
        float visualDuration,
        float maxVisualDuration,
        bool won,
        float[] heroHpPercents,
        int heroCount,
        float teamEndPercent,
        float[] enemyHpPercents,
        int enemyCount,
        float enemyEndPercent)
    {
        if (won && !HasLivingCombatant(enemyHpPercents, enemyCount))
        {
            return false;
        }

        if (!won && !HasLivingCombatant(heroHpPercents, heroCount))
        {
            return false;
        }

        if (timer < visualDuration)
        {
            return true;
        }

        if (timer >= maxVisualDuration)
        {
            return false;
        }

        var values = won ? enemyHpPercents : heroHpPercents;
        var count = won ? enemyCount : heroCount;
        var targetAverage = won ? enemyEndPercent : teamEndPercent;
        return GetAverageCombatHealth(values, count) > targetAverage + 0.002f;
    }

    private static bool HasLivingCombatant(float[] healthPercents, int activeCount)
    {
        if (healthPercents == null)
        {
            return false;
        }

        activeCount = Mathf.Clamp(activeCount, 0, healthPercents.Length);
        for (var i = 0; i < activeCount; i++)
        {
            if (healthPercents[i] > 0.001f)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateFightVisualMovement(
        FightVisualUnitState[] attackers,
        int attackerCount,
        float[] attackerHealthPercents,
        FightVisualUnitState[] targets,
        int activeTargetCount,
        float[] targetHealthPercents,
        bool attackersAreHeroes,
        float deltaTime)
    {
        if (attackers == null || targets == null || attackerHealthPercents == null)
        {
            return;
        }

        attackerCount = Mathf.Clamp(attackerCount, 0, Mathf.Min(attackers.Length, attackerHealthPercents.Length));
        for (var i = 0; i < attackerCount; i++)
        {
            var state = attackers[i];
            if (attackerHealthPercents[i] <= 0.001f)
            {
                state.isMoving = false;
                attackers[i] = state;
                continue;
            }

            if (!IsFightTargetAlive(state.targetIndex, targetHealthPercents, activeTargetCount))
            {
                state.targetIndex = FindNearestLivingVisualTarget(state.position, targets, targetHealthPercents, activeTargetCount);
                state.hasLockedMeleePosition = false;
                state.lockedMeleeTargetIndex = -1;
            }

            if (state.targetIndex < 0)
            {
                state.isMoving = false;
                attackers[i] = state;
                continue;
            }

            if (IsFightVisualRanged(attackersAreHeroes, i))
            {
                state.isMoving = false;
                state.position = ClampFightArenaPosition(state.position);
                attackers[i] = state;
                continue;
            }

            var targetIsRanged = IsFightVisualRanged(!attackersAreHeroes, state.targetIndex);
            if (!state.hasLockedMeleePosition || state.lockedMeleeTargetIndex != state.targetIndex)
            {
                state.lockedMeleePosition = GetMeleeContactPosition(state.position, targets[state.targetIndex].position, attackersAreHeroes, i, state.targetIndex, targetIsRanged);
                state.lockedMeleeTargetIndex = state.targetIndex;
                state.hasLockedMeleePosition = true;
            }
            else if (Vector2.Distance(state.position, state.lockedMeleePosition) <= 10f
                && Vector2.Distance(state.lockedMeleePosition, targets[state.targetIndex].position) > GetFightVisualMeleeRange(targetIsRanged) + 28f)
            {
                state.lockedMeleePosition = GetMeleeContactPosition(state.position, targets[state.targetIndex].position, attackersAreHeroes, i, state.targetIndex, targetIsRanged);
            }

            var desiredPosition = state.lockedMeleePosition;
            var distance = Vector2.Distance(state.position, desiredPosition);
            if (distance > 6f)
            {
                var moveSpeed = attackersAreHeroes ? GetHeroVisualMoveSpeed(i) : GetEnemyVisualMoveSpeed(GetFightEnemyTextureName(i));
                state.position = ClampFightArenaPosition(Vector2.MoveTowards(state.position, desiredPosition, moveSpeed * deltaTime));
                state.isMoving = true;
            }
            else
            {
                state.position = ClampFightArenaPosition(desiredPosition);
                state.isMoving = false;
            }

            attackers[i] = state;
        }
    }

    private void ResolveFightVisualAttacks(
        FightVisualUnitState[] attackers,
        int attackerCount,
        float[] attackerHealthPercents,
        FightVisualUnitState[] targets,
        int activeTargetCount,
        float[] targetHealthPercents,
        bool attackersAreHeroes,
        float timer,
        float animationTimer,
        float targetEndAverage,
        float damageUnit,
        int stageNumber,
        int enemyDamage,
        ref int floatingIndex)
    {
        if (attackers == null || targets == null || attackerHealthPercents == null || targetHealthPercents == null)
        {
            return;
        }

        attackerCount = Mathf.Clamp(attackerCount, 0, Mathf.Min(attackers.Length, attackerHealthPercents.Length));
        for (var i = 0; i < attackerCount; i++)
        {
            var state = attackers[i];
            if (attackerHealthPercents[i] <= 0.001f)
            {
                continue;
            }

            if (!IsFightTargetAlive(state.targetIndex, targetHealthPercents, activeTargetCount))
            {
                state.targetIndex = FindNearestLivingVisualTarget(state.position, targets, targetHealthPercents, activeTargetCount);
            }

            if (state.targetIndex < 0 || !IsFightVisualInRange(state, targets[state.targetIndex].position, attackersAreHeroes, i))
            {
                attackers[i] = state;
                continue;
            }

            if (timer < state.nextAttackTime)
            {
                attackers[i] = state;
                continue;
            }

            state.attackStartedAt = animationTimer;
            state.nextAttackTime = timer + GetFightVisualAttackInterval(attackersAreHeroes, i) * UnityEngine.Random.Range(0.94f, 1.08f);
            attackers[i] = state;

            if (GetAverageCombatHealth(targetHealthPercents, activeTargetCount) <= targetEndAverage + 0.002f)
            {
                continue;
            }

            var targetPosition = targets[state.targetIndex].position;
            if (attackersAreHeroes && !RollPercentChance(GetHeroAccuracyPercent(i)))
            {
                ShowFightFloatingText(floatingIndex++, "MISS", targetPosition + new Vector2(UnityEngine.Random.Range(-36, 36), UnityEngine.Random.Range(-88, -32)), new Color(0.72f, 0.82f, 0.95f));
                continue;
            }

            var crit = attackersAreHeroes && RollPercentChance(GetHeroCritChancePercent(i));
            var damageWeight = GetFightVisualDamageWeight(attackersAreHeroes, i);
            var requestedDamage = damageUnit * damageWeight * UnityEngine.Random.Range(0.86f, 1.16f);
            if (attackersAreHeroes && crit)
            {
                requestedDamage *= CritDamageMultiplier;
            }
            else if (!attackersAreHeroes)
            {
                requestedDamage *= GetFightVisualHeroIncomingDamageWeight(state.targetIndex, enemyDamage > 0 ? enemyDamage : GetCampaignEnemyDamage(stageNumber));
            }

            var actualDamage = ApplyTargetDamageTowardAverage(
                targetHealthPercents,
                state.targetIndex,
                activeTargetCount,
                targetEndAverage,
                requestedDamage);

            if (actualDamage <= 0.001f)
            {
                continue;
            }

            if (attackersAreHeroes)
            {
                GainFightHeroMana(i, GetHeroAutoAttackManaGain(i));
                TryApplyHeroPassiveOnHit(i, attackerHealthPercents, attackerCount, targets[state.targetIndex].position, ref floatingIndex);
            }

            var damageNumber = attackersAreHeroes
                ? Mathf.Max(1, Mathf.RoundToInt(GetHeroEffectiveAttack(i) * (crit ? CritDamageMultiplier : 1f) * UnityEngine.Random.Range(0.8f, 1.22f)))
                : Mathf.Max(1, Mathf.RoundToInt(GetMitigatedEnemyDamageAgainstHero(enemyDamage > 0 ? enemyDamage : GetCampaignEnemyDamage(stageNumber), state.targetIndex) * GetFightVisualDamageWeight(false, i) * UnityEngine.Random.Range(0.88f, 1.12f)));
            var color = crit ? new Color(1f, 0.46f, 0.16f) : attackersAreHeroes ? new Color(1f, 0.84f, 0.28f) : new Color(1f, 0.34f, 0.3f);
            var prefix = crit ? "CRIT " : string.Empty;
            ShowFightFloatingText(floatingIndex++, $"{prefix}-{FormatCompactNumber(damageNumber)}", targetPosition + new Vector2(UnityEngine.Random.Range(-36, 36), UnityEngine.Random.Range(-88, -32)), color);
        }
    }

    private bool IsFightVisualRanged(bool isHero, int index)
    {
        return isHero ? IsHeroRangedCombatant(index) : IsEnemyRangedCombatant(GetFightEnemyTextureName(index));
    }

    private bool IsFightVisualInRange(FightVisualUnitState attacker, Vector2 target, bool isHero, int index)
    {
        if (!IsFightVisualRanged(isHero, index))
        {
            return attacker.hasLockedMeleePosition
                && Vector2.Distance(attacker.position, attacker.lockedMeleePosition) <= 8f
                && Vector2.Distance(attacker.position, target) <= GetFightVisualMeleeRange(IsFightVisualRanged(!isHero, attacker.targetIndex));
        }

        return Vector2.Distance(attacker.position, target) <= 760f;
    }

    private static Vector2 GetMeleeContactPosition(Vector2 attackerPosition, Vector2 targetPosition, bool attackerIsHero, int attackerIndex, int targetIndex, bool targetIsRanged)
    {
        var laneSign = attackerIsHero ? 1f : -1f;
        var laneOffset = ((attackerIndex + targetIndex) % 3 - 1) * 18f * laneSign;
        var closeSideOffset = attackerIsHero ? -82f : 82f;
        return ClampFightArenaPosition(targetPosition + new Vector2(closeSideOffset, laneOffset));
    }

    private static float GetFightVisualMeleeRange(bool targetIsRanged)
    {
        return targetIsRanged ? 118f : 132f;
    }

    private static Vector2 ClampFightArenaPosition(Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp(position.x, -365f, 365f),
            Mathf.Clamp(position.y, -815f, -330f));
    }

    private static float GetHeroVisualMoveSpeed(int heroIndex)
    {
        var heroId = GetHeroTextureName(heroIndex);
        if (heroId == "hero_dante")
        {
            return 335f;
        }

        if (heroId == "hero_borin")
        {
            return 230f;
        }

        if (heroId == "hero_ravik")
        {
            return 300f;
        }

        return 285f;
    }

    private static float GetEnemyVisualMoveSpeed(string enemyTextureName)
    {
        if (enemyTextureName == "enemy_canine")
        {
            return 330f;
        }

        if (enemyTextureName == "enemy_golem")
        {
            return 210f;
        }

        return 260f;
    }

    private float GetFightVisualAttackInterval(bool isHero, int index)
    {
        if (isHero)
        {
            var heroId = GetHeroTextureName(index);
            if (heroId == "hero_dante")
            {
                return 0.95f;
            }

            if (heroId == "hero_borin")
            {
                return 1.6f;
            }

            if (heroId == "hero_cyra")
            {
                return 1.25f;
            }

            if (heroId == "hero_elowen")
            {
                return 1.7f;
            }

            if (heroId == "hero_ravik")
            {
                return 1.16f;
            }

            return 1.12f;
        }

        var enemyTextureName = GetFightEnemyTextureName(index);
        if (enemyTextureName == "enemy_canine")
        {
            return 1.25f;
        }

        if (enemyTextureName == "enemy_golem")
        {
            return 2.05f;
        }

        if (enemyTextureName == "enemy_dragon")
        {
            return 1.85f;
        }

        if (enemyTextureName == "enemy_bat")
        {
            return 1.35f;
        }

        if (enemyTextureName == "enemy_slime")
        {
            return 1.9f;
        }

        return 1.55f;
    }

    private float GetFightVisualDamageWeight(bool isHero, int index)
    {
        if (isHero)
        {
            return Mathf.Clamp(GetHeroEffectiveAttack(index) / Mathf.Max(1f, GetTeamDamage() / (float)HeroCount), 0.65f, 1.65f);
        }

        var enemyTextureName = GetFightEnemyTextureName(index);
        if (enemyTextureName == "enemy_golem")
        {
            return 1.35f;
        }

        if (enemyTextureName == "enemy_dragon")
        {
            return 1.45f;
        }

        if (enemyTextureName == "enemy_canine")
        {
            return 1.15f;
        }

        return 1f;
    }

    private float GetFightVisualHeroIncomingDamageWeight(int heroIndex, int enemyDamage)
    {
        heroIndex = Mathf.Clamp(heroIndex, 0, HeroCount - 1);
        var averageHeroHealth = GetTeamHealth() / (float)Mathf.Max(1, HeroCount);
        var heroHealth = Mathf.Max(1f, GetHeroCombatMaxHealth(heroIndex));
        var healthWeight = averageHeroHealth / heroHealth;
        var averageMitigatedDamage = Mathf.Max(1f, GetMitigatedEnemyDamage(enemyDamage));
        var heroMitigatedDamage = Mathf.Max(1f, GetMitigatedEnemyDamageAgainstHero(enemyDamage, heroIndex));
        var defenseWeight = heroMitigatedDamage / averageMitigatedDamage;
        return Mathf.Clamp(healthWeight * defenseWeight, 0.4f, 1.85f);
    }

    private static int GetHeroMaxMana(int heroIndex)
    {
        var heroId = GetHeroTextureName(heroIndex);
        if (heroId == "hero_dante")
        {
            return 25;
        }

        if (heroId == "hero_astra")
        {
            return 26;
        }

        if (heroId == "hero_cyra")
        {
            return 27;
        }

        if (heroId == "hero_elowen")
        {
            return 28;
        }

        if (heroId == "hero_borin")
        {
            return 30;
        }

        if (heroId == "hero_ravik")
        {
            return 27;
        }

        return 28;
    }

    private static int GetHeroAutoAttackManaGain(int heroIndex)
    {
        return FightAutoAttackManaGain;
    }

    private static float GetHeroUltimateDamageMultiplier(int heroIndex)
    {
        var heroId = GetHeroTextureName(heroIndex);
        if (heroId == "hero_cyra")
        {
            return 7.5f;
        }

        if (heroId == "hero_dante")
        {
            return 6.25f;
        }

        if (heroId == "hero_borin")
        {
            return 3.5f;
        }

        if (heroId == "hero_elowen")
        {
            return 3.4f;
        }

        if (heroId == "hero_ravik")
        {
            return 7.9f;
        }

        return 4.8f;
    }

    private void RefreshFightArenaBackground(bool useDungeonBattleMap)
    {
        if (fightArenaBackgroundImage == null)
        {
            return;
        }

        if (!useDungeonBattleMap)
        {
            fightArenaBackgroundImage.texture = null;
            fightArenaBackgroundImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            fightArenaBackgroundImage.gameObject.SetActive(false);
            return;
        }

        var texture = LoadRuntimeTexture(GetSelectedDungeonBattleMapTextureName());
        fightArenaBackgroundImage.texture = texture;
        fightArenaBackgroundImage.gameObject.SetActive(texture != null);
        if (texture == null)
        {
            return;
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        var rect = fightArenaBackgroundImage.rectTransform;
        const float targetWidth = 960f;
        const float targetHeight = 1190f;
        rect.sizeDelta = new Vector2(targetWidth, targetHeight);
        rect.anchoredPosition = Vector2.zero;

        var textureAspect = texture.width / (float)Mathf.Max(1, texture.height);
        var targetAspect = targetWidth / targetHeight;
        var cropHeight = Mathf.Clamp01(textureAspect / targetAspect);
        var cropY = Mathf.Clamp(0.16f, 0f, 1f - cropHeight);
        fightArenaBackgroundImage.uvRect = new Rect(0f, cropY, 1f, cropHeight);
    }

    private void RefreshFormationArenaBackground(bool useDungeonBattleMap)
    {
        if (formationArenaBackgroundImage == null)
        {
            return;
        }

        if (!useDungeonBattleMap)
        {
            formationArenaBackgroundImage.texture = null;
            formationArenaBackgroundImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            formationArenaBackgroundImage.gameObject.SetActive(false);
            return;
        }

        var texture = LoadRuntimeTexture(GetSelectedDungeonBattleMapTextureName());
        formationArenaBackgroundImage.texture = texture;
        formationArenaBackgroundImage.gameObject.SetActive(texture != null);
        if (texture == null)
        {
            return;
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        var rect = formationArenaBackgroundImage.rectTransform;
        rect.sizeDelta = new Vector2(820f, 410f);
        rect.anchoredPosition = Vector2.zero;

        var textureAspect = texture.width / (float)Mathf.Max(1, texture.height);
        const float previewAspect = 820f / 410f;
        var cropHeight = Mathf.Clamp01(textureAspect / previewAspect);
        var cropY = Mathf.Clamp(0.36f, 0f, 1f - cropHeight);
        formationArenaBackgroundImage.uvRect = new Rect(0f, cropY, 1f, cropHeight);
    }

    private void SelectRandomDungeonBattleMap(string dungeonId)
    {
        var maps = GetDungeonBattleMapTextureNames(dungeonId);
        selectedDungeonBattleMapTextureName = maps[UnityEngine.Random.Range(0, maps.Length)];
    }

    private void EnsureSelectedDungeonBattleMap()
    {
        if (!IsDungeonBattleMapTextureNameForDungeon(selectedDungeonId, selectedDungeonBattleMapTextureName))
        {
            SelectRandomDungeonBattleMap(selectedDungeonId);
        }
    }

    private string GetSelectedDungeonBattleMapTextureName()
    {
        EnsureSelectedDungeonBattleMap();
        return selectedDungeonBattleMapTextureName;
    }

    private static bool IsDungeonBattleMapTextureNameForDungeon(string dungeonId, string textureName)
    {
        if (string.IsNullOrWhiteSpace(textureName))
        {
            return false;
        }

        var maps = GetDungeonBattleMapTextureNames(dungeonId);
        for (var i = 0; i < maps.Length; i++)
        {
            if (maps[i] == textureName)
            {
                return true;
            }
        }

        return false;
    }

    private static string[] GetDungeonBattleMapTextureNames(string dungeonId)
    {
        if (dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return EssenceDungeonBattleMapTextureNames;
        }

        if (dungeonId == GearDungeonDefinition.dungeonId)
        {
            return GearDungeonBattleMapTextureNames;
        }

        return GoldDungeonBattleMapTextureNames;
    }

    private float GetExpectedHeroVisualAttackWeight(float visualDuration)
    {
        var total = 0f;
        for (var i = 0; i < HeroCount; i++)
        {
            total += GetFightVisualDamageWeight(true, i) * Mathf.Max(1f, visualDuration / GetFightVisualAttackInterval(true, i));
        }

        return Mathf.Max(1f, total);
    }

    private float GetExpectedEnemyVisualAttackWeight(float visualDuration, int activeEnemyCount)
    {
        var total = 0f;
        activeEnemyCount = Mathf.Clamp(activeEnemyCount, 0, HeroCount);
        for (var i = 0; i < activeEnemyCount; i++)
        {
            total += GetFightVisualDamageWeight(false, i) * Mathf.Max(1f, visualDuration / GetFightVisualAttackInterval(false, i));
        }

        return Mathf.Max(1f, total);
    }

    private void PrepareFightAnimationTextures(int stageNumber, bool singleBoss = false, string bossTextureName = null)
    {
        fightHeroIdleFrames = new Texture2D[HeroCount][];
        fightHeroRunFrames = new Texture2D[HeroCount][];
        fightHeroAttackFrames = new Texture2D[HeroCount][];
        fightHeroUltimateFrames = new Texture2D[HeroCount][];
        fightHeroAttackFxFrames = new Texture2D[HeroCount][];
        fightHeroUltimateFxFrames = new Texture2D[HeroCount][];
        fightEnemyIdleFrames = new Texture2D[HeroCount][];
        fightEnemyRunFrames = new Texture2D[HeroCount][];
        fightEnemyAttackFrames = new Texture2D[HeroCount][];
        fightEnemyTextureNames = new string[HeroCount];

        for (var i = 0; i < HeroCount; i++)
        {
            var heroTextureName = GetHeroTextureName(i);
            fightHeroIdleFrames[i] = LoadCombatAnimationFrames(heroTextureName, "idle", heroTextureName);
            fightHeroRunFrames[i] = LoadCombatAnimationFrames(heroTextureName, "run", heroTextureName);
            fightHeroAttackFrames[i] = LoadCombatAnimationFrames(heroTextureName, "attack", heroTextureName);
            fightHeroUltimateFrames[i] = LoadCombatAnimationFrames(heroTextureName, "ultimate", heroTextureName);
            fightHeroAttackFxFrames[i] = LoadCombatAnimationFrames(heroTextureName, "fx_attack", null);
            fightHeroUltimateFxFrames[i] = LoadCombatAnimationFrames(heroTextureName, "fx_ultimate", null);
            SetRawImageTexture(fightHeroImages, i, GetFirstTexture(fightHeroIdleFrames[i]));
            if (fightHeroImages != null && i < fightHeroImages.Length && fightHeroImages[i] != null)
            {
                var useRavikRig = IsRavikHero(i) && HasRavikSkeletalView(fightHeroSkeletalViews, i);
                fightHeroImages[i].gameObject.SetActive(!useRavikRig);
                fightHeroImages[i].rectTransform.localScale = new Vector3(GetHeroFacingScale(i), 1f, 1f);
            }

            if (fightHeroSkeletalViews != null && i < fightHeroSkeletalViews.Length && fightHeroSkeletalViews[i] != null)
            {
                if (IsRavikHero(i))
                {
                    fightHeroSkeletalViews[i].ShowPreview(GetFightHeroPositions()[Mathf.Min(i, GetFightHeroPositions().Length - 1)], GetHeroFacingScale(i), 1f);
                }
                else
                {
                    fightHeroSkeletalViews[i].Hide();
                }
            }

            var enemyTextureName = singleBoss
                ? (string.IsNullOrWhiteSpace(bossTextureName) ? GetDungeonBossTextureName(selectedDungeonId) : bossTextureName)
                : GetCampaignEnemyTextureName(stageNumber, i);
            fightEnemyTextureNames[i] = enemyTextureName;
            fightEnemyIdleFrames[i] = LoadCombatAnimationFrames(enemyTextureName, "idle", "enemy_campaign");
            fightEnemyRunFrames[i] = LoadCombatAnimationFrames(enemyTextureName, "run", "enemy_campaign");
            fightEnemyAttackFrames[i] = LoadCombatAnimationFrames(enemyTextureName, "attack", "enemy_campaign");
            SetRawImageTexture(fightEnemyImages, i, GetFirstTexture(fightEnemyIdleFrames[i]));
            if (fightEnemyImages != null && i < fightEnemyImages.Length && fightEnemyImages[i] != null)
            {
                fightEnemyImages[i].rectTransform.localScale = new Vector3(GetEnemyFacingScale(enemyTextureName), 1f, 1f);
            }
        }
    }

    private void ConfigureFightEnemyPresentation(bool singleBoss)
    {
        var positions = singleBoss ? GetFightBossEnemyPositions() : GetFightEnemyPositions();
        if (fightBossHpFill != null && fightBossHpFill.transform.parent != null)
        {
            fightBossHpFill.transform.parent.gameObject.SetActive(singleBoss);
        }

        for (var i = 0; i < HeroCount; i++)
        {
            var visible = !singleBoss || i == 0;
            var showSmallHp = visible && !singleBoss;
            if (fightEnemyImages != null && i < fightEnemyImages.Length && fightEnemyImages[i] != null)
            {
                fightEnemyImages[i].gameObject.SetActive(visible);
                fightEnemyImages[i].rectTransform.sizeDelta = singleBoss ? new Vector2(230, 230) : new Vector2(126, 126);
                var enemyTextureName = fightEnemyTextureNames != null && i < fightEnemyTextureNames.Length ? fightEnemyTextureNames[i] : GetCampaignEnemyTextureName(1, i);
                fightEnemyImages[i].rectTransform.localScale = new Vector3(GetEnemyFacingScale(enemyTextureName), 1f, 1f);
            }

            if (fightEnemyRects != null && i < fightEnemyRects.Length && fightEnemyRects[i] != null && i < positions.Length)
            {
                fightEnemyRects[i].anchoredPosition = positions[i];
            }

            if (fightEnemyHpFills != null && i < fightEnemyHpFills.Length)
            {
                SetHealthFillVisible(fightEnemyHpFills[i], showSmallHp);
                if (fightEnemyHpFills[i] != null && i < positions.Length)
                {
                    var hpRect = fightEnemyHpFills[i].transform.parent.GetComponent<RectTransform>();
                    hpRect.sizeDelta = new Vector2(singleBoss ? 220f : 112f, hpRect.sizeDelta.y);
                    hpRect.anchoredPosition = positions[i] + new Vector2(0, singleBoss ? -218 : -122);
                }
            }

            if (fightEnemyHpPercentTexts != null && i < fightEnemyHpPercentTexts.Length && fightEnemyHpPercentTexts[i] != null)
            {
                fightEnemyHpPercentTexts[i].gameObject.SetActive(showSmallHp);
            }
        }
    }

    private static void SetHealthFillVisible(Image fill, bool isVisible)
    {
        if (fill == null || fill.transform.parent == null)
        {
            return;
        }

        fill.transform.parent.gameObject.SetActive(isVisible);
    }

    private static Image GetHealthFill(Image[] fills, int index)
    {
        return fills != null && index >= 0 && index < fills.Length ? fills[index] : null;
    }

    private static bool HasFrames(Texture2D[] frames)
    {
        return frames != null && frames.Length > 0 && frames[0] != null;
    }

    private static Texture2D GetFirstTexture(Texture2D[] frames)
    {
        return HasFrames(frames) ? frames[0] : null;
    }

    private static void SetRawImageTexture(RawImage[] images, int index, Texture2D texture)
    {
        if (images == null || index < 0 || index >= images.Length || images[index] == null || texture == null)
        {
            return;
        }

        images[index].texture = texture;
    }

    private void AnimateFightUnitsWithState(
        FightVisualUnitState[] heroStates,
        FightVisualUnitState[] enemyStates,
        float[] heroHpPercents,
        float[] enemyHpPercents,
        int activeEnemyCount,
        float animationTimer,
        float slowedWorldAnimationTimer,
        bool singleBoss,
        int ultimateCinematicHeroIndex,
        float ultimateCinematicRemaining)
    {
        SetProjectilesVisible(fightHeroProjectileImages, false);
        SetProjectilesVisible(fightEnemyProjectileImages, false);
        SetRawImagesVisible(fightHeroFxImages, false);
        HideRavikSkeletalViews(fightHeroSkeletalViews);
        var ultimateCinematicActive = ultimateCinematicHeroIndex >= 0 && ultimateCinematicRemaining > 0f;
        var ultimateProgress = ultimateCinematicActive ? Mathf.Clamp01(1f - (ultimateCinematicRemaining / FightUltimateCinematicSeconds)) : 0f;
        var ultimatePulse = ultimateCinematicActive ? Mathf.Sin(ultimateProgress * Mathf.PI) : 0f;

        for (var i = 0; i < HeroCount; i++)
        {
            var state = heroStates != null && i < heroStates.Length ? heroStates[i] : default;
            var unitAnimationTimer = ultimateCinematicActive && i != ultimateCinematicHeroIndex ? slowedWorldAnimationTimer : animationTimer;
            var isRavik = IsRavikHero(i);
            var usesRuntimeRig = isRavik;
            var position = state.position + new Vector2(0f, usesRuntimeRig ? 0f : Mathf.Sin(unitAnimationTimer * 5.4f + i) * 4.5f);
            var alive = heroHpPercents != null && i < heroHpPercents.Length && heroHpPercents[i] > 0.001f;
            var frames = GetFightFrameSet(fightHeroIdleFrames, i);
            var frameSpeed = 6.8f;
            var actionAge = unitAnimationTimer - state.attackStartedAt;
            var tint = Color.white;
            var scaleMultiplier = 1f;
            var ravikClip = RavikSkeletalCombatView.Clip.Idle;
            var ravikHasTarget = false;
            var ravikTargetPosition = Vector2.zero;

            if (ultimateCinematicActive && alive)
            {
                if (i == ultimateCinematicHeroIndex)
                {
                    tint = Color.Lerp(Color.white, new Color(1f, 0.88f, 0.28f, 1f), 0.35f + ultimatePulse * 0.45f);
                    if (!usesRuntimeRig)
                    {
                        scaleMultiplier = 1f + ultimatePulse * 0.18f;
                        position += new Vector2(0f, -18f * ultimatePulse);
                    }
                }
                else
                {
                    tint = new Color(0.62f, 0.68f, 0.82f, 0.62f);
                }
            }

            if (alive && state.isMoving)
            {
                frames = GetFightFrameSet(fightHeroRunFrames, i, fightHeroIdleFrames);
                frameSpeed = 10.5f;
                ravikClip = RavikSkeletalCombatView.Clip.Run;
            }

            if (alive && IsFightActionActive(actionAge))
            {
                var phase = Mathf.Clamp01(actionAge / 0.72f);
                if (IsHeroRangedCombatant(i))
                {
                    frames = GetFightFrameSet(fightHeroAttackFrames, i, fightHeroIdleFrames);
                    frameSpeed = 10.5f;
                    ravikClip = RavikSkeletalCombatView.Clip.Attack;
                    if (!usesRuntimeRig)
                    {
                        position += new Vector2(Mathf.Sin(phase * Mathf.PI * 2f) * 5f, Mathf.Sin(phase * Mathf.PI) * 7f);
                    }
                    if (enemyStates != null && state.targetIndex >= 0 && state.targetIndex < enemyStates.Length)
                    {
                        ravikHasTarget = true;
                        ravikTargetPosition = enemyStates[state.targetIndex].position + new Vector2(-54f, -70f);
                        if (!usesRuntimeRig)
                        {
                            AnimateProjectile(GetProjectileImage(fightHeroProjectileImages, i), GetProjectileRect(fightHeroProjectileRects, i), state.position + new Vector2(58f, -64f), ravikTargetPosition, phase);
                        }
                    }

                    if (isRavik && !HasRavikSkeletalView(fightHeroSkeletalViews, i))
                    {
                        AnimateHeroCastFx(i, fightHeroAttackFxFrames, position + new Vector2(82f, -74f), new Vector2(190f, 95f), actionAge, 8.5f, 0.95f);
                    }
                }
                else
                {
                    frames = GetFightFrameSet(fightHeroAttackFrames, i, fightHeroIdleFrames);
                    frameSpeed = 12f;
                    ravikClip = RavikSkeletalCombatView.Clip.Attack;
                }
            }

            if (ultimateCinematicActive && alive && i == ultimateCinematicHeroIndex)
            {
                frames = GetFightFrameSet(fightHeroUltimateFrames, i, fightHeroAttackFrames);
                frameSpeed = 9.5f;
                ravikClip = RavikSkeletalCombatView.Clip.Ultimate;
                if (isRavik)
                {
                    var fxPosition = position + new Vector2(142f, -88f);
                    if (enemyStates != null && state.targetIndex >= 0 && state.targetIndex < enemyStates.Length)
                    {
                        fxPosition = Vector2.Lerp(state.position, enemyStates[state.targetIndex].position, 0.55f) + new Vector2(0f, -92f);
                    }

                    ravikHasTarget = true;
                    ravikTargetPosition = fxPosition;
                    if (!HasRavikSkeletalView(fightHeroSkeletalViews, i))
                    {
                        var ultimateElapsed = FightUltimateCinematicSeconds - ultimateCinematicRemaining;
                        AnimateHeroCastFx(i, fightHeroUltimateFxFrames, fxPosition, new Vector2(380f, 190f), ultimateElapsed, 6.6f, 1f);
                    }
                }
            }

            if (!alive)
            {
                ravikClip = RavikSkeletalCombatView.Clip.Death;
            }

            if (TryApplyRavikSkeletalFightPose(i, ravikClip, position, unitAnimationTimer, actionAge, GetHeroFacingScale(i), scaleMultiplier, tint, alive || isRavik, ravikHasTarget, ravikTargetPosition, ultimateProgress))
            {
                SetHealthFillVisible(GetHealthFill(fightHeroHpFills, i), false);
                continue;
            }

            ApplyFightUnitAnimationFrame(fightHeroImages, i, frames, frameSpeed, i * 1.3f, unitAnimationTimer);
            SetFightUnitColor(fightHeroImages, i, alive, tint);
            SetFightUnitScale(fightHeroImages, i, GetHeroFacingScale(i), scaleMultiplier);
            SetHealthFillVisible(GetHealthFill(fightHeroHpFills, i), false);
            SetFightUnitPosition(fightHeroRects, null, i, position, -128f);
        }

        for (var i = 0; i < HeroCount; i++)
        {
            if (i >= activeEnemyCount)
            {
                continue;
            }

            var state = enemyStates != null && i < enemyStates.Length ? enemyStates[i] : default;
            var unitAnimationTimer = ultimateCinematicActive ? slowedWorldAnimationTimer : animationTimer;
            var position = state.position + new Vector2(0f, Mathf.Sin(unitAnimationTimer * 5.1f + i * 1.4f) * 4f);
            var alive = enemyHpPercents != null && i < enemyHpPercents.Length && enemyHpPercents[i] > 0.001f;
            var frames = GetFightFrameSet(fightEnemyIdleFrames, i);
            var frameSpeed = 6.2f;
            var enemyTextureName = GetFightEnemyTextureName(i);
            var actionAge = unitAnimationTimer - state.attackStartedAt;
            var tint = ultimateCinematicActive && alive ? new Color(0.58f, 0.62f, 0.72f, 0.58f) : Color.white;

            if (alive && state.isMoving)
            {
                frames = GetFightFrameSet(fightEnemyRunFrames, i, fightEnemyIdleFrames);
                frameSpeed = 10f;
            }

            if (alive && IsFightActionActive(actionAge))
            {
                var phase = Mathf.Clamp01(actionAge / 0.72f);
                if (IsEnemyRangedCombatant(enemyTextureName))
                {
                    frames = GetFightFrameSet(fightEnemyAttackFrames, i, fightEnemyIdleFrames);
                    frameSpeed = 10.5f;
                    position += new Vector2(Mathf.Sin(phase * Mathf.PI * 2f) * -5f, Mathf.Sin(phase * Mathf.PI) * 7f);
                    if (heroStates != null && state.targetIndex >= 0 && state.targetIndex < heroStates.Length)
                    {
                        AnimateProjectile(GetProjectileImage(fightEnemyProjectileImages, i), GetProjectileRect(fightEnemyProjectileRects, i), state.position + new Vector2(-58f, -64f), heroStates[state.targetIndex].position + new Vector2(54f, -70f), phase);
                    }
                }
                else
                {
                    frames = GetFightFrameSet(fightEnemyAttackFrames, i, fightEnemyIdleFrames);
                    frameSpeed = 11.5f;
                }
            }

            ApplyFightUnitAnimationFrame(fightEnemyImages, i, frames, frameSpeed, i * 1.7f, unitAnimationTimer);
            SetFightUnitColor(fightEnemyImages, i, alive, tint);
            SetFightUnitScale(fightEnemyImages, i, GetEnemyFacingScale(enemyTextureName), 1f);
            SetHealthFillVisible(GetHealthFill(fightEnemyHpFills, i), alive && !singleBoss);
            SetFightUnitPosition(fightEnemyRects, fightEnemyHpFills, i, position, singleBoss ? -218f : -122f);
        }
    }

    private static bool IsFightActionActive(float actionAge)
    {
        return actionAge >= 0f && actionAge <= 0.72f;
    }

    private string GetFightEnemyTextureName(int index)
    {
        if (fightEnemyTextureNames != null && index >= 0 && index < fightEnemyTextureNames.Length && !string.IsNullOrWhiteSpace(fightEnemyTextureNames[index]))
        {
            return fightEnemyTextureNames[index];
        }

        return "enemy_rat";
    }

    private static Texture2D[] GetFightFrameSet(Texture2D[][] preferredFrames, int index, Texture2D[][] fallbackFrames = null)
    {
        if (preferredFrames != null && index >= 0 && index < preferredFrames.Length && HasFrames(preferredFrames[index]))
        {
            return preferredFrames[index];
        }

        if (fallbackFrames != null && index >= 0 && index < fallbackFrames.Length && HasFrames(fallbackFrames[index]))
        {
            return fallbackFrames[index];
        }

        return Array.Empty<Texture2D>();
    }

    private static void ApplyFightUnitAnimationFrame(RawImage[] images, int index, Texture2D[] frames, float speed, float phaseOffset, float animationTimer)
    {
        if (images == null || index < 0 || index >= images.Length || images[index] == null || !HasFrames(frames))
        {
            return;
        }

        var frameIndex = Mathf.Abs(Mathf.FloorToInt(animationTimer * Mathf.Max(1f, speed) + phaseOffset)) % frames.Length;
        images[index].texture = frames[frameIndex];
    }

    private static void SetFightUnitColor(RawImage[] images, int index, bool alive, Color aliveColor)
    {
        if (images == null || index < 0 || index >= images.Length || images[index] == null)
        {
            return;
        }

        images[index].gameObject.SetActive(alive);
        images[index].color = alive ? aliveColor : new Color(0.6f, 0.6f, 0.6f, 0f);
    }

    private static void SetFightUnitScale(RawImage[] images, int index, float facingScale, float scaleMultiplier)
    {
        if (images == null || index < 0 || index >= images.Length || images[index] == null)
        {
            return;
        }

        images[index].rectTransform.localScale = new Vector3(facingScale * Mathf.Max(0.1f, scaleMultiplier), Mathf.Max(0.1f, scaleMultiplier), 1f);
    }

    private static void SetFightUnitPosition(RectTransform[] rects, Image[] hpFills, int index, Vector2 position, float healthBarOffsetY)
    {
        if (rects != null && index >= 0 && index < rects.Length && rects[index] != null)
        {
            rects[index].anchoredPosition = position;
        }

        if (hpFills == null || index < 0 || index >= hpFills.Length || hpFills[index] == null || hpFills[index].transform.parent == null)
        {
            return;
        }

        var hpRect = hpFills[index].transform.parent.GetComponent<RectTransform>();
        if (hpRect != null)
        {
            hpRect.anchoredPosition = position + new Vector2(0f, healthBarOffsetY);
        }
    }

    private static void SetProjectileVisible(Image image, bool isVisible)
    {
        if (image != null)
        {
            image.gameObject.SetActive(isVisible);
        }
    }

    private static void SetProjectilesVisible(Image[] images, bool isVisible)
    {
        if (images == null)
        {
            return;
        }

        for (var i = 0; i < images.Length; i++)
        {
            SetProjectileVisible(images[i], isVisible);
        }
    }

    private static void SetRawImagesVisible(RawImage[] images, bool isVisible)
    {
        if (images == null)
        {
            return;
        }

        for (var i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
            {
                images[i].gameObject.SetActive(isVisible);
            }
        }
    }

    private static void HideRavikSkeletalViews(RavikSkeletalCombatView[] views)
    {
        if (views == null)
        {
            return;
        }

        for (var i = 0; i < views.Length; i++)
        {
            if (views[i] != null)
            {
                views[i].HideTransientEffects();
            }
        }
    }

    private bool TryApplyRavikSkeletalFightPose(
        int heroIndex,
        RavikSkeletalCombatView.Clip clip,
        Vector2 position,
        float animationTimer,
        float actionAge,
        float facingScale,
        float scaleMultiplier,
        Color tint,
        bool visible,
        bool hasTarget,
        Vector2 targetPosition,
        float ultimateProgress)
    {
        if (!IsRavikHero(heroIndex) || !HasRavikSkeletalView(fightHeroSkeletalViews, heroIndex))
        {
            return false;
        }

        if (fightHeroImages != null && heroIndex >= 0 && heroIndex < fightHeroImages.Length && fightHeroImages[heroIndex] != null)
        {
            fightHeroImages[heroIndex].gameObject.SetActive(false);
        }

        fightHeroSkeletalViews[heroIndex].ApplyCombatPose(
            clip,
            position,
            animationTimer,
            actionAge,
            facingScale,
            scaleMultiplier,
            tint,
            visible,
            hasTarget,
            targetPosition,
            ultimateProgress);
        return true;
    }

    private static Image GetProjectileImage(Image[] images, int index)
    {
        return images != null && index >= 0 && index < images.Length ? images[index] : null;
    }

    private static RectTransform GetProjectileRect(RectTransform[] rects, int index)
    {
        return rects != null && index >= 0 && index < rects.Length ? rects[index] : null;
    }

    private static void AnimateProjectile(Image image, RectTransform rect, Vector2 from, Vector2 to, float phase)
    {
        if (image == null || rect == null)
        {
            return;
        }

        if (phase < 0.16f || phase > 0.88f)
        {
            return;
        }

        var t = Mathf.InverseLerp(0.16f, 0.88f, phase);
        var smooth = Mathf.SmoothStep(0f, 1f, t);
        var delta = to - from;
        rect.anchoredPosition = Vector2.Lerp(from, to, smooth);
        rect.sizeDelta = new Vector2(44f + Mathf.Sin(t * Mathf.PI) * 20f, 9f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var color = image.color;
        color.a = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 0.95f);
        image.color = color;
        image.gameObject.SetActive(true);
    }

    private void AnimateHeroCastFx(int index, Texture2D[][] frameSets, Vector2 position, Vector2 size, float animationTimer, float speed, float alpha)
    {
        if (fightHeroFxImages == null || fightHeroFxRects == null || index < 0 || index >= fightHeroFxImages.Length || index >= fightHeroFxRects.Length)
        {
            return;
        }

        var image = fightHeroFxImages[index];
        var rect = fightHeroFxRects[index];
        var frames = GetFightFrameSet(frameSets, index);
        if (image == null || rect == null || !HasFrames(frames))
        {
            return;
        }

        var frameIndex = Mathf.Abs(Mathf.FloorToInt(animationTimer * Mathf.Max(1f, speed))) % frames.Length;
        image.texture = frames[frameIndex];
        image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        image.gameObject.SetActive(true);
    }

    private void ShowFightFloatingText(int index, string text, Vector2 position, Color color)
    {
        if (fightFloatingTexts == null || fightFloatingTexts.Length == 0)
        {
            return;
        }

        var label = fightFloatingTexts[Mathf.Abs(index) % fightFloatingTexts.Length];
        if (label == null)
        {
            return;
        }

        label.gameObject.SetActive(true);
        label.text = text;
        label.color = color;
        label.rectTransform.anchoredPosition = position;
        label.transform.SetAsLastSibling();
    }

    private void HideFightFloatingTexts()
    {
        if (fightFloatingTexts == null)
        {
            return;
        }

        for (var i = 0; i < fightFloatingTexts.Length; i++)
        {
            if (fightFloatingTexts[i] != null)
            {
                fightFloatingTexts[i].gameObject.SetActive(false);
            }
        }
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
        EnsureHeroEquipment();
        EnsureAccessories();
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
            damageText.text = $"ATK {damage}   HP {GetTeamHealth()}   Crit {GetTeamCritChancePercent()}%   Acc {GetTeamAccuracyPercent()}%   DEF {GetTeamDefense()}";
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
        RefreshInventoryPopupUi();
        RefreshSummonUi();
        RefreshDailyMissionUi();
        RefreshBattlePassUi();
        RefreshBackendUi();
        RefreshRuntimeArtUi();
        RefreshTopBarUi();
        RefreshHomeGeneratedUi();
        RefreshCampaignMapUi();
        RefreshFormationUi();

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
            summonButton.interactable = gems >= GetSummonPackCost(1);
        }

        if (summonTenButton != null)
        {
            summonTenButton.interactable = gems >= GetSummonPackCost(10);
        }

        if (summonResultTenButton != null)
        {
            summonResultTenButton.interactable = !summonAutoRunning && gems >= GetSummonPackCost(SummonAutoStepCount);
        }

        if (summonResultMaxButton != null)
        {
            summonResultMaxButton.interactable = !summonAutoRunning && gems >= GetSummonPackCost(MaxSummonPullCount);
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
        var busy = backendRequestInProgress || backendLifecycleFlushInProgress;
        var canInteract = !busy && !campaignFightInProgress;
        var canManageHeroes = !busy && !IsDungeonBattleFocusLocked();
        var canChangeTeam = !busy && (!campaignFightInProgress || battleTargetMode == BattleTargetMode.Campaign);
        var canConfirmFormation = battleTargetMode == BattleTargetMode.Dungeon || selectedCampaignStage == enemyLevel;

        SetButtonInteractable(fightButton, canInteract);
        SetButtonInteractable(homeBeginButton, canInteract && selectedCampaignStage == enemyLevel);
        SetButtonInteractable(formationConfirmButton, canInteract && canConfirmFormation);
        SetButtonInteractable(formationBackButton, canInteract);
        SetButtonInteractable(formationAutoContinueButton, canInteract);
        SetButtonInteractable(fightEndButton, campaignFightInProgress || battleFlowMode == BattleFlowMode.Fight);
        SetButtonsInteractable(formationSlotButtons, canInteract);
        SetButtonsInteractable(campaignStageButtons, canInteract);
        SetButtonInteractable(goldDungeonButton, canInteract);
        SetButtonInteractable(essenceDungeonButton, canInteract);
        SetButtonInteractable(gearDungeonButton, canInteract);
        GateButton(upgradeButton, canInteract);
        GateButton(heroUpgradeButton, canManageHeroes);
        GateButton(heroAscendButton, canManageHeroes);
        SetButtonInteractable(heroDetailCloseButton, true);
        SetButtonInteractable(heroDetailPreviousButton, canManageHeroes);
        SetButtonInteractable(heroDetailNextButton, canManageHeroes);
        SetButtonsInteractable(heroDetailGearSlotButtons, canManageHeroes);
        SetButtonInteractable(heroDetailGearListCloseButton, true);
        SetButtonInteractable(heroRosterTabButton, canManageHeroes);
        SetButtonInteractable(heroSetTeamTabButton, canManageHeroes);
        SetButtonInteractable(heroSortToggleButton, canManageHeroes);
        SetButtonInteractable(heroAttackTypeFilterButton, canManageHeroes);
        SetButtonInteractable(heroAutoSetTeamButton, canChangeTeam);
        SetButtonsInteractable(heroTeamSlotButtons, canChangeTeam);
        GateButton(heroDetailLevelButton, canManageHeroes);
        SetButtonInteractable(heroDetailEquipGearButton, canManageHeroes);
        SetButtonInteractable(heroDetailRemoveGearButton, canManageHeroes);
        GateButton(weaponUpgradeButton, canManageHeroes);
        GateButton(armorUpgradeButton, canManageHeroes);
        SetButtonInteractable(accessoryPreviousSlotButton, canManageHeroes);
        SetButtonInteractable(accessoryNextSlotButton, canManageHeroes);
        SetButtonInteractable(accessoryPreviousRarityButton, canManageHeroes);
        SetButtonInteractable(accessoryNextRarityButton, canManageHeroes);
        GateButton(accessoryEquipButton, canManageHeroes);
        GateButton(accessoryLevelButton, canManageHeroes);
        GateButton(accessoryFuseButton, canInteract);
        GateButton(summonButton, canInteract);
        GateButton(summonTenButton, canInteract);
        GateButton(summonResultTenButton, canInteract);
        GateButton(summonResultMaxButton, canInteract);
        SetButtonInteractable(summonResultCloseButton, true);
        SetButtonInteractable(summonAutoToggleButton, canInteract || summonAutoRunning);
        SetButtonInteractable(resetButton, canInteract && !backendGameplayEnabled);
        SetButtonInteractable(debugGoldButton, canInteract && !backendGameplayEnabled);
        SetButtonInteractable(debugEssenceButton, canInteract && !backendGameplayEnabled);
        SetButtonInteractable(debugGemsButton, canInteract && !backendGameplayEnabled);
        SetButtonInteractable(debugAccessoryButton, canInteract && !backendGameplayEnabled);
        SetButtonsInteractable(heroSelectButtons, canManageHeroes);
        GateButtons(dailyMissionButtons, canInteract);
        GateButtons(battlePassRewardButtons, canInteract);
        RefreshHeroDetailGearList();
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
        EnsureRuntimeHeroSelectButtons();
        if (heroSelectButtons == null || heroSelectButtons.Length == 0)
        {
            RegisterHeroScreenControls();
            return;
        }

        if (heroSelectButtons.Length > 0 && heroSelectButtons[0] != null) heroSelectButtons[0].onClick.AddListener(SelectHeroCard0);
        if (heroSelectButtons.Length > 1 && heroSelectButtons[1] != null) heroSelectButtons[1].onClick.AddListener(SelectHeroCard1);
        if (heroSelectButtons.Length > 2 && heroSelectButtons[2] != null) heroSelectButtons[2].onClick.AddListener(SelectHeroCard2);
        if (heroSelectButtons.Length > 3 && heroSelectButtons[3] != null) heroSelectButtons[3].onClick.AddListener(SelectHeroCard3);
        if (heroSelectButtons.Length > 4 && heroSelectButtons[4] != null) heroSelectButtons[4].onClick.AddListener(SelectHeroCard4);
        if (heroSelectButtons.Length > 5 && heroSelectButtons[5] != null) heroSelectButtons[5].onClick.AddListener(SelectHeroCard5);

        RegisterHeroScreenControls();
        RegisterHeroDragTriggers();
    }

    private void UnregisterHeroButtons()
    {
        if (heroSelectButtons == null || heroSelectButtons.Length == 0)
        {
            UnregisterHeroScreenControls();
            return;
        }

        if (heroSelectButtons.Length > 0 && heroSelectButtons[0] != null) heroSelectButtons[0].onClick.RemoveListener(SelectHeroCard0);
        if (heroSelectButtons.Length > 1 && heroSelectButtons[1] != null) heroSelectButtons[1].onClick.RemoveListener(SelectHeroCard1);
        if (heroSelectButtons.Length > 2 && heroSelectButtons[2] != null) heroSelectButtons[2].onClick.RemoveListener(SelectHeroCard2);
        if (heroSelectButtons.Length > 3 && heroSelectButtons[3] != null) heroSelectButtons[3].onClick.RemoveListener(SelectHeroCard3);
        if (heroSelectButtons.Length > 4 && heroSelectButtons[4] != null) heroSelectButtons[4].onClick.RemoveListener(SelectHeroCard4);
        if (heroSelectButtons.Length > 5 && heroSelectButtons[5] != null) heroSelectButtons[5].onClick.RemoveListener(SelectHeroCard5);

        UnregisterHeroScreenControls();
    }

    private void RegisterHeroDragTriggers()
    {
        if (heroSelectButtons != null)
        {
            for (var i = 0; i < heroSelectButtons.Length; i++)
            {
                var button = heroSelectButtons[i];
                if (button == null)
                {
                    continue;
                }

                var capturedIndex = i;
                AddEventTrigger(button.gameObject, EventTriggerType.BeginDrag, eventData => BeginHeroCardDrag(capturedIndex));
                AddEventTrigger(button.gameObject, EventTriggerType.EndDrag, eventData => EndHeroCardDrag(capturedIndex, eventData));
            }
        }

        if (heroTeamSlotButtons != null)
        {
            for (var i = 0; i < heroTeamSlotButtons.Length; i++)
            {
                var button = heroTeamSlotButtons[i];
                if (button == null)
                {
                    continue;
                }

                var capturedIndex = i;
                AddEventTrigger(button.gameObject, EventTriggerType.BeginDrag, eventData => BeginHeroTeamSlotDrag(capturedIndex));
                AddEventTrigger(button.gameObject, EventTriggerType.EndDrag, eventData => EndHeroTeamSlotDrag(capturedIndex, eventData));
            }
        }
    }

    private void RegisterHeroScreenControls()
    {
        if (heroRosterTabButton != null) heroRosterTabButton.onClick.AddListener(ShowHeroesRosterTab);
        if (heroSetTeamTabButton != null) heroSetTeamTabButton.onClick.AddListener(ShowHeroesSetTeamTab);
        if (heroSortToggleButton != null) heroSortToggleButton.onClick.AddListener(ToggleHeroSortDirection);
        if (heroAttackTypeFilterButton != null) heroAttackTypeFilterButton.onClick.AddListener(CycleHeroAttackTypeFilter);
        if (heroAutoSetTeamButton != null) heroAutoSetTeamButton.onClick.AddListener(AutoSetTeamByPower);
        if (heroTeamSlotButtons != null)
        {
            if (heroTeamSlotButtons.Length > 0 && heroTeamSlotButtons[0] != null) heroTeamSlotButtons[0].onClick.AddListener(SelectHeroTeamSlot0);
            if (heroTeamSlotButtons.Length > 1 && heroTeamSlotButtons[1] != null) heroTeamSlotButtons[1].onClick.AddListener(SelectHeroTeamSlot1);
            if (heroTeamSlotButtons.Length > 2 && heroTeamSlotButtons[2] != null) heroTeamSlotButtons[2].onClick.AddListener(SelectHeroTeamSlot2);
            if (heroTeamSlotButtons.Length > 3 && heroTeamSlotButtons[3] != null) heroTeamSlotButtons[3].onClick.AddListener(SelectHeroTeamSlot3);
            if (heroTeamSlotButtons.Length > 4 && heroTeamSlotButtons[4] != null) heroTeamSlotButtons[4].onClick.AddListener(SelectHeroTeamSlot4);
        }
    }

    private void UnregisterHeroScreenControls()
    {
        if (heroRosterTabButton != null) heroRosterTabButton.onClick.RemoveListener(ShowHeroesRosterTab);
        if (heroSetTeamTabButton != null) heroSetTeamTabButton.onClick.RemoveListener(ShowHeroesSetTeamTab);
        if (heroSortToggleButton != null) heroSortToggleButton.onClick.RemoveListener(ToggleHeroSortDirection);
        if (heroAttackTypeFilterButton != null) heroAttackTypeFilterButton.onClick.RemoveListener(CycleHeroAttackTypeFilter);
        if (heroAutoSetTeamButton != null) heroAutoSetTeamButton.onClick.RemoveListener(AutoSetTeamByPower);
        if (heroTeamSlotButtons != null)
        {
            if (heroTeamSlotButtons.Length > 0 && heroTeamSlotButtons[0] != null) heroTeamSlotButtons[0].onClick.RemoveListener(SelectHeroTeamSlot0);
            if (heroTeamSlotButtons.Length > 1 && heroTeamSlotButtons[1] != null) heroTeamSlotButtons[1].onClick.RemoveListener(SelectHeroTeamSlot1);
            if (heroTeamSlotButtons.Length > 2 && heroTeamSlotButtons[2] != null) heroTeamSlotButtons[2].onClick.RemoveListener(SelectHeroTeamSlot2);
            if (heroTeamSlotButtons.Length > 3 && heroTeamSlotButtons[3] != null) heroTeamSlotButtons[3].onClick.RemoveListener(SelectHeroTeamSlot3);
            if (heroTeamSlotButtons.Length > 4 && heroTeamSlotButtons[4] != null) heroTeamSlotButtons[4].onClick.RemoveListener(SelectHeroTeamSlot4);
        }
    }

    private void RegisterHeroDetailGearButtons()
    {
        if (heroDetailGearSlotButtons != null && heroDetailGearSlotButtons.Length > 0)
        {
            if (heroDetailGearSlotButtons.Length > 0 && heroDetailGearSlotButtons[0] != null) heroDetailGearSlotButtons[0].onClick.AddListener(ShowHeroDetailGearSlot0);
            if (heroDetailGearSlotButtons.Length > 1 && heroDetailGearSlotButtons[1] != null) heroDetailGearSlotButtons[1].onClick.AddListener(ShowHeroDetailGearSlot1);
            if (heroDetailGearSlotButtons.Length > 2 && heroDetailGearSlotButtons[2] != null) heroDetailGearSlotButtons[2].onClick.AddListener(ShowHeroDetailGearSlot2);
            if (heroDetailGearSlotButtons.Length > 3 && heroDetailGearSlotButtons[3] != null) heroDetailGearSlotButtons[3].onClick.AddListener(ShowHeroDetailGearSlot3);
            if (heroDetailGearSlotButtons.Length > 4 && heroDetailGearSlotButtons[4] != null) heroDetailGearSlotButtons[4].onClick.AddListener(ShowHeroDetailGearSlot4);
            if (heroDetailGearSlotButtons.Length > 5 && heroDetailGearSlotButtons[5] != null) heroDetailGearSlotButtons[5].onClick.AddListener(ShowHeroDetailGearSlot5);
        }

        if (heroDetailGearOptionButtons != null && heroDetailGearOptionButtons.Length > 0)
        {
            if (heroDetailGearOptionButtons.Length > 0 && heroDetailGearOptionButtons[0] != null) heroDetailGearOptionButtons[0].onClick.AddListener(EquipHeroDetailGearOption0);
            if (heroDetailGearOptionButtons.Length > 1 && heroDetailGearOptionButtons[1] != null) heroDetailGearOptionButtons[1].onClick.AddListener(EquipHeroDetailGearOption1);
            if (heroDetailGearOptionButtons.Length > 2 && heroDetailGearOptionButtons[2] != null) heroDetailGearOptionButtons[2].onClick.AddListener(EquipHeroDetailGearOption2);
            if (heroDetailGearOptionButtons.Length > 3 && heroDetailGearOptionButtons[3] != null) heroDetailGearOptionButtons[3].onClick.AddListener(EquipHeroDetailGearOption3);
            if (heroDetailGearOptionButtons.Length > 4 && heroDetailGearOptionButtons[4] != null) heroDetailGearOptionButtons[4].onClick.AddListener(EquipHeroDetailGearOption4);
        }

        if (heroDetailGearListCloseButton != null)
        {
            heroDetailGearListCloseButton.onClick.AddListener(HideHeroDetailGearList);
        }
    }

    private void UnregisterHeroDetailGearButtons()
    {
        if (heroDetailGearSlotButtons != null && heroDetailGearSlotButtons.Length > 0)
        {
            if (heroDetailGearSlotButtons.Length > 0 && heroDetailGearSlotButtons[0] != null) heroDetailGearSlotButtons[0].onClick.RemoveListener(ShowHeroDetailGearSlot0);
            if (heroDetailGearSlotButtons.Length > 1 && heroDetailGearSlotButtons[1] != null) heroDetailGearSlotButtons[1].onClick.RemoveListener(ShowHeroDetailGearSlot1);
            if (heroDetailGearSlotButtons.Length > 2 && heroDetailGearSlotButtons[2] != null) heroDetailGearSlotButtons[2].onClick.RemoveListener(ShowHeroDetailGearSlot2);
            if (heroDetailGearSlotButtons.Length > 3 && heroDetailGearSlotButtons[3] != null) heroDetailGearSlotButtons[3].onClick.RemoveListener(ShowHeroDetailGearSlot3);
            if (heroDetailGearSlotButtons.Length > 4 && heroDetailGearSlotButtons[4] != null) heroDetailGearSlotButtons[4].onClick.RemoveListener(ShowHeroDetailGearSlot4);
            if (heroDetailGearSlotButtons.Length > 5 && heroDetailGearSlotButtons[5] != null) heroDetailGearSlotButtons[5].onClick.RemoveListener(ShowHeroDetailGearSlot5);
        }

        if (heroDetailGearOptionButtons != null && heroDetailGearOptionButtons.Length > 0)
        {
            if (heroDetailGearOptionButtons.Length > 0 && heroDetailGearOptionButtons[0] != null) heroDetailGearOptionButtons[0].onClick.RemoveListener(EquipHeroDetailGearOption0);
            if (heroDetailGearOptionButtons.Length > 1 && heroDetailGearOptionButtons[1] != null) heroDetailGearOptionButtons[1].onClick.RemoveListener(EquipHeroDetailGearOption1);
            if (heroDetailGearOptionButtons.Length > 2 && heroDetailGearOptionButtons[2] != null) heroDetailGearOptionButtons[2].onClick.RemoveListener(EquipHeroDetailGearOption2);
            if (heroDetailGearOptionButtons.Length > 3 && heroDetailGearOptionButtons[3] != null) heroDetailGearOptionButtons[3].onClick.RemoveListener(EquipHeroDetailGearOption3);
            if (heroDetailGearOptionButtons.Length > 4 && heroDetailGearOptionButtons[4] != null) heroDetailGearOptionButtons[4].onClick.RemoveListener(EquipHeroDetailGearOption4);
        }

        if (heroDetailGearListCloseButton != null)
        {
            heroDetailGearListCloseButton.onClick.RemoveListener(HideHeroDetailGearList);
        }
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

    private void SelectHeroCard0() => SelectHeroCard(0);
    private void SelectHeroCard1() => SelectHeroCard(1);
    private void SelectHeroCard2() => SelectHeroCard(2);
    private void SelectHeroCard3() => SelectHeroCard(3);
    private void SelectHeroCard4() => SelectHeroCard(4);
    private void SelectHeroCard5() => SelectHeroCard(5);
    private void SelectHeroCard(int cardIndex)
    {
        var heroIndex = GetHeroCardDisplayIndex(cardIndex);
        if (heroIndex < 0)
        {
            return;
        }

        if (activeScreen == AppScreen.Heroes && heroesTabMode == HeroesTabMode.SetTeam)
        {
            AssignHeroToSelectedTeamSlot(heroIndex);
            return;
        }

        SelectHero(heroIndex);
    }

    private void SelectHero(int index)
    {
        selectedHeroIndex = Mathf.Clamp(index, 0, HeroCount - 1);
        SyncSelectedHeroEquipmentMirrors();
        SyncSelectedHeroAccessoryMirrors();
        SaveProgress();
        RefreshUi();

        if (activeScreen == AppScreen.Heroes)
        {
            ShowHeroDetail(selectedHeroIndex);
        }
    }

    private void ShowHeroesRosterTab()
    {
        heroesTabMode = HeroesTabMode.Hero;
        selectedHeroTeamSlotIndex = -1;
        RefreshHeroUi();
        RefreshGameplayInteractivity();
    }

    private void ShowHeroesSetTeamTab()
    {
        heroesTabMode = HeroesTabMode.SetTeam;
        selectedHeroTeamSlotIndex = -1;
        HideHeroDetail();
        RefreshHeroUi();
        RefreshGameplayInteractivity();
    }

    private void ToggleHeroSortDirection()
    {
        heroSortDirection = heroSortDirection == HeroSortDirection.Descending
            ? HeroSortDirection.Ascending
            : HeroSortDirection.Descending;
        RefreshHeroUi();
    }

    private void CycleHeroAttackTypeFilter()
    {
        heroAttackTypeFilter = heroAttackTypeFilter == HeroAttackTypeFilter.All
            ? HeroAttackTypeFilter.Melee
            : heroAttackTypeFilter == HeroAttackTypeFilter.Melee
                ? HeroAttackTypeFilter.Ranged
                : HeroAttackTypeFilter.All;
        RefreshHeroUi();
    }

    private void AutoSetTeamByPower()
    {
        if (!CanChangeHeroTeamNow())
        {
            return;
        }

        EnsureFormationOrder();
        var indices = CreateAllHeroIndices();
        SortHeroIndicesByPower(indices, descending: true);
        var changed = false;
        for (var i = 0; i < HeroCount; i++)
        {
            changed |= formationSlotHeroIndices[i] != indices[i];
            formationSlotHeroIndices[i] = indices[i];
        }

        selectedHeroTeamSlotIndex = -1;
        if (changed)
        {
            CancelCampaignFightForFormationChange();
        }

        SaveProgress();
        RefreshUi();
    }

    private bool CanChangeHeroTeamNow()
    {
        return !backendRequestInProgress && !backendLifecycleFlushInProgress && (!campaignFightInProgress || battleTargetMode == BattleTargetMode.Campaign);
    }

    private void CancelCampaignFightForFormationChange()
    {
        if (!campaignFightInProgress || battleTargetMode != BattleTargetMode.Campaign)
        {
            return;
        }

        fightCancelRequested = true;
        if (activeFightCoroutine != null)
        {
            StopCoroutine(activeFightCoroutine);
            activeFightCoroutine = null;
            fightCancelRequested = false;
        }

        campaignFightInProgress = false;
        autoContinueFightsEnabled = false;
        fightAutoSkillsEnabled = false;
        if (autoContinueFightCoroutine != null)
        {
            StopCoroutine(autoContinueFightCoroutine);
            autoContinueFightCoroutine = null;
        }

        SetProjectilesVisible(fightHeroProjectileImages, false);
        SetProjectilesVisible(fightEnemyProjectileImages, false);
        SetRawImagesVisible(fightHeroFxImages, false);
        HideRavikSkeletalViews(fightHeroSkeletalViews);
        SetBattleFlowMode(BattleFlowMode.Formation);
        SetDungeonResult("Fight cancelled because the team formation changed.");
        RefreshFormationAutoContinueToggle();
        RefreshFightAutoSkillButton();
    }

    private void SelectHeroTeamSlot0() => SelectHeroTeamSlot(0);
    private void SelectHeroTeamSlot1() => SelectHeroTeamSlot(1);
    private void SelectHeroTeamSlot2() => SelectHeroTeamSlot(2);
    private void SelectHeroTeamSlot3() => SelectHeroTeamSlot(3);
    private void SelectHeroTeamSlot4() => SelectHeroTeamSlot(4);

    private void SelectHeroTeamSlot(int slotIndex)
    {
        if (!CanChangeHeroTeamNow())
        {
            return;
        }

        EnsureFormationOrder();
        slotIndex = Mathf.Clamp(slotIndex, 0, HeroCount - 1);
        if (selectedHeroTeamSlotIndex < 0)
        {
            selectedHeroTeamSlotIndex = slotIndex;
        }
        else if (selectedHeroTeamSlotIndex == slotIndex)
        {
            selectedHeroTeamSlotIndex = -1;
        }
        else
        {
            SwapTeamSlots(selectedHeroTeamSlotIndex, slotIndex);
            selectedHeroTeamSlotIndex = -1;
            CancelCampaignFightForFormationChange();
            SaveProgress();
        }

        RefreshHeroUi();
    }

    private void AssignHeroToSelectedTeamSlot(int heroIndex)
    {
        if (!CanChangeHeroTeamNow())
        {
            return;
        }

        EnsureFormationOrder();
        heroIndex = Mathf.Clamp(heroIndex, 0, HeroCount - 1);
        selectedHeroIndex = heroIndex;
        SyncSelectedHeroEquipmentMirrors();
        SyncSelectedHeroAccessoryMirrors();

        var currentSlot = FindFormationSlotForHero(heroIndex);
        if (selectedHeroTeamSlotIndex < 0)
        {
            selectedHeroTeamSlotIndex = currentSlot >= 0 ? currentSlot : 0;
            RefreshHeroUi();
            return;
        }

        var targetSlot = Mathf.Clamp(selectedHeroTeamSlotIndex, 0, HeroCount - 1);
        if (currentSlot == targetSlot)
        {
            selectedHeroTeamSlotIndex = -1;
            RefreshHeroUi();
            return;
        }

        if (currentSlot >= 0)
        {
            SwapTeamSlots(targetSlot, currentSlot);
        }
        else
        {
            formationSlotHeroIndices[targetSlot] = heroIndex;
        }

        selectedHeroTeamSlotIndex = -1;
        CancelCampaignFightForFormationChange();
        SaveProgress();
        RefreshUi();
    }

    private void SwapTeamSlots(int firstSlot, int secondSlot)
    {
        EnsureFormationOrder();
        firstSlot = Mathf.Clamp(firstSlot, 0, HeroCount - 1);
        secondSlot = Mathf.Clamp(secondSlot, 0, HeroCount - 1);
        var firstHero = formationSlotHeroIndices[firstSlot];
        formationSlotHeroIndices[firstSlot] = formationSlotHeroIndices[secondSlot];
        formationSlotHeroIndices[secondSlot] = firstHero;
    }

    private void BeginHeroCardDrag(int cardIndex)
    {
        draggedHeroCardIndex = cardIndex;
    }

    private void EndHeroCardDrag(int cardIndex, BaseEventData eventData)
    {
        if (activeScreen != AppScreen.Heroes)
        {
            draggedHeroCardIndex = -1;
            return;
        }

        var targetSlot = GetPointerHeroTeamSlot(eventData);
        if (targetSlot >= 0)
        {
            selectedHeroTeamSlotIndex = targetSlot;
            var heroIndex = GetHeroCardDisplayIndex(draggedHeroCardIndex >= 0 ? draggedHeroCardIndex : cardIndex);
            if (heroIndex >= 0)
            {
                heroesTabMode = HeroesTabMode.SetTeam;
                AssignHeroToSelectedTeamSlot(heroIndex);
            }
        }

        draggedHeroCardIndex = -1;
    }

    private void BeginHeroTeamSlotDrag(int slotIndex)
    {
        draggedHeroTeamSlotIndex = Mathf.Clamp(slotIndex, 0, HeroCount - 1);
    }

    private void EndHeroTeamSlotDrag(int slotIndex, BaseEventData eventData)
    {
        if (activeScreen != AppScreen.Heroes || !CanChangeHeroTeamNow())
        {
            draggedHeroTeamSlotIndex = -1;
            return;
        }

        var targetSlot = GetPointerHeroTeamSlot(eventData);
        var sourceSlot = draggedHeroTeamSlotIndex >= 0 ? draggedHeroTeamSlotIndex : Mathf.Clamp(slotIndex, 0, HeroCount - 1);
        if (targetSlot >= 0 && targetSlot != sourceSlot)
        {
            heroesTabMode = HeroesTabMode.SetTeam;
            SwapTeamSlots(sourceSlot, targetSlot);
            selectedHeroTeamSlotIndex = -1;
            CancelCampaignFightForFormationChange();
            SaveProgress();
            RefreshUi();
        }

        draggedHeroTeamSlotIndex = -1;
    }

    private int GetPointerHeroTeamSlot(BaseEventData eventData)
    {
        var pointerEvent = eventData as PointerEventData;
        if (pointerEvent == null || heroTeamSlotFrames == null)
        {
            return -1;
        }

        for (var slotIndex = 0; slotIndex < heroTeamSlotFrames.Length; slotIndex++)
        {
            var frame = heroTeamSlotFrames[slotIndex];
            if (frame == null)
            {
                continue;
            }

            var rect = frame.GetComponent<RectTransform>();
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, pointerEvent.position, pointerEvent.pressEventCamera))
            {
                return slotIndex;
            }
        }

        return -1;
    }

    private static void AddEventTrigger(GameObject target, EventTriggerType triggerType, Action<BaseEventData> callback)
    {
        if (target == null || callback == null)
        {
            return;
        }

        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }

        var entry = new EventTrigger.Entry { eventID = triggerType };
        entry.callback.AddListener(eventData => callback(eventData));
        trigger.triggers.Add(entry);
    }

    private void RegisterSummonCarouselButtons()
    {
        if (summonCarouselPreviousButton != null) summonCarouselPreviousButton.onClick.AddListener(ShowPreviousSummonBanner);
        if (summonCarouselNextButton != null) summonCarouselNextButton.onClick.AddListener(ShowNextSummonBanner);
        if (summonCarouselButtons != null)
        {
            if (summonCarouselButtons.Length > 0 && summonCarouselButtons[0] != null) summonCarouselButtons[0].onClick.AddListener(SelectSummonCarouselCard0);
            if (summonCarouselButtons.Length > 1 && summonCarouselButtons[1] != null) summonCarouselButtons[1].onClick.AddListener(SelectSummonCarouselCard1);
            if (summonCarouselButtons.Length > 2 && summonCarouselButtons[2] != null) summonCarouselButtons[2].onClick.AddListener(SelectSummonCarouselCard2);
        }
    }

    private void UnregisterSummonCarouselButtons()
    {
        if (summonCarouselPreviousButton != null) summonCarouselPreviousButton.onClick.RemoveListener(ShowPreviousSummonBanner);
        if (summonCarouselNextButton != null) summonCarouselNextButton.onClick.RemoveListener(ShowNextSummonBanner);
        if (summonCarouselButtons != null)
        {
            if (summonCarouselButtons.Length > 0 && summonCarouselButtons[0] != null) summonCarouselButtons[0].onClick.RemoveListener(SelectSummonCarouselCard0);
            if (summonCarouselButtons.Length > 1 && summonCarouselButtons[1] != null) summonCarouselButtons[1].onClick.RemoveListener(SelectSummonCarouselCard1);
            if (summonCarouselButtons.Length > 2 && summonCarouselButtons[2] != null) summonCarouselButtons[2].onClick.RemoveListener(SelectSummonCarouselCard2);
        }
    }

    private void RegisterSummonResultButtons()
    {
        if (summonResultCloseButton != null) summonResultCloseButton.onClick.AddListener(HideSummonResultPopup);
        if (summonResultTenButton != null) summonResultTenButton.onClick.AddListener(SummonResultTen);
        if (summonResultMaxButton != null) summonResultMaxButton.onClick.AddListener(SummonResultMax);
        if (summonAutoToggleButton != null) summonAutoToggleButton.onClick.AddListener(ToggleSummonAuto);
    }

    private void UnregisterSummonResultButtons()
    {
        if (summonResultCloseButton != null) summonResultCloseButton.onClick.RemoveListener(HideSummonResultPopup);
        if (summonResultTenButton != null) summonResultTenButton.onClick.RemoveListener(SummonResultTen);
        if (summonResultMaxButton != null) summonResultMaxButton.onClick.RemoveListener(SummonResultMax);
        if (summonAutoToggleButton != null) summonAutoToggleButton.onClick.RemoveListener(ToggleSummonAuto);
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

        if (homeQuestButton != null)
        {
            homeQuestButton.onClick.AddListener(ShowShop);
        }

        if (homeRewardsButton != null)
        {
            homeRewardsButton.onClick.AddListener(ShowFastRewardsPopup);
        }

        if (homeTreasureChestButton != null)
        {
            homeTreasureChestButton.onClick.AddListener(ShowInventoryPopup);
        }

        if (homeShopButton != null)
        {
            homeShopButton.onClick.AddListener(ShowShop);
        }

        if (homeWorldMapButton != null)
        {
            homeWorldMapButton.onClick.AddListener(ShowHome);
        }

        if (homeChatButton != null)
        {
            homeChatButton.onClick.AddListener(ShowChatPopup);
        }

        if (topGemPlusButton != null)
        {
            topGemPlusButton.onClick.AddListener(ShowShop);
        }

        if (homeShortcutToggleButton != null)
        {
            homeShortcutToggleButton.onClick.AddListener(ToggleHomeShortcuts);
        }

        if (homeLeftShortcutToggleButton != null)
        {
            homeLeftShortcutToggleButton.onClick.AddListener(ToggleHomeShortcuts);
        }

        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.onClick.AddListener(HideInventoryPopup);
        }

        if (inventoryDetailCloseButton != null)
        {
            inventoryDetailCloseButton.onClick.AddListener(HideInventoryItemDetails);
        }

        if (inventoryMiscTabButton != null)
        {
            inventoryMiscTabButton.onClick.AddListener(ShowInventoryMiscTab);
        }

        if (inventoryGearTabButton != null)
        {
            inventoryGearTabButton.onClick.AddListener(ShowInventoryGearTab);
        }

        if (inventoryAllTabButton != null)
        {
            inventoryAllTabButton.onClick.AddListener(ShowInventoryAllTab);
        }

        if (fastRewardsCloseButton != null)
        {
            fastRewardsCloseButton.onClick.AddListener(HideFastRewardsPopup);
        }

        if (fastRewardsRedeemButton != null)
        {
            fastRewardsRedeemButton.onClick.AddListener(RedeemFastRewards);
        }

        if (chatCloseButton != null)
        {
            chatCloseButton.onClick.AddListener(HideChatPopup);
        }

        if (formationConfirmButton != null)
        {
            formationConfirmButton.onClick.AddListener(StartCampaignFightFromFormation);
        }

        if (formationBackButton != null)
        {
            formationBackButton.onClick.AddListener(BackToCampaignMap);
        }

        if (formationAutoContinueButton != null)
        {
            formationAutoContinueButton.onClick.AddListener(ToggleFormationAutoContinue);
        }

        if (fightContinueButton != null)
        {
            fightContinueButton.onClick.AddListener(ContinueAfterCampaignFight);
        }

        if (fightEndButton != null)
        {
            fightEndButton.onClick.AddListener(EndCurrentFight);
        }

        if (villageNavButton != null)
        {
            villageNavButton.onClick.AddListener(ShowHome);
        }

        if (campaignNavButton != null)
        {
            campaignNavButton.onClick.AddListener(ShowHome);
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

        if (homeQuestButton != null)
        {
            homeQuestButton.onClick.RemoveListener(ShowShop);
        }

        if (homeRewardsButton != null)
        {
            homeRewardsButton.onClick.RemoveListener(ShowFastRewardsPopup);
        }

        if (homeTreasureChestButton != null)
        {
            homeTreasureChestButton.onClick.RemoveListener(ShowInventoryPopup);
        }

        if (homeShopButton != null)
        {
            homeShopButton.onClick.RemoveListener(ShowShop);
        }

        if (homeWorldMapButton != null)
        {
            homeWorldMapButton.onClick.RemoveListener(ShowHome);
        }

        if (homeChatButton != null)
        {
            homeChatButton.onClick.RemoveListener(ShowChatPopup);
        }

        if (topGemPlusButton != null)
        {
            topGemPlusButton.onClick.RemoveListener(ShowShop);
        }

        if (homeShortcutToggleButton != null)
        {
            homeShortcutToggleButton.onClick.RemoveListener(ToggleHomeShortcuts);
        }

        if (homeLeftShortcutToggleButton != null)
        {
            homeLeftShortcutToggleButton.onClick.RemoveListener(ToggleHomeShortcuts);
        }

        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.onClick.RemoveListener(HideInventoryPopup);
        }

        if (inventoryDetailCloseButton != null)
        {
            inventoryDetailCloseButton.onClick.RemoveListener(HideInventoryItemDetails);
        }

        if (inventoryMiscTabButton != null)
        {
            inventoryMiscTabButton.onClick.RemoveListener(ShowInventoryMiscTab);
        }

        if (inventoryGearTabButton != null)
        {
            inventoryGearTabButton.onClick.RemoveListener(ShowInventoryGearTab);
        }

        if (inventoryAllTabButton != null)
        {
            inventoryAllTabButton.onClick.RemoveListener(ShowInventoryAllTab);
        }

        if (fastRewardsCloseButton != null)
        {
            fastRewardsCloseButton.onClick.RemoveListener(HideFastRewardsPopup);
        }

        if (fastRewardsRedeemButton != null)
        {
            fastRewardsRedeemButton.onClick.RemoveListener(RedeemFastRewards);
        }

        if (chatCloseButton != null)
        {
            chatCloseButton.onClick.RemoveListener(HideChatPopup);
        }

        if (formationConfirmButton != null)
        {
            formationConfirmButton.onClick.RemoveListener(StartCampaignFightFromFormation);
        }

        if (formationBackButton != null)
        {
            formationBackButton.onClick.RemoveListener(BackToCampaignMap);
        }

        if (formationAutoContinueButton != null)
        {
            formationAutoContinueButton.onClick.RemoveListener(ToggleFormationAutoContinue);
        }

        if (fightContinueButton != null)
        {
            fightContinueButton.onClick.RemoveListener(ContinueAfterCampaignFight);
        }

        if (fightEndButton != null)
        {
            fightEndButton.onClick.RemoveListener(EndCurrentFight);
        }

        if (villageNavButton != null)
        {
            villageNavButton.onClick.RemoveListener(ShowHome);
        }

        if (campaignNavButton != null)
        {
            campaignNavButton.onClick.RemoveListener(ShowHome);
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
        if (screen != AppScreen.Heroes && heroDetailRoot != null)
        {
            heroDetailRoot.gameObject.SetActive(false);
        }

        ApplyBattleFlowVisibility();
        ApplyNavigationChromeVisibility();
    }

    private void SetBattleFlowMode(BattleFlowMode mode)
    {
        battleFlowMode = mode;
        ApplyBattleFlowVisibility();
    }

    private void ApplyBattleFlowVisibility()
    {
        var battleVisible = activeScreen == AppScreen.Battle;
        if (formationRoot != null)
        {
            formationRoot.gameObject.SetActive(battleVisible && battleFlowMode == BattleFlowMode.Formation);
        }

        if (fightRoot != null)
        {
            fightRoot.gameObject.SetActive(battleVisible && (battleFlowMode == BattleFlowMode.Fight || battleFlowMode == BattleFlowMode.Result));
        }

        if (fightResultRoot != null)
        {
            fightResultRoot.gameObject.SetActive(battleVisible && battleFlowMode == BattleFlowMode.Result);
        }

        if (fightEndButton != null)
        {
            fightEndButton.gameObject.SetActive(battleVisible && battleFlowMode == BattleFlowMode.Fight);
        }

        if (fightAutoSkillButton != null)
        {
            fightAutoSkillButton.gameObject.SetActive(battleVisible && battleFlowMode == BattleFlowMode.Fight);
        }

        if (fightSpeedButton != null)
        {
            fightSpeedButton.gameObject.SetActive(battleVisible && battleFlowMode == BattleFlowMode.Fight);
        }

        ApplyNavigationChromeVisibility();
    }

    private void ApplyNavigationChromeVisibility()
    {
        var hideChrome = activeScreen == AppScreen.Battle && battleTargetMode == BattleTargetMode.Dungeon;
        if (topBarRoot != null)
        {
            topBarRoot.gameObject.SetActive(!hideChrome);
        }

        if (bottomNavRoot != null)
        {
            bottomNavRoot.gameObject.SetActive(!hideChrome);
        }

        if (artBottomNavRoot != null)
        {
            artBottomNavRoot.gameObject.SetActive(!hideChrome);
        }
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
        EnsureFormationOrder();
        RebuildHeroCardDisplayIndices();

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
            selectedHeroText.text = $"{hero.name}  Lv. {FormatCappedValue(heroLevels[selectedHeroIndex], GetHeroLevelCap(selectedHeroIndex))}  Asc. {FormatCappedValue(heroAscensions[selectedHeroIndex], GetHeroAscensionCap(selectedHeroIndex))}\n{hero.rarityName} {GetHeroAttackTypeLabel(selectedHeroIndex)}  Power {GetHeroPower(selectedHeroIndex)}\nATK {GetHeroEffectiveAttack(selectedHeroIndex)}  HP {GetHeroCombatMaxHealth(selectedHeroIndex)}  Shards {heroShards[selectedHeroIndex]}";
        }

        if (heroCardTexts != null)
        {
            for (var i = 0; i < heroCardTexts.Length; i++)
            {
                var heroIndex = GetHeroCardDisplayIndex(i);
                if (heroCardTexts[i] != null)
                {
                    heroCardTexts[i].text = string.Empty;
                    heroCardTexts[i].gameObject.SetActive(false);
                }

                RefreshHeroRosterCardText(i, heroIndex);
            }
        }
        else if (heroCardLevelTexts != null)
        {
            for (var i = 0; i < heroCardLevelTexts.Length; i++)
            {
                RefreshHeroRosterCardText(i, GetHeroCardDisplayIndex(i));
            }
        }

        RefreshHeroesTabModeUi();
        RefreshHeroTeamUi();
        RefreshHeroCardVisuals();
        RefreshHeroDetailUi();
    }

    private void RefreshHeroRosterCardText(int cardIndex, int heroIndex)
    {
        var visible = heroIndex >= 0;
        if (heroCardLevelTexts != null && cardIndex < heroCardLevelTexts.Length && heroCardLevelTexts[cardIndex] != null)
        {
            heroCardLevelTexts[cardIndex].gameObject.SetActive(visible);
            heroCardLevelTexts[cardIndex].text = visible ? $"Lv. {FormatCappedValue(heroLevels[heroIndex], GetHeroLevelCap(heroIndex))}" : string.Empty;
        }

        if (heroCardStarTexts != null && cardIndex < heroCardStarTexts.Length && heroCardStarTexts[cardIndex] != null)
        {
            heroCardStarTexts[cardIndex].gameObject.SetActive(visible);
            heroCardStarTexts[cardIndex].text = visible ? GetHeroRarityStars(heroIndex) : string.Empty;
        }

        if (heroCardRoleBadgeTexts != null && cardIndex < heroCardRoleBadgeTexts.Length && heroCardRoleBadgeTexts[cardIndex] != null)
        {
            heroCardRoleBadgeTexts[cardIndex].transform.parent.gameObject.SetActive(visible);
            heroCardRoleBadgeTexts[cardIndex].text = visible ? (IsHeroRangedCombatant(heroIndex) ? "R" : "M") : string.Empty;
        }

        if (heroCardTeamBadgeTexts != null && cardIndex < heroCardTeamBadgeTexts.Length && heroCardTeamBadgeTexts[cardIndex] != null)
        {
            heroCardTeamBadgeTexts[cardIndex].transform.parent.gameObject.SetActive(visible && FindFormationSlotForHero(heroIndex) >= 0);
        }

        if (heroCardShardTexts != null && cardIndex < heroCardShardTexts.Length && heroCardShardTexts[cardIndex] != null)
        {
            var ascensionNeed = visible ? Mathf.Max(1, GetHeroAscensionCost(heroIndex)) : 1;
            heroCardShardTexts[cardIndex].gameObject.SetActive(visible);
            heroCardShardTexts[cardIndex].text = visible ? $"{heroShards[heroIndex]}/{ascensionNeed}" : string.Empty;
            if (heroCardShardFills != null && cardIndex < heroCardShardFills.Length && heroCardShardFills[cardIndex] != null)
            {
                SetRuntimeFillPercent(heroCardShardFills[cardIndex], visible ? heroShards[heroIndex] / (float)ascensionNeed : 0f);
                heroCardShardFills[cardIndex].transform.parent.gameObject.SetActive(visible);
            }
        }
        else if (heroCardShardFills != null && cardIndex < heroCardShardFills.Length && heroCardShardFills[cardIndex] != null)
        {
            heroCardShardFills[cardIndex].transform.parent.gameObject.SetActive(visible);
            if (!visible)
            {
                SetRuntimeFillPercent(heroCardShardFills[cardIndex], 0f);
            }
        }
    }

    private void RefreshHeroesTabModeUi()
    {
        var isSetTeam = heroesTabMode == HeroesTabMode.SetTeam;
        LayoutHeroCards();
        SetComponentActive(heroTeamRoot, isSetTeam);
        SetComponentActive(heroAutoSetTeamButton, isSetTeam);
        SetComponentActive(heroTeamHintText, isSetTeam);
        HideLegacyHeroesOverviewElements();
        SetComponentActive(heroSortToggleButton, !isSetTeam);
        SetComponentActive(heroAttackTypeFilterButton, !isSetTeam);
        SetComponentActive(heroRosterFilterRoot, !isSetTeam);

        SetHeroSubTabVisual(heroRosterTabButton, heroRosterTabText, !isSetTeam);
        SetHeroSubTabVisual(heroSetTeamTabButton, heroSetTeamTabText, isSetTeam);

        if (heroSortToggleText != null)
        {
            heroSortToggleText.text = heroSortDirection == HeroSortDirection.Descending ? "Desc" : "Asc";
        }

        if (heroAttackTypeFilterText != null)
        {
            heroAttackTypeFilterText.text = heroAttackTypeFilter == HeroAttackTypeFilter.All
                ? "Alle"
                : heroAttackTypeFilter == HeroAttackTypeFilter.Melee ? "Melee" : "Ranged";
        }

        if (heroRosterCountText != null)
        {
            heroRosterCountText.text = $"{CountVisibleHeroCards()}/{HeroCount}";
        }

        if (heroTeamHintText != null)
        {
            heroTeamHintText.text = selectedHeroTeamSlotIndex >= 0
                ? "Tap a hero card or another slot to place/swap."
                : "Tap a slot, then choose a hero. Auto-Set uses highest power.";
        }
    }

    private void HideLegacyHeroesOverviewElements()
    {
        if (heroesPanel != null)
        {
            HideHeroesPanelChild("Hero Header");
            HideHeroesPanelChild("Selected Hero Card");
        }

        SetComponentActive(selectedHeroText, false);
        SetComponentActive(heroUpgradeButton, false);
        SetComponentActive(heroAscendButton, false);
        if (heroEssenceAmountText != null && heroEssenceAmountText.transform.parent != null)
        {
            heroEssenceAmountText.transform.parent.gameObject.SetActive(false);
        }
    }

    private void HideHeroesPanelChild(string childName)
    {
        if (heroesPanel == null)
        {
            return;
        }

        var child = heroesPanel.transform.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void SetHeroSubTabVisual(Button button, TMP_Text text, bool active)
    {
        if (button != null)
        {
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? new Color(0.1f, 0.56f, 0.62f, 0.96f) : new Color(0.18f, 0.12f, 0.07f, 0.88f);
            }
        }

        if (text != null)
        {
            text.color = active ? new Color(1f, 0.94f, 0.68f) : new Color(0.84f, 0.76f, 0.62f);
        }
    }

    private void RefreshHeroTeamUi()
    {
        if (heroTeamRoot == null)
        {
            return;
        }

        EnsureFormationOrder();
        for (var slotIndex = 0; slotIndex < HeroCount; slotIndex++)
        {
            var heroIndex = formationSlotHeroIndices[Mathf.Clamp(slotIndex, 0, formationSlotHeroIndices.Length - 1)];
            heroIndex = Mathf.Clamp(heroIndex, 0, HeroCount - 1);
            var hero = GetHeroDefinition(heroIndex);
            if (heroTeamSlotPortraits != null && slotIndex < heroTeamSlotPortraits.Length && heroTeamSlotPortraits[slotIndex] != null)
            {
                heroTeamSlotPortraits[slotIndex].texture = LoadCombatTexture(GetHeroTextureName(heroIndex), "idle", 0, GetHeroTextureName(heroIndex));
                heroTeamSlotPortraits[slotIndex].rectTransform.localScale = new Vector3(GetHeroFacingScale(heroIndex), 1f, 1f);
                heroTeamSlotPortraits[slotIndex].color = Color.white;
            }

            if (heroTeamSlotTexts != null && slotIndex < heroTeamSlotTexts.Length && heroTeamSlotTexts[slotIndex] != null)
            {
                heroTeamSlotTexts[slotIndex].text = $"{slotIndex + 1}. {hero.name}\nPower {GetHeroPower(heroIndex)}";
            }

            if (heroTeamSlotFrames != null && slotIndex < heroTeamSlotFrames.Length && heroTeamSlotFrames[slotIndex] != null)
            {
                heroTeamSlotFrames[slotIndex].color = selectedHeroTeamSlotIndex == slotIndex
                    ? new Color(1f, 0.74f, 0.18f, 0.96f)
                    : new Color(0.1f, 0.14f, 0.19f, 0.82f);
            }
        }
    }

    private void RebuildHeroCardDisplayIndices()
    {
        var filtered = CreateFilledIntArray(HeroCount, -1);
        var count = 0;
        for (var i = 0; i < HeroCount; i++)
        {
            if (!DoesHeroMatchAttackTypeFilter(i))
            {
                continue;
            }

            filtered[count] = i;
            count++;
        }

        for (var i = 0; i < count - 1; i++)
        {
            for (var j = i + 1; j < count; j++)
            {
                if (CompareHeroCardOrder(filtered[i], filtered[j]) > 0)
                {
                    var temp = filtered[i];
                    filtered[i] = filtered[j];
                    filtered[j] = temp;
                }
            }
        }

        heroCardDisplayIndices = filtered;
    }

    private int CountVisibleHeroCards()
    {
        if (heroCardDisplayIndices == null || heroCardDisplayIndices.Length != HeroCount)
        {
            RebuildHeroCardDisplayIndices();
        }

        var count = 0;
        for (var i = 0; i < heroCardDisplayIndices.Length; i++)
        {
            if (heroCardDisplayIndices[i] >= 0)
            {
                count++;
            }
        }

        return count;
    }

    private int GetHeroCardDisplayIndex(int cardIndex)
    {
        if (heroCardDisplayIndices == null || heroCardDisplayIndices.Length != HeroCount)
        {
            RebuildHeroCardDisplayIndices();
        }

        return cardIndex >= 0 && cardIndex < heroCardDisplayIndices.Length ? heroCardDisplayIndices[cardIndex] : -1;
    }

    private int CompareHeroCardOrder(int firstHeroIndex, int secondHeroIndex)
    {
        var firstTeamSlot = FindFormationSlotForHero(firstHeroIndex);
        var secondTeamSlot = FindFormationSlotForHero(secondHeroIndex);
        var firstInTeam = firstTeamSlot >= 0;
        var secondInTeam = secondTeamSlot >= 0;
        if (firstInTeam != secondInTeam)
        {
            return firstInTeam ? -1 : 1;
        }

        var rarityCompare = GetHeroRarityRank(firstHeroIndex).CompareTo(GetHeroRarityRank(secondHeroIndex));
        if (rarityCompare != 0)
        {
            return heroSortDirection == HeroSortDirection.Descending ? -rarityCompare : rarityCompare;
        }

        var powerCompare = GetHeroPower(firstHeroIndex).CompareTo(GetHeroPower(secondHeroIndex));
        if (powerCompare != 0)
        {
            return heroSortDirection == HeroSortDirection.Descending ? -powerCompare : powerCompare;
        }

        return string.Compare(GetHeroDefinition(firstHeroIndex).name, GetHeroDefinition(secondHeroIndex).name, StringComparison.Ordinal);
    }

    private bool DoesHeroMatchAttackTypeFilter(int heroIndex)
    {
        if (heroesTabMode == HeroesTabMode.SetTeam)
        {
            return true;
        }

        if (heroAttackTypeFilter == HeroAttackTypeFilter.All)
        {
            return true;
        }

        var isRanged = IsHeroRangedCombatant(heroIndex);
        return heroAttackTypeFilter == HeroAttackTypeFilter.Ranged ? isRanged : !isRanged;
    }

    private static string GetHeroAttackTypeLabel(int heroIndex)
    {
        return IsHeroRangedCombatant(heroIndex) ? "Ranged" : "Melee";
    }

    private static int GetHeroRarityRank(int heroIndex)
    {
        var rarityId = GetHeroDefinition(heroIndex).rarityId;
        if (rarityId == LegendaryRarityId)
        {
            return 3;
        }

        if (rarityId == EpicRarityId)
        {
            return 2;
        }

        return 1;
    }

    private static string GetHeroRarityStars(int heroIndex)
    {
        var rank = GetHeroRarityRank(heroIndex);
        if (rank >= 3)
        {
            return "*****";
        }

        return rank == 2 ? "****" : "***";
    }

    private int[] CreateAllHeroIndices()
    {
        var indices = new int[HeroCount];
        for (var i = 0; i < HeroCount; i++)
        {
            indices[i] = i;
        }

        return indices;
    }

    private void SortHeroIndicesByPower(int[] indices, bool descending)
    {
        if (indices == null)
        {
            return;
        }

        for (var i = 0; i < indices.Length - 1; i++)
        {
            for (var j = i + 1; j < indices.Length; j++)
            {
                var compare = GetHeroPower(indices[i]).CompareTo(GetHeroPower(indices[j]));
                if (descending ? compare < 0 : compare > 0)
                {
                    var temp = indices[i];
                    indices[i] = indices[j];
                    indices[j] = temp;
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
        EnsureHeroEquipment();
        var heroIndex = GetSelectedHeroIndex();
        var hero = GetHeroDefinition(heroIndex);
        var heroWeaponLevel = GetHeroEquipmentLevel(heroIndex, isWeapon: true);
        var heroArmorLevel = GetHeroEquipmentLevel(heroIndex, isWeapon: false);
        var displayedWeaponLevel = GetEquipmentDisplayLevel(WeaponTrack, heroWeaponLevel);
        var displayedArmorLevel = GetEquipmentDisplayLevel(ArmorTrack, heroArmorLevel);

        if (equipmentSummaryText != null)
        {
            equipmentSummaryText.text = $"{hero.name} Equipment\n{WeaponTrack.name} Lv. {FormatCappedValue(displayedWeaponLevel, GetEquipmentLevelCap(WeaponTrack))}  +{GetHeroEquipmentAttackBonus(heroIndex)} {WeaponTrack.statLabel}\n{ArmorTrack.name} Lv. {FormatCappedValue(displayedArmorLevel, GetEquipmentLevelCap(ArmorTrack))}  +{GetHeroEquipmentHealthBonus(heroIndex)} {ArmorTrack.statLabel}";
        }

        if (weaponUpgradeCostText != null)
        {
            weaponUpgradeCostText.text = IsEquipmentLevelMax(WeaponTrack, heroWeaponLevel)
                ? $"{WeaponTrack.name}\nMax Lv. {GetEquipmentLevelCap(WeaponTrack)}"
                : $"{WeaponTrack.name} +1\n{GetWeaponUpgradeCost()} Gold";
        }

        if (armorUpgradeCostText != null)
        {
            armorUpgradeCostText.text = IsEquipmentLevelMax(ArmorTrack, heroArmorLevel)
                ? $"{ArmorTrack.name}\nMax Lv. {GetEquipmentLevelCap(ArmorTrack)}"
                : $"{ArmorTrack.name} +1\n{GetArmorUpgradeCost()} Gold";
        }
    }

    private void RefreshAccessoryUi()
    {
        EnsureAccessories();
        var slot = Mathf.Clamp(selectedAccessorySlot, 0, AccessorySlotCount - 1);
        var rarity = Mathf.Clamp(selectedAccessoryRarity, 0, AccessoryRarityCount - 1);
        var heroIndex = GetSelectedHeroIndex();

        if (accessorySummaryText != null)
        {
            accessorySummaryText.text = $"{GetHeroDefinition(heroIndex).name} Accessories\nATK +{GetHeroAccessoryAttackBonus(heroIndex)}  HP +{GetHeroAccessoryHealthBonus(heroIndex)}\nGear Dungeon Floor {gearDungeonFloor}";
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
            var equippedRarity = GetHeroEquippedAccessoryRarity(heroIndex, slot);
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
        var heroIndex = GetSelectedHeroIndex();
        var equippedRarity = GetHeroEquippedAccessoryRarity(heroIndex, slot);
        var canLevel = equippedRarity >= 0 && GetHeroEquippedAccessoryLevel(heroIndex, slot) < GetAccessoryMaxLevel(equippedRarity) && gold >= GetAccessoryLevelCost(slot);

        if (accessoryEquipButton != null)
        {
            accessoryEquipButton.interactable = GetHeroEquippedAccessoryRarity(heroIndex, slot) != rarity && GetAccessoryInventoryCount(slot, rarity) > 0;
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
        var heroIndex = GetSelectedHeroIndex();
        var rarity = GetHeroEquippedAccessoryRarity(heroIndex, slot);
        if (rarity < 0)
        {
            return "None";
        }

        var level = GetHeroEquippedAccessoryLevel(heroIndex, slot);
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
        var heroIndex = GetSelectedHeroIndex();
        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            for (var rarity = 0; rarity < AccessoryRarityCount; rarity++)
            {
                if (accessoryInventory[GetAccessoryInventoryIndex(slot, rarity)] > 0 && GetHeroEquippedAccessoryRarity(heroIndex, slot) != rarity)
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

    private void EnsureHeroEquipment()
    {
        var legacyWeaponLevel = Mathf.Max(StarterEquipmentLevel, weaponLevel);
        var legacyArmorLevel = Mathf.Max(StarterEquipmentLevel, armorLevel);
        if (heroWeaponLevels == null || heroWeaponLevels.Length != HeroCount)
        {
            heroWeaponLevels = CreateFilledIntArray(HeroCount, legacyWeaponLevel);
        }

        if (heroArmorLevels == null || heroArmorLevels.Length != HeroCount)
        {
            heroArmorLevels = CreateFilledIntArray(HeroCount, legacyArmorLevel);
        }

        for (var i = 0; i < HeroCount; i++)
        {
            heroWeaponLevels[i] = Mathf.Max(StarterEquipmentLevel, heroWeaponLevels[i] <= 0 ? legacyWeaponLevel : heroWeaponLevels[i]);
            heroArmorLevels[i] = Mathf.Max(StarterEquipmentLevel, heroArmorLevels[i] <= 0 ? legacyArmorLevel : heroArmorLevels[i]);
        }

        SyncSelectedHeroEquipmentMirrors();
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

        var needsHeroAccessoryMigration = heroEquippedAccessoryRarities == null || heroEquippedAccessoryRarities.Length != HeroCount * AccessorySlotCount;
        if (needsHeroAccessoryMigration)
        {
            var legacyRarities = CopyIntArray(equippedAccessoryRarities, AccessorySlotCount, -1);
            var legacyLevels = CopyIntArray(equippedAccessoryLevels, AccessorySlotCount, 0);
            heroEquippedAccessoryRarities = CreateFilledIntArray(HeroCount * AccessorySlotCount, -1);
            heroEquippedAccessoryLevels = new int[HeroCount * AccessorySlotCount];
            var heroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
            for (var slot = 0; slot < AccessorySlotCount; slot++)
            {
                var rarity = Mathf.Clamp(legacyRarities[slot], -1, AccessoryRarityCount - 1);
                if (rarity >= 0)
                {
                    var index = GetHeroAccessoryIndex(heroIndex, slot);
                    heroEquippedAccessoryRarities[index] = rarity;
                    heroEquippedAccessoryLevels[index] = Mathf.Clamp(legacyLevels[slot], 1, GetAccessoryMaxLevel(rarity));
                }
            }
        }

        if (heroEquippedAccessoryLevels == null || heroEquippedAccessoryLevels.Length != HeroCount * AccessorySlotCount)
        {
            heroEquippedAccessoryLevels = new int[HeroCount * AccessorySlotCount];
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

        for (var heroIndex = 0; heroIndex < HeroCount; heroIndex++)
        {
            for (var slot = 0; slot < AccessorySlotCount; slot++)
            {
                var index = GetHeroAccessoryIndex(heroIndex, slot);
                heroEquippedAccessoryRarities[index] = Mathf.Clamp(heroEquippedAccessoryRarities[index], -1, AccessoryRarityCount - 1);
                if (heroEquippedAccessoryRarities[index] < 0)
                {
                    heroEquippedAccessoryLevels[index] = 0;
                }
                else
                {
                    heroEquippedAccessoryLevels[index] = Mathf.Clamp(heroEquippedAccessoryLevels[index], 1, GetAccessoryMaxLevel(heroEquippedAccessoryRarities[index]));
                }
            }
        }

        SyncSelectedHeroAccessoryMirrors();
    }

    private static int[] CreateFilledIntArray(int length, int value)
    {
        var values = new int[Mathf.Max(0, length)];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = value;
        }

        return values;
    }

    private static int GetHeroAccessoryIndex(int heroIndex, int slot)
    {
        heroIndex = Mathf.Clamp(heroIndex, 0, HeroCount - 1);
        slot = Mathf.Clamp(slot, 0, AccessorySlotCount - 1);
        return (heroIndex * AccessorySlotCount) + slot;
    }

    private int GetSelectedHeroIndex()
    {
        return Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
    }

    private int GetHeroEquipmentLevel(int heroIndex, bool isWeapon)
    {
        EnsureHeroEquipment();
        heroIndex = Mathf.Clamp(heroIndex, 0, HeroCount - 1);
        return Mathf.Max(StarterEquipmentLevel, isWeapon ? heroWeaponLevels[heroIndex] : heroArmorLevels[heroIndex]);
    }

    private void SetHeroEquipmentLevel(int heroIndex, bool isWeapon, int level)
    {
        EnsureHeroEquipment();
        heroIndex = Mathf.Clamp(heroIndex, 0, HeroCount - 1);
        level = Mathf.Max(StarterEquipmentLevel, level);
        if (isWeapon)
        {
            heroWeaponLevels[heroIndex] = level;
        }
        else
        {
            heroArmorLevels[heroIndex] = level;
        }

        SyncSelectedHeroEquipmentMirrors();
    }

    private int GetHeroEquippedAccessoryRarity(int heroIndex, int slot)
    {
        EnsureAccessories();
        return heroEquippedAccessoryRarities[GetHeroAccessoryIndex(heroIndex, slot)];
    }

    private int GetHeroEquippedAccessoryLevel(int heroIndex, int slot)
    {
        EnsureAccessories();
        return heroEquippedAccessoryLevels[GetHeroAccessoryIndex(heroIndex, slot)];
    }

    private void SetHeroEquippedAccessory(int heroIndex, int slot, int rarity, int level)
    {
        EnsureAccessories();
        var index = GetHeroAccessoryIndex(heroIndex, slot);
        heroEquippedAccessoryRarities[index] = Mathf.Clamp(rarity, -1, AccessoryRarityCount - 1);
        heroEquippedAccessoryLevels[index] = heroEquippedAccessoryRarities[index] < 0
            ? 0
            : Mathf.Clamp(level, 1, GetAccessoryMaxLevel(heroEquippedAccessoryRarities[index]));
        SyncSelectedHeroAccessoryMirrors();
    }

    private void SyncSelectedHeroEquipmentMirrors()
    {
        var heroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        if (heroWeaponLevels != null && heroIndex < heroWeaponLevels.Length)
        {
            weaponLevel = Mathf.Max(StarterEquipmentLevel, heroWeaponLevels[heroIndex]);
        }

        if (heroArmorLevels != null && heroIndex < heroArmorLevels.Length)
        {
            armorLevel = Mathf.Max(StarterEquipmentLevel, heroArmorLevels[heroIndex]);
        }
    }

    private void SyncSelectedHeroAccessoryMirrors()
    {
        if (equippedAccessoryRarities == null || equippedAccessoryRarities.Length != AccessorySlotCount)
        {
            equippedAccessoryRarities = CreateFilledIntArray(AccessorySlotCount, -1);
        }

        if (equippedAccessoryLevels == null || equippedAccessoryLevels.Length != AccessorySlotCount)
        {
            equippedAccessoryLevels = new int[AccessorySlotCount];
        }

        if (heroEquippedAccessoryRarities == null || heroEquippedAccessoryRarities.Length != HeroCount * AccessorySlotCount)
        {
            return;
        }

        var heroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            var index = GetHeroAccessoryIndex(heroIndex, slot);
            equippedAccessoryRarities[slot] = heroEquippedAccessoryRarities[index];
            equippedAccessoryLevels[slot] = heroEquippedAccessoryLevels != null && index < heroEquippedAccessoryLevels.Length ? heroEquippedAccessoryLevels[index] : 0;
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

    private static string GetHeroTextureName(int index)
    {
        return GetHeroDefinition(index).heroId;
    }

    private static string GetCampaignEnemyTextureName(int stageNumber, int enemyIndex)
    {
        if (CampaignEnemyCombatTextureNames.Length == 0)
        {
            return "enemy_rat";
        }

        if (stageNumber % 10 == 0 && enemyIndex == 0)
        {
            return "enemy_dragon";
        }

        if (stageNumber % 5 == 0 && enemyIndex == 0)
        {
            return "enemy_golem";
        }

        var regularEnemyCount = Mathf.Max(1, CampaignEnemyCombatTextureNames.Length - 1);
        var textureIndex = Mathf.Abs(stageNumber + enemyIndex - 1) % regularEnemyCount;
        return CampaignEnemyCombatTextureNames[textureIndex];
    }

    private static string GetDungeonBossTextureName(string dungeonId)
    {
        if (dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return "enemy_dragon";
        }

        if (dungeonId == GearDungeonDefinition.dungeonId)
        {
            return "enemy_canine";
        }

        return "enemy_golem";
    }

    private static string GetDungeonBossName(string dungeonId)
    {
        if (dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return "Rift Dragon";
        }

        if (dungeonId == GearDungeonDefinition.dungeonId)
        {
            return "Iron Hound";
        }

        return "Treasure Golem";
    }

    private static float GetHeroFacingScale(int heroIndex)
    {
        return GetHeroTextureName(heroIndex) == "hero_dante" ? -1f : 1f;
    }

    private static bool IsRavikHero(int heroIndex)
    {
        return GetHeroTextureName(heroIndex) == "hero_ravik";
    }

    private static bool HasRavikSkeletalView(RavikSkeletalCombatView[] views, int index)
    {
        return views != null && index >= 0 && index < views.Length && views[index] != null;
    }

    private static float GetEnemyFacingScale(string enemyTextureName)
    {
        return enemyTextureName == "enemy_canine" ? 1f : -1f;
    }

    private static bool IsHeroRangedCombatant(int heroIndex)
    {
        var roleId = GetHeroDefinition(heroIndex).roleId;
        return roleId == MageRoleId || roleId == SupportRoleId;
    }

    private static bool IsEnemyRangedCombatant(string enemyTextureName)
    {
        return enemyTextureName == "enemy_dragon" || enemyTextureName == "enemy_bat";
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

    private SummonBannerDefinition GetActiveSummonBanner()
    {
        return GetSummonBanner(WrapSummonBannerIndex(selectedSummonBannerIndex));
    }

    private static SummonBannerDefinition GetSummonBanner(int bannerIndex)
    {
        if (LocalSummonBanners == null || LocalSummonBanners.Length == 0)
        {
            return HeroShardBanner;
        }

        return LocalSummonBanners[WrapSummonBannerIndex(bannerIndex)];
    }

    private static bool TryGetLocalSummonBanner(string bannerId, out SummonBannerDefinition banner)
    {
        if (LocalSummonBanners != null)
        {
            for (var i = 0; i < LocalSummonBanners.Length; i++)
            {
                if (LocalSummonBanners[i].bannerId == bannerId)
                {
                    banner = LocalSummonBanners[i];
                    return true;
                }
            }
        }

        banner = default(SummonBannerDefinition);
        return false;
    }

    private static int WrapSummonBannerIndex(int bannerIndex)
    {
        var count = LocalSummonBanners == null || LocalSummonBanners.Length == 0 ? 1 : LocalSummonBanners.Length;
        if (count <= 1)
        {
            return 0;
        }

        return ((bannerIndex % count) + count) % count;
    }

    private static int GetSummonBannerFeaturedHeroIndex(SummonBannerDefinition banner, int slotIndex)
    {
        if (banner.featuredHeroIndexes != null && banner.featuredHeroIndexes.Length > 0)
        {
            return Mathf.Clamp(banner.featuredHeroIndexes[Mathf.Abs(slotIndex) % banner.featuredHeroIndexes.Length], 0, HeroCount - 1);
        }

        return 0;
    }

    private int GetSummonCost()
    {
        return GetSummonCost(GetActiveSummonBanner());
    }

    private int GetSummonCost(SummonBannerDefinition banner)
    {
        if (TryGetBackendSummonBannerDefinition(banner.bannerId, out var backendBanner))
        {
            return Mathf.Max(0, backendBanner.costAmount);
        }

        return Mathf.Max(0, banner.costAmount);
    }

    private string GetSummonBannerDisplayName()
    {
        var banner = GetActiveSummonBanner();
        if (TryGetBackendSummonBannerDefinition(banner.bannerId, out var backendBanner) && !string.IsNullOrWhiteSpace(backendBanner.displayName))
        {
            return backendBanner.displayName;
        }

        return banner.displayName;
    }

    private int GetSummonPackCost(int count)
    {
        return GetSummonPackCost(count, GetActiveSummonBanner());
    }

    private int GetSummonPackCost(int count, SummonBannerDefinition banner)
    {
        count = Mathf.Clamp(count, 1, MaxSummonPullCount);
        var singleCost = GetSummonCost(banner);
        if (count >= 10)
        {
            return Mathf.RoundToInt(singleCost * count * 0.9f);
        }

        return singleCost * count;
    }

    private static string BuildSummonPackResultMessage(int count, int totalShards, int[] shardTotals)
    {
        var message = $"Summon x{count} complete\n+{totalShards} total shards";
        if (shardTotals == null)
        {
            return message;
        }

        var shown = 0;
        for (var i = 0; i < Mathf.Min(shardTotals.Length, HeroDefinitions.Length); i++)
        {
            if (shardTotals[i] <= 0)
            {
                continue;
            }

            message += shown == 0 ? "\n" : "  ";
            message += $"{GetHeroDefinition(i).name} +{shardTotals[i]}";
            shown++;
        }

        return message;
    }

    private string GetSummonRatesText()
    {
        var banner = GetActiveSummonBanner();
        if (TryGetBackendSummonBannerDefinition(banner.bannerId, out var backendBanner))
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

        if (banner.rates == null || banner.rates.Length == 0)
        {
            return text;
        }

        for (var i = banner.rates.Length - 1; i >= 0; i--)
        {
            var lowerBound = i > 0 ? banner.rates[i - 1].cumulativeChance : 0;
            var chance = Mathf.Max(0, banner.rates[i].cumulativeChance - lowerBound);
            text += i == banner.rates.Length - 1 ? "\n" : "  ";
            text += $"{GetHeroRarityName(banner.rates[i].rarityId)} {chance}%";
        }

        return text;
    }

    private static int GetDungeonEnemyHp(DungeonDefinition dungeon, int floor)
    {
        return Mathf.Max(1, Mathf.FloorToInt(GetScaledDefinitionValue(dungeon.baseEnemyHp, dungeon.enemyHpScale, dungeon.enemyHpGrowth, floor) * DungeonBossHpMultiplier));
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
        return Mathf.Max(1, Mathf.FloorToInt((baseHp + (requiredPower * hpPerPower) + (floor * hpPerFloor)) * DungeonBossHpMultiplier));
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

    private DungeonDefinition ResolveDungeonDefinition(string dungeonId)
    {
        if (dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return EssenceDungeonDefinition;
        }

        if (dungeonId == GearDungeonDefinition.dungeonId)
        {
            return GearDungeonDefinition;
        }

        return GoldDungeonDefinition;
    }

    private int GetDungeonFloor(string dungeonId)
    {
        if (dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return Mathf.Max(1, essenceDungeonFloor);
        }

        if (dungeonId == GearDungeonDefinition.dungeonId)
        {
            return Mathf.Max(1, gearDungeonFloor);
        }

        return Mathf.Max(1, goldDungeonFloor);
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

        return Mathf.Max(1, power);
    }

    private int GetTeamDamage()
    {
        if (backendGameplayEnabled && backendTeamAttack > 0)
        {
            return backendTeamAttack;
        }

        var damageTotal = 0;
        for (var i = 0; i < HeroCount; i++)
        {
            damageTotal += GetHeroEffectiveAttack(i);
        }

        return Mathf.Max(1, damageTotal);
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
            health += GetHeroCombatMaxHealth(i);
        }

        return Mathf.Max(1, health);
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

    private int GetHeroEffectiveAttack(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        return Mathf.Max(1, Mathf.RoundToInt((GetHeroAttack(index) + GetHeroGearAttackBonus(index)) * GetTeamRoleDamageMultiplier()));
    }

    private int GetHeroPower(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        return GetHeroEffectiveAttack(index)
            + Mathf.FloorToInt(GetHeroCombatMaxHealth(index) / 8f)
            + GetHeroDefense(index)
            + Mathf.FloorToInt(GetHeroCritChancePercent(index) * 1.6f)
            + Mathf.FloorToInt((GetHeroAccuracyPercent(index) - 80) * 1.2f);
    }

    private int GetEquipmentPower()
    {
        var power = 0;
        for (var i = 0; i < HeroCount; i++)
        {
            power += GetHeroEquipmentAttackBonus(i) + Mathf.FloorToInt(GetHeroEquipmentHealthBonus(i) / 8f);
        }

        return power;
    }

    private int GetAccessoryPower()
    {
        var power = 0;
        for (var i = 0; i < HeroCount; i++)
        {
            power += GetHeroAccessoryAttackBonus(i) + Mathf.FloorToInt(GetHeroAccessoryHealthBonus(i) / 8f);
        }

        return power;
    }

    private int GetEquipmentAttackBonus()
    {
        return GetHeroEquipmentAttackBonus(GetSelectedHeroIndex());
    }

    private int GetEquipmentHealthBonus()
    {
        return GetHeroEquipmentHealthBonus(GetSelectedHeroIndex());
    }

    private int GetHeroEquipmentAttackBonus(int heroIndex)
    {
        if (TryGetBackendEquipmentDefinition(WeaponTrack.equipmentId, out var definition))
        {
            var level = Mathf.Clamp(GetEquipmentDisplayLevel(WeaponTrack, GetHeroEquipmentLevel(heroIndex, isWeapon: true)), 0, Mathf.Max(1, definition.maxLevel));
            return Mathf.Max(0, definition.attackPerLevel) * level;
        }

        return GetEquipmentBonus(WeaponTrack, GetHeroEquipmentLevel(heroIndex, isWeapon: true));
    }

    private int GetHeroEquipmentHealthBonus(int heroIndex)
    {
        if (TryGetBackendEquipmentDefinition(ArmorTrack.equipmentId, out var definition))
        {
            var level = Mathf.Clamp(GetEquipmentDisplayLevel(ArmorTrack, GetHeroEquipmentLevel(heroIndex, isWeapon: false)), 0, Mathf.Max(1, definition.maxLevel));
            return Mathf.Max(0, definition.healthPerLevel) * level;
        }

        return GetEquipmentBonus(ArmorTrack, GetHeroEquipmentLevel(heroIndex, isWeapon: false));
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
        return GetHeroAccessoryAttackBonus(GetSelectedHeroIndex());
    }

    private int GetAccessoryHealthBonus()
    {
        return GetHeroAccessoryHealthBonus(GetSelectedHeroIndex());
    }

    private int GetHeroAccessoryAttackBonus(int heroIndex)
    {
        EnsureAccessories();
        var attack = 0;

        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            attack += GetAccessoryAttackFor(slot, GetHeroEquippedAccessoryRarity(heroIndex, slot), GetHeroEquippedAccessoryLevel(heroIndex, slot));
        }

        return attack;
    }

    private int GetHeroAccessoryHealthBonus(int heroIndex)
    {
        EnsureAccessories();
        var health = 0;

        for (var slot = 0; slot < AccessorySlotCount; slot++)
        {
            health += GetAccessoryHealthFor(slot, GetHeroEquippedAccessoryRarity(heroIndex, slot), GetHeroEquippedAccessoryLevel(heroIndex, slot));
        }

        return health;
    }

    private int GetHeroGearAttackBonus(int heroIndex)
    {
        return GetHeroEquipmentAttackBonus(heroIndex) + GetHeroAccessoryAttackBonus(heroIndex);
    }

    private int GetHeroGearHealthBonus(int heroIndex)
    {
        return GetHeroEquipmentHealthBonus(heroIndex) + GetHeroAccessoryHealthBonus(heroIndex);
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

    private int GetHeroCritChancePercent(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroAscensions();
        var hero = GetHeroDefinition(index);
        return Mathf.Clamp(hero.critChancePercent + Mathf.FloorToInt(heroAscensions[index] * 0.6f), 0, 75);
    }

    private int GetHeroAccuracyPercent(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroAscensions();
        var hero = GetHeroDefinition(index);
        return Mathf.Clamp(hero.accuracyPercent + Mathf.FloorToInt(heroAscensions[index] * 0.35f), 50, 100);
    }

    private int GetHeroDefense(int index)
    {
        index = Mathf.Clamp(index, 0, HeroCount - 1);
        EnsureHeroLevels();
        EnsureHeroAscensions();
        var hero = GetHeroDefinition(index);
        return Mathf.Max(0, hero.defense + Mathf.FloorToInt(heroLevels[index] * 0.45f) + (heroAscensions[index] * 3));
    }

    private int GetTeamDefense()
    {
        var defense = 0;
        for (var i = 0; i < HeroCount; i++)
        {
            defense += GetHeroDefense(i) + Mathf.FloorToInt(GetHeroGearHealthBonus(i) / 95f);
        }

        return defense;
    }

    private int GetTeamCritChancePercent()
    {
        var totalAttack = 0f;
        var weightedCrit = 0f;
        for (var i = 0; i < HeroCount; i++)
        {
            var attack = Mathf.Max(1, GetHeroEffectiveAttack(i));
            totalAttack += attack;
            weightedCrit += attack * GetHeroCritChancePercent(i);
        }

        return Mathf.Clamp(Mathf.RoundToInt(weightedCrit / Mathf.Max(1f, totalAttack)), 0, 100);
    }

    private int GetTeamAccuracyPercent()
    {
        var totalAttack = 0f;
        var weightedAccuracy = 0f;
        for (var i = 0; i < HeroCount; i++)
        {
            var attack = Mathf.Max(1, GetHeroEffectiveAttack(i));
            totalAttack += attack;
            weightedAccuracy += attack * GetHeroAccuracyPercent(i);
        }

        return Mathf.Clamp(Mathf.RoundToInt(weightedAccuracy / Mathf.Max(1f, totalAttack)), 0, 100);
    }

    private int GetTeamCombatDamagePerSecond()
    {
        var baseDamage = GetTeamDamage();
        var accuracyRate = GetTeamAccuracyPercent() / 100f;
        var critRate = GetTeamCritChancePercent() / 100f;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * accuracyRate * (1f + (critRate * (CritDamageMultiplier - 1f)))));
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
        return GetEquipmentUpgradeCost(WeaponTrack, GetHeroEquipmentLevel(GetSelectedHeroIndex(), isWeapon: true));
    }

    private int GetArmorUpgradeCost()
    {
        return GetEquipmentUpgradeCost(ArmorTrack, GetHeroEquipmentLevel(GetSelectedHeroIndex(), isWeapon: false));
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

    private float GetTeamRoleDamageMultiplier()
    {
        return 1f
            + (CountHeroesWithRole(WarriorRoleId) * WarriorDamageBonusRate)
            + (CountHeroesWithRole(MageRoleId) * MageDamageBonusRate);
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
        var afterGuard = enemyDamage * (1f - GetTankDamageReductionRate());
        var defense = GetTeamDefense();
        var defenseReduction = defense / Mathf.Max(1f, defense + (enemyDamage * 8f));
        defenseReduction = Mathf.Clamp(defenseReduction, 0f, 0.45f);
        return Mathf.Max(1, Mathf.CeilToInt(afterGuard * (1f - defenseReduction)));
    }

    private int GetMitigatedEnemyDamageAgainstHero(int enemyDamage, int heroIndex)
    {
        heroIndex = Mathf.Clamp(heroIndex, 0, HeroCount - 1);
        enemyDamage = Mathf.Max(1, enemyDamage);
        var afterGuard = enemyDamage * (1f - GetTankDamageReductionRate());
        var gearDefense = Mathf.FloorToInt(GetHeroGearHealthBonus(heroIndex) / 95f);
        var defense = Mathf.Max(0, GetHeroDefense(heroIndex) + gearDefense);
        var defenseReduction = defense / Mathf.Max(1f, defense + (enemyDamage * 8f));
        defenseReduction = Mathf.Clamp(defenseReduction, 0f, 0.45f);
        return Mathf.Max(1, Mathf.CeilToInt(afterGuard * (1f - defenseReduction)));
    }

    private static bool RollPercentChance(int chancePercent)
    {
        return UnityEngine.Random.Range(0, 100) < Mathf.Clamp(chancePercent, 0, 100);
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
        RefreshDungeonCardUi(GoldDungeonDefinition, goldDungeonFloor, goldDungeonTitleText, goldDungeonProgressText, goldDungeonText);
        RefreshDungeonCardUi(EssenceDungeonDefinition, essenceDungeonFloor, essenceDungeonTitleText, essenceDungeonProgressText, essenceDungeonText);
        RefreshDungeonCardUi(GearDungeonDefinition, gearDungeonFloor, gearDungeonTitleText, gearDungeonProgressText, gearDungeonText);

        if (dungeonResultText != null && string.IsNullOrWhiteSpace(dungeonResultText.text))
        {
            dungeonResultText.text = "Dungeons are the active resource source.";
        }
    }

    private void RefreshDungeonCardUi(DungeonDefinition dungeon, int floor, TMP_Text titleText, TMP_Text progressText, TMP_Text detailText)
    {
        floor = Mathf.Max(1, floor);
        if (titleText != null)
        {
            titleText.text = GetDungeonSetTitle(dungeon.dungeonId);
        }

        if (progressText != null)
        {
            var setProgress = Mathf.Max(0, floor - 1) % DungeonSetProgressGoal;
            progressText.text = $"Progress: <color=#F8E85A>{setProgress}/{DungeonSetProgressGoal}</color>   Floor {floor}";
        }

        if (detailText != null)
        {
            detailText.text = FormatDungeonCardMeta(dungeon, floor);
        }
    }

    private string FormatDungeonCardMeta(DungeonDefinition localDefinition, int floor)
    {
        var bossName = GetDungeonBossName(localDefinition.dungeonId);
        if (UseBackendDefinitionView() && TryGetBackendDungeonDefinition(localDefinition.dungeonId, out var backendDefinition))
        {
            var requiredPower = FormatCompactNumber(GetBackendDungeonRequiredPower(backendDefinition, floor));
            if (string.IsNullOrWhiteSpace(backendDefinition.rewardCurrencyId))
            {
                return $"{bossName}  |  Rec {requiredPower}  |  Accessory drop";
            }

            var rewardAmount = FormatCompactNumber(GetBackendDungeonRewardAmount(backendDefinition, floor));
            var currencyName = GetCurrencyDefinition(backendDefinition.rewardCurrencyId).displayName;
            return $"{bossName}  |  Rec {requiredPower}  |  +{rewardAmount} {currencyName}";
        }

        var localRequiredPower = FormatCompactNumber(GetDungeonRecommendedPower(localDefinition, floor));
        if (localDefinition.dungeonId == GoldDungeonDefinition.dungeonId)
        {
            return $"{bossName}  |  Rec {localRequiredPower}  |  +{FormatCompactNumber(GetGoldDungeonReward(floor))} {GetCurrencyDefinition(localDefinition.rewardCurrencyId).displayName}";
        }

        if (localDefinition.dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return $"{bossName}  |  Rec {localRequiredPower}  |  +{FormatCompactNumber(GetEssenceDungeonReward(floor))} {GetCurrencyDefinition(localDefinition.rewardCurrencyId).displayName}";
        }

        return $"{bossName}  |  Rec {localRequiredPower}  |  Accessory drop";
    }

    private static string GetDungeonSetTitle(string dungeonId)
    {
        if (dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return "Rift Essence Set";
        }

        if (dungeonId == GearDungeonDefinition.dungeonId)
        {
            return "Iron Armory Set";
        }

        return "Gold Treasury Set";
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
                return $"{backendDefinition.displayName} F{floor}  Rec {requiredPower}\n{GetDungeonBossName(localDefinition.dungeonId)} HP {enemyHp}  DMG {enemyDamage}  Formation";
            }

            var rewardAmount = GetBackendDungeonRewardAmount(backendDefinition, floor);
            var currencyName = GetCurrencyDefinition(backendDefinition.rewardCurrencyId).displayName;
            return $"{backendDefinition.displayName} F{floor}  Rec {requiredPower}\n{GetDungeonBossName(localDefinition.dungeonId)} HP {enemyHp}  +{rewardAmount} {currencyName}";
        }

        if (localDefinition.dungeonId == GoldDungeonDefinition.dungeonId)
        {
            return $"{localDefinition.displayName} F{floor}  Rec {GetDungeonRecommendedPower(floor)}\nBoss HP {FormatCompactNumber(GetDungeonEnemyHp(localDefinition, floor))}  +{GetGoldDungeonReward(floor)} {GetCurrencyDefinition(localDefinition.rewardCurrencyId).displayName}";
        }

        if (localDefinition.dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return $"{localDefinition.displayName} F{floor}  Rec {GetDungeonRecommendedPower(floor)}\nBoss HP {FormatCompactNumber(GetDungeonEnemyHp(localDefinition, floor))}  +{GetEssenceDungeonReward(floor)} {GetCurrencyDefinition(localDefinition.rewardCurrencyId).displayName}";
        }

        return $"{localDefinition.displayName} F{floor}  Rec {GetGearDungeonRecommendedPower(floor)}\nBoss HP {FormatCompactNumber(GetDungeonEnemyHp(localDefinition, floor))}  Accessory drop";
    }

    private string FormatDungeonFormationRewardLine(DungeonDefinition dungeon, int floor)
    {
        if (UseBackendDefinitionView() && TryGetBackendDungeonDefinition(dungeon.dungeonId, out var backendDefinition))
        {
            if (string.IsNullOrWhiteSpace(backendDefinition.rewardCurrencyId))
            {
                return "Reward: random accessory drop";
            }

            return $"Reward: +{GetBackendDungeonRewardAmount(backendDefinition, floor)} {GetCurrencyDefinition(backendDefinition.rewardCurrencyId).displayName}";
        }

        if (dungeon.dungeonId == GoldDungeonDefinition.dungeonId)
        {
            return $"Reward: +{GetGoldDungeonReward(floor)} {GetCurrencyDefinition(dungeon.rewardCurrencyId).displayName}";
        }

        if (dungeon.dungeonId == EssenceDungeonDefinition.dungeonId)
        {
            return $"Reward: +{GetEssenceDungeonReward(floor)} {GetCurrencyDefinition(dungeon.rewardCurrencyId).displayName}";
        }

        return "Reward: random accessory drop";
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

        var pendingGold = CalculateAfkGoldReward(afkRewardStoredSeconds);
        var pendingEssence = CalculateAfkEssenceReward(afkRewardStoredSeconds);
        offlineRewardText.text =
            $"Fast Rewards: +{FormatCompactNumber(pendingGold)} Gold, +{FormatCompactNumber(pendingEssence)} Essence " +
            $"({FormatDuration(Mathf.FloorToInt(afkRewardStoredSeconds))}/{FormatDuration(GetAfkRewardMaxSeconds())})";
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
        var activeBanner = GetActiveSummonBanner();
        if (summonCostText != null)
        {
            summonCostText.text = $"Cost: {GetSummonCost()} Gems";
        }

        if (summonRatesText != null)
        {
            summonRatesText.text = GetSummonRatesText();
            summonRatesText.fontSize = 20;
            summonRatesText.fontSizeMin = 16;
            summonRatesText.fontSizeMax = 20;
            summonRatesText.enableAutoSizing = true;
            summonRatesText.fontStyle = FontStyles.Bold;
            summonRatesText.color = new Color(1f, 0.92f, 0.55f);
            summonRatesText.alignment = TextAlignmentOptions.Center;
        }

        if (summonCountText != null)
        {
            summonCountText.text = $"Summons {summonCount}";
            summonCountText.fontSize = 18;
            summonCountText.fontSizeMin = 14;
            summonCountText.fontSizeMax = 18;
            summonCountText.enableAutoSizing = true;
            summonCountText.fontStyle = FontStyles.Bold;
            summonCountText.color = new Color(0.75f, 0.92f, 1f);
            summonCountText.alignment = TextAlignmentOptions.Left;
            summonCountText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (summonResultText != null && string.IsNullOrWhiteSpace(summonResultText.text))
        {
            summonResultText.text = "Summon heroes to collect shards and raise team power.";
        }

        if (summonResultText != null)
        {
            summonResultText.fontSize = 22;
            summonResultText.fontSizeMin = 17;
            summonResultText.fontSizeMax = 22;
            summonResultText.enableAutoSizing = true;
            summonResultText.fontStyle = FontStyles.Bold;
            summonResultText.color = new Color(0.72f, 0.86f, 1f);
            summonResultText.alignment = TextAlignmentOptions.Center;
        }

        if (summonOfferTitleText != null)
        {
            summonOfferTitleText.text = GetSummonBannerDisplayName();
        }

        if (summonOfferPromoText != null)
        {
            summonOfferPromoText.text = string.IsNullOrWhiteSpace(activeBanner.promoText)
                ? "Featured hero shard rotation"
                : activeBanner.promoText;
        }

        RefreshSummonOfferHeroes(activeBanner);
        RefreshSummonCarousel();

        SetButtonLabel(summonButton, "Summon");
        SetButtonLabel(summonTenButton, "Summon 10");

        if (summonSingleCostText != null)
        {
            summonSingleCostText.text = GetSummonPackCost(1).ToString();
        }

        if (summonTenCostText != null)
        {
            summonTenCostText.text = GetSummonPackCost(10).ToString();
        }

        if (summonResultTenCostText != null)
        {
            summonResultTenCostText.text = GetSummonPackCost(SummonAutoStepCount, activeBanner).ToString();
        }

        if (summonResultMaxCostText != null)
        {
            summonResultMaxCostText.text = GetSummonPackCost(MaxSummonPullCount, activeBanner).ToString();
        }

        RefreshSummonAutoToggle();
    }

    private void RefreshSummonOfferHeroes(SummonBannerDefinition banner)
    {
        if (summonOfferHeroImages == null)
        {
            return;
        }

        for (var i = 0; i < summonOfferHeroImages.Length; i++)
        {
            var image = summonOfferHeroImages[i];
            if (image == null)
            {
                continue;
            }

            var heroIndex = GetSummonBannerFeaturedHeroIndex(banner, i);
            image.texture = LoadRuntimeTexture($"hero_{GetHeroDefinition(heroIndex).name.ToLowerInvariant()}");
            image.rectTransform.localScale = new Vector3(GetHeroFacingScale(heroIndex), 1f, 1f);
            image.color = Color.white;
        }
    }

    private void RefreshSummonCarousel()
    {
        if (summonCarouselButtons == null)
        {
            return;
        }

        if (summonCarouselCardBannerIndices == null || summonCarouselCardBannerIndices.Length != summonCarouselButtons.Length)
        {
            summonCarouselCardBannerIndices = new int[summonCarouselButtons.Length];
        }

        for (var cardIndex = 0; cardIndex < summonCarouselButtons.Length; cardIndex++)
        {
            var bannerIndex = WrapSummonBannerIndex(selectedSummonBannerIndex + cardIndex - 1);
            summonCarouselCardBannerIndices[cardIndex] = bannerIndex;
            var banner = GetSummonBanner(bannerIndex);
            var isSelected = bannerIndex == WrapSummonBannerIndex(selectedSummonBannerIndex);

            if (summonCarouselFrames != null && cardIndex < summonCarouselFrames.Length && summonCarouselFrames[cardIndex] != null)
            {
                summonCarouselFrames[cardIndex].color = isSelected
                    ? new Color(0.95f, 0.67f, 0.3f, 0.98f)
                    : new Color(0.12f, 0.075f, 0.045f, 0.94f);
            }

            if (summonCarouselTitleTexts != null && cardIndex < summonCarouselTitleTexts.Length && summonCarouselTitleTexts[cardIndex] != null)
            {
                summonCarouselTitleTexts[cardIndex].text = banner.displayName;
                summonCarouselTitleTexts[cardIndex].color = isSelected ? new Color(1f, 0.93f, 0.58f) : new Color(0.88f, 0.78f, 0.62f);
            }

            if (summonCarouselRateTexts != null && cardIndex < summonCarouselRateTexts.Length && summonCarouselRateTexts[cardIndex] != null)
            {
                summonCarouselRateTexts[cardIndex].text = GetSummonBannerLeadRateText(banner);
                summonCarouselRateTexts[cardIndex].color = isSelected ? new Color(0.74f, 1f, 0.95f) : new Color(0.66f, 0.86f, 0.82f);
            }

            if (summonCarouselHeroImages == null)
            {
                continue;
            }

            for (var heroSlot = 0; heroSlot < SummonCarouselHeroSlotsPerCard; heroSlot++)
            {
                var imageIndex = cardIndex * SummonCarouselHeroSlotsPerCard + heroSlot;
                if (imageIndex < 0 || imageIndex >= summonCarouselHeroImages.Length || summonCarouselHeroImages[imageIndex] == null)
                {
                    continue;
                }

                var heroIndex = GetSummonBannerFeaturedHeroIndex(banner, heroSlot);
                summonCarouselHeroImages[imageIndex].texture = LoadRuntimeTexture($"hero_{GetHeroDefinition(heroIndex).name.ToLowerInvariant()}");
                summonCarouselHeroImages[imageIndex].rectTransform.localScale = new Vector3(GetHeroFacingScale(heroIndex), 1f, 1f);
                summonCarouselHeroImages[imageIndex].color = isSelected ? Color.white : new Color(0.86f, 0.86f, 0.86f, 0.92f);
            }
        }
    }

    private static string GetSummonBannerLeadRateText(SummonBannerDefinition banner)
    {
        if (banner.rates == null || banner.rates.Length == 0)
        {
            return "Rates";
        }

        var bestRateIndex = Mathf.Clamp(banner.rates.Length - 1, 0, banner.rates.Length - 1);
        for (var i = 0; i < banner.rates.Length; i++)
        {
            if (banner.rates[i].rarityId == LegendaryRarityId)
            {
                bestRateIndex = i;
                break;
            }
        }

        var lowerBound = bestRateIndex > 0 ? banner.rates[bestRateIndex - 1].cumulativeChance : 0;
        var chance = Mathf.Max(0, banner.rates[bestRateIndex].cumulativeChance - lowerBound);
        return $"{GetHeroRarityName(banner.rates[bestRateIndex].rarityId)} {chance}%";
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

    private int RollSummonHero(SummonBannerDefinition banner)
    {
        var roll = UnityEngine.Random.Range(0, 100);

        if (banner.rates != null)
        {
            for (var i = 0; i < banner.rates.Length; i++)
            {
                if (roll < banner.rates[i].cumulativeChance)
                {
                    return PickRandomHero(banner.rates[i].heroIndexes);
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
        return 1;
    }

    private void ShowSummonResultPopup(int[] drawCounts, int pullCount)
    {
        EnsureRuntimeSummonResultPopup();
        if (summonResultPopupRoot == null)
        {
            return;
        }

        summonResultPopupRoot.gameObject.SetActive(true);
        summonResultPopupRoot.SetAsLastSibling();
        if (summonResultPopupTitleText != null)
        {
            summonResultPopupTitleText.text = pullCount <= 1 ? "Summon Result" : $"Summon x{pullCount} Result";
        }

        var slotIndex = 0;
        for (var heroIndex = 0; heroIndex < HeroDefinitions.Length && slotIndex < HeroCount; heroIndex++)
        {
            var drawCount = drawCounts != null && heroIndex < drawCounts.Length ? drawCounts[heroIndex] : 0;
            if (drawCount <= 0)
            {
                continue;
            }

            var hero = GetHeroDefinition(heroIndex);
            if (summonResultHeroFrames != null && slotIndex < summonResultHeroFrames.Length && summonResultHeroFrames[slotIndex] != null)
            {
                var frame = summonResultHeroFrames[slotIndex];
                frame.gameObject.SetActive(true);
                frame.color = GetHeroRarityColor(hero.rarityId);
            }

            if (summonResultHeroImages != null && slotIndex < summonResultHeroImages.Length && summonResultHeroImages[slotIndex] != null)
            {
                summonResultHeroImages[slotIndex].texture = LoadCombatTexture(GetHeroTextureName(heroIndex), "idle", 0, GetHeroTextureName(heroIndex));
                summonResultHeroImages[slotIndex].rectTransform.localScale = new Vector3(GetHeroFacingScale(heroIndex), 1f, 1f);
            }

            if (summonResultHeroNameTexts != null && slotIndex < summonResultHeroNameTexts.Length && summonResultHeroNameTexts[slotIndex] != null)
            {
                summonResultHeroNameTexts[slotIndex].text = hero.name;
                summonResultHeroNameTexts[slotIndex].color = new Color(1f, 0.95f, 0.78f);
            }

            if (summonResultHeroCountTexts != null && slotIndex < summonResultHeroCountTexts.Length && summonResultHeroCountTexts[slotIndex] != null)
            {
                summonResultHeroCountTexts[slotIndex].text = $"x{drawCount}";
            }

            slotIndex++;
        }

        for (var i = slotIndex; i < HeroCount; i++)
        {
            if (summonResultHeroFrames != null && i < summonResultHeroFrames.Length && summonResultHeroFrames[i] != null)
            {
                summonResultHeroFrames[i].gameObject.SetActive(false);
            }
        }

        RefreshSummonAutoToggle();
        RefreshUi();
    }

    private void ShowBackendSummonResultPopup(string message, int pullCount)
    {
        var drawCounts = new int[HeroDefinitions.Length];
        var anyCount = false;
        for (var heroIndex = 0; heroIndex < HeroDefinitions.Length; heroIndex++)
        {
            var heroId = GetHeroDefinition(heroIndex).heroId;
            var count = ExtractBackendSummonCount(message, heroId);
            if (count <= 0 && pullCount == 1 && !string.IsNullOrWhiteSpace(message) && message.Contains(heroId))
            {
                count = 1;
            }

            if (count <= 0)
            {
                continue;
            }

            drawCounts[heroIndex] = count;
            anyCount = true;
        }

        if (!anyCount)
        {
            return;
        }

        ShowSummonResultPopup(drawCounts, Mathf.Clamp(pullCount, 1, MaxSummonPullCount));
    }

    private static int ExtractBackendSummonCount(string message, string heroId)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(heroId))
        {
            return 0;
        }

        var token = $"{heroId} +";
        var index = message.IndexOf(token, StringComparison.Ordinal);
        if (index < 0)
        {
            return 0;
        }

        index += token.Length;
        var count = 0;
        while (index < message.Length && char.IsDigit(message[index]))
        {
            count = (count * 10) + (message[index] - '0');
            index++;
        }

        return count;
    }

    private void HideSummonResultPopup()
    {
        if (summonResultPopupRoot != null)
        {
            summonResultPopupRoot.gameObject.SetActive(false);
        }
    }

    private void RefreshSummonAutoToggle()
    {
        if (summonAutoCheckboxImage != null)
        {
            summonAutoCheckboxImage.color = summonAutoEnabled
                ? new Color(1f, 0.78f, 0.18f, 0.98f)
                : new Color(0.04f, 0.025f, 0.02f, 0.92f);
        }

        if (summonAutoCheckboxMarkText != null)
        {
            summonAutoCheckboxMarkText.text = summonAutoEnabled ? "X" : string.Empty;
        }

        if (summonAutoToggleText != null)
        {
            summonAutoToggleText.text = summonAutoRunning
                ? $"Auto-Summon {summonAutoRemainingPulls}"
                : "Auto-Summon";
        }
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
            PlayerPrefs.DeleteKey($"{HeroWeaponLevelKeyPrefix}{i}");
            PlayerPrefs.DeleteKey($"{HeroArmorLevelKeyPrefix}{i}");
        }

        for (var i = 0; i < HeroCount * AccessorySlotCount; i++)
        {
            PlayerPrefs.DeleteKey($"{HeroEquippedAccessoryRarityKeyPrefix}{i}");
            PlayerPrefs.DeleteKey($"{HeroEquippedAccessoryLevelKeyPrefix}{i}");
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

    private string GetPlayerDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(playerDisplayName))
        {
            return playerDisplayName;
        }

        if (backendClient != null && backendClient.HasSession && !string.IsNullOrWhiteSpace(backendClient.PlayerId))
        {
            return backendClient.PlayerId;
        }

        return "Player";
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
        EnsureRuntimeSummonOffer();
        EnsureRuntimeHomeActions();
        EnsureRuntimeCampaignMap();
        EnsureRuntimeHomePopups();
        EnsureRuntimeBattleFlowUi();
        EnsureRuntimeMenuHeader();
        EnsureRuntimeHeroCardArt();
        EnsureRuntimeHeroEssenceCounter();
        EnsureRuntimeHeroesTabs();
        EnsureRuntimeHeroDetailWindow();
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
        SetRuntimeRect(topBarRoot, new Vector2(0, 0), new Vector2(1080, 176), new Vector2(0.5f, 1f));
        topBarRoot.SetAsLastSibling();

        var topBarImage = topBarObject.GetComponent<Image>();
        topBarImage.color = Color.clear;
        topBarImage.raycastTarget = false;

        var generatedTopbar = GetHomeGeneratedTexture("home_topbar_frame");
        if (generatedTopbar != null)
        {
            topbarFrameImage = CreateRuntimeRawImage(topBarObject.transform, "Generated Topbar Frame", generatedTopbar, Vector2.zero, new Vector2(1080, 176), new Vector2(0.5f, 1f));
            topGemAmountText = CreateTopbarAmountText(topBarObject.transform, "Top Mythic Gem Amount", new Vector2(50, -36), new Vector2(160, 42));
            topGoldAmountText = CreateTopbarAmountText(topBarObject.transform, "Top Gold Amount", new Vector2(338, -36), new Vector2(205, 42));
        }
        else
        {
            topGemAmountText = CreateTopResourceCounter(topBarObject.transform, "Top Mythic Gem Counter", GetCurrencyIconTexture("mythic_gem"), new Vector2(120, -58), new Vector2(220, 54), out topGemIconImage);
            topGoldAmountText = CreateTopResourceCounter(topBarObject.transform, "Top Gold Counter", GetCurrencyIconTexture("gold_coin"), new Vector2(380, -58), new Vector2(285, 54), out topGoldIconImage);
        }

        topPlayerNameText = CreateRuntimeText(topBarObject.transform, "Top Player Name", GetPlayerDisplayName(), 30, new Vector2(-180, -18), new Vector2(250, 42));
        topPlayerNameText.alignment = TextAlignmentOptions.Left;
        topPlayerNameText.fontStyle = FontStyles.Bold;
        topPlayerNameText.textWrappingMode = TextWrappingModes.NoWrap;
        topPlayerNameText.enableAutoSizing = true;
        topPlayerNameText.fontSizeMin = 20;
        topPlayerNameText.fontSizeMax = 30;

        topPowerIconImage = CreateRuntimeRawImage(topBarObject.transform, "Top Combat Power Icon", GetHomeGeneratedTexture("home_power_icon"), new Vector2(-320, -92), new Vector2(42, 36), new Vector2(0.5f, 1f));
        topPowerText = CreateRuntimeText(topBarObject.transform, "Top Combat Power", "0", 23, new Vector2(-174, -88), new Vector2(220, 34));
        topPowerText.alignment = TextAlignmentOptions.Left;
        topPowerText.fontStyle = FontStyles.Bold;
        topPowerText.textWrappingMode = TextWrappingModes.NoWrap;
        topPowerText.enableAutoSizing = true;
        topPowerText.fontSizeMin = 16;
        topPowerText.fontSizeMax = 23;
        topPowerText.color = Color.white;

        topGemPlusButton = CreateRuntimeButton(topBarObject.transform, "Top Gem Shop Plus Button", "+", 174, -24, 44, 44);
        var plusImage = topGemPlusButton.GetComponent<Image>();
        if (plusImage != null)
        {
            plusImage.color = new Color(0.55f, 0.32f, 0.15f, 0.96f);
        }

        SetComponentActive(titleText, false);
        SetComponentActive(versionText, false);
        SetComponentActive(goldText, false);
        SetComponentActive(homeGoldText, false);
        SetComponentActive(gemsText, false);
        SetComponentActive(mythEssenceText, false);
    }

    private void EnsureRuntimeScreenBackdrops()
    {
        EnsureParchmentBackdrop(gearPanel, "Gear Parchment Backdrop");
        EnsureParchmentBackdrop(summonPanel, "Summon Parchment Backdrop");
        EnsureParchmentBackdrop(shopPanel, "Shop Parchment Backdrop");
    }

    private void EnsureRuntimeSummonOffer()
    {
        if (summonPanel == null)
        {
            return;
        }

        if (summonOfferRoot != null)
        {
            EnsureRuntimeSummonResultPopup();
            return;
        }

        var legacyBanner = summonPanel.transform.Find("Summon Banner");
        if (legacyBanner != null)
        {
            legacyBanner.gameObject.SetActive(false);
        }

        var legacyRatesCard = summonPanel.transform.Find("Summon Rates Card");
        if (legacyRatesCard != null)
        {
            legacyRatesCard.gameObject.SetActive(false);
        }

        summonOfferRoot = CreateRuntimePanel(summonPanel.transform, "Summon Offer Banner", new Vector2(0, -312), new Vector2(820, 458), new Color(0.08f, 0.045f, 0.025f, 0.98f));
        summonOfferRoot.SetAsLastSibling();
        CreateLayeredRuntimeBackground(summonOfferRoot, new Vector2(780, 386), 0.78f);

        CreateRuntimePanel(summonOfferRoot, "Summon Offer Top Shade", new Vector2(0, -18), new Vector2(782, 122), new Color(0.04f, 0.025f, 0.018f, 0.55f));
        CreateRuntimePanel(summonOfferRoot, "Summon Offer Promo Shade", new Vector2(0, -104), new Vector2(560, 42), new Color(0.025f, 0.02f, 0.018f, 0.48f));
        CreateRuntimePanel(summonOfferRoot, "Summon Offer Bottom Shade", new Vector2(0, -344), new Vector2(782, 86), new Color(0.055f, 0.028f, 0.016f, 0.84f));
        CreateRuntimePanel(summonOfferRoot, "Summon Offer Border Top", new Vector2(0, -10), new Vector2(790, 5), new Color(0.92f, 0.62f, 0.28f, 0.9f));
        CreateRuntimePanel(summonOfferRoot, "Summon Offer Border Bottom", new Vector2(0, -448), new Vector2(790, 5), new Color(0.92f, 0.62f, 0.28f, 0.9f));

        summonOfferHeroImages = new RawImage[SummonFeaturedHeroCount];
        summonOfferHeroImages[0] = CreateRuntimeRawImage(summonOfferRoot, "Summon Offer Hero Left", LoadRuntimeTexture("hero_dante"), new Vector2(-248, -248), new Vector2(172, 172), new Vector2(0.5f, 1f));
        summonOfferHeroImages[1] = CreateRuntimeRawImage(summonOfferRoot, "Summon Offer Hero Right", LoadRuntimeTexture("hero_elowen"), new Vector2(248, -242), new Vector2(172, 172), new Vector2(0.5f, 1f));
        summonOfferHeroImages[2] = CreateRuntimeRawImage(summonOfferRoot, "Summon Offer Hero Center", LoadRuntimeTexture("hero_cyra"), new Vector2(0, -222), new Vector2(172, 172), new Vector2(0.5f, 1f));

        summonOfferTitleText = CreateRuntimeText(summonOfferRoot, "Summon Offer Title", "Awaken Heroes", 34, new Vector2(0, -52), new Vector2(720, 50));
        summonOfferTitleText.fontStyle = FontStyles.Bold;
        summonOfferTitleText.color = new Color(1f, 0.92f, 0.62f);
        summonOfferTitleText.textWrappingMode = TextWrappingModes.NoWrap;

        summonOfferPromoText = CreateRuntimeText(summonOfferRoot, "Summon Offer Promo", "First x10 pack has a 10% discount", 24, new Vector2(0, -102), new Vector2(540, 36));
        summonOfferPromoText.fontStyle = FontStyles.Bold;
        summonOfferPromoText.color = new Color(0.72f, 1f, 0.94f);
        summonOfferPromoText.textWrappingMode = TextWrappingModes.NoWrap;

        if (summonButton == null)
        {
            summonButton = CreateRuntimeButton(summonPanel.transform, "Summon Button", "Summon", -190, -786, 300, 82);
        }

        summonTenButton = CreateRuntimeButton(summonPanel.transform, "Summon Ten Button", "Summon 10", 190, -786, 300, 82);
        CreateRuntimeRawImage(summonButton.transform, "Gem Icon", GetCurrencyIconTexture("mythic_gem"), new Vector2(-122, -24), new Vector2(20, 27), new Vector2(0.5f, 1f));
        CreateRuntimeRawImage(summonTenButton.transform, "Gem Icon", GetCurrencyIconTexture("mythic_gem"), new Vector2(-122, -24), new Vector2(20, 27), new Vector2(0.5f, 1f));
        summonSingleCostText = CreateSummonButtonCostText(summonButton.transform, "Single Cost");
        summonTenCostText = CreateSummonButtonCostText(summonTenButton.transform, "Ten Cost");

        summonResultBoxRoot = CreateRuntimePanel(summonPanel.transform, "Summon Result Strip", new Vector2(0, -890), new Vector2(760, 70), new Color(0.09f, 0.13f, 0.22f, 0.94f));
        summonCountChipRoot = CreateRuntimePanel(summonPanel.transform, "Summon Count Chip", new Vector2(-286, -976), new Vector2(210, 52), new Color(0.06f, 0.16f, 0.22f, 0.95f));
        summonRatesBoxRoot = CreateRuntimePanel(summonPanel.transform, "Summon Rates Box", new Vector2(130, -976), new Vector2(510, 118), new Color(0.08f, 0.2f, 0.26f, 0.95f));
        CreateRuntimePanel(summonRatesBoxRoot, "Rates Box Glow", new Vector2(0, -4), new Vector2(480, 4), new Color(0.3f, 0.95f, 0.92f, 0.72f));

        summonCarouselRoot = CreateRuntimePanel(summonPanel.transform, "Summon Banner Carousel", new Vector2(0, -890), new Vector2(820, 142), new Color(0.07f, 0.035f, 0.02f, 0.96f));
        CreateRuntimePanel(summonCarouselRoot, "Carousel Top Line", new Vector2(0, -8), new Vector2(790, 4), new Color(0.82f, 0.55f, 0.24f, 0.85f));
        summonCarouselPreviousButton = CreateRuntimeButton(summonCarouselRoot, "Summon Carousel Previous", "<", -366, -40, 58, 78);
        summonCarouselNextButton = CreateRuntimeButton(summonCarouselRoot, "Summon Carousel Next", ">", 366, -40, 58, 78);
        StyleSummonCarouselArrowButton(summonCarouselPreviousButton);
        StyleSummonCarouselArrowButton(summonCarouselNextButton);
        summonCarouselButtons = new Button[SummonCarouselCardCount];
        summonCarouselFrames = new Image[SummonCarouselCardCount];
        summonCarouselTitleTexts = new TMP_Text[SummonCarouselCardCount];
        summonCarouselRateTexts = new TMP_Text[SummonCarouselCardCount];
        summonCarouselHeroImages = new RawImage[SummonCarouselCardCount * SummonCarouselHeroSlotsPerCard];
        summonCarouselCardBannerIndices = new int[SummonCarouselCardCount];
        CreateSummonCarouselCard(0, new Vector2(-256, -24), new Vector2(214, 100));
        CreateSummonCarouselCard(1, new Vector2(0, -18), new Vector2(292, 112));
        CreateSummonCarouselCard(2, new Vector2(256, -24), new Vector2(214, 100));
        AddSummonCarouselSwipeTarget(summonCarouselRoot.gameObject);
        EnsureRuntimeSummonResultPopup();
    }

    private void EnsureRuntimeSummonResultPopup()
    {
        if (summonPanel == null || summonResultPopupRoot != null)
        {
            return;
        }

        summonResultPopupRoot = CreateRuntimePanel(summonPanel.transform, "Summon Result Popup", new Vector2(0, -140), new Vector2(900, 1040), new Color(0.01f, 0.01f, 0.014f, 0.92f));
        summonResultPopupRoot.SetAsLastSibling();
        CreateRuntimePanel(summonResultPopupRoot, "Result Parchment", new Vector2(0, -455), new Vector2(820, 740), new Color(0.15f, 0.09f, 0.055f, 0.98f));
        CreateRuntimePanel(summonResultPopupRoot, "Result Header Glow", new Vector2(0, -70), new Vector2(640, 12), new Color(1f, 0.75f, 0.23f, 0.95f));
        summonResultPopupTitleText = CreateRuntimeText(summonResultPopupRoot, "Result Title", "Summon Results", 34, new Vector2(0, -36), new Vector2(760, 58));
        summonResultPopupTitleText.fontStyle = FontStyles.Bold;
        summonResultPopupTitleText.color = new Color(1f, 0.86f, 0.28f);

        summonResultHeroImages = new RawImage[HeroCount];
        summonResultHeroNameTexts = new TMP_Text[HeroCount];
        summonResultHeroCountTexts = new TMP_Text[HeroCount];
        summonResultHeroFrames = new Image[HeroCount];

        for (var i = 0; i < HeroCount; i++)
        {
            var row = i < 3 ? 0 : 1;
            var column = row == 0 ? i : i - 3;
            var x = row == 0 ? -260f + (column * 260f) : -130f + (column * 260f);
            var y = row == 0 ? -175f : -445f;
            var card = CreateRuntimePanel(summonResultPopupRoot, $"Result Hero Card {i + 1}", new Vector2(x, y), new Vector2(205, 244), new Color(0.62f, 0.26f, 0.12f, 0.96f));
            summonResultHeroFrames[i] = card.GetComponent<Image>();
            CreateRuntimePanel(card, "Inner Glow", new Vector2(0, -118), new Vector2(188, 18), new Color(1f, 0.72f, 0.28f, 0.5f));
            summonResultHeroImages[i] = CreateRuntimeRawImage(card, "Hero", LoadCombatTexture(GetHeroTextureName(i), "idle", 0, GetHeroTextureName(i)), new Vector2(0, -14), new Vector2(150, 150), new Vector2(0.5f, 1f));
            summonResultHeroNameTexts[i] = CreateRuntimeText(card, "Name", string.Empty, 19, new Vector2(0, -166), new Vector2(178, 28));
            summonResultHeroNameTexts[i].fontStyle = FontStyles.Bold;
            summonResultHeroNameTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            summonResultHeroNameTexts[i].enableAutoSizing = true;
            summonResultHeroNameTexts[i].fontSizeMin = 13;
            summonResultHeroNameTexts[i].fontSizeMax = 19;
            summonResultHeroCountTexts[i] = CreateRuntimeText(card, "Draw Count", string.Empty, 30, new Vector2(0, -202), new Vector2(178, 38));
            summonResultHeroCountTexts[i].fontStyle = FontStyles.Bold;
            summonResultHeroCountTexts[i].color = Color.white;
            card.gameObject.SetActive(false);
        }

        summonAutoToggleButton = CreateRuntimeButton(summonResultPopupRoot, "Auto Summon Toggle", string.Empty, 0, -742, 330, 58);
        var autoLabel = summonAutoToggleButton.transform.Find("Label");
        if (autoLabel != null)
        {
            autoLabel.gameObject.SetActive(false);
        }

        var checkbox = CreateRuntimePanel(summonAutoToggleButton.transform, "Checkbox", new Vector2(-130, -9), new Vector2(34, 34), new Color(0.04f, 0.025f, 0.02f, 0.92f));
        summonAutoCheckboxImage = checkbox.GetComponent<Image>();
        summonAutoCheckboxMarkText = CreateRuntimeText(checkbox, "Mark", string.Empty, 26, new Vector2(0, -1), new Vector2(32, 32));
        summonAutoCheckboxMarkText.fontStyle = FontStyles.Bold;
        summonAutoCheckboxMarkText.color = new Color(0.05f, 0.03f, 0.01f);
        summonAutoToggleText = CreateRuntimeText(summonAutoToggleButton.transform, "Auto Text", "Auto-Summon", 26, new Vector2(38, -9), new Vector2(240, 38));
        summonAutoToggleText.alignment = TextAlignmentOptions.Left;
        summonAutoToggleText.fontStyle = FontStyles.Bold;
        summonAutoToggleText.color = new Color(1f, 0.86f, 0.28f);

        summonResultTenButton = CreateRuntimeButton(summonResultPopupRoot, "Result Summon Ten", "x10", -270, -850, 265, 96);
        summonResultMaxButton = CreateRuntimeButton(summonResultPopupRoot, "Result Summon Max", "x300", 270, -850, 265, 96);
        summonResultCloseButton = CreateRuntimeButton(summonResultPopupRoot, "Result Close", "X", 0, -850, 86, 86);
        CreateRuntimeRawImage(summonResultTenButton.transform, "Gem Icon", GetCurrencyIconTexture("mythic_gem"), new Vector2(-92, -58), new Vector2(20, 27), new Vector2(0.5f, 1f));
        CreateRuntimeRawImage(summonResultMaxButton.transform, "Gem Icon", GetCurrencyIconTexture("mythic_gem"), new Vector2(-92, -58), new Vector2(20, 27), new Vector2(0.5f, 1f));
        summonResultTenCostText = CreateSummonResultButtonCostText(summonResultTenButton.transform, "Cost");
        summonResultMaxCostText = CreateSummonResultButtonCostText(summonResultMaxButton.transform, "Cost");

        summonResultPopupRoot.gameObject.SetActive(false);
    }

    private void CreateSummonCarouselCard(int cardIndex, Vector2 anchoredPosition, Vector2 size)
    {
        if (summonCarouselRoot == null || summonCarouselButtons == null || cardIndex < 0 || cardIndex >= summonCarouselButtons.Length)
        {
            return;
        }

        var button = CreateRuntimeButton(summonCarouselRoot, $"Summon Carousel Card {cardIndex + 1}", string.Empty, anchoredPosition.x, anchoredPosition.y, size.x, size.y);
        summonCarouselButtons[cardIndex] = button;
        summonCarouselFrames[cardIndex] = button.GetComponent<Image>();
        if (summonCarouselFrames[cardIndex] != null)
        {
            summonCarouselFrames[cardIndex].color = new Color(0.12f, 0.075f, 0.045f, 0.94f);
        }

        var label = button.transform.Find("Label");
        if (label != null)
        {
            label.gameObject.SetActive(false);
        }

        CreateLayeredRuntimeBackground(button.transform, new Vector2(size.x - 14f, size.y - 16f), 0.32f);
        CreateRuntimePanel(button.transform, "Thumbnail Shade", new Vector2(0, -58), new Vector2(size.x - 18f, 42), new Color(0.02f, 0.015f, 0.012f, 0.58f));
        summonCarouselTitleTexts[cardIndex] = CreateRuntimeText(button.transform, "Title", string.Empty, 15, new Vector2(0, -8), new Vector2(size.x - 22f, 24));
        summonCarouselTitleTexts[cardIndex].fontStyle = FontStyles.Bold;
        summonCarouselTitleTexts[cardIndex].fontSizeMin = 11;
        summonCarouselTitleTexts[cardIndex].fontSizeMax = 15;
        summonCarouselTitleTexts[cardIndex].enableAutoSizing = true;
        summonCarouselTitleTexts[cardIndex].textWrappingMode = TextWrappingModes.NoWrap;

        summonCarouselRateTexts[cardIndex] = CreateRuntimeText(button.transform, "Lead Rate", string.Empty, 14, new Vector2(0, -73), new Vector2(size.x - 22f, 20));
        summonCarouselRateTexts[cardIndex].fontStyle = FontStyles.Bold;
        summonCarouselRateTexts[cardIndex].fontSizeMin = 10;
        summonCarouselRateTexts[cardIndex].fontSizeMax = 14;
        summonCarouselRateTexts[cardIndex].enableAutoSizing = true;
        summonCarouselRateTexts[cardIndex].textWrappingMode = TextWrappingModes.NoWrap;

        var leftHeroIndex = cardIndex * SummonCarouselHeroSlotsPerCard;
        var rightHeroIndex = leftHeroIndex + 1;
        summonCarouselHeroImages[leftHeroIndex] = CreateRuntimeRawImage(button.transform, "Hero Preview Left", LoadRuntimeTexture("hero_astra"), new Vector2(-42, -38), new Vector2(72, 72), new Vector2(0.5f, 1f));
        summonCarouselHeroImages[rightHeroIndex] = CreateRuntimeRawImage(button.transform, "Hero Preview Right", LoadRuntimeTexture("hero_elowen"), new Vector2(42, -38), new Vector2(72, 72), new Vector2(0.5f, 1f));
        AddSummonCarouselSwipeTarget(button.gameObject);
    }

    private void AddSummonCarouselSwipeTarget(GameObject target)
    {
        AddEventTrigger(target, EventTriggerType.BeginDrag, BeginSummonCarouselDrag);
        AddEventTrigger(target, EventTriggerType.EndDrag, EndSummonCarouselDrag);
    }

    private static void StyleSummonCarouselArrowButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.43f, 0.23f, 0.1f, 0.96f);
        }

        var label = button.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label != null)
        {
            label.fontSize = 34;
            label.fontSizeMin = 24;
            label.fontSizeMax = 34;
            label.color = new Color(1f, 0.82f, 0.42f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private static TMP_Text CreateSummonButtonCostText(Transform parent, string name)
    {
        var text = CreateRuntimeText(parent, name, string.Empty, 14, new Vector2(-122, -50), new Vector2(58, 18));
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.fontSizeMin = 11;
        text.fontSizeMax = 14;
        text.enableAutoSizing = true;
        text.color = Color.white;
        text.outlineColor = new Color(0.12f, 0.06f, 0.02f, 1f);
        text.outlineWidth = 0.12f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static TMP_Text CreateSummonResultButtonCostText(Transform parent, string name)
    {
        var text = CreateRuntimeText(parent, name, string.Empty, 25, new Vector2(-24, -58), new Vector2(140, 34));
        text.alignment = TextAlignmentOptions.Left;
        text.fontStyle = FontStyles.Bold;
        text.fontSizeMin = 16;
        text.fontSizeMax = 25;
        text.enableAutoSizing = true;
        text.color = Color.white;
        text.outlineColor = new Color(0.12f, 0.06f, 0.02f, 1f);
        text.outlineWidth = 0.18f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
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
        if (homePanel == null || homeActionRoot != null)
        {
            return;
        }

        var rootObject = new GameObject("Home Generated Art Root", typeof(RectTransform));
        rootObject.transform.SetParent(homePanel.transform, false);
        homeActionRoot = rootObject.GetComponent<RectTransform>();
        StretchRuntime(homeActionRoot, Vector2.zero);

        homeLeftShortcutShadow = CreateRuntimePanel(homeActionRoot, "Home Left Shortcut Shadow", new Vector2(-430, -135), new Vector2(142, 230), new Color(0f, 0f, 0f, 0.28f));
        homeRightShortcutShadow = CreateRuntimePanel(homeActionRoot, "Home Right Shortcut Shadow", new Vector2(430, -80), new Vector2(148, 340), new Color(0f, 0f, 0f, 0.28f));
        homeLeftShortcutShadow.SetAsFirstSibling();
        homeRightShortcutShadow.SetAsFirstSibling();

        CreateRuntimeRawImage(homeActionRoot, "Home Stage Level Badge", GetHomeGeneratedTexture("home_stage_level_badge"), new Vector2(0, -72), new Vector2(490, 92), new Vector2(0.5f, 1f));
        homeStageLevelBadgeText = CreateRuntimeText(homeActionRoot, "Home Stage Level Text", "Stufe 1", 38, new Vector2(38, -93), new Vector2(340, 50));
        homeStageLevelBadgeText.fontStyle = FontStyles.Bold;
        homeStageLevelBadgeText.textWrappingMode = TextWrappingModes.NoWrap;
        homeStageLevelBadgeText.enableAutoSizing = true;
        homeStageLevelBadgeText.fontSizeMin = 26;
        homeStageLevelBadgeText.fontSizeMax = 38;

        CreateRuntimeRawImage(homeActionRoot, "Home Stage Mode Badge", GetHomeGeneratedTexture("home_stage_mode_badge"), new Vector2(0, -134), new Vector2(390, 70), new Vector2(0.5f, 1f));
        homeStageModeBadgeText = CreateRuntimeText(homeActionRoot, "Home Stage Mode Text", "Albtraum", 38, new Vector2(42, -144), new Vector2(275, 48));
        homeStageModeBadgeText.fontStyle = FontStyles.Bold;
        homeStageModeBadgeText.textWrappingMode = TextWrappingModes.NoWrap;
        homeStageModeBadgeText.enableAutoSizing = true;
        homeStageModeBadgeText.fontSizeMin = 24;
        homeStageModeBadgeText.fontSizeMax = 38;
        homeStageModeBadgeText.color = new Color(0.87f, 0.5f, 1f);

        CreateRuntimeRawImage(homeActionRoot, "Home Stage Extra Badge", GetHomeGeneratedTexture("home_stage_extra_badge"), new Vector2(0, -182), new Vector2(170, 61), new Vector2(0.5f, 1f));

        homeShopButton = CreateRuntimeImageButton(homeActionRoot, "Home Shop Button", GetHomeGeneratedTexture("home_shop_button"), new Vector2(-430, -150), new Vector2(145, 165), out _);
        homeTreasureChestButton = CreateRuntimeImageButton(homeActionRoot, "Home Inventory Chest Button", GetHomeGeneratedTexture("home_treasure_chest_button"), new Vector2(430, -88), new Vector2(130, 148), out _);
        homeQuestButton = CreateRuntimeImageButton(homeActionRoot, "Home Quest Button", GetHomeGeneratedTexture("home_quest_button"), new Vector2(430, -238), new Vector2(145, 165), out _);
        homeShortcutToggleButton = CreateRuntimeButton(homeActionRoot, "Home Shortcut Toggle Button", "^", 430, -410, 70, 42);
        homeShortcutToggleText = homeShortcutToggleButton.GetComponentInChildren<TMP_Text>();
        var toggleImage = homeShortcutToggleButton.GetComponent<Image>();
        if (toggleImage != null)
        {
            toggleImage.color = new Color(0.06f, 0.04f, 0.03f, 0.62f);
        }

        homeLeftShortcutToggleButton = CreateRuntimeButton(homeActionRoot, "Home Left Shortcut Toggle Button", "^", -430, -332, 70, 42);
        homeLeftShortcutToggleText = homeLeftShortcutToggleButton.GetComponentInChildren<TMP_Text>();
        var leftToggleImage = homeLeftShortcutToggleButton.GetComponent<Image>();
        if (leftToggleImage != null)
        {
            leftToggleImage.color = new Color(0.06f, 0.04f, 0.03f, 0.62f);
        }

        homeWorldMapButton = CreateRuntimeImageButton(homeActionRoot, "Home World Map Button", GetHomeGeneratedTexture("home_world_map_button"), new Vector2(-410, -1010), new Vector2(150, 171), out _);
        homeRewardsButton = CreateRuntimeImageButton(homeActionRoot, "Home Fast Rewards Button", GetHomeGeneratedTexture("home_fast_rewards_button"), new Vector2(395, -1010), new Vector2(150, 171), out _);
        homeChatButton = CreateRuntimeImageButton(homeActionRoot, "Home Chat Button", GetHomeGeneratedTexture("home_chat_button"), new Vector2(-432, -835), new Vector2(126, 126), out _);
        homeBeginButton = CreateRuntimeImageButton(homeActionRoot, "Home Battle Button", GetHomeGeneratedTexture("home_battle_button"), new Vector2(0, -1090), new Vector2(430, 131), out _);

        SetHomeShortcutsExpanded(homeShortcutsExpanded);
    }

    private void EnsureRuntimeCampaignMap()
    {
        if (homeActionRoot == null || homeCampaignMapRoot != null)
        {
            return;
        }

        var rootObject = new GameObject("Home Campaign Map Root", typeof(RectTransform));
        rootObject.transform.SetParent(homeActionRoot, false);
        homeCampaignMapRoot = rootObject.GetComponent<RectTransform>();
        SetRuntimeRect(homeCampaignMapRoot, new Vector2(0, -245), new Vector2(860, 710), new Vector2(0.5f, 1f));
        homeCampaignMapRoot.SetAsFirstSibling();

        var mapBack = CreateRuntimePanel(homeCampaignMapRoot, "Campaign Map Backplate", Vector2.zero, new Vector2(860, 710), new Color(0.08f, 0.12f, 0.19f, 0.98f));
        mapBack.SetAsFirstSibling();
        CreateLayeredRuntimeBackground(mapBack, new Vector2(860, 710), 0.72f);

        var lake = CreateRuntimePanel(mapBack, "Frozen River", new Vector2(-118, -535), new Vector2(690, 54), new Color(0.5f, 0.8f, 1f, 0.34f));
        lake.localRotation = Quaternion.Euler(0f, 0f, -8f);

        var nodePositions = GetCampaignMapNodePositions();
        for (var i = 0; i < nodePositions.Length - 1; i++)
        {
            CreateCampaignPathSegment(homeCampaignMapRoot, nodePositions[i], nodePositions[i + 1]);
        }

        campaignStageButtons = new Button[nodePositions.Length];
        campaignStageButtonTexts = new TMP_Text[nodePositions.Length];
        campaignStageButtonIcons = new RawImage[nodePositions.Length];
        campaignStageButtonFrames = new Image[nodePositions.Length];

        for (var i = 0; i < nodePositions.Length; i++)
        {
            campaignStageButtons[i] = CreateCampaignStageButton(homeCampaignMapRoot, i, nodePositions[i]);
        }

        campaignStagePreviewRoot = CreateRuntimePanel(homeCampaignMapRoot, "Campaign Stage Preview", new Vector2(0, -620), new Vector2(790, 118), new Color(0.03f, 0.035f, 0.055f, 0.82f));
        campaignStagePreviewText = CreateRuntimeText(campaignStagePreviewRoot, "Campaign Stage Preview Text", string.Empty, 22, new Vector2(0, -16), new Vector2(740, 88));
        campaignStagePreviewText.enableAutoSizing = true;
        campaignStagePreviewText.fontSizeMin = 16;
        campaignStagePreviewText.fontSizeMax = 22;
        campaignStagePreviewText.alignment = TextAlignmentOptions.Center;
    }

    private Button CreateCampaignStageButton(Transform parent, int nodeIndex, Vector2 position)
    {
        var buttonObject = new GameObject($"Campaign Stage Node {nodeIndex + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRuntimeRect(buttonObject.GetComponent<RectTransform>(), position, new Vector2(104, 118), new Vector2(0.5f, 1f));

        var frame = buttonObject.GetComponent<Image>();
        frame.color = new Color(0.21f, 0.12f, 0.21f, 0.96f);

        var icon = CreateRuntimeRawImage(buttonObject.transform, "Stage Icon", LoadCombatTexture("enemy_rat", "idle", 0, "enemy_campaign"), new Vector2(0, -18), new Vector2(70, 70), new Vector2(0.5f, 1f));
        icon.raycastTarget = false;

        var text = CreateRuntimeText(buttonObject.transform, "Stage Label", "1-1", 20, new Vector2(0, -86), new Vector2(92, 28));
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14;
        text.fontSizeMax = 20;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        campaignStageButtonFrames[nodeIndex] = frame;
        campaignStageButtonIcons[nodeIndex] = icon;
        campaignStageButtonTexts[nodeIndex] = text;

        var capturedIndex = nodeIndex;
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = frame;
        button.onClick.AddListener(() => SelectVisibleCampaignStage(capturedIndex));
        return button;
    }

    private void CreateCampaignPathSegment(Transform parent, Vector2 start, Vector2 end)
    {
        var midpoint = (start + end) * 0.5f;
        var delta = end - start;
        var length = Mathf.Max(1f, delta.magnitude);
        var segment = CreateRuntimePanel(parent, "Campaign Path Segment", midpoint, new Vector2(length + 24f, 18f), new Color(0.74f, 0.82f, 0.93f, 0.68f));
        segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private void EnsureRuntimeBattleFlowUi()
    {
        if (battlePanel == null || formationRoot != null)
        {
            return;
        }

        EnsureRuntimeFormationUi();
        EnsureRuntimeFightUi();
        ApplyBattleFlowVisibility();
    }

    private void EnsureRuntimeFormationUi()
    {
        var rootObject = new GameObject("Campaign Formation Root", typeof(RectTransform));
        rootObject.transform.SetParent(battlePanel.transform, false);
        formationRoot = rootObject.GetComponent<RectTransform>();
        StretchRuntime(formationRoot, Vector2.zero);

        var backdrop = CreateRuntimePanel(formationRoot, "Formation Backdrop", new Vector2(0, -150), new Vector2(880, 1120), new Color(0.06f, 0.045f, 0.055f, 0.96f));
        CreateLayeredRuntimeBackground(backdrop, new Vector2(880, 1120), 0.52f);

        formationHeaderText = CreateRuntimeText(formationRoot, "Formation Header", "Formation", 34, new Vector2(0, -118), new Vector2(780, 50));
        formationHeaderText.fontStyle = FontStyles.Bold;

        formationTeamText = CreateRuntimeText(formationRoot, "Formation Team Power", string.Empty, 23, new Vector2(0, -172), new Vector2(780, 40));
        formationTeamText.color = new Color(0.78f, 0.91f, 1f);
        formationTeamText.fontStyle = FontStyles.Bold;
        formationTeamText.enableAutoSizing = true;
        formationTeamText.fontSizeMin = 15;
        formationTeamText.fontSizeMax = 23;

        var arena = CreateRuntimePanel(formationRoot, "Formation Arena Preview", new Vector2(0, -235), new Vector2(820, 410), new Color(0.03f, 0.045f, 0.07f, 0.88f));
        CreateLayeredRuntimeBackground(arena, new Vector2(820, 410), 0.62f);
        formationArenaBackgroundImage = CreateRuntimeRawImage(arena, "Formation Dungeon Battle Map", null, Vector2.zero, new Vector2(820, 410), new Vector2(0.5f, 1f));
        formationArenaBackgroundImage.color = new Color(1f, 1f, 1f, 0.82f);
        formationArenaBackgroundImage.gameObject.SetActive(false);

        EnsureFormationOrder();
        formationSlotButtons = new Button[HeroCount];
        formationSlotFrames = new Image[HeroCount];
        formationHeroImages = new RawImage[HeroCount];
        formationHeroSkeletalViews = new RavikSkeletalCombatView[HeroCount];
        formationHeroTexts = new TMP_Text[HeroCount];
        var heroPositions = GetFormationHeroPositions();
        for (var i = 0; i < HeroCount; i++)
        {
            var slotObject = new GameObject($"Formation Slot {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            slotObject.transform.SetParent(arena, false);
            SetRuntimeRect(slotObject.GetComponent<RectTransform>(), heroPositions[i] + new Vector2(0, 4), new Vector2(158, 172), new Vector2(0.5f, 1f));

            var slotFrame = slotObject.GetComponent<Image>();
            slotFrame.color = new Color(0.1f, 0.13f, 0.2f, 0.62f);
            formationSlotFrames[i] = slotFrame;

            var slotButton = slotObject.GetComponent<Button>();
            slotButton.targetGraphic = slotFrame;
            var capturedSlot = i;
            slotButton.onClick.AddListener(() => SelectFormationSlot(capturedSlot));
            formationSlotButtons[i] = slotButton;

            formationHeroImages[i] = CreateRuntimeRawImage(arena, $"Formation Hero {i + 1}", LoadCombatTexture(GetHeroTextureName(i), "idle", 0, GetHeroTextureName(i)), heroPositions[i], new Vector2(124, 124), new Vector2(0.5f, 1f));
            formationHeroImages[i].rectTransform.localScale = new Vector3(GetHeroFacingScale(i), 1f, 1f);
            formationHeroImages[i].raycastTarget = false;
            formationHeroSkeletalViews[i] = RavikSkeletalCombatView.Create(arena, $"Formation Ravik Skeletal View {i + 1}", heroPositions[i], 0.54f);
            formationHeroTexts[i] = CreateRuntimeText(arena, $"Formation Hero Label {i + 1}", string.Empty, 16, heroPositions[i] + new Vector2(0, -112), new Vector2(126, 26));
            formationHeroTexts[i].fontStyle = FontStyles.Bold;
            formationHeroTexts[i].enableAutoSizing = true;
            formationHeroTexts[i].fontSizeMin = 12;
            formationHeroTexts[i].fontSizeMax = 16;
            formationHeroTexts[i].raycastTarget = false;
        }

        formationEnemyImage = CreateRuntimeRawImage(arena, "Formation Enemy", LoadCombatTexture("enemy_rat", "idle", 0, "enemy_campaign"), new Vector2(260, -164), new Vector2(150, 150), new Vector2(0.5f, 1f));
        formationEnemyImage.rectTransform.localScale = new Vector3(GetEnemyFacingScale("enemy_rat"), 1f, 1f);
        formationEnemyText = CreateRuntimeText(formationRoot, "Formation Enemy Text", string.Empty, 23, new Vector2(0, -675), new Vector2(780, 82));
        formationEnemyText.enableAutoSizing = true;
        formationEnemyText.fontSizeMin = 16;
        formationEnemyText.fontSizeMax = 23;

        formationHintText = CreateRuntimeText(formationRoot, "Formation Hint", $"Confirm starts a visible {DefaultCombatDurationSeconds}s combat sim.", 20, new Vector2(0, -770), new Vector2(760, 40));
        formationHintText.color = new Color(0.78f, 0.84f, 0.92f);

        formationAutoContinueButton = CreateRuntimeButton(formationRoot, "Formation Auto Continue Toggle", string.Empty, 0, -835, 560, 50);
        var autoContinueButtonImage = formationAutoContinueButton.GetComponent<Image>();
        if (autoContinueButtonImage != null)
        {
            autoContinueButtonImage.color = new Color(0.02f, 0.025f, 0.04f, 0.4f);
        }

        formationAutoContinueBox = CreateRuntimePanel(formationAutoContinueButton.transform, "Checkbox", new Vector2(-250, 0), new Vector2(34, 34), new Color(0.04f, 0.055f, 0.08f, 0.96f)).GetComponent<Image>();
        formationAutoContinueMarkText = CreateRuntimeText(formationAutoContinueButton.transform, "Checkbox Mark", string.Empty, 24, new Vector2(-250, 0), new Vector2(34, 34));
        formationAutoContinueMarkText.fontStyle = FontStyles.Bold;
        formationAutoContinueMarkText.raycastTarget = false;
        formationAutoContinueText = CreateRuntimeText(formationAutoContinueButton.transform, "Auto Continue Label", "Auto next after win (skills AUTO)", 20, new Vector2(34, 0), new Vector2(470, 38));
        formationAutoContinueText.alignment = TextAlignmentOptions.Left;
        formationAutoContinueText.enableAutoSizing = true;
        formationAutoContinueText.fontSizeMin = 14;
        formationAutoContinueText.fontSizeMax = 20;
        formationAutoContinueText.raycastTarget = false;
        RefreshFormationAutoContinueToggle();

        formationBackButton = CreateRuntimeButton(formationRoot, "Formation Back Button", "Back", -225, -900, 210, 62);
        formationConfirmButton = CreateRuntimeButton(formationRoot, "Formation Confirm Button", "Confirm", 135, -900, 330, 70);
    }

    private void EnsureRuntimeFightUi()
    {
        var rootObject = new GameObject("Campaign Fight Root", typeof(RectTransform));
        rootObject.transform.SetParent(battlePanel.transform, false);
        fightRoot = rootObject.GetComponent<RectTransform>();
        StretchRuntime(fightRoot, Vector2.zero);

        var backdrop = CreateRuntimePanel(fightRoot, "Fight Arena Backdrop", new Vector2(0, -110), new Vector2(960, 1190), new Color(0.045f, 0.055f, 0.07f, 0.98f));
        CreateLayeredRuntimeBackground(backdrop, new Vector2(960, 1190), 0.76f);
        fightArenaBackgroundImage = CreateRuntimeRawImage(backdrop, "Fight Arena Battle Map", null, Vector2.zero, new Vector2(960, 1190), new Vector2(0.5f, 1f));
        fightArenaBackgroundImage.color = new Color(1f, 1f, 1f, 0.94f);
        fightArenaBackgroundImage.gameObject.SetActive(false);

        fightVsText = CreateRuntimeText(fightRoot, "Fight VS Text", "VS", 38, new Vector2(0, -130), new Vector2(820, 52));
        fightVsText.fontStyle = FontStyles.Bold;
        fightTimerText = CreateRuntimeText(fightRoot, "Fight Timer Text", FormatFightTimer(DefaultCombatDurationSeconds), 25, new Vector2(0, -184), new Vector2(320, 42));
        fightTimerText.fontStyle = FontStyles.Bold;
        fightStatusText = CreateRuntimeText(fightRoot, "Fight Status Text", "Ready", 23, new Vector2(0, -930), new Vector2(820, 58));
        fightStatusText.enableAutoSizing = true;
        fightStatusText.fontSizeMin = 16;
        fightStatusText.fontSizeMax = 23;
        fightEndButton = CreateRuntimeButton(fightRoot, "Fight End Button", "End Fight", -386, -842, 142, 58);
        var fightEndButtonImage = fightEndButton.GetComponent<Image>();
        if (fightEndButtonImage != null)
        {
            fightEndButtonImage.color = new Color(0.48f, 0.16f, 0.12f, 0.96f);
        }

        fightBossHpFill = CreateRuntimeHealthFill(fightRoot, "Fight Boss Top HP", new Vector2(0, -230), 690, new Color(0.88f, 0.16f, 0.2f));
        var bossHpBack = fightBossHpFill.transform.parent.GetComponent<RectTransform>();
        bossHpBack.sizeDelta = new Vector2(690, 34);
        fightBossHpText = CreateRuntimeText(fightBossHpFill.transform.parent, "Boss HP Text", "Boss HP 100%", 21, Vector2.zero, new Vector2(670, 34));
        fightBossHpText.fontStyle = FontStyles.Bold;
        fightBossHpText.raycastTarget = false;
        fightBossHpFill.transform.parent.gameObject.SetActive(false);

        fightHeroImages = new RawImage[HeroCount];
        fightEnemyImages = new RawImage[HeroCount];
        fightHeroSkeletalViews = new RavikSkeletalCombatView[HeroCount];
        fightHeroRects = new RectTransform[HeroCount];
        fightEnemyRects = new RectTransform[HeroCount];
        fightHeroHpFills = new Image[HeroCount];
        fightEnemyHpFills = new Image[HeroCount];
        fightEnemyHpPercentTexts = new TMP_Text[HeroCount];

        var heroPositions = GetFightHeroPositions();
        var enemyPositions = GetFightEnemyPositions();
        for (var i = 0; i < HeroCount; i++)
        {
            fightHeroImages[i] = CreateRuntimeRawImage(fightRoot, $"Fight Hero {i + 1}", LoadCombatTexture(GetHeroTextureName(i), "idle", 0, GetHeroTextureName(i)), heroPositions[i], new Vector2(132, 132), new Vector2(0.5f, 1f));
            fightHeroImages[i].rectTransform.localScale = new Vector3(GetHeroFacingScale(i), 1f, 1f);
            fightHeroRects[i] = fightHeroImages[i].GetComponent<RectTransform>();
            fightHeroSkeletalViews[i] = RavikSkeletalCombatView.Create(fightRoot, $"Fight Ravik Skeletal View {i + 1}", heroPositions[i], 0.65f);
            fightHeroHpFills[i] = CreateRuntimeHealthFill(fightRoot, $"Fight Hero HP {i + 1}", heroPositions[i] + new Vector2(0, -128), 118, new Color(0.16f, 0.78f, 0.33f));
            SetHealthFillVisible(fightHeroHpFills[i], false);

            var enemyTextureName = GetCampaignEnemyTextureName(1, i);
            fightEnemyImages[i] = CreateRuntimeRawImage(fightRoot, $"Fight Enemy {i + 1}", LoadCombatTexture(enemyTextureName, "idle", 0, "enemy_campaign"), enemyPositions[i], new Vector2(126, 126), new Vector2(0.5f, 1f));
            fightEnemyImages[i].rectTransform.localScale = new Vector3(GetEnemyFacingScale(enemyTextureName), 1f, 1f);
            fightEnemyRects[i] = fightEnemyImages[i].GetComponent<RectTransform>();
            fightEnemyHpFills[i] = CreateRuntimeHealthFill(fightRoot, $"Fight Enemy HP {i + 1}", enemyPositions[i] + new Vector2(0, -122), 112, new Color(0.86f, 0.18f, 0.22f));
            fightEnemyHpPercentTexts[i] = CreateRuntimeText(fightEnemyHpFills[i].transform.parent, "HP Percent", "100%", 12, Vector2.zero, new Vector2(108, 15));
            fightEnemyHpPercentTexts[i].fontStyle = FontStyles.Bold;
            fightEnemyHpPercentTexts[i].raycastTarget = false;
        }

        fightHeroProjectileImages = new Image[HeroCount];
        fightEnemyProjectileImages = new Image[HeroCount];
        fightHeroProjectileRects = new RectTransform[HeroCount];
        fightEnemyProjectileRects = new RectTransform[HeroCount];
        fightHeroFxImages = new RawImage[HeroCount];
        fightHeroFxRects = new RectTransform[HeroCount];
        for (var i = 0; i < HeroCount; i++)
        {
            fightHeroProjectileImages[i] = CreateRuntimeProjectile(fightRoot, $"Hero Projectile {i + 1}", new Color(0.38f, 0.94f, 1f, 0.92f), out fightHeroProjectileRects[i]);
            fightEnemyProjectileImages[i] = CreateRuntimeProjectile(fightRoot, $"Enemy Projectile {i + 1}", new Color(1f, 0.36f, 0.24f, 0.92f), out fightEnemyProjectileRects[i]);
            fightHeroFxImages[i] = CreateRuntimeRawImage(fightRoot, $"Hero Cast FX {i + 1}", null, new Vector2(0, -520), new Vector2(220, 120), new Vector2(0.5f, 1f));
            fightHeroFxRects[i] = fightHeroFxImages[i].rectTransform;
            fightHeroFxImages[i].gameObject.SetActive(false);
        }

        CreateFightSkillBar();

        fightFloatingTexts = new TMP_Text[4];
        for (var i = 0; i < fightFloatingTexts.Length; i++)
        {
            fightFloatingTexts[i] = CreateRuntimeText(fightRoot, $"Fight Floating Text {i + 1}", string.Empty, 26, new Vector2(0, -520), new Vector2(220, 44));
            fightFloatingTexts[i].fontStyle = FontStyles.Bold;
            fightFloatingTexts[i].gameObject.SetActive(false);
        }

        fightResultRoot = CreateRuntimePopup(fightRoot, "Campaign Fight Result Popup", new Vector2(0, -360), new Vector2(760, 360), "Result");
        fightResultTitleText = fightResultRoot.Find("Title").GetComponent<TMP_Text>();
        fightResultBodyText = CreateRuntimeText(fightResultRoot, "Result Body", string.Empty, 22, new Vector2(0, -105), new Vector2(660, 150));
        fightResultBodyText.enableAutoSizing = true;
        fightResultBodyText.fontSizeMin = 15;
        fightResultBodyText.fontSizeMax = 22;
        fightContinueButton = CreateRuntimeButton(fightResultRoot, "Fight Continue Button", "Continue", 0, -286, 240, 62);
        fightResultRoot.gameObject.SetActive(false);
    }

    private void CreateFightSkillBar()
    {
        fightSkillButtons = new Button[HeroCount];
        fightSkillBackplates = new Image[HeroCount];
        fightSkillHpFills = new Image[HeroCount];
        fightSkillHpTexts = new TMP_Text[HeroCount];
        fightSkillManaFills = new Image[HeroCount];
        fightSkillPortraits = new RawImage[HeroCount];
        fightSkillNameTexts = new TMP_Text[HeroCount];
        fightSkillManaTexts = new TMP_Text[HeroCount];

        const float spacing = 154f;
        var startX = -((HeroCount - 1) * spacing * 0.5f);
        for (var i = 0; i < HeroCount; i++)
        {
            var cardObject = new GameObject($"Fight Skill Card {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cardObject.transform.SetParent(fightRoot, false);
            SetRuntimeRect(cardObject.GetComponent<RectTransform>(), new Vector2(startX + spacing * i, -1048), new Vector2(132, 180), new Vector2(0.5f, 1f));

            var backplate = cardObject.GetComponent<Image>();
            backplate.color = new Color(0.07f, 0.09f, 0.13f, 0.96f);
            fightSkillBackplates[i] = backplate;

            var button = cardObject.GetComponent<Button>();
            button.targetGraphic = backplate;
            var capturedIndex = i;
            button.onClick.AddListener(() => QueueFightHeroUltimate(capturedIndex));
            fightSkillButtons[i] = button;

            var portrait = CreateRuntimeRawImage(cardObject.transform, "Portrait", LoadCombatTexture(GetHeroTextureName(i), "idle", 0, GetHeroTextureName(i)), new Vector2(0, -12), new Vector2(104, 112), new Vector2(0.5f, 1f));
            portrait.rectTransform.localScale = new Vector3(GetHeroFacingScale(i), 1f, 1f);
            portrait.raycastTarget = false;
            fightSkillPortraits[i] = portrait;

            fightSkillHpFills[i] = CreateRuntimeHealthFill(cardObject.transform, "HP Back", new Vector2(0, -114), 104, new Color(0.16f, 0.78f, 0.33f, 0.96f));
            var hpBack = fightSkillHpFills[i].transform.parent.GetComponent<RectTransform>();
            if (hpBack != null)
            {
                hpBack.sizeDelta = new Vector2(104, 13);
            }

            fightSkillHpTexts[i] = CreateRuntimeText(fightSkillHpFills[i].transform.parent, "HP Text", "100%", 10, Vector2.zero, new Vector2(98, 13));
            fightSkillHpTexts[i].fontStyle = FontStyles.Bold;
            fightSkillHpTexts[i].raycastTarget = false;

            var manaBack = CreateRuntimePanel(cardObject.transform, "Mana Back", new Vector2(0, -132), new Vector2(104, 14), new Color(0.02f, 0.025f, 0.04f, 0.94f));
            var manaFillObject = new GameObject("Mana Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            manaFillObject.transform.SetParent(manaBack, false);
            var manaFillRect = manaFillObject.GetComponent<RectTransform>();
            manaFillRect.anchorMin = Vector2.zero;
            manaFillRect.anchorMax = Vector2.one;
            manaFillRect.offsetMin = Vector2.zero;
            manaFillRect.offsetMax = Vector2.zero;
            fightSkillManaFills[i] = manaFillObject.GetComponent<Image>();
            fightSkillManaFills[i].color = new Color(0.16f, 0.68f, 1f, 0.96f);
            fightSkillManaFills[i].raycastTarget = false;

            fightSkillNameTexts[i] = CreateRuntimeText(cardObject.transform, "Name", GetHeroDefinition(i).name, 16, new Vector2(0, -150), new Vector2(118, 24));
            fightSkillNameTexts[i].fontStyle = FontStyles.Bold;
            fightSkillNameTexts[i].enableAutoSizing = true;
            fightSkillNameTexts[i].fontSizeMin = 11;
            fightSkillNameTexts[i].fontSizeMax = 16;
            fightSkillNameTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            fightSkillNameTexts[i].raycastTarget = false;

            fightSkillManaTexts[i] = CreateRuntimeText(cardObject.transform, "Mana", "0/100", 14, new Vector2(0, -168), new Vector2(118, 22));
            fightSkillManaTexts[i].color = new Color(0.74f, 0.9f, 1f);
            fightSkillManaTexts[i].enableAutoSizing = true;
            fightSkillManaTexts[i].fontSizeMin = 10;
            fightSkillManaTexts[i].fontSizeMax = 14;
            fightSkillManaTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            fightSkillManaTexts[i].raycastTarget = false;
        }

        fightAutoSkillButton = CreateRuntimeButton(fightRoot, "Fight Auto Skill Button", "AUTO", 316, -966, 96, 56);
        fightAutoSkillButton.onClick.AddListener(ToggleFightAutoSkills);
        fightAutoSkillButtonText = fightAutoSkillButton.GetComponentInChildren<TMP_Text>();
        RefreshFightAutoSkillButton();

        fightSpeedButton = CreateRuntimeButton(fightRoot, "Fight Speed Button", "x2", 424, -966, 82, 56);
        fightSpeedButton.onClick.AddListener(ToggleFightSpeed);
        fightSpeedButtonText = fightSpeedButton.GetComponentInChildren<TMP_Text>();
        RefreshFightSpeedButton();
    }

    private void EnsureRuntimeHomePopups()
    {
        if (homeActionRoot == null || inventoryPopupRoot != null)
        {
            return;
        }

        inventoryPopupRoot = CreateRuntimePanel(homeActionRoot, "Inventory Popup", new Vector2(0, -96), new Vector2(920, 1110), new Color(0.72f, 0.53f, 0.29f, 0.98f));
        var inventoryImage = inventoryPopupRoot.GetComponent<Image>();
        if (inventoryImage != null)
        {
            inventoryImage.raycastTarget = true;
        }

        CreateRuntimePanel(inventoryPopupRoot, "Inventory Inner Parchment", new Vector2(0, -162), new Vector2(860, 860), new Color(0.97f, 0.82f, 0.52f, 0.96f));
        CreateRuntimePanel(inventoryPopupRoot, "Inventory Header Plaque", new Vector2(0, -26), new Vector2(470, 106), new Color(0.19f, 0.1f, 0.045f, 0.96f));
        CreateRuntimePanel(inventoryPopupRoot, "Inventory Header Glow", new Vector2(0, -88), new Vector2(600, 8), new Color(0.88f, 0.56f, 0.24f, 0.86f));
        CreateRuntimePanel(inventoryPopupRoot, "Inventory Bottom Rail", new Vector2(0, -958), new Vector2(884, 44), new Color(0.19f, 0.1f, 0.045f, 0.96f));

        var title = CreateRuntimeText(inventoryPopupRoot, "Inventory Title", "Bag", 36, new Vector2(0, -38), new Vector2(360, 56));
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.88f, 0.62f);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.outlineColor = new Color(0.09f, 0.035f, 0.01f, 0.96f);
        title.outlineWidth = 0.16f;

        inventoryPopupText = CreateRuntimeText(inventoryPopupRoot, "Inventory Summary", string.Empty, 20, new Vector2(0, -118), new Vector2(760, 42));
        inventoryPopupText.enableAutoSizing = true;
        inventoryPopupText.fontSizeMin = 14;
        inventoryPopupText.fontSizeMax = 20;
        inventoryPopupText.fontStyle = FontStyles.Bold;
        inventoryPopupText.color = new Color(0.29f, 0.15f, 0.055f);
        inventoryPopupText.textWrappingMode = TextWrappingModes.NoWrap;

        inventoryGridRoot = CreateRuntimePanel(inventoryPopupRoot, "Inventory Grid", new Vector2(0, -180), new Vector2(830, 740), new Color(1f, 0.88f, 0.62f, 0.18f));
        inventoryGridRoot.GetComponent<Image>().raycastTarget = false;
        CreateInventoryGridSlots();
        CreateInventoryDetailPanel();

        inventoryMiscTabButton = CreateRuntimeButton(inventoryPopupRoot, "Inventory Misc Tab", "Misc", -260, -1000, 200, 64);
        inventoryGearTabButton = CreateRuntimeButton(inventoryPopupRoot, "Inventory Gear Tab", "Gear", 0, -1000, 200, 64);
        inventoryAllTabButton = CreateRuntimeButton(inventoryPopupRoot, "Inventory All Tab", "All", 260, -1000, 200, 64);
        inventoryCloseButton = CreateRuntimeButton(inventoryPopupRoot, "Inventory Close Button", "X", 398, -32, 58, 58);
        inventoryPopupRoot.gameObject.SetActive(false);
        RefreshInventoryPopupUi();

        fastRewardsPopupRoot = CreateRuntimePopup(homeActionRoot, "Fast Rewards Popup", new Vector2(0, -380), new Vector2(760, 370), "Fast Rewards");
        fastRewardsPopupText = CreateRuntimeText(fastRewardsPopupRoot, "Fast Rewards Body", string.Empty, 24, new Vector2(0, -95), new Vector2(660, 165));
        fastRewardsPopupText.alignment = TextAlignmentOptions.Center;
        fastRewardsPopupText.enableAutoSizing = true;
        fastRewardsPopupText.fontSizeMin = 18;
        fastRewardsPopupText.fontSizeMax = 24;
        fastRewardsPopupText.textWrappingMode = TextWrappingModes.Normal;
        fastRewardsRedeemButton = CreateRuntimeButton(fastRewardsPopupRoot, "Fast Rewards Redeem Button", "Redeem", -120, -290, 210, 58);
        fastRewardsCloseButton = CreateRuntimeButton(fastRewardsPopupRoot, "Fast Rewards Close Button", "Close", 145, -290, 160, 58);
        fastRewardsPopupRoot.gameObject.SetActive(false);

        chatPopupRoot = CreateRuntimePopup(homeActionRoot, "Chat Popup", new Vector2(-210, -590), new Vector2(560, 240), "Chat");
        var chatBodyText = CreateRuntimeText(chatPopupRoot, "Chat Body", "Chat UI kommt spaeter hier rein.\nDer Home-Button und Handler sind schon vorbereitet.", 23, new Vector2(0, -98), new Vector2(480, 92));
        chatBodyText.enableAutoSizing = true;
        chatBodyText.fontSizeMin = 17;
        chatBodyText.fontSizeMax = 23;
        chatBodyText.textWrappingMode = TextWrappingModes.Normal;
        chatCloseButton = CreateRuntimeButton(chatPopupRoot, "Chat Close Button", "Close", 0, -184, 170, 52);
        chatPopupRoot.gameObject.SetActive(false);
    }

    private void CreateInventoryGridSlots()
    {
        if (inventoryGridRoot == null)
        {
            return;
        }

        inventorySlotRoots = new RectTransform[InventoryGridSlotCount];
        inventorySlotButtons = new Button[InventoryGridSlotCount];
        inventorySlotFrames = new Image[InventoryGridSlotCount];
        inventorySlotIcons = new RawImage[InventoryGridSlotCount];
        inventorySlotCountTexts = new TMP_Text[InventoryGridSlotCount];
        inventorySlotNameTexts = new TMP_Text[InventoryGridSlotCount];
        inventorySlotDetailTexts = new TMP_Text[InventoryGridSlotCount];

        const int columns = 6;
        const float startX = -360f;
        const float startY = -12f;
        const float spacingX = 144f;
        const float spacingY = 138f;

        for (var i = 0; i < InventoryGridSlotCount; i++)
        {
            var column = i % columns;
            var row = i / columns;
            var slotRoot = CreateRuntimePanel(
                inventoryGridRoot,
                $"Inventory Slot {i + 1}",
                new Vector2(startX + (column * spacingX), startY - (row * spacingY)),
                new Vector2(116, 128),
                new Color(0.18f, 0.13f, 0.1f, 0.86f));
            inventorySlotRoots[i] = slotRoot;
            inventorySlotFrames[i] = slotRoot.GetComponent<Image>();
            inventorySlotFrames[i].raycastTarget = true;

            var slotButton = slotRoot.gameObject.AddComponent<Button>();
            var capturedSlot = i;
            slotButton.targetGraphic = inventorySlotFrames[i];
            slotButton.onClick.AddListener(() => SelectInventoryItem(capturedSlot));
            inventorySlotButtons[i] = slotButton;

            var inner = CreateRuntimePanel(slotRoot, "Inner", new Vector2(0, -8), new Vector2(96, 82), new Color(0.9f, 0.82f, 0.7f, 0.62f));
            inner.SetAsFirstSibling();

            inventorySlotIcons[i] = CreateRuntimeRawImage(slotRoot, "Icon", null, new Vector2(0, -15), new Vector2(70, 70), new Vector2(0.5f, 1f));
            inventorySlotIcons[i].raycastTarget = false;

            var countBack = CreateRuntimePanel(slotRoot, "Count Back", new Vector2(36, -68), new Vector2(52, 26), new Color(0.03f, 0.025f, 0.02f, 0.88f));
            inventorySlotCountTexts[i] = CreateRuntimeText(countBack, "Count", string.Empty, 17, Vector2.zero, new Vector2(48, 24));
            inventorySlotCountTexts[i].fontStyle = FontStyles.Bold;
            inventorySlotCountTexts[i].enableAutoSizing = true;
            inventorySlotCountTexts[i].fontSizeMin = 10;
            inventorySlotCountTexts[i].fontSizeMax = 17;
            inventorySlotCountTexts[i].color = new Color(1f, 0.9f, 0.54f);
            inventorySlotCountTexts[i].textWrappingMode = TextWrappingModes.NoWrap;

            inventorySlotNameTexts[i] = CreateRuntimeText(slotRoot, "Name", string.Empty, 14, new Vector2(0, -92), new Vector2(104, 22));
            inventorySlotNameTexts[i].fontStyle = FontStyles.Bold;
            inventorySlotNameTexts[i].enableAutoSizing = true;
            inventorySlotNameTexts[i].fontSizeMin = 9;
            inventorySlotNameTexts[i].fontSizeMax = 14;
            inventorySlotNameTexts[i].color = new Color(0.16f, 0.08f, 0.025f);
            inventorySlotNameTexts[i].textWrappingMode = TextWrappingModes.NoWrap;

            inventorySlotDetailTexts[i] = CreateRuntimeText(slotRoot, "Detail", string.Empty, 11, new Vector2(0, -112), new Vector2(106, 18));
            inventorySlotDetailTexts[i].enableAutoSizing = true;
            inventorySlotDetailTexts[i].fontSizeMin = 8;
            inventorySlotDetailTexts[i].fontSizeMax = 11;
            inventorySlotDetailTexts[i].color = new Color(0.28f, 0.17f, 0.07f);
            inventorySlotDetailTexts[i].textWrappingMode = TextWrappingModes.NoWrap;

            slotRoot.gameObject.SetActive(false);
        }
    }

    private void CreateInventoryDetailPanel()
    {
        if (inventoryPopupRoot == null || inventoryDetailRoot != null)
        {
            return;
        }

        inventoryDetailRoot = CreateRuntimePanel(inventoryPopupRoot, "Inventory Detail Panel", new Vector2(0, -362), new Vector2(760, 324), new Color(0.09f, 0.045f, 0.025f, 0.98f));
        inventoryDetailRoot.GetComponent<Image>().raycastTarget = true;
        CreateRuntimePanel(inventoryDetailRoot, "Detail Inner", new Vector2(0, -34), new Vector2(716, 268), new Color(0.96f, 0.78f, 0.46f, 0.94f));
        inventoryDetailFrame = CreateRuntimePanel(inventoryDetailRoot, "Detail Icon Frame", new Vector2(-272, -96), new Vector2(128, 128), new Color(0.38f, 0.24f, 0.13f, 0.96f)).GetComponent<Image>();
        inventoryDetailIcon = CreateRuntimeRawImage(inventoryDetailFrame.transform, "Icon", null, new Vector2(0, -18), new Vector2(88, 88), new Vector2(0.5f, 1f));
        inventoryDetailIcon.raycastTarget = false;

        inventoryDetailTitleText = CreateRuntimeText(inventoryDetailRoot, "Detail Title", string.Empty, 28, new Vector2(72, -38), new Vector2(490, 42));
        inventoryDetailTitleText.alignment = TextAlignmentOptions.Left;
        inventoryDetailTitleText.fontStyle = FontStyles.Bold;
        inventoryDetailTitleText.color = new Color(1f, 0.9f, 0.62f);
        inventoryDetailTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        inventoryDetailTitleText.enableAutoSizing = true;
        inventoryDetailTitleText.fontSizeMin = 18;
        inventoryDetailTitleText.fontSizeMax = 28;

        inventoryDetailDescriptionText = CreateRuntimeText(inventoryDetailRoot, "Detail Description", string.Empty, 20, new Vector2(110, -94), new Vector2(420, 94));
        inventoryDetailDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        inventoryDetailDescriptionText.color = new Color(0.24f, 0.12f, 0.04f);
        inventoryDetailDescriptionText.textWrappingMode = TextWrappingModes.Normal;
        inventoryDetailDescriptionText.enableAutoSizing = true;
        inventoryDetailDescriptionText.fontSizeMin = 14;
        inventoryDetailDescriptionText.fontSizeMax = 20;

        inventoryDetailStatsText = CreateRuntimeText(inventoryDetailRoot, "Detail Stats", string.Empty, 20, new Vector2(90, -204), new Vector2(560, 74));
        inventoryDetailStatsText.alignment = TextAlignmentOptions.TopLeft;
        inventoryDetailStatsText.fontStyle = FontStyles.Bold;
        inventoryDetailStatsText.color = new Color(0.13f, 0.07f, 0.03f);
        inventoryDetailStatsText.textWrappingMode = TextWrappingModes.Normal;
        inventoryDetailStatsText.enableAutoSizing = true;
        inventoryDetailStatsText.fontSizeMin = 13;
        inventoryDetailStatsText.fontSizeMax = 20;

        inventoryDetailCloseButton = CreateRuntimeButton(inventoryDetailRoot, "Detail Close Button", "X", 340, -28, 48, 48);
        inventoryDetailRoot.gameObject.SetActive(false);
    }

    private void RefreshInventoryPopupUi()
    {
        if (inventoryPopupRoot == null || inventorySlotRoots == null)
        {
            return;
        }

        var items = BuildInventoryItems();
        var visibleCount = Mathf.Min(items.Count, InventoryGridSlotCount);

        for (var i = 0; i < inventorySlotRoots.Length; i++)
        {
            var visible = i < visibleCount;
            if (inventorySlotRoots[i] == null)
            {
                continue;
            }

            inventorySlotRoots[i].gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            var item = items[i];
            if (inventorySlotFrames != null && inventorySlotFrames[i] != null)
            {
                inventorySlotFrames[i].color = selectedInventoryItemIndex == i
                    ? Color.Lerp(item.frameColor, new Color(1f, 0.92f, 0.52f, 1f), 0.48f)
                    : item.frameColor;
            }

            if (inventorySlotIcons != null && inventorySlotIcons[i] != null)
            {
                inventorySlotIcons[i].texture = LoadRuntimeTexture(item.iconTextureName);
                inventorySlotIcons[i].color = Color.white;
            }

            if (inventorySlotCountTexts != null && inventorySlotCountTexts[i] != null)
            {
                inventorySlotCountTexts[i].text = item.countText;
            }

            if (inventorySlotNameTexts != null && inventorySlotNameTexts[i] != null)
            {
                inventorySlotNameTexts[i].text = item.displayName;
            }

            if (inventorySlotDetailTexts != null && inventorySlotDetailTexts[i] != null)
            {
                inventorySlotDetailTexts[i].text = item.detail;
            }
        }

        if (selectedInventoryItemIndex >= items.Count)
        {
            selectedInventoryItemIndex = -1;
        }

        if (inventoryPopupText != null)
        {
            if (items.Count <= 0)
            {
                inventoryPopupText.text = selectedInventoryTab == InventoryTabMode.Misc
                    ? "No misc items yet. Currencies and heroes stay in their own screens."
                    : "No gear items in the bag yet. Gear Dungeon drops will appear here.";
            }
            else if (items.Count > InventoryGridSlotCount)
            {
                inventoryPopupText.text = $"{GetInventoryTabLabel(selectedInventoryTab)}  |  Showing {InventoryGridSlotCount}/{items.Count} items";
            }
            else
            {
                inventoryPopupText.text = $"{GetInventoryTabLabel(selectedInventoryTab)}  |  {items.Count} item{(items.Count == 1 ? string.Empty : "s")}";
            }
        }

        RefreshInventoryDetailPanel(items);
        StyleInventoryTab(inventoryMiscTabButton, selectedInventoryTab == InventoryTabMode.Misc);
        StyleInventoryTab(inventoryGearTabButton, selectedInventoryTab == InventoryTabMode.Gear);
        StyleInventoryTab(inventoryAllTabButton, selectedInventoryTab == InventoryTabMode.All);
    }

    private void RefreshInventoryDetailPanel(List<InventoryItemViewData> items)
    {
        if (inventoryDetailRoot == null)
        {
            return;
        }

        var hasSelection = selectedInventoryItemIndex >= 0 && selectedInventoryItemIndex < items.Count;
        inventoryDetailRoot.gameObject.SetActive(hasSelection);
        if (!hasSelection)
        {
            return;
        }

        var item = items[selectedInventoryItemIndex];
        inventoryDetailRoot.SetAsLastSibling();

        if (inventoryDetailFrame != null)
        {
            inventoryDetailFrame.color = item.frameColor;
        }

        if (inventoryDetailIcon != null)
        {
            inventoryDetailIcon.texture = LoadRuntimeTexture(item.iconTextureName);
            inventoryDetailIcon.color = Color.white;
        }

        if (inventoryDetailTitleText != null)
        {
            inventoryDetailTitleText.text = item.displayName;
        }

        if (inventoryDetailDescriptionText != null)
        {
            inventoryDetailDescriptionText.text = item.description;
        }

        if (inventoryDetailStatsText != null)
        {
            inventoryDetailStatsText.text = item.statsText;
        }
    }

    private List<InventoryItemViewData> BuildInventoryItems()
    {
        var items = new List<InventoryItemViewData>();
        if (selectedInventoryTab != InventoryTabMode.Gear)
        {
            AddMiscInventoryItems(items);
        }

        if (selectedInventoryTab != InventoryTabMode.Misc)
        {
            AddGearInventoryItems(items);
        }

        return items;
    }

    private void AddMiscInventoryItems(List<InventoryItemViewData> items)
    {
        if (battlePassXp > 0)
        {
            items.Add(new InventoryItemViewData(
                "Mission XP",
                "Mission Track",
                "Progress earned from daily missions. It advances the Mission Track and unlocks claimable reward steps.",
                $"Stored XP: {FormatCompactNumber(battlePassXp)}\nVisible in: Quests & Systems\nSources: daily mission claims",
                FormatCompactNumber(battlePassXp),
                "vfx_summon",
                new Color(0.48f, 0.32f, 0.78f, 0.96f)));
        }
    }

    private void AddGearInventoryItems(List<InventoryItemViewData> items)
    {
        EnsureAccessories();
        EnsureHeroEquipment();

        var heroIndex = GetSelectedHeroIndex();
        var heroName = GetHeroDefinition(heroIndex).name;
        var displayedWeaponLevel = GetEquipmentDisplayLevel(WeaponTrack, GetHeroEquipmentLevel(heroIndex, isWeapon: true));
        var displayedArmorLevel = GetEquipmentDisplayLevel(ArmorTrack, GetHeroEquipmentLevel(heroIndex, isWeapon: false));

        items.Add(new InventoryItemViewData(
            WeaponTrack.name,
            $"{heroName} +{GetHeroEquipmentAttackBonus(heroIndex)} {WeaponTrack.statLabel}",
            $"The active weapon track for {heroName}. Upgrade it from the Gear screen to raise this hero's attack.",
            $"Level {FormatCappedValue(displayedWeaponLevel, GetEquipmentLevelCap(WeaponTrack))}\nATK +{GetHeroEquipmentAttackBonus(heroIndex)}\nNext upgrade: {GetWeaponUpgradeCost()} Gold",
            $"Lv {displayedWeaponLevel}",
            "icon_weapon",
            new Color(0.58f, 0.36f, 0.18f, 0.96f)));

        items.Add(new InventoryItemViewData(
            ArmorTrack.name,
            $"{heroName} +{GetHeroEquipmentHealthBonus(heroIndex)} {ArmorTrack.statLabel}",
            $"The active armor track for {heroName}. Upgrade it from the Gear screen to improve survivability.",
            $"Level {FormatCappedValue(displayedArmorLevel, GetEquipmentLevelCap(ArmorTrack))}\nHP +{GetHeroEquipmentHealthBonus(heroIndex)}\nNext upgrade: {GetArmorUpgradeCost()} Gold",
            $"Lv {displayedArmorLevel}",
            "icon_armor",
            new Color(0.36f, 0.38f, 0.42f, 0.96f)));

        for (var rarity = AccessoryRarityCount - 1; rarity >= 0; rarity--)
        {
            for (var slot = 0; slot < AccessorySlotCount; slot++)
            {
                var copies = GetAccessoryInventoryCount(slot, rarity);
                var equipped = CountEquippedAccessoryCopies(slot, rarity);
                var total = copies + equipped;
                if (total <= 0)
                {
                    continue;
                }

                var detail = equipped > 0
                    ? $"Bag {copies}  Equip {equipped}"
                    : $"Bag {copies}";
                items.Add(new InventoryItemViewData(
                    $"{GetAccessoryRarityName(rarity)} {AccessorySlots[slot].name}",
                    detail,
                    "Accessory loot from Gear Dungeon. Copies can be equipped on heroes or fused upward from the Hero gear panel.",
                    GetAccessoryInventoryStatsText(slot, rarity, copies, equipped),
                    total.ToString(),
                    GetInventoryAccessoryIconTextureName(slot),
                    GetAccessoryRarityColor(rarity)));
            }
        }
    }

    private int CountEquippedAccessoryCopies(int slot, int rarity)
    {
        EnsureAccessories();
        var count = 0;
        for (var heroIndex = 0; heroIndex < HeroCount; heroIndex++)
        {
            if (GetHeroEquippedAccessoryRarity(heroIndex, slot) == rarity)
            {
                count++;
            }
        }

        return count;
    }

    private string GetAccessoryInventoryStatsText(int slot, int rarity, int copies, int equipped)
    {
        var maxLevel = GetAccessoryMaxLevel(rarity);
        var attackAtLevelOne = GetAccessoryAttackFor(slot, rarity, 1);
        var healthAtLevelOne = GetAccessoryHealthFor(slot, rarity, 1);
        var attackAtMax = GetAccessoryAttackFor(slot, rarity, maxLevel);
        var healthAtMax = GetAccessoryHealthFor(slot, rarity, maxLevel);
        var fuseCost = GetAccessoryFuseCost(rarity);
        var fuseLine = rarity >= AccessoryRarityCount - 1
            ? "Fuse: max rarity"
            : $"Fuse: {fuseCost} copies -> {GetAccessoryRarityName(rarity + 1)}";

        return $"Owned {copies + equipped}  |  Bag {copies}  |  Equipped {equipped}\n" +
               $"Lv 1: +{attackAtLevelOne} ATK, +{healthAtLevelOne} HP\n" +
               $"Max Lv {maxLevel}: +{attackAtMax} ATK, +{healthAtMax} HP\n" +
               fuseLine;
    }

    private static string GetInventoryAccessoryIconTextureName(int slot)
    {
        switch (Mathf.Clamp(slot, 0, AccessorySlotCount - 1))
        {
            case 0:
                return "dungeon_essence";
            case 1:
                return "dungeon_portal";
            case 2:
                return "icon_armor";
            case 3:
                return "icon_weapon";
            default:
                return "dungeon_fire";
        }
    }

    private static string GetInventoryTabLabel(InventoryTabMode tab)
    {
        switch (tab)
        {
            case InventoryTabMode.Misc:
                return "Misc";
            case InventoryTabMode.Gear:
                return "Gear";
            default:
                return "All";
        }
    }

    private static void StyleInventoryTab(Button button, bool active)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = active
                ? new Color(0.79f, 0.42f, 0.14f, 0.98f)
                : new Color(0.2f, 0.11f, 0.055f, 0.95f);
        }

        var text = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (text != null)
        {
            text.fontSize = active ? 25 : 22;
            text.fontSizeMin = 14;
            text.fontSizeMax = active ? 25 : 22;
            text.enableAutoSizing = true;
            text.fontStyle = FontStyles.Bold;
            text.color = active ? new Color(1f, 0.9f, 0.62f) : new Color(0.86f, 0.72f, 0.52f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }
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
        EnsureRuntimeHeroSelectButtons();
        if (heroSelectButtons == null || (heroCardPortraits != null && heroCardPortraits.Length == heroSelectButtons.Length && heroCardShardFills != null && heroCardShardFills.Length == heroSelectButtons.Length))
        {
            return;
        }

        heroCardPortraits = new RawImage[heroSelectButtons.Length];
        heroCardLevelTexts = new TMP_Text[heroSelectButtons.Length];
        heroCardStarTexts = new TMP_Text[heroSelectButtons.Length];
        heroCardShardTexts = new TMP_Text[heroSelectButtons.Length];
        heroCardRoleBadgeTexts = new TMP_Text[heroSelectButtons.Length];
        heroCardTeamBadgeTexts = new TMP_Text[heroSelectButtons.Length];
        heroCardShardFills = new Image[heroSelectButtons.Length];
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
            }
            else
            {
                var portraitMat = CreateRuntimePanel(button.transform, "Runtime Hero Portrait Matte", new Vector2(0, -10), new Vector2(138, 148), new Color(1f, 1f, 1f, 0.13f));
                portraitMat.SetAsFirstSibling();

                var portraitObject = new GameObject("Runtime Hero Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                portraitObject.transform.SetParent(button.transform, false);
                var portraitRect = portraitObject.GetComponent<RectTransform>();
                SetRuntimeRect(portraitRect, new Vector2(0, -12), new Vector2(132, 132), new Vector2(0.5f, 1f));
                portraitObject.transform.SetSiblingIndex(1);

                var portrait = portraitObject.GetComponent<RawImage>();
                portrait.raycastTarget = false;
                portrait.color = Color.white;
                heroCardPortraits[i] = portrait;
            }

            heroCardRoleBadgeTexts[i] = CreateHeroCardBadge(button.transform, "Runtime Hero Role Badge", new Vector2(-48, -8), "M");
            heroCardTeamBadgeTexts[i] = CreateHeroCardBadge(button.transform, "Runtime Hero Team Badge", new Vector2(48, -8), "T");

            heroCardLevelTexts[i] = CreateRuntimeText(button.transform, "Runtime Hero Level", string.Empty, 22, new Vector2(0, -112), new Vector2(128, 30));
            heroCardLevelTexts[i].fontStyle = FontStyles.Bold;
            heroCardLevelTexts[i].color = new Color(1f, 0.86f, 0.24f);
            heroCardLevelTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            heroCardLevelTexts[i].raycastTarget = false;

            heroCardStarTexts[i] = CreateRuntimeText(button.transform, "Runtime Hero Stars", string.Empty, 20, new Vector2(0, -142), new Vector2(132, 24));
            heroCardStarTexts[i].fontStyle = FontStyles.Bold;
            heroCardStarTexts[i].color = new Color(1f, 0.76f, 0.23f);
            heroCardStarTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            heroCardStarTexts[i].raycastTarget = false;

            var shardBack = CreateRuntimePanel(button.transform, "Runtime Hero Shard Back", new Vector2(0, -178), new Vector2(132, 26), new Color(0.02f, 0.025f, 0.025f, 0.86f));
            var fillObject = new GameObject("Runtime Hero Shard Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(shardBack, false);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            heroCardShardFills[i] = fillObject.GetComponent<Image>();
            heroCardShardFills[i].color = new Color(0.08f, 0.78f, 0.86f, 0.96f);
            heroCardShardFills[i].raycastTarget = false;

            heroCardShardTexts[i] = CreateRuntimeText(shardBack, "Runtime Hero Shards", string.Empty, 16, Vector2.zero, new Vector2(126, 26));
            heroCardShardTexts[i].fontStyle = FontStyles.Bold;
            heroCardShardTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            heroCardShardTexts[i].raycastTarget = false;
        }
    }

    private void EnsureRuntimeHeroSelectButtons()
    {
        if (heroesPanel == null)
        {
            return;
        }

        var existingButtonCount = heroSelectButtons == null ? 0 : heroSelectButtons.Length;
        if (existingButtonCount >= HeroCount && heroCardTexts != null && heroCardTexts.Length >= HeroCount)
        {
            return;
        }

        var nextButtons = new Button[HeroCount];
        var nextTexts = heroCardTexts != null || existingButtonCount > 0 ? new TMP_Text[HeroCount] : null;
        for (var i = 0; i < HeroCount; i++)
        {
            if (heroSelectButtons != null && i < heroSelectButtons.Length)
            {
                nextButtons[i] = heroSelectButtons[i];
            }

            if (nextTexts != null && heroCardTexts != null && i < heroCardTexts.Length)
            {
                nextTexts[i] = heroCardTexts[i];
            }
        }

        for (var i = existingButtonCount; i < HeroCount; i++)
        {
            var button = CreateRuntimeButton(heroesPanel.transform, $"Runtime Hero Select Button {i + 1}", string.Empty, 0, 0, 154, 226);
            var label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (label != null)
            {
                label.text = string.Empty;
                label.gameObject.SetActive(false);
            }

            nextButtons[i] = button;
            if (nextTexts != null)
            {
                nextTexts[i] = label;
            }
        }

        heroSelectButtons = nextButtons;
        if (nextTexts != null)
        {
            heroCardTexts = nextTexts;
        }
    }

    private static TMP_Text CreateHeroCardBadge(Transform parent, string name, Vector2 anchoredPosition, string label)
    {
        var badge = CreateRuntimePanel(parent, name, anchoredPosition, new Vector2(42, 34), new Color(0.04f, 0.48f, 0.46f, 0.94f));
        var text = CreateRuntimeText(badge, "Label", label, 18, new Vector2(0, -3), new Vector2(38, 24));
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private void EnsureRuntimeHeroEssenceCounter()
    {
        if (heroesPanel == null || heroEssenceAmountText != null)
        {
            return;
        }

        var chip = CreateRuntimePanel(heroesPanel.transform, "Hero Essence Counter", new Vector2(292, -248), new Vector2(245, 52), new Color(0.08f, 0.035f, 0.08f, 0.84f));
        heroEssenceIconImage = CreateRuntimeRawImage(chip, "Hero Essence Icon", GetCurrencyIconTexture("exp_shard"), new Vector2(-96, -26), new Vector2(42, 55), new Vector2(0.5f, 1f));
        heroEssenceAmountText = CreateRuntimeText(chip, "Hero Essence Amount Text", "0", 22, new Vector2(18, -12), new Vector2(170, 34));
        heroEssenceAmountText.alignment = TextAlignmentOptions.Left;
        heroEssenceAmountText.fontStyle = FontStyles.Bold;
        heroEssenceAmountText.textWrappingMode = TextWrappingModes.NoWrap;
        heroEssenceAmountText.enableAutoSizing = true;
        heroEssenceAmountText.fontSizeMin = 16;
        heroEssenceAmountText.fontSizeMax = 22;
    }

    private void EnsureRuntimeHeroesTabs()
    {
        if (heroesPanel == null || heroRosterTabButton != null)
        {
            return;
        }

        var oldBackdrop = heroesPanel.transform.Find("Heroes Parchment Backdrop");
        if (oldBackdrop != null)
        {
            oldBackdrop.gameObject.SetActive(false);
        }
        HideLegacyHeroesOverviewElements();

        heroCleanBackdropRoot = CreateRuntimePanel(heroesPanel.transform, "Heroes Clean Backdrop", new Vector2(0, -176), new Vector2(1080, 1040), new Color(0.045f, 0.055f, 0.068f, 0.98f));
        heroCleanBackdropRoot.SetAsFirstSibling();

        heroRosterFilterRoot = CreateRuntimePanel(heroesPanel.transform, "Hero Roster Filter Bar", new Vector2(0, -186), new Vector2(1080, 104), new Color(0.09f, 0.28f, 0.34f, 0.96f));
        heroRosterCountText = CreateRuntimeText(heroRosterFilterRoot, "Hero Count", "5/5", 34, new Vector2(-405, -24), new Vector2(210, 56));
        heroRosterCountText.alignment = TextAlignmentOptions.Left;
        heroRosterCountText.fontStyle = FontStyles.Bold;
        heroRosterCountText.textWrappingMode = TextWrappingModes.NoWrap;

        heroSortToggleButton = CreateRuntimeButton(heroRosterFilterRoot, "Hero Sort Direction Button", "Desc", -130, -20, 142, 58);
        heroSortToggleText = heroSortToggleButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
        heroAttackTypeFilterButton = CreateRuntimeButton(heroRosterFilterRoot, "Hero Attack Type Filter Button", "Alle", 250, -20, 300, 58);
        heroAttackTypeFilterText = heroAttackTypeFilterButton.GetComponentInChildren<TMP_Text>(includeInactive: true);

        heroSubTabRoot = CreateRuntimePanel(heroesPanel.transform, "Hero Sub Tab Bar", new Vector2(0, -1050), new Vector2(820, 82), new Color(0.55f, 0.52f, 0.45f, 0.92f));
        heroRosterTabButton = CreateRuntimeButton(heroSubTabRoot, "Heroes Roster Tab", "Held", -206, -9, 290, 64);
        heroSetTeamTabButton = CreateRuntimeButton(heroSubTabRoot, "Heroes Set Team Tab", "Team festlegen", 136, -9, 360, 64);
        heroRosterTabText = heroRosterTabButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
        heroSetTeamTabText = heroSetTeamTabButton.GetComponentInChildren<TMP_Text>(includeInactive: true);

        var rootObject = new GameObject("Heroes Set Team Root", typeof(RectTransform));
        rootObject.transform.SetParent(heroesPanel.transform, false);
        heroTeamRoot = rootObject.GetComponent<RectTransform>();
        SetRuntimeRect(heroTeamRoot, new Vector2(0, -288), new Vector2(900, 350), new Vector2(0.5f, 1f));

        var back = CreateRuntimePanel(heroTeamRoot, "Team Backplate", Vector2.zero, new Vector2(900, 350), new Color(0.055f, 0.075f, 0.095f, 0.92f));
        back.SetAsFirstSibling();

        heroTeamHintText = CreateRuntimeText(heroTeamRoot, "Team Hint", string.Empty, 19, new Vector2(0, -18), new Vector2(740, 38));
        heroTeamHintText.color = new Color(0.78f, 0.9f, 1f);
        heroTeamHintText.enableAutoSizing = true;
        heroTeamHintText.fontSizeMin = 13;
        heroTeamHintText.fontSizeMax = 19;

        heroAutoSetTeamButton = CreateRuntimeButton(heroTeamRoot, "Auto Set Team Button", "Auto-Set", 308, -288, 172, 48);

        heroTeamSlotPortraits = new RawImage[HeroCount];
        heroTeamSlotTexts = new TMP_Text[HeroCount];
        heroTeamSlotFrames = new Image[HeroCount];
        heroTeamSlotButtons = new Button[HeroCount];
        const float spacing = 154f;
        var startX = -((HeroCount - 1) * spacing * 0.5f);
        for (var i = 0; i < HeroCount; i++)
        {
            var slotObject = new GameObject($"Hero Team Slot {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            slotObject.transform.SetParent(heroTeamRoot, false);
            SetRuntimeRect(slotObject.GetComponent<RectTransform>(), new Vector2(startX + spacing * i, -82), new Vector2(136, 186), new Vector2(0.5f, 1f));

            var frame = slotObject.GetComponent<Image>();
            frame.color = new Color(0.1f, 0.14f, 0.19f, 0.82f);
            heroTeamSlotFrames[i] = frame;

            var button = slotObject.GetComponent<Button>();
            button.targetGraphic = frame;
            heroTeamSlotButtons[i] = button;

            heroTeamSlotPortraits[i] = CreateRuntimeRawImage(slotObject.transform, "Portrait", LoadCombatTexture(GetHeroTextureName(i), "idle", 0, GetHeroTextureName(i)), new Vector2(0, -12), new Vector2(104, 112), new Vector2(0.5f, 1f));
            heroTeamSlotPortraits[i].raycastTarget = false;
            heroTeamSlotTexts[i] = CreateRuntimeText(slotObject.transform, "Label", string.Empty, 16, new Vector2(0, -128), new Vector2(124, 44));
            heroTeamSlotTexts[i].fontStyle = FontStyles.Bold;
            heroTeamSlotTexts[i].enableAutoSizing = true;
            heroTeamSlotTexts[i].fontSizeMin = 10;
            heroTeamSlotTexts[i].fontSizeMax = 16;
            heroTeamSlotTexts[i].raycastTarget = false;
        }

        heroTeamRoot.gameObject.SetActive(false);
    }

    private void EnsureRuntimeHeroDetailWindow()
    {
        if (heroesPanel == null || heroDetailRoot != null)
        {
            return;
        }

        heroDetailRoot = CreateRuntimePanel(heroesPanel.transform, "Hero Detail Window", new Vector2(0, -118), new Vector2(860, 1186), new Color(0.07f, 0.035f, 0.022f, 0.98f));
        heroDetailRoot.SetAsLastSibling();

        var rootImage = heroDetailRoot.GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.raycastTarget = true;
        }

        CreateLayeredRuntimeBackground(heroDetailRoot, new Vector2(820, 680), 0.18f);
        CreateRuntimePanel(heroDetailRoot, "Hero Detail Top Glow", new Vector2(0, -38), new Vector2(520, 10), new Color(0.08f, 0.78f, 1f, 0.72f));
        CreateRuntimePanel(heroDetailRoot, "Hero Detail Name Backplate", new Vector2(0, -58), new Vector2(520, 126), new Color(0.11f, 0.05f, 0.035f, 0.58f));
        CreateRuntimePanel(heroDetailRoot, "Hero Detail Stage", new Vector2(0, -210), new Vector2(460, 430), new Color(0.95f, 0.55f, 0.24f, 0.15f));
        CreateRuntimePanel(heroDetailRoot, "Hero Detail Stat Backplate", new Vector2(0, -724), new Vector2(780, 160), new Color(0.1f, 0.045f, 0.035f, 0.78f));

        heroDetailRarityText = CreateRuntimeText(heroDetailRoot, "Hero Detail Rarity", string.Empty, 30, new Vector2(0, -47), new Vector2(500, 34));
        heroDetailRarityText.fontStyle = FontStyles.Bold;
        heroDetailRarityText.textWrappingMode = TextWrappingModes.NoWrap;

        heroDetailTitleText = CreateRuntimeText(heroDetailRoot, "Hero Detail Title", string.Empty, 27, new Vector2(0, -82), new Vector2(520, 34));
        heroDetailTitleText.fontStyle = FontStyles.Bold;
        heroDetailTitleText.color = new Color(1f, 0.88f, 0.64f);
        heroDetailTitleText.textWrappingMode = TextWrappingModes.NoWrap;

        heroDetailNameText = CreateRuntimeText(heroDetailRoot, "Hero Detail Name", string.Empty, 34, new Vector2(0, -118), new Vector2(520, 42));
        heroDetailNameText.fontStyle = FontStyles.Bold;
        heroDetailNameText.textWrappingMode = TextWrappingModes.NoWrap;

        heroDetailPortrait = CreateRuntimeRawImage(heroDetailRoot, "Hero Detail Portrait", LoadCombatTexture(GetHeroTextureName(selectedHeroIndex), "idle", 0, GetHeroTextureName(selectedHeroIndex)), new Vector2(0, -265), new Vector2(315, 315), new Vector2(0.5f, 1f));

        heroDetailGearSlotTexts = new TMP_Text[6];
        heroDetailGearSlotFrames = new Image[6];
        heroDetailGearSlotButtons = new Button[6];
        for (var i = 0; i < heroDetailGearSlotTexts.Length; i++)
        {
            var side = i < 3 ? -1f : 1f;
            var row = i % 3;
            var slotRoot = CreateRuntimePanel(heroDetailRoot, $"Hero Detail Gear Slot {i + 1}", new Vector2(side * 340f, -205f - row * 145f), new Vector2(114, 114), new Color(0.07f, 0.035f, 0.025f, 0.92f));
            heroDetailGearSlotFrames[i] = slotRoot.GetComponent<Image>();
            heroDetailGearSlotFrames[i].raycastTarget = true;
            heroDetailGearSlotButtons[i] = slotRoot.gameObject.AddComponent<Button>();
            heroDetailGearSlotButtons[i].targetGraphic = heroDetailGearSlotFrames[i];
            var inner = CreateRuntimePanel(slotRoot, "Inner", new Vector2(0, -9), new Vector2(90, 82), new Color(0.15f, 0.075f, 0.04f, 0.82f));
            inner.SetAsFirstSibling();
            heroDetailGearSlotTexts[i] = CreateRuntimeText(slotRoot, "Label", string.Empty, 15, new Vector2(0, -26), new Vector2(100, 66));
            heroDetailGearSlotTexts[i].fontStyle = FontStyles.Bold;
            heroDetailGearSlotTexts[i].enableAutoSizing = true;
            heroDetailGearSlotTexts[i].fontSizeMin = 10;
            heroDetailGearSlotTexts[i].fontSizeMax = 15;
            heroDetailGearSlotTexts[i].raycastTarget = false;
        }

        heroDetailPowerText = CreateRuntimeText(heroDetailRoot, "Hero Detail Power", string.Empty, 31, new Vector2(0, -668), new Vector2(520, 44));
        heroDetailPowerText.fontStyle = FontStyles.Bold;
        heroDetailPowerText.color = new Color(1f, 0.82f, 0.34f);
        heroDetailPowerText.textWrappingMode = TextWrappingModes.NoWrap;

        heroDetailStatsText = CreateRuntimeText(heroDetailRoot, "Hero Detail Stats", string.Empty, 23, new Vector2(0, -734), new Vector2(780, 84));
        heroDetailStatsText.fontStyle = FontStyles.Bold;
        heroDetailStatsText.enableAutoSizing = true;
        heroDetailStatsText.fontSizeMin = 16;
        heroDetailStatsText.fontSizeMax = 23;

        heroDetailResourceText = CreateRuntimeText(heroDetailRoot, "Hero Detail Resources", string.Empty, 20, new Vector2(0, -835), new Vector2(780, 40));
        heroDetailResourceText.color = new Color(0.82f, 0.9f, 1f);
        heroDetailResourceText.enableAutoSizing = true;
        heroDetailResourceText.fontSizeMin = 14;
        heroDetailResourceText.fontSizeMax = 20;
        heroDetailResourceText.textWrappingMode = TextWrappingModes.NoWrap;

        heroDetailPreviousButton = CreateRuntimeButton(heroDetailRoot, "Hero Detail Previous Button", "<", -360, -605, 76, 72);
        heroDetailNextButton = CreateRuntimeButton(heroDetailRoot, "Hero Detail Next Button", ">", 360, -605, 76, 72);
        heroDetailRemoveGearButton = CreateRuntimeButton(heroDetailRoot, "Hero Detail Remove Gear Button", "Remove Gear", -250, -898, 210, 62);
        heroDetailLevelButton = CreateRuntimeButton(heroDetailRoot, "Hero Detail Level Button", "Level Up", 0, -902, 260, 74);
        heroDetailEquipGearButton = CreateRuntimeButton(heroDetailRoot, "Hero Detail Equip Gear Button", "Equip Gear", 250, -898, 210, 62);

        var tabBack = CreateRuntimePanel(heroDetailRoot, "Hero Detail Tabs Backplate", new Vector2(0, -1000), new Vector2(640, 78), new Color(0.045f, 0.027f, 0.02f, 0.86f));
        CreateRuntimeText(tabBack, "Story Tab", "Story", 22, new Vector2(-210, -18), new Vector2(160, 44)).color = new Color(0.86f, 0.72f, 0.52f);
        var heroTabText = CreateRuntimeText(tabBack, "Hero Tab", "Hero", 24, new Vector2(0, -17), new Vector2(160, 44));
        heroTabText.fontStyle = FontStyles.Bold;
        heroTabText.color = new Color(1f, 0.93f, 0.68f);
        CreateRuntimeText(tabBack, "Skills Tab", "Skills", 22, new Vector2(210, -18), new Vector2(160, 44)).color = new Color(0.86f, 0.72f, 0.52f);

        heroDetailCloseButton = CreateRuntimeButton(heroDetailRoot, "Hero Detail Close Button", "X", 385, -24, 52, 52);
        CreateHeroDetailGearList();
        heroDetailRoot.gameObject.SetActive(false);
    }

    private void CreateHeroDetailGearList()
    {
        heroDetailGearListRoot = CreateRuntimePanel(heroDetailRoot, "Hero Detail Gear List", new Vector2(0, -525), new Vector2(730, 390), new Color(0.045f, 0.028f, 0.02f, 0.98f));
        heroDetailGearListRoot.SetAsLastSibling();
        var listImage = heroDetailGearListRoot.GetComponent<Image>();
        if (listImage != null)
        {
            listImage.raycastTarget = true;
        }

        CreateRuntimePanel(heroDetailGearListRoot, "Divider", new Vector2(0, -62), new Vector2(660, 3), new Color(0.86f, 0.58f, 0.27f, 0.88f));
        heroDetailGearListTitleText = CreateRuntimeText(heroDetailGearListRoot, "Title", string.Empty, 27, new Vector2(0, -20), new Vector2(610, 40));
        heroDetailGearListTitleText.fontStyle = FontStyles.Bold;
        heroDetailGearListTitleText.color = new Color(1f, 0.88f, 0.58f);
        heroDetailGearListTitleText.textWrappingMode = TextWrappingModes.NoWrap;

        heroDetailGearListCloseButton = CreateRuntimeButton(heroDetailGearListRoot, "Close", "X", 326, -18, 46, 46);
        heroDetailGearOptionButtons = new Button[AccessoryRarityCount];
        heroDetailGearOptionTexts = new TMP_Text[AccessoryRarityCount];
        for (var rarity = 0; rarity < AccessoryRarityCount; rarity++)
        {
            var option = CreateRuntimeButton(heroDetailGearListRoot, $"Gear Option {rarity + 1}", string.Empty, 0, -92 - rarity * 56, 640, 48);
            var image = option.GetComponent<Image>();
            if (image != null)
            {
                image.color = GetAccessoryRarityColor(rarity);
            }

            var label = option.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (label != null)
            {
                label.fontSize = 18;
                label.fontSizeMin = 11;
                label.fontSizeMax = 18;
                label.enableAutoSizing = true;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.alignment = TextAlignmentOptions.Center;
            }

            heroDetailGearOptionButtons[rarity] = option;
            heroDetailGearOptionTexts[rarity] = label;
        }

        heroDetailGearListRoot.gameObject.SetActive(false);
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
        if (dungeonsPanel == null)
        {
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
        }

        if (dungeonsHeaderText == null)
        {
            dungeonsHeaderText = CreateRuntimeText(dungeonsPanel.transform, "Dungeons Header", "Dungeons", 36, new Vector2(0, -132), new Vector2(860, 54));
            dungeonsHeaderText.fontStyle = FontStyles.Bold;
            dungeonsHeaderText.color = new Color(1f, 0.9f, 0.68f);
        }

        EnsureRuntimeDungeonCard(
            ref goldDungeonButton,
            ref goldDungeonBannerImage,
            ref goldDungeonBossImage,
            ref goldDungeonTitleText,
            ref goldDungeonProgressText,
            ref goldDungeonText,
            "Gold Dungeon Card",
            "gold_dungeon_set_banner",
            GoldDungeonDefinition.dungeonId);
        EnsureRuntimeDungeonCard(
            ref essenceDungeonButton,
            ref essenceDungeonBannerImage,
            ref essenceDungeonBossImage,
            ref essenceDungeonTitleText,
            ref essenceDungeonProgressText,
            ref essenceDungeonText,
            "Essence Dungeon Card",
            "essence_dungeon_set_banner",
            EssenceDungeonDefinition.dungeonId);
        EnsureRuntimeDungeonCard(
            ref gearDungeonButton,
            ref gearDungeonBannerImage,
            ref gearDungeonBossImage,
            ref gearDungeonTitleText,
            ref gearDungeonProgressText,
            ref gearDungeonText,
            "Gear Dungeon Card",
            "gear_dungeon_set_banner",
            GearDungeonDefinition.dungeonId);

        if (runtimeDungeonResultText == null)
        {
            runtimeDungeonResultText = CreateRuntimeText(dungeonsPanel.transform, "Dungeon Result Text", "Dungeons are the active resource source.", 22, new Vector2(0, -1018), new Vector2(760, 52));
        }

        runtimeDungeonResultText.color = new Color(0.78f, 0.9f, 1f);
        runtimeDungeonResultText.enableAutoSizing = true;
        runtimeDungeonResultText.fontSizeMin = 16;
        runtimeDungeonResultText.fontSizeMax = 22;
        runtimeDungeonResultText.fontStyle = FontStyles.Bold;
    }

    private void EnsureRuntimeDungeonCard(
        ref Button button,
        ref RawImage bannerImage,
        ref RawImage bossImage,
        ref TMP_Text titleText,
        ref TMP_Text progressText,
        ref TMP_Text detailText,
        string cardName,
        string bannerTextureName,
        string dungeonId)
    {
        if (dungeonsPanel == null)
        {
            return;
        }

        if (button == null)
        {
            button = CreateRuntimeButton(dungeonsPanel.transform, cardName, string.Empty, 0f, 0f, 850f, 220f);
        }
        else
        {
            button.transform.SetParent(dungeonsPanel.transform, false);
        }

        ApplyDungeonCardButtonSkin(button);
        if (bannerImage == null)
        {
            var bannerTexture = LoadRuntimeTexture(bannerTextureName);
            if (bannerTexture != null)
            {
                bannerTexture.filterMode = FilterMode.Bilinear;
            }

            bannerImage = CreateRuntimeRawImage(button.transform, "Dungeon Banner Art", bannerTexture, Vector2.zero, new Vector2(850f, 220f), new Vector2(0.5f, 0.5f));
            bannerImage.SetNativeSize();
            SetRuntimeRect(bannerImage.rectTransform, Vector2.zero, new Vector2(850f, 220f), new Vector2(0.5f, 0.5f));
            bannerImage.transform.SetAsFirstSibling();

            var topShade = CreateRuntimePanel(button.transform, "Dungeon Banner Top Shade", new Vector2(0f, -34f), new Vector2(830f, 68f), new Color(0.08f, 0.035f, 0.02f, 0.68f));
            topShade.SetAsLastSibling();
            var bottomShade = CreateRuntimePanel(button.transform, "Dungeon Banner Progress Shade", new Vector2(0f, -182f), new Vector2(830f, 64f), new Color(0.035f, 0.025f, 0.02f, 0.74f));
            bottomShade.SetAsLastSibling();

            CreateDungeonCardFrame(button.transform);
        }

        if (bossImage == null)
        {
            var bossTextureName = GetDungeonBossTextureName(dungeonId);
            bossImage = CreateRuntimeRawImage(button.transform, "Dungeon Boss Accent", LoadCombatTexture(bossTextureName, "idle", 0, bossTextureName), new Vector2(312f, -125f), new Vector2(116f, 116f), new Vector2(0.5f, 1f));
            bossImage.color = new Color(1f, 1f, 1f, 0.92f);
            bossImage.rectTransform.localScale = new Vector3(GetEnemyFacingScale(bossTextureName), 1f, 1f);
        }

        if (titleText == null)
        {
            titleText = CreateRuntimeText(button.transform, "Dungeon Set Title", string.Empty, 32, new Vector2(0f, -23f), new Vector2(760f, 48));
        }

        titleText.transform.SetParent(button.transform, false);
        StyleDungeonCardTitle(titleText);

        if (progressText == null)
        {
            progressText = CreateRuntimeText(button.transform, "Dungeon Set Progress", string.Empty, 25, new Vector2(-104f, -176f), new Vector2(512f, 38));
        }

        progressText.transform.SetParent(button.transform, false);
        StyleDungeonCardProgress(progressText);

        if (detailText == null)
        {
            detailText = CreateRuntimeText(button.transform, "Dungeon Set Detail", string.Empty, 17, new Vector2(-110f, -202f), new Vector2(560f, 26));
        }

        detailText.transform.SetParent(button.transform, false);
        StyleDungeonCardDetail(detailText);
    }

    private void ApplyDungeonCardsButtonSkin()
    {
        ApplyDungeonCardButtonSkin(goldDungeonButton);
        ApplyDungeonCardButtonSkin(essenceDungeonButton);
        ApplyDungeonCardButtonSkin(gearDungeonButton);
    }

    private static void ApplyDungeonCardButtonSkin(Button button)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = new Color(0f, 0f, 0f, 0.01f);
        image.raycastTarget = true;
        button.targetGraphic = image;
    }

    private static void CreateDungeonCardFrame(Transform parent)
    {
        var outer = new Color(0.73f, 0.42f, 0.16f, 1f);
        var inner = new Color(1f, 0.78f, 0.36f, 0.92f);
        var corner = new Color(0.75f, 0.07f, 0.05f, 1f);

        CreateRuntimePanel(parent, "Dungeon Frame Top", new Vector2(0f, -4f), new Vector2(850f, 8f), outer).SetAsLastSibling();
        CreateRuntimePanel(parent, "Dungeon Frame Bottom", new Vector2(0f, -216f), new Vector2(850f, 8f), outer).SetAsLastSibling();
        CreateRuntimePanel(parent, "Dungeon Frame Left", new Vector2(-421f, -110f), new Vector2(8f, 220f), outer).SetAsLastSibling();
        CreateRuntimePanel(parent, "Dungeon Frame Right", new Vector2(421f, -110f), new Vector2(8f, 220f), outer).SetAsLastSibling();
        CreateRuntimePanel(parent, "Dungeon Frame Inner Top", new Vector2(0f, -13f), new Vector2(826f, 3f), inner).SetAsLastSibling();
        CreateRuntimePanel(parent, "Dungeon Frame Inner Bottom", new Vector2(0f, -207f), new Vector2(826f, 3f), inner).SetAsLastSibling();
        CreateRuntimePanel(parent, "Dungeon Corner Top Left", new Vector2(-408f, -18f), new Vector2(22f, 22f), corner).SetAsLastSibling();
        CreateRuntimePanel(parent, "Dungeon Corner Top Right", new Vector2(408f, -18f), new Vector2(22f, 22f), corner).SetAsLastSibling();
        CreateRuntimePanel(parent, "Dungeon Corner Bottom Left", new Vector2(-408f, -202f), new Vector2(22f, 22f), corner).SetAsLastSibling();
        CreateRuntimePanel(parent, "Dungeon Corner Bottom Right", new Vector2(408f, -202f), new Vector2(22f, 22f), corner).SetAsLastSibling();
    }

    private static void StyleDungeonCardTitle(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        SetRuntimeRect(text.rectTransform, new Vector2(0f, -23f), new Vector2(760f, 48f), new Vector2(0.5f, 1f));
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.9f, 0.68f);
        text.fontSize = 32;
        text.fontSizeMin = 24;
        text.fontSizeMax = 32;
        text.enableAutoSizing = true;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineColor = new Color(0.1f, 0.04f, 0.01f, 0.95f);
        text.outlineWidth = 0.22f;
        text.transform.SetAsLastSibling();
    }

    private static void StyleDungeonCardProgress(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        SetRuntimeRect(text.rectTransform, new Vector2(-104f, -176f), new Vector2(512f, 38f), new Vector2(0.5f, 1f));
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.9f, 0.68f);
        text.fontSize = 25;
        text.fontSizeMin = 18;
        text.fontSizeMax = 25;
        text.enableAutoSizing = true;
        text.fontStyle = FontStyles.Bold;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineColor = new Color(0.1f, 0.04f, 0.01f, 0.95f);
        text.outlineWidth = 0.18f;
        text.transform.SetAsLastSibling();
    }

    private static void StyleDungeonCardDetail(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        SetRuntimeRect(text.rectTransform, new Vector2(-110f, -202f), new Vector2(560f, 26f), new Vector2(0.5f, 1f));
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.84f, 0.93f, 1f);
        text.fontSize = 17;
        text.fontSizeMin = 12;
        text.fontSizeMax = 17;
        text.enableAutoSizing = true;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineColor = new Color(0.04f, 0.03f, 0.02f, 0.95f);
        text.outlineWidth = 0.12f;
        text.transform.SetAsLastSibling();
    }

    private void HideLegacyRuntimeDungeonTower()
    {
        var oldDungeonTower = dungeonsPanel != null ? dungeonsPanel.transform.Find("Runtime Art Dungeon Tower") : null;
        if (oldDungeonTower != null)
        {
            oldDungeonTower.gameObject.SetActive(false);
        }
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
        if (homePanel == null)
        {
            return;
        }

        for (var i = 0; i < homePanel.transform.childCount; i++)
        {
            var child = homePanel.transform.GetChild(i);
            child.gameObject.SetActive(homeActionRoot != null && child == homeActionRoot.transform);
        }

        if (homeActionRoot != null)
        {
            homeActionRoot.SetAsLastSibling();
        }
    }

    private void LayoutBattleScreen()
    {
        SetComponentActive(upgradeButton, false);
        SetComponentActive(damageText, false);
        SetComponentActive(autoAttackText, false);
        SetComponentActive(enemyText, false);
        SetComponentActive(enemyHpText, false);
        SetComponentActive(fightButton, false);
        SetComponentActive(dungeonResultText, false);

        MoveUiElement(damageText, battlePanel, new Vector2(0, -130), new Vector2(760, 34));
        MoveUiElement(autoAttackText, battlePanel, new Vector2(0, -166), new Vector2(760, 28));
        MoveUiElement(enemyText, battlePanel, new Vector2(0, -210), new Vector2(760, 64));
        MoveUiElement(enemyHpText, battlePanel, new Vector2(0, -270), new Vector2(760, 32));
        MoveUiElement(fightButton, battlePanel, new Vector2(0, -690), new Vector2(420, 76));
        MoveUiElement(dungeonResultText, battlePanel, new Vector2(0, -782), new Vector2(760, 72));

        if (formationRoot != null)
        {
            formationRoot.SetAsLastSibling();
        }

        if (fightRoot != null)
        {
            fightRoot.SetAsLastSibling();
        }
    }

    private void LayoutDungeonsScreen()
    {
        if (dungeonsPanel == null)
        {
            return;
        }

        MoveUiElement(dungeonsHeaderText, dungeonsPanel, new Vector2(0, -132), new Vector2(860, 54));
        MoveUiElement(goldDungeonButton, dungeonsPanel, new Vector2(0, -198), new Vector2(850, 220));
        MoveUiElement(essenceDungeonButton, dungeonsPanel, new Vector2(0, -446), new Vector2(850, 220));
        MoveUiElement(gearDungeonButton, dungeonsPanel, new Vector2(0, -694), new Vector2(850, 220));
        MoveUiElement(runtimeDungeonResultText, dungeonsPanel, new Vector2(0, -954), new Vector2(760, 52));
        ApplyDungeonCardsButtonSkin();
    }

    private void LayoutHeroesScreen()
    {
        var oldBackdrop = heroesPanel != null ? heroesPanel.transform.Find("Heroes Parchment Backdrop") : null;
        if (oldBackdrop != null)
        {
            oldBackdrop.gameObject.SetActive(false);
        }
        HideLegacyHeroesOverviewElements();

        MoveUiElement(selectedHeroText, heroesPanel, new Vector2(0, -142), new Vector2(760, 82));
        MoveUiElement(heroCleanBackdropRoot, heroesPanel, new Vector2(0, -176), new Vector2(1080, 1040));
        if (heroCleanBackdropRoot != null)
        {
            heroCleanBackdropRoot.SetAsFirstSibling();
        }

        MoveUiElement(heroRosterFilterRoot, heroesPanel, new Vector2(0, -186), new Vector2(1080, 104));
        MoveUiElement(heroSortToggleButton, heroRosterFilterRoot != null ? heroRosterFilterRoot.gameObject : heroesPanel, new Vector2(-130, -20), new Vector2(142, 58));
        MoveUiElement(heroAttackTypeFilterButton, heroRosterFilterRoot != null ? heroRosterFilterRoot.gameObject : heroesPanel, new Vector2(250, -20), new Vector2(300, 58));
        MoveUiElement(heroSubTabRoot, heroesPanel, new Vector2(0, -1050), new Vector2(820, 82));
        MoveUiElement(heroRosterTabButton, heroSubTabRoot != null ? heroSubTabRoot.gameObject : heroesPanel, new Vector2(-206, -9), new Vector2(290, 64));
        MoveUiElement(heroSetTeamTabButton, heroSubTabRoot != null ? heroSubTabRoot.gameObject : heroesPanel, new Vector2(136, -9), new Vector2(360, 64));
        MoveUiElement(heroTeamRoot, heroesPanel, new Vector2(0, -288), new Vector2(900, 350));
        LayoutHeroCards();
        SetTextArrayActive(teamSlotTexts, false);
        SetComponentActive(heroUpgradeButton, false);
        SetComponentActive(heroAscendButton, false);
    }

    private void LayoutHeroCards()
    {
        if (heroSelectButtons == null)
        {
            return;
        }

        const float cardWidth = 154f;
        const float cardHeight = 226f;
        var xPositions = new[] { -348f, -174f, 0f, 174f, 348f };
        var yPositions = heroesTabMode == HeroesTabMode.SetTeam
            ? new[] { -670f, -910f, -1150f }
            : new[] { -332f, -572f, -812f };

        for (var i = 0; i < heroSelectButtons.Length; i++)
        {
            var x = xPositions[i % xPositions.Length];
            var y = yPositions[Mathf.Min(yPositions.Length - 1, i / xPositions.Length)];
            MoveUiElement(heroSelectButtons[i], heroesPanel, new Vector2(x, y), new Vector2(cardWidth, cardHeight));

            if (heroCardTexts != null && i < heroCardTexts.Length)
            {
                MoveUiElement(heroCardTexts[i], heroSelectButtons[i] != null ? heroSelectButtons[i].gameObject : null, new Vector2(0, -145), new Vector2(132, 70));
            }

            if (heroCardPortraits != null && i < heroCardPortraits.Length && heroCardPortraits[i] != null)
            {
                SetRuntimeRect(heroCardPortraits[i].rectTransform, new Vector2(0, -12), new Vector2(132, 132), new Vector2(0.5f, 1f));
            }

            if (heroCardLevelTexts != null && i < heroCardLevelTexts.Length)
            {
                MoveUiElement(heroCardLevelTexts[i], heroSelectButtons[i] != null ? heroSelectButtons[i].gameObject : null, new Vector2(0, -112), new Vector2(128, 30));
            }

            if (heroCardStarTexts != null && i < heroCardStarTexts.Length)
            {
                MoveUiElement(heroCardStarTexts[i], heroSelectButtons[i] != null ? heroSelectButtons[i].gameObject : null, new Vector2(0, -142), new Vector2(132, 24));
            }

            if (heroCardShardFills != null && i < heroCardShardFills.Length && heroCardShardFills[i] != null)
            {
                var shardBack = heroCardShardFills[i].transform.parent.GetComponent<RectTransform>();
                SetRuntimeRect(shardBack, new Vector2(0, -178), new Vector2(132, 26), new Vector2(0.5f, 1f));
            }

            if (heroCardRoleBadgeTexts != null && i < heroCardRoleBadgeTexts.Length && heroCardRoleBadgeTexts[i] != null)
            {
                var badge = heroCardRoleBadgeTexts[i].transform.parent.GetComponent<RectTransform>();
                SetRuntimeRect(badge, new Vector2(-48, -8), new Vector2(42, 34), new Vector2(0.5f, 1f));
            }

            if (heroCardTeamBadgeTexts != null && i < heroCardTeamBadgeTexts.Length && heroCardTeamBadgeTexts[i] != null)
            {
                var badge = heroCardTeamBadgeTexts[i].transform.parent.GetComponent<RectTransform>();
                SetRuntimeRect(badge, new Vector2(48, -8), new Vector2(42, 34), new Vector2(0.5f, 1f));
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
        SetComponentActive(summonCostText, false);
        var summonBackdrop = summonPanel != null ? summonPanel.transform.Find("Summon Parchment Backdrop")?.GetComponent<RectTransform>() : null;
        SetComponentActive(summonBackdrop, false);
        MoveUiElement(summonOfferRoot, summonPanel, new Vector2(0, -312), new Vector2(820, 458));
        MoveUiElement(summonButton, summonPanel, new Vector2(-190, -786), new Vector2(300, 82));
        MoveUiElement(summonTenButton, summonPanel, new Vector2(190, -786), new Vector2(300, 82));
        MoveUiElement(summonSingleCostText, summonButton != null ? summonButton.gameObject : null, new Vector2(-122, -50), new Vector2(58, 18));
        MoveUiElement(summonTenCostText, summonTenButton != null ? summonTenButton.gameObject : null, new Vector2(-122, -50), new Vector2(58, 18));
        MoveUiElement(summonCarouselRoot, summonPanel, new Vector2(0, -900), new Vector2(820, 142));
        MoveUiElement(summonCarouselPreviousButton, summonCarouselRoot != null ? summonCarouselRoot.gameObject : null, new Vector2(-366, -40), new Vector2(58, 78));
        MoveUiElement(summonCarouselNextButton, summonCarouselRoot != null ? summonCarouselRoot.gameObject : null, new Vector2(366, -40), new Vector2(58, 78));
        MoveUiElement(summonResultBoxRoot, summonPanel, new Vector2(0, -1046), new Vector2(760, 64));
        MoveUiElement(summonCountChipRoot, summonPanel, new Vector2(-286, -1128), new Vector2(210, 52));
        MoveUiElement(summonRatesBoxRoot, summonPanel, new Vector2(130, -1128), new Vector2(510, 118));
        MoveUiElement(summonResultText, summonResultBoxRoot != null ? summonResultBoxRoot.gameObject : summonPanel, new Vector2(0, -10), new Vector2(700, 50));
        MoveUiElement(summonCountText, summonCountChipRoot != null ? summonCountChipRoot.gameObject : summonPanel, new Vector2(4, -11), new Vector2(176, 32));
        MoveUiElement(summonRatesText, summonRatesBoxRoot != null ? summonRatesBoxRoot.gameObject : summonPanel, new Vector2(0, -22), new Vector2(470, 84));
        MoveUiElement(summonResultPopupRoot, summonPanel, new Vector2(0, -140), new Vector2(900, 1040));
    }

    private void LayoutShopScreen()
    {
        MoveUiElement(menuHeaderText, shopPanel, new Vector2(0, -145), new Vector2(790, 48));

        if (dailyMissionButtons != null)
        {
            for (var i = 0; i < dailyMissionButtons.Length; i++)
            {
                SetComponentActive(dailyMissionButtons[i], true);
                MoveUiElement(dailyMissionButtons[i], shopPanel, new Vector2(0, -230 - i * 82), new Vector2(720, 70));
                if (dailyMissionTexts != null && i < dailyMissionTexts.Length)
                {
                    SetComponentActive(dailyMissionTexts[i], true);
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

            var heroIndex = GetHeroCardDisplayIndex(i);
            button.gameObject.SetActive(heroIndex >= 0);
            if (heroIndex < 0)
            {
                continue;
            }

            var frame = button.GetComponent<Image>();
            if (frame != null)
            {
                var isSelected = heroIndex == selectedHeroIndex;
                var isTeamTarget = heroesTabMode == HeroesTabMode.SetTeam && selectedHeroTeamSlotIndex >= 0 && FindFormationSlotForHero(heroIndex) == selectedHeroTeamSlotIndex;
                var rarityColor = GetHeroRosterCardColor(heroIndex);
                frame.sprite = null;
                frame.type = Image.Type.Simple;
                frame.color = isSelected || isTeamTarget
                    ? Color.Lerp(rarityColor, new Color(1f, 0.95f, 0.58f, 1f), 0.45f)
                    : FindFormationSlotForHero(heroIndex) >= 0
                        ? Color.Lerp(rarityColor, new Color(0.12f, 0.68f, 0.64f, 1f), 0.2f)
                        : rarityColor;
            }

            if (heroCardPortraits == null || i >= heroCardPortraits.Length || heroCardPortraits[i] == null)
            {
                continue;
            }

            var hero = GetHeroDefinition(heroIndex);
            heroCardPortraits[i].texture = LoadRuntimeTexture($"hero_{hero.name.ToLowerInvariant()}");
            heroCardPortraits[i].rectTransform.localScale = new Vector3(GetHeroFacingScale(heroIndex), 1f, 1f);
            heroCardPortraits[i].color = Color.white;
        }
    }

    private static Color GetHeroRosterCardColor(int heroIndex)
    {
        var rarityId = GetHeroDefinition(heroIndex).rarityId;
        if (rarityId == LegendaryRarityId)
        {
            return new Color(0.9f, 0.55f, 0.13f, 0.98f);
        }

        if (rarityId == EpicRarityId)
        {
            return new Color(0.62f, 0.37f, 0.78f, 0.98f);
        }

        return new Color(0.52f, 0.62f, 0.22f, 0.98f);
    }

    private void ShowHeroDetail(int index)
    {
        EnsureRuntimeHeroDetailWindow();
        if (heroDetailRoot == null)
        {
            return;
        }

        selectedHeroIndex = Mathf.Clamp(index, 0, HeroCount - 1);
        heroDetailRoot.SetAsLastSibling();
        heroDetailRoot.gameObject.SetActive(true);
        RefreshHeroDetailUi();
    }

    private void HideHeroDetail()
    {
        HideHeroDetailGearList();
        if (heroDetailRoot != null)
        {
            heroDetailRoot.gameObject.SetActive(false);
        }
    }

    private void ShowPreviousHeroDetail()
    {
        SelectHero((selectedHeroIndex + HeroCount - 1) % HeroCount);
    }

    private void ShowNextHeroDetail()
    {
        SelectHero((selectedHeroIndex + 1) % HeroCount);
    }

    private void RefreshHeroDetailUi()
    {
        if (heroDetailRoot == null)
        {
            return;
        }

        EnsureHeroLevels();
        EnsureHeroShards();
        EnsureHeroAscensions();
        EnsureAccessories();

        var heroIndex = Mathf.Clamp(selectedHeroIndex, 0, HeroCount - 1);
        var hero = GetHeroDefinition(heroIndex);
        var level = heroLevels[heroIndex];
        var ascension = heroAscensions[heroIndex];
        var levelCap = GetHeroLevelCap(heroIndex);
        var ascensionCap = GetHeroAscensionCap(heroIndex);
        var upgradeCost = GetHeroUpgradeCost(heroIndex);
        var ascensionCost = GetHeroAscensionCost(heroIndex);
        var heroColor = GetHeroRarityColor(hero.rarityId);

        if (heroDetailPortrait != null)
        {
            heroDetailPortrait.texture = LoadCombatTexture(GetHeroTextureName(heroIndex), "idle", 0, GetHeroTextureName(heroIndex));
            heroDetailPortrait.rectTransform.localScale = new Vector3(GetHeroFacingScale(heroIndex), 1f, 1f);
            heroDetailPortrait.color = Color.white;
        }

        if (heroDetailRarityText != null)
        {
            heroDetailRarityText.text = hero.rarityName;
            heroDetailRarityText.color = heroColor;
        }

        if (heroDetailTitleText != null)
        {
            heroDetailTitleText.text = GetHeroDetailTitle(hero);
        }

        if (heroDetailNameText != null)
        {
            heroDetailNameText.text = hero.name;
            heroDetailNameText.color = heroColor;
        }

        if (heroDetailPowerText != null)
        {
            heroDetailPowerText.text = $"Power {GetHeroPower(heroIndex)}";
        }

        if (heroDetailStatsText != null)
        {
            heroDetailStatsText.text = $"Lvl {FormatCappedValue(level, levelCap)}   Asc {FormatCappedValue(ascension, ascensionCap)}   {hero.roleName}\nHP {GetHeroCombatMaxHealth(heroIndex)}   ATK {GetHeroEffectiveAttack(heroIndex)}   DEF {GetHeroDefense(heroIndex)}\nCrit {GetHeroCritChancePercent(heroIndex)}%   Acc {GetHeroAccuracyPercent(heroIndex)}%";
        }

        if (heroDetailResourceText != null)
        {
            heroDetailResourceText.text = $"Essence {mythEssence}/{upgradeCost}   Shards {heroShards[heroIndex]}/{ascensionCost}";
        }

        RefreshHeroDetailGearSlots();
        RefreshHeroDetailGearList();

        SetButtonLabel(heroDetailLevelButton, IsHeroLevelMax(heroIndex) ? "Max Level" : "Level Up");
        SetButtonLabel(heroDetailEquipGearButton, "Equip Gear");
        SetButtonLabel(heroDetailRemoveGearButton, "Remove Gear");
        SetButtonInteractable(heroDetailLevelButton, !IsHeroLevelMax(heroIndex) && mythEssence >= upgradeCost);
        SetButtonInteractable(heroDetailEquipGearButton, true);
        SetButtonInteractable(heroDetailRemoveGearButton, true);
    }

    private void RefreshHeroDetailGearSlots()
    {
        if (heroDetailGearSlotTexts == null)
        {
            return;
        }

        for (var i = 0; i < heroDetailGearSlotTexts.Length; i++)
        {
            if (heroDetailGearSlotTexts[i] != null)
            {
                heroDetailGearSlotTexts[i].text = GetHeroDetailGearSlotText(i);
            }

            if (heroDetailGearSlotFrames != null && i < heroDetailGearSlotFrames.Length && heroDetailGearSlotFrames[i] != null)
            {
                var color = GetHeroDetailGearSlotColor(i);
                if (i == selectedHeroDetailGearSlotIndex && heroDetailGearListRoot != null && heroDetailGearListRoot.gameObject.activeSelf)
                {
                    color = Color.Lerp(color, new Color(1f, 0.88f, 0.32f, 1f), 0.58f);
                }

                heroDetailGearSlotFrames[i].color = color;
            }
        }
    }

    private void ShowHeroDetailGearSlot0() => ShowHeroDetailGearSlot(0);
    private void ShowHeroDetailGearSlot1() => ShowHeroDetailGearSlot(1);
    private void ShowHeroDetailGearSlot2() => ShowHeroDetailGearSlot(2);
    private void ShowHeroDetailGearSlot3() => ShowHeroDetailGearSlot(3);
    private void ShowHeroDetailGearSlot4() => ShowHeroDetailGearSlot(4);
    private void ShowHeroDetailGearSlot5() => ShowHeroDetailGearSlot(5);

    private void ShowHeroDetailGearSlot(int slotIndex)
    {
        if (heroDetailGearListRoot == null)
        {
            return;
        }

        selectedHeroDetailGearSlotIndex = Mathf.Clamp(slotIndex, 0, heroDetailGearSlotTexts == null ? 5 : heroDetailGearSlotTexts.Length - 1);
        heroDetailGearListRoot.gameObject.SetActive(true);
        heroDetailGearListRoot.SetAsLastSibling();
        RefreshHeroDetailGearSlots();
        RefreshHeroDetailGearList();
    }

    private void HideHeroDetailGearList()
    {
        selectedHeroDetailGearSlotIndex = -1;
        if (heroDetailGearListRoot != null)
        {
            heroDetailGearListRoot.gameObject.SetActive(false);
        }

        RefreshHeroDetailGearSlots();
    }

    private void RefreshHeroDetailGearList()
    {
        if (heroDetailGearListRoot == null || !heroDetailGearListRoot.gameObject.activeSelf)
        {
            return;
        }

        if (selectedHeroDetailGearSlotIndex < 0)
        {
            heroDetailGearListRoot.gameObject.SetActive(false);
            return;
        }

        EnsureAccessories();
        var heroIndex = GetSelectedHeroIndex();
        if (selectedHeroDetailGearSlotIndex < 2)
        {
            RefreshHeroDetailEquipmentTrackList(
                selectedHeroDetailGearSlotIndex == 0 ? WeaponTrack : ArmorTrack,
                selectedHeroDetailGearSlotIndex == 0 ? GetHeroEquipmentLevel(heroIndex, isWeapon: true) : GetHeroEquipmentLevel(heroIndex, isWeapon: false),
                heroIndex);
            return;
        }

        var accessorySlot = selectedHeroDetailGearSlotIndex - 2;
        if (accessorySlot < 0 || accessorySlot >= AccessorySlotCount)
        {
            return;
        }

        if (heroDetailGearListTitleText != null)
        {
            heroDetailGearListTitleText.text = $"{AccessorySlots[accessorySlot].name} Gear";
        }

        var canInteract = !backendRequestInProgress && !backendLifecycleFlushInProgress && !campaignFightInProgress;
        for (var rarity = 0; rarity < AccessoryRarityCount; rarity++)
        {
            var optionButton = heroDetailGearOptionButtons != null && rarity < heroDetailGearOptionButtons.Length ? heroDetailGearOptionButtons[rarity] : null;
            var optionText = heroDetailGearOptionTexts != null && rarity < heroDetailGearOptionTexts.Length ? heroDetailGearOptionTexts[rarity] : null;
            if (optionButton == null)
            {
                continue;
            }

            optionButton.gameObject.SetActive(true);
            var copies = GetAccessoryInventoryCount(accessorySlot, rarity);
            var equipped = GetHeroEquippedAccessoryRarity(heroIndex, accessorySlot) == rarity;
            var levelText = equipped ? $"  Equipped Lv {GetHeroEquippedAccessoryLevel(heroIndex, accessorySlot)}" : string.Empty;
            var actionText = equipped ? "Equipped" : copies > 0 ? "Tap to equip" : "No copy";
            if (optionText != null)
            {
                optionText.text = $"{GetAccessoryRarityName(rarity)} {AccessorySlots[accessorySlot].name}   Copies {copies}{levelText}\n{actionText}";
            }

            var image = optionButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = equipped
                    ? new Color(1f, 0.76f, 0.25f, 0.98f)
                    : copies > 0
                        ? GetAccessoryRarityColor(rarity)
                        : new Color(0.16f, 0.11f, 0.09f, 0.84f);
            }

            optionButton.interactable = canInteract && copies > 0 && !equipped;
        }
    }

    private void RefreshHeroDetailEquipmentTrackList(EquipmentTrackDefinition track, int level, int heroIndex)
    {
        var hero = GetHeroDefinition(heroIndex);
        if (heroDetailGearListTitleText != null)
        {
            heroDetailGearListTitleText.text = $"{hero.name} {track.name}";
        }

        for (var i = 0; i < AccessoryRarityCount; i++)
        {
            var optionButton = heroDetailGearOptionButtons != null && i < heroDetailGearOptionButtons.Length ? heroDetailGearOptionButtons[i] : null;
            var optionText = heroDetailGearOptionTexts != null && i < heroDetailGearOptionTexts.Length ? heroDetailGearOptionTexts[i] : null;
            if (optionButton == null)
            {
                continue;
            }

            optionButton.gameObject.SetActive(i < 2);
            optionButton.interactable = i == 1;
            var image = optionButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = i == 1 ? new Color(0.38f, 0.2f, 0.08f, 0.96f) : new Color(0.18f, 0.11f, 0.07f, 0.9f);
            }

            if (optionText != null)
            {
                var bonus = track.equipmentId == WeaponTrack.equipmentId ? GetHeroEquipmentAttackBonus(heroIndex) : GetHeroEquipmentHealthBonus(heroIndex);
                optionText.text = i == 0
                    ? $"{track.name} Lv {GetEquipmentDisplayLevel(track, level)}   +{bonus} {track.statLabel}\nEquipped on {hero.name}"
                    : $"Open Gear screen for {hero.name}";
            }
        }
    }

    private void EquipHeroDetailGearOption0() => EquipHeroDetailGearOption(0);
    private void EquipHeroDetailGearOption1() => EquipHeroDetailGearOption(1);
    private void EquipHeroDetailGearOption2() => EquipHeroDetailGearOption(2);
    private void EquipHeroDetailGearOption3() => EquipHeroDetailGearOption(3);
    private void EquipHeroDetailGearOption4() => EquipHeroDetailGearOption(4);

    private void EquipHeroDetailGearOption(int rarity)
    {
        if (selectedHeroDetailGearSlotIndex < 0)
        {
            return;
        }

        if (selectedHeroDetailGearSlotIndex < 2)
        {
            ShowGear();
            return;
        }

        var accessorySlot = selectedHeroDetailGearSlotIndex - 2;
        if (accessorySlot < 0 || accessorySlot >= AccessorySlotCount)
        {
            return;
        }

        selectedAccessorySlot = accessorySlot;
        selectedAccessoryRarity = Mathf.Clamp(rarity, 0, AccessoryRarityCount - 1);
        if (backendGameplayEnabled)
        {
            if (TryStartBackendRequest("Server: equipping accessory..."))
            {
                StartCoroutine(backendClient.EquipAccessory(GetAccessoryDefinition(selectedAccessorySlot, selectedAccessoryRarity).accessoryId, OnBackendGameplayAction));
            }

            return;
        }

        var result = EquipAccessory(GetAccessoryDefinition(selectedAccessorySlot, selectedAccessoryRarity).accessoryId);
        SetDungeonResult(result.message);
        ShowHeroDetailGearSlot(selectedHeroDetailGearSlotIndex);
    }

    private string GetHeroDetailGearSlotText(int slotIndex)
    {
        var heroIndex = GetSelectedHeroIndex();
        if (slotIndex == 0)
        {
            return $"{WeaponTrack.name}\nLv {GetEquipmentDisplayLevel(WeaponTrack, GetHeroEquipmentLevel(heroIndex, isWeapon: true))}";
        }

        if (slotIndex == 1)
        {
            return $"{ArmorTrack.name}\nLv {GetEquipmentDisplayLevel(ArmorTrack, GetHeroEquipmentLevel(heroIndex, isWeapon: false))}";
        }

        var accessorySlot = slotIndex - 2;
        if (accessorySlot < 0 || accessorySlot >= AccessorySlotCount)
        {
            return "Locked";
        }

        var rarity = GetHeroEquippedAccessoryRarity(heroIndex, accessorySlot);
        if (rarity < 0)
        {
            return $"{AccessorySlots[accessorySlot].name}\nEmpty";
        }

        return $"{AccessorySlots[accessorySlot].name}\n{GetAccessoryRarityName(rarity)} Lv {GetHeroEquippedAccessoryLevel(heroIndex, accessorySlot)}";
    }

    private Color GetHeroDetailGearSlotColor(int slotIndex)
    {
        if (slotIndex < 2)
        {
            return new Color(0.72f, 0.47f, 0.22f, 0.96f);
        }

        var accessorySlot = slotIndex - 2;
        if (accessorySlot < 0 || accessorySlot >= AccessorySlotCount)
        {
            return new Color(0.18f, 0.13f, 0.1f, 0.78f);
        }

        var rarity = GetHeroEquippedAccessoryRarity(GetSelectedHeroIndex(), accessorySlot);
        return rarity >= 0 ? GetAccessoryRarityColor(rarity) : new Color(0.36f, 0.22f, 0.13f, 0.9f);
    }

    private static string GetHeroDetailTitle(HeroDefinition hero)
    {
        switch (hero.heroId)
        {
            case "hero_astra":
                return "The Frost Vanguard";
            case "hero_borin":
                return "The Iron Sentinel";
            case "hero_cyra":
                return "The Burning Light";
            case "hero_dante":
                return "The Rift Marksman";
            case "hero_elowen":
                return "The Grove Oracle";
            case "hero_ravik":
                return "The Cinder Prodigy";
            default:
                return $"The {hero.roleName}";
        }
    }

    private static Color GetHeroRarityColor(string rarityId)
    {
        if (rarityId == LegendaryRarityId)
        {
            return new Color(0.35f, 1f, 0.92f);
        }

        if (rarityId == EpicRarityId)
        {
            return new Color(0.86f, 0.58f, 1f);
        }

        return new Color(0.55f, 0.9f, 1f);
    }

    private static Color GetAccessoryRarityColor(int rarity)
    {
        var tier = Mathf.Clamp(rarity, 0, AccessoryRarityCount - 1);
        switch (tier)
        {
            case 4:
                return new Color(1f, 0.72f, 0.24f, 0.96f);
            case 3:
                return new Color(0.86f, 0.55f, 1f, 0.96f);
            case 2:
                return new Color(0.35f, 0.75f, 1f, 0.96f);
            case 1:
                return new Color(0.35f, 0.9f, 0.48f, 0.96f);
            default:
                return new Color(0.72f, 0.72f, 0.72f, 0.92f);
        }
    }

    private void RefreshTopBarUi()
    {
        if (topBarRoot == null)
        {
            return;
        }

        topBarRoot.SetAsLastSibling();

        if (topPlayerNameText != null)
        {
            topPlayerNameText.transform.SetAsLastSibling();
            topPlayerNameText.gameObject.SetActive(true);
            topPlayerNameText.text = GetPlayerDisplayName();
        }

        if (topPowerText != null)
        {
            topPowerText.transform.SetAsLastSibling();
            topPowerText.gameObject.SetActive(true);
            topPowerText.text = FormatCompactNumber(GetTeamPower());
        }

        if (topPowerIconImage != null)
        {
            topPowerIconImage.transform.SetAsLastSibling();
            topPowerIconImage.gameObject.SetActive(true);
            topPowerIconImage.texture = GetHomeGeneratedTexture("home_power_icon");
        }

        if (topGemAmountText != null)
        {
            topGemAmountText.transform.SetAsLastSibling();
            topGemAmountText.gameObject.SetActive(true);
            topGemAmountText.text = FormatCompactNumber(gems);
        }

        if (topGoldAmountText != null)
        {
            topGoldAmountText.transform.SetAsLastSibling();
            topGoldAmountText.gameObject.SetActive(true);
            topGoldAmountText.text = FormatCompactNumber(gold);
        }

        if (topGemIconImage != null)
        {
            topGemIconImage.texture = GetCurrencyIconTexture("mythic_gem");
        }

        if (topGoldIconImage != null)
        {
            topGoldIconImage.texture = GetCurrencyIconTexture("gold_coin");
        }

        if (topGemPlusButton != null)
        {
            topGemPlusButton.transform.SetAsLastSibling();
            topGemPlusButton.gameObject.SetActive(true);
        }

        if (heroEssenceAmountText != null)
        {
            heroEssenceAmountText.text = $"Essence {FormatCompactNumber(mythEssence)}";
        }

        if (heroEssenceIconImage != null)
        {
            heroEssenceIconImage.texture = GetCurrencyIconTexture("exp_shard");
        }

        if (topbarFrameImage != null)
        {
            topbarFrameImage.texture = GetHomeGeneratedTexture("home_topbar_frame");
        }
    }

    private void RefreshHomeGeneratedUi()
    {
        if (homeActionRoot == null)
        {
            return;
        }

        if (homeStageLevelBadgeText != null)
        {
            homeStageLevelBadgeText.text = $"Stufe {enemyLevel}";
        }

        if (homeStageModeBadgeText != null)
        {
            homeStageModeBadgeText.text = "Albtraum";
        }

        SetHomeShortcutsExpanded(homeShortcutsExpanded);
        RefreshFastRewardsPopupUi();

    }

    private void RefreshCampaignMapUi()
    {
        if (homeCampaignMapRoot == null || campaignStageButtons == null)
        {
            return;
        }

        if (selectedCampaignStage <= 0)
        {
            selectedCampaignStage = Mathf.Max(1, enemyLevel);
        }

        var startStage = GetCampaignMapStartStage();
        for (var i = 0; i < campaignStageButtons.Length; i++)
        {
            var stageNumber = startStage + i;
            var isCleared = stageNumber < enemyLevel;
            var isCurrent = stageNumber == enemyLevel;
            var isLocked = stageNumber > enemyLevel;
            var isSelected = stageNumber == selectedCampaignStage;

            if (campaignStageButtonTexts != null && i < campaignStageButtonTexts.Length && campaignStageButtonTexts[i] != null)
            {
                campaignStageButtonTexts[i].text = $"{(stageNumber - 1) / 10 + 1}-{(stageNumber - 1) % 10 + 1}";
                campaignStageButtonTexts[i].color = isLocked ? new Color(0.56f, 0.6f, 0.68f) : Color.white;
            }

            if (campaignStageButtonFrames != null && i < campaignStageButtonFrames.Length && campaignStageButtonFrames[i] != null)
            {
                campaignStageButtonFrames[i].color = isSelected
                    ? new Color(1f, 0.78f, 0.24f, 1f)
                    : isCurrent
                        ? new Color(0.54f, 0.23f, 0.75f, 0.98f)
                        : isCleared
                            ? new Color(0.22f, 0.48f, 0.42f, 0.95f)
                            : new Color(0.12f, 0.13f, 0.17f, 0.82f);
            }

            if (campaignStageButtonIcons != null && i < campaignStageButtonIcons.Length && campaignStageButtonIcons[i] != null)
            {
                campaignStageButtonIcons[i].texture = isCleared
                    ? LoadRuntimeTexture("dungeon_portal")
                    : LoadCombatTexture(GetCampaignEnemyTextureName(stageNumber, 0), "idle", 0, "enemy_campaign");
                campaignStageButtonIcons[i].color = isLocked ? new Color(0.32f, 0.34f, 0.4f, 0.72f) : Color.white;
            }

            campaignStageButtons[i].interactable = !campaignFightInProgress && !backendRequestInProgress && !backendLifecycleFlushInProgress;
        }

        RefreshCampaignStagePreview();
    }

    private void RefreshCampaignStagePreview()
    {
        if (campaignStagePreviewText == null)
        {
            return;
        }

        var stageNumber = Mathf.Max(1, selectedCampaignStage);
        var stage = GetStageDefinition(stageNumber);
        var status = stageNumber < enemyLevel ? "Cleared" : stageNumber == enemyLevel ? "Current Target" : "Locked";
        var requiredPower = GetStageRecommendedPower(stageNumber);
        var fightLine = stageNumber == enemyLevel
            ? "Battle opens formation."
            : stageNumber < enemyLevel
                ? "Replay selection comes later."
                : "Clear the current stage first.";
        campaignStagePreviewText.text =
            $"Stage {stageNumber}: {stage.enemyName}  |  {status}\n" +
            $"Power {FormatCompactNumber(GetTeamPower())}/{FormatCompactNumber(requiredPower)}  Reward +{stage.essenceReward} Essence\n" +
            fightLine;
    }

    private void EnsureFormationOrder()
    {
        if (formationSlotHeroIndices == null || formationSlotHeroIndices.Length != HeroCount)
        {
            formationSlotHeroIndices = new int[HeroCount];
            for (var i = 0; i < HeroCount; i++)
            {
                formationSlotHeroIndices[i] = i;
            }

            selectedFormationSlotIndex = -1;
            return;
        }

        var usedHeroes = new bool[HeroCount];
        for (var slotIndex = 0; slotIndex < formationSlotHeroIndices.Length; slotIndex++)
        {
            var heroIndex = formationSlotHeroIndices[slotIndex];
            if (heroIndex < 0 || heroIndex >= HeroCount || usedHeroes[heroIndex])
            {
                formationSlotHeroIndices[slotIndex] = -1;
                continue;
            }

            usedHeroes[heroIndex] = true;
        }

        var nextMissingHero = 0;
        for (var slotIndex = 0; slotIndex < formationSlotHeroIndices.Length; slotIndex++)
        {
            if (formationSlotHeroIndices[slotIndex] >= 0)
            {
                continue;
            }

            while (nextMissingHero < HeroCount && usedHeroes[nextMissingHero])
            {
                nextMissingHero++;
            }

            formationSlotHeroIndices[slotIndex] = nextMissingHero < HeroCount ? nextMissingHero : slotIndex;
            if (nextMissingHero < HeroCount)
            {
                usedHeroes[nextMissingHero] = true;
            }
        }

        if (selectedFormationSlotIndex < 0 || selectedFormationSlotIndex >= HeroCount)
        {
            selectedFormationSlotIndex = -1;
        }
    }

    private int FindFormationSlotForHero(int heroIndex)
    {
        EnsureFormationOrder();
        heroIndex = Mathf.Clamp(heroIndex, 0, HeroCount - 1);
        for (var slotIndex = 0; slotIndex < formationSlotHeroIndices.Length; slotIndex++)
        {
            if (formationSlotHeroIndices[slotIndex] == heroIndex)
            {
                return slotIndex;
            }
        }

        return -1;
    }

    private void SelectFormationSlot(int slotIndex)
    {
        if (campaignFightInProgress || backendRequestInProgress || backendLifecycleFlushInProgress)
        {
            return;
        }

        EnsureFormationOrder();
        slotIndex = Mathf.Clamp(slotIndex, 0, HeroCount - 1);
        if (selectedFormationSlotIndex < 0)
        {
            selectedFormationSlotIndex = slotIndex;
        }
        else if (selectedFormationSlotIndex == slotIndex)
        {
            selectedFormationSlotIndex = -1;
        }
        else
        {
            var heroIndex = formationSlotHeroIndices[selectedFormationSlotIndex];
            formationSlotHeroIndices[selectedFormationSlotIndex] = formationSlotHeroIndices[slotIndex];
            formationSlotHeroIndices[slotIndex] = heroIndex;
            selectedFormationSlotIndex = -1;
            SaveProgress();
        }

        RefreshFormationUi();
        RefreshGameplayInteractivity();
    }

    private Vector2[] GetCurrentFightHeroPositions()
    {
        EnsureFormationOrder();

        var slotPositions = GetFightHeroPositions();
        var heroPositions = new Vector2[HeroCount];
        var assignedHeroes = new bool[HeroCount];
        for (var slotIndex = 0; slotIndex < HeroCount && slotIndex < slotPositions.Length; slotIndex++)
        {
            var heroIndex = formationSlotHeroIndices[slotIndex];
            if (heroIndex < 0 || heroIndex >= HeroCount)
            {
                continue;
            }

            heroPositions[heroIndex] = slotPositions[slotIndex];
            assignedHeroes[heroIndex] = true;
        }

        for (var heroIndex = 0; heroIndex < HeroCount; heroIndex++)
        {
            if (!assignedHeroes[heroIndex])
            {
                heroPositions[heroIndex] = slotPositions[Mathf.Min(heroIndex, slotPositions.Length - 1)];
            }
        }

        return heroPositions;
    }

    private void RefreshFormationSlotHighlights()
    {
        if (formationSlotFrames == null)
        {
            return;
        }

        var canInteract = !backendRequestInProgress && !backendLifecycleFlushInProgress && !campaignFightInProgress;
        for (var slotIndex = 0; slotIndex < formationSlotFrames.Length; slotIndex++)
        {
            var isSelected = selectedFormationSlotIndex == slotIndex;
            var isSwapTarget = selectedFormationSlotIndex >= 0 && selectedFormationSlotIndex != slotIndex;
            if (formationSlotFrames[slotIndex] != null)
            {
                formationSlotFrames[slotIndex].color = isSelected
                    ? new Color(1f, 0.74f, 0.18f, 0.95f)
                    : isSwapTarget
                        ? new Color(0.22f, 0.72f, 1f, 0.82f)
                        : new Color(0.1f, 0.13f, 0.2f, 0.62f);
            }

            if (formationSlotButtons != null && slotIndex < formationSlotButtons.Length)
            {
                SetButtonInteractable(formationSlotButtons[slotIndex], canInteract);
            }
        }
    }

    private void RefreshFormationUi()
    {
        if (formationRoot == null)
        {
            return;
        }

        EnsureFormationOrder();

        var isDungeon = battleTargetMode == BattleTargetMode.Dungeon;
        var stageNumber = Mathf.Max(1, selectedCampaignStage == enemyLevel ? selectedCampaignStage : enemyLevel);
        var stage = GetStageDefinition(stageNumber);
        var dungeon = ResolveDungeonDefinition(selectedDungeonId);
        var dungeonFloor = GetDungeonFloor(dungeon.dungeonId);
        RefreshFormationArenaBackground(isDungeon);

        if (formationHeaderText != null)
        {
            formationHeaderText.text = isDungeon
                ? $"{dungeon.displayName} F{dungeonFloor} Formation"
                : $"Stage {stageNumber} Formation";
        }

        if (formationTeamText != null)
        {
            formationTeamText.text = $"Power {FormatCompactNumber(GetTeamPower())}   ATK {FormatCompactNumber(GetTeamDamage())}   HP {FormatCompactNumber(GetTeamHealth())}   Crit {GetTeamCritChancePercent()}%   Acc {GetTeamAccuracyPercent()}%   DEF {GetTeamDefense()}";
        }

        if (formationEnemyText != null)
        {
            formationEnemyText.text = isDungeon
                ? $"{GetDungeonBossName(dungeon.dungeonId)}\n" +
                  $"Recommended {FormatCompactNumber(GetDungeonRecommendedPower(dungeon, dungeonFloor))}   Boss HP {FormatCompactNumber(GetDungeonEnemyHp(dungeon, dungeonFloor))}   Damage {FormatCompactNumber(GetDungeonEnemyDamage(dungeon, dungeonFloor))}\n" +
                  FormatDungeonFormationRewardLine(dungeon, dungeonFloor)
                : $"{stage.enemyName}\n" +
                  $"Recommended {FormatCompactNumber(GetStageRecommendedPower(stageNumber))}   HP {FormatCompactNumber(stage.maxHp)}   Damage {FormatCompactNumber(GetCampaignEnemyDamage(stageNumber))}";
        }

        if (formationEnemyImage != null)
        {
            var enemyTextureName = isDungeon ? GetDungeonBossTextureName(dungeon.dungeonId) : GetCampaignEnemyTextureName(stageNumber, 0);
            formationEnemyImage.texture = LoadCombatTexture(enemyTextureName, "idle", 0, "enemy_campaign");
            formationEnemyImage.rectTransform.sizeDelta = isDungeon ? new Vector2(176, 176) : new Vector2(150, 150);
            formationEnemyImage.rectTransform.localScale = new Vector3(GetEnemyFacingScale(enemyTextureName), 1f, 1f);
        }

        if (formationHeroImages != null)
        {
            for (var slotIndex = 0; slotIndex < formationHeroImages.Length; slotIndex++)
            {
                var heroIndex = formationSlotHeroIndices[Mathf.Clamp(slotIndex, 0, formationSlotHeroIndices.Length - 1)];
                if (formationHeroImages[slotIndex] != null)
                {
                    var useRavikRig = IsRavikHero(heroIndex) && HasRavikSkeletalView(formationHeroSkeletalViews, slotIndex);
                    formationHeroImages[slotIndex].gameObject.SetActive(!useRavikRig);
                    formationHeroImages[slotIndex].texture = LoadCombatTexture(GetHeroTextureName(heroIndex), "idle", 0, GetHeroTextureName(heroIndex));
                    formationHeroImages[slotIndex].rectTransform.localScale = new Vector3(GetHeroFacingScale(heroIndex), 1f, 1f);
                    if (useRavikRig)
                    {
                        formationHeroSkeletalViews[slotIndex].ShowPreview(formationHeroImages[slotIndex].rectTransform.anchoredPosition, GetHeroFacingScale(heroIndex), 1f);
                    }
                }

                if (formationHeroSkeletalViews != null
                    && slotIndex < formationHeroSkeletalViews.Length
                    && formationHeroSkeletalViews[slotIndex] != null
                    && !IsRavikHero(heroIndex))
                {
                    formationHeroSkeletalViews[slotIndex].Hide();
                }

                if (formationHeroTexts != null && slotIndex < formationHeroTexts.Length && formationHeroTexts[slotIndex] != null)
                {
                    var hero = GetHeroDefinition(heroIndex);
                    formationHeroTexts[slotIndex].text = $"{hero.name} Lv {heroLevels[heroIndex]}";
                }
            }
        }

        RefreshFormationSlotHighlights();
        RefreshFormationAutoContinueToggle();

        if (formationHintText != null)
        {
            if (selectedFormationSlotIndex >= 0)
            {
                formationHintText.text = "Tippe eine leuchtende Position, um die Helden zu tauschen.";
            }
            else
            {
                formationHintText.text = backendGameplayEnabled
                    ? "Server Mode resolves rewards, then plays the visible fight."
                    : isDungeon
                        ? "Confirm starts a single-boss dungeon fight."
                        : $"Confirm starts a visible {DefaultCombatDurationSeconds}s combat sim.";
            }
        }
    }

    private void SelectVisibleCampaignStage(int nodeIndex)
    {
        selectedCampaignStage = Mathf.Max(1, GetCampaignMapStartStage() + Mathf.Clamp(nodeIndex, 0, 9));
        RefreshCampaignMapUi();
        RefreshGameplayInteractivity();
    }

    private int GetCampaignMapStartStage()
    {
        if (enemyLevel <= 7)
        {
            return 1;
        }

        return Mathf.Max(1, enemyLevel - 4);
    }

    private static Vector2[] GetCampaignMapNodePositions()
    {
        return new[]
        {
            new Vector2(-320, -490),
            new Vector2(-220, -410),
            new Vector2(-92, -465),
            new Vector2(42, -382),
            new Vector2(178, -432),
            new Vector2(300, -332),
            new Vector2(238, -230),
            new Vector2(82, -258),
            new Vector2(-76, -190),
            new Vector2(-252, -238)
        };
    }

    private static Vector2[] GetFormationHeroPositions()
    {
        return new[]
        {
            new Vector2(-330, -250),
            new Vector2(-230, -132),
            new Vector2(-128, -260),
            new Vector2(-22, -142),
            new Vector2(86, -258),
            new Vector2(190, -154),
            new Vector2(300, -244)
        };
    }

    private static Vector2[] GetFightHeroPositions()
    {
        return new[]
        {
            new Vector2(-342, -650),
            new Vector2(-238, -520),
            new Vector2(-130, -708),
            new Vector2(-50, -455),
            new Vector2(-326, -365),
            new Vector2(-170, -362),
            new Vector2(-18, -610)
        };
    }

    private static Vector2[] GetFightEnemyPositions()
    {
        return new[]
        {
            new Vector2(310, -405),
            new Vector2(194, -545),
            new Vector2(350, -632),
            new Vector2(118, -728),
            new Vector2(324, -805),
            new Vector2(138, -362),
            new Vector2(248, -702)
        };
    }

    private static Vector2[] GetFightBossEnemyPositions()
    {
        return new[]
        {
            new Vector2(250, -545),
            new Vector2(250, -545),
            new Vector2(250, -545),
            new Vector2(250, -545),
            new Vector2(250, -545),
            new Vector2(250, -545),
            new Vector2(250, -545)
        };
    }

    private void SetInventoryPopupVisible(bool isVisible)
    {
        if (inventoryPopupRoot == null)
        {
            return;
        }

        inventoryPopupRoot.gameObject.SetActive(isVisible);
        if (!isVisible)
        {
            selectedInventoryItemIndex = -1;
            SetComponentActive(inventoryDetailRoot, false);
            return;
        }

        if (isVisible)
        {
            if (fastRewardsPopupRoot != null)
            {
                fastRewardsPopupRoot.gameObject.SetActive(false);
            }

            if (chatPopupRoot != null)
            {
                chatPopupRoot.gameObject.SetActive(false);
            }

            inventoryPopupRoot.SetAsLastSibling();
            RefreshInventoryPopupUi();
        }
    }

    private void SetFastRewardsPopupVisible(bool isVisible)
    {
        if (fastRewardsPopupRoot == null)
        {
            return;
        }

        fastRewardsPopupRoot.gameObject.SetActive(isVisible);
        if (isVisible)
        {
            if (inventoryPopupRoot != null)
            {
                inventoryPopupRoot.gameObject.SetActive(false);
            }

            if (chatPopupRoot != null)
            {
                chatPopupRoot.gameObject.SetActive(false);
            }

            fastRewardsPopupRoot.SetAsLastSibling();
            RefreshFastRewardsPopupUi();
        }
    }

    private void SetChatPopupVisible(bool isVisible)
    {
        if (chatPopupRoot == null)
        {
            return;
        }

        chatPopupRoot.gameObject.SetActive(isVisible);
        if (isVisible)
        {
            if (inventoryPopupRoot != null)
            {
                inventoryPopupRoot.gameObject.SetActive(false);
            }

            if (fastRewardsPopupRoot != null)
            {
                fastRewardsPopupRoot.gameObject.SetActive(false);
            }

            chatPopupRoot.SetAsLastSibling();
        }
    }

    private void ToggleHomeShortcuts()
    {
        SetHomeShortcutsExpanded(!homeShortcutsExpanded);
    }

    private void SetHomeShortcutsExpanded(bool isExpanded)
    {
        homeShortcutsExpanded = isExpanded;

        SetComponentActive(homeShopButton, true);
        SetComponentActive(homeTreasureChestButton, true);
        SetComponentActive(homeQuestButton, isExpanded);

        if (homeLeftShortcutShadow != null)
        {
            homeLeftShortcutShadow.gameObject.SetActive(true);
            SetRuntimeRect(homeLeftShortcutShadow, new Vector2(-430, -135), new Vector2(142, 230), new Vector2(0.5f, 1f));
        }

        if (homeRightShortcutShadow != null)
        {
            homeRightShortcutShadow.gameObject.SetActive(true);
            SetRuntimeRect(
                homeRightShortcutShadow,
                new Vector2(430, -80),
                isExpanded ? new Vector2(148, 340) : new Vector2(148, 205),
                new Vector2(0.5f, 1f));
        }

        if (homeShortcutToggleButton != null)
        {
            var toggleRect = homeShortcutToggleButton.GetComponent<RectTransform>();
            if (toggleRect != null)
            {
                SetRuntimeRect(toggleRect, new Vector2(430, isExpanded ? -410 : -252), new Vector2(70, 42), new Vector2(0.5f, 1f));
            }
        }

        if (homeShortcutToggleText != null)
        {
            homeShortcutToggleText.text = isExpanded ? "^" : "v";
        }

        if (homeLeftShortcutToggleText != null)
        {
            homeLeftShortcutToggleText.text = isExpanded ? "^" : "v";
        }
    }

    private void RefreshFastRewardsPopupUi()
    {
        if (fastRewardsPopupText == null)
        {
            return;
        }

        var storedSeconds = Mathf.FloorToInt(afkRewardStoredSeconds);
        var pendingGold = CalculateAfkGoldReward(afkRewardStoredSeconds);
        var pendingEssence = CalculateAfkEssenceReward(afkRewardStoredSeconds);
        fastRewardsPopupText.text =
            $"Stored: {FormatDuration(storedSeconds)} / {FormatDuration(GetAfkRewardMaxSeconds())}\n" +
            $"Rate: +{FormatRate(GetAfkGoldPerSecond())} Gold/s   +{FormatRate(GetAfkEssencePerSecond())} Essence/s\n" +
            $"Ready: +{FormatCompactNumber(pendingGold)} Gold   +{FormatCompactNumber(pendingEssence)} Essence";

        if (fastRewardsRedeemButton != null)
        {
            fastRewardsRedeemButton.interactable = pendingGold > 0 || pendingEssence > 0;
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

    private static string FormatRate(float value)
    {
        return value >= 10f ? $"{value:0.#}" : $"{value:0.##}";
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
        HideLegacyRuntimeDungeonTower();
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
            summonTenButton,
            summonResultTenButton,
            summonResultMaxButton,
            summonResultCloseButton,
            summonAutoToggleButton,
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
            fightEndButton,
            homeBeginButton,
            heroRosterTabButton,
            heroSetTeamTabButton,
            heroSortToggleButton,
            heroAttackTypeFilterButton,
            heroAutoSetTeamButton,
            heroDetailCloseButton,
            heroDetailPreviousButton,
            heroDetailNextButton,
            heroDetailLevelButton,
            heroDetailEquipGearButton,
            heroDetailRemoveGearButton);
        runtimeArt.ApplyButtonStyle(heroSelectButtons);
        runtimeArt.ApplyButtonStyle(heroTeamSlotButtons);
        runtimeArt.ApplyButtonStyle(dailyMissionButtons);
        runtimeArt.ApplyButtonStyle(battlePassRewardButtons);
        runtimeArt.ApplyButtonStyle("ui_button_blue", fightButton, homeBeginButton);
        ApplyDungeonCardsButtonSkin();
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

    private static TMP_Text CreateTopResourceCounter(Transform parent, string name, Texture2D icon, Vector2 anchoredPosition, Vector2 size, out RawImage iconImage)
    {
        var chip = CreateRuntimePanel(parent, $"{name} Chip", anchoredPosition, size, new Color(0.025f, 0.035f, 0.035f, 0.82f));
        var iconHeight = Mathf.Min(42f, size.y - 14f);
        var iconWidth = icon != null && icon.height > 0 ? iconHeight * icon.width / icon.height : iconHeight;
        iconImage = CreateRuntimeRawImage(chip, $"{name} Icon", icon, new Vector2((-size.x * 0.5f) + 34f, 0f), new Vector2(iconWidth, iconHeight), new Vector2(0.5f, 0.5f));

        var amountText = CreateRuntimeText(chip, $"{name} Amount", "0", 25, new Vector2(26, 0f), new Vector2(size.x - 84f, size.y - 14f));
        SetRuntimeRect(amountText.rectTransform, new Vector2(26, 0f), new Vector2(size.x - 84f, size.y - 14f), new Vector2(0.5f, 0.5f));
        amountText.alignment = TextAlignmentOptions.MidlineRight;
        amountText.fontStyle = FontStyles.Bold;
        amountText.textWrappingMode = TextWrappingModes.NoWrap;
        amountText.enableAutoSizing = true;
        amountText.fontSizeMin = 16;
        amountText.fontSizeMax = 25;
        return amountText;
    }

    private static TMP_Text CreateTopbarAmountText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        var amountText = CreateRuntimeText(parent, name, "0", 25, anchoredPosition, size);
        SetRuntimeRect(amountText.rectTransform, anchoredPosition, size, new Vector2(0.5f, 1f));
        amountText.alignment = TextAlignmentOptions.MidlineRight;
        amountText.fontStyle = FontStyles.Bold;
        amountText.textWrappingMode = TextWrappingModes.NoWrap;
        amountText.enableAutoSizing = true;
        amountText.fontSizeMin = 16;
        amountText.fontSizeMax = 25;
        amountText.color = Color.white;
        return amountText;
    }

    private static RectTransform CreateRuntimePopup(Transform parent, string name, Vector2 anchoredPosition, Vector2 rectSize, string title)
    {
        var popup = CreateRuntimePanel(parent, name, anchoredPosition, rectSize, new Color(0.08f, 0.045f, 0.025f, 0.97f));
        var image = popup.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        var titleText = CreateRuntimeText(popup, "Title", title, 32, new Vector2(0, -28), new Vector2(rectSize.x - 80f, 48));
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.9f, 0.65f);
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        var divider = CreateRuntimePanel(popup, "Divider", new Vector2(0, -82), new Vector2(rectSize.x - 90f, 4), new Color(0.8f, 0.55f, 0.24f, 0.85f));
        divider.SetAsFirstSibling();
        return popup;
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

    private static void CreateLayeredRuntimeBackground(Transform parent, Vector2 size, float alpha)
    {
        var sky = CreateRuntimeRawImage(parent, "Runtime Sky Layer", LoadRuntimeTexture("bg_sky"), new Vector2(0, -8), new Vector2(size.x, size.y * 0.72f), new Vector2(0.5f, 1f));
        sky.color = new Color(0.62f, 0.82f, 1f, alpha);

        var clouds = CreateRuntimeRawImage(parent, "Runtime Cloud Layer", LoadRuntimeTexture("bg_clouds"), new Vector2(0, -95), new Vector2(size.x * 0.88f, size.y * 0.14f), new Vector2(0.5f, 1f));
        clouds.color = new Color(1f, 1f, 1f, alpha * 0.62f);

        var mountains = CreateRuntimeRawImage(parent, "Runtime Mountain Layer", LoadRuntimeTexture("bg_mountains"), new Vector2(0, -size.y * 0.28f), new Vector2(size.x * 0.94f, size.y * 0.24f), new Vector2(0.5f, 1f));
        mountains.color = new Color(0.68f, 0.75f, 0.98f, alpha * 0.92f);

        var hills = CreateRuntimeRawImage(parent, "Runtime Hill Layer", LoadRuntimeTexture("bg_hills"), new Vector2(0, -size.y * 0.48f), new Vector2(size.x * 0.96f, size.y * 0.24f), new Vector2(0.5f, 1f));
        hills.color = new Color(0.56f, 0.72f, 0.62f, alpha * 0.95f);

        var castle = CreateRuntimeRawImage(parent, "Runtime Castle Accent", LoadRuntimeTexture("bg_castle"), new Vector2(size.x * 0.32f, -size.y * 0.42f), new Vector2(size.x * 0.13f, size.x * 0.13f), new Vector2(0.5f, 1f));
        castle.color = new Color(1f, 1f, 1f, alpha);

        var tree = CreateRuntimeRawImage(parent, "Runtime Tree Accent", LoadRuntimeTexture("bg_tree"), new Vector2(-size.x * 0.36f, -size.y * 0.52f), new Vector2(size.x * 0.11f, size.x * 0.14f), new Vector2(0.5f, 1f));
        tree.color = new Color(1f, 1f, 1f, alpha);
    }

    private static Image CreateRuntimeHealthFill(Transform parent, string name, Vector2 anchoredPosition, float width, Color fillColor)
    {
        var back = CreateRuntimePanel(parent, name, anchoredPosition, new Vector2(width, 15), new Color(0.025f, 0.025f, 0.035f, 0.92f));

        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(back, false);
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

    private static Image CreateRuntimeProjectile(Transform parent, string name, Color color, out RectTransform rect)
    {
        var projectileObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        projectileObject.transform.SetParent(parent, false);
        rect = projectileObject.GetComponent<RectTransform>();
        SetRuntimeRect(rect, new Vector2(0, -520), new Vector2(38, 10), new Vector2(0.5f, 1f));

        var image = projectileObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        projectileObject.SetActive(false);
        return image;
    }

    private static void SetRuntimeFillPercent(Image fill, float percent)
    {
        if (fill == null)
        {
            return;
        }

        var rect = fill.GetComponent<RectTransform>();
        rect.anchorMax = new Vector2(Mathf.Clamp01(percent), 1f);
        rect.offsetMax = Vector2.zero;
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

    private static Button CreateRuntimeImageButton(Transform parent, string name, Texture2D texture, Vector2 anchoredPosition, Vector2 rectSize, out RawImage image)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRuntimeRect(buttonObject.GetComponent<RectTransform>(), anchoredPosition, rectSize, new Vector2(0.5f, 1f));

        image = buttonObject.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = true;
        image.color = texture != null ? Color.white : new Color(1f, 1f, 1f, 0.08f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
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

    private static Texture2D[] LoadCombatAnimationFrames(string textureName, string clipName, string fallbackTextureName)
    {
        const int maxFrameCount = 16;

        var frames = new Texture2D[maxFrameCount];
        var frameCount = 0;
        for (var i = 0; i < maxFrameCount; i++)
        {
            var texture = LoadCombatTexture(textureName, clipName, i, null);
            if (texture == null)
            {
                continue;
            }

            frames[frameCount++] = texture;
        }

        if (frameCount == 0)
        {
            var fallback = LoadRuntimeTexture(fallbackTextureName);
            return fallback == null ? Array.Empty<Texture2D>() : new[] { fallback };
        }

        Array.Resize(ref frames, frameCount);
        return frames;
    }

    private static Texture2D LoadCombatTexture(string textureName, string clipName, int frameIndex, string fallbackTextureName)
    {
        if (!string.IsNullOrWhiteSpace(textureName) && !string.IsNullOrWhiteSpace(clipName))
        {
            var resourcePath = $"Mythwake/Art/CombatAnimated/{textureName}_{clipName}_{frameIndex:00}";
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Bilinear;
                return texture;
            }

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                sprite.texture.filterMode = FilterMode.Bilinear;
                return sprite.texture;
            }
        }

        return LoadRuntimeTexture(fallbackTextureName);
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

    private Texture2D GetHomeGeneratedTexture(string textureName)
    {
        Texture2D texture = textureName switch
        {
            "home_topbar_frame" => homeTopbarFrameTexture,
            "home_battle_button" => homeBattleButtonTexture,
            "home_quest_button" => homeQuestButtonTexture,
            "home_rewards_button" => homeRewardsButtonTexture,
            "home_fast_rewards_button" => homeFastRewardsButtonTexture != null ? homeFastRewardsButtonTexture : homeRewardsButtonTexture,
            "home_treasure_chest_button" => homeTreasureChestButtonTexture,
            "home_shop_button" => homeShopButtonTexture,
            "home_stage_level_badge" => homeStageLevelBadgeTexture,
            "home_stage_mode_badge" => homeStageModeBadgeTexture,
            "home_stage_extra_badge" => homeStageExtraBadgeTexture,
            "home_world_map_button" => homeWorldMapButtonTexture,
            "home_chat_button" => homeChatButtonTexture,
            "home_power_icon" => homePowerIconTexture,
            _ => null
        };

        if (texture != null)
        {
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        var resourcesTexture = Resources.Load<Texture2D>($"Mythwake/UI/HomeScreen/Generated/{textureName}");
        if (resourcesTexture != null)
        {
            resourcesTexture.filterMode = FilterMode.Bilinear;
            return resourcesTexture;
        }

#if UNITY_EDITOR
        var editorTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Mythwake/UI/Home Screen/generated/{textureName}.png");
        if (editorTexture != null)
        {
            editorTexture.filterMode = FilterMode.Bilinear;
            return editorTexture;
        }
#endif

        return null;
    }

    private Texture2D GetCurrencyIconTexture(string iconName)
    {
        Texture2D texture = iconName switch
        {
            "exp_shard" => expShardIconTexture,
            "gold_coin" => goldCoinIconTexture,
            "mythic_gem" => mythicGemIconTexture,
            _ => null
        };

        if (texture != null)
        {
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        var resourcesTexture = Resources.Load<Texture2D>($"Mythwake/UI/icons/{iconName}");
        if (resourcesTexture != null)
        {
            resourcesTexture.filterMode = FilterMode.Bilinear;
            return resourcesTexture;
        }

#if UNITY_EDITOR
        var editorTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Mythwake/UI/icons/{iconName}.png");
        if (editorTexture != null)
        {
            editorTexture.filterMode = FilterMode.Bilinear;
            return editorTexture;
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

    private static string FormatFightTimer(int seconds)
    {
        seconds = Mathf.Max(0, seconds);
        return $"{seconds / 60:00}:{seconds % 60:00}";
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
