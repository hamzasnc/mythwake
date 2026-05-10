package balance

import (
	"fmt"
	"sort"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
)

const (
	DungeonGold    = "gold_dungeon"
	DungeonEssence = "essence_dungeon"
	DungeonGear    = "gear_dungeon"

	EquipmentWeapon = "equipment_weapon"
	EquipmentArmor  = "equipment_armor"

	BannerHeroShardStandard = "hero_shard_standard"

	RewardSummonShards         = "reward_summon_shards"
	RewardGearDrop             = "reward_gear_drop"
	StarterGearDropAccessoryID = "accessory_earrings_r0"
)

type DungeonDefinition struct {
	ID                    string
	DisplayName           string
	RewardCurrencyID      string
	BaseRequiredPower     int
	RequiredPowerPerFloor int
	BaseRewardAmount      int
	RewardPerFloor        int
}

type DailyMissionDefinition struct {
	ID           string
	DisplayName  string
	ProgressType string
	Target       int
	Reward       api.Reward
}

type BattlePassRewardDefinition struct {
	ID             string
	RequiredPassXP int
	Reward         api.Reward
}

type SummonShardDrop struct {
	HeroID string
	Shards int
	Reward api.Reward
}

type ProgressionCostDefinition struct {
	ID             string
	Domain         string
	TargetID       string
	CostCurrencyID string
	BaseAmount     int
	AmountPerLevel int
	Formula        string
}

type SummonBannerDefinition struct {
	ID             string
	DisplayName    string
	CostCurrencyID string
	CostAmount     int
	ResolutionMode string
	ShardDrops     []SummonShardDrop
}

var dungeonDefinitions = map[string]DungeonDefinition{
	DungeonGold: {
		ID:                    DungeonGold,
		DisplayName:           "Gold Dungeon",
		RewardCurrencyID:      economy.CurrencyGold,
		BaseRequiredPower:     100,
		RequiredPowerPerFloor: 50,
		BaseRewardAmount:      95,
		RewardPerFloor:        34,
	},
	DungeonEssence: {
		ID:                    DungeonEssence,
		DisplayName:           "Essence Dungeon",
		RewardCurrencyID:      economy.CurrencyMythEssence,
		BaseRequiredPower:     100,
		RequiredPowerPerFloor: 50,
		BaseRewardAmount:      110,
		RewardPerFloor:        40,
	},
	DungeonGear: {
		ID:                    DungeonGear,
		DisplayName:           "Gear Dungeon",
		RewardCurrencyID:      "",
		BaseRequiredPower:     120,
		RequiredPowerPerFloor: 56,
		BaseRewardAmount:      0,
		RewardPerFloor:        0,
	},
}

var dailyMissionDefinitions = []DailyMissionDefinition{
	{
		ID:           "daily_battles_15",
		DisplayName:  "Battle 15 times",
		ProgressType: "fight",
		Target:       15,
		Reward:       api.Reward{RewardID: "reward_daily_battles_15", Gold: 40, Gems: 5, MythEssence: 70, PassXP: 40},
	},
	{
		ID:           "daily_stage_clears_3",
		DisplayName:  "Clear 3 stages",
		ProgressType: "stage_clear",
		Target:       3,
		Reward:       api.Reward{RewardID: "reward_daily_stage_clears_3", Gold: 70, Gems: 10, MythEssence: 110, PassXP: 40},
	},
	{
		ID:           "daily_summon_1",
		DisplayName:  "Summon 1 hero",
		ProgressType: "summon",
		Target:       1,
		Reward:       api.Reward{RewardID: "reward_daily_summon_1", Gold: 35, Gems: 20, MythEssence: 55, PassXP: 40},
	},
}

