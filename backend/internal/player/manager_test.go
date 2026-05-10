package player

import (
	"context"
	"testing"
	"time"
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

func TestManagerResetPlayerReplacesLoadedService(t *testing.T) {
	store := &managerFlushStore{saved: map[string]PersistentState{}}
	manager := NewManager(store)

	service, err := manager.ServiceForPlayer(context.Background(), "player-reset")
	if err != nil {
		t.Fatalf("service for player: %v", err)
	}
	service.state.Gold = 999

	reset, err := manager.ResetPlayer(context.Background(), "player-reset")
	if err != nil {
		t.Fatalf("reset player: %v", err)
	}

	if reset == service {
		t.Fatal("expected reset to replace the hot service")
	}
	if reset.GetState().Gold != 0 || reset.GetState().Gems != 35 {
		t.Fatalf("expected fresh player state after reset, got %#v", reset.GetState())
	}
	if store.resetPlayerID != "player-reset" {
		t.Fatalf("expected reset store call, got %q", store.resetPlayerID)
	}
}

func TestManagerFlushIdleFlushesAndUnloadsExpiredPlayers(t *testing.T) {
	store := &managerFlushStore{saved: map[string]PersistentState{}}
	now := time.Date(2026, 5, 10, 12, 0, 0, 0, time.UTC)
	manager := NewManager(store, WithClock(func() time.Time { return now }))

	expired, err := manager.ServiceForPlayer(context.Background(), "player-expired")
	if err != nil {
		t.Fatalf("expired service: %v", err)
	}
	active, err := manager.ServiceForPlayer(context.Background(), "player-active")
	if err != nil {
		t.Fatalf("active service: %v", err)
	}

	store.saved = map[string]PersistentState{}
	store.flushCount = 0
	expired.state.Gold = 42
	active.state.Gold = 99

	now = now.Add(20 * time.Minute)
	if _, err := manager.ServiceForPlayer(context.Background(), "player-active"); err != nil {
		t.Fatalf("touch active service: %v", err)
	}

	unloaded, err := manager.FlushIdle(context.Background(), 10*time.Minute)
	if err != nil {
		t.Fatalf("flush idle: %v", err)
	}

	if unloaded != 1 {
		t.Fatalf("expected one unloaded player, got %d", unloaded)
	}
	if manager.LoadedPlayerCount() != 1 {
		t.Fatalf("expected one loaded player after idle flush, got %d", manager.LoadedPlayerCount())
	}
	if store.saved["player-expired"].PlayerState.Gold != 42 {
		t.Fatalf("expected expired player gold 42, got %d", store.saved["player-expired"].PlayerState.Gold)
	}
	if _, ok := store.saved["player-active"]; ok {
		t.Fatal("expected active player to remain unflushed")
	}

	reloaded, err := manager.ServiceForPlayer(context.Background(), "player-expired")
	if err != nil {
		t.Fatalf("reload expired service: %v", err)
	}
	if reloaded == expired {
		t.Fatal("expected expired player to be removed from hot manager cache")
	}
}

func TestManagerFlushIdleKeepsPlayerTouchedDuringFlush(t *testing.T) {
	store := &managerFlushStore{saved: map[string]PersistentState{}}
	now := time.Date(2026, 5, 10, 12, 0, 0, 0, time.UTC)
	manager := NewManager(store, WithClock(func() time.Time { return now }))

	service, err := manager.ServiceForPlayer(context.Background(), "player-hot")
	if err != nil {
		t.Fatalf("service for player: %v", err)
	}

	store.saved = map[string]PersistentState{}
	store.flushCount = 0
	service.state.Gold = 42

	now = now.Add(20 * time.Minute)
	store.onSave = func() {
		now = now.Add(time.Minute)
		if _, err := manager.ServiceForPlayer(context.Background(), "player-hot"); err != nil {
			t.Fatalf("touch during flush: %v", err)
		}
	}

	unloaded, err := manager.FlushIdle(context.Background(), 10*time.Minute)
	if err != nil {
		t.Fatalf("flush idle: %v", err)
	}

	if unloaded != 0 {
		t.Fatalf("expected touched player to stay loaded, got %d unloads", unloaded)
	}
	if manager.LoadedPlayerCount() != 1 {
		t.Fatalf("expected touched player to remain loaded, got %d loaded players", manager.LoadedPlayerCount())
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
	saved         map[string]PersistentState
	flushCount    int
	resetPlayerID string
	onSave        func()
}

func (store *managerFlushStore) LoadState(context.Context, string) (PersistentState, bool, error) {
	return PersistentState{}, false, nil
}

func (store *managerFlushStore) SaveState(_ context.Context, playerID string, state PersistentState, _ StateSaveSource) error {
	store.saved[playerID] = state
	if store.onSave != nil {
		store.onSave()
	}
	return nil
}

func (store *managerFlushStore) Flush(context.Context) error {
	store.flushCount++
	return nil
}

func (store *managerFlushStore) ResetState(_ context.Context, playerID string) error {
	store.resetPlayerID = playerID
	delete(store.saved, playerID)
	return nil
}

type expensiveSummonCatalog struct {
	StaticBalanceCatalog
}

func (expensiveSummonCatalog) SummonCost(string) (int, bool) {
	return 999, true
}
