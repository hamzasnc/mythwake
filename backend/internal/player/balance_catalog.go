package player

import (
	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
)

type BalanceCatalog interface {
	CampaignEnemyCombatStats(stage int) balance.EnemyCombatStats
	CampaignReward(stage int) api.Reward
	DungeonDefinitionByID(dungeonID string) (balance.DungeonDefinition, bool)
	DungeonEnemyCombatStats(definition balance.DungeonDefinition, floor int) balance.EnemyCombatStats
	DungeonReward(definition balance.DungeonDefinition, floor int) api.Reward
	GearDungeonDropAccessoryID(floor int) string
	GearDungeonReward() api.Reward
	AFKReward(stage int, elapsedSeconds int) (api.Reward, int)
	AFKMinClaimSeconds() int
	RewardAFKClaim() string
	HeroLevelCost(level int) int
	HeroAscensionShardCost(ascension int) int
	EquipmentLevelCost(equipmentID string, level int) (int, bool)
	AccessoryLevelCost(accessoryID string, level int) int
	SummonCost(bannerID string) (int, bool)
	SummonShardReward(bannerID string, summonCount int) (balance.SummonShardDrop, bool)
	DailyMissionDefinitionByID(missionID string) (balance.DailyMissionDefinition, bool)
	DailyMissionDefinitions() []balance.DailyMissionDefinition
	DailyMissionReward(missionID string) (api.Reward, bool)
	BattlePassRequiredXP(rewardID string) (int, bool)
	BattlePassReward(rewardID string) (api.Reward, bool)
}

type StaticBalanceCatalog struct{}

func (StaticBalanceCatalog) CampaignEnemyCombatStats(stage int) balance.EnemyCombatStats {
	return balance.CampaignEnemyCombatStats(stage)
}

func (StaticBalanceCatalog) CampaignReward(stage int) api.Reward {
	return balance.CampaignReward(stage)
}

func (StaticBalanceCatalog) DungeonDefinitionByID(dungeonID string) (balance.DungeonDefinition, bool) {
	return balance.DungeonDefinitionByID(dungeonID)
}

func (StaticBalanceCatalog) DungeonEnemyCombatStats(definition balance.DungeonDefinition, floor int) balance.EnemyCombatStats {
	return balance.DungeonEnemyCombatStats(definition, floor)
}

func (StaticBalanceCatalog) DungeonReward(definition balance.DungeonDefinition, floor int) api.Reward {
	return balance.DungeonReward(definition, floor)
}

func (StaticBalanceCatalog) GearDungeonDropAccessoryID(floor int) string {
	return balance.GearDungeonDropAccessoryID(floor)
}

func (StaticBalanceCatalog) GearDungeonReward() api.Reward {
	return balance.GearDungeonReward()
}

func (StaticBalanceCatalog) AFKReward(stage int, elapsedSeconds int) (api.Reward, int) {
	return balance.AFKReward(stage, elapsedSeconds)
}

func (StaticBalanceCatalog) AFKMinClaimSeconds() int {
	return balance.AFKMinClaimSeconds
}

func (StaticBalanceCatalog) RewardAFKClaim() string {
	return balance.RewardAFKClaim
}

func (StaticBalanceCatalog) HeroLevelCost(level int) int {
	return balance.HeroLevelCost(level)
}

func (StaticBalanceCatalog) HeroAscensionShardCost(ascension int) int {
	return balance.HeroAscensionShardCost(ascension)
}

func (StaticBalanceCatalog) EquipmentLevelCost(equipmentID string, level int) (int, bool) {
	return balance.EquipmentLevelCost(equipmentID, level)
}

func (StaticBalanceCatalog) AccessoryLevelCost(accessoryID string, level int) int {
	return balance.AccessoryLevelCost(accessoryID, level)
}

func (StaticBalanceCatalog) SummonCost(bannerID string) (int, bool) {
	return balance.SummonCost(bannerID)
}

func (StaticBalanceCatalog) SummonShardReward(bannerID string, summonCount int) (balance.SummonShardDrop, bool) {
	return balance.SummonShardReward(bannerID, summonCount)
}

func (StaticBalanceCatalog) DailyMissionDefinitionByID(missionID string) (balance.DailyMissionDefinition, bool) {
	return balance.DailyMissionDefinitionByID(missionID)
}

func (StaticBalanceCatalog) DailyMissionDefinitions() []balance.DailyMissionDefinition {
	return balance.DailyMissionDefinitions()
}

func (StaticBalanceCatalog) DailyMissionReward(missionID string) (api.Reward, bool) {
	return balance.DailyMissionReward(missionID)
}

func (StaticBalanceCatalog) BattlePassRequiredXP(rewardID string) (int, bool) {
	return balance.BattlePassRequiredXP(rewardID)
}

func (StaticBalanceCatalog) BattlePassReward(rewardID string) (api.Reward, bool) {
	return balance.BattlePassReward(rewardID)
}
