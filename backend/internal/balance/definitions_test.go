package balance

import (
	"testing"

	"github.com/hamzasnc/mythwake/backend/internal/economy"
)

func TestDungeonDefinitionsMatchSeededCurves(t *testing.T) {
	gold, ok := DungeonDefinitionByID(DungeonGold)
	if !ok {
		t.Fatalf("expected %s definition", DungeonGold)
	}
	if gold.RewardCurrencyID != economy.CurrencyGold || DungeonRequiredPower(gold, 1) != 150 {
		t.Fatalf("unexpected gold dungeon definition: %#v", gold)
	}
	if reward := DungeonReward(gold, 1); reward.Gold != 129 {
		t.Fatalf("expected floor 1 gold reward 129, got %#v", reward)
	}
	goldCombat := DungeonEnemyCombatStats(gold, 1)
	if goldCombat.MaxHP != 1107 || goldCombat.Damage != 32 || goldCombat.MaxSeconds != DefaultCombatDurationSeconds {
		t.Fatalf("unexpected gold dungeon combat stats: %#v", goldCombat)
	}

	essence, ok := DungeonDefinitionByID(DungeonEssence)
	if !ok {
		t.Fatalf("expected %s definition", DungeonEssence)
	}
	if essence.RewardCurrencyID != economy.CurrencyMythEssence {
		t.Fatalf("unexpected essence dungeon currency: %#v", essence)
	}
	if reward := DungeonReward(essence, 1); reward.MythEssence != 150 {
		t.Fatalf("expected floor 1 essence reward 150, got %#v", reward)
	}
}

func TestCampaignEnemyCombatStats(t *testing.T) {
	stats := CampaignEnemyCombatStats(1)

	if stats.MaxHP != 458 || stats.Damage != 24 || stats.MaxSeconds != DefaultCombatDurationSeconds {
		t.Fatalf("unexpected campaign stage 1 combat stats: %#v", stats)
	}
}

func TestCampaignMilestoneReward(t *testing.T) {
	reward := CampaignReward(5)

	if reward.MythEssence != 27 || reward.Gems != 17 || reward.PassXP != 25 {
		t.Fatalf("unexpected stage 5 reward: %#v", reward)
	}
}

func TestHeroDefinitions(t *testing.T) {
	hero, ok := HeroDefinitionByID("hero_astra")
	if !ok || !hero.StarterOwned || hero.SortOrder != 10 {
		t.Fatalf("unexpected hero_astra definition: %#v ok=%v", hero, ok)
	}
	if hero.MaxLevel != 100 || hero.MaxAscension != 10 || hero.BaseAttack != 18 || hero.AttackPerLevel != 5 || hero.AttackPerAscension != 11 {
		t.Fatalf("unexpected hero_astra attack progression: %#v", hero)
	}
	if hero.BaseHealth != 150 || hero.HealthPerLevel != 28 || hero.HealthPerAscension != 70 {
		t.Fatalf("unexpected hero_astra health progression: %#v", hero)
	}
	ravik, ok := HeroDefinitionByID("hero_ravik")
	if !ok || !ravik.StarterOwned || ravik.SortOrder != 60 || ravik.BaseAttack != 24 || ravik.AttackPerLevel != 7 || ravik.HealthPerLevel != 22 {
		t.Fatalf("unexpected hero_ravik definition: %#v ok=%v", ravik, ok)
	}
	liora, ok := HeroDefinitionByID("hero_liora")
	if !ok || !liora.StarterOwned || liora.SortOrder != 70 || liora.BaseAttack != 21 || liora.AttackPerLevel != 6 || liora.HealthPerLevel != 27 {
		t.Fatalf("unexpected hero_liora definition: %#v ok=%v", liora, ok)
	}
	if _, ok := HeroDefinitionByID("hero_fake"); ok {
		t.Fatal("unknown hero should not exist")
	}
}

func TestEquipmentDefinitions(t *testing.T) {
	weapon, ok := EquipmentDefinitionByID(EquipmentWeapon)
	if !ok || !weapon.StarterOwned || weapon.MaxLevel != 100 || weapon.AttackPerLevel != 7 || weapon.HealthPerLevel != 0 {
		t.Fatalf("unexpected weapon definition: %#v ok=%v", weapon, ok)
	}
	armor, ok := EquipmentDefinitionByID(EquipmentArmor)
	if !ok || !armor.StarterOwned || armor.MaxLevel != 100 || armor.AttackPerLevel != 0 || armor.HealthPerLevel != 65 {
		t.Fatalf("unexpected armor definition: %#v ok=%v", armor, ok)
	}
	if _, ok := EquipmentDefinitionByID("equipment_fake"); ok {
		t.Fatal("unknown equipment should not exist")
	}
}

