package config

import (
	"os"
	"time"
)

type Config struct {
	ServiceName        string
	Addr               string
	Environment        string
	Version            string
	DatabaseURL        string
	DatabaseStatus     string
	StateCacheStatus   string
	StateFlushInterval time.Duration
	StateFlushTimeout  time.Duration
}

func Load() Config {
	return Config{
		ServiceName:        "mythwake-api",
		Addr:               getEnv("MYTHWAKE_API_ADDR", ":8080"),
		Environment:        getEnv("MYTHWAKE_ENV", "local"),
		Version:            getEnv("MYTHWAKE_API_VERSION", "0.2.10"),
		DatabaseURL:        os.Getenv("MYTHWAKE_DATABASE_URL"),
		DatabaseStatus:     "disabled",
		StateCacheStatus:   "disabled",
		StateFlushInterval: getDurationEnv("MYTHWAKE_STATE_FLUSH_INTERVAL", 30*time.Second),
		StateFlushTimeout:  getDurationEnv("MYTHWAKE_STATE_FLUSH_TIMEOUT", 5*time.Second),
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
