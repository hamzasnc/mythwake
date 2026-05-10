using System;

[Serializable]
public struct MythwakePlayerStateDto
{
    public int saveVersion;
    public int gold;
    public int gems;
    public int mythEssence;
    public int passXp;
    public int campaignStage;
    public int goldDungeonFloor;
    public int essenceDungeonFloor;
    public int gearDungeonFloor;
    public int teamPower;
    public int teamAttack;
    public int teamHealth;
}

[Serializable]
public struct MythwakePlayerSnapshotDto
{
    public string playerId;
    public MythwakePlayerStateDto state;
    public string lastAfkClaimUtc;
    public string dailyDate;
    public MythwakeDailyProgressDto[] dailyProgress;
    public MythwakeHeroStateDto[] heroes;
    public MythwakeHeroShardStateDto[] heroShards;
    public MythwakeEquipmentStateDto[] equipment;
    public MythwakeAccessoryStateDto[] accessories;
    public MythwakeEquippedAccessoryDto[] equippedAccessories;
    public MythwakeClaimStateDto[] dailyClaims;
    public MythwakeClaimStateDto[] battlePassClaims;
    public int summonCount;
}

[Serializable]
public struct MythwakeHeroStateDto
{
    public string heroId;
    public int level;
    public int ascension;
}

[Serializable]
public struct MythwakeHeroShardStateDto
{
    public string heroId;
    public int shards;
}

[Serializable]
public struct MythwakeEquipmentStateDto
{
    public string equipmentId;
    public int level;
}

[Serializable]
public struct MythwakeAccessoryStateDto
{
    public string accessoryId;
    public int copies;
    public int level;
}

[Serializable]
public struct MythwakeEquippedAccessoryDto
{
    public string slotId;
    public string accessoryId;
}

[Serializable]
public struct MythwakeClaimStateDto
{
    public string claimId;
    public bool claimed;
}

[Serializable]
public struct MythwakeDailyProgressDto
{
    public string missionId;
    public int progress;
    public int target;
    public bool claimed;
}

[Serializable]
public struct MythwakeRewardDto
{
    public string rewardId;
    public int gold;
    public int gems;
    public int mythEssence;
    public int passXp;
}

[Serializable]
public struct MythwakeCombatResultDto
{
    public string mode;
    public string targetId;
    public int targetLevel;
    public bool won;
    public int elapsedSeconds;
    public int maxSeconds;
    public int teamAttack;
    public int teamMaxHp;
    public int teamHpRemaining;
    public int enemyMaxHp;
    public int enemyHpRemaining;
    public int enemyDamage;
    public int damageDealt;
    public int damageTaken;
}

[Serializable]
public struct MythwakeActionResultDto
{
    public bool success;
    public string actionId;
    public string idempotencyKey;
    public bool replay;
    public string errorCode;
    public string message;
    public MythwakePlayerStateDto playerState;
    public MythwakePlayerSnapshotDto playerSnapshot;
    public MythwakeRewardDto reward;
    public MythwakeCombatResultDto combat;
}

[Serializable]
public struct MythwakeGuestAuthResponseDto
{
    public string playerId;
    public string sessionToken;
    public MythwakePlayerStateDto playerState;
    public MythwakePlayerSnapshotDto playerSnapshot;
}

[Serializable]
public struct MythwakeHealthDto
{
    public string service;
    public string status;
    public string database;
    public string state_cache;
    public string state_write_mode;
    public string state_flush_interval;
    public string environment;
    public string version;
    public string time_utc;
}

[Serializable]
public struct MythwakeServerClockDto
{
    public string serverTimeUtc;
    public long serverUnixMs;
    public string dailyResetUtc;
    public string weeklyResetUtc;
    public long secondsUntilDailyReset;
    public long secondsUntilWeeklyReset;
}

