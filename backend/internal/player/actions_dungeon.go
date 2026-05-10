package player

import (
	"context"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

func (service *Service) RunDungeon(dungeonID string) api.ActionResult {
	return service.RunDungeonWithRequest(context.Background(), ActionRequest{}, dungeonID)
}

func (service *Service) RunDungeonWithRequest(ctx context.Context, request ActionRequest, dungeonID string) api.ActionResult {
	return service.dungeonActions.RunDungeon(ctx, request, dungeonID)
}

func (actions dungeonActions) RunDungeon(ctx context.Context, request ActionRequest, dungeonID string) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, dungeonActionID(dungeonID), func() actionOutcome {
		switch dungeonID {
		case goldDungeonID:
			return actions.runResourceDungeon(dungeonID, service.state.GoldDungeonFloor, true)
		case essenceDungeonID:
			return actions.runResourceDungeon(dungeonID, service.state.EssenceDungeonFloor, false)
		case gearDungeonID:
			return actions.runGearDungeon()
		default:
			return actionFailure("invalid_dungeon", fmt.Sprintf("Unknown dungeon: %s", dungeonID))
		}
	})
}

func dungeonActionID(dungeonID string) string {
	switch dungeonID {
	case goldDungeonID:
		return gameplay.ActionGoldDungeonRun
	case essenceDungeonID:
		return gameplay.ActionEssenceDungeonRun
	case gearDungeonID:
		return gameplay.ActionGearDungeonRun
	default:
		return gameplay.ActionDungeonRun
	}
}

func (actions dungeonActions) runResourceDungeon(dungeonID string, floor int, isGold bool) actionOutcome {
	service := actions.service
	definition, ok := balance.DungeonDefinitionByID(dungeonID)
	if !ok {
		return actionFailure("invalid_dungeon", fmt.Sprintf("Unknown dungeon: %s", dungeonID))
	}

	requiredPower := balance.DungeonRequiredPower(definition, floor)
	if service.state.TeamPower < requiredPower {
		return actionFailure("combat_lost", fmt.Sprintf("Floor %d failed. Required Power %d.", floor, requiredPower))
	}

	reward := balance.DungeonReward(definition, floor)
	if isGold {
		service.state.GoldDungeonFloor++
	} else {
		service.state.EssenceDungeonFloor++
	}
	service.dailyFightCount++

	economy.Grant(&service.state, reward)
	return actionSuccess(fmt.Sprintf("%s floor %d cleared.", dungeonID, floor), reward)
}

func (actions dungeonActions) runGearDungeon() actionOutcome {
	service := actions.service
	floor := service.state.GearDungeonFloor
	definition, ok := balance.DungeonDefinitionByID(gearDungeonID)
	if !ok {
		return actionFailure("invalid_dungeon", fmt.Sprintf("Unknown dungeon: %s", gearDungeonID))
	}

	requiredPower := balance.DungeonRequiredPower(definition, floor)
	if service.state.TeamPower < requiredPower {
		return actionFailure("combat_lost", fmt.Sprintf("Floor %d failed. Required Power %d.", floor, requiredPower))
	}

	accessoryID := balance.GearDungeonDropAccessoryID(floor)
	service.accessoryInventory[accessoryID]++
	service.state.GearDungeonFloor++
	service.dailyFightCount++
	return actionSuccess(fmt.Sprintf("Dropped %s.", accessoryID), balance.GearDungeonReward())
}
