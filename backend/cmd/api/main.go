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

	"github.com/hamzasnc/mythwake/backend/internal/auth"
	"github.com/hamzasnc/mythwake/backend/internal/config"
	"github.com/hamzasnc/mythwake/backend/internal/database"
	apihttp "github.com/hamzasnc/mythwake/backend/internal/http"
	"github.com/hamzasnc/mythwake/backend/internal/player"
	"github.com/hamzasnc/mythwake/backend/internal/store/cache"
	"github.com/hamzasnc/mythwake/backend/internal/store/postgres"
)

func main() {
	cfg := config.Load()
	logger := log.New(os.Stdout, "mythwake-api ", log.LstdFlags|log.LUTC)
	authOptions := []auth.ServiceOption{
		auth.WithSessionCacheTTL(cfg.SessionCacheTTL),
		auth.WithSessionTouchInterval(cfg.SessionTouchWindow),
	}
	authService := auth.NewService(nil, authOptions...)
	var cachedStateStore *cache.WriteBehindStateStore
	var stateStore player.StateStore

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
		authService = auth.NewService(postgres.NewAccountStore(db), authOptions...)
		cachedStateStore = cache.NewWriteBehindStateStore(
			postgres.NewPlayerStateStore(db),
			cache.Config{
				FlushInterval: cfg.StateFlushInterval,
				FlushTimeout:  cfg.StateFlushTimeout,
				WriteBehind:   cfg.StateWriteMode != config.StateWriteModeWriteThrough,
			},
			logger,
		)
		stateStore = cachedStateStore
		cancel()
		cfg.DatabaseStatus = "connected"
		cfg.StateCacheStatus = cfg.StateWriteMode
	}
	playerManager := player.NewManager(stateStore)

	server := &http.Server{
		Addr:              cfg.Addr,
		Handler:           apihttp.NewRouter(cfg, logger, authService, playerManager),
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
	if playerManager.LoadedPlayerCount() > 0 {
		logger.Printf("flushing %d loaded player contexts", playerManager.LoadedPlayerCount())
		if err := playerManager.FlushAll(shutdownContext); err != nil {
			logger.Printf("loaded player flush failed during shutdown: %v", err)
		}
	}
	if cachedStateStore != nil {
		logger.Println("flushing player state cache")
		if err := cachedStateStore.Close(shutdownContext); err != nil {
			logger.Printf("state cache flush failed during shutdown: %v", err)
		}
	}
}
