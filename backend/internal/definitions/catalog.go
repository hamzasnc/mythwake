package definitions

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/auth"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

const SchemaVersion = 3

func Snapshot(apiVersion string) api.DefinitionSnapshot {
	snapshot := api.DefinitionSnapshot{
		SchemaVersion:     SchemaVersion,
		APIVersion:        apiVersion,
		AuthProviders:     authProviderDefinitions(),
		Currencies:        currencyDefinitions(),
		Heroes:            heroDefinitions(),
		Rewards:           rewardDefinitions(),
		Campaigns:         campaignDefinitions(),
		CampaignStages:    campaignStageDefinitions(),
		Dungeons:          dungeonDefinitions(),
		AccessorySlots:    accessorySlotDefinitions(),
		AccessoryRarities: accessoryRarityDefinitions(),
		Accessories:       accessoryDefinitions(),
		ProgressionCosts:  progressionCostDefinitions(),
		SummonBanners:     summonBannerDefinitions(),
		DailyMissions:     dailyMissionDefinitions(),
		BattlePassRewards: battlePassRewardDefinitions(),
		GameplayActions:   gameplayActionDefinitions(),
	}
	snapshot.ContentHash = ContentHash(snapshot)
	return snapshot
}

func ContentHash(snapshot api.DefinitionSnapshot) string {
	snapshot.ContentHash = ""
	rawSnapshot, err := json.Marshal(snapshot)
	if err != nil {
		return ""
	}

	sum := sha256.Sum256(rawSnapshot)
	return hex.EncodeToString(sum[:])
}

func ETag(snapshot api.DefinitionSnapshot) string {
	return fmt.Sprintf(`"definitions-%s"`, snapshot.ContentHash)
}

func authProviderDefinitions() []api.AuthProviderDefinition {
	definitions := auth.ProviderDefinitions()
	response := make([]api.AuthProviderDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.AuthProviderDefinition{
			ProviderID:        definition.ID,
			DisplayName:       definition.DisplayName,
			ExternalProvider:  definition.ExternalProvider,
			SupportsLinking:   definition.SupportsLinking,
			SupportsMobileSSO: definition.SupportsMobileSSO,
		})
	}
	return response
}

func currencyDefinitions() []api.CurrencyDefinition {
	definitions := economy.CurrencyDefinitions()
	response := make([]api.CurrencyDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.CurrencyDefinition{
			CurrencyID:  definition.ID,
			DisplayName: definition.DisplayName,
			IsPremium:   definition.IsPremium,
		})
	}
	return response
}

func heroDefinitions() []api.HeroDefinition {
	definitions := balance.HeroDefinitions()
	response := make([]api.HeroDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.HeroDefinition{
			HeroID:             definition.ID,
			DisplayName:        definition.DisplayName,
			SortOrder:          definition.SortOrder,
			StarterOwned:       definition.StarterOwned,
			MaxLevel:           definition.MaxLevel,
			MaxAscension:       definition.MaxAscension,
			BaseAttack:         definition.BaseAttack,
			AttackPerLevel:     definition.AttackPerLevel,
			AttackPerAscension: definition.AttackPerAscension,
			BaseHealth:         definition.BaseHealth,
			HealthPerLevel:     definition.HealthPerLevel,
			HealthPerAscension: definition.HealthPerAscension,
		})
	}
	return response
}

func rewardDefinitions() []api.RewardDefinition {
	definitions := balance.RewardDefinitions()
	response := make([]api.RewardDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.RewardDefinition{
			RewardID:    definition.ID,
			DisplayName: definition.DisplayName,
			RewardType:  definition.RewardType,
			Reward:      definition.Reward,
		})
	}
	return response
}

