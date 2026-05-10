package apihttp

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"io"
	"log"
	"net/http"
	"strconv"
	"strings"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/auth"
	"github.com/hamzasnc/mythwake/backend/internal/config"
	"github.com/hamzasnc/mythwake/backend/internal/definitions"
	"github.com/hamzasnc/mythwake/backend/internal/player"
)

type Router struct {
	config             config.Config
	logger             *log.Logger
	mux                *http.ServeMux
	authService        *auth.Service
	playerManager      *player.Manager
	definitionProvider definitions.SnapshotProvider
}

type RouterOption func(*Router)

func WithDefinitionProvider(provider definitions.SnapshotProvider) RouterOption {
	return func(router *Router) {
		if provider != nil {
			router.definitionProvider = provider
		}
	}
}

func NewRouter(cfg config.Config, logger *log.Logger, authService *auth.Service, playerManager *player.Manager, options ...RouterOption) http.Handler {
	if authService == nil {
		authService = auth.NewService(nil)
	}
	if playerManager == nil {
		playerManager = player.NewManager(nil)
	}

	router := &Router{
		config:             cfg,
		logger:             logger,
		mux:                http.NewServeMux(),
		authService:        authService,
		playerManager:      playerManager,
		definitionProvider: definitions.NewStaticSnapshotProvider(),
	}

	for _, option := range options {
		option(router)
	}

	router.routes()
	return withRequestID(logRequests(router.logger, recoverPanic(router.logger, withRateLimit(router.config, router.mux))))
}

func (router *Router) routes() {
	router.mux.HandleFunc("POST /auth/guest", router.handleGuestAuth)
	router.mux.HandleFunc("POST /auth/logout", router.handleLogout)
	router.mux.HandleFunc("GET /health", router.handleHealth)
	router.mux.HandleFunc("GET /time", router.handleTime)
	router.mux.HandleFunc("GET /definitions", router.handleDefinitions)
	router.mux.HandleFunc("GET /player/state", router.handlePlayerState)
	router.mux.HandleFunc("POST /player/state/flush", router.handlePlayerStateFlush)
	router.mux.HandleFunc("GET /player/core-state", router.handlePlayerCoreState)
	router.mux.HandleFunc("POST /player/offline/claim", router.handleOfflineClaim)
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
	if router.config.DevToolsEnabled {
		router.mux.HandleFunc("POST /dev/player/reset", router.handleDevPlayerReset)
	}
}

func (router *Router) handleDefinitions(response http.ResponseWriter, request *http.Request) {
	snapshot, err := router.definitionProvider.Snapshot(request.Context(), router.config.Version)
	if err != nil {
		writeError(response, request, http.StatusInternalServerError, "definitions_unavailable", "Definition catalog is temporarily unavailable.")
		return
	}

	etag := definitions.ETag(snapshot)

	response.Header().Set("Cache-Control", "private, max-age=60, must-revalidate")
	response.Header().Set("ETag", etag)
	if matchesIfNoneMatch(request.Header.Get("If-None-Match"), etag) {
		response.WriteHeader(http.StatusNotModified)
		return
	}

	writeJSON(response, http.StatusOK, snapshot)
}

func matchesIfNoneMatch(header string, etag string) bool {
	for _, match := range strings.Split(header, ",") {
		match = strings.TrimSpace(match)
		if match == "*" || match == etag || strings.TrimPrefix(match, "W/") == etag {
			return true
		}
	}

	return false
}

func (router *Router) handleHealth(response http.ResponseWriter, request *http.Request) {
	writeJSON(response, http.StatusOK, map[string]string{
		"service":              router.config.ServiceName,
		"status":               "ok",
		"database":             router.config.DatabaseStatus,
		"state_cache":          router.config.StateCacheStatus,
		"balance_catalog":      router.config.BalanceCatalog,
		"state_write_mode":     router.config.StateWriteMode,
		"state_flush_interval": router.config.StateFlushInterval.String(),
		"session_cache_ttl":    router.config.SessionCacheTTL.String(),
		"session_touch_window": router.config.SessionTouchWindow.String(),
		"rate_limit_enabled":   boolLabel(router.config.RateLimitEnabled),
		"rate_limit_window":    router.config.RateLimitWindow.String(),
		"rate_limit_auth":      strconv.Itoa(router.config.RateLimitAuth),
		"rate_limit_gameplay":  strconv.Itoa(router.config.RateLimitGameplay),
		"require_idempotency":  boolLabel(router.config.RequireIdempotency),
		"dev_tools":            boolLabel(router.config.DevToolsEnabled),
		"environment":          router.config.Environment,
		"version":              router.config.Version,
		"time_utc":             time.Now().UTC().Format(time.RFC3339),
	})
}

