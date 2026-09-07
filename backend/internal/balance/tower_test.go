package balance

import "testing"

func TestTowerDefinitionUsesBossCurvesAndShardMilestones(t *testing.T) {
	definition, ok := TowerDefinitionByID(TowerDungeon)
	if !ok {
		t.Fatalf("expected %s definition", TowerDungeon)
	}
	if definition.MaxFloor != 1000 || definition.SectionSize != 100 {
		t.Fatalf("unexpected tower bounds: %#v", definition)
	}
	if TowerRequiredPower(definition, 1) != 259 {
		t.Fatalf("expected floor 1 required power 259, got %d", TowerRequiredPower(definition, 1))
	}
	if stats := TowerEnemyCombatStats(definition, 1); stats.MaxHP != 37 || stats.Damage != 128 || stats.MaxSeconds != DefaultCombatDurationSeconds {
		t.Fatalf("unexpected normal tower combat stats: %#v", stats)
	}
	if TowerBossType(definition, 25) != TowerBossMini || TowerBossType(definition, 100) != TowerBossBig {
		t.Fatalf("expected mini and big boss floors, got %s and %s", TowerBossType(definition, 25), TowerBossType(definition, 100))
	}
	if reward := TowerReward(definition, 10); len(reward.HeroShards) != 1 || reward.HeroShards[0].Shards != 1 {
		t.Fatalf("expected normal shard milestone reward, got %#v", reward)
	}
	if reward := TowerReward(definition, 100); len(reward.HeroShards) != 1 || reward.HeroShards[0].Shards != 13 {
		t.Fatalf("expected big boss shard reward, got %#v", reward)
	}
}