var battlePassRewardDefinitions = []BattlePassRewardDefinition{
	{ID: "mission_track_reward_01", RequiredPassXP: 40, Reward: api.Reward{RewardID: "reward_mission_track_01", Gold: 100, Gems: 10}},
	{ID: "mission_track_reward_02", RequiredPassXP: 80, Reward: api.Reward{RewardID: "reward_mission_track_02", Gold: 125, Gems: 15, MythEssence: 120}},
	{ID: "mission_track_reward_03", RequiredPassXP: 120, Reward: api.Reward{RewardID: "reward_mission_track_03", Gold: 175, Gems: 20}},
	{ID: "mission_track_reward_04", RequiredPassXP: 180, Reward: api.Reward{RewardID: "reward_mission_track_04", Gold: 225, Gems: 25, MythEssence: 180}},
	{ID: "mission_track_reward_05", RequiredPassXP: 240, Reward: api.Reward{RewardID: "reward_mission_track_05", Gold: 350, Gems: 40, MythEssence: 300}},
}

var heroShardStandardPool = []SummonShardDrop{
	{HeroID: "hero_astra", Shards: 7, Reward: api.Reward{RewardID: RewardSummonShards}},
	{HeroID: "hero_borin", Shards: 7, Reward: api.Reward{RewardID: RewardSummonShards}},
	{HeroID: "hero_cyra", Shards: 7, Reward: api.Reward{RewardID: RewardSummonShards}},
	{HeroID: "hero_dante", Shards: 7, Reward: api.Reward{RewardID: RewardSummonShards}},
	{HeroID: "hero_elowen", Shards: 7, Reward: api.Reward{RewardID: RewardSummonShards}},
}

var progressionCostDefinitions = []ProgressionCostDefinition{
	{
		ID:             "hero_level_any",
		Domain:         "hero",
		TargetID:       "*",
		CostCurrencyID: economy.CurrencyMythEssence,
		BaseAmount:     14,
		AmountPerLevel: 6,
		Formula:        "base_amount + current_level * amount_per_level",
	},
	{
		ID:             "hero_ascension_any",
		Domain:         "hero",
		TargetID:       "*",
		CostCurrencyID: "hero_shards",
		BaseAmount:     20,
		AmountPerLevel: 15,
		Formula:        "base_amount + current_ascension * amount_per_level",
	},
	{
		ID:             "equipment_weapon_level",
		Domain:         "equipment",
		TargetID:       EquipmentWeapon,
		CostCurrencyID: economy.CurrencyGold,
		BaseAmount:     80,
		AmountPerLevel: 35,
		Formula:        "base_amount + current_level * amount_per_level",
	},
	{
		ID:             "equipment_armor_level",
		Domain:         "equipment",
		TargetID:       EquipmentArmor,
		CostCurrencyID: economy.CurrencyGold,
		BaseAmount:     75,
		AmountPerLevel: 35,
		Formula:        "base_amount + current_level * amount_per_level",
	},
	{
		ID:             "accessory_level_any",
		Domain:         "accessory",
		TargetID:       "*",
		CostCurrencyID: economy.CurrencyGold,
		BaseAmount:     35,
		AmountPerLevel: 0,
		Formula:        "flat base_amount",
	},
}

func DungeonDefinitionByID(dungeonID string) (DungeonDefinition, bool) {
	definition, ok := dungeonDefinitions[dungeonID]
	return definition, ok
}

func DungeonDefinitions() []DungeonDefinition {
	definitions := make([]DungeonDefinition, 0, len(dungeonDefinitions))
	for _, definition := range dungeonDefinitions {
		definitions = append(definitions, definition)
	}
	sort.Slice(definitions, func(left int, right int) bool {
		return definitions[left].ID < definitions[right].ID
	})
	return definitions
}

func DungeonRequiredPower(definition DungeonDefinition, floor int) int {
	return definition.BaseRequiredPower + (floor * definition.RequiredPowerPerFloor)
}

func DungeonReward(definition DungeonDefinition, floor int) api.Reward {
	reward := api.Reward{
		RewardID: fmt.Sprintf("reward_%s_floor_%d", definition.ID, floor),
	}
	amount := definition.BaseRewardAmount + (floor * definition.RewardPerFloor)

	switch definition.RewardCurrencyID {
	case economy.CurrencyGold:
		reward.Gold = amount
	case economy.CurrencyMythEssence:
		reward.MythEssence = amount
	case economy.CurrencyGems:
		reward.Gems = amount
	case economy.CurrencyPassXP:
		reward.PassXP = amount
	}

	return reward
}