[Serializable]
public struct MythwakeDefinitionSnapshotDto
{
    public int schemaVersion;
    public string apiVersion;
    public string contentHash;
    public MythwakeAuthProviderDefinitionDto[] authProviders;
    public MythwakeCurrencyDefinitionDto[] currencies;
    public MythwakeHeroDefinitionDto[] heroes;
    public MythwakeEquipmentDefinitionDto[] equipment;
    public MythwakeRewardDefinitionDto[] rewards;
    public MythwakeAfkRewardDefinitionDto[] afkRewards;
    public MythwakeCampaignDefinitionDto[] campaigns;
    public MythwakeCampaignStageDefinitionDto[] campaignStages;
    public MythwakeDungeonDefinitionDto[] dungeons;
    public MythwakeAccessorySlotDefinitionDto[] accessorySlots;
    public MythwakeAccessoryRarityDefinitionDto[] accessoryRarities;
    public MythwakeAccessoryDefinitionDto[] accessories;
    public MythwakeProgressionCostDefinitionDto[] progressionCosts;
    public MythwakeSummonBannerDefinitionDto[] summonBanners;
    public MythwakeDailyMissionDefinitionDto[] dailyMissions;
    public MythwakeBattlePassRewardDefinitionDto[] battlePassRewards;
    public MythwakeGameplayActionDefinitionDto[] gameplayActions;
}

[Serializable]
public struct MythwakeAuthProviderDefinitionDto
{
    public string providerId;
    public string displayName;
    public bool externalProvider;
    public bool supportsLinking;
    public bool supportsMobileSso;
}

[Serializable]
public struct MythwakeCurrencyDefinitionDto
{
    public string currencyId;
    public string displayName;
    public bool isPremium;
}

[Serializable]
public struct MythwakeHeroDefinitionDto
{
    public string heroId;
    public string displayName;
    public int sortOrder;
    public bool starterOwned;
    public int maxLevel;
    public int maxAscension;
    public int baseAttack;
    public int attackPerLevel;
    public int attackPerAscension;
    public int baseHealth;
    public int healthPerLevel;
    public int healthPerAscension;
}

[Serializable]
public struct MythwakeEquipmentDefinitionDto
{
    public string equipmentId;
    public string displayName;
    public int sortOrder;
    public bool starterOwned;
    public int maxLevel;
    public int attackPerLevel;
    public int healthPerLevel;
}

[Serializable]
public struct MythwakeRewardDefinitionDto
{
    public string rewardId;
    public string displayName;
    public string rewardType;
    public MythwakeRewardDto reward;
}

[Serializable]
public struct MythwakeAfkRewardDefinitionDto
{
    public string afkRewardId;
    public string rewardId;
    public string displayName;
    public int minClaimSeconds;
    public int maxClaimSeconds;
    public int tickSeconds;
    public int baseMythEssencePerTick;
    public int mythEssencePerStage;
    public int goldPerMythEssenceDivisor;
}

[Serializable]
public struct MythwakeCampaignDefinitionDto
{
    public string campaignId;
    public string displayName;
    public int baseRequiredPower;
    public int requiredPowerPerStage;
    public int baseMythEssenceReward;
    public int mythEssenceRewardPerStage;
    public int milestoneEveryStages;
    public int milestoneBaseGems;
    public int milestoneGemsPerStage;
    public int milestonePassXp;
    public int enemyBaseHp;
    public int enemyHpPerPower;
    public int enemyHpPerStageSquared;
    public int enemyBaseDamage;
    public int enemyDamagePerStage;
    public int enemyDamagePowerDivisor;
    public int maxCombatSeconds;
}

[Serializable]
public struct MythwakeCampaignStageDefinitionDto
{
    public string stageId;
    public string campaignId;
    public int stageNumber;
    public string displayName;
    public int requiredPower;
    public string rewardId;
    public string enemyProfileId;
    public int enemyMaxHp;
    public int enemyDamage;
    public int maxCombatSeconds;
}

