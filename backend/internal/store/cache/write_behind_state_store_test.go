package cache

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
	"github.com/hamzasnc/mythwake/backend/internal/player"
)

func TestWriteBehindStateStoreFlushesLatestStateOnly(t *testing.T) {
	base := &fakeStateStore{}
	store := NewWriteBehindStateStore(base, Config{FlushInterval: time.Hour, FlushTimeout: time.Second, WriteBehind: true}, nil)
	defer closeStore(t, store)

	if err := store.SaveState(context.Background(), "player-1", testState(2), idempotentSource(gameplay.ActionCampaignFight, "key-1")); err != nil {
		t.Fatalf("queue first state: %v", err)
	}
	if err := store.SaveState(context.Background(), "player-1", testState(3), idempotentSource(gameplay.ActionGoldDungeonRun, "key-2")); err != nil {
		t.Fatalf("queue second state: %v", err)
	}

	if base.saveCount != 0 {
		t.Fatalf("expected no immediate write, got %d", base.saveCount)
	}
	if base.actionSaveCount != 2 {
		t.Fatalf("expected two immediate action result writes, got %d", base.actionSaveCount)
	}
	if stats := store.Stats(); stats.DirtyPlayers != 1 || stats.QueuedSaves != 2 {
		t.Fatalf("unexpected stats before flush: %#v", stats)
	}

	if err := store.Flush(context.Background()); err != nil {
		t.Fatalf("flush: %v", err)
	}

	if base.saveCount != 1 {
		t.Fatalf("expected one batched write, got %d", base.saveCount)
	}
	if base.saved.PlayerState.CampaignStage != 3 {
		t.Fatalf("expected latest campaign stage 3, got %d", base.saved.PlayerState.CampaignStage)
	}
	if base.source.ActionID != gameplay.ActionStateCacheFlush+":2:"+gameplay.ActionGoldDungeonRun {
		t.Fatalf("expected batched source, got %#v", base.source)
	}
	if stats := store.Stats(); stats.DirtyPlayers != 0 || stats.FlushedSaves != 1 {
		t.Fatalf("unexpected stats after flush: %#v", stats)
	}
}

func TestWriteBehindStateStoreWritesSeedImmediately(t *testing.T) {
	base := &fakeStateStore{}
	store := NewWriteBehindStateStore(base, Config{FlushInterval: time.Hour, FlushTimeout: time.Second, WriteBehind: true}, nil)
	defer closeStore(t, store)

	if err := store.SaveState(context.Background(), "player-1", testState(1), player.StateSaveSource{ActionID: gameplay.ActionPlayerStateSeed}); err != nil {
		t.Fatalf("seed save: %v", err)
	}

	if base.saveCount != 1 {
		t.Fatalf("expected immediate seed write, got %d", base.saveCount)
	}
	if stats := store.Stats(); stats.DirtyPlayers != 0 || stats.QueuedSaves != 0 {
		t.Fatalf("expected no dirty state for seed, got %#v", stats)
	}
}

func TestWriteBehindStateStoreKeepsDirtyStateAfterFlushFailure(t *testing.T) {
	base := &fakeStateStore{saveError: errors.New("database unavailable")}
	store := NewWriteBehindStateStore(base, Config{FlushInterval: time.Hour, FlushTimeout: time.Second, WriteBehind: true}, nil)
	defer closeStore(t, store)

	if err := store.SaveState(context.Background(), "player-1", testState(4), idempotentSource(gameplay.ActionCampaignFight, "key-1")); err != nil {
		t.Fatalf("queue state: %v", err)
	}

	if err := store.Flush(context.Background()); err == nil {
		t.Fatal("expected flush failure")
	}

	if stats := store.Stats(); stats.DirtyPlayers != 1 || stats.FailedFlushes != 1 {
		t.Fatalf("expected dirty state to remain, got %#v", stats)
	}

	base.saveError = nil
	if err := store.Flush(context.Background()); err != nil {
		t.Fatalf("retry flush: %v", err)
	}

	if stats := store.Stats(); stats.DirtyPlayers != 0 || stats.FlushedSaves != 1 {
		t.Fatalf("expected retry to clear dirty state, got %#v", stats)
	}
}

