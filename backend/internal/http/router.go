package apihttp

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"io"
	"log"
	"net/http"
	"strings"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/config"
	"github.com/hamzasnc/mythwake/backend/internal/player"
)

type Router struct {
	config        config.Config
	logger        *log.Logger
	mux           *http.ServeMux
	playerService *player.Service
}

func NewRouter(cfg config.Config, logger *log.Logger, playerService *player.Service) http.Handler {
	router := &Router{
		config:        cfg,
		logger:        logger,
		mux:           http.NewServeMux(),
		playerService: playerService,
	}

	router.routes()
	return router.logRequests(router.mux)
}

func (router *Router) routes() {
	router.mux.HandleFunc("POST /auth/guest", router.handleGuestAuth)
	router.mux.HandleFunc("GET /health", router.handleHealth)
	router.mux.HandleFunc("GET /player/state", router.handlePlayerState)
	router.mux.HandleFunc("POST /player/state/flush", router.handlePlayerStateFlush)
	router.mux.HandleFunc("GET /player/core-state", router.handlePlayerCoreState)
	router.mux.HandleFunc("POST /campaign/fight", router.handleCampaignFight)
	router.mux.HandleFunc("POST /dungeons/{dungeon_id}/run", router.handleDungeonRun)
	router.mux.HandleFunc("POST /heroes/{hero_id}/level-up", router.handleHeroLevel)
	router.mux.HandleFunc("POST /heroes/{hero_id}/ascend", router.handleHeroAscend)
	router.mux.HandleFunc("POST /equipment/{equipment_id}/level-up", router.handleEquipmentLevel)
	router.mux.HandleFunc("POST /gear/accessories/equip", router.handleAccessoryEquip)
	router.mux.HandleFunc("POST /gear/accessories/level-up", router.handleAccessoryLevel)
	router.mux.HandleFunc("POST /gear/accessories/fuse", router.handleAccessoryFuse)
	router.mux.HandleFunc("POST /summons/{banner_id}/pull", router.handleSummonPull)
	router.mux.HandleFunc("POST /missions/{mission_id}/claim", router.handleDailyMissionClaim)
	router.mux.HandleFunc("POST /battle-pass/{reward_id}/claim", router.handleBattlePassClaim)
}

func (router *Router) handleHealth(response http.ResponseWriter, request *http.Request) {
	writeJSON(response, http.StatusOK, map[string]string{
		"service":              router.config.ServiceName,
		"status":               "ok",
		"database":             router.config.DatabaseStatus,
		"state_cache":          router.config.StateCacheStatus,
		"state_write_mode":     router.config.StateWriteMode,
		"state_flush_interval": router.config.StateFlushInterval.String(),
		"require_idempotency":  boolLabel(router.config.RequireIdempotency),
		"environment":          router.config.Environment,
		"version":              router.config.Version,
		"time_utc":             time.Now().UTC().Format(time.RFC3339),
	})
}

func (router *Router) handlePlayerState(response http.ResponseWriter, request *http.Request) {
	writeJSON(response, http.StatusOK, router.playerService.GetSnapshot())
}

func (router *Router) handlePlayerCoreState(response http.ResponseWriter, request *http.Request) {
	writeJSON(response, http.StatusOK, router.playerService.GetState())
}

func (router *Router) handlePlayerStateFlush(response http.ResponseWriter, request *http.Request) {
	if err := router.playerService.FlushState(request.Context()); err != nil {
		writeJSON(response, http.StatusInternalServerError, map[string]string{
			"errorCode": "state_flush_failed",
			"message":   err.Error(),
		})
		return
	}

	writeJSON(response, http.StatusOK, map[string]string{
		"status": "ok",
	})
}

func (router *Router) handleGuestAuth(response http.ResponseWriter, request *http.Request) {
	writeJSON(response, http.StatusOK, router.playerService.GuestAuth())
}

func (router *Router) handleCampaignFight(response http.ResponseWriter, request *http.Request) {
	action, ok := router.actionRequest(response, request, "")
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.FightCampaignWithRequest(request.Context(), action))
}

func (router *Router) handleDungeonRun(response http.ResponseWriter, request *http.Request) {
	action, ok := router.actionRequest(response, request, "")
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.RunDungeonWithRequest(request.Context(), action, request.PathValue("dungeon_id")))
}

func (router *Router) handleHeroLevel(response http.ResponseWriter, request *http.Request) {
	action, ok := router.actionRequest(response, request, "")
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.LevelHeroWithRequest(request.Context(), action, request.PathValue("hero_id")))
}

func (router *Router) handleHeroAscend(response http.ResponseWriter, request *http.Request) {
	action, ok := router.actionRequest(response, request, "")
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.AscendHeroWithRequest(request.Context(), action, request.PathValue("hero_id")))
}

func (router *Router) handleEquipmentLevel(response http.ResponseWriter, request *http.Request) {
	action, ok := router.actionRequest(response, request, "")
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.LevelEquipmentWithRequest(request.Context(), action, request.PathValue("equipment_id")))
}

func (router *Router) handleAccessoryEquip(response http.ResponseWriter, request *http.Request) {
	accessoryRequest, rawBody, ok := decodeAccessoryRequest(response, request)
	if !ok {
		return
	}

	action, ok := router.actionRequest(response, request, rawBody)
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.EquipAccessoryWithRequest(request.Context(), action, accessoryRequest.AccessoryID))
}

