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
    public MythwakeRewardDto reward;
}

public interface IMythwakePlayerStateService
{
    MythwakePlayerStateDto GetPlayerState();
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
