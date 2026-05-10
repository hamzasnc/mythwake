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

func TestCampaignMilestoneReward(t *testing.T) {
	reward := CampaignReward(5)

	if reward.MythEssence != 27 || reward.Gems != 17 || reward.PassXP != 25 {
		t.Fatalf("unexpected stage 5 reward: %#v", reward)
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
	if cost, ok := SummonCost(BannerHeroShardStandard); !ok || cost != 35 {
		t.Fatalf("expected summon cost 35, got %d ok=%v", cost, ok)
	}
	if reward := GearDungeonReward(); reward.RewardID != RewardGearDrop {
		t.Fatalf("expected gear reward id %s, got %#v", RewardGearDrop, reward)
	}
}
