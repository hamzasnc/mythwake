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
	"github.com/hamzasnc/mythwake/backend/internal/cache/actionlock"
	"github.com/hamzasnc/mythwake/backend/internal/cache/ratelimit"
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
	stateCacheStats    StateCacheStatsProvider
	rateLimiter        ratelimit.Limiter
	playerLocker       actionlock.Locker
}

type RouterOption func(*Router)

type StateCacheStats struct {
	DirtyPlayers  int
	QueuedSaves   int64
	FlushedSaves  int64
	FailedFlushes int64
	LastFlushAt   time.Time
	LastError     string
}

type StateCacheStatsProvider func() StateCacheStats

func WithDefinitionProvider(provider definitions.SnapshotProvider) RouterOption {
	return func(router *Router) {
		if provider != nil {
			router.definitionProvider = provider
		}
	}
}

func WithStateCacheStatsProvider(provider StateCacheStatsProvider) RouterOption {
	return func(router *Router) {
		if provider != nil {
			router.stateCacheStats = provider
		}
	}
}

func WithRateLimiter(limiter ratelimit.Limiter) RouterOption {
	return func(router *Router) {
		if limiter != nil {
			router.rateLimiter = limiter
		}
	}
}

func WithPlayerLocker(locker actionlock.Locker) RouterOption {
	return func(router *Router) {
		if locker != nil {
			router.playerLocker = locker
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
	if cfg.PlayerLockStore == "" {
		cfg.PlayerLockStore = config.CacheStoreMemory
	}
	if cfg.PlayerLockTTL <= 0 {
		cfg.PlayerLockTTL = 5 * time.Second
	}

	router := &Router{
		config:             cfg,
		logger:             logger,
		mux:                http.NewServeMux(),
		authService:        authService,
		playerManager:      playerManager,
		definitionProvider: definitions.NewStaticSnapshotProvider(),
		stateCacheStats:    func() StateCacheStats { return StateCacheStats{} },
		rateLimiter:        ratelimit.NewMemoryLimiter(),
		playerLocker:       actionlock.NewMemoryLocker(),
	}

	for _, option := range options {
		option(router)
	}

	router.routes()
	return withRequestID(logRequests(router.logger, recoverPanic(router.logger, withRateLimit(router.config, router.rateLimiter, router.mux))))
}

func (router *Router) routes() {
	router.mux.HandleFunc("POST /auth/guest", router.handleGuestAuth)
	router.mux.HandleFunc("POST /auth/logout", router.handleLogout)
	router.mux.HandleFunc("GET /health", router.handleHealth)
	router.mux.HandleFunc("GET /time", router.handleTime)
	router.mux.HandleFunc("GET /definitions", router.handleDefinitions)
	router.mux.HandleFunc("GET /client/bootstrap", router.handleClientBootstrap)
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
	cacheStats := router.stateCacheStats()
	lastFlushUTC := ""
	if !cacheStats.LastFlushAt.IsZero() {
		lastFlushUTC = cacheStats.LastFlushAt.UTC().Format(time.RFC3339)
	}
	sessionCacheStore := router.config.SessionCacheStore
	if sessionCacheStore == "" {
		sessionCacheStore = config.CacheStoreMemory
	}
	rateLimitStore := router.config.RateLimitStore
	if rateLimitStore == "" {
		rateLimitStore = config.CacheStoreMemory
	}
	redisStatus := router.config.RedisStatus
	if redisStatus == "" {
		redisStatus = "disabled"
	}

	writeJSON(response, http.StatusOK, map[string]string{
		"service":              router.config.ServiceName,
		"status":               "ok",
		"database":             router.config.DatabaseStatus,
		"redis":                redisStatus,
		"state_cache":          router.config.StateCacheStatus,
		"balance_catalog":      router.config.BalanceCatalog,
		"state_write_mode":     router.config.StateWriteMode,
		"state_flush_interval": router.config.StateFlushInterval.String(),
		"state_cache_dirty":    strconv.Itoa(cacheStats.DirtyPlayers),
		"state_cache_queued":   strconv.FormatInt(cacheStats.QueuedSaves, 10),
		"state_cache_flushed":  strconv.FormatInt(cacheStats.FlushedSaves, 10),
		"state_cache_failed":   strconv.FormatInt(cacheStats.FailedFlushes, 10),
		"state_cache_last_utc": lastFlushUTC,
		"state_cache_error":    cacheStats.LastError,
		"session_cache_store":  sessionCacheStore,
		"session_cache_ttl":    router.config.SessionCacheTTL.String(),
		"session_touch_window": router.config.SessionTouchWindow.String(),
		"rate_limit_store":     rateLimitStore,
		"rate_limit_enabled":   boolLabel(router.config.RateLimitEnabled),
		"rate_limit_window":    router.config.RateLimitWindow.String(),
		"rate_limit_auth":      strconv.Itoa(router.config.RateLimitAuth),
		"rate_limit_gameplay":  strconv.Itoa(router.config.RateLimitGameplay),
		"player_lock_store":    playerLockStore(router.config),
		"player_lock_ttl":      router.config.PlayerLockTTL.String(),
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

func (router *Router) handleClientBootstrap(response http.ResponseWriter, request *http.Request) {
	playerService, ok := router.authenticatedPlayerService(response, request)
	if !ok {
		return
	}

	snapshot, err := router.definitionProvider.Snapshot(request.Context(), router.config.Version)
	if err != nil {
		writeError(response, request, http.StatusInternalServerError, "definitions_unavailable", "Definition catalog is temporarily unavailable.")
		return
	}

	response.Header().Set("Cache-Control", "no-store")
	response.Header().Set("X-Definitions-ETag", definitions.ETag(snapshot))

	writeJSON(response, http.StatusOK, api.ClientBootstrapResponse{
		ServerClock:    serverClock(time.Now().UTC()),
		Definitions:    snapshot,
		PlayerSnapshot: playerService.GetSnapshot(),
	})
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
	session, ok := router.authenticatedSession(response, request)
	if !ok {
		return
	}

	action, ok := router.actionRequest(response, request, rawBody)
	if !ok {
		return
	}

	release, ok := router.acquirePlayerMutationLock(response, request, session.PlayerID)
	if !ok {
		return
	}
	defer release()

	playerService, ok := router.playerServiceForSession(response, request, session)
	if !ok {
		return
	}

	writeActionResult(response, run(playerService, action))
}

func (router *Router) acquirePlayerMutationLock(response http.ResponseWriter, request *http.Request, playerID string) (func(), bool) {
	if router.playerLocker == nil {
		return func() {}, true
	}

	lock, acquired, err := router.playerLocker.Acquire(request.Context(), playerMutationLockKey(playerID), router.config.PlayerLockTTL)
	if err != nil {
		writeError(response, request, http.StatusServiceUnavailable, "player_lock_unavailable", "Player mutation lock is temporarily unavailable.")
		return nil, false
	}
	if !acquired {
		response.Header().Set("Retry-After", strconv.Itoa(retryAfterSeconds(router.config.PlayerLockTTL)))
		writeError(response, request, http.StatusConflict, "player_busy", "Another player action is already being processed.")
		return nil, false
	}

	return func() {
		if err := lock.Release(request.Context()); err != nil {
			router.logger.Printf("request_id=%s player_id=%s lock_release_failed=%v", requestIDFromContext(request.Context()), playerID, err)
		}
	}, true
}

func playerMutationLockKey(playerID string) string {
	return "player:" + playerID + ":mutation"
}

func playerLockStore(cfg config.Config) string {
	if cfg.PlayerLockStore == "" {
		return config.CacheStoreMemory
	}

	return cfg.PlayerLockStore
}

func retryAfterSeconds(duration time.Duration) int {
	if duration <= 0 {
		return 1
	}

	seconds := int(duration.Seconds())
	if time.Duration(seconds)*time.Second < duration {
		seconds++
	}
	if seconds < 1 {
		return 1
	}

	return seconds
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
	expectedRevision, errorCode, message := expectedStateRevision(request)
	if errorCode != "" {
		return player.ActionRequest{}, errorCode, message
	}

	idempotencyKey := strings.TrimSpace(request.Header.Get("Idempotency-Key"))
	if idempotencyKey == "" {
		idempotencyKey = strings.TrimSpace(request.Header.Get("X-Idempotency-Key"))
	}
	if idempotencyKey == "" {
		return player.ActionRequest{ExpectedRevision: expectedRevision}, "", ""
	}
	if !validIdempotencyKey(idempotencyKey) {
		return player.ActionRequest{}, "invalid_idempotency_key", "Idempotency-Key must be 8-128 chars and only contain letters, numbers, dot, underscore, colon, or dash."
	}

	hashInput := request.Method + "\n" + request.URL.EscapedPath() + "\n" + request.URL.RawQuery + "\n" + rawBody + "\n" + strconv.FormatInt(expectedRevision, 10)
	hash := sha256.Sum256([]byte(hashInput))
	return player.ActionRequest{
		IdempotencyKey:   idempotencyKey,
		RequestHash:      hex.EncodeToString(hash[:]),
		ExpectedRevision: expectedRevision,
	}, "", ""
}

func expectedStateRevision(request *http.Request) (int64, string, string) {
	value := strings.TrimSpace(request.Header.Get("X-Player-State-Revision"))
	if value == "" {
		return 0, "", ""
	}

	revision, err := strconv.ParseInt(value, 10, 64)
	if err != nil || revision <= 0 {
		return 0, "invalid_state_revision", "X-Player-State-Revision must be a positive integer."
	}

	return revision, "", ""
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
