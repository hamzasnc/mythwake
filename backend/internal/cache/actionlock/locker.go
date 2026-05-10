package actionlock

import (
	"context"
	"crypto/rand"
	"encoding/base64"
	"fmt"
	"sync"
	"time"
)

type ReleaseFunc func(ctx context.Context) error

func (release ReleaseFunc) Release(ctx context.Context) error {
	return release(ctx)
}

type Lock interface {
	Release(ctx context.Context) error
}

type Locker interface {
	Acquire(ctx context.Context, key string, ttl time.Duration) (Lock, bool, error)
}

type MemoryLocker struct {
	mu    sync.Mutex
	now   func() time.Time
	locks map[string]memoryLock
}

type memoryLock struct {
	token     string
	expiresAt time.Time
}

func NewMemoryLocker() *MemoryLocker {
	return &MemoryLocker{
		now:   time.Now,
		locks: map[string]memoryLock{},
	}
}

func (locker *MemoryLocker) Acquire(_ context.Context, key string, ttl time.Duration) (Lock, bool, error) {
	if ttl <= 0 {
		ttl = 5 * time.Second
	}

	token, err := randomToken()
	if err != nil {
		return nil, false, err
	}

	now := locker.now()
	locker.mu.Lock()
	defer locker.mu.Unlock()

	current, exists := locker.locks[key]
	if exists && current.expiresAt.After(now) {
		return nil, false, nil
	}

	locker.locks[key] = memoryLock{
		token:     token,
		expiresAt: now.Add(ttl),
	}

	return ReleaseFunc(func(context.Context) error {
		locker.mu.Lock()
		defer locker.mu.Unlock()

		current, exists := locker.locks[key]
		if exists && current.token == token {
			delete(locker.locks, key)
		}

		return nil
	}), true, nil
}

func randomToken() (string, error) {
	bytes := make([]byte, 16)
	if _, err := rand.Read(bytes); err != nil {
		return "", fmt.Errorf("generate lock token: %w", err)
	}

	return base64.RawURLEncoding.EncodeToString(bytes), nil
}
