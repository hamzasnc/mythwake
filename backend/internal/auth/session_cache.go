package auth

import (
	"context"
	"sync"
	"time"
)

type SessionCacheEntry struct {
	Session          Session
	CachedAt         time.Time
	LastStoreTouchAt time.Time
}

type SessionCache interface {
	Load(ctx context.Context, tokenHash string) (SessionCacheEntry, bool, error)
	Store(ctx context.Context, tokenHash string, entry SessionCacheEntry) error
	Delete(ctx context.Context, tokenHash string) error
}

type MemorySessionCache struct {
	mu       sync.Mutex
	sessions map[string]SessionCacheEntry
}

func NewMemorySessionCache() *MemorySessionCache {
	return &MemorySessionCache{sessions: map[string]SessionCacheEntry{}}
}

func (cache *MemorySessionCache) Load(_ context.Context, tokenHash string) (SessionCacheEntry, bool, error) {
	cache.mu.Lock()
	defer cache.mu.Unlock()

	entry, ok := cache.sessions[tokenHash]
	return entry, ok, nil
}

func (cache *MemorySessionCache) Store(_ context.Context, tokenHash string, entry SessionCacheEntry) error {
	cache.mu.Lock()
	defer cache.mu.Unlock()

	cache.sessions[tokenHash] = entry
	return nil
}

func (cache *MemorySessionCache) Delete(_ context.Context, tokenHash string) error {
	cache.mu.Lock()
	defer cache.mu.Unlock()

	delete(cache.sessions, tokenHash)
	return nil
}
