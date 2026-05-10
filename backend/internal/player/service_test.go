package player

import (
	"context"
	"testing"
)

func TestServicePersistsSuccessfulActionWhenStoreIsAttached(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.FightCampaign()
	if !result.Success {
		t.Fatalf("expected campaign fight to succeed, got %#v", result)
	}

	if store.saved.PlayerState.CampaignStage != 2 {
		t.Fatalf("expected saved campaign stage 2, got %d", store.saved.PlayerState.CampaignStage)
	}
}

func TestAccessoryFuseTargetUsesNextRarityID(t *testing.T) {
	target, ok := accessoryFuseTarget("accessory_earrings_r0")
	if !ok {
		t.Fatal("expected r0 accessory to have a fuse target")
	}

	if target != "accessory_earrings_r1" {
		t.Fatalf("expected accessory_earrings_r1, got %s", target)
	}
}

func TestAccessoryFuseTargetStopsAtR4(t *testing.T) {
	if target, ok := accessoryFuseTarget("accessory_earrings_r4"); ok {
		t.Fatalf("expected r4 accessory to have no fuse target, got %s", target)
	}
}

func TestSummonCountPersistsAfterPull(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.PullSummon(heroBannerID)
	if !result.Success {
		t.Fatalf("expected summon to succeed, got %#v", result)
	}

	if store.saved.SummonCount != 1 {
		t.Fatalf("expected saved summon count 1, got %d", store.saved.SummonCount)
	}
}

func TestMissionClaimsPersist(t *testing.T) {
	store := &fakeStateStore{}
	service := NewService()

	if err := service.UseStateStore(context.Background(), store); err != nil {
		t.Fatalf("attach store: %v", err)
	}

	result := service.ClaimDailyMission("daily_battles")
	if !result.Success {
		t.Fatalf("expected daily claim to succeed, got %#v", result)
	}

	if !store.saved.ClaimedDaily["daily_battles"] {
		t.Fatalf("expected daily_battles claim to be saved")
	}
}

type fakeStateStore struct {
	saved PersistentState
}

func (store *fakeStateStore) LoadState(context.Context, string) (PersistentState, bool, error) {
	return PersistentState{}, false, nil
}

func (store *fakeStateStore) SaveState(_ context.Context, _ string, state PersistentState, _ StateSaveSource) error {
	store.saved = state
	return nil
}
