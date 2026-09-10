package player

import (
	"context"
	"encoding/json"
	"reflect"
	"testing"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
)

func TestKaelStarterGrantPreservesExistingProgress(t *testing.T) {
	old := NewService()
	delete(old.heroLevels, "hero_kael")
	delete(old.heroShards, "hero_kael")
	delete(old.heroAscensions, "hero_kael")
	delete(old.heroStars, "hero_kael")
	old.heroLevels["hero_astra"] = 42
	old.heroShards["hero_astra"] = 27
	old.heroAscensions["hero_astra"] = 3
	old.heroStars["hero_astra"] = 2
	old.state.Gold = 4321
	old.state.CampaignStage = 32
	saved := old.persistentState()
	restored := NewService()
	restored.applyPersistentState(saved)
	if restored.heroLevels["hero_kael"] != 1 || restored.heroShards["hero_kael"] != 0 || restored.heroAscensions["hero_kael"] != 0 || restored.heroStars["hero_kael"] != 0 {
		t.Fatal("expected one free level 1 Kael with no extra progression")
	}
	for id, level := range saved.HeroLevels {
		if restored.heroLevels[id] != level || restored.heroShards[id] != saved.HeroShards[id] || restored.heroAscensions[id] != saved.HeroAscensions[id] || restored.heroStars[id] != saved.HeroStars[id] {
			t.Fatalf("existing progression changed for %s", id)
		}
	}
	if restored.state.Gold != 4321 || restored.state.CampaignStage != 32 {
		t.Fatal("starter grant changed currency or campaign progression")
	}
	again := NewService()
	again.applyPersistentState(restored.persistentState())
	if !reflect.DeepEqual(again.persistentState(), restored.persistentState()) {
		t.Fatal("second reload changed the starter grant or existing state")
	}
}

func TestCombatFormationFiltersStatsAndPreservesAccount(t *testing.T) {
	service := NewService()
	service.equipmentLevels[balance.EquipmentWeapon] = 1
	service.equipmentLevels[balance.EquipmentArmor] = 1
	service.recalculatePower()
	before := service.persistentState()
	combat := service.simulateCombat(combatEnemy{maxHP: 50000, damage: 1, maxSeconds: 30}, "hero_kael", "hero_astra")
	if len(combat.Heroes) != 2 || combat.Heroes[0].HeroID != "hero_kael" || combat.Heroes[1].HeroID != "hero_astra" {
		t.Fatalf("wrong ordered combatants: %#v", combat.Heroes)
	}
	if combat.TeamAttack != 43 || combat.TeamMaxHP != 365 {
		t.Fatalf("unselected heroes or duplicated shared equipment affected combat: attack=%d hp=%d", combat.TeamAttack, combat.TeamMaxHP)
	}
	if !reflect.DeepEqual(before, service.persistentState()) {
		t.Fatal("simulation modified account-wide state")
	}
	for _, event := range combat.Events {
		if event.ActorID != "enemy" && combat.Heroes[event.ActorIndex].HeroID != event.ActorID {
			t.Fatalf("actor identity did not follow ordered formation: %#v", event)
		}
		if event.EventType == "enemy_attack" && combat.Heroes[event.TargetIndex].HeroID != event.TargetID {
			t.Fatalf("enemy reaction target did not follow formation: %#v", event)
		}
	}
}