func CampaignRequiredPower(stage int) int {
	return 90 + (stage * 46)
}

func CampaignReward(stage int) api.Reward {
	reward := api.Reward{
		RewardID:    fmt.Sprintf("reward_campaign_stage_%03d", stage),
		MythEssence: 7 + (stage * 4),
	}
	if stage%5 == 0 {
		reward.Gems = 12 + stage
		reward.PassXP = 25
	}

	return reward
}

func HeroLevelCost(level int) int {
	return 14 + (level * 6)
}

func HeroAscensionShardCost(ascension int) int {
	return 20 + (ascension * 15)
}

func EquipmentLevelCost(equipmentID string, level int) (int, bool) {
	switch equipmentID {
	case EquipmentWeapon:
		return 80 + (level * 35), true
	case EquipmentArmor:
		return 75 + (level * 35), true
	default:
		return 0, false
	}
}

func AccessoryLevelCost(_ string, _ int) int {
	return 35
}

func SummonCost(bannerID string) (int, bool) {
	if bannerID != BannerHeroShardStandard {
		return 0, false
	}

	return 35, true
}

func SummonShardReward(bannerID string, summonCount int) (SummonShardDrop, bool) {
	if bannerID != BannerHeroShardStandard || len(heroShardStandardPool) == 0 {
		return SummonShardDrop{}, false
	}

	drop := heroShardStandardPool[summonCount%len(heroShardStandardPool)]
	return drop, true
}

func SummonBannerDefinitions() []SummonBannerDefinition {
	pool := make([]SummonShardDrop, len(heroShardStandardPool))
	copy(pool, heroShardStandardPool)
	return []SummonBannerDefinition{
		{
			ID:             BannerHeroShardStandard,
			DisplayName:    "Standard Hero Shards",
			CostCurrencyID: economy.CurrencyGems,
			CostAmount:     35,
			ResolutionMode: "deterministic_rotation",
			ShardDrops:     pool,
		},
	}
}

func ProgressionCostDefinitions() []ProgressionCostDefinition {
	definitions := make([]ProgressionCostDefinition, len(progressionCostDefinitions))
	copy(definitions, progressionCostDefinitions)
	return definitions
}

func DailyMissionDefinitionByID(missionID string) (DailyMissionDefinition, bool) {
	for _, definition := range dailyMissionDefinitions {
		if definition.ID == missionID {
			return definition, true
		}
	}

	return DailyMissionDefinition{}, false
}

func DailyMissionDefinitions() []DailyMissionDefinition {
	definitions := make([]DailyMissionDefinition, len(dailyMissionDefinitions))
	copy(definitions, dailyMissionDefinitions)
	return definitions
}

func DailyMissionReward(missionID string) (api.Reward, bool) {
	definition, ok := DailyMissionDefinitionByID(missionID)
	if !ok {
		return api.Reward{}, false
	}

	return definition.Reward, true
}

func BattlePassRewardDefinitionByID(rewardID string) (BattlePassRewardDefinition, bool) {
	for _, definition := range battlePassRewardDefinitions {
		if definition.ID == rewardID {
			return definition, true
		}
	}

	return BattlePassRewardDefinition{}, false
}

func BattlePassRewardDefinitions() []BattlePassRewardDefinition {
	definitions := make([]BattlePassRewardDefinition, len(battlePassRewardDefinitions))
	copy(definitions, battlePassRewardDefinitions)
	return definitions
}

func BattlePassRequiredXP(rewardID string) (int, bool) {
	definition, ok := BattlePassRewardDefinitionByID(rewardID)
	if !ok {
		return 0, false
	}

	return definition.RequiredPassXP, true
}

func BattlePassReward(rewardID string) (api.Reward, bool) {
	definition, ok := BattlePassRewardDefinitionByID(rewardID)
	if !ok {
		return api.Reward{}, false
	}

	return definition.Reward, true
}

func GearDungeonDropAccessoryID(_ int) string {
	return StarterGearDropAccessoryID
}

func GearDungeonReward() api.Reward {
	return api.Reward{
		RewardID: RewardGearDrop,
	}
}
