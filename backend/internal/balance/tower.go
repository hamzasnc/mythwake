package balance

import (
	"fmt"
	"math"
	"sort"

	"github.com/hamzasnc/mythwake/backend/internal/api"
)

const (
	TowerDungeon = "tower_dungeon"

	TowerBossNone    = "none"
	TowerBossMini    = "mini_boss"
	TowerBossBig     = "big_boss"
	RewardTowerFloor = "reward_tower_dungeon_floor"
)

type TowerDefinition struct {
	ID                                 string
	DisplayName                        string
	MaxFloor                           int
	SectionSize                        int
	MiniBossInterval                   int
	BigBossInterval                    int
	ShardInterval                      int
	BaseRequiredPower                  int
	RequiredPowerScale                 float64
	RequiredPowerGrowth                float64
	BaseRewardGold                     int
	RewardGoldScale                    float64
	RewardGoldGrowth                   float64
	BaseRewardEssence                  int
	RewardEssenceScale                 float64
	RewardEssenceGrowth                float64
	BaseEnemyHP                        int
	EnemyHPScale                       float64
	EnemyHPGrowth                      float64
	BaseEnemyDamage                    int
	EnemyDamageScale                   float64
	EnemyDamageGrowth                  float64
	NormalEnemyHPMultiplier            float64
	MiniBossEnemyHPMultiplier          float64
	BigBossEnemyHPMultiplier           float64
	NormalEnemyDamageMultiplier        float64
	MiniBossEnemyDamageMultiplier      float64
	BigBossEnemyDamageMultiplier       float64
	NormalRecommendedPowerMultiplier   float64
	MiniBossRecommendedPowerMultiplier float64
	BigBossRecommendedPowerMultiplier  float64
	NormalShardBase                    int
	NormalShardEveryFloors             int
	MiniBossShardBase                  int
	MiniBossShardEveryFloors           int
	BigBossShardBase                   int
	BigBossShardEveryFloors            int
	MaxCombatSeconds                   int
}

var towerDefinitions = []TowerDefinition{
	{
		ID:                                 TowerDungeon,
		DisplayName:                        "Tower Dungeon",
		MaxFloor:                           1000,
		SectionSize:                        100,
		MiniBossInterval:                   25,
		BigBossInterval:                    100,
		ShardInterval:                      10,
		BaseRequiredPower:                  185,
		RequiredPowerScale:                 74,
		RequiredPowerGrowth:                1.12,
		BaseRewardGold:                     55,
		RewardGoldScale:                    16,
		RewardGoldGrowth:                   1.08,
		BaseRewardEssence:                  14,
		RewardEssenceScale:                 5,
		RewardEssenceGrowth:                1.04,
		BaseEnemyHP:                        18,
		EnemyHPScale:                       5.8,
		EnemyHPGrowth:                      1.08,
		BaseEnemyDamage:                    105,
		EnemyDamageScale:                   34,
		EnemyDamageGrowth:                  1.12,
		NormalEnemyHPMultiplier:            0.86,
		MiniBossEnemyHPMultiplier:          1.55,
		BigBossEnemyHPMultiplier:           2.35,
		NormalEnemyDamageMultiplier:        0.92,
		MiniBossEnemyDamageMultiplier:      1.28,
		BigBossEnemyDamageMultiplier:       1.62,
		NormalRecommendedPowerMultiplier:   1,
		MiniBossRecommendedPowerMultiplier: 1.38,
		BigBossRecommendedPowerMultiplier:  2.05,
		NormalShardBase:                    1,
		NormalShardEveryFloors:             250,
		MiniBossShardBase:                  4,
		MiniBossShardEveryFloors:           200,
		BigBossShardBase:                   12,
		BigBossShardEveryFloors:            100,
		MaxCombatSeconds:                   DefaultCombatDurationSeconds,
	},
}

func TowerDefinitionByID(towerID string) (TowerDefinition, bool) {
	for _, definition := range towerDefinitions {
		if definition.ID == towerID {
			return definition, true
		}
	}

	return TowerDefinition{}, false
}

func TowerDefinitions() []TowerDefinition {
	definitions := make([]TowerDefinition, len(towerDefinitions))
	copy(definitions, towerDefinitions)
	sort.Slice(definitions, func(left int, right int) bool {
		return definitions[left].ID < definitions[right].ID
	})
	return definitions
}

