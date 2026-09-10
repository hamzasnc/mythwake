package balance

import "testing"

func TestKaelDefinitionAndShardPool(t *testing.T) {
	hero, ok := HeroDefinitionByID("hero_kael")
	if !ok || hero.DisplayName != "Kael" || hero.SortOrder != 80 || !hero.StarterOwned || hero.MaxLevel != 100 || hero.MaxAscension != 10 || hero.BaseAttack != 18 || hero.AttackPerLevel != 5 || hero.AttackPerAscension != 11 || hero.BaseHealth != 150 || hero.HealthPerLevel != 28 || hero.HealthPerAscension != 70 {
		t.Fatalf("unexpected Kael definition: %#v", hero)
	}
	seen := map[string]bool{}
	for _, definition := range HeroDefinitions() {
		if seen[definition.ID] {
			t.Fatalf("hero ID collision: %s", definition.ID)
		}
		seen[definition.ID] = true
	}
	drop, ok := SummonShardReward(BannerHeroShardStandard, 7)
	if !ok || drop.HeroID != "hero_kael" || drop.Shards != 1 {
		t.Fatalf("Kael has no reachable standard shard reward: %#v", drop)
	}
}
