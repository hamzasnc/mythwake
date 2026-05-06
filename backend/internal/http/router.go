package apihttp

import (
	"encoding/json"
	"log"
	"net/http"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/config"
)

type Router struct {
	config config.Config
	logger *log.Logger
	mux    *http.ServeMux
}

func NewRouter(cfg config.Config, logger *log.Logger) http.Handler {
	router := &Router{
		config: cfg,
		logger: logger,
		mux:    http.NewServeMux(),
	}

	router.routes()
	return router.logRequests(router.mux)
}

func (router *Router) routes() {
	router.mux.HandleFunc("GET /health", router.handleHealth)
}

func (router *Router) handleHealth(response http.ResponseWriter, request *http.Request) {
	writeJSON(response, http.StatusOK, map[string]string{
		"service":     router.config.ServiceName,
		"status":      "ok",
		"environment": router.config.Environment,
		"version":     router.config.Version,
		"time_utc":    time.Now().UTC().Format(time.RFC3339),
	})
}

func (router *Router) logRequests(next http.Handler) http.Handler {
	return http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		startedAt := time.Now()
		next.ServeHTTP(response, request)
		router.logger.Printf("%s %s %s", request.Method, request.URL.Path, time.Since(startedAt))
	})
}

func writeJSON(response http.ResponseWriter, statusCode int, payload any) {
	response.Header().Set("Content-Type", "application/json")
	response.WriteHeader(statusCode)

	if err := json.NewEncoder(response).Encode(payload); err != nil {
		http.Error(response, "failed to encode response", http.StatusInternalServerError)
	}
}