func (router *Router) handleTime(response http.ResponseWriter, request *http.Request) {
	writeJSON(response, http.StatusOK, serverClock(time.Now().UTC()))
}

func serverClock(now time.Time) api.ServerClockResponse {
	now = now.UTC()
	dailyReset := nextDailyResetUTC(now)
	weeklyReset := nextWeeklyResetUTC(now)

	return api.ServerClockResponse{
		ServerTimeUTC:           now.Format(time.RFC3339),
		ServerUnixMs:            now.UnixMilli(),
		DailyResetUTC:           dailyReset.Format(time.RFC3339),
		WeeklyResetUTC:          weeklyReset.Format(time.RFC3339),
		SecondsUntilDailyReset:  int64(dailyReset.Sub(now).Seconds()),
		SecondsUntilWeeklyReset: int64(weeklyReset.Sub(now).Seconds()),
	}
}

func nextDailyResetUTC(now time.Time) time.Time {
	now = now.UTC()
	return time.Date(now.Year(), now.Month(), now.Day()+1, 0, 0, 0, 0, time.UTC)
}

func nextWeeklyResetUTC(now time.Time) time.Time {
	now = now.UTC()
	daysUntilMonday := (int(time.Monday) - int(now.Weekday()) + 7) % 7
	if daysUntilMonday == 0 {
		daysUntilMonday = 7
	}

	resetDate := now.AddDate(0, 0, daysUntilMonday)
	return time.Date(resetDate.Year(), resetDate.Month(), resetDate.Day(), 0, 0, 0, 0, time.UTC)
}

func (router *Router) handlePlayerState(response http.ResponseWriter, request *http.Request) {
	playerService, ok := router.authenticatedPlayerService(response, request)
	if !ok {
		return
	}

	writeJSON(response, http.StatusOK, playerService.GetSnapshot())
}

func (router *Router) handlePlayerCoreState(response http.ResponseWriter, request *http.Request) {
	playerService, ok := router.authenticatedPlayerService(response, request)
	if !ok {
		return
	}

	writeJSON(response, http.StatusOK, playerService.GetState())
}

func (router *Router) handlePlayerStateFlush(response http.ResponseWriter, request *http.Request) {
	playerService, ok := router.authenticatedPlayerService(response, request)
	if !ok {
		return
	}

	if err := playerService.FlushState(request.Context()); err != nil {
		writeError(response, request, http.StatusInternalServerError, "state_flush_failed", err.Error())
		return
	}

	writeJSON(response, http.StatusOK, map[string]string{
		"status": "ok",
	})
}

func (router *Router) handleDevPlayerReset(response http.ResponseWriter, request *http.Request) {
	session, ok := router.authenticatedSession(response, request)
	if !ok {
		return
	}

	playerService, err := router.playerManager.ResetPlayer(request.Context(), session.PlayerID)
	if err != nil {
		writeError(response, request, http.StatusInternalServerError, "player_reset_failed", err.Error())
		return
	}

	writeJSON(response, http.StatusOK, map[string]any{
		"status":         "ok",
		"playerId":       session.PlayerID,
		"playerSnapshot": playerService.GetSnapshot(),
	})
}

func (router *Router) handleGuestAuth(response http.ResponseWriter, request *http.Request) {
	if token := sessionTokenFromRequest(request); token != "" {
		session, err := router.authService.ValidateSession(request.Context(), token)
		if err == nil {
			playerService, ok := router.playerServiceForSession(response, request, session)
			if !ok {
				return
			}

			writeJSON(response, http.StatusOK, playerService.GuestAuth(token))
			return
		}
	}

	session, err := router.authService.IssueGuestSession(request.Context(), request.UserAgent())
	if err != nil {
		writeError(response, request, http.StatusInternalServerError, "guest_auth_failed", err.Error())
		return
	}

	playerService, ok := router.playerServiceForSession(response, request, session)
	if !ok {
		return
	}

	writeJSON(response, http.StatusOK, playerService.GuestAuth(session.Token))
}