[Serializable]
public struct MythwakeDungeonDefinitionDto
{
    public string dungeonId;
    public string displayName;
    public string rewardCurrencyId;
    public int baseRequiredPower;
    public int requiredPowerPerFloor;
    public int baseRewardAmount;
    public int rewardPerFloor;
    public int enemyBaseHp;
    public int enemyHpPerPower;
    public int enemyHpPerFloor;
    public int enemyBaseDamage;
    public int enemyDamagePerFloor;
    public int enemyDamagePowerDivisor;
    public int maxCombatSeconds;
}

[Serializable]
public struct MythwakeAccessorySlotDefinitionDto
{
    public string slotId;
    public string displayName;
    public int sortOrder;
}

[Serializable]
public struct MythwakeAccessoryRarityDefinitionDto
{
    public string rarityId;
    public int rarityIndex;
    public string displayName;
    public int maxLevel;
    public int fuseCopyCost;
}

[Serializable]
public struct MythwakeAccessoryDefinitionDto
{
    public string accessoryId;
    public string slotId;
    public string rarityId;
    public int attackPerLevel;
    public int healthPerLevel;
    public int dropWeight;
    public string fuseTargetId;
}

[Serializable]
public struct MythwakeProgressionCostDefinitionDto
{
    public string costId;
    public string domain;
    public string targetId;
    public string costCurrencyId;
    public int baseAmount;
    public int amountPerLevel;
    public string formula;
}

[Serializable]
public struct MythwakeSummonBannerDefinitionDto
{
    public string bannerId;
    public string displayName;
    public string costCurrencyId;
    public int costAmount;
    public string resolutionMode;
    public MythwakeSummonShardDropDefinitionDto[] shardDrops;
}

[Serializable]
public struct MythwakeSummonShardDropDefinitionDto
{
    public string heroId;
    public int shards;
    public string rewardId;
}

[Serializable]
public struct MythwakeDailyMissionDefinitionDto
{
    public string missionId;
    public string displayName;
    public string progressType;
    public int target;
    public MythwakeRewardDto reward;
}

[Serializable]
public struct MythwakeBattlePassRewardDefinitionDto
{
    public string rewardId;
    public int requiredPassXp;
    public MythwakeRewardDto reward;
}

[Serializable]
public struct MythwakeGameplayActionDefinitionDto
{
    public string actionId;
    public string domain;
    public bool requiresIdempotency;
    public bool materializedByFlush;
}

public interface IMythwakePlayerStateService
{
    MythwakePlayerStateDto GetPlayerState();
}

public interface IMythwakePlayerSnapshotService
{
    MythwakePlayerSnapshotDto GetPlayerSnapshot();
}

public interface IMythwakeDefinitionService
{
    bool TryGetDefinitions(out MythwakeDefinitionSnapshotDto definitions);
}

public interface IMythwakeEconomyService
{
    bool TrySpendCurrency(string currencyId, int amount);
    void GrantCurrency(string currencyId, int amount);
    MythwakeRewardDto GrantReward(MythwakeRewardDto reward);
}

public interface IMythwakeBattleService
{
    MythwakeActionResultDto FightCampaign();
    MythwakeActionResultDto RunDungeon(string dungeonId);
}

public interface IMythwakeSummonService
{
    MythwakeActionResultDto Pull(string bannerId);
}

public interface IMythwakeInventoryService
{
    MythwakeActionResultDto EquipAccessory(string accessoryId);
    MythwakeActionResultDto LevelAccessory(string accessoryId);
    MythwakeActionResultDto FuseAccessory(string accessoryId);
}

public interface IMythwakeProgressionService
{
    MythwakeActionResultDto LevelHero(string heroId);
    MythwakeActionResultDto AscendHero(string heroId);
    MythwakeActionResultDto LevelEquipment(string equipmentId);
}

public interface IMythwakeMissionService
{
    MythwakeActionResultDto ClaimDailyMission(string missionId);
    MythwakeActionResultDto ClaimBattlePassReward(string rewardId);
}
