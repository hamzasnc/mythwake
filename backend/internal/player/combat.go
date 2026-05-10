package player

import (
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
)

type combatEnemy struct {
	mode        string
	targetID    string
	targetLevel int
	maxHP       int
	damage      int
	maxSeconds  int
}

func (service *Service) campaignEnemy(stage int) combatEnemy {
	stage = max(1, stage)
	stats := service.balanceCatalog.CampaignEnemyCombatStats(stage)
	return combatEnemy{
		mode:        "campaign",
		targetID:    fmt.Sprintf("campaign_stage_%03d", stage),
		targetLevel: stage,
		maxHP:       stats.MaxHP,
		damage:      stats.Damage,
		maxSeconds:  stats.MaxSeconds,
	}
}

func (service *Service) dungeonEnemy(definition balance.DungeonDefinition, floor int) combatEnemy {
	floor = max(1, floor)
	stats := service.balanceCatalog.DungeonEnemyCombatStats(definition, floor)

	return combatEnemy{
		mode:        "dungeon",
		targetID:    definition.ID,
		targetLevel: floor,
		maxHP:       stats.MaxHP,
		damage:      stats.Damage,
		maxSeconds:  stats.MaxSeconds,
	}
}

func (service *Service) simulateCombat(enemy combatEnemy) api.CombatResult {
	teamHP := max(1, service.state.TeamHealth)
	teamAttack := max(1, service.state.TeamAttack)
	enemyHP := max(1, enemy.maxHP)
	enemyDamage := max(1, enemy.damage)

	result := api.CombatResult{
		Mode:             enemy.mode,
		TargetID:         enemy.targetID,
		TargetLevel:      enemy.targetLevel,
		MaxSeconds:       max(1, enemy.maxSeconds),
		TeamAttack:       teamAttack,
		TeamMaxHP:        teamHP,
		TeamHPRemaining:  teamHP,
		EnemyMaxHP:       enemyHP,
		EnemyHPRemaining: enemyHP,
		EnemyDamage:      enemyDamage,
	}

	for second := 1; second <= result.MaxSeconds; second++ {
		result.ElapsedSeconds = second

		teamDamage := min(teamAttack, enemyHP)
		enemyHP -= teamDamage
		result.DamageDealt += teamDamage
		result.EnemyHPRemaining = enemyHP
		if enemyHP <= 0 {
			result.Won = true
			break
		}

		incomingDamage := min(enemyDamage, teamHP)
		teamHP -= incomingDamage
		result.DamageTaken += incomingDamage
		result.TeamHPRemaining = teamHP
		if teamHP <= 0 {
			break
		}
	}

	result.TeamHPRemaining = max(0, teamHP)
	result.EnemyHPRemaining = max(0, enemyHP)
	return result
}

func formatCombatMessage(label string, combat api.CombatResult) string {
	if combat.Won {
		return fmt.Sprintf("%s cleared in %ds. HP %d/%d, enemy HP %d/%d, dealt %d, took %d.",
			label,
			combat.ElapsedSeconds,
			combat.TeamHPRemaining,
			combat.TeamMaxHP,
			combat.EnemyHPRemaining,
			combat.EnemyMaxHP,
			combat.DamageDealt,
			combat.DamageTaken,
		)
	}

	return fmt.Sprintf("%s failed after %ds. HP %d/%d, enemy HP %d/%d, dealt %d, took %d.",
		label,
		combat.ElapsedSeconds,
		combat.TeamHPRemaining,
		combat.TeamMaxHP,
		combat.EnemyHPRemaining,
		combat.EnemyMaxHP,
		combat.DamageDealt,
		combat.DamageTaken,
	)
}
