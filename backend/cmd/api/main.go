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
	apihttp "github.com/hamzasnc/mythwake/backend/internal/http"
)

func main() {
	cfg := config.Load()
	logger := log.New(os.Stdout, "mythwake-api ", log.LstdFlags|log.LUTC)

	server := &http.Server{
		Addr:              cfg.Addr,
		Handler:           apihttp.NewRouter(cfg, logger),
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
