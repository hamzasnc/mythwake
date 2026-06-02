package player

import (
	"context"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

const (
	heroStarMaxLevel         = 5
	heroStarBaseShardCost    = 5
	heroStarShardCostPerStep = 5
	heroShardChestShardBase  = 5
)

func (service *Service) LevelHero(heroID string) api.ActionResult {
	return service.LevelHeroWithRequest(context.Background(), ActionRequest{}, heroID)
}

func (service *Service) LevelHeroWithRequest(ctx context.Context, request ActionRequest, heroID string) api.ActionResult {
	return service.heroActions.LevelHero(ctx, request, heroID)
}

func (actions heroProgressionActions) LevelHero(ctx context.Context, request ActionRequest, heroID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionHeroLevel, func() actionOutcome {
		level, ok := service.heroLevels[heroID]
		if !ok {
			return actionFailure("invalid_hero", fmt.Sprintf("Unknown hero: %s", heroID))
		}
		definition, ok := service.balanceCatalog.HeroDefinitionByID(heroID)
		if !ok {
			return actionFailure("invalid_hero", fmt.Sprintf("Unknown hero definition: %s", heroID))
		}
		if definition.MaxLevel > 0 && level >= definition.MaxLevel {
			return actionFailure("max_level", fmt.Sprintf("%s is already Lv. %d.", heroID, level))
		}

		cost := service.balanceCatalog.HeroLevelCost(level)
		if failure, ok := service.spendCurrency(economy.CurrencyMythEssence, cost); !ok {
			return failure
		}

		service.heroLevels[heroID] = level + 1
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s reached Lv. %d.", heroID, level+1), api.Reward{})
	})
}

func (service *Service) AscendHero(heroID string) api.ActionResult {
	return service.AscendHeroWithRequest(context.Background(), ActionRequest{}, heroID)
}

func (service *Service) AscendHeroWithRequest(ctx context.Context, request ActionRequest, heroID string) api.ActionResult {
	return service.heroActions.AscendHero(ctx, request, heroID)
}

func (actions heroProgressionActions) AscendHero(ctx context.Context, request ActionRequest, heroID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionHeroAscend, func() actionOutcome {
		if _, ok := service.heroLevels[heroID]; !ok {
			return actionFailure("invalid_hero", fmt.Sprintf("Unknown hero: %s", heroID))
		}
		definition, ok := service.balanceCatalog.HeroDefinitionByID(heroID)
		if !ok {
			return actionFailure("invalid_hero", fmt.Sprintf("Unknown hero definition: %s", heroID))
		}
		currentLevel := service.heroLevels[heroID]
		if definition.MaxLevel > 0 && currentLevel < definition.MaxLevel {
			return actionFailure("level_required", fmt.Sprintf("%s must reach Lv. %d before Awakening.", heroID, definition.MaxLevel))
		}

		currentAscension := service.heroAscensions[heroID]
		if definition.MaxAscension > 0 && currentAscension >= definition.MaxAscension {
			return actionFailure("max_ascension", fmt.Sprintf("%s is already Awakening %d.", heroID, currentAscension))
		}

		cost := service.balanceCatalog.HeroAscensionShardCost(currentAscension)
		if failure, ok := service.spendCurrency(economy.CurrencyAwakeningShards, cost); !ok {
			failure.errorCode = "insufficient_awakening_shards"
			failure.message = fmt.Sprintf("Need %d Awakening Shards.", cost)
			return failure
		}

		service.heroAscensions[heroID]++
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s awakened to %d.", heroID, service.heroAscensions[heroID]), api.Reward{})
	})
}

func (service *Service) UpgradeHeroStar(heroID string) api.ActionResult {
	return service.UpgradeHeroStarWithRequest(context.Background(), ActionRequest{}, heroID)
}

func (service *Service) UpgradeHeroStarWithRequest(ctx context.Context, request ActionRequest, heroID string) api.ActionResult {
	return service.heroActions.UpgradeHeroStar(ctx, request, heroID)
}

