package player

import (
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
)

const (
	combatAutoAttackManaGain = 2
	combatReplayStepMS       = 100
	combatMaxReplayEvents    = 180
)

type combatEnemy struct {
	mode        string
	targetID    string
	targetLevel int
	maxHP       int
	damage      int
	maxSeconds  int
}

type combatHeroRuntime struct {
	index              int
	id                 string
	name               string
	attack             int
	critChancePercent  int
	accuracyPercent    int
	defense            int
	maxMana            int
	mana               int
	nextAttackMS       int
	attackIntervalMS   int
	nextUltimateMS     int
	ultimateCooldownMS int
	autoAttackCount    int
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
	enemyHP := max(1, enemy.maxHP)
	enemyDamage := max(1, enemy.damage)
	heroes := service.combatHeroes()
	teamDefense := 0
	for _, hero := range heroes {
		teamDefense += hero.defense
	}

	result := api.CombatResult{
		Mode:             enemy.mode,
		TargetID:         enemy.targetID,
		TargetLevel:      enemy.targetLevel,
		MaxSeconds:       max(1, enemy.maxSeconds),
		TeamAttack:       max(1, service.state.TeamAttack),
		TeamMaxHP:        teamHP,
		TeamHPRemaining:  teamHP,
		EnemyMaxHP:       enemyHP,
		EnemyHPRemaining: enemyHP,
		EnemyDamage:      enemyDamage,
		Heroes:           combatHeroStates(heroes),
	}

	enemyNextAttackMS := 900
	enemyAttackIntervalMS := 1450
	lastSecond := 0
	for timeMS := 0; timeMS <= result.MaxSeconds*1000; timeMS += combatReplayStepMS {
		lastSecond = max(1, (timeMS+999)/1000)

		for i := range heroes {
			hero := &heroes[i]
			if enemyHP <= 0 || timeMS < hero.nextAttackMS {
				continue
			}

			hero.autoAttackCount++
			if !combatPercentCheck(timeMS, hero.index, hero.autoAttackCount, hero.accuracyPercent) {
				result.Events = appendCombatEvent(result.Events, api.CombatEvent{
					TimeMS:           timeMS,
					EventType:        "miss",
					ActorID:          hero.id,
					ActorIndex:       hero.index,
					TargetID:         "enemy",
					TargetIndex:      0,
					Amount:           0,
					ManaAfter:        hero.mana,
					TeamHPRemaining:  teamHP,
					EnemyHPRemaining: enemyHP,
				})
				hero.nextAttackMS = timeMS + hero.attackIntervalMS
				continue
			}

			eventType := "auto_attack"
			damage := hero.attack
			if combatPercentCheck(timeMS+37, hero.index, hero.autoAttackCount, hero.critChancePercent) {
				eventType = "critical_attack"
				damage = max(1, damage*3/2)
			}

			damage = min(damage, enemyHP)
			enemyHP -= damage
			result.DamageDealt += damage
			result.EnemyHPRemaining = enemyHP
			hero.mana = min(hero.maxMana, hero.mana+heroAutoAttackManaGain(hero.id))
			result.Events = appendCombatEvent(result.Events, api.CombatEvent{
				TimeMS:           timeMS,
				EventType:        eventType,
				ActorID:          hero.id,
				ActorIndex:       hero.index,
				TargetID:         "enemy",
				TargetIndex:      0,
				Amount:           damage,
				ManaAfter:        hero.mana,
				TeamHPRemaining:  teamHP,
				EnemyHPRemaining: enemyHP,
			})

			if hero.id == "hero_elowen" && hero.autoAttackCount%4 == 0 && teamHP < result.TeamMaxHP {
				heal := min(max(1, result.TeamMaxHP/20), result.TeamMaxHP-teamHP)
				teamHP += heal
				result.Events = appendCombatEvent(result.Events, api.CombatEvent{
					TimeMS:           timeMS,
					EventType:        "passive_heal",
					ActorID:          hero.id,
					ActorIndex:       hero.index,
					SkillID:          "passive_grove_mending",
					Amount:           heal,
					ManaAfter:        hero.mana,
					TeamHPRemaining:  teamHP,
					EnemyHPRemaining: enemyHP,
				})
			}

			if enemyHP <= 0 {
				result.Won = true
				break
			}

			if hero.mana >= hero.maxMana && timeMS >= hero.nextUltimateMS {
				ultimateDamage, ultimateHeal := heroUltimateEffect(hero.id, hero.attack, result.TeamMaxHP)
				hero.mana = 0
				hero.nextUltimateMS = timeMS + hero.ultimateCooldownMS
				if ultimateDamage > 0 {
					ultimateDamage = min(ultimateDamage, enemyHP)
					enemyHP -= ultimateDamage
					result.DamageDealt += ultimateDamage
				}
				if ultimateHeal > 0 && teamHP < result.TeamMaxHP {
					ultimateHeal = min(ultimateHeal, result.TeamMaxHP-teamHP)
					teamHP += ultimateHeal
				}
				result.EnemyHPRemaining = enemyHP
				result.TeamHPRemaining = teamHP
				result.Events = appendCombatEvent(result.Events, api.CombatEvent{
					TimeMS:           timeMS,
					EventType:        "ultimate",
					ActorID:          hero.id,
					ActorIndex:       hero.index,
					TargetID:         "enemy",
					TargetIndex:      0,
					SkillID:          heroUltimateID(hero.id),
					Amount:           ultimateDamage + ultimateHeal,
					ManaAfter:        hero.mana,
					TeamHPRemaining:  teamHP,
					EnemyHPRemaining: enemyHP,
				})
			}

			hero.nextAttackMS = timeMS + hero.attackIntervalMS
			if enemyHP <= 0 {
				result.Won = true
				break
			}
		}

		if result.Won {
			break
		}

		if timeMS >= enemyNextAttackMS {
			incomingDamage := min(mitigateEnemyDamage(enemyDamage, teamDefense), teamHP)
			teamHP -= incomingDamage
			result.DamageTaken += incomingDamage
			result.TeamHPRemaining = teamHP
			result.Events = appendCombatEvent(result.Events, api.CombatEvent{
				TimeMS:           timeMS,
				EventType:        "enemy_attack",
				ActorID:          "enemy",
				ActorIndex:       0,
				TargetID:         heroes[timeMS/1000%len(heroes)].id,
				TargetIndex:      timeMS / 1000 % len(heroes),
				Amount:           incomingDamage,
				TeamHPRemaining:  teamHP,
				EnemyHPRemaining: enemyHP,
			})
			enemyNextAttackMS = timeMS + enemyAttackIntervalMS
			if teamHP <= 0 {
				break
			}
		}
	}

	result.ElapsedSeconds = min(result.MaxSeconds, max(1, lastSecond))
	result.TeamHPRemaining = max(0, teamHP)
	result.EnemyHPRemaining = max(0, enemyHP)
	result.Heroes = combatHeroStates(heroes)
	return result
}