func (router *Router) handleAccessoryLevel(response http.ResponseWriter, request *http.Request) {
	accessoryRequest, rawBody, ok := decodeAccessoryRequest(response, request)
	if !ok {
		return
	}

	action, ok := router.actionRequest(response, request, rawBody)
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.LevelAccessoryWithRequest(request.Context(), action, accessoryRequest.AccessoryID))
}

func (router *Router) handleAccessoryFuse(response http.ResponseWriter, request *http.Request) {
	accessoryRequest, rawBody, ok := decodeAccessoryRequest(response, request)
	if !ok {
		return
	}

	action, ok := router.actionRequest(response, request, rawBody)
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.FuseAccessoryWithRequest(request.Context(), action, accessoryRequest.AccessoryID))
}

func (router *Router) handleSummonPull(response http.ResponseWriter, request *http.Request) {
	action, ok := router.actionRequest(response, request, "")
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.PullSummonWithRequest(request.Context(), action, request.PathValue("banner_id")))
}

func (router *Router) handleDailyMissionClaim(response http.ResponseWriter, request *http.Request) {
	action, ok := router.actionRequest(response, request, "")
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.ClaimDailyMissionWithRequest(request.Context(), action, request.PathValue("mission_id")))
}

func (router *Router) handleBattlePassClaim(response http.ResponseWriter, request *http.Request) {
	action, ok := router.actionRequest(response, request, "")
	if !ok {
		return
	}

	writeActionResult(response, router.playerService.ClaimBattlePassRewardWithRequest(request.Context(), action, request.PathValue("reward_id")))
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

func writeActionResult(response http.ResponseWriter, result any) {
	writeJSON(response, http.StatusOK, result)
}

func (router *Router) actionRequest(response http.ResponseWriter, request *http.Request, rawBody string) (player.ActionRequest, bool) {
	action, errorCode, message := buildActionRequest(request, rawBody)
	if errorCode != "" {
		writeJSON(response, http.StatusBadRequest, map[string]string{
			"errorCode": errorCode,
			"message":   message,
		})
		return player.ActionRequest{}, false
	}
	if router.config.RequireIdempotency && !action.HasIdempotency() {
		writeJSON(response, http.StatusBadRequest, map[string]string{
			"errorCode": "missing_idempotency_key",
			"message":   "Gameplay mutation requests require an Idempotency-Key header.",
		})
		return player.ActionRequest{}, false
	}

	return action, true
}

func buildActionRequest(request *http.Request, rawBody string) (player.ActionRequest, string, string) {
	idempotencyKey := strings.TrimSpace(request.Header.Get("Idempotency-Key"))
	if idempotencyKey == "" {
		idempotencyKey = strings.TrimSpace(request.Header.Get("X-Idempotency-Key"))
	}
	if idempotencyKey == "" {
		return player.ActionRequest{}, "", ""
	}
	if !validIdempotencyKey(idempotencyKey) {
		return player.ActionRequest{}, "invalid_idempotency_key", "Idempotency-Key must be 8-128 chars and only contain letters, numbers, dot, underscore, colon, or dash."
	}

	hash := sha256.Sum256([]byte(request.Method + "\n" + request.URL.EscapedPath() + "\n" + request.URL.RawQuery + "\n" + rawBody))
	return player.ActionRequest{
		IdempotencyKey: idempotencyKey,
		RequestHash:    hex.EncodeToString(hash[:]),
	}, "", ""
}

func validIdempotencyKey(key string) bool {
	if len(key) < 8 || len(key) > 128 {
		return false
	}

	for _, char := range key {
		if char >= 'a' && char <= 'z' {
			continue
		}
		if char >= 'A' && char <= 'Z' {
			continue
		}
		if char >= '0' && char <= '9' {
			continue
		}
		if char == '.' || char == '_' || char == ':' || char == '-' {
			continue
		}
		return false
	}

	return true
}

func boolLabel(value bool) string {
	if value {
		return "true"
	}

	return "false"
}

func decodeAccessoryRequest(response http.ResponseWriter, request *http.Request) (api.AccessoryRequest, string, bool) {
	rawBody, err := io.ReadAll(request.Body)
	if err != nil {
		writeJSON(response, http.StatusBadRequest, map[string]string{
			"errorCode": "invalid_body",
			"message":   "Could not read request body.",
		})
		return api.AccessoryRequest{}, "", false
	}

	var accessoryRequest api.AccessoryRequest
	if err := json.Unmarshal(rawBody, &accessoryRequest); err != nil {
		writeJSON(response, http.StatusBadRequest, map[string]string{
			"errorCode": "invalid_json",
			"message":   "Expected JSON body with accessoryId.",
		})
		return api.AccessoryRequest{}, string(rawBody), false
	}

	if accessoryRequest.AccessoryID == "" {
		writeJSON(response, http.StatusBadRequest, map[string]string{
			"errorCode": "missing_accessory_id",
			"message":   "Expected accessoryId.",
		})
		return api.AccessoryRequest{}, string(rawBody), false
	}

	return accessoryRequest, string(rawBody), true
}
