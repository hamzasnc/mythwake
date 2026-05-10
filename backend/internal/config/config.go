package config

import "os"

type Config struct {
	ServiceName    string
	Addr           string
	Environment    string
	Version        string
	DatabaseURL    string
	DatabaseStatus string
}

func Load() Config {
	return Config{
		ServiceName:    "mythwake-api",
		Addr:           getEnv("MYTHWAKE_API_ADDR", ":8080"),
		Environment:    getEnv("MYTHWAKE_ENV", "local"),
		Version:        getEnv("MYTHWAKE_API_VERSION", "0.2.7"),
		DatabaseURL:    os.Getenv("MYTHWAKE_DATABASE_URL"),
		DatabaseStatus: "disabled",
	}
}

func getEnv(key string, fallback string) string {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}

	return value
}
