// Package config reads the API configuration from the environment.
package config

import (
	"errors"
	"os"
	"time"
)

type Config struct {
	Addr        string
	DatabaseURL string
	RulesURL    string
	JWTSecret   []byte
	TokenTTL    time.Duration
	// ArenaBots is the number of bot defenders kept in the arena so new servers have opponents.
	ArenaBots int
}

// Load reads MYTHWAKE_* variables. Only the JWT secret has no default: a server must never run with a known key.
func Load() (Config, error) {
	c := Config{
		Addr:        env("MYTHWAKE_ADDR", "127.0.0.1:8080"),
		DatabaseURL: env("MYTHWAKE_DATABASE_URL", "postgres://mythwake@127.0.0.1:55432/mythwake?sslmode=disable"),
		RulesURL:    env("MYTHWAKE_RULES_URL", "http://127.0.0.1:5081"),
		JWTSecret:   []byte(os.Getenv("MYTHWAKE_JWT_SECRET")),
		TokenTTL:    7 * 24 * time.Hour,
		ArenaBots:   30,
	}
	if len(c.JWTSecret) < 32 {
		return c, errors.New("MYTHWAKE_JWT_SECRET must be set (at least 32 bytes)")
	}
	return c, nil
}

func env(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
