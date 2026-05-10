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
	t.Setenv("MYTHWAKE_SESSION_CACHE_TTL", "45s")
	t.Setenv("MYTHWAKE_SESSION_TOUCH_WINDOW", "2m")

	cfg := Load()

	if cfg.SessionCacheTTL != 45*time.Second {
		t.Fatalf("expected session cache ttl 45s, got %s", cfg.SessionCacheTTL)
	}
	if cfg.SessionTouchWindow != 2*time.Minute {
		t.Fatalf("expected session touch window 2m, got %s", cfg.SessionTouchWindow)
	}
}
