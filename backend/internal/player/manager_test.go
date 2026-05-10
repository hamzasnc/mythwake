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

	store.saved = map[string]PersistentState{}
	store.flushCount = 0
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

func TestManagerFlushPlayerIfLoadedOnlyFlushesTarget(t *testing.T) {
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

	store.saved = map[string]PersistentState{}
	store.flushCount = 0
	first.state.Gold = 10
	second.state.Gold = 20

	flushed, err := manager.FlushPlayerIfLoaded(context.Background(), "player-b")
	if err != nil {
		t.Fatalf("flush player: %v", err)
	}
	if !flushed {
		t.Fatal("expected loaded player to flush")
	}
	if _, ok := store.saved["player-a"]; ok {
		t.Fatal("expected player-a to remain unflushed")
	}
	if store.saved["player-b"].PlayerState.Gold != 20 {
		t.Fatalf("expected player-b gold 20, got %d", store.saved["player-b"].PlayerState.Gold)
	}
}

func TestManagerFlushPlayerIfLoadedSkipsColdPlayer(t *testing.T) {
	manager := NewManager(nil)

	flushed, err := manager.FlushPlayerIfLoaded(context.Background(), "player-cold")
	if err != nil {
		t.Fatalf("flush cold player: %v", err)
	}
	if flushed {
		t.Fatal("expected cold player to be skipped")
	}
}

func TestManagerInjectsBalanceCatalogIntoServices(t *testing.T) {
	manager := NewManager(nil, WithBalanceCatalog(expensiveSummonCatalog{}))
	service, err := manager.ServiceForPlayer(context.Background(), "catalog-player")
	if err != nil {
		t.Fatalf("service for player: %v", err)
	}

	service.state.Gems = 998
	result := service.PullSummon(heroBannerID)
	if result.Success || result.ErrorCode != "insufficient_currency" {
		t.Fatalf("expected injected summon cost to block pull, got %#v", result)
	}

	service.state.Gems = 999
	result = service.PullSummon(heroBannerID)
	if !result.Success {
		t.Fatalf("expected exact injected summon cost to pass, got %#v", result)
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

type expensiveSummonCatalog struct {
	StaticBalanceCatalog
}

func (expensiveSummonCatalog) SummonCost(string) (int, bool) {
	return 999, true
}
