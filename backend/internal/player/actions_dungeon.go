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
		case shardRiftDungeonID:
			return actions.runShardRiftDungeon()
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
	case shardRiftDungeonID:
		return gameplay.ActionShardRiftRun
	default:
		return gameplay.ActionDungeonRun
	}
}

func (actions dungeonActions) runResourceDungeon(dungeonID string, floor int, isGold bool) actionOutcome {
	service := actions.service
	definition, ok := service.balanceCatalog.DungeonDefinitionByID(dungeonID)
	if !ok {
		return actionFailure("invalid_dungeon", fmt.Sprintf("Unknown dungeon: %s", dungeonID))
	}

	combat := service.simulateCombat(service.dungeonEnemy(definition, floor))
	service.dailyFightCount++
	label := fmt.Sprintf("%s Floor %d", definition.DisplayName, floor)
	if !combat.Won {
		return actionFailureWithCombat("combat_lost", formatCombatMessage(label, combat), combat, true)
	}

	reward := service.balanceCatalog.DungeonReward(definition, floor)
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
	definition, ok := service.balanceCatalog.DungeonDefinitionByID(gearDungeonID)
	if !ok {
		return actionFailure("invalid_dungeon", fmt.Sprintf("Unknown dungeon: %s", gearDungeonID))
	}

	combat := service.simulateCombat(service.dungeonEnemy(definition, floor))
	service.dailyFightCount++
	label := fmt.Sprintf("%s Floor %d", definition.DisplayName, floor)
	if !combat.Won {
		return actionFailureWithCombat("combat_lost", formatCombatMessage(label, combat), combat, true)
	}

	accessoryID := service.balanceCatalog.GearDungeonDropAccessoryID(floor)
	service.accessoryInventory[accessoryID]++
	service.state.GearDungeonFloor++
	message := fmt.Sprintf("%s Dropped %s.", formatCombatMessage(label, combat), accessoryID)
	return actionSuccessWithCombat(message, service.balanceCatalog.GearDungeonReward(), combat)
}

func (actions dungeonActions) runShardRiftDungeon() actionOutcome {
	service := actions.service
	definition, ok := service.balanceCatalog.DungeonDefinitionByID(shardRiftDungeonID)
	if !ok {
		return actionFailure("invalid_dungeon", fmt.Sprintf("Unknown dungeon: %s", shardRiftDungeonID))
	}

	defeated := 0
	var finalCombat api.CombatResult
	for encounter := 1; encounter <= 50; encounter++ {
		enemy := service.dungeonEnemy(definition, encounter)
		enemy.mode = "shard_rift"
		enemy.targetID = shardRiftDungeonID
		finalCombat = service.simulateCombat(enemy)
		if !finalCombat.Won {
			break
		}

		defeated++
	}

	service.dailyFightCount++
	awakeningShards := shardRiftAwakeningShardReward(defeated)
	heroShardChests := shardRiftHeroShardChestReward(defeated)
	reward := api.Reward{
		RewardID:        balance.RewardShardRiftRun,
		AwakeningShards: awakeningShards,
		HeroShardChests: heroShardChests,
	}
	service.state.AwakeningShards += awakeningShards
	service.heroShardChests += heroShardChests
	service.shardRiftTotal += defeated
	if defeated > service.shardRiftBest {
		service.shardRiftBest = defeated
	}

	message := fmt.Sprintf(
		"Shard Rift ended after %d kills. Reward +%d Awakening Shards, +%d Hero Shard Chests.",
		defeated,
		awakeningShards,
		heroShardChests,
	)
	if defeated <= 0 {
		return actionFailureWithCombat("combat_lost", message, finalCombat, true)
	}

	return actionSuccessWithCombat(message, reward, finalCombat)
}

func shardRiftAwakeningShardReward(defeated int) int {
	if defeated <= 0 {
		return 0
	}

	return defeated*2 + defeated/5
}

func shardRiftHeroShardChestReward(defeated int) int {
	if defeated <= 0 {
		return 0
	}

	return defeated / 7
}
