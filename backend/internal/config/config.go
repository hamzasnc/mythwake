package config

import "os"

type Config struct {
	ServiceName string
	Addr        string
	Environment string
	Version     string
}

func Load() Config {
	return Config{
		ServiceName: "mythwake-api",
		Addr:        getEnv("MYTHWAKE_API_ADDR", ":8080"),
		Environment: getEnv("MYTHWAKE_ENV", "local"),
		Version:     getEnv("MYTHWAKE_API_VERSION", "0.1.0"),
	}
}

func getEnv(key string, fallback string) string {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}

	return value
}
