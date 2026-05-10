package definitions

import (
	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

const SchemaVersion = 1

func Snapshot(apiVersion string) api.DefinitionSnapshot {
	return api.DefinitionSnapshot{
		SchemaVersion:     SchemaVersion,
		APIVersion:        apiVersion,
		Dungeons:          dungeonDefinitions(),
		ProgressionCosts:  progressionCostDefinitions(),
		SummonBanners:     summonBannerDefinitions(),
		DailyMissions:     dailyMissionDefinitions(),
		BattlePassRewards: battlePassRewardDefinitions(),
		GameplayActions:   gameplayActionDefinitions(),
	}
}

func dungeonDefinitions() []api.DungeonDefinition {
	definitions := balance.DungeonDefinitions()
	response := make([]api.DungeonDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.DungeonDefinition{
			DungeonID:             definition.ID,
			DisplayName:           definition.DisplayName,
			RewardCurrencyID:      definition.RewardCurrencyID,
			BaseRequiredPower:     definition.BaseRequiredPower,
			RequiredPowerPerFloor: definition.RequiredPowerPerFloor,
			BaseRewardAmount:      definition.BaseRewardAmount,
			RewardPerFloor:        definition.RewardPerFloor,
		})
	}
	return response
}

func progressionCostDefinitions() []api.ProgressionCostDefinition {
	definitions := balance.ProgressionCostDefinitions()
	response := make([]api.ProgressionCostDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.ProgressionCostDefinition{
			CostID:         definition.ID,
			Domain:         definition.Domain,
			TargetID:       definition.TargetID,
			CostCurrencyID: definition.CostCurrencyID,
			BaseAmount:     definition.BaseAmount,
			AmountPerLevel: definition.AmountPerLevel,
			Formula:        definition.Formula,
		})
	}
	return response
}

func summonBannerDefinitions() []api.SummonBannerDefinition {
	definitions := balance.SummonBannerDefinitions()
	response := make([]api.SummonBannerDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.SummonBannerDefinition{
			BannerID:       definition.ID,
			DisplayName:    definition.DisplayName,
			CostCurrencyID: definition.CostCurrencyID,
			CostAmount:     definition.CostAmount,
			ResolutionMode: definition.ResolutionMode,
			ShardDrops:     summonShardDrops(definition.ShardDrops),
		})
	}
	return response
}

func summonShardDrops(drops []balance.SummonShardDrop) []api.SummonShardDrop {
	response := make([]api.SummonShardDrop, 0, len(drops))
	for _, drop := range drops {
		response = append(response, api.SummonShardDrop{
			HeroID:   drop.HeroID,
			Shards:   drop.Shards,
			RewardID: drop.Reward.RewardID,
		})
	}
	return response
}

func dailyMissionDefinitions() []api.DailyMissionDefinition {
	definitions := balance.DailyMissionDefinitions()
	response := make([]api.DailyMissionDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.DailyMissionDefinition{
			MissionID:    definition.ID,
			DisplayName:  definition.DisplayName,
			ProgressType: definition.ProgressType,
			Target:       definition.Target,
			Reward:       definition.Reward,
		})
	}
	return response
}

func battlePassRewardDefinitions() []api.BattlePassRewardDefinition {
	definitions := balance.BattlePassRewardDefinitions()
	response := make([]api.BattlePassRewardDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.BattlePassRewardDefinition{
			RewardID:       definition.ID,
			RequiredPassXP: definition.RequiredPassXP,
			Reward:         definition.Reward,
		})
	}
	return response
}

func gameplayActionDefinitions() []api.GameplayActionDefinition {
	definitions := gameplay.ActionCatalog()
	response := make([]api.GameplayActionDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.GameplayActionDefinition{
			ActionID:            definition.ID,
			Domain:              definition.Domain,
			RequiresIdempotency: definition.RequiresIdempotency,
			MaterializedByFlush: definition.MaterializedByFlush,
		})
	}
	return response
}
