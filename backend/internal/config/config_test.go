package config

import "testing"

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