func (router *Router) handleLogout(response http.ResponseWriter, request *http.Request) {
	token := sessionTokenFromRequest(request)
	if token == "" {
		writeError(response, request, http.StatusUnauthorized, "missing_session", "Bearer session token is required.")
		return
	}

	session, err := router.authService.RevokeSession(request.Context(), token)
	if err != nil {
		writeError(response, request, http.StatusUnauthorized, "invalid_session", "Session token is invalid or expired.")
		return
	}

	stateFlushed, err := router.playerManager.FlushPlayerIfLoaded(request.Context(), session.PlayerID)
	if err != nil {
		writeError(response, request, http.StatusInternalServerError, "logout_flush_failed", err.Error())
		return
	}

	writeJSON(response, http.StatusOK, map[string]any{
		"status":       "ok",
		"playerId":     session.PlayerID,
		"stateFlushed": stateFlushed,
	})
}

func (router *Router) handleCampaignFight(response http.ResponseWriter, request *http.Request) {
	router.writeGameplayAction(response, request, "", func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.FightCampaignWithRequest(request.Context(), action)
	})
}

func (router *Router) handleOfflineClaim(response http.ResponseWriter, request *http.Request) {
	router.writeGameplayAction(response, request, "", func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.ClaimAFKRewardsWithRequest(request.Context(), action)
	})
}

func (router *Router) handleDungeonRun(response http.ResponseWriter, request *http.Request) {
	router.writeGameplayAction(response, request, "", func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.RunDungeonWithRequest(request.Context(), action, request.PathValue("dungeon_id"))
	})
}

func (router *Router) handleHeroLevel(response http.ResponseWriter, request *http.Request) {
	router.writeGameplayAction(response, request, "", func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.LevelHeroWithRequest(request.Context(), action, request.PathValue("hero_id"))
	})
}

func (router *Router) handleHeroAscend(response http.ResponseWriter, request *http.Request) {
	router.writeGameplayAction(response, request, "", func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.AscendHeroWithRequest(request.Context(), action, request.PathValue("hero_id"))
	})
}

func (router *Router) handleEquipmentLevel(response http.ResponseWriter, request *http.Request) {
	router.writeGameplayAction(response, request, "", func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.LevelEquipmentWithRequest(request.Context(), action, request.PathValue("equipment_id"))
	})
}

func (router *Router) handleAccessoryEquip(response http.ResponseWriter, request *http.Request) {
	accessoryRequest, rawBody, ok := decodeAccessoryRequest(response, request)
	if !ok {
		return
	}

	router.writeGameplayAction(response, request, rawBody, func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.EquipAccessoryWithRequest(request.Context(), action, accessoryRequest.AccessoryID)
	})
}

func (router *Router) handleAccessoryLevel(response http.ResponseWriter, request *http.Request) {
	accessoryRequest, rawBody, ok := decodeAccessoryRequest(response, request)
	if !ok {
		return
	}

	router.writeGameplayAction(response, request, rawBody, func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.LevelAccessoryWithRequest(request.Context(), action, accessoryRequest.AccessoryID)
	})
}

func (router *Router) handleAccessoryFuse(response http.ResponseWriter, request *http.Request) {
	accessoryRequest, rawBody, ok := decodeAccessoryRequest(response, request)
	if !ok {
		return
	}

	router.writeGameplayAction(response, request, rawBody, func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.FuseAccessoryWithRequest(request.Context(), action, accessoryRequest.AccessoryID)
	})
}

func (router *Router) handleSummonPull(response http.ResponseWriter, request *http.Request) {
	router.writeGameplayAction(response, request, "", func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.PullSummonWithRequest(request.Context(), action, request.PathValue("banner_id"))
	})
}

func (router *Router) handleDailyMissionClaim(response http.ResponseWriter, request *http.Request) {
	router.writeGameplayAction(response, request, "", func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.ClaimDailyMissionWithRequest(request.Context(), action, request.PathValue("mission_id"))
	})
}

