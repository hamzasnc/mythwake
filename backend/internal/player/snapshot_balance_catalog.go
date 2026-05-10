package player

import (
	"fmt"
	"sort"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
)

type SnapshotBalanceCatalog struct {
	fallback              StaticBalanceCatalog
	campaigns             []api.CampaignDefinition
	campaignStagesByLevel map[int]api.CampaignStageDefinition
	heroes                []balance.HeroDefinition
	heroesByID            map[string]balance.HeroDefinition
	rewards               map[string]api.Reward
	dungeons              map[string]balance.DungeonDefinition
	progressionCosts      []api.ProgressionCostDefinition
	summonBanners         map[string]api.SummonBannerDefinition
	accessories           map[string]balance.AccessoryDefinition
	accessoryRarities     map[string]balance.AccessoryRarityDefinition
	dailyMissions         []balance.DailyMissionDefinition
	dailyMissionsByID     map[string]balance.DailyMissionDefinition
	battlePassRewards     map[string]balance.BattlePassRewardDefinition
	gearDrops             []api.AccessoryDefinition
	totalGearDropWeight   int
}

func NewSnapshotBalanceCatalog(snapshot api.DefinitionSnapshot) *SnapshotBalanceCatalog {
	catalog := &SnapshotBalanceCatalog{
		campaignStagesByLevel: map[int]api.CampaignStageDefinition{},
		heroesByID:            map[string]balance.HeroDefinition{},
		rewards:               map[string]api.Reward{},
		dungeons:              map[string]balance.DungeonDefinition{},
		summonBanners:         map[string]api.SummonBannerDefinition{},
		accessories:           map[string]balance.AccessoryDefinition{},
		accessoryRarities:     map[string]balance.AccessoryRarityDefinition{},
		dailyMissionsByID:     map[string]balance.DailyMissionDefinition{},
		battlePassRewards:     map[string]balance.BattlePassRewardDefinition{},
	}

	catalog.campaigns = append(catalog.campaigns, snapshot.Campaigns...)
	sort.Slice(catalog.campaigns, func(left int, right int) bool {
		return catalog.campaigns[left].CampaignID < catalog.campaigns[right].CampaignID
	})

	for _, definition := range snapshot.CampaignStages {
		catalog.campaignStagesByLevel[definition.StageNumber] = definition
	}

	for _, definition := range snapshot.Heroes {
		hero := balance.HeroDefinition{
			ID:           definition.HeroID,
			DisplayName:  definition.DisplayName,
			SortOrder:    definition.SortOrder,
			StarterOwned: definition.StarterOwned,
		}
		catalog.heroes = append(catalog.heroes, hero)
		catalog.heroesByID[hero.ID] = hero
	}
	sort.Slice(catalog.heroes, func(left int, right int) bool {
		if catalog.heroes[left].SortOrder == catalog.heroes[right].SortOrder {
			return catalog.heroes[left].ID < catalog.heroes[right].ID
		}
		return catalog.heroes[left].SortOrder < catalog.heroes[right].SortOrder
	})

	for _, definition := range snapshot.Rewards {
		reward := definition.Reward
		if reward.RewardID == "" {
			reward.RewardID = definition.RewardID
		}
		catalog.rewards[definition.RewardID] = reward
	}

	for _, definition := range snapshot.Dungeons {
		catalog.dungeons[definition.DungeonID] = balance.DungeonDefinition{
			ID:                    definition.DungeonID,
			DisplayName:           definition.DisplayName,
			RewardCurrencyID:      definition.RewardCurrencyID,
			BaseRequiredPower:     definition.BaseRequiredPower,
			RequiredPowerPerFloor: definition.RequiredPowerPerFloor,
			BaseRewardAmount:      definition.BaseRewardAmount,
			RewardPerFloor:        definition.RewardPerFloor,
			EnemyBaseHP:           definition.EnemyBaseHP,
			EnemyHPPerPower:       definition.EnemyHPPerPower,
			EnemyHPPerFloor:       definition.EnemyHPPerFloor,
			EnemyBaseDamage:       definition.EnemyBaseDamage,
			EnemyDamagePerFloor:   definition.EnemyDamagePerFloor,
			EnemyDamagePowerDiv:   definition.EnemyDamagePowerDivisor,
			MaxCombatSeconds:      definition.MaxCombatSeconds,
		}
	}

	catalog.progressionCosts = append(catalog.progressionCosts, snapshot.ProgressionCosts...)

	for _, definition := range snapshot.SummonBanners {
		catalog.summonBanners[definition.BannerID] = definition
	}

	for _, definition := range snapshot.AccessoryRarities {
		catalog.accessoryRarities[definition.RarityID] = balance.AccessoryRarityDefinition{
			ID:           definition.RarityID,
			RarityIndex:  definition.RarityIndex,
			DisplayName:  definition.DisplayName,
			MaxLevel:     definition.MaxLevel,
			FuseCopyCost: definition.FuseCopyCost,
		}
	}

	for _, definition := range snapshot.Accessories {
		catalog.accessories[definition.AccessoryID] = balance.AccessoryDefinition{
			ID:             definition.AccessoryID,
			SlotID:         definition.SlotID,
			RarityID:       definition.RarityID,
			AttackPerLevel: definition.AttackPerLevel,
			HealthPerLevel: definition.HealthPerLevel,
			DropWeight:     definition.DropWeight,
			FuseTargetID:   definition.FuseTargetID,
		}
	}

	for _, definition := range snapshot.DailyMissions {
		mission := balance.DailyMissionDefinition{
			ID:           definition.MissionID,
			DisplayName:  definition.DisplayName,
			ProgressType: definition.ProgressType,
			Target:       definition.Target,
			Reward:       definition.Reward,
		}
		catalog.dailyMissions = append(catalog.dailyMissions, mission)
		catalog.dailyMissionsByID[mission.ID] = mission
	}
	sort.Slice(catalog.dailyMissions, func(left int, right int) bool {
		return catalog.dailyMissions[left].ID < catalog.dailyMissions[right].ID
	})

	for _, definition := range snapshot.BattlePassRewards {
		catalog.battlePassRewards[definition.RewardID] = balance.BattlePassRewardDefinition{
			ID:             definition.RewardID,
			RequiredPassXP: definition.RequiredPassXP,
			Reward:         definition.Reward,
		}
	}

	catalog.gearDrops = append(catalog.gearDrops, snapshot.Accessories...)
	sort.Slice(catalog.gearDrops, func(left int, right int) bool {
		return catalog.gearDrops[left].AccessoryID < catalog.gearDrops[right].AccessoryID
	})
	for _, definition := range catalog.gearDrops {
		if definition.DropWeight > 0 {
			catalog.totalGearDropWeight += definition.DropWeight
		}
	}

	return catalog
}

