package config

import (
	"testing"
	"time"
)

func TestLoadDefaultsRequireIdempotency(t *testing.T) {
	t.Setenv("MYTHWAKE_REQUIRE_IDEMPOTENCY", "")

	cfg := Load()

	if !cfg.RequireIdempotency {
		t.Fatal("expected idempotency to be required by default")
	}
}

func TestLoadCanDisableIdempotencyForLocalDebug(t *testing.T) {
	t.Setenv("MYTHWAKE_REQUIRE_IDEMPOTENCY", "false")

	cfg := Load()

	if cfg.RequireIdempotency {
		t.Fatal("expected idempotency requirement to be disabled")
	}
}

func TestLoadFallsBackForInvalidIdempotencyFlag(t *testing.T) {
	t.Setenv("MYTHWAKE_REQUIRE_IDEMPOTENCY", "maybe")

	cfg := Load()

	if !cfg.RequireIdempotency {
		t.Fatal("expected invalid idempotency flag to fall back to true")
	}
}

func TestLoadSessionCacheDurations(t *testing.T) {
	t.Setenv("MYTHWAKE_SESSION_CACHE_STORE", "memory")
	t.Setenv("MYTHWAKE_SESSION_CACHE_TTL", "45s")
	t.Setenv("MYTHWAKE_SESSION_TOUCH_WINDOW", "2m")

	cfg := Load()

	if cfg.SessionCacheStore != CacheStoreMemory {
		t.Fatalf("expected memory session cache, got %s", cfg.SessionCacheStore)
	}
	if cfg.SessionCacheTTL != 45*time.Second {
		t.Fatalf("expected session cache ttl 45s, got %s", cfg.SessionCacheTTL)
	}
	if cfg.SessionTouchWindow != 2*time.Minute {
		t.Fatalf("expected session touch window 2m, got %s", cfg.SessionTouchWindow)
	}
}

func TestLoadRateLimitSettings(t *testing.T) {
	t.Setenv("MYTHWAKE_RATE_LIMIT_STORE", "memory")
	t.Setenv("MYTHWAKE_RATE_LIMIT_ENABLED", "false")
	t.Setenv("MYTHWAKE_RATE_LIMIT_WINDOW", "15s")
	t.Setenv("MYTHWAKE_RATE_LIMIT_AUTH", "7")
	t.Setenv("MYTHWAKE_RATE_LIMIT_GAMEPLAY", "9")

	cfg := Load()

	if cfg.RateLimitStore != CacheStoreMemory {
		t.Fatalf("expected memory rate limit store, got %s", cfg.RateLimitStore)
	}
	if cfg.RateLimitEnabled {
		t.Fatal("expected rate limiting to be disabled")
	}
	if cfg.RateLimitWindow != 15*time.Second {
		t.Fatalf("expected rate limit window 15s, got %s", cfg.RateLimitWindow)
	}
	if cfg.RateLimitAuth != 7 {
		t.Fatalf("expected auth limit 7, got %d", cfg.RateLimitAuth)
	}
	if cfg.RateLimitGameplay != 9 {
		t.Fatalf("expected gameplay limit 9, got %d", cfg.RateLimitGameplay)
	}
}

func TestLoadFallsBackToMemoryCacheStores(t *testing.T) {
	t.Setenv("MYTHWAKE_SESSION_CACHE_STORE", "redis")
	t.Setenv("MYTHWAKE_RATE_LIMIT_STORE", "bad-store")

	cfg := Load()

	if cfg.SessionCacheStore != CacheStoreMemory || cfg.RateLimitStore != CacheStoreMemory {
		t.Fatalf("expected unsupported cache stores to fall back to memory, got session=%s rate=%s", cfg.SessionCacheStore, cfg.RateLimitStore)
	}
}
