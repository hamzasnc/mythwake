package player

import (
	"context"
	"testing"

	"github.com/hamzasnc/mythwake/backend/internal/api"
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

	if store.saved.CampaignStage != 2 {
		t.Fatalf("expected saved campaign stage 2, got %d", store.saved.CampaignStage)
	}
}

type fakeStateStore struct {
	saved api.PlayerState
}

func (store *fakeStateStore) LoadState(context.Context, string) (api.PlayerState, bool, error) {
	return api.PlayerState{}, false, nil
}

func (store *fakeStateStore) SaveState(_ context.Context, _ string, state api.PlayerState) error {
	store.saved = state
	return nil
}