func TestAFKRewardDefinitions(t *testing.T) {
	definition, ok := AFKRewardDefinitionByID("afk_default")
	if !ok || definition.RewardID != RewardAFKClaim || definition.MinClaimSeconds != 60 || definition.MaxClaimSeconds != 21600 || definition.TickSeconds != 60 {
		t.Fatalf("unexpected afk_default definition: %#v ok=%v", definition, ok)
	}

	reward, claimedSeconds := AFKRewardFromDefinition(definition, 5, 2*60)
	if claimedSeconds != 120 || reward.Gold != 8 || reward.MythEssence != 16 {
		t.Fatalf("unexpected AFK reward: reward=%#v claimedSeconds=%d", reward, claimedSeconds)
	}

	if _, ok := AFKRewardDefinitionByID("afk_fake"); ok {
		t.Fatal("unknown AFK reward definition should not exist")
	}
}

func TestProgressionCosts(t *testing.T) {
	if HeroLevelCost(1) != 20 {
		t.Fatalf("expected level 1 hero cost 20, got %d", HeroLevelCost(1))
	}
	if HeroAscensionShardCost(2) != 50 {
		t.Fatalf("expected ascension 2 shard cost 50, got %d", HeroAscensionShardCost(2))
	}
	if cost, ok := EquipmentLevelCost(EquipmentWeapon, 1); !ok || cost != 115 {
		t.Fatalf("expected weapon level 1 cost 115, got %d ok=%v", cost, ok)
	}
	if cost := AccessoryLevelCost(StarterGearDropAccessoryID, 0); cost != 35 {
		t.Fatalf("expected accessory level cost 35, got %d", cost)
	}
	accessory, ok := AccessoryDefinitionByID(StarterGearDropAccessoryID)
	if !ok || accessory.SlotID != "earrings" || accessory.RarityID != "r0" || accessory.FuseTargetID != "accessory_earrings_r1" {
		t.Fatalf("unexpected starter accessory definition: %#v ok=%v", accessory, ok)
	}
	rarity, ok := AccessoryRarityDefinitionByID("r0")
	if !ok || rarity.MaxLevel != 20 || rarity.FuseCopyCost != 3 {
		t.Fatalf("unexpected r0 rarity definition: %#v ok=%v", rarity, ok)
	}
	if cost, ok := SummonCost(BannerHeroShardStandard); !ok || cost != 35 {
		t.Fatalf("expected summon cost 35, got %d ok=%v", cost, ok)
	}
	if drop, ok := SummonShardReward(BannerHeroShardStandard, 2); !ok || drop.HeroID != "hero_cyra" || drop.Shards != 1 {
		t.Fatalf("expected summon drop hero_cyra x1, got %#v ok=%v", drop, ok)
	}
	if reward := GearDungeonReward(); reward.RewardID != RewardGearDrop {
		t.Fatalf("expected gear reward id %s, got %#v", RewardGearDrop, reward)
	}
}

func TestDailyMissionDefinitions(t *testing.T) {
	reward, ok := DailyMissionReward("daily_stage_clears_3")
	if !ok {
		t.Fatal("expected daily_stage_clears_3 definition")
	}
	if reward.Gold != 70 || reward.Gems != 10 || reward.MythEssence != 110 || reward.PassXP != 40 {
		t.Fatalf("unexpected daily reward: %#v", reward)
	}
	if _, ok := DailyMissionReward("daily_fake_claim"); ok {
		t.Fatal("unknown daily mission should not have a reward")
	}
}

func TestBattlePassRewardDefinitions(t *testing.T) {
	requiredXP, ok := BattlePassRequiredXP("mission_track_reward_04")
	if !ok || requiredXP != 180 {
		t.Fatalf("expected mission_track_reward_04 to require 180 XP, got %d ok=%v", requiredXP, ok)
	}

	reward, ok := BattlePassReward("mission_track_reward_04")
	if !ok {
		t.Fatal("expected mission_track_reward_04 reward")
	}
	if reward.Gold != 225 || reward.Gems != 25 || reward.MythEssence != 180 {
		t.Fatalf("unexpected battle pass reward: %#v", reward)
	}
	if _, ok := BattlePassReward("mission_track_fake"); ok {
		t.Fatal("unknown battle pass reward should not exist")
	}
}
