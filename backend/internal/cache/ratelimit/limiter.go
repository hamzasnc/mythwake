package ratelimit

import (
	"context"
	"sync"
	"time"
)

type Decision struct {
	Allowed    bool
	RetryAfter time.Duration
}

type Limiter interface {
	Allow(ctx context.Context, key string, limit int, window time.Duration) (Decision, error)
}

type MemoryLimiterOption func(*MemoryLimiter)

type MemoryLimiter struct {
	mu      sync.Mutex
	now     func() time.Time
	entries map[string]entry
}

type entry struct {
	count   int
	resetAt time.Time
}

func NewMemoryLimiter(options ...MemoryLimiterOption) *MemoryLimiter {
	limiter := &MemoryLimiter{
		now:     time.Now,
		entries: map[string]entry{},
	}
	for _, option := range options {
		option(limiter)
	}

	return limiter
}

func WithClock(now func() time.Time) MemoryLimiterOption {
	return func(limiter *MemoryLimiter) {
		if now != nil {
			limiter.now = now
		}
	}
}

func (limiter *MemoryLimiter) Allow(_ context.Context, key string, limit int, window time.Duration) (Decision, error) {
	if limit <= 0 {
		return Decision{Allowed: true}, nil
	}
	if window <= 0 {
		window = time.Minute
	}

	now := limiter.now()
	limiter.mu.Lock()
	defer limiter.mu.Unlock()

	current := limiter.entries[key]
	if current.resetAt.IsZero() || !current.resetAt.After(now) {
		current = entry{resetAt: now.Add(window)}
	}

	if current.count >= limit {
		return Decision{Allowed: false, RetryAfter: current.resetAt.Sub(now)}, nil
	}

	current.count++
	limiter.entries[key] = current
	return Decision{Allowed: true}, nil
}
