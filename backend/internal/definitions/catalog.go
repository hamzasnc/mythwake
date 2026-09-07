package definitions

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/auth"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

const SchemaVersion = 9

func Snapshot(apiVersion string) api.DefinitionSnapshot {
	snapshot := api.DefinitionSnapshot{
		SchemaVersion:     SchemaVersion,
		APIVersion:        apiVersion,
		AuthProviders:     authProviderDefinitions(),
		Currencies:        currencyDefinitions(),
		Heroes:            heroDefinitions(),
		Equipment:         equipmentDefinitions(),
		Rewards:           rewardDefinitions(),
		AFKRewards:        afkRewardDefinitions(),
		Campaigns:         campaignDefinitions(),
		CampaignStages:    campaignStageDefinitions(),
		Dungeons:          dungeonDefinitions(),
		AccessorySlots:    accessorySlotDefinitions(),
		AccessoryRarities: accessoryRarityDefinitions(),
		Accessories:       accessoryDefinitions(),
		VillageBuildings:  villageBuildingDefinitions(),
		ProgressionCosts:  progressionCostDefinitions(),
		SummonBanners:     summonBannerDefinitions(),
		DailyMissions:     dailyMissionDefinitions(),
		BattlePassRewards: battlePassRewardDefinitions(),
		ShopOffers:        shopOfferDefinitions(),
		GameplayActions:   gameplayActionDefinitions(),
	}
	snapshot.ContentHash = ContentHash(snapshot)
	return snapshot
}

func ContentHash(snapshot api.DefinitionSnapshot) string {
	snapshot.ContentHash = ""
	rawSnapshot, err := json.Marshal(snapshot)
	if err != nil {
		return ""
	}

	sum := sha256.Sum256(rawSnapshot)
	return hex.EncodeToString(sum[:])
}

func ETag(snapshot api.DefinitionSnapshot) string {
	return fmt.Sprintf(`"definitions-%s"`, snapshot.ContentHash)
}

func authProviderDefinitions() []api.AuthProviderDefinition {
	definitions := auth.ProviderDefinitions()
	response := make([]api.AuthProviderDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.AuthProviderDefinition{
			ProviderID:        definition.ID,
			DisplayName:       definition.DisplayName,
			ExternalProvider:  definition.ExternalProvider,
			SupportsLinking:   definition.SupportsLinking,
			SupportsMobileSSO: definition.SupportsMobileSSO,
		})
	}
	return response
}

func currencyDefinitions() []api.CurrencyDefinition {
	definitions := economy.CurrencyDefinitions()
	response := make([]api.CurrencyDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.CurrencyDefinition{
			CurrencyID:  definition.ID,
			DisplayName: definition.DisplayName,
			IsPremium:   definition.IsPremium,
		})
	}
	return response
}

func heroDefinitions() []api.HeroDefinition {
	definitions := balance.HeroDefinitions()
	response := make([]api.HeroDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.HeroDefinition{
			HeroID:             definition.ID,
			DisplayName:        definition.DisplayName,
			SortOrder:          definition.SortOrder,
			StarterOwned:       definition.StarterOwned,
			MaxLevel:           definition.MaxLevel,
			MaxAscension:       definition.MaxAscension,
			BaseAttack:         definition.BaseAttack,
			AttackPerLevel:     definition.AttackPerLevel,
			AttackPerAscension: definition.AttackPerAscension,
			BaseHealth:         definition.BaseHealth,
			HealthPerLevel:     definition.HealthPerLevel,
			HealthPerAscension: definition.HealthPerAscension,
		})
	}
	return response
}

func equipmentDefinitions() []api.EquipmentDefinition {
	definitions := balance.EquipmentDefinitions()
	response := make([]api.EquipmentDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.EquipmentDefinition{
			EquipmentID:    definition.ID,
			DisplayName:    definition.DisplayName,
			SortOrder:      definition.SortOrder,
			StarterOwned:   definition.StarterOwned,
			MaxLevel:       definition.MaxLevel,
			AttackPerLevel: definition.AttackPerLevel,
			HealthPerLevel: definition.HealthPerLevel,
		})
	}
	return response
}

