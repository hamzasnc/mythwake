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

	combat := service.simulateCombat(dungeonEnemy(definition, floor))
	service.dailyFightCount++
	label := fmt.Sprintf("%s Floor %d", definition.DisplayName, floor)
	if !combat.Won {
		return actionFailureWithCombat("combat_lost", formatCombatMessage(label, combat), combat, true)
	}

	reward := balance.DungeonReward(definition, floor)
	if isGold {
		service.state.GoldDungeonFloor++
	} else {
		service.state.EssenceDungeonFloor++
	}

	economy.Grant(&service.state, reward)
	message := formatCombatMessage(label, combat)
	if reward.Gold > 0 {
		message = fmt.Sprintf("%s Reward +%d Gold.", message, reward.Gold)
	}
	if reward.MythEssence > 0 {
		message = fmt.Sprintf("%s Reward +%d Myth Essence.", message, reward.MythEssence)
	}
	return actionSuccessWithCombat(message, reward, combat)
}

func (actions dungeonActions) runGearDungeon() actionOutcome {
	service := actions.service
	floor := service.state.GearDungeonFloor
	definition, ok := balance.DungeonDefinitionByID(gearDungeonID)
	if !ok {
		return actionFailure("invalid_dungeon", fmt.Sprintf("Unknown dungeon: %s", gearDungeonID))
	}

	combat := service.simulateCombat(dungeonEnemy(definition, floor))
	service.dailyFightCount++
	label := fmt.Sprintf("%s Floor %d", definition.DisplayName, floor)
	if !combat.Won {
		return actionFailureWithCombat("combat_lost", formatCombatMessage(label, combat), combat, true)
	}

	accessoryID := balance.GearDungeonDropAccessoryID(floor)
	service.accessoryInventory[accessoryID]++
	service.state.GearDungeonFloor++
	message := fmt.Sprintf("%s Dropped %s.", formatCombatMessage(label, combat), accessoryID)
	return actionSuccessWithCombat(message, balance.GearDungeonReward(), combat)
}