func TowerBossType(definition TowerDefinition, floor int) string {
	floor = clampTowerFloor(definition, floor)
	if definition.BigBossInterval > 0 && floor%definition.BigBossInterval == 0 {
		return TowerBossBig
	}
	if definition.MiniBossInterval > 0 && floor%definition.MiniBossInterval == 0 {
		return TowerBossMini
	}
	return TowerBossNone
}

func TowerRequiredPower(definition TowerDefinition, floor int) int {
	floor = clampTowerFloor(definition, floor)
	return max(1, definition.BaseRequiredPower+int(math.Floor(definition.RequiredPowerScale*math.Pow(float64(floor), definition.RequiredPowerGrowth))))
}

func TowerEnemyCombatStats(definition TowerDefinition, floor int) EnemyCombatStats {
	floor = clampTowerFloor(definition, floor)
	bossType := TowerBossType(definition, floor)
	hpMultiplier, damageMultiplier := towerCombatMultipliers(definition, bossType)
	baseHP := float64(definition.BaseEnemyHP) + (definition.EnemyHPScale * math.Pow(float64(floor), definition.EnemyHPGrowth))
	baseDamage := float64(definition.BaseEnemyDamage) + (definition.EnemyDamageScale * math.Pow(float64(floor), definition.EnemyDamageGrowth))
	return EnemyCombatStats{
		MaxHP:      max(1, int(math.Ceil(baseHP*1.8*hpMultiplier))),
		Damage:     max(1, int(math.Ceil(baseDamage*damageMultiplier))),
		MaxSeconds: max(1, definition.MaxCombatSeconds),
	}
}

func TowerReward(definition TowerDefinition, floor int) api.Reward {
	floor = clampTowerFloor(definition, floor)
	bossType := TowerBossType(definition, floor)
	gold := definition.BaseRewardGold + int(math.Floor(definition.RewardGoldScale*math.Pow(float64(floor), definition.RewardGoldGrowth)))
	essence := definition.BaseRewardEssence + int(math.Floor(definition.RewardEssenceScale*math.Pow(float64(floor), definition.RewardEssenceGrowth)))
	shards := 0
	switch bossType {
	case TowerBossBig:
		gold = int(math.Ceil(float64(gold) * 4.5))
		essence = int(math.Ceil(float64(essence) * 4.25))
		shards = definition.BigBossShardBase + floor/max(1, definition.BigBossShardEveryFloors)
	case TowerBossMini:
		gold = int(math.Ceil(float64(gold) * 2.15))
		essence = int(math.Ceil(float64(essence) * 2))
		shards = definition.MiniBossShardBase + floor/max(1, definition.MiniBossShardEveryFloors)
	default:
		if definition.ShardInterval > 0 && floor%definition.ShardInterval == 0 {
			shards = definition.NormalShardBase + floor/max(1, definition.NormalShardEveryFloors)
		}
	}

	result := api.Reward{
		RewardID:    fmt.Sprintf("%s_%d", RewardTowerFloor, floor),
		Gold:        max(0, gold),
		MythEssence: max(0, essence),
	}
	if shards > 0 {
		heroes := HeroDefinitions()
		if len(heroes) > 0 {
			heroIndex := (floor / max(1, definition.ShardInterval)) % len(heroes)
			result.HeroShards = []api.HeroShardReward{{HeroID: heroes[heroIndex].ID, Shards: shards}}
		}
	}
	return result
}

func towerCombatMultipliers(definition TowerDefinition, bossType string) (float64, float64) {
	switch bossType {
	case TowerBossBig:
		return positiveMultiplier(definition.BigBossEnemyHPMultiplier), positiveMultiplier(definition.BigBossEnemyDamageMultiplier)
	case TowerBossMini:
		return positiveMultiplier(definition.MiniBossEnemyHPMultiplier), positiveMultiplier(definition.MiniBossEnemyDamageMultiplier)
	default:
		return positiveMultiplier(definition.NormalEnemyHPMultiplier), positiveMultiplier(definition.NormalEnemyDamageMultiplier)
	}
}

func positiveMultiplier(value float64) float64 {
	if value <= 0 {
		return 1
	}
	return value
}

func clampTowerFloor(definition TowerDefinition, floor int) int {
	maxFloor := max(1, definition.MaxFloor)
	return min(max(1, floor), maxFloor)
}
