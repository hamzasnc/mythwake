package config

import (
	"os"
	"strconv"
	"strings"
	"time"
)

const (
	StateWriteModeLedgerWriteBehind = "ledger_write_behind"
	StateWriteModeWriteThrough      = "write_through"
	StateWriteModeWriteBehind       = "write_behind"
)

type Config struct {
	ServiceName        string
	Addr               string
	Environment        string
	Version            string
	DatabaseURL        string
	DatabaseStatus     string
	StateCacheStatus   string
	StateWriteMode     string
	StateFlushInterval time.Duration
	StateFlushTimeout  time.Duration
	SessionCacheTTL    time.Duration
	SessionTouchWindow time.Duration
	RateLimitEnabled   bool
	RateLimitWindow    time.Duration
	RateLimitAuth      int
	RateLimitGameplay  int
	RequireIdempotency bool
}

func Load() Config {
	return Config{
		ServiceName:        "mythwake-api",
		Addr:               getEnv("MYTHWAKE_API_ADDR", ":8080"),
		Environment:        getEnv("MYTHWAKE_ENV", "local"),
		Version:            getEnv("MYTHWAKE_API_VERSION", "0.2.31"),
		DatabaseURL:        os.Getenv("MYTHWAKE_DATABASE_URL"),
		DatabaseStatus:     "disabled",
		StateCacheStatus:   "disabled",
		StateWriteMode:     getStateWriteMode(),
		StateFlushInterval: getDurationEnv("MYTHWAKE_STATE_FLUSH_INTERVAL", 30*time.Second),
		StateFlushTimeout:  getDurationEnv("MYTHWAKE_STATE_FLUSH_TIMEOUT", 5*time.Second),
		SessionCacheTTL:    getDurationEnv("MYTHWAKE_SESSION_CACHE_TTL", 30*time.Second),
		SessionTouchWindow: getDurationEnv("MYTHWAKE_SESSION_TOUCH_WINDOW", 30*time.Second),
		RateLimitEnabled:   getBoolEnv("MYTHWAKE_RATE_LIMIT_ENABLED", true),
		RateLimitWindow:    getDurationEnv("MYTHWAKE_RATE_LIMIT_WINDOW", time.Minute),
		RateLimitAuth:      getIntEnv("MYTHWAKE_RATE_LIMIT_AUTH", 30),
		RateLimitGameplay:  getIntEnv("MYTHWAKE_RATE_LIMIT_GAMEPLAY", 240),
		RequireIdempotency: getBoolEnv("MYTHWAKE_REQUIRE_IDEMPOTENCY", true),
	}
}

func getEnv(key string, fallback string) string {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}

	return value
}

func getDurationEnv(key string, fallback time.Duration) time.Duration {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}

	duration, err := time.ParseDuration(value)
	if err != nil {
		return fallback
	}

	return duration
}

func getBoolEnv(key string, fallback bool) bool {
	value := strings.ToLower(strings.TrimSpace(os.Getenv(key)))
	if value == "" {
		return fallback
	}

	switch value {
	case "1", "true", "yes", "y", "on":
		return true
	case "0", "false", "no", "n", "off":
		return false
	default:
		return fallback
	}
}

func getIntEnv(key string, fallback int) int {
	value := strings.TrimSpace(os.Getenv(key))
	if value == "" {
		return fallback
	}

	parsed, err := strconv.Atoi(value)
	if err != nil {
		return fallback
	}

	return parsed
}

func getStateWriteMode() string {
	value := getEnv("MYTHWAKE_STATE_WRITE_MODE", StateWriteModeLedgerWriteBehind)
	switch value {
	case StateWriteModeLedgerWriteBehind, StateWriteModeWriteThrough, StateWriteModeWriteBehind:
		return value
	}

	return StateWriteModeLedgerWriteBehind
}