func (catalog *SnapshotBalanceCatalog) CampaignEnemyCombatStats(stage int) balance.EnemyCombatStats {
	stage = max(1, stage)
	if definition, ok := catalog.campaignStagesByLevel[stage]; ok {
		return balance.EnemyCombatStats{
			MaxHP:      max(1, definition.EnemyMaxHP),
			Damage:     max(1, definition.EnemyDamage),
			MaxSeconds: max(1, definition.MaxCombatSeconds),
		}
	}

	if campaign, ok := catalog.primaryCampaign(); ok {
		requiredPower := campaign.BaseRequiredPower + (stage * campaign.RequiredPowerPerStage)
		powerDivisor := max(1, campaign.EnemyDamagePowerDivisor)
		return balance.EnemyCombatStats{
			MaxHP:      max(1, campaign.EnemyBaseHP+(requiredPower*campaign.EnemyHPPerPower)+(stage*stage*campaign.EnemyHPPerStageSquared)),
			Damage:     max(1, campaign.EnemyBaseDamage+(stage*campaign.EnemyDamagePerStage)+(requiredPower/powerDivisor)),
			MaxSeconds: max(1, campaign.MaxCombatSeconds),
		}
	}

	return catalog.fallback.CampaignEnemyCombatStats(stage)
}

func (catalog *SnapshotBalanceCatalog) CampaignReward(stage int) api.Reward {
	stage = max(1, stage)
	rewardID := fmt.Sprintf("reward_campaign_stage_%03d", stage)
	if reward, ok := catalog.rewards[rewardID]; ok {
		return reward
	}

	if campaign, ok := catalog.primaryCampaign(); ok {
		reward := api.Reward{
			RewardID:    rewardID,
			MythEssence: campaign.BaseMythEssenceReward + (stage * campaign.MythEssenceRewardPerStage),
		}
		if campaign.MilestoneEveryStages > 0 && stage%campaign.MilestoneEveryStages == 0 {
			reward.Gems = campaign.MilestoneBaseGems + (stage * campaign.MilestoneGemsPerStage)
			reward.PassXP = campaign.MilestonePassXP
		}
		return reward
	}

	return catalog.fallback.CampaignReward(stage)
}

func (catalog *SnapshotBalanceCatalog) DungeonDefinitionByID(dungeonID string) (balance.DungeonDefinition, bool) {
	if definition, ok := catalog.dungeons[dungeonID]; ok {
		return definition, true
	}

	return catalog.fallback.DungeonDefinitionByID(dungeonID)
}