func TestWriteBehindStateStoreDefaultsToWriteThroughDurability(t *testing.T) {
	base := &fakeStateStore{}
	store := NewWriteBehindStateStore(base, Config{FlushInterval: time.Hour, FlushTimeout: time.Second}, nil)
	defer closeStore(t, store)

	if err := store.SaveState(context.Background(), "player-1", testState(5), player.StateSaveSource{ActionID: gameplay.ActionSummonPull}); err != nil {
		t.Fatalf("save state: %v", err)
	}

	if base.saveCount != 1 {
		t.Fatalf("expected immediate write-through save, got %d", base.saveCount)
	}
	if base.saved.PlayerState.CampaignStage != 5 {
		t.Fatalf("expected campaign stage 5, got %d", base.saved.PlayerState.CampaignStage)
	}
	if stats := store.Stats(); stats.DirtyPlayers != 0 || stats.QueuedSaves != 0 {
		t.Fatalf("expected no dirty write-behind state, got %#v", stats)
	}
}

func TestWriteBehindStateStoreSavesIdempotentActionResultAndQueuesState(t *testing.T) {
	base := &fakeStateStore{}
	store := NewWriteBehindStateStore(base, Config{FlushInterval: time.Hour, FlushTimeout: time.Second, WriteBehind: true}, nil)
	defer closeStore(t, store)

	err := store.SaveState(context.Background(), "player-1", testState(6), idempotentSource(gameplay.ActionSummonPull, "summon-key-1"))
	if err != nil {
		t.Fatalf("save idempotent state: %v", err)
	}

	if base.actionSaveCount != 1 {
		t.Fatalf("expected immediate action result save, got %d", base.actionSaveCount)
	}
	if base.saveCount != 0 {
		t.Fatalf("expected materialized state to wait for flush, got %d", base.saveCount)
	}
	if stats := store.Stats(); stats.DirtyPlayers != 1 {
		t.Fatalf("expected dirty write-behind state, got %#v", stats)
	}

	if err := store.Flush(context.Background()); err != nil {
		t.Fatalf("flush state: %v", err)
	}
	if base.saveCount != 1 {
		t.Fatalf("expected materialized state flush, got %d", base.saveCount)
	}
	if stats := store.Stats(); stats.DirtyPlayers != 0 {
		t.Fatalf("expected no dirty write-behind state, got %#v", stats)
	}
}

func idempotentSource(actionID string, key string) player.StateSaveSource {
	return player.StateSaveSource{
		ActionID:       actionID,
		IdempotencyKey: key,
		RequestHash:    key + "-hash",
		ActionResult:   &api.ActionResult{Success: true, ActionID: actionID, IdempotencyKey: key},
	}
}

func testState(stage int) player.PersistentState {
	return player.PersistentState{
		PlayerState: api.PlayerState{
			SaveVersion:   1,
			CampaignStage: stage,
		},
		HeroLevels:         map[string]int{"hero_astra": 1},
		HeroShards:         map[string]int{},
		HeroAscensions:     map[string]int{},
		EquipmentLevels:    map[string]int{},
		AccessoryInventory: map[string]int{},
		AccessoryLevels:    map[string]int{},
		EquippedAccessory:  map[string]string{},
		ClaimedDaily:       map[string]bool{},
		ClaimedBattlePass:  map[string]bool{},
	}
}

func closeStore(t *testing.T, store *WriteBehindStateStore) {
	t.Helper()

	ctx, cancel := context.WithTimeout(context.Background(), time.Second)
	defer cancel()
	if err := store.Close(ctx); err != nil {
		t.Fatalf("close store: %v", err)
	}
}

type fakeStateStore struct {
	saveError       error
	actionSaveError error
	saveCount       int
	actionSaveCount int
	saved           player.PersistentState
	source          player.StateSaveSource
}

func (store *fakeStateStore) LoadState(context.Context, string) (player.PersistentState, bool, error) {
	return player.PersistentState{}, false, nil
}

func (store *fakeStateStore) SaveState(_ context.Context, _ string, state player.PersistentState, source player.StateSaveSource) error {
	if store.saveError != nil {
		return store.saveError
	}

	store.saveCount++
	store.saved = state
	store.source = source
	return nil
}

func (store *fakeStateStore) SaveActionResult(_ context.Context, _ string, source player.StateSaveSource) error {
	if store.actionSaveError != nil {
		return store.actionSaveError
	}

	store.actionSaveCount++
	store.source = source
	return nil
}