func (router *Router) handleBattlePassClaim(response http.ResponseWriter, request *http.Request) {
	router.writeGameplayAction(response, request, "", func(playerService *player.Service, action player.ActionRequest) api.ActionResult {
		return playerService.ClaimBattlePassRewardWithRequest(request.Context(), action, request.PathValue("reward_id"))
	})
}

func writeJSON(response http.ResponseWriter, statusCode int, payload any) {
	response.Header().Set("Content-Type", "application/json")
	response.WriteHeader(statusCode)

	if err := json.NewEncoder(response).Encode(payload); err != nil {
		http.Error(response, "failed to encode response", http.StatusInternalServerError)
	}
}

func writeError(response http.ResponseWriter, request *http.Request, statusCode int, errorCode string, message string) {
	writeJSON(response, statusCode, api.ErrorResponse{
		ErrorCode: errorCode,
		Message:   message,
		RequestID: requestIDFromContext(request.Context()),
	})
}

func writeActionResult(response http.ResponseWriter, result any) {
	writeJSON(response, http.StatusOK, result)
}

func (router *Router) writeGameplayAction(response http.ResponseWriter, request *http.Request, rawBody string, run func(*player.Service, player.ActionRequest) api.ActionResult) {
	playerService, ok := router.authenticatedPlayerService(response, request)
	if !ok {
		return
	}

	action, ok := router.actionRequest(response, request, rawBody)
	if !ok {
		return
	}

	writeActionResult(response, run(playerService, action))
}

func (router *Router) authenticatedPlayerService(response http.ResponseWriter, request *http.Request) (*player.Service, bool) {
	session, ok := router.authenticatedSession(response, request)
	if !ok {
		return nil, false
	}

	return router.playerServiceForSession(response, request, session)
}

func (router *Router) authenticatedSession(response http.ResponseWriter, request *http.Request) (auth.Session, bool) {
	token := sessionTokenFromRequest(request)
	if token == "" {
		writeError(response, request, http.StatusUnauthorized, "missing_session", "Bearer session token is required.")
		return auth.Session{}, false
	}

	session, err := router.authService.ValidateSession(request.Context(), token)
	if err != nil {
		writeError(response, request, http.StatusUnauthorized, "invalid_session", "Session token is invalid or expired.")
		return auth.Session{}, false
	}

	return session, true
}

func (router *Router) playerServiceForSession(response http.ResponseWriter, request *http.Request, session auth.Session) (*player.Service, bool) {
	playerService, err := router.playerManager.ServiceForPlayer(request.Context(), session.PlayerID)
	if err != nil {
		writeError(response, request, http.StatusInternalServerError, "player_context_failed", err.Error())
		return nil, false
	}

	return playerService, true
}

func sessionTokenFromRequest(request *http.Request) string {
	authorization := strings.TrimSpace(request.Header.Get("Authorization"))
	if authorization != "" {
		parts := strings.Fields(authorization)
		if len(parts) == 2 && strings.EqualFold(parts[0], "Bearer") {
			return strings.TrimSpace(parts[1])
		}
	}

	return strings.TrimSpace(request.Header.Get("X-Session-Token"))
}

func (router *Router) actionRequest(response http.ResponseWriter, request *http.Request, rawBody string) (player.ActionRequest, bool) {
	action, errorCode, message := buildActionRequest(request, rawBody)
	if errorCode != "" {
		writeError(response, request, http.StatusBadRequest, errorCode, message)
		return player.ActionRequest{}, false
	}
	if router.config.RequireIdempotency && !action.HasIdempotency() {
		writeError(response, request, http.StatusBadRequest, "missing_idempotency_key", "Gameplay mutation requests require an Idempotency-Key header.")
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
		writeError(response, request, http.StatusBadRequest, "invalid_body", "Could not read request body.")
		return api.AccessoryRequest{}, "", false
	}

	var accessoryRequest api.AccessoryRequest
	if err := json.Unmarshal(rawBody, &accessoryRequest); err != nil {
		writeError(response, request, http.StatusBadRequest, "invalid_json", "Expected JSON body with accessoryId.")
		return api.AccessoryRequest{}, string(rawBody), false
	}

	if accessoryRequest.AccessoryID == "" {
		writeError(response, request, http.StatusBadRequest, "missing_accessory_id", "Expected accessoryId.")
		return api.AccessoryRequest{}, string(rawBody), false
	}

	return accessoryRequest, string(rawBody), true
}
