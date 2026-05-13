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
	RewardAFKClaim             = "reward_afk_claim"
	StarterGearDropAccessoryID = "accessory_earrings_r0"

	AFKMinClaimSeconds   = 60
	AFKMaxClaimSeconds   = 6 * 60 * 60
	AFKRewardTickSeconds = 60

	DefaultCombatDurationSeconds = 30
	DungeonBossHpNumerator       = 18
	DungeonBossHpDenominator     = 10
)

type DungeonDefinition struct {
	ID                    string
	DisplayName           string
	RewardCurrencyID      string
	BaseRequiredPower     int
	RequiredPowerPerFloor int
	BaseRewardAmount      int
	RewardPerFloor        int
	EnemyBaseHP           int
	EnemyHPPerPower       int
	EnemyHPPerFloor       int
	EnemyBaseDamage       int
	EnemyDamagePerFloor   int
	EnemyDamagePowerDiv   int
	MaxCombatSeconds      int
}

type HeroDefinition struct {
	ID                 string
	DisplayName        string
	SortOrder          int
	StarterOwned       bool
	MaxLevel           int
	MaxAscension       int
	BaseAttack         int
	AttackPerLevel     int
	AttackPerAscension int
	BaseHealth         int
	HealthPerLevel     int
	HealthPerAscension int
}

type EquipmentDefinition struct {
	ID             string
	DisplayName    string
	SortOrder      int
	StarterOwned   bool
	MaxLevel       int
	AttackPerLevel int
	HealthPerLevel int
}

type RewardDefinition struct {
	ID          string
	DisplayName string
	RewardType  string
	Reward      api.Reward
}

type AFKRewardDefinition struct {
	ID                        string
	RewardID                  string
	DisplayName               string
	MinClaimSeconds           int
	MaxClaimSeconds           int
	TickSeconds               int
	BaseMythEssencePerTick    int
	MythEssencePerStage       int
	GoldPerMythEssenceDivisor int
}

type CampaignDefinition struct {
	ID                        string
	DisplayName               string
	BaseRequiredPower         int
	RequiredPowerPerStage     int
	BaseMythEssenceReward     int
	MythEssenceRewardPerStage int
	MilestoneEveryStages      int
	MilestoneBaseGems         int
	MilestoneGemsPerStage     int
	MilestonePassXP           int
	EnemyBaseHP               int
	EnemyHPPerPower           int
	EnemyHPPerStageSquared    int
	EnemyBaseDamage           int
	EnemyDamagePerStage       int
	EnemyDamagePowerDiv       int
	MaxCombatSeconds          int
}

type CampaignStageDefinition struct {
	ID               string
	CampaignID       string
	StageNumber      int
	DisplayName      string
	RequiredPower    int
	RewardID         string
	EnemyProfileID   string
	EnemyMaxHP       int
	EnemyDamage      int
	MaxCombatSeconds int
}

type AccessorySlotDefinition struct {
	ID          string
	DisplayName string
	SortOrder   int
}

type AccessoryRarityDefinition struct {
	ID           string
	RarityIndex  int
	DisplayName  string
	MaxLevel     int
	FuseCopyCost int
}