func rewardDefinitions() []api.RewardDefinition {
	definitions := balance.RewardDefinitions()
	response := make([]api.RewardDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.RewardDefinition{
			RewardID:    definition.ID,
			DisplayName: definition.DisplayName,
			RewardType:  definition.RewardType,
			Reward:      definition.Reward,
		})
	}
	return response
}

func afkRewardDefinitions() []api.AFKRewardDefinition {
	definitions := balance.AFKRewardDefinitions()
	response := make([]api.AFKRewardDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.AFKRewardDefinition{
			AFKRewardID:               definition.ID,
			RewardID:                  definition.RewardID,
			DisplayName:               definition.DisplayName,
			MinClaimSeconds:           definition.MinClaimSeconds,
			MaxClaimSeconds:           definition.MaxClaimSeconds,
			TickSeconds:               definition.TickSeconds,
			BaseMythEssencePerTick:    definition.BaseMythEssencePerTick,
			MythEssencePerStage:       definition.MythEssencePerStage,
			GoldPerMythEssenceDivisor: definition.GoldPerMythEssenceDivisor,
		})
	}
	return response
}

func campaignDefinitions() []api.CampaignDefinition {
	definitions := balance.CampaignDefinitions()
	response := make([]api.CampaignDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.CampaignDefinition{
			CampaignID:                definition.ID,
			DisplayName:               definition.DisplayName,
			BaseRequiredPower:         definition.BaseRequiredPower,
			RequiredPowerPerStage:     definition.RequiredPowerPerStage,
			BaseMythEssenceReward:     definition.BaseMythEssenceReward,
			MythEssenceRewardPerStage: definition.MythEssenceRewardPerStage,
			MilestoneEveryStages:      definition.MilestoneEveryStages,
			MilestoneBaseGems:         definition.MilestoneBaseGems,
			MilestoneGemsPerStage:     definition.MilestoneGemsPerStage,
			MilestonePassXP:           definition.MilestonePassXP,
			EnemyBaseHP:               definition.EnemyBaseHP,
			EnemyHPPerPower:           definition.EnemyHPPerPower,
			EnemyHPPerStageSquared:    definition.EnemyHPPerStageSquared,
			EnemyBaseDamage:           definition.EnemyBaseDamage,
			EnemyDamagePerStage:       definition.EnemyDamagePerStage,
			EnemyDamagePowerDivisor:   definition.EnemyDamagePowerDiv,
			MaxCombatSeconds:          definition.MaxCombatSeconds,
		})
	}
	return response
}

func campaignStageDefinitions() []api.CampaignStageDefinition {
	definitions := balance.CampaignStageDefinitions()
	response := make([]api.CampaignStageDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.CampaignStageDefinition{
			StageID:          definition.ID,
			CampaignID:       definition.CampaignID,
			StageNumber:      definition.StageNumber,
			DisplayName:      definition.DisplayName,
			RequiredPower:    definition.RequiredPower,
			RewardID:         definition.RewardID,
			EnemyProfileID:   definition.EnemyProfileID,
			EnemyMaxHP:       definition.EnemyMaxHP,
			EnemyDamage:      definition.EnemyDamage,
			MaxCombatSeconds: definition.MaxCombatSeconds,
		})
	}
	return response
}

func dungeonDefinitions() []api.DungeonDefinition {
	definitions := balance.DungeonDefinitions()
	response := make([]api.DungeonDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.DungeonDefinition{
			DungeonID:               definition.ID,
			DisplayName:             definition.DisplayName,
			RewardCurrencyID:        definition.RewardCurrencyID,
			BaseRequiredPower:       definition.BaseRequiredPower,
			RequiredPowerPerFloor:   definition.RequiredPowerPerFloor,
			BaseRewardAmount:        definition.BaseRewardAmount,
			RewardPerFloor:          definition.RewardPerFloor,
			EnemyBaseHP:             definition.EnemyBaseHP,
			EnemyHPPerPower:         definition.EnemyHPPerPower,
			EnemyHPPerFloor:         definition.EnemyHPPerFloor,
			EnemyBaseDamage:         definition.EnemyBaseDamage,
			EnemyDamagePerFloor:     definition.EnemyDamagePerFloor,
			EnemyDamagePowerDivisor: definition.EnemyDamagePowerDiv,
			MaxCombatSeconds:        definition.MaxCombatSeconds,
		})
	}
	return response
}

