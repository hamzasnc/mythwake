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
	if len(snapshot.Dungeons) != 3 {
		t.Fatalf("expected 3 dungeons, got %#v", snapshot.Dungeons)
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

func hasAction(snapshot api.DefinitionSnapshot, actionID string) bool {
	for _, action := range snapshot.GameplayActions {
		if action.ActionID == actionID {
			return true
		}
	}
	return false
}
