package player

import (
	"context"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

func (service *Service) RunTowerWithRequest(ctx context.Context, request ActionRequest, floor int) api.ActionResult {
	return service.dungeonActions.RunTower(ctx, request, floor)
}

func (actions dungeonActions) RunTower(ctx context.Context, request ActionRequest, floor int) api.ActionResult {
	service := actions.service
	service.mu.Lock()
	defer service.mu.Unlock()

	return service.executeAction(ctx, request, gameplay.ActionTowerRun, func() actionOutcome {
		return actions.runTower(floor)
	})
}

func (actions dungeonActions) runTower(floor int) actionOutcome {
	service := actions.service
	service.normalizeTowerProgress()
	definition, ok := service.balanceCatalog.TowerDefinitionByID(towerDungeonID)
	if !ok {
		return actionFailure("invalid_tower", "Tower definition is unavailable.")
	}

	floor = max(1, floor)
	if floor != service.towerHighestUnlockedFloor || floor > definition.MaxFloor || floor <= service.towerHighestClearedFloor {
		return actionFailure("tower_floor_not_ready", fmt.Sprintf("Tower floor %d is not the active unlocked floor.", floor))
	}

	stats := service.balanceCatalog.TowerEnemyCombatStats(definition, floor)
	combat := service.simulateCombat(combatEnemy{
		mode:        "tower",
		targetID:    towerDungeonID,
		targetLevel: floor,
		maxHP:       stats.MaxHP,
		damage:      stats.Damage,
		maxSeconds:  stats.MaxSeconds,
	})
	service.dailyFightCount++
	label := fmt.Sprintf("%s Floor %d", definition.DisplayName, floor)
	if bossType := balance.TowerBossType(definition, floor); bossType != balance.TowerBossNone {
		label = fmt.Sprintf("%s [%s]", label, bossType)
	}
	if !combat.Won {
		return actionFailureWithCombat("combat_lost", formatCombatMessage(label, combat), combat, true)
	}

	reward := service.balanceCatalog.TowerReward(definition, floor)
	economy.Grant(&service.state, reward)
	for _, shardReward := range reward.HeroShards {
		if shardReward.HeroID == "" || shardReward.Shards <= 0 {
			continue
		}
		service.heroShards[shardReward.HeroID] += shardReward.Shards
	}
	service.towerHighestClearedFloor = max(service.towerHighestClearedFloor, floor)
	service.towerHighestUnlockedFloor = min(max(1, definition.MaxFloor), max(service.towerHighestUnlockedFloor, floor+1))
	service.towerSelectedFloor = service.towerHighestUnlockedFloor
	service.normalizeTowerProgress()

	message := formatCombatMessage(label, combat)
	if reward.Gold > 0 {
		message = fmt.Sprintf("%s Reward +%d Gold.", message, reward.Gold)
	}
	if reward.MythEssence > 0 {
		message = fmt.Sprintf("%s Reward +%d Myth Essence.", message, reward.MythEssence)
	}
	for _, shardReward := range reward.HeroShards {
		if shardReward.Shards > 0 {
			message = fmt.Sprintf("%s Reward +%d %s Shards.", message, shardReward.Shards, shardReward.HeroID)
		}
	}
	return actionSuccessWithCombat(message, reward, combat)
}