func accessorySlotDefinitions() []api.AccessorySlotDefinition {
	definitions := balance.AccessorySlotDefinitions()
	response := make([]api.AccessorySlotDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.AccessorySlotDefinition{
			SlotID:      definition.ID,
			DisplayName: definition.DisplayName,
			SortOrder:   definition.SortOrder,
		})
	}
	return response
}

func accessoryRarityDefinitions() []api.AccessoryRarityDefinition {
	definitions := balance.AccessoryRarityDefinitions()
	response := make([]api.AccessoryRarityDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.AccessoryRarityDefinition{
			RarityID:     definition.ID,
			RarityIndex:  definition.RarityIndex,
			DisplayName:  definition.DisplayName,
			MaxLevel:     definition.MaxLevel,
			FuseCopyCost: definition.FuseCopyCost,
		})
	}
	return response
}

func accessoryDefinitions() []api.AccessoryDefinition {
	definitions := balance.AccessoryDefinitions()
	response := make([]api.AccessoryDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.AccessoryDefinition{
			AccessoryID:    definition.ID,
			SlotID:         definition.SlotID,
			RarityID:       definition.RarityID,
			AttackPerLevel: definition.AttackPerLevel,
			HealthPerLevel: definition.HealthPerLevel,
			DropWeight:     definition.DropWeight,
			FuseTargetID:   definition.FuseTargetID,
		})
	}
	return response
}

func villageBuildingDefinitions() []api.VillageBuildingDefinition {
	definitions := balance.VillageBuildingDefinitions()
	response := make([]api.VillageBuildingDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.VillageBuildingDefinition{
			BuildingID:          definition.ID,
			SlotIndex:           definition.SlotIndex,
			BuildingOptionIndex: definition.BuildingOptionIndex,
			DisplayName:         definition.DisplayName,
			TextureName:         definition.TextureName,
			BuildCost:           definition.BuildCost,
			MaxLevel:            definition.MaxLevel,
			UpgradeCostPerLevel: definition.UpgradeCostPerLevel,
			BonusType:           definition.BonusType,
			BonusLabel:          definition.BonusLabel,
			BonusValuePerLevel:  definition.BonusValuePerLevel,
			BonusCurve:          definition.BonusCurve,
			UpgradeCostFormula:  definition.UpgradeCostFormula,
			ModeCompatibility:   definition.ModeCompatibility,
		})
	}
	return response
}

func progressionCostDefinitions() []api.ProgressionCostDefinition {
	definitions := balance.ProgressionCostDefinitions()
	response := make([]api.ProgressionCostDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.ProgressionCostDefinition{
			CostID:         definition.ID,
			Domain:         definition.Domain,
			TargetID:       definition.TargetID,
			CostCurrencyID: definition.CostCurrencyID,
			BaseAmount:     definition.BaseAmount,
			AmountPerLevel: definition.AmountPerLevel,
			Formula:        definition.Formula,
		})
	}
	return response
}

func summonBannerDefinitions() []api.SummonBannerDefinition {
	definitions := balance.SummonBannerDefinitions()
	response := make([]api.SummonBannerDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.SummonBannerDefinition{
			BannerID:       definition.ID,
			DisplayName:    definition.DisplayName,
			CostCurrencyID: definition.CostCurrencyID,
			CostAmount:     definition.CostAmount,
			ResolutionMode: definition.ResolutionMode,
			ShardDrops:     summonShardDrops(definition.ShardDrops),
		})
	}
	return response
}

func summonShardDrops(drops []balance.SummonShardDrop) []api.SummonShardDrop {
	response := make([]api.SummonShardDrop, 0, len(drops))
	for _, drop := range drops {
		response = append(response, api.SummonShardDrop{
			HeroID:   drop.HeroID,
			Shards:   drop.Shards,
			RewardID: drop.Reward.RewardID,
		})
	}
	return response
}