func TestCombatFormationValidationAndLegacyDefault(t *testing.T) {
	service := NewService()
	legacy, err := service.resolveCombatFormation(nil)
	if err != nil || len(legacy) != 7 {
		t.Fatalf("legacy formation unavailable: %v %#v", err, legacy)
	}
	for _, id := range legacy {
		if id == "hero_kael" {
			t.Fatal("omitted body silently added Kael to a legacy formation")
		}
	}
	for _, requested := range [][]string{{}, {"unknown"}, {"hero_kael", "hero_kael"}, append(append([]string{}, legacy...), "hero_kael")} {
		if _, err := service.resolveCombatFormation(requested); err == nil {
			t.Fatalf("accepted invalid formation: %#v", requested)
		}
	}
	delete(service.heroLevels, "hero_borin")
	if _, err := service.resolveCombatFormation([]string{"hero_borin"}); err == nil {
		t.Fatal("accepted unowned hero")
	}
	requested := []string{"hero_kael", "hero_astra"}
	frozen, err := service.resolveCombatFormation(requested)
	if err != nil {
		t.Fatal(err)
	}
	requested[0] = "unknown"
	if frozen[0] != "hero_kael" {
		t.Fatal("formation retained caller-owned slice")
	}
	before := service.persistentState()
	result := service.FightCampaignWithRequest(context.Background(), ActionRequest{HeroIDs: []string{}})
	if result.Success || result.ErrorCode != "invalid_formation" || !reflect.DeepEqual(before, service.persistentState()) {
		t.Fatal("invalid formation changed progress")
	}
}

func TestKaelReplayHasSerializedStartsAndUniqueContacts(t *testing.T) {
	service := NewService()
	combat := service.simulateCombat(combatEnemy{maxHP: 50000, damage: 1, maxSeconds: 60}, "hero_kael")
	starts := map[string]api.CombatEvent{}
	contacts := map[string]bool{}
	budgets := map[string]int{}
	mana, nextReady, lastTime, totalDamage, totalTaken, skills, basic := 0, 0, 0, 0, 0, 0, 0
	for _, event := range combat.Events {
		if event.ActionID == "" || event.TimeMS < lastTime {
			t.Fatalf("unordered or unidentified event: %#v", event)
		}
		lastTime = event.TimeMS
		if event.ActorID == "enemy" {
			totalTaken += event.Amount
			continue
		}
		if event.EventType == "action_start" {
			if _, exists := starts[event.ActionID]; exists || event.TimeMS < nextReady || event.Amount != 0 || event.TimeMS != event.ActionStartMS || event.ContactIndex != -1 {
				t.Fatalf("overlapping/duplicate action: %#v", event)
			}
			nextReady = event.TimeMS + kaelAttackDurationMS
			if event.SkillID != "" {
				if mana != 26 || event.SkillID != "hero_kael_ultimate" || event.CooldownUntilMS != event.TimeMS+4500 || event.AnimationVariant != "skill" || event.ContactCount != 2 {
					t.Fatalf("invalid skill acceptance: mana=%d %#v", mana, event)
				}
				mana = 0
				nextReady = event.TimeMS + kaelSkillDurationMS
			} else {
				expected := []string{"attack_cross", "attack_spin", "attack_jump"}[basic%3]
				if event.AnimationVariant != expected {
					t.Fatalf("basic cycle expected %s got %#v", expected, event)
				}
				basic++
			}
			if event.ActionDurationMS != nextReady-event.TimeMS {
				t.Fatal("serialized recovery differs from simulation")
			}
			starts[event.ActionID] = event
		} else {
			start, ok := starts[event.ActionID]
			if !ok || event.ContactID == "" || contacts[event.ContactID] || start.ActionStartMS != event.ActionStartMS || event.AnimationVariant != start.AnimationVariant || event.ContactCount != start.ContactCount {
				t.Fatalf("contact has no unique start: %#v", event)
			}
			contacts[event.ContactID] = true
			offsets := map[string][]int{"attack_cross": {240, 520}, "attack_spin": {420}, "attack_jump": {460}, "skill": {400, 920}}[event.AnimationVariant]
			if event.ContactIndex < 0 || event.ContactIndex >= len(offsets) || event.TimeMS != event.ActionStartMS+offsets[event.ContactIndex] || event.ContactTimeMS != offsets[event.ContactIndex] {
				t.Fatalf("contact does not match animation timing: %#v", event)
			}
			budgets[event.ActionID] += event.Amount
			if event.EventType != "ultimate" && event.EventType != "miss" && event.ContactIndex == 0 {
				mana = min(26, mana+2)
			}
			if event.ContactIndex == event.ContactCount-1 {
				budget := 18
				switch event.EventType {
				case "ultimate":
					budget = 72
					skills++
				case "critical_attack":
					budget = 27
				case "miss":
					budget = 0
				}
				if budgets[event.ActionID] != budget {
					t.Fatalf("wrong total action budget: expected %d got %d %#v", budget, budgets[event.ActionID], event)
				}
			}
			totalDamage += event.Amount
		}
		if event.ManaAfter != mana {
			t.Fatalf("double/missing mana: expected=%d %#v", mana, event)
		}
	}
	if skills < 2 || totalDamage != combat.DamageDealt || totalTaken != combat.DamageTaken || combat.EnemyMaxHP-totalDamage != combat.EnemyHPRemaining || combat.TeamMaxHP-totalTaken != combat.TeamHPRemaining {
		t.Fatalf("replay did not reconcile: skills=%d damage=%d taken=%d", skills, totalDamage, totalTaken)
	}
	if combat.Heroes[0].PassiveID != "passive_none" || combat.Heroes[0].PassiveName != "" || combat.Heroes[0].MaxMana != 26 || combat.Heroes[0].ManaRemaining != mana {
		t.Fatalf("invalid final Kael state: %#v", combat.Heroes[0])
	}
}

