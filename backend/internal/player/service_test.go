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

type fakeStateStore struct {
	saved PersistentState
}

func (store *fakeStateStore) LoadState(context.Context, string) (PersistentState, bool, error) {
	return PersistentState{}, false, nil
}

func (store *fakeStateStore) SaveState(_ context.Context, _ string, state PersistentState) error {
	store.saved = state
	return nil
}
