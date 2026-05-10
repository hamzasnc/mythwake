package player

import (
	"testing"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/balance"
	"github.com/hamzasnc/mythwake/backend/internal/economy"
)

func TestSnapshotBalanceCatalogUsesSnapshotCombatDefinitions(t *testing.T) {
	catalog := NewSnapshotBalanceCatalog(api.DefinitionSnapshot{
		Heroes: []api.HeroDefinition{
			{HeroID: "hero_custom", DisplayName: "Custom", SortOrder: 20, StarterOwned: true, MaxLevel: 70, MaxAscension: 7, BaseAttack: 31, AttackPerLevel: 4, AttackPerAscension: 9, BaseHealth: 222, HealthPerLevel: 11, HealthPerAscension: 33},
			{HeroID: "hero_locked", DisplayName: "Locked", SortOrder: 10, StarterOwned: false, MaxLevel: 60, MaxAscension: 6, BaseAttack: 12, AttackPerLevel: 2, AttackPerAscension: 6, BaseHealth: 150, HealthPerLevel: 9, HealthPerAscension: 22},
		},
		Equipment: []api.EquipmentDefinition{
			{EquipmentID: "equipment_custom", DisplayName: "Custom Gear", SortOrder: 30, StarterOwned: true, MaxLevel: 5, AttackPerLevel: 4, HealthPerLevel: 25},
		},
		Rewards: []api.RewardDefinition{
			{
				RewardID: "reward_campaign_stage_003",
				Reward:   api.Reward{RewardID: "reward_campaign_stage_003", MythEssence: 777, Gems: 13},
			},
		},
		AFKRewards: []api.AFKRewardDefinition{
			{
				AFKRewardID:               "afk_test",
				RewardID:                  "reward_afk_test",
				DisplayName:               "AFK Test",
				MinClaimSeconds:           120,
				MaxClaimSeconds:           300,
				TickSeconds:               60,
				BaseMythEssencePerTick:    5,
				MythEssencePerStage:       2,
				GoldPerMythEssenceDivisor: 3,
			},
		},
		CampaignStages: []api.CampaignStageDefinition{
			{StageNumber: 3, EnemyMaxHP: 999, EnemyDamage: 77, MaxCombatSeconds: 12},
		},
		Dungeons: []api.DungeonDefinition{
			{
				DungeonID:               balance.DungeonGold,
				DisplayName:             "Gold Dungeon",
				RewardCurrencyID:        economy.CurrencyGold,
				BaseRequiredPower:       10,
				RequiredPowerPerFloor:   5,
				BaseRewardAmount:        20,
				RewardPerFloor:          7,
				EnemyBaseHP:             30,
				EnemyHPPerPower:         2,
				EnemyHPPerFloor:         9,
				EnemyBaseDamage:         4,
				EnemyDamagePerFloor:     3,
				EnemyDamagePowerDivisor: 5,
				MaxCombatSeconds:        18,
			},
		},
	})

	campaignCombat := catalog.CampaignEnemyCombatStats(3)
	if campaignCombat.MaxHP != 999 || campaignCombat.Damage != 77 || campaignCombat.MaxSeconds != 12 {
		t.Fatalf("expected snapshot campaign combat stats, got %#v", campaignCombat)
	}

	campaignReward := catalog.CampaignReward(3)
	if campaignReward.MythEssence != 777 || campaignReward.Gems != 13 {
		t.Fatalf("expected snapshot campaign reward, got %#v", campaignReward)
	}

	if catalog.AFKMinClaimSeconds() != 120 || catalog.RewardAFKClaim() != "reward_afk_test" {
		t.Fatalf("expected snapshot AFK meta, min=%d reward=%s", catalog.AFKMinClaimSeconds(), catalog.RewardAFKClaim())
	}
	afkReward, claimedSeconds := catalog.AFKReward(4, 10*60)
	if claimedSeconds != 300 || afkReward.RewardID != "reward_afk_test" || afkReward.Gold != 20 || afkReward.MythEssence != 65 {
		t.Fatalf("expected snapshot AFK reward cap and formula, reward=%#v claimedSeconds=%d", afkReward, claimedSeconds)
	}

	dungeon, ok := catalog.DungeonDefinitionByID(balance.DungeonGold)
	if !ok {
		t.Fatal("expected snapshot dungeon definition")
	}
	dungeonCombat := catalog.DungeonEnemyCombatStats(dungeon, 2)
	if dungeonCombat.MaxSeconds != 18 || dungeonCombat.MaxHP <= 0 || dungeonCombat.Damage <= 0 {
		t.Fatalf("expected snapshot dungeon combat stats, got %#v", dungeonCombat)
	}

	dungeonReward := catalog.DungeonReward(dungeon, 2)
	if dungeonReward.Gold != 34 {
		t.Fatalf("expected snapshot dungeon reward gold 34, got %#v", dungeonReward)
	}

	heroes := catalog.HeroDefinitions()
	if len(heroes) != 2 || heroes[0].ID != "hero_locked" || heroes[1].ID != "hero_custom" {
		t.Fatalf("expected sorted snapshot hero definitions, got %#v", heroes)
	}
	hero, ok := catalog.HeroDefinitionByID("hero_custom")
	if !ok || !hero.StarterOwned || hero.MaxLevel != 70 || hero.BaseAttack != 31 || hero.HealthPerAscension != 33 {
		t.Fatalf("expected snapshot hero_custom definition, got %#v ok=%t", hero, ok)
	}
	equipment, ok := catalog.EquipmentDefinitionByID("equipment_custom")
	if !ok || equipment.MaxLevel != 5 || equipment.AttackPerLevel != 4 || equipment.HealthPerLevel != 25 {
		t.Fatalf("expected snapshot equipment_custom definition, got %#v ok=%t", equipment, ok)
	}
}

