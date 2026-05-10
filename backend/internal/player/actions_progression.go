package player

import (
	"context"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

func (service *Service) LevelHero(heroID string) api.ActionResult {
	return service.LevelHeroWithRequest(context.Background(), ActionRequest{}, heroID)
}

func (service *Service) LevelHeroWithRequest(ctx context.Context, request ActionRequest, heroID string) api.ActionResult {
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionHeroLevel, func() actionOutcome {
		level, ok := service.heroLevels[heroID]
		if !ok {
			return actionFailure("invalid_hero", fmt.Sprintf("Unknown hero: %s", heroID))
		}

		cost := balance.HeroLevelCost(level)
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
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionHeroAscend, func() actionOutcome {
		if _, ok := service.heroLevels[heroID]; !ok {
			return actionFailure("invalid_hero", fmt.Sprintf("Unknown hero: %s", heroID))
		}

		cost := balance.HeroAscensionShardCost(service.heroAscensions[heroID])
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
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionEquipmentLevel, func() actionOutcome {
		level, ok := service.equipmentLevels[equipmentID]
		if !ok {
			return actionFailure("invalid_equipment", fmt.Sprintf("Unknown equipment: %s", equipmentID))
		}

		cost, _ := balance.EquipmentLevelCost(equipmentID, level)
		if failure, ok := service.spendCurrency(economy.CurrencyGold, cost); !ok {
			return failure
		}

		service.equipmentLevels[equipmentID] = level + 1
		service.recalculatePower()
		return actionSuccess(fmt.Sprintf("%s reached Lv. %d.", equipmentID, level+1), api.Reward{})
	})
}
