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
public struct MythwakeRewardDto
{
    public string rewardId;
    public int gold;
    public int gems;
    public int mythEssence;
    public int passXp;
}

[Serializable]
public struct MythwakeActionResultDto
{
    public bool success;
    public string actionId;
    public string errorCode;
    public string message;
    public MythwakePlayerStateDto playerState;
    public MythwakePlayerSnapshotDto playerSnapshot;
    public MythwakeRewardDto reward;
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
    public string environment;
    public string version;
    public string time_utc;
}

public interface IMythwakePlayerStateService
{
    MythwakePlayerStateDto GetPlayerState();
}

public interface IMythwakePlayerSnapshotService
{
    MythwakePlayerSnapshotDto GetPlayerSnapshot();
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