func TestSnapshotBalanceCatalogUsesSnapshotCostsAndMetaRewards(t *testing.T) {
	catalog := NewSnapshotBalanceCatalog(api.DefinitionSnapshot{
		ProgressionCosts: []api.ProgressionCostDefinition{
			{Domain: "hero", TargetID: "*", CostCurrencyID: economy.CurrencyMythEssence, BaseAmount: 100, AmountPerLevel: 3},
			{Domain: "hero", TargetID: "*", CostCurrencyID: "hero_shards", BaseAmount: 40, AmountPerLevel: 11},
			{Domain: "equipment", TargetID: balance.EquipmentWeapon, CostCurrencyID: economy.CurrencyGold, BaseAmount: 9, AmountPerLevel: 2},
			{Domain: "accessory", TargetID: "*", CostCurrencyID: economy.CurrencyGold, BaseAmount: 6, AmountPerLevel: 1},
		},
		Equipment: []api.EquipmentDefinition{
			{EquipmentID: balance.EquipmentWeapon, StarterOwned: true, MaxLevel: 10, AttackPerLevel: 11},
		},
		SummonBanners: []api.SummonBannerDefinition{
			{
				BannerID:   balance.BannerHeroShardStandard,
				CostAmount: 123,
				ShardDrops: []api.SummonShardDrop{
					{HeroID: "hero_a", Shards: 5, RewardID: balance.RewardSummonShards},
					{HeroID: "hero_b", Shards: 8, RewardID: balance.RewardSummonShards},
				},
			},
		},
		Rewards: []api.RewardDefinition{
			{RewardID: balance.RewardSummonShards, Reward: api.Reward{RewardID: balance.RewardSummonShards}},
			{RewardID: balance.RewardGearDrop, Reward: api.Reward{RewardID: balance.RewardGearDrop}},
		},
		AccessoryRarities: []api.AccessoryRarityDefinition{
			{RarityID: "test_rarity", MaxLevel: 12, FuseCopyCost: 2},
		},
		DailyMissions: []api.DailyMissionDefinition{
			{MissionID: "daily_test", ProgressType: "fight", Target: 2, Reward: api.Reward{RewardID: "reward_daily_test", Gems: 9}},
		},
		BattlePassRewards: []api.BattlePassRewardDefinition{
			{RewardID: "track_test", RequiredPassXP: 88, Reward: api.Reward{RewardID: "reward_track_test", MythEssence: 44}},
		},
		Accessories: []api.AccessoryDefinition{
			{AccessoryID: "accessory_only_drop", SlotID: "test_slot", RarityID: "test_rarity", DropWeight: 1, FuseTargetID: "accessory_next"},
		},
	})

	if cost := catalog.HeroLevelCost(4); cost != 112 {
		t.Fatalf("expected hero level cost 112, got %d", cost)
	}
	if cost := catalog.HeroAscensionShardCost(2); cost != 62 {
		t.Fatalf("expected hero ascension cost 62, got %d", cost)
	}
	if cost, ok := catalog.EquipmentLevelCost(balance.EquipmentWeapon, 3); !ok || cost != 15 {
		t.Fatalf("expected equipment cost 15, ok=true, got %d %t", cost, ok)
	}
	if cost := catalog.AccessoryLevelCost("accessory_any", 5); cost != 11 {
		t.Fatalf("expected accessory cost 11, got %d", cost)
	}

	if cost, ok := catalog.SummonCost(balance.BannerHeroShardStandard); !ok || cost != 123 {
		t.Fatalf("expected summon cost 123, ok=true, got %d %t", cost, ok)
	}
	drop, ok := catalog.SummonShardReward(balance.BannerHeroShardStandard, 1)
	if !ok || drop.HeroID != "hero_b" || drop.Shards != 8 {
		t.Fatalf("expected snapshot summon drop hero_b x8, got %#v ok=%t", drop, ok)
	}

	mission, ok := catalog.DailyMissionDefinitionByID("daily_test")
	if !ok || mission.Target != 2 {
		t.Fatalf("expected daily mission definition, got %#v ok=%t", mission, ok)
	}
	missionReward, ok := catalog.DailyMissionReward("daily_test")
	if !ok || missionReward.Gems != 9 {
		t.Fatalf("expected daily mission reward, got %#v ok=%t", missionReward, ok)
	}
	requiredXP, ok := catalog.BattlePassRequiredXP("track_test")
	if !ok || requiredXP != 88 {
		t.Fatalf("expected battle pass required xp 88, got %d ok=%t", requiredXP, ok)
	}
	battlePassReward, ok := catalog.BattlePassReward("track_test")
	if !ok || battlePassReward.MythEssence != 44 {
		t.Fatalf("expected battle pass reward, got %#v ok=%t", battlePassReward, ok)
	}

	if accessoryID := catalog.GearDungeonDropAccessoryID(25); accessoryID != "accessory_only_drop" {
		t.Fatalf("expected only weighted accessory drop, got %s", accessoryID)
	}
	accessory, ok := catalog.AccessoryDefinitionByID("accessory_only_drop")
	if !ok || accessory.SlotID != "test_slot" || accessory.FuseTargetID != "accessory_next" {
		t.Fatalf("expected snapshot accessory definition, got %#v ok=%t", accessory, ok)
	}
	rarity, ok := catalog.AccessoryRarityDefinitionByID("test_rarity")
	if !ok || rarity.MaxLevel != 12 || rarity.FuseCopyCost != 2 {
		t.Fatalf("expected snapshot rarity definition, got %#v ok=%t", rarity, ok)
	}
}