func (catalog *SnapshotBalanceCatalog) DungeonEnemyCombatStats(definition balance.DungeonDefinition, floor int) balance.EnemyCombatStats {
	floor = max(1, floor)
	requiredPower := definition.BaseRequiredPower + (floor * definition.RequiredPowerPerFloor)
	powerDivisor := max(1, definition.EnemyDamagePowerDiv)
	return balance.EnemyCombatStats{
		MaxHP:      max(1, definition.EnemyBaseHP+(requiredPower*definition.EnemyHPPerPower)+(floor*definition.EnemyHPPerFloor)),
		Damage:     max(1, definition.EnemyBaseDamage+(floor*definition.EnemyDamagePerFloor)+(requiredPower/powerDivisor)),
		MaxSeconds: max(1, definition.MaxCombatSeconds),
	}
}

func (catalog *SnapshotBalanceCatalog) DungeonReward(definition balance.DungeonDefinition, floor int) api.Reward {
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

func (catalog *SnapshotBalanceCatalog) GearDungeonDropAccessoryID(floor int) string {
	if catalog.totalGearDropWeight <= 0 {
		return catalog.fallback.GearDungeonDropAccessoryID(floor)
	}

	roll := deterministicLootRoll(max(1, floor), catalog.totalGearDropWeight)
	for _, definition := range catalog.gearDrops {
		if definition.DropWeight <= 0 {
			continue
		}
		if roll < definition.DropWeight {
			return definition.AccessoryID
		}
		roll -= definition.DropWeight
	}

	return catalog.fallback.GearDungeonDropAccessoryID(floor)
}

func (catalog *SnapshotBalanceCatalog) GearDungeonReward() api.Reward {
	if reward, ok := catalog.rewards[balance.RewardGearDrop]; ok {
		return reward
	}

	return catalog.fallback.GearDungeonReward()
}

func (catalog *SnapshotBalanceCatalog) AFKReward(stage int, elapsedSeconds int) (api.Reward, int) {
	return catalog.fallback.AFKReward(stage, elapsedSeconds)
}

func (catalog *SnapshotBalanceCatalog) AFKMinClaimSeconds() int {
	return catalog.fallback.AFKMinClaimSeconds()
}

func (catalog *SnapshotBalanceCatalog) RewardAFKClaim() string {
	return catalog.fallback.RewardAFKClaim()
}

func (catalog *SnapshotBalanceCatalog) HeroDefinitions() []balance.HeroDefinition {
	if len(catalog.heroes) == 0 {
		return catalog.fallback.HeroDefinitions()
	}

	definitions := make([]balance.HeroDefinition, len(catalog.heroes))
	copy(definitions, catalog.heroes)
	return definitions
}

func (catalog *SnapshotBalanceCatalog) HeroDefinitionByID(heroID string) (balance.HeroDefinition, bool) {
	if definition, ok := catalog.heroesByID[heroID]; ok {
		return definition, true
	}

	return catalog.fallback.HeroDefinitionByID(heroID)
}

func (catalog *SnapshotBalanceCatalog) HeroLevelCost(level int) int {
	if cost, ok := catalog.progressionCost("hero", "*", economy.CurrencyMythEssence, level); ok {
		return cost
	}

	return catalog.fallback.HeroLevelCost(level)
}

func (catalog *SnapshotBalanceCatalog) HeroAscensionShardCost(ascension int) int {
	if cost, ok := catalog.progressionCost("hero", "*", "hero_shards", ascension); ok {
		return cost
	}

	return catalog.fallback.HeroAscensionShardCost(ascension)
}

func (catalog *SnapshotBalanceCatalog) EquipmentLevelCost(equipmentID string, level int) (int, bool) {
	if cost, ok := catalog.progressionCost("equipment", equipmentID, economy.CurrencyGold, level); ok {
		return cost, true
	}

	return catalog.fallback.EquipmentLevelCost(equipmentID, level)
}

func (catalog *SnapshotBalanceCatalog) AccessoryDefinitionByID(accessoryID string) (balance.AccessoryDefinition, bool) {
	if definition, ok := catalog.accessories[accessoryID]; ok {
		return definition, true
	}

	return catalog.fallback.AccessoryDefinitionByID(accessoryID)
}

func (catalog *SnapshotBalanceCatalog) AccessoryRarityDefinitionByID(rarityID string) (balance.AccessoryRarityDefinition, bool) {
	if definition, ok := catalog.accessoryRarities[rarityID]; ok {
		return definition, true
	}

	return catalog.fallback.AccessoryRarityDefinitionByID(rarityID)
}

func (catalog *SnapshotBalanceCatalog) AccessoryLevelCost(accessoryID string, level int) int {
	if cost, ok := catalog.progressionCost("accessory", accessoryID, economy.CurrencyGold, level); ok {
		return cost
	}

	return catalog.fallback.AccessoryLevelCost(accessoryID, level)
}

func (catalog *SnapshotBalanceCatalog) SummonCost(bannerID string) (int, bool) {
	if definition, ok := catalog.summonBanners[bannerID]; ok {
		return max(0, definition.CostAmount), true
	}

	return catalog.fallback.SummonCost(bannerID)
}

func (catalog *SnapshotBalanceCatalog) SummonShardReward(bannerID string, summonCount int) (balance.SummonShardDrop, bool) {
	definition, ok := catalog.summonBanners[bannerID]
	if !ok || len(definition.ShardDrops) == 0 {
		return catalog.fallback.SummonShardReward(bannerID, summonCount)
	}

	drop := definition.ShardDrops[max(0, summonCount)%len(definition.ShardDrops)]
	reward := api.Reward{RewardID: drop.RewardID}
	if definedReward, ok := catalog.rewards[drop.RewardID]; ok {
		reward = definedReward
	}

	return balance.SummonShardDrop{
		HeroID: drop.HeroID,
		Shards: drop.Shards,
		Reward: reward,
	}, true
}

func (catalog *SnapshotBalanceCatalog) DailyMissionDefinitionByID(missionID string) (balance.DailyMissionDefinition, bool) {
	if definition, ok := catalog.dailyMissionsByID[missionID]; ok {
		return definition, true
	}

	return catalog.fallback.DailyMissionDefinitionByID(missionID)
}

func (catalog *SnapshotBalanceCatalog) DailyMissionDefinitions() []balance.DailyMissionDefinition {
	if len(catalog.dailyMissions) == 0 {
		return catalog.fallback.DailyMissionDefinitions()
	}

	definitions := make([]balance.DailyMissionDefinition, len(catalog.dailyMissions))
	copy(definitions, catalog.dailyMissions)
	return definitions
}

func (catalog *SnapshotBalanceCatalog) DailyMissionReward(missionID string) (api.Reward, bool) {
	if definition, ok := catalog.dailyMissionsByID[missionID]; ok {
		return definition.Reward, true
	}

	return catalog.fallback.DailyMissionReward(missionID)
}

func (catalog *SnapshotBalanceCatalog) BattlePassRequiredXP(rewardID string) (int, bool) {
	if definition, ok := catalog.battlePassRewards[rewardID]; ok {
		return definition.RequiredPassXP, true
	}

	return catalog.fallback.BattlePassRequiredXP(rewardID)
}

func (catalog *SnapshotBalanceCatalog) BattlePassReward(rewardID string) (api.Reward, bool) {
	if definition, ok := catalog.battlePassRewards[rewardID]; ok {
		return definition.Reward, true
	}

	return catalog.fallback.BattlePassReward(rewardID)
}

func (catalog *SnapshotBalanceCatalog) primaryCampaign() (api.CampaignDefinition, bool) {
	if len(catalog.campaigns) == 0 {
		return api.CampaignDefinition{}, false
	}

	for _, definition := range catalog.campaigns {
		if definition.CampaignID == "main_campaign" {
			return definition, true
		}
	}

	return catalog.campaigns[0], true
}

func (catalog *SnapshotBalanceCatalog) progressionCost(domain string, targetID string, costCurrencyID string, currentLevel int) (int, bool) {
	if cost, ok := catalog.exactProgressionCost(domain, targetID, costCurrencyID, currentLevel); ok {
		return cost, true
	}

	if targetID != "*" {
		return catalog.exactProgressionCost(domain, "*", costCurrencyID, currentLevel)
	}

	return 0, false
}

func (catalog *SnapshotBalanceCatalog) exactProgressionCost(domain string, targetID string, costCurrencyID string, currentLevel int) (int, bool) {
	for _, definition := range catalog.progressionCosts {
		if definition.Domain == domain && definition.TargetID == targetID && definition.CostCurrencyID == costCurrencyID {
			return max(0, definition.BaseAmount+(max(0, currentLevel)*definition.AmountPerLevel)), true
		}
	}

	return 0, false
}

func deterministicLootRoll(floor int, totalWeight int) int {
	value := (uint64(max(1, floor)) * 1103515245) + 12345
	return int(value % uint64(totalWeight))
}