func TestKaelCooldownMissAndDeathCancel(t *testing.T) {
	hero := NewService().combatHeroes("hero_kael")[0]
	hero.mana = 26
	result := api.CombatResult{TeamMaxHP: 150}
	enemyHP := 10000
	advanceKaelCombat(&hero, 0, &result, &enemyHP, 150)
	advanceKaelCombat(&hero, 400, &result, &enemyHP, 150)
	if hero.mana != 0 || enemyHP != 9982 || hero.nextUltimateMS != 4500 || hero.pendingAction == nil {
		t.Fatal("first skill contact/acceptance failed")
	}
	advanceKaelCombat(&hero, 920, &result, &enemyHP, 150)
	if enemyHP != 9928 || hero.pendingAction != nil {
		t.Fatal("final skill contact failed")
	}
	hero.mana = 26
	advanceKaelCombat(&hero, 1320, &result, &enemyHP, 150)
	if hero.pendingAction == nil || hero.pendingAction.skill {
		t.Fatal("skill ignored cooldown")
	}
	hero.accuracyPercent = 0
	advanceKaelCombat(&hero, 1560, &result, &enemyHP, 150)
	advanceKaelCombat(&hero, 1840, &result, &enemyHP, 150)
	if event := result.Events[len(result.Events)-1]; event.EventType != "miss" || event.Amount != 0 || hero.mana != 26 || hero.autoAttackCount != 1 {
		t.Fatal("miss granted mana/damage or rolled a second basic")
	}
	advanceKaelCombat(&hero, 4500, &result, &enemyHP, 150)
	if hero.pendingAction == nil || !hero.pendingAction.skill {
		t.Fatal("skill did not become ready after cooldown")
	}
	count := len(result.Events)
	advanceKaelCombat(&hero, 4600, &result, &enemyHP, 0)
	advanceKaelCombat(&hero, 5420, &result, &enemyHP, 0)
	if hero.pendingAction != nil || len(result.Events) != count || enemyHP != 9928 {
		t.Fatal("dead team left a pending skill or late contact")
	}
}