func campaignDefinitions() []api.CampaignDefinition {
	definitions := balance.CampaignDefinitions()
	response := make([]api.CampaignDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.CampaignDefinition{
			CampaignID:                definition.ID,
			DisplayName:               definition.DisplayName,
			BaseRequiredPower:         definition.BaseRequiredPower,
			RequiredPowerPerStage:     definition.RequiredPowerPerStage,
			BaseMythEssenceReward:     definition.BaseMythEssenceReward,
			MythEssenceRewardPerStage: definition.MythEssenceRewardPerStage,
			MilestoneEveryStages:      definition.MilestoneEveryStages,
			MilestoneBaseGems:         definition.MilestoneBaseGems,
			MilestoneGemsPerStage:     definition.MilestoneGemsPerStage,
			MilestonePassXP:           definition.MilestonePassXP,
			EnemyBaseHP:               definition.EnemyBaseHP,
			EnemyHPPerPower:           definition.EnemyHPPerPower,
			EnemyHPPerStageSquared:    definition.EnemyHPPerStageSquared,
			EnemyBaseDamage:           definition.EnemyBaseDamage,
			EnemyDamagePerStage:       definition.EnemyDamagePerStage,
			EnemyDamagePowerDivisor:   definition.EnemyDamagePowerDiv,
			MaxCombatSeconds:          definition.MaxCombatSeconds,
		})
	}
	return response
}

func campaignStageDefinitions() []api.CampaignStageDefinition {
	definitions := balance.CampaignStageDefinitions()
	response := make([]api.CampaignStageDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.CampaignStageDefinition{
			StageID:          definition.ID,
			CampaignID:       definition.CampaignID,
			StageNumber:      definition.StageNumber,
			DisplayName:      definition.DisplayName,
			RequiredPower:    definition.RequiredPower,
			RewardID:         definition.RewardID,
			EnemyProfileID:   definition.EnemyProfileID,
			EnemyMaxHP:       definition.EnemyMaxHP,
			EnemyDamage:      definition.EnemyDamage,
			MaxCombatSeconds: definition.MaxCombatSeconds,
		})
	}
	return response
}

func dungeonDefinitions() []api.DungeonDefinition {
	definitions := balance.DungeonDefinitions()
	response := make([]api.DungeonDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.DungeonDefinition{
			DungeonID:               definition.ID,
			DisplayName:             definition.DisplayName,
			RewardCurrencyID:        definition.RewardCurrencyID,
			BaseRequiredPower:       definition.BaseRequiredPower,
			RequiredPowerPerFloor:   definition.RequiredPowerPerFloor,
			BaseRewardAmount:        definition.BaseRewardAmount,
			RewardPerFloor:          definition.RewardPerFloor,
			EnemyBaseHP:             definition.EnemyBaseHP,
			EnemyHPPerPower:         definition.EnemyHPPerPower,
			EnemyHPPerFloor:         definition.EnemyHPPerFloor,
			EnemyBaseDamage:         definition.EnemyBaseDamage,
			EnemyDamagePerFloor:     definition.EnemyDamagePerFloor,
			EnemyDamagePowerDivisor: definition.EnemyDamagePowerDiv,
			MaxCombatSeconds:        definition.MaxCombatSeconds,
		})
	}
	return response
}

func accessorySlotDefinitions() []api.AccessorySlotDefinition {
	definitions := balance.AccessorySlotDefinitions()
	response := make([]api.AccessorySlotDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.AccessorySlotDefinition{
			SlotID:      definition.ID,
			DisplayName: definition.DisplayName,
			SortOrder:   definition.SortOrder,
		})
	}
	return response
}

func accessoryRarityDefinitions() []api.AccessoryRarityDefinition {
	definitions := balance.AccessoryRarityDefinitions()
	response := make([]api.AccessoryRarityDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.AccessoryRarityDefinition{
			RarityID:     definition.ID,
			RarityIndex:  definition.RarityIndex,
			DisplayName:  definition.DisplayName,
			MaxLevel:     definition.MaxLevel,
			FuseCopyCost: definition.FuseCopyCost,
		})
	}
	return response
}

func accessoryDefinitions() []api.AccessoryDefinition {
	definitions := balance.AccessoryDefinitions()
	response := make([]api.AccessoryDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.AccessoryDefinition{
			AccessoryID:    definition.ID,
			SlotID:         definition.SlotID,
			RarityID:       definition.RarityID,
			AttackPerLevel: definition.AttackPerLevel,
			HealthPerLevel: definition.HealthPerLevel,
			DropWeight:     definition.DropWeight,
			FuseTargetID:   definition.FuseTargetID,
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
