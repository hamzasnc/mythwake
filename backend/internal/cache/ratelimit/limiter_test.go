package ratelimit

import (
	"context"
	"testing"
	"time"
)

func TestMemoryLimiterBlocksUntilWindowReset(t *testing.T) {
	now := time.Date(2026, 5, 10, 12, 0, 0, 0, time.UTC)
	limiter := NewMemoryLimiter(WithClock(func() time.Time { return now }))
	ctx := context.Background()

	for i := 0; i < 2; i++ {
		decision, err := limiter.Allow(ctx, "auth:ip:127.0.0.1", 2, time.Minute)
		if err != nil {
			t.Fatalf("allow request %d: %v", i+1, err)
		}
		if !decision.Allowed {
			t.Fatalf("expected request %d to be allowed, got %#v", i+1, decision)
		}
	}

	blocked, err := limiter.Allow(ctx, "auth:ip:127.0.0.1", 2, time.Minute)
	if err != nil {
		t.Fatalf("block request: %v", err)
	}
	if blocked.Allowed || blocked.RetryAfter != time.Minute {
		t.Fatalf("expected third request to be blocked for one minute, got %#v", blocked)
	}

	now = now.Add(time.Minute)
	allowed, err := limiter.Allow(ctx, "auth:ip:127.0.0.1", 2, time.Minute)
	if err != nil {
		t.Fatalf("allow after reset: %v", err)
	}
	if !allowed.Allowed {
		t.Fatalf("expected request after reset to be allowed, got %#v", allowed)
	}
}
