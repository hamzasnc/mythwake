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

func TestLoadUsesRedisStoresWhenRedisAddressIsSet(t *testing.T) {
	t.Setenv("MYTHWAKE_REDIS_ADDR", "localhost:6379")
	t.Setenv("MYTHWAKE_REDIS_PASSWORD", "secret")
	t.Setenv("MYTHWAKE_REDIS_DB", "2")
	t.Setenv("MYTHWAKE_SESSION_CACHE_STORE", "")
	t.Setenv("MYTHWAKE_RATE_LIMIT_STORE", "")

	cfg := Load()

	if cfg.RedisAddr != "localhost:6379" || cfg.RedisPassword != "secret" || cfg.RedisDB != 2 {
		t.Fatalf("expected redis config to load, got %#v", cfg)
	}
	if cfg.SessionCacheStore != CacheStoreRedis || cfg.RateLimitStore != CacheStoreRedis {
		t.Fatalf("expected redis stores when redis addr is set, got session=%s rate=%s", cfg.SessionCacheStore, cfg.RateLimitStore)
	}
	if cfg.PlayerLockStore != CacheStoreRedis {
		t.Fatalf("expected redis player lock store when redis addr is set, got %s", cfg.PlayerLockStore)
	}
}

func TestLoadCanKeepMemoryStoresWithRedisAddress(t *testing.T) {
	t.Setenv("MYTHWAKE_REDIS_ADDR", "localhost:6379")
	t.Setenv("MYTHWAKE_SESSION_CACHE_STORE", "memory")
	t.Setenv("MYTHWAKE_RATE_LIMIT_STORE", "memory")
	t.Setenv("MYTHWAKE_PLAYER_LOCK_STORE", "memory")

	cfg := Load()

	if cfg.SessionCacheStore != CacheStoreMemory || cfg.RateLimitStore != CacheStoreMemory || cfg.PlayerLockStore != CacheStoreMemory {
		t.Fatalf("expected explicit memory stores, got session=%s rate=%s lock=%s", cfg.SessionCacheStore, cfg.RateLimitStore, cfg.PlayerLockStore)
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

func TestLoadPlayerLockSettings(t *testing.T) {
	t.Setenv("MYTHWAKE_PLAYER_LOCK_STORE", "memory")
	t.Setenv("MYTHWAKE_PLAYER_LOCK_TTL", "3s")

	cfg := Load()

	if cfg.PlayerLockStore != CacheStoreMemory {
		t.Fatalf("expected memory player lock store, got %s", cfg.PlayerLockStore)
	}
	if cfg.PlayerLockTTL != 3*time.Second {
		t.Fatalf("expected player lock ttl 3s, got %s", cfg.PlayerLockTTL)
	}
}

func TestLoadFallsBackToMemoryCacheStores(t *testing.T) {
	t.Setenv("MYTHWAKE_REDIS_ADDR", "")
	t.Setenv("MYTHWAKE_SESSION_CACHE_STORE", "bad-store")
	t.Setenv("MYTHWAKE_RATE_LIMIT_STORE", "bad-store")
	t.Setenv("MYTHWAKE_PLAYER_LOCK_STORE", "bad-store")

	cfg := Load()

	if cfg.SessionCacheStore != CacheStoreMemory || cfg.RateLimitStore != CacheStoreMemory || cfg.PlayerLockStore != CacheStoreMemory {
		t.Fatalf("expected unsupported cache stores to fall back to memory, got session=%s rate=%s lock=%s", cfg.SessionCacheStore, cfg.RateLimitStore, cfg.PlayerLockStore)
	}
}
