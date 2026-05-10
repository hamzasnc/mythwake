package balance

import (
	"fmt"

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

func DungeonDefinitionByID(dungeonID string) (DungeonDefinition, bool) {
	definition, ok := dungeonDefinitions[dungeonID]
	return definition, ok
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

func DailyMissionReward(missionID string) api.Reward {
	return api.Reward{RewardID: "reward_" + missionID, Gold: 40, Gems: 5, MythEssence: 70, PassXP: 40}
}

func BattlePassRequiredXP(_ string) int {
	return 40
}

func BattlePassReward(rewardID string) api.Reward {
	return api.Reward{RewardID: rewardID, Gold: 100, Gems: 10}
}

func GearDungeonDropAccessoryID(_ int) string {
	return StarterGearDropAccessoryID
}

func GearDungeonReward() api.Reward {
	return api.Reward{
		RewardID: RewardGearDrop,
	}
}