func TestKaelContactBudgetsRemaindersAndPartialCancellation(t *testing.T) {
	for _, critical := range []bool{false, true} {
		hero := NewService().combatHeroes("hero_kael")[0]
		hero.attack, hero.accuracyPercent, hero.critChancePercent, hero.nextAttackMS = 21, 100, 0, 0
		if critical {
			hero.critChancePercent = 100
		}
		result, enemyHP := api.CombatResult{TeamMaxHP: 150}, 10000
		advanceKaelCombat(&hero, 0, &result, &enemyHP, 150)
		advanceKaelCombat(&hero, 240, &result, &enemyHP, 150)
		advanceKaelCombat(&hero, 240, &result, &enemyHP, 150)
		if len(result.Events) != 2 || hero.mana != 2 {
			t.Fatal("duplicate first resolution caused an event or mana")
		}
		advanceKaelCombat(&hero, 520, &result, &enemyHP, 150)
		expected := 21
		if critical {
			expected = 31
		}
		if result.DamageDealt != expected || result.Events[1].Amount != expected/2 || result.Events[2].Amount != expected-expected/2 || hero.mana != 2 || hero.autoAttackCount != 1 {
			t.Fatalf("remainder/one-roll contract broken: %#v %#v", result, hero)
		}
	}
	for _, targetDies := range []bool{false, true} {
		hero := NewService().combatHeroes("hero_kael")[0]
		hero.mana = 26
		result, enemyHP := api.CombatResult{TeamMaxHP: 150}, 1000
		if targetDies {
			enemyHP = 10
		}
		advanceKaelCombat(&hero, 0, &result, &enemyHP, 150)
		advanceKaelCombat(&hero, 400, &result, &enemyHP, 150)
		count, applied := len(result.Events), result.DamageDealt
		if targetDies {
			advanceKaelCombat(&hero, 920, &result, &enemyHP, 150)
		} else {
			advanceKaelCombat(&hero, 900, &result, &enemyHP, 0)
			advanceKaelCombat(&hero, 920, &result, &enemyHP, 0)
		}
		if hero.pendingAction != nil || len(result.Events) != count || result.DamageDealt != applied {
			t.Fatal("unfinished skill survived caster/target death")
		}
	}
}

func TestLongCombatReplayIsCompleteAndStopsOnDefeat(t *testing.T) {
	service := NewService()
	full := service.simulateCombat(combatEnemy{maxHP: 100000, damage: 1, maxSeconds: 60})
	if len(full.Events) <= 180 {
		t.Fatalf("fixture did not pass the old truncation cap: %d", len(full.Events))
	}
	teamBefore, enemyBefore := full.TeamMaxHP, full.EnemyMaxHP
	for _, event := range full.Events {
		applied := enemyBefore - event.EnemyHPRemaining + event.TeamHPRemaining - teamBefore
		if event.EventType == "enemy_attack" {
			applied = teamBefore - event.TeamHPRemaining
		}
		if event.EventType != "passive_mana" && event.Amount != applied {
			t.Fatalf("replay amount includes damage/healing that was not applied: expected=%d event=%#v", applied, event)
		}
		teamBefore, enemyBefore = event.TeamHPRemaining, event.EnemyHPRemaining
	}
	last := full.Events[len(full.Events)-1]
	if last.TeamHPRemaining != full.TeamHPRemaining || last.EnemyHPRemaining != full.EnemyHPRemaining {
		t.Fatal("replay truncation left final HP unavailable")
	}
	dead := service.simulateCombat(combatEnemy{maxHP: 10000, damage: 10000, maxSeconds: 30}, "hero_kael")
	last = dead.Events[len(dead.Events)-1]
	if dead.TeamHPRemaining != 0 || last.EventType != "enemy_attack" || last.TeamHPRemaining != 0 {
		t.Fatal("events continued after shared team defeat")
	}
}

// The verbose JSON line is consumed by the standalone C# replay compatibility
// check, so that test uses a real server result rather than a hand-written DTO.
func TestKaelReplayWireContract(t *testing.T) {
	service := NewService()
	combat := service.simulateCombat(combatEnemy{maxHP: 100000, damage: 1, maxSeconds: 60}, "hero_astra", "hero_kael")
	encoded, err := json.Marshal(combat)
	if err != nil {
		t.Fatal(err)
	}
	var decoded api.CombatResult
	if err := json.Unmarshal(encoded, &decoded); err != nil {
		t.Fatal(err)
	}
	if !reflect.DeepEqual(combat, decoded) {
		t.Fatal("wire round-trip lost authoritative contact metadata or HP")
	}
	if combat.Heroes[1].UltimateName != "Crescent Tempest" {
		t.Fatal("Kael's new display name did not reach the service contract")
	}
	t.Logf("KAEL_REPLAY_FIXTURE %s", encoded)
}
