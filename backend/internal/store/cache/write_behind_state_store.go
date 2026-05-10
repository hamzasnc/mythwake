package cache

import (
	"context"
	"errors"
	"fmt"
	"io"
	"log"
	"sync"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/player"
)

const seedActionID = "player_state_seed"

type Config struct {
	FlushInterval time.Duration
	FlushTimeout  time.Duration
}

type Stats struct {
	DirtyPlayers  int
	QueuedSaves   int64
	FlushedSaves  int64
	FailedFlushes int64
	LastFlushAt   time.Time
	LastError     string
}

type WriteBehindStateStore struct {
	base   player.StateStore
	config Config
	logger *log.Logger

	mu            sync.Mutex
	dirty         map[string]queuedState
	nextSequence  uint64
	queuedSaves   int64
	flushedSaves  int64
	failedFlushes int64
	lastFlushAt   time.Time
	lastError     string

	stopOnce sync.Once
	stop     chan struct{}
	done     chan struct{}
}

type queuedState struct {
	state       player.PersistentState
	source      player.StateSaveSource
	sequence    uint64
	queuedAt    time.Time
	updatedAt   time.Time
	updateCount int
}

func NewWriteBehindStateStore(base player.StateStore, config Config, logger *log.Logger) *WriteBehindStateStore {
	if logger == nil {
		logger = log.New(io.Discard, "", 0)
	}

	store := &WriteBehindStateStore{
		base:   base,
		config: withDefaults(config),
		logger: logger,
		dirty:  map[string]queuedState{},
		stop:   make(chan struct{}),
		done:   make(chan struct{}),
	}

	go store.run()

	return store
}

func (store *WriteBehindStateStore) LoadState(ctx context.Context, playerID string) (player.PersistentState, bool, error) {
	return store.base.LoadState(ctx, playerID)
}

func (store *WriteBehindStateStore) SaveState(ctx context.Context, playerID string, state player.PersistentState, source player.StateSaveSource) error {
	if err := ctx.Err(); err != nil {
		return err
	}
	if playerID == "" {
		return errors.New("player id is required")
	}

	state = player.ClonePersistentState(state)
	if source.ActionID == seedActionID {
		return store.base.SaveState(ctx, playerID, state, source)
	}

	now := time.Now().UTC()

	store.mu.Lock()
	defer store.mu.Unlock()

	queued := store.dirty[playerID]
	if queued.queuedAt.IsZero() {
		queued.queuedAt = now
	}

	store.nextSequence++
	queued.state = state
	queued.source = source
	queued.sequence = store.nextSequence
	queued.updatedAt = now
	queued.updateCount++
	store.queuedSaves++
	store.dirty[playerID] = queued

	return nil
}

func (store *WriteBehindStateStore) Flush(ctx context.Context) error {
	dirty := store.snapshotDirty()
	if len(dirty) == 0 {
		return nil
	}

	var flushError error
	for playerID, queued := range dirty {
		if err := ctx.Err(); err != nil {
			return errors.Join(flushError, err)
		}

		saveContext := ctx
		cancel := func() {}
		if store.config.FlushTimeout > 0 {
			saveContext, cancel = context.WithTimeout(ctx, store.config.FlushTimeout)
		}

		err := store.base.SaveState(saveContext, playerID, queued.state, batchedSource(queued))
		cancel()
		if err != nil {
			store.recordFlushError(err)
			flushError = errors.Join(flushError, fmt.Errorf("flush player %s: %w", playerID, err))
			continue
		}

		store.recordFlushSuccess(playerID, queued.sequence)
	}

	return flushError
}

func (store *WriteBehindStateStore) Close(ctx context.Context) error {
	var stopError error
	store.stopOnce.Do(func() {
		close(store.stop)
		select {
		case <-store.done:
		case <-ctx.Done():
			stopError = ctx.Err()
		}
	})
	if stopError != nil {
		return stopError
	}

	return store.Flush(ctx)
}

func (store *WriteBehindStateStore) Stats() Stats {
	store.mu.Lock()
	defer store.mu.Unlock()

	return Stats{
		DirtyPlayers:  len(store.dirty),
		QueuedSaves:   store.queuedSaves,
		FlushedSaves:  store.flushedSaves,
		FailedFlushes: store.failedFlushes,
		LastFlushAt:   store.lastFlushAt,
		LastError:     store.lastError,
	}
}

func (store *WriteBehindStateStore) run() {
	ticker := time.NewTicker(store.config.FlushInterval)
	defer ticker.Stop()
	defer close(store.done)

	for {
		select {
		case <-ticker.C:
			ctx, cancel := context.WithTimeout(context.Background(), store.config.FlushTimeout)
			if err := store.Flush(ctx); err != nil {
				store.logger.Printf("state cache flush failed: %v", err)
			}
			cancel()
		case <-store.stop:
			return
		}
	}
}

func (store *WriteBehindStateStore) snapshotDirty() map[string]queuedState {
	store.mu.Lock()
	defer store.mu.Unlock()

	snapshot := make(map[string]queuedState, len(store.dirty))
	for playerID, queued := range store.dirty {
		snapshot[playerID] = queued
	}

	return snapshot
}

func (store *WriteBehindStateStore) recordFlushSuccess(playerID string, sequence uint64) {
	store.mu.Lock()
	defer store.mu.Unlock()

	if current, ok := store.dirty[playerID]; ok && current.sequence == sequence {
		delete(store.dirty, playerID)
	}
	store.flushedSaves++
	store.lastFlushAt = time.Now().UTC()
	store.lastError = ""
}

func (store *WriteBehindStateStore) recordFlushError(err error) {
	store.mu.Lock()
	defer store.mu.Unlock()

	store.failedFlushes++
	store.lastError = err.Error()
}

func withDefaults(config Config) Config {
	if config.FlushInterval <= 0 {
		config.FlushInterval = 30 * time.Second
	}
	if config.FlushTimeout <= 0 {
		config.FlushTimeout = 5 * time.Second
	}

	return config
}

func batchedSource(queued queuedState) player.StateSaveSource {
	source := queued.source
	if source.ActionID == "" {
		source.ActionID = "state_cache_flush"
	}
	if queued.updateCount > 1 {
		source.ActionID = fmt.Sprintf("state_cache_flush:%d:%s", queued.updateCount, source.ActionID)
	}

	return source
}
