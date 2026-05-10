package config

import (
	"os"
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
	RequireIdempotency bool
}

func Load() Config {
	return Config{
		ServiceName:        "mythwake-api",
		Addr:               getEnv("MYTHWAKE_API_ADDR", ":8080"),
		Environment:        getEnv("MYTHWAKE_ENV", "local"),
		Version:            getEnv("MYTHWAKE_API_VERSION", "0.2.20"),
		DatabaseURL:        os.Getenv("MYTHWAKE_DATABASE_URL"),
		DatabaseStatus:     "disabled",
		StateCacheStatus:   "disabled",
		StateWriteMode:     getStateWriteMode(),
		StateFlushInterval: getDurationEnv("MYTHWAKE_STATE_FLUSH_INTERVAL", 30*time.Second),
		StateFlushTimeout:  getDurationEnv("MYTHWAKE_STATE_FLUSH_TIMEOUT", 5*time.Second),
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

func getStateWriteMode() string {
	value := getEnv("MYTHWAKE_STATE_WRITE_MODE", StateWriteModeLedgerWriteBehind)
	switch value {
	case StateWriteModeLedgerWriteBehind, StateWriteModeWriteThrough, StateWriteModeWriteBehind:
		return value
	}

	return StateWriteModeLedgerWriteBehind
}
