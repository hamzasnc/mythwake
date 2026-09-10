package player

import (
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
)

const (
	kaelAttackDurationMS = 840
	kaelSkillDurationMS  = 1320
)

type kaelCombatAction struct {
	id, animationVariant, eventType                string
	startMS, durationMS, contactIndex, totalDamage int
	contacts                                       []int
	skill, rolled, manaGranted                     bool
}

// Contacts share one action's roll and damage budget. Only the authoritative
// simulation resolves them; animation callbacks never apply damage or mana.
func advanceKaelCombat(hero *combatHeroRuntime, timeMS int, result *api.CombatResult, enemyHP *int, teamHP int) {
	if *enemyHP <= 0 || teamHP <= 0 {
		hero.pendingAction = nil
		return
	}
	if action := hero.pendingAction; action != nil {
		for action.contactIndex < len(action.contacts) && timeMS >= action.startMS+action.contacts[action.contactIndex] {
			contactTime := action.startMS + action.contacts[action.contactIndex]
			if !action.rolled {
				action.rolled = true
				action.eventType, action.totalDamage = "auto_attack", hero.attack
				if action.skill {
					action.eventType = "ultimate"
					action.totalDamage, _ = heroUltimateEffect(hero.id, hero.attack, result.TeamMaxHP)
				} else {
					hero.autoAttackCount++
					if !combatPercentCheck(contactTime, hero.index, hero.autoAttackCount, hero.accuracyPercent) {
						action.eventType, action.totalDamage = "miss", 0
					} else if combatPercentCheck(contactTime+37, hero.index, hero.autoAttackCount, hero.critChancePercent) {
						action.eventType, action.totalDamage = "critical_attack", max(1, hero.attack*3/2)
					}
				}
			}
			damage := action.totalDamage
			if len(action.contacts) > 1 {
				firstShare := action.totalDamage / 2
				if action.skill {
					firstShare = action.totalDamage / 4
				}
				if action.contactIndex == 0 {
					damage = firstShare
				} else {
					damage -= firstShare
				}
			}
			damage = min(damage, *enemyHP)
			*enemyHP -= damage
			result.DamageDealt += damage
			result.EnemyHPRemaining = *enemyHP
			if !action.skill && action.eventType != "miss" && !action.manaGranted {
				hero.mana = min(hero.maxMana, hero.mana+heroAutoAttackManaGain(hero.id))
				action.manaGranted = true
			}
			emitKaelContact(hero, action, contactTime, action.eventType, damage, result, *enemyHP, teamHP)
			action.contactIndex++
			if *enemyHP <= 0 {
				break
			}
		}
		if action.contactIndex == len(action.contacts) || *enemyHP <= 0 {
			hero.pendingAction = nil
		}
		return
	}
	if timeMS < hero.readyAtMS {
		return
	}
	skill := hero.mana >= hero.maxMana && timeMS >= hero.nextUltimateMS
	if !skill && timeMS < hero.nextAttackMS {
		return
	}
	hero.actionSequence++
	action := &kaelCombatAction{
		id: fmt.Sprintf("%s:action:%d", hero.id, hero.actionSequence), startMS: timeMS,
		contacts: []int{240, 520}, animationVariant: "attack_cross", durationMS: kaelAttackDurationMS, skill: skill,
	}
	if skill {
		hero.mana = 0
		hero.nextUltimateMS = timeMS + hero.ultimateCooldownMS
		action.contacts, action.animationVariant, action.durationMS = []int{400, 920}, "skill", kaelSkillDurationMS
		hero.readyAtMS = timeMS + action.durationMS
		hero.nextAttackMS = max(hero.nextAttackMS, hero.readyAtMS)
	} else {
		switch hero.basicActionCount % 3 {
		case 1:
			action.contacts, action.animationVariant = []int{420}, "attack_spin"
		case 2:
			action.contacts, action.animationVariant = []int{460}, "attack_jump"
		}
		hero.basicActionCount++
		hero.readyAtMS = timeMS + action.durationMS
		hero.nextAttackMS = timeMS + hero.attackIntervalMS
	}
	hero.pendingAction = action
	emitKaelContact(hero, action, timeMS, "action_start", 0, result, *enemyHP, teamHP)
}

func emitKaelContact(hero *combatHeroRuntime, action *kaelCombatAction, timeMS int, eventType string, damage int, result *api.CombatResult, enemyHP, teamHP int) {
	skillID, contactID, contactIndex := "", "", action.contactIndex
	if action.skill {
		skillID = heroUltimateID(hero.id)
	}
	if eventType == "action_start" {
		contactIndex = -1
	} else {
		contactID = fmt.Sprintf("%s:contact:%d", action.id, contactIndex)
	}
	result.Events = appendCombatEvent(result.Events, api.CombatEvent{
		TimeMS: timeMS, ActionStartMS: action.startMS, ActionID: action.id,
		AnimationVariant: action.animationVariant, ActionDurationMS: action.durationMS,
		ContactID: contactID, ContactIndex: contactIndex, ContactCount: len(action.contacts), ContactTimeMS: action.contacts[action.contactIndex],
		CooldownUntilMS: hero.nextUltimateMS, EventType: eventType,
		ActorID: hero.id, ActorIndex: hero.index, TargetID: "enemy", TargetIndex: 0,
		SkillID: skillID, Amount: damage, ManaAfter: hero.mana,
		TeamHPRemaining: teamHP, EnemyHPRemaining: enemyHP,
	})
}
