package player

import (
	"context"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
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
		currentAscension := service.heroAscensions[heroID]
		if definition.MaxAscension > 0 && currentAscension >= definition.MaxAscension {
			return actionFailure("max_ascension", fmt.Sprintf("%s is already +%d.", heroID, currentAscension))
		}

		cost := service.balanceCatalog.HeroAscensionShardCost(currentAscension)
		if service.heroShards[heroID] < cost {
			return actionFailure("insufficient_shards", fmt.Sprintf("Need %d shards.", cost))
		}

		service.heroShards[heroID] -= cost
		service.heroAscensions[heroID]++
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s ascended.", heroID), api.Reward{})
	})
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

		cost, _ := service.balanceCatalog.EquipmentLevelCost(equipmentID, level)
		if failure, ok := service.spendCurrency(economy.CurrencyGold, cost); !ok {
			return failure
		}

		service.equipmentLevels[equipmentID] = level + 1
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s reached Lv. %d.", equipmentID, level+1), api.Reward{})
	})
}
