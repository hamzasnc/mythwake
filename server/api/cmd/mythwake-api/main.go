// Command mythwake-api is the Mythwake game server (see docs/SERVER.md).
package main

import (
	"context"
	"errors"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"mythwake/api/internal/api"
	"mythwake/api/internal/auth"
	"mythwake/api/internal/config"
	"mythwake/api/internal/db"
	"mythwake/api/internal/rules"
	"mythwake/api/internal/store"
)

func main() {
	log := slog.New(slog.NewTextHandler(os.Stdout, &slog.HandlerOptions{Level: slog.LevelInfo}))
	if err := run(log); err != nil {
		log.Error("server stopped", "err", err)
		os.Exit(1)
	}
}

func run(log *slog.Logger) error {
	cfg, err := config.Load()
	if err != nil {
		return err
	}
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	pool, err := db.Open(ctx, cfg.DatabaseURL)
	if err != nil {
		return err
	}
	defer pool.Close()
	applied, err := db.Migrate(ctx, pool)
	if err != nil {
		return err
	}
	if len(applied) > 0 {
		log.Info("migrations applied", "names", applied)
	}

	rulesClient := rules.New(cfg.RulesURL)
	version, err := waitForRules(ctx, rulesClient, log)
	if err != nil {
		return err
	}
	log.Info("rules service ready", "url", cfg.RulesURL, "contentVersion", version)

	server := api.New(store.New(pool), rulesClient, auth.NewTokens(cfg.JWTSecret, cfg.TokenTTL), log)
	if err := server.EnsureBots(ctx, cfg.ArenaBots); err != nil {
		log.Warn("arena bots not created", "err", err)
	}

	httpServer := &http.Server{
		Addr:              cfg.Addr,
		Handler:           server.Handler(),
		ReadHeaderTimeout: 5 * time.Second,
		ReadTimeout:       15 * time.Second,
		WriteTimeout:      30 * time.Second,
		IdleTimeout:       90 * time.Second,
	}
	errs := make(chan error, 1)
	go func() {
		log.Info("listening", "addr", cfg.Addr)
		errs <- httpServer.ListenAndServe()
	}()
	select {
	case err := <-errs:
		if !errors.Is(err, http.ErrServerClosed) {
			return err
		}
	case <-ctx.Done():
		log.Info("shutting down")
		shutdown, cancel := context.WithTimeout(context.Background(), 10*time.Second)
		defer cancel()
		return httpServer.Shutdown(shutdown)
	}
	return nil
}

// waitForRules waits up to 60 s for the rules service (it may still be starting next to the API).
func waitForRules(ctx context.Context, c *rules.Client, log *slog.Logger) (string, error) {
	deadline := time.Now().Add(60 * time.Second)
	for {
		version, err := c.Health(ctx)
		if err == nil {
			return version, nil
		}
		if time.Now().After(deadline) {
			return "", err
		}
		log.Info("waiting for rules service", "err", err)
		select {
		case <-ctx.Done():
			return "", ctx.Err()
		case <-time.After(2 * time.Second):
		}
	}
}
