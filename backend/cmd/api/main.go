package main

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/config"
	"github.com/hamzasnc/mythwake/backend/internal/database"
	apihttp "github.com/hamzasnc/mythwake/backend/internal/http"
	"github.com/hamzasnc/mythwake/backend/internal/player"
	"github.com/hamzasnc/mythwake/backend/internal/store/postgres"
)

func main() {
	cfg := config.Load()
	logger := log.New(os.Stdout, "mythwake-api ", log.LstdFlags|log.LUTC)
	playerService := player.NewService()

	if cfg.DatabaseURL != "" {
		setupContext, cancel := context.WithTimeout(context.Background(), 15*time.Second)
		db, err := database.Open(setupContext, cfg.DatabaseURL)
		if err != nil {
			cancel()
			logger.Fatalf("database connection failed: %v", err)
		}
		defer db.Close()

		if err := database.Migrate(setupContext, db); err != nil {
			cancel()
			logger.Fatalf("database migration failed: %v", err)
		}
		if err := playerService.UseStateStore(setupContext, postgres.NewPlayerStateStore(db)); err != nil {
			cancel()
			logger.Fatalf("player state store failed: %v", err)
		}
		cancel()
		cfg.DatabaseStatus = "connected"
	}

	server := &http.Server{
		Addr:              cfg.Addr,
		Handler:           apihttp.NewRouter(cfg, logger, playerService),
		ReadHeaderTimeout: 5 * time.Second,
	}

	go func() {
		logger.Printf("starting %s %s on %s", cfg.ServiceName, cfg.Version, cfg.Addr)
		if err := server.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			logger.Fatalf("server failed: %v", err)
		}
	}()

	stop := make(chan os.Signal, 1)
	signal.Notify(stop, os.Interrupt, syscall.SIGTERM)
	<-stop

	shutdownContext, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	logger.Println("shutting down")
	if err := server.Shutdown(shutdownContext); err != nil {
		logger.Fatalf("shutdown failed: %v", err)
	}
}