func dailyMissionDefinitions() []api.DailyMissionDefinition {
	definitions := balance.DailyMissionDefinitions()
	response := make([]api.DailyMissionDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.DailyMissionDefinition{
			MissionID:    definition.ID,
			DisplayName:  definition.DisplayName,
			ProgressType: definition.ProgressType,
			Target:       definition.Target,
			Reward:       definition.Reward,
		})
	}
	return response
}

func battlePassRewardDefinitions() []api.BattlePassRewardDefinition {
	definitions := balance.BattlePassRewardDefinitions()
	response := make([]api.BattlePassRewardDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.BattlePassRewardDefinition{
			RewardID:       definition.ID,
			RequiredPassXP: definition.RequiredPassXP,
			Reward:         definition.Reward,
		})
	}
	return response
}

func shopOfferDefinitions() []api.ShopOfferDefinition {
	return []api.ShopOfferDefinition{
		{OfferID: "starter_pack", Tab: "featured", DisplayName: "Starter Pack", Contents: "500 Crystals\n25K Gold · 5 Essence", Price: "€2.99", IconKey: "icon_gold", SortOrder: 10},
		{OfferID: "crystal_cache", Tab: "featured", DisplayName: "Crystal Cache", Contents: "1,100 Crystals\n60K Gold · 5 Essence", Price: "€4.99", IconKey: "icon_gems", SortOrder: 20},
		{OfferID: "adventurer_bundle", Tab: "featured", DisplayName: "Adventurer Bundle", Contents: "2,200 Crystals\n120K Gold · 15 Essence", Price: "€14.99", IconKey: "home_shop_button", SortOrder: 30},
		{OfferID: "legendary_chest", Tab: "featured", DisplayName: "Legendary Chest", Contents: "5,000 Crystals\n250K Gold · 25 Essence", Price: "€19.99", IconKey: "home_treasure_chest_button", SortOrder: 40},
		{OfferID: "crystal_pouch", Tab: "crystals", DisplayName: "Crystal Pouch", Contents: "100 Myth Crystals\nStarter stash", Price: "€0.99", IconKey: "shop_icon_crystal_altar", SortOrder: 10},
		{OfferID: "crystal_pack", Tab: "crystals", DisplayName: "Crystal Pack", Contents: "500 Myth Crystals\n+ 5% bonus", Price: "€3.99", IconKey: "shop_icon_crystal_vault", SortOrder: 20},
		{OfferID: "crystal_cache", Tab: "crystals", DisplayName: "Crystal Cache", Contents: "1,100 Myth Crystals\n+ 10% bonus", Price: "€7.99", IconKey: "shop_icon_crystal_altar", SortOrder: 30, TopPick: true, BadgeLabel: "BEST VALUE"},
		{OfferID: "crystal_vault", Tab: "crystals", DisplayName: "Crystal Vault", Contents: "2,500 Myth Crystals\n+ 25% bonus", Price: "€14.99", IconKey: "shop_icon_crystal_vault", SortOrder: 40},
		{OfferID: "crystal_reserve", Tab: "crystals", DisplayName: "Crystal Reserve", Contents: "5,000 Myth Crystals\n+ 35% bonus", Price: "€24.99", IconKey: "shop_icon_crystal_altar", SortOrder: 50},
		{OfferID: "crystal_treasury", Tab: "crystals", DisplayName: "Crystal Treasury", Contents: "12,000 Myth Crystals\n+ 45% bonus", Price: "€49.99", IconKey: "shop_icon_crystal_vault", SortOrder: 60, TopPick: true, BadgeLabel: "BEST VALUE"},
		{OfferID: "crystal_hoard", Tab: "crystals", DisplayName: "Crystal Hoard", Contents: "25,000 Myth Crystals\n+ 55% bonus", Price: "€89.99", IconKey: "shop_icon_crystal_altar", SortOrder: 70},
		{OfferID: "crystal_relic", Tab: "crystals", DisplayName: "Ancient Relic Cache", Contents: "50,000 Myth Crystals\n+ 70% bonus", Price: "€149.99", IconKey: "shop_icon_crystal_vault", SortOrder: 80},
		{OfferID: "crystal_ascendant", Tab: "crystals", DisplayName: "Ascendant Crystals", Contents: "100,000 Myth Crystals\n+ 90% bonus", Price: "€249.99", IconKey: "shop_icon_crystal_altar", SortOrder: 90},
		{OfferID: "crystal_eternal", Tab: "crystals", DisplayName: "Eternal Crystal Vault", Contents: "250,000 Myth Crystals\n+ 120% bonus", Price: "€499.99", IconKey: "shop_icon_crystal_vault", SortOrder: 100},
		{OfferID: "daily_deal", Tab: "bundles", DisplayName: "Daily Deal", Contents: "250 Crystals\n15K Gold · 2 Essence", Price: "€1.99", IconKey: "shop_icon_adventurer_satchel", SortOrder: 10},
		{OfferID: "hero_bundle", Tab: "bundles", DisplayName: "Hero Bundle", Contents: "1,000 Crystals\nHero Shard Chest ×2", Price: "€8.99", IconKey: "shop_icon_bundle_chest", SortOrder: 20},
		{OfferID: "adventurer_bundle", Tab: "bundles", DisplayName: "Adventurer Bundle", Contents: "2,200 Crystals\n120K Gold · 15 Essence", Price: "€14.99", IconKey: "shop_icon_adventurer_satchel", SortOrder: 30, TopPick: true, BadgeLabel: "TOP PICK"},
		{OfferID: "legendary_chest", Tab: "bundles", DisplayName: "Legendary Chest", Contents: "5,000 Crystals\n250K Gold · 25 Essence", Price: "€19.99", IconKey: "shop_icon_bundle_chest", SortOrder: 40},
		{OfferID: "dungeon_expedition", Tab: "bundles", DisplayName: "Dungeon Expedition", Contents: "3,500 Crystals\n150K Gold · 20 Essence", Price: "€24.99", IconKey: "shop_icon_adventurer_satchel", SortOrder: 50},
		{OfferID: "guild_foundry", Tab: "bundles", DisplayName: "Guild Foundry Pack", Contents: "4,500 Crystals\n200K Gold · 30 Essence", Price: "€34.99", IconKey: "shop_icon_bundle_chest", SortOrder: 60},
		{OfferID: "royal_war_chest", Tab: "bundles", DisplayName: "Royal War Chest", Contents: "8,000 Crystals\n400K Gold · 45 Essence", Price: "€49.99", IconKey: "shop_icon_bundle_chest", SortOrder: 70, TopPick: true, BadgeLabel: "TOP PICK"},
		{OfferID: "mythic_arsenal", Tab: "bundles", DisplayName: "Mythic Arsenal", Contents: "12,000 Crystals\n650K Gold · 60 Essence", Price: "€69.99", IconKey: "shop_icon_adventurer_satchel", SortOrder: 80},
		{OfferID: "worldbreaker_cache", Tab: "bundles", DisplayName: "Worldbreaker Cache", Contents: "20,000 Crystals\n1M Gold · 90 Essence", Price: "€99.99", IconKey: "shop_icon_bundle_chest", SortOrder: 90},
		{OfferID: "founder_legacy", Tab: "bundles", DisplayName: "Founder’s Legacy", Contents: "35,000 Crystals\n2M Gold · 120 Essence", Price: "€149.99", IconKey: "shop_icon_adventurer_satchel", SortOrder: 100},
	}
}

func gameplayActionDefinitions() []api.GameplayActionDefinition {
	definitions := gameplay.ActionCatalog()
	response := make([]api.GameplayActionDefinition, 0, len(definitions))
	for _, definition := range definitions {
		response = append(response, api.GameplayActionDefinition{
			ActionID:            definition.ID,
			Domain:              definition.Domain,
			RequiresIdempotency: definition.RequiresIdempotency,
			MaterializedByFlush: definition.MaterializedByFlush,
		})
	}
	return response
}