func (service *Service) combatHeroes() []combatHeroRuntime {
	definitions := service.balanceCatalog.HeroDefinitions()
	baseTotal := 0
	for _, definition := range definitions {
		level := service.heroLevels[definition.ID]
		if level <= 0 {
			continue
		}
		ascension := service.heroAscensions[definition.ID]
		baseTotal += heroAttackFromDefinition(definition, level, ascension)
	}
	baseTotal = max(1, baseTotal)

	heroes := make([]combatHeroRuntime, 0, len(definitions))
	for _, definition := range definitions {
		level := service.heroLevels[definition.ID]
		if level <= 0 {
			continue
		}
		ascension := service.heroAscensions[definition.ID]
		baseAttack := heroAttackFromDefinition(definition, level, ascension)
		scaledAttack := max(1, baseAttack*max(1, service.state.TeamAttack)/baseTotal)
		index := len(heroes)
		heroes = append(heroes, combatHeroRuntime{
			index:              index,
			id:                 definition.ID,
			name:               definition.DisplayName,
			attack:             scaledAttack,
			critChancePercent:  heroCritChancePercent(definition.ID, ascension),
			accuracyPercent:    heroAccuracyPercent(definition.ID, ascension),
			defense:            heroDefense(definition.ID, level, ascension),
			maxMana:            heroMaxMana(definition.ID),
			nextAttackMS:       250 + index*170,
			attackIntervalMS:   heroAttackIntervalMS(definition.ID),
			ultimateCooldownMS: heroUltimateCooldownMS(definition.ID),
		})
	}
	if len(heroes) == 0 {
		heroes = append(heroes, combatHeroRuntime{
			id:                 "hero_unknown",
			name:               "Hero",
			attack:             max(1, service.state.TeamAttack),
			critChancePercent:  10,
			accuracyPercent:    90,
			defense:            10,
			maxMana:            100,
			attackIntervalMS:   1500,
			ultimateCooldownMS: 5000,
		})
	}
	return heroes
}

func heroAttackFromDefinition(definition balance.HeroDefinition, level int, ascension int) int {
	level = max(1, level)
	ascension = max(0, ascension)
	return definition.BaseAttack + ((level - 1) * definition.AttackPerLevel) + (ascension * definition.AttackPerAscension)
}

func heroCritChancePercent(heroID string, ascension int) int {
	base := 10
	switch heroID {
	case "hero_astra":
		base = 12
	case "hero_borin":
		base = 5
	case "hero_cyra":
		base = 15
	case "hero_dante":
		base = 18
	case "hero_elowen":
		base = 8
	}
	return clampInt(base+ascension/2, 0, 75)
}

