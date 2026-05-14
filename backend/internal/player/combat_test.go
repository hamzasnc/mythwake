package player

import "testing"

func TestCombatReplayIncludesHeroManaAndUltimateEvents(t *testing.T) {
	service := NewService()

	combat := service.simulateCombat(combatEnemy{
		mode:        "campaign",
		targetID:    "campaign_stage_test",
		targetLevel: 1,
		maxHP:       5000,
		damage:      1,
		maxSeconds:  30,
	})

	if len(combat.Heroes) != 6 {
		t.Fatalf("expected 6 combat heroes, got %d", len(combat.Heroes))
	}

	danteFound := false
	ravikFound := false
	for _, hero := range combat.Heroes {
		if hero.HeroID == "hero_dante" {
			danteFound = true
			if hero.MaxMana != 25 || hero.AutoAttackManaGain != 2 || hero.PassiveID != "passive_momentum" {
				t.Fatalf("unexpected Dante mana/passive data: %#v", hero)
			}
		}
		if hero.HeroID == "hero_ravik" {
			ravikFound = true
			if hero.MaxMana != 27 || hero.AutoAttackManaGain != 2 || hero.PassiveID != "passive_cinder_hunger" || hero.UltimateName != "Dragonflame Nova" {
				t.Fatalf("unexpected Ravik mana/passive data: %#v", hero)
			}
		}
	}
	if !danteFound {
		t.Fatal("expected Dante in combat hero states")
	}
	if !ravikFound {
		t.Fatal("expected Ravik in combat hero states")
	}
	hasAutoAttack := false
	hasUltimate := false
	for _, event := range combat.Events {
		if event.EventType == "auto_attack" && event.ManaAfter > 0 {
			hasAutoAttack = true
		}
		if event.EventType == "ultimate" {
			hasUltimate = true
		}
	}
	if !hasAutoAttack || !hasUltimate {
		t.Fatalf("expected auto attack and ultimate events, auto=%v ultimate=%v events=%#v", hasAutoAttack, hasUltimate, combat.Events)
	}
}
