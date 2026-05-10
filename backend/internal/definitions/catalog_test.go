package definitions

import (
	"testing"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
)

func TestSnapshotIncludesCoreDefinitionSets(t *testing.T) {
	snapshot := Snapshot("test-version")

	if snapshot.SchemaVersion != SchemaVersion {
		t.Fatalf("expected schema version %d, got %d", SchemaVersion, snapshot.SchemaVersion)
	}
	if snapshot.APIVersion != "test-version" {
		t.Fatalf("expected API version test-version, got %s", snapshot.APIVersion)
	}
	if snapshot.ContentHash == "" {
		t.Fatal("expected content hash")
	}
	if len(snapshot.AuthProviders) != 4 {
		t.Fatalf("expected 4 auth provider definitions, got %#v", snapshot.AuthProviders)
	}
	if len(snapshot.Currencies) != 5 {
		t.Fatalf("expected 5 currency definitions, got %#v", snapshot.Currencies)
	}
	if len(snapshot.Heroes) != 5 {
		t.Fatalf("expected 5 hero definitions, got %#v", snapshot.Heroes)
	}
	if snapshot.Heroes[0].MaxLevel != 100 || snapshot.Heroes[0].BaseAttack <= 0 || snapshot.Heroes[0].BaseHealth <= 0 {
		t.Fatalf("expected hero stat definitions, got %#v", snapshot.Heroes[0])
	}
	if len(snapshot.Equipment) != 2 {
		t.Fatalf("expected 2 equipment definitions, got %#v", snapshot.Equipment)
	}
	if snapshot.Equipment[0].MaxLevel != 100 || snapshot.Equipment[0].AttackPerLevel <= 0 {
		t.Fatalf("expected weapon stat definition, got %#v", snapshot.Equipment[0])
	}
	if len(snapshot.Rewards) != 71 {
		t.Fatalf("expected 71 reward definitions, got %d", len(snapshot.Rewards))
	}
	if len(snapshot.Campaigns) != 1 {
		t.Fatalf("expected 1 campaign definition, got %#v", snapshot.Campaigns)
	}
	if len(snapshot.CampaignStages) != 60 {
		t.Fatalf("expected 60 campaign stage definitions, got %d", len(snapshot.CampaignStages))
	}
	if len(snapshot.Dungeons) != 3 {
		t.Fatalf("expected 3 dungeons, got %#v", snapshot.Dungeons)
	}
	if len(snapshot.AccessorySlots) != 5 {
		t.Fatalf("expected 5 accessory slot definitions, got %#v", snapshot.AccessorySlots)
	}
	if len(snapshot.AccessoryRarities) != 5 {
		t.Fatalf("expected 5 accessory rarity definitions, got %#v", snapshot.AccessoryRarities)
	}
	if len(snapshot.Accessories) != 25 {
		t.Fatalf("expected 25 accessory definitions, got %d", len(snapshot.Accessories))
	}
	if len(snapshot.ProgressionCosts) == 0 {
		t.Fatal("expected progression cost definitions")
	}
	if len(snapshot.SummonBanners) != 1 || len(snapshot.SummonBanners[0].ShardDrops) != 5 {
		t.Fatalf("expected standard summon banner with 5 shard drops, got %#v", snapshot.SummonBanners)
	}
	if len(snapshot.DailyMissions) != 3 {
		t.Fatalf("expected 3 daily missions, got %#v", snapshot.DailyMissions)
	}
	if len(snapshot.BattlePassRewards) != 5 {
		t.Fatalf("expected 5 battle pass rewards, got %#v", snapshot.BattlePassRewards)
	}
	if !hasAction(snapshot, gameplay.ActionCampaignFight) {
		t.Fatalf("expected action catalog to include %s", gameplay.ActionCampaignFight)
	}
	if !hasAction(snapshot, gameplay.ActionAFKRewardClaim) {
		t.Fatalf("expected action catalog to include %s", gameplay.ActionAFKRewardClaim)
	}
}

func TestSnapshotContentHashIsStableForSameVersion(t *testing.T) {
	first := Snapshot("test-version")
	second := Snapshot("test-version")

	if first.ContentHash != second.ContentHash {
		t.Fatalf("expected stable content hash, got %s and %s", first.ContentHash, second.ContentHash)
	}
	if ETag(first) != ETag(second) {
		t.Fatalf("expected stable etag, got %s and %s", ETag(first), ETag(second))
	}
}

func TestSnapshotContentHashChangesWithAPIVersion(t *testing.T) {
	first := Snapshot("test-version-a")
	second := Snapshot("test-version-b")

	if first.ContentHash == second.ContentHash {
		t.Fatalf("expected API version to affect content hash, both were %s", first.ContentHash)
	}
}

func TestSnapshotCarriesAuthoritativeRewardValues(t *testing.T) {
	snapshot := Snapshot("test-version")

	var found bool
	for _, mission := range snapshot.DailyMissions {
		if mission.MissionID != "daily_stage_clears_3" {
			continue
		}
		found = true
		if mission.Reward.Gold != 70 || mission.Reward.Gems != 10 || mission.Reward.MythEssence != 110 || mission.Reward.PassXP != 40 {
			t.Fatalf("unexpected daily_stage_clears_3 reward: %#v", mission.Reward)
		}
	}
	if !found {
		t.Fatal("expected daily_stage_clears_3 in snapshot")
	}
}

func TestSnapshotCarriesCampaignAndAccessoryDefinitionData(t *testing.T) {
	snapshot := Snapshot("test-version")

	if snapshot.CampaignStages[4].RewardID != "reward_campaign_stage_005" || snapshot.CampaignStages[4].RequiredPower != 320 {
		t.Fatalf("unexpected campaign stage 5 definition: %#v", snapshot.CampaignStages[4])
	}
	if snapshot.CampaignStages[0].EnemyMaxHP != 458 || snapshot.CampaignStages[0].EnemyDamage != 24 || snapshot.CampaignStages[0].MaxCombatSeconds != 30 {
		t.Fatalf("unexpected campaign stage 1 combat definition: %#v", snapshot.CampaignStages[0])
	}

	var foundGoldDungeon bool
	for _, definition := range snapshot.Dungeons {
		if definition.DungeonID != "gold_dungeon" {
			continue
		}
		foundGoldDungeon = true
		if definition.EnemyBaseHP != 220 || definition.EnemyHPPerFloor != 95 || definition.EnemyDamagePowerDivisor != 48 || definition.MaxCombatSeconds != 30 {
			t.Fatalf("unexpected gold dungeon combat definition: %#v", definition)
		}
	}
	if !foundGoldDungeon {
		t.Fatal("expected gold_dungeon definition")
	}

	var found bool
	for _, definition := range snapshot.Accessories {
		if definition.AccessoryID != "accessory_earrings_r0" {
			continue
		}
		found = true
		if definition.SlotID != "earrings" || definition.RarityID != "r0" || definition.FuseTargetID != "accessory_earrings_r1" {
			t.Fatalf("unexpected accessory definition: %#v", definition)
		}
	}
	if !found {
		t.Fatal("expected accessory_earrings_r0 definition")
	}
}

func hasAction(snapshot api.DefinitionSnapshot, actionID string) bool {
	for _, action := range snapshot.GameplayActions {
		if action.ActionID == actionID {
			return true
		}
	}
	return false
}