func heroAccuracyPercent(heroID string, ascension int) int {
	base := 90
	switch heroID {
	case "hero_astra":
		base = 92
	case "hero_borin":
		base = 88
	case "hero_cyra":
		base = 90
	case "hero_dante":
		base = 95
	case "hero_elowen":
		base = 90
	}
	return clampInt(base+ascension/3, 50, 100)
}

func heroDefense(heroID string, level int, ascension int) int {
	base := 10
	switch heroID {
	case "hero_astra":
		base = 8
	case "hero_borin":
		base = 24
	case "hero_cyra":
		base = 6
	case "hero_dante":
		base = 8
	case "hero_elowen":
		base = 14
	}
	return max(0, base+level/2+ascension*3)
}

func combatPercentCheck(timeMS int, actorIndex int, sequence int, chancePercent int) bool {
	chancePercent = clampInt(chancePercent, 0, 100)
	if chancePercent <= 0 {
		return false
	}
	if chancePercent >= 100 {
		return true
	}
	roll := (timeMS/10 + actorIndex*37 + sequence*53) % 100
	return roll < chancePercent
}

func mitigateEnemyDamage(enemyDamage int, teamDefense int) int {
	enemyDamage = max(1, enemyDamage)
	teamDefense = max(0, teamDefense)
	reductionPercent := 0
	if teamDefense > 0 {
		reductionPercent = teamDefense * 100 / max(1, teamDefense+enemyDamage*8)
	}
	reductionPercent = clampInt(reductionPercent, 0, 45)
	return max(1, enemyDamage*(100-reductionPercent)/100)
}

func clampInt(value int, lower int, upper int) int {
	return min(max(value, lower), upper)
}

func combatHeroStates(heroes []combatHeroRuntime) []api.CombatHeroState {
	states := make([]api.CombatHeroState, len(heroes))
	for i, hero := range heroes {
		states[i] = api.CombatHeroState{
			HeroID:             hero.id,
			DisplayName:        hero.name,
			MaxMana:            hero.maxMana,
			ManaRemaining:      hero.mana,
			AutoAttackManaGain: heroAutoAttackManaGain(hero.id),
			PassiveID:          heroPassiveID(hero.id),
			PassiveName:        heroPassiveName(hero.id),
			UltimateID:         heroUltimateID(hero.id),
			UltimateName:       heroUltimateName(hero.id),
			UltimateCooldownMS: hero.ultimateCooldownMS,
		}
	}
	return states
}

func appendCombatEvent(events []api.CombatEvent, event api.CombatEvent) []api.CombatEvent {
	if len(events) >= combatMaxReplayEvents {
		return events
	}
	return append(events, event)
}

func heroMaxMana(heroID string) int {
	switch heroID {
	case "hero_dante":
		return 25
	case "hero_astra":
		return 26
	case "hero_cyra":
		return 27
	case "hero_elowen":
		return 28
	case "hero_borin":
		return 30
	default:
		return 28
	}
}

func heroAutoAttackManaGain(heroID string) int {
	return combatAutoAttackManaGain
}

func heroAttackIntervalMS(heroID string) int {
	switch heroID {
	case "hero_dante":
		return 950
	case "hero_borin":
		return 1600
	case "hero_cyra":
		return 1250
	case "hero_elowen":
		return 1700
	default:
		return 1120
	}
}

func heroUltimateCooldownMS(heroID string) int {
	switch heroID {
	case "hero_dante":
		return 3500
	case "hero_borin":
		return 5200
	case "hero_elowen":
		return 5600
	default:
		return 4500
	}
}

func heroPassiveID(heroID string) string {
	switch heroID {
	case "hero_dante":
		return "passive_momentum"
	case "hero_elowen":
		return "passive_grove_mending"
	default:
		return "passive_none"
	}
}

func heroPassiveName(heroID string) string {
	switch heroID {
	case "hero_dante":
		return "Momentum: +2 mana on successful hits"
	case "hero_elowen":
		return "Grove Mending: 25% attacks heal the team"
	default:
		return "Passive"
	}
}

func heroUltimateID(heroID string) string {
	return heroID + "_ultimate"
}

func heroUltimateName(heroID string) string {
	switch heroID {
	case "hero_astra":
		return "Starfall Slash"
	case "hero_borin":
		return "Stoneguard Oath"
	case "hero_cyra":
		return "Arcane Burst"
	case "hero_dante":
		return "Marked Execute"
	case "hero_elowen":
		return "Wild Bloom"
	default:
		return "Ultimate"
	}
}

func heroUltimateEffect(heroID string, attack int, teamMaxHP int) (int, int) {
	switch heroID {
	case "hero_borin":
		return attack * 3, max(1, teamMaxHP/12)
	case "hero_cyra":
		return attack * 6, 0
	case "hero_dante":
		return attack * 5, 0
	case "hero_elowen":
		return attack * 3, max(1, teamMaxHP/4)
	default:
		return attack * 4, 0
	}
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
