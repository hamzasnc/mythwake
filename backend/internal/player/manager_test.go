package player

import (
	"context"
	"testing"
)

func TestManagerReturnsStableServicePerPlayer(t *testing.T) {
	manager := NewManager(nil)

	first, err := manager.ServiceForPlayer(context.Background(), "player-a")
	if err != nil {
		t.Fatalf("first service: %v", err)
	}
	second, err := manager.ServiceForPlayer(context.Background(), "player-a")
	if err != nil {
		t.Fatalf("second service: %v", err)
	}
	other, err := manager.ServiceForPlayer(context.Background(), "player-b")
	if err != nil {
		t.Fatalf("other service: %v", err)
	}

	if first != second {
		t.Fatal("expected same service instance for same player")
	}
	if first == other {
		t.Fatal("expected different service instance for different player")
	}
	if manager.LoadedPlayerCount() != 2 {
		t.Fatalf("expected two loaded players, got %d", manager.LoadedPlayerCount())
	}
}

func TestManagerFlushAllFlushesLoadedPlayers(t *testing.T) {
	store := &managerFlushStore{saved: map[string]PersistentState{}}
	manager := NewManager(store)

	first, err := manager.ServiceForPlayer(context.Background(), "player-a")
	if err != nil {
		t.Fatalf("first service: %v", err)
	}
	second, err := manager.ServiceForPlayer(context.Background(), "player-b")
	if err != nil {
		t.Fatalf("second service: %v", err)
	}

	first.state.Gold = 10
	second.state.Gold = 20

	if err := manager.FlushAll(context.Background()); err != nil {
		t.Fatalf("flush all: %v", err)
	}

	if store.saved["player-a"].PlayerState.Gold != 10 {
		t.Fatalf("expected player-a gold 10, got %d", store.saved["player-a"].PlayerState.Gold)
	}
	if store.saved["player-b"].PlayerState.Gold != 20 {
		t.Fatalf("expected player-b gold 20, got %d", store.saved["player-b"].PlayerState.Gold)
	}
	if store.flushCount != 2 {
		t.Fatalf("expected two service flush calls, got %d", store.flushCount)
	}
}

type managerFlushStore struct {
	saved      map[string]PersistentState
	flushCount int
}

func (store *managerFlushStore) LoadState(context.Context, string) (PersistentState, bool, error) {
	return PersistentState{}, false, nil
}

func (store *managerFlushStore) SaveState(_ context.Context, playerID string, state PersistentState, _ StateSaveSource) error {
	store.saved[playerID] = state
	return nil
}

func (store *managerFlushStore) Flush(context.Context) error {
	store.flushCount++
	return nil
}