type AccessoryDefinition struct {
	ID             string
	SlotID         string
	RarityID       string
	AttackPerLevel int
	HealthPerLevel int
	DropWeight     int
	FuseTargetID   string
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

type EnemyCombatStats struct {
	MaxHP      int
	Damage     int
	MaxSeconds int
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

var heroDefinitions = []HeroDefinition{
	{ID: "hero_astra", DisplayName: "Astra", SortOrder: 10, StarterOwned: true, MaxLevel: 100, MaxAscension: 10, BaseAttack: 18, AttackPerLevel: 5, AttackPerAscension: 11, BaseHealth: 150, HealthPerLevel: 28, HealthPerAscension: 70},
	{ID: "hero_borin", DisplayName: "Borin", SortOrder: 20, StarterOwned: true, MaxLevel: 100, MaxAscension: 10, BaseAttack: 10, AttackPerLevel: 3, AttackPerAscension: 8, BaseHealth: 230, HealthPerLevel: 42, HealthPerAscension: 55},
	{ID: "hero_cyra", DisplayName: "Cyra", SortOrder: 30, StarterOwned: true, MaxLevel: 100, MaxAscension: 10, BaseAttack: 22, AttackPerLevel: 7, AttackPerAscension: 11, BaseHealth: 110, HealthPerLevel: 20, HealthPerAscension: 70},
	{ID: "hero_dante", DisplayName: "Dante", SortOrder: 40, StarterOwned: true, MaxLevel: 100, MaxAscension: 10, BaseAttack: 20, AttackPerLevel: 6, AttackPerAscension: 8, BaseHealth: 125, HealthPerLevel: 23, HealthPerAscension: 55},
	{ID: "hero_elowen", DisplayName: "Elowen", SortOrder: 50, StarterOwned: true, MaxLevel: 100, MaxAscension: 10, BaseAttack: 12, AttackPerLevel: 4, AttackPerAscension: 14, BaseHealth: 165, HealthPerLevel: 34, HealthPerAscension: 90},
}

var equipmentDefinitions = []EquipmentDefinition{
	{ID: EquipmentWeapon, DisplayName: "Weapon", SortOrder: 10, StarterOwned: true, MaxLevel: 100, AttackPerLevel: 7, HealthPerLevel: 0},
	{ID: EquipmentArmor, DisplayName: "Armor", SortOrder: 20, StarterOwned: true, MaxLevel: 100, AttackPerLevel: 0, HealthPerLevel: 65},
}

var afkRewardDefinitions = []AFKRewardDefinition{
	{
		ID:                        "afk_default",
		RewardID:                  RewardAFKClaim,
		DisplayName:               "AFK Gold and Myth Essence",
		MinClaimSeconds:           AFKMinClaimSeconds,
		MaxClaimSeconds:           AFKMaxClaimSeconds,
		TickSeconds:               AFKRewardTickSeconds,
		BaseMythEssencePerTick:    3,
		MythEssencePerStage:       1,
		GoldPerMythEssenceDivisor: 2,
	},
}

var campaignDefinitions = []CampaignDefinition{
	{
		ID:                        "main_campaign",
		DisplayName:               "Main Campaign",
		BaseRequiredPower:         90,
		RequiredPowerPerStage:     46,
		BaseMythEssenceReward:     7,
		MythEssenceRewardPerStage: 4,
		MilestoneEveryStages:      5,
		MilestoneBaseGems:         12,
		MilestoneGemsPerStage:     1,
		MilestonePassXP:           25,
		EnemyBaseHP:               180,
		EnemyHPPerPower:           2,
		EnemyHPPerStageSquared:    6,
		EnemyBaseDamage:           18,
		EnemyDamagePerStage:       4,
		EnemyDamagePowerDiv:       50,
		MaxCombatSeconds:          DefaultCombatDurationSeconds,
	},
}

var accessorySlotDefinitions = []AccessorySlotDefinition{
	{ID: "earrings", DisplayName: "Ohrringe", SortOrder: 10},
	{ID: "necklace", DisplayName: "Kette", SortOrder: 20},
	{ID: "bracelet", DisplayName: "Armband", SortOrder: 30},
	{ID: "gloves", DisplayName: "Handschuhe", SortOrder: 40},
	{ID: "shoes", DisplayName: "Schuhe", SortOrder: 50},
}

var accessoryRarityDefinitions = []AccessoryRarityDefinition{
	{ID: "r0", RarityIndex: 0, DisplayName: "R0", MaxLevel: 20, FuseCopyCost: 3},
	{ID: "r1", RarityIndex: 1, DisplayName: "R1", MaxLevel: 30, FuseCopyCost: 3},
	{ID: "r2", RarityIndex: 2, DisplayName: "R2", MaxLevel: 40, FuseCopyCost: 3},
	{ID: "r3", RarityIndex: 3, DisplayName: "R3", MaxLevel: 50, FuseCopyCost: 3},
	{ID: "r4", RarityIndex: 4, DisplayName: "R4", MaxLevel: 60, FuseCopyCost: 3},
}

var accessoryDefinitions = []AccessoryDefinition{
	{ID: "accessory_earrings_r0", SlotID: "earrings", RarityID: "r0", AttackPerLevel: 2, HealthPerLevel: 8, DropWeight: 120, FuseTargetID: "accessory_earrings_r1"},
	{ID: "accessory_earrings_r1", SlotID: "earrings", RarityID: "r1", AttackPerLevel: 4, HealthPerLevel: 16, DropWeight: 55, FuseTargetID: "accessory_earrings_r2"},
	{ID: "accessory_earrings_r2", SlotID: "earrings", RarityID: "r2", AttackPerLevel: 7, HealthPerLevel: 28, DropWeight: 22, FuseTargetID: "accessory_earrings_r3"},
	{ID: "accessory_earrings_r3", SlotID: "earrings", RarityID: "r3", AttackPerLevel: 11, HealthPerLevel: 44, DropWeight: 8, FuseTargetID: "accessory_earrings_r4"},
	{ID: "accessory_earrings_r4", SlotID: "earrings", RarityID: "r4", AttackPerLevel: 16, HealthPerLevel: 64, DropWeight: 2},
	{ID: "accessory_necklace_r0", SlotID: "necklace", RarityID: "r0", AttackPerLevel: 1, HealthPerLevel: 12, DropWeight: 120, FuseTargetID: "accessory_necklace_r1"},
	{ID: "accessory_necklace_r1", SlotID: "necklace", RarityID: "r1", AttackPerLevel: 3, HealthPerLevel: 24, DropWeight: 55, FuseTargetID: "accessory_necklace_r2"},
	{ID: "accessory_necklace_r2", SlotID: "necklace", RarityID: "r2", AttackPerLevel: 5, HealthPerLevel: 40, DropWeight: 22, FuseTargetID: "accessory_necklace_r3"},
	{ID: "accessory_necklace_r3", SlotID: "necklace", RarityID: "r3", AttackPerLevel: 8, HealthPerLevel: 64, DropWeight: 8, FuseTargetID: "accessory_necklace_r4"},
	{ID: "accessory_necklace_r4", SlotID: "necklace", RarityID: "r4", AttackPerLevel: 12, HealthPerLevel: 92, DropWeight: 2},
	{ID: "accessory_bracelet_r0", SlotID: "bracelet", RarityID: "r0", AttackPerLevel: 2, HealthPerLevel: 10, DropWeight: 120, FuseTargetID: "accessory_bracelet_r1"},
	{ID: "accessory_bracelet_r1", SlotID: "bracelet", RarityID: "r1", AttackPerLevel: 4, HealthPerLevel: 20, DropWeight: 55, FuseTargetID: "accessory_bracelet_r2"},
	{ID: "accessory_bracelet_r2", SlotID: "bracelet", RarityID: "r2", AttackPerLevel: 6, HealthPerLevel: 34, DropWeight: 22, FuseTargetID: "accessory_bracelet_r3"},
	{ID: "accessory_bracelet_r3", SlotID: "bracelet", RarityID: "r3", AttackPerLevel: 10, HealthPerLevel: 54, DropWeight: 8, FuseTargetID: "accessory_bracelet_r4"},
	{ID: "accessory_bracelet_r4", SlotID: "bracelet", RarityID: "r4", AttackPerLevel: 14, HealthPerLevel: 78, DropWeight: 2},
	{ID: "accessory_gloves_r0", SlotID: "gloves", RarityID: "r0", AttackPerLevel: 3, HealthPerLevel: 6, DropWeight: 120, FuseTargetID: "accessory_gloves_r1"},
	{ID: "accessory_gloves_r1", SlotID: "gloves", RarityID: "r1", AttackPerLevel: 5, HealthPerLevel: 12, DropWeight: 55, FuseTargetID: "accessory_gloves_r2"},
	{ID: "accessory_gloves_r2", SlotID: "gloves", RarityID: "r2", AttackPerLevel: 9, HealthPerLevel: 22, DropWeight: 22, FuseTargetID: "accessory_gloves_r3"},
	{ID: "accessory_gloves_r3", SlotID: "gloves", RarityID: "r3", AttackPerLevel: 13, HealthPerLevel: 36, DropWeight: 8, FuseTargetID: "accessory_gloves_r4"},
	{ID: "accessory_gloves_r4", SlotID: "gloves", RarityID: "r4", AttackPerLevel: 18, HealthPerLevel: 52, DropWeight: 2},
	{ID: "accessory_shoes_r0", SlotID: "shoes", RarityID: "r0", AttackPerLevel: 1, HealthPerLevel: 14, DropWeight: 120, FuseTargetID: "accessory_shoes_r1"},
	{ID: "accessory_shoes_r1", SlotID: "shoes", RarityID: "r1", AttackPerLevel: 3, HealthPerLevel: 26, DropWeight: 55, FuseTargetID: "accessory_shoes_r2"},
	{ID: "accessory_shoes_r2", SlotID: "shoes", RarityID: "r2", AttackPerLevel: 5, HealthPerLevel: 44, DropWeight: 22, FuseTargetID: "accessory_shoes_r3"},
	{ID: "accessory_shoes_r3", SlotID: "shoes", RarityID: "r3", AttackPerLevel: 8, HealthPerLevel: 70, DropWeight: 8, FuseTargetID: "accessory_shoes_r4"},
	{ID: "accessory_shoes_r4", SlotID: "shoes", RarityID: "r4", AttackPerLevel: 12, HealthPerLevel: 100, DropWeight: 2},
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
		EnemyBaseHP:           220,
		EnemyHPPerPower:       2,
		EnemyHPPerFloor:       95,
		EnemyBaseDamage:       26,
		EnemyDamagePerFloor:   3,
		EnemyDamagePowerDiv:   48,
		MaxCombatSeconds:      DefaultCombatDurationSeconds,
	},
	DungeonEssence: {
		ID:                    DungeonEssence,
		DisplayName:           "Essence Dungeon",
		RewardCurrencyID:      economy.CurrencyMythEssence,
		BaseRequiredPower:     100,
		RequiredPowerPerFloor: 50,
		BaseRewardAmount:      110,
		RewardPerFloor:        40,
		EnemyBaseHP:           220,
		EnemyHPPerPower:       2,
		EnemyHPPerFloor:       95,
		EnemyBaseDamage:       26,
		EnemyDamagePerFloor:   3,
		EnemyDamagePowerDiv:   48,
		MaxCombatSeconds:      DefaultCombatDurationSeconds,
	},
	DungeonGear: {
		ID:                    DungeonGear,
		DisplayName:           "Gear Dungeon",
		RewardCurrencyID:      "",
		BaseRequiredPower:     120,
		RequiredPowerPerFloor: 56,
		BaseRewardAmount:      0,
		RewardPerFloor:        0,
		EnemyBaseHP:           220,
		EnemyHPPerPower:       2,
		EnemyHPPerFloor:       95,
		EnemyBaseDamage:       26,
		EnemyDamagePerFloor:   4,
		EnemyDamagePowerDiv:   48,
		MaxCombatSeconds:      DefaultCombatDurationSeconds,
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

func HeroDefinitions() []HeroDefinition {
	definitions := make([]HeroDefinition, len(heroDefinitions))
	copy(definitions, heroDefinitions)
	return definitions
}

func HeroDefinitionByID(heroID string) (HeroDefinition, bool) {
	for _, definition := range heroDefinitions {
		if definition.ID == heroID {
			return definition, true
		}
	}

	return HeroDefinition{}, false
}

func EquipmentDefinitions() []EquipmentDefinition {
	definitions := make([]EquipmentDefinition, len(equipmentDefinitions))
	copy(definitions, equipmentDefinitions)
	return definitions
}

func EquipmentDefinitionByID(equipmentID string) (EquipmentDefinition, bool) {
	for _, definition := range equipmentDefinitions {
		if definition.ID == equipmentID {
			return definition, true
		}
	}

	return EquipmentDefinition{}, false
}

func AFKRewardDefinitions() []AFKRewardDefinition {
	definitions := make([]AFKRewardDefinition, len(afkRewardDefinitions))
	copy(definitions, afkRewardDefinitions)
	return definitions
}

func AFKRewardDefinitionByID(afkRewardID string) (AFKRewardDefinition, bool) {
	for _, definition := range afkRewardDefinitions {
		if definition.ID == afkRewardID {
			return definition, true
		}
	}

	return AFKRewardDefinition{}, false
}

func CampaignDefinitions() []CampaignDefinition {
	definitions := make([]CampaignDefinition, len(campaignDefinitions))
	copy(definitions, campaignDefinitions)
	return definitions
}

func CampaignStageDefinitions() []CampaignStageDefinition {
	definitions := make([]CampaignStageDefinition, 0, 60)
	for stage := 1; stage <= 60; stage++ {
		combat := CampaignEnemyCombatStats(stage)
		definitions = append(definitions, CampaignStageDefinition{
			ID:               fmt.Sprintf("campaign_stage_%03d", stage),
			CampaignID:       "main_campaign",
			StageNumber:      stage,
			DisplayName:      fmt.Sprintf("Rift Echo %d", stage),
			RequiredPower:    CampaignRequiredPower(stage),
			RewardID:         fmt.Sprintf("reward_campaign_stage_%03d", stage),
			EnemyProfileID:   "rift_echo",
			EnemyMaxHP:       combat.MaxHP,
			EnemyDamage:      combat.Damage,
			MaxCombatSeconds: combat.MaxSeconds,
		})
	}
	return definitions
}

func RewardDefinitions() []RewardDefinition {
	definitions := []RewardDefinition{
		{ID: RewardSummonShards, DisplayName: "Hero Shards", RewardType: "summon", Reward: api.Reward{RewardID: RewardSummonShards}},
		{ID: RewardGearDrop, DisplayName: "Gear Dungeon Accessory Drop", RewardType: "gear_drop", Reward: api.Reward{RewardID: RewardGearDrop}},
		{ID: RewardAFKClaim, DisplayName: "AFK Gold and Myth Essence", RewardType: "afk", Reward: api.Reward{RewardID: RewardAFKClaim}},
	}

	for _, definition := range dailyMissionDefinitions {
		definitions = append(definitions, RewardDefinition{
			ID:          definition.Reward.RewardID,
			DisplayName: rewardDisplayName(definition.Reward.RewardID, fmt.Sprintf("%s Reward", definition.DisplayName)),
			RewardType:  "daily_mission",
			Reward:      definition.Reward,
		})
	}

	for index, definition := range battlePassRewardDefinitions {
		definitions = append(definitions, RewardDefinition{
			ID:          definition.Reward.RewardID,
			DisplayName: rewardDisplayName(definition.Reward.RewardID, fmt.Sprintf("Mission Track Reward %02d", index+1)),
			RewardType:  "battle_pass",
			Reward:      definition.Reward,
		})
	}

	for stage := 1; stage <= 60; stage++ {
		reward := CampaignReward(stage)
		definitions = append(definitions, RewardDefinition{
			ID:          reward.RewardID,
			DisplayName: fmt.Sprintf("Campaign Stage %d Reward", stage),
			RewardType:  "campaign_stage",
			Reward:      reward,
		})
	}

	return definitions
}

func rewardDisplayName(rewardID string, fallback string) string {
	switch rewardID {
	case "reward_daily_battles_15":
		return "Daily Battles Reward"
	case "reward_daily_stage_clears_3":
		return "Daily Stage Clears Reward"
	case "reward_daily_summon_1":
		return "Daily Summon Reward"
	default:
		return fallback
	}
}

func AccessorySlotDefinitions() []AccessorySlotDefinition {
	definitions := make([]AccessorySlotDefinition, len(accessorySlotDefinitions))
	copy(definitions, accessorySlotDefinitions)
	return definitions
}

func AccessoryRarityDefinitions() []AccessoryRarityDefinition {
	definitions := make([]AccessoryRarityDefinition, len(accessoryRarityDefinitions))
	copy(definitions, accessoryRarityDefinitions)
	return definitions
}

func AccessoryRarityDefinitionByID(rarityID string) (AccessoryRarityDefinition, bool) {
	for _, definition := range accessoryRarityDefinitions {
		if definition.ID == rarityID {
			return definition, true
		}
	}

	return AccessoryRarityDefinition{}, false
}

func AccessoryDefinitions() []AccessoryDefinition {
	definitions := make([]AccessoryDefinition, len(accessoryDefinitions))
	copy(definitions, accessoryDefinitions)
	return definitions
}

func AccessoryDefinitionByID(accessoryID string) (AccessoryDefinition, bool) {
	for _, definition := range accessoryDefinitions {
		if definition.ID == accessoryID {
			return definition, true
		}
	}

	return AccessoryDefinition{}, false
}

func DungeonRequiredPower(definition DungeonDefinition, floor int) int {
	floor = max(1, floor)
	return definition.BaseRequiredPower + (floor * definition.RequiredPowerPerFloor)
}

func DungeonEnemyCombatStats(definition DungeonDefinition, floor int) EnemyCombatStats {
	floor = max(1, floor)
	requiredPower := DungeonRequiredPower(definition, floor)
	powerDivisor := max(1, definition.EnemyDamagePowerDiv)
	baseMaxHP := definition.EnemyBaseHP + (requiredPower * definition.EnemyHPPerPower) + (floor * definition.EnemyHPPerFloor)
	return EnemyCombatStats{
		MaxHP:      DungeonBossMaxHP(baseMaxHP),
		Damage:     max(1, definition.EnemyBaseDamage+(floor*definition.EnemyDamagePerFloor)+(requiredPower/powerDivisor)),
		MaxSeconds: max(1, definition.MaxCombatSeconds),
	}
}

func DungeonBossMaxHP(baseMaxHP int) int {
	return max(1, (baseMaxHP*DungeonBossHpNumerator)/DungeonBossHpDenominator)
}

func DungeonReward(definition DungeonDefinition, floor int) api.Reward {
	floor = max(1, floor)
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
	stage = max(1, stage)
	return 90 + (stage * 46)
}

func CampaignEnemyCombatStats(stage int) EnemyCombatStats {
	stage = max(1, stage)
	definition := campaignDefinitions[0]
	requiredPower := CampaignRequiredPower(stage)
	powerDivisor := max(1, definition.EnemyDamagePowerDiv)
	return EnemyCombatStats{
		MaxHP:      max(1, definition.EnemyBaseHP+(requiredPower*definition.EnemyHPPerPower)+(stage*stage*definition.EnemyHPPerStageSquared)),
		Damage:     max(1, definition.EnemyBaseDamage+(stage*definition.EnemyDamagePerStage)+(requiredPower/powerDivisor)),
		MaxSeconds: max(1, definition.MaxCombatSeconds),
	}
}

func CampaignReward(stage int) api.Reward {
	stage = max(1, stage)
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

func AFKReward(stage int, elapsedSeconds int) (api.Reward, int) {
	return AFKRewardFromDefinition(afkRewardDefinitions[0], stage, elapsedSeconds)
}

func AFKRewardFromDefinition(definition AFKRewardDefinition, stage int, elapsedSeconds int) (api.Reward, int) {
	definition = NormalizeAFKRewardDefinition(definition)
	if elapsedSeconds < definition.MinClaimSeconds {
		return api.Reward{RewardID: definition.RewardID}, 0
	}

	claimSeconds := min(elapsedSeconds, definition.MaxClaimSeconds)
	ticks := claimSeconds / definition.TickSeconds
	essencePerTick := definition.BaseMythEssencePerTick + (max(1, stage) * definition.MythEssencePerStage)
	goldPerTick := max(1, essencePerTick/definition.GoldPerMythEssenceDivisor)
	return api.Reward{
		RewardID:    definition.RewardID,
		Gold:        ticks * goldPerTick,
		MythEssence: ticks * essencePerTick,
	}, claimSeconds
}

func NormalizeAFKRewardDefinition(definition AFKRewardDefinition) AFKRewardDefinition {
	if definition.ID == "" {
		definition.ID = "afk_default"
	}
	if definition.RewardID == "" {
		definition.RewardID = RewardAFKClaim
	}
	if definition.DisplayName == "" {
		definition.DisplayName = "AFK Gold and Myth Essence"
	}
	if definition.MinClaimSeconds <= 0 {
		definition.MinClaimSeconds = AFKMinClaimSeconds
	}
	if definition.MaxClaimSeconds <= 0 {
		definition.MaxClaimSeconds = AFKMaxClaimSeconds
	}
	if definition.MaxClaimSeconds < definition.MinClaimSeconds {
		definition.MaxClaimSeconds = definition.MinClaimSeconds
	}
	if definition.TickSeconds <= 0 {
		definition.TickSeconds = AFKRewardTickSeconds
	}
	if definition.TickSeconds > definition.MaxClaimSeconds {
		definition.TickSeconds = definition.MaxClaimSeconds
	}
	if definition.BaseMythEssencePerTick <= 0 {
		definition.BaseMythEssencePerTick = 3
	}
	if definition.MythEssencePerStage < 0 {
		definition.MythEssencePerStage = 1
	}
	if definition.GoldPerMythEssenceDivisor <= 0 {
		definition.GoldPerMythEssenceDivisor = 2
	}

	return definition
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
