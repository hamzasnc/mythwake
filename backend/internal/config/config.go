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
	CacheStoreMemory                = "memory"
	CacheStoreRedis                 = "redis"
)

type Config struct {
	ServiceName        string
	Addr               string
	Environment        string
	Version            string
	DatabaseURL        string
	DatabaseStatus     string
	RedisAddr          string
	RedisPassword      string
	RedisDB            int
	RedisStatus        string
	StateCacheStatus   string
	BalanceCatalog     string
	StateWriteMode     string
	StateFlushInterval time.Duration
	StateFlushTimeout  time.Duration
	SessionCacheStore  string
	SessionCacheTTL    time.Duration
	SessionTouchWindow time.Duration
	RateLimitStore     string
	RateLimitEnabled   bool
	RateLimitWindow    time.Duration
	RateLimitAuth      int
	RateLimitGameplay  int
	RequireIdempotency bool
	DevToolsEnabled    bool
}

func Load() Config {
	redisAddr := strings.TrimSpace(os.Getenv("MYTHWAKE_REDIS_ADDR"))
	defaultCache := defaultCacheStore(redisAddr)

	return Config{
		ServiceName:        "mythwake-api",
		Addr:               getEnv("MYTHWAKE_API_ADDR", ":8080"),
		Environment:        getEnv("MYTHWAKE_ENV", "local"),
		Version:            getEnv("MYTHWAKE_API_VERSION", "0.2.52"),
		DatabaseURL:        os.Getenv("MYTHWAKE_DATABASE_URL"),
		DatabaseStatus:     "disabled",
		RedisAddr:          redisAddr,
		RedisPassword:      os.Getenv("MYTHWAKE_REDIS_PASSWORD"),
		RedisDB:            getIntEnv("MYTHWAKE_REDIS_DB", 0),
		RedisStatus:        "disabled",
		StateCacheStatus:   "disabled",
		BalanceCatalog:     "static",
		StateWriteMode:     getStateWriteMode(),
		StateFlushInterval: getDurationEnv("MYTHWAKE_STATE_FLUSH_INTERVAL", 30*time.Second),
		StateFlushTimeout:  getDurationEnv("MYTHWAKE_STATE_FLUSH_TIMEOUT", 5*time.Second),
		SessionCacheStore:  getCacheStore("MYTHWAKE_SESSION_CACHE_STORE", defaultCache),
		SessionCacheTTL:    getDurationEnv("MYTHWAKE_SESSION_CACHE_TTL", 30*time.Second),
		SessionTouchWindow: getDurationEnv("MYTHWAKE_SESSION_TOUCH_WINDOW", 30*time.Second),
		RateLimitStore:     getCacheStore("MYTHWAKE_RATE_LIMIT_STORE", defaultCache),
		RateLimitEnabled:   getBoolEnv("MYTHWAKE_RATE_LIMIT_ENABLED", true),
		RateLimitWindow:    getDurationEnv("MYTHWAKE_RATE_LIMIT_WINDOW", time.Minute),
		RateLimitAuth:      getIntEnv("MYTHWAKE_RATE_LIMIT_AUTH", 30),
		RateLimitGameplay:  getIntEnv("MYTHWAKE_RATE_LIMIT_GAMEPLAY", 240),
		RequireIdempotency: getBoolEnv("MYTHWAKE_REQUIRE_IDEMPOTENCY", true),
		DevToolsEnabled:    getBoolEnv("MYTHWAKE_DEV_TOOLS_ENABLED", defaultDevToolsEnabled(getEnv("MYTHWAKE_ENV", "local"))),
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

func defaultCacheStore(redisAddr string) string {
	if strings.TrimSpace(redisAddr) != "" {
		return CacheStoreRedis
	}

	return CacheStoreMemory
}

func getCacheStore(key string, fallback string) string {
	if fallback == "" {
		fallback = CacheStoreMemory
	}

	value := strings.ToLower(strings.TrimSpace(getEnv(key, fallback)))
	switch value {
	case CacheStoreMemory, CacheStoreRedis:
		return value
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

func defaultDevToolsEnabled(environment string) bool {
	switch strings.ToLower(strings.TrimSpace(environment)) {
	case "local", "local-e2e", "dev", "development", "test":
		return true
	default:
		return false
	}
}