func (actions heroProgressionActions) UpgradeHeroStar(ctx context.Context, request ActionRequest, heroID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionHeroStarUpgrade, func() actionOutcome {
		if _, ok := service.heroLevels[heroID]; !ok {
			return actionFailure("invalid_hero", fmt.Sprintf("Unknown hero: %s", heroID))
		}

		currentStar := clampHeroStarLevel(service.heroStars[heroID])
		if currentStar >= heroStarMaxLevel {
			return actionFailure("max_star_level", fmt.Sprintf("%s is already Star %d.", heroID, heroStarMaxLevel))
		}

		cost := heroStarUpgradeCost(currentStar)
		if service.heroShards[heroID] < cost {
			return actionFailure("insufficient_hero_shards", fmt.Sprintf("Need %d %s shards.", cost, heroID))
		}

		service.heroShards[heroID] -= cost
		service.heroStars[heroID] = currentStar + 1
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s reached Star %d.", heroID, service.heroStars[heroID]), api.Reward{})
	})
}

func (service *Service) OpenHeroShardChest() api.ActionResult {
	return service.OpenHeroShardChestWithRequest(context.Background(), ActionRequest{})
}

func (service *Service) OpenHeroShardChestWithRequest(ctx context.Context, request ActionRequest) api.ActionResult {
	return service.heroActions.OpenHeroShardChest(ctx, request)
}

func (actions heroProgressionActions) OpenHeroShardChest(ctx context.Context, request ActionRequest) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionHeroShardChestOpen, func() actionOutcome {
		if service.heroShardChests <= 0 {
			return actionFailure("missing_chest", "No Hero Shard Chest available.")
		}

		heroes := service.balanceCatalog.HeroDefinitions()
		if len(heroes) == 0 {
			return actionFailure("invalid_hero_pool", "No heroes are available for Hero Shard Chest rewards.")
		}

		index := max(0, service.summonCount+service.shardRiftTotal+service.heroShardChests) % len(heroes)
		heroID := heroes[index].ID
		shards := heroShardChestShardBase + min(max(0, service.state.CampaignStage/10), 10)
		service.heroShardChests--
		service.heroShards[heroID] += shards
		return actionSuccess(
			fmt.Sprintf("Hero Shard Chest opened: +%d %s shards.", shards, heroID),
			api.Reward{RewardID: balance.RewardHeroShardChest},
		)
	})
}

func heroStarUpgradeCost(currentStar int) int {
	return heroStarBaseShardCost + (clampHeroStarLevel(currentStar) * heroStarShardCostPerStep)
}

func (service *Service) LevelEquipment(equipmentID string) api.ActionResult {
	return service.LevelEquipmentWithRequest(context.Background(), ActionRequest{}, equipmentID)
}

func (service *Service) LevelEquipmentWithRequest(ctx context.Context, request ActionRequest, equipmentID string) api.ActionResult {
	return service.equipmentActions.LevelEquipment(ctx, request, equipmentID)
}

func (actions equipmentActions) LevelEquipment(ctx context.Context, request ActionRequest, equipmentID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionEquipmentLevel, func() actionOutcome {
		level, ok := service.equipmentLevels[equipmentID]
		if !ok {
			return actionFailure("invalid_equipment", fmt.Sprintf("Unknown equipment: %s", equipmentID))
		}
		definition, ok := service.balanceCatalog.EquipmentDefinitionByID(equipmentID)
		if !ok {
			return actionFailure("invalid_equipment", fmt.Sprintf("Unknown equipment definition: %s", equipmentID))
		}
		if definition.MaxLevel > 0 && level >= definition.MaxLevel {
			return actionFailure("max_level", fmt.Sprintf("%s is already Lv. %d.", equipmentID, level))
		}

		cost, ok := service.balanceCatalog.EquipmentLevelCost(equipmentID, level)
		if !ok {
			return actionFailure("invalid_cost", fmt.Sprintf("Missing equipment level cost for %s.", equipmentID))
		}
		if failure, ok := service.spendCurrency(economy.CurrencyGold, cost); !ok {
			return failure
		}

		service.equipmentLevels[equipmentID] = level + 1
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s reached Lv. %d.", equipmentID, level+1), api.Reward{})
	})
}
