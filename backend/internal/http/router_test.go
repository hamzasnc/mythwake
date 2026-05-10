package apihttp

import (
	"context"
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/config"
	"github.com/hamzasnc/mythwake/backend/internal/definitions"
	"github.com/hamzasnc/mythwake/backend/internal/gameplay"
	"github.com/hamzasnc/mythwake/backend/internal/player"
)

func TestHealthEndpoint(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/health", nil)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}
	if response.Header().Get("X-Request-ID") == "" {
		t.Fatal("expected request id response header")
	}
}

func TestServerClockEndpoint(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/time", nil)
	request.Header.Set("X-Request-ID", "clock-request-001")

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}
	if response.Header().Get("X-Request-ID") != "clock-request-001" {
		t.Fatalf("expected request id header, got %q", response.Header().Get("X-Request-ID"))
	}

	var body api.ServerClockResponse
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}

	serverTime, err := time.Parse(time.RFC3339, body.ServerTimeUTC)
	if err != nil {
		t.Fatalf("expected RFC3339 server time, got %q", body.ServerTimeUTC)
	}
	dailyReset, err := time.Parse(time.RFC3339, body.DailyResetUTC)
	if err != nil {
		t.Fatalf("expected RFC3339 daily reset time, got %q", body.DailyResetUTC)
	}
	weeklyReset, err := time.Parse(time.RFC3339, body.WeeklyResetUTC)
	if err != nil {
		t.Fatalf("expected RFC3339 weekly reset time, got %q", body.WeeklyResetUTC)
	}

	if body.ServerUnixMs <= 0 || !dailyReset.After(serverTime) || !weeklyReset.After(serverTime) {
		t.Fatalf("expected future reset times, got %#v", body)
	}
	if body.SecondsUntilDailyReset <= 0 || body.SecondsUntilWeeklyReset <= 0 {
		t.Fatalf("expected positive reset countdowns, got %#v", body)
	}
}

func TestServerClockUsesUTCResetBoundaries(t *testing.T) {
	now := time.Date(2026, 5, 10, 22, 30, 15, 0, time.UTC)
	clock := serverClock(now)

	if clock.ServerTimeUTC != "2026-05-10T22:30:15Z" {
		t.Fatalf("expected exact server time, got %#v", clock)
	}
	if clock.DailyResetUTC != "2026-05-11T00:00:00Z" {
		t.Fatalf("expected next daily reset, got %#v", clock)
	}
	if clock.WeeklyResetUTC != "2026-05-11T00:00:00Z" {
		t.Fatalf("expected next weekly reset, got %#v", clock)
	}
	if clock.SecondsUntilDailyReset != 5385 || clock.SecondsUntilWeeklyReset != 5385 {
		t.Fatalf("expected reset countdowns from fixed time, got %#v", clock)
	}
}

func TestRequestIDHeaderIsPreserved(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/health", nil)
	request.Header.Set("X-Request-ID", "client-request-001")

	handler.ServeHTTP(response, request)

	if response.Header().Get("X-Request-ID") != "client-request-001" {
		t.Fatalf("expected client request id to be preserved, got %q", response.Header().Get("X-Request-ID"))
	}
}

func TestInvalidRequestIDHeaderIsReplaced(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/health", nil)
	request.Header.Set("X-Request-ID", "bad id")

	handler.ServeHTTP(response, request)

	requestID := response.Header().Get("X-Request-ID")
	if requestID == "" || requestID == "bad id" {
		t.Fatalf("expected invalid request id to be replaced, got %q", requestID)
	}
}

func TestErrorResponseIncludesRequestID(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/player/state", nil)
	request.Header.Set("X-Request-ID", "client-request-401")

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusUnauthorized {
		t.Fatalf("expected status 401, got %d", response.Code)
	}

	var body api.ErrorResponse
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body.ErrorCode != "missing_session" || body.RequestID != "client-request-401" {
		t.Fatalf("expected request id error response, got %#v", body)
	}
}

func TestPanicRecoveryReturnsJSONError(t *testing.T) {
	handler := withRequestID(logRequests(log.New(testWriter{}, "", 0), recoverPanic(log.New(testWriter{}, "", 0), http.HandlerFunc(func(http.ResponseWriter, *http.Request) {
		panic("test panic")
	}))))
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/panic", nil)
	request.Header.Set("X-Request-ID", "panic-request-001")

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusInternalServerError {
		t.Fatalf("expected status 500, got %d", response.Code)
	}
	if response.Header().Get("X-Request-ID") != "panic-request-001" {
		t.Fatalf("expected request id header, got %q", response.Header().Get("X-Request-ID"))
	}

	var body api.ErrorResponse
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body.ErrorCode != "internal_error" || body.RequestID != "panic-request-001" {
		t.Fatalf("expected internal error with request id, got %#v", body)
	}
}

func TestAuthRateLimit(t *testing.T) {
	handler := newTestHandlerWithConfig(config.Config{
		RateLimitEnabled: true,
		RateLimitWindow:  time.Minute,
		RateLimitAuth:    2,
	})

	for i := 0; i < 2; i++ {
		response := httptest.NewRecorder()
		request := httptest.NewRequest(http.MethodPost, "/auth/guest", nil)
		handler.ServeHTTP(response, request)
		if response.Code != http.StatusOK {
			t.Fatalf("expected auth request %d to pass, got %d", i+1, response.Code)
		}
	}

	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/auth/guest", nil)
	request.Header.Set("X-Request-ID", "auth-limit-001")
	handler.ServeHTTP(response, request)

	if response.Code != http.StatusTooManyRequests {
		t.Fatalf("expected rate limited auth status 429, got %d", response.Code)
	}
	if response.Header().Get("Retry-After") == "" {
		t.Fatal("expected Retry-After header")
	}

	var body api.ErrorResponse
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body.ErrorCode != "rate_limited" || body.RequestID != "auth-limit-001" {
		t.Fatalf("expected rate limit error with request id, got %#v", body)
	}
}

func TestGameplayRateLimitUsesSessionToken(t *testing.T) {
	handler := newTestHandlerWithConfig(config.Config{
		RateLimitEnabled:  true,
		RateLimitWindow:   time.Minute,
		RateLimitGameplay: 1,
	})
	login := loginGuest(t, handler)

	first := httptest.NewRecorder()
	firstRequest := httptest.NewRequest(http.MethodPost, "/campaign/fight", nil)
	addAuth(firstRequest, login.SessionToken)
	addIdempotencyKey(firstRequest, "campaign-rate-001")
	handler.ServeHTTP(first, firstRequest)
	if first.Code != http.StatusOK {
		t.Fatalf("expected first gameplay request to pass, got %d", first.Code)
	}

	second := httptest.NewRecorder()
	secondRequest := httptest.NewRequest(http.MethodPost, "/campaign/fight", nil)
	secondRequest.Header.Set("X-Request-ID", "gameplay-limit-001")
	addAuth(secondRequest, login.SessionToken)
	addIdempotencyKey(secondRequest, "campaign-rate-002")
	handler.ServeHTTP(second, secondRequest)

	if second.Code != http.StatusTooManyRequests {
		t.Fatalf("expected rate limited gameplay status 429, got %d", second.Code)
	}

	var body api.ErrorResponse
	if err := json.NewDecoder(second.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body.ErrorCode != "rate_limited" || body.RequestID != "gameplay-limit-001" {
		t.Fatalf("expected gameplay rate limit error with request id, got %#v", body)
	}
}

func TestDefinitionsEndpoint(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/definitions", nil)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}

	var body api.DefinitionSnapshot
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}

	if body.APIVersion != "test" || body.SchemaVersion == 0 {
		t.Fatalf("expected versioned definition snapshot, got %#v", body)
	}
	if body.ContentHash == "" {
		t.Fatalf("expected content hash, got %#v", body)
	}
	if len(body.Dungeons) == 0 || len(body.SummonBanners) == 0 || len(body.GameplayActions) == 0 {
		t.Fatalf("expected populated definitions, got %#v", body)
	}
	if response.Header().Get("ETag") == "" {
		t.Fatalf("expected ETag header")
	}
	if response.Header().Get("Cache-Control") == "" {
		t.Fatalf("expected Cache-Control header")
	}
}

func TestDefinitionsEndpointCanUseInjectedProvider(t *testing.T) {
	handler := NewRouter(
		config.Config{ServiceName: "test-api", Addr: ":0", Environment: "test", Version: "db-backed-test"},
		log.New(testWriter{}, "", 0),
		nil,
		player.NewManager(nil),
		WithDefinitionProvider(fixedDefinitionProvider{}),
	)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/definitions", nil)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}

	var body api.DefinitionSnapshot
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body.APIVersion != "db-backed-test" || len(body.Dungeons) != 1 || body.Dungeons[0].DungeonID != "db_gold" {
		t.Fatalf("expected injected definition catalog, got %#v", body)
	}
	if body.ContentHash == "" || response.Header().Get("ETag") == "" {
		t.Fatalf("expected hashed injected catalog, got %#v", body)
	}
}

func TestDefinitionsEndpointReportsProviderFailures(t *testing.T) {
	handler := NewRouter(
		config.Config{ServiceName: "test-api", Addr: ":0", Environment: "test", Version: "test"},
		log.New(testWriter{}, "", 0),
		nil,
		player.NewManager(nil),
		WithDefinitionProvider(failingDefinitionProvider{}),
	)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/definitions", nil)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusInternalServerError {
		t.Fatalf("expected status 500, got %d", response.Code)
	}

	var body api.ErrorResponse
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body.ErrorCode != "definitions_unavailable" {
		t.Fatalf("expected definitions error, got %#v", body)
	}
}

func TestDefinitionsEndpointSupportsETagRevalidation(t *testing.T) {
	handler := newTestHandler()

	firstResponse := httptest.NewRecorder()
	firstRequest := httptest.NewRequest(http.MethodGet, "/definitions", nil)
	handler.ServeHTTP(firstResponse, firstRequest)
	etag := firstResponse.Header().Get("ETag")
	if etag == "" {
		t.Fatal("expected initial ETag")
	}

	secondResponse := httptest.NewRecorder()
	secondRequest := httptest.NewRequest(http.MethodGet, "/definitions", nil)
	secondRequest.Header.Set("If-None-Match", etag)
	handler.ServeHTTP(secondResponse, secondRequest)

	if secondResponse.Code != http.StatusNotModified {
		t.Fatalf("expected status 304, got %d", secondResponse.Code)
	}
	if secondResponse.Body.Len() != 0 {
		t.Fatalf("expected empty 304 body, got %q", secondResponse.Body.String())
	}
}

func TestDefinitionsEndpointAcceptsWeakAndListedETags(t *testing.T) {
	handler := newTestHandler()

	firstResponse := httptest.NewRecorder()
	firstRequest := httptest.NewRequest(http.MethodGet, "/definitions", nil)
	handler.ServeHTTP(firstResponse, firstRequest)
	etag := firstResponse.Header().Get("ETag")
	if etag == "" {
		t.Fatal("expected initial ETag")
	}

	secondResponse := httptest.NewRecorder()
	secondRequest := httptest.NewRequest(http.MethodGet, "/definitions", nil)
	secondRequest.Header.Set("If-None-Match", `"older-definition", W/`+etag)
	handler.ServeHTTP(secondResponse, secondRequest)

	if secondResponse.Code != http.StatusNotModified {
		t.Fatalf("expected status 304, got %d", secondResponse.Code)
	}
}

func TestGuestAuthEndpoint(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/auth/guest", nil)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}

	var body api.GuestAuthResponse
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}

	if body.PlayerID == "" || body.SessionToken == "" {
		t.Fatalf("expected guest player and session token, got %#v", body)
	}
	if !strings.HasPrefix(body.SessionToken, "mw_sess_") {
		t.Fatalf("expected issued session token, got %#v", body)
	}
	if len(body.PlayerSnapshot.Heroes) == 0 {
		t.Fatalf("expected guest auth to include player snapshot, got %#v", body)
	}
}

func TestLogoutEndpointRevokesSession(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)

	logoutResponse := httptest.NewRecorder()
	logoutRequest := httptest.NewRequest(http.MethodPost, "/auth/logout", nil)
	addAuth(logoutRequest, login.SessionToken)
	handler.ServeHTTP(logoutResponse, logoutRequest)

	if logoutResponse.Code != http.StatusOK {
		t.Fatalf("expected logout status 200, got %d", logoutResponse.Code)
	}
	var logoutBody map[string]any
	if err := json.NewDecoder(logoutResponse.Body).Decode(&logoutBody); err != nil {
		t.Fatalf("decode logout response: %v", err)
	}
	if logoutBody["stateFlushed"] != true {
		t.Fatalf("expected logout to flush loaded player state, got %#v", logoutBody)
	}

	stateResponse := httptest.NewRecorder()
	stateRequest := httptest.NewRequest(http.MethodGet, "/player/state", nil)
	addAuth(stateRequest, login.SessionToken)
	handler.ServeHTTP(stateResponse, stateRequest)

	if stateResponse.Code != http.StatusUnauthorized {
		t.Fatalf("expected revoked session to return 401, got %d", stateResponse.Code)
	}
}

func TestCampaignFightEndpoint(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/campaign/fight", nil)
	addAuth(request, login.SessionToken)
	addIdempotencyKey(request, "campaign-fight-001")

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}

	var body api.ActionResult
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}

	if !body.Success || body.ActionID != gameplay.ActionCampaignFight {
		t.Fatalf("expected successful campaign fight, got %#v", body)
	}
	if len(body.PlayerSnapshot.Heroes) == 0 || body.PlayerSnapshot.State.CampaignStage != body.PlayerState.CampaignStage {
		t.Fatalf("expected action result snapshot to match player state, got %#v", body)
	}
}

func TestOfflineClaimEndpoint(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/player/offline/claim", nil)
	addAuth(request, login.SessionToken)
	addIdempotencyKey(request, "offline-claim-001")

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}

	var body api.ActionResult
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body.ActionID != gameplay.ActionAFKRewardClaim {
		t.Fatalf("expected afk claim action, got %#v", body)
	}
	if body.PlayerSnapshot.LastAFKClaimUTC == "" {
		t.Fatalf("expected afk claim timestamp in snapshot, got %#v", body.PlayerSnapshot)
	}
}

func TestMutatingActionRequiresIdempotencyHeader(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/campaign/fight", nil)
	addAuth(request, login.SessionToken)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusBadRequest {
		t.Fatalf("expected status 400, got %d", response.Code)
	}

	var body map[string]string
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body["errorCode"] != "missing_idempotency_key" {
		t.Fatalf("expected missing idempotency error, got %#v", body)
	}
}

func TestProtectedEndpointRequiresSession(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/player/state", nil)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusUnauthorized {
		t.Fatalf("expected status 401, got %d", response.Code)
	}

	var body map[string]string
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body["errorCode"] != "missing_session" {
		t.Fatalf("expected missing session error, got %#v", body)
	}
}

func TestProtectedEndpointRejectsInvalidSession(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/player/state", nil)
	addAuth(request, "mw_sess_invalid")

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusUnauthorized {
		t.Fatalf("expected status 401, got %d", response.Code)
	}

	var body map[string]string
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body["errorCode"] != "invalid_session" {
		t.Fatalf("expected invalid session error, got %#v", body)
	}
}

func TestMutatingActionRejectsInvalidIdempotencyHeader(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/campaign/fight", nil)
	addAuth(request, login.SessionToken)
	addIdempotencyKey(request, "bad key with spaces")

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusBadRequest {
		t.Fatalf("expected status 400, got %d", response.Code)
	}

	var body map[string]string
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body["errorCode"] != "invalid_idempotency_key" {
		t.Fatalf("expected invalid idempotency error, got %#v", body)
	}
}

func TestMutatingActionAcceptsLegacyIdempotencyHeader(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/campaign/fight", nil)
	addAuth(request, login.SessionToken)
	request.Header.Set("X-Idempotency-Key", "legacy-campaign-001")

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}

	var body api.ActionResult
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body.IdempotencyKey != "legacy-campaign-001" {
		t.Fatalf("expected legacy idempotency key in response, got %#v", body)
	}
}

func TestPlayerStateEndpointReturnsSnapshot(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/player/state", nil)
	addAuth(request, login.SessionToken)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}

	var body api.PlayerSnapshot
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}

	if body.PlayerID == "" || len(body.Heroes) == 0 || len(body.Equipment) == 0 {
		t.Fatalf("expected rich player snapshot, got %#v", body)
	}
}

func TestPlayerStateFlushEndpoint(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/player/state/flush", nil)
	addAuth(request, login.SessionToken)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}
}

func TestDevPlayerResetEndpointIsLocalOnly(t *testing.T) {
	handler := newTestHandlerWithConfig(config.Config{DevToolsEnabled: false})
	login := loginGuest(t, handler)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/dev/player/reset", nil)
	addAuth(request, login.SessionToken)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusNotFound {
		t.Fatalf("expected status 404 when dev tools are disabled, got %d", response.Code)
	}
}

func TestDevPlayerResetEndpointResetsHotPlayer(t *testing.T) {
	handler := newTestHandlerWithConfig(config.Config{DevToolsEnabled: true})
	login := loginGuest(t, handler)

	fightResponse := httptest.NewRecorder()
	fightRequest := httptest.NewRequest(http.MethodPost, "/campaign/fight", nil)
	addAuth(fightRequest, login.SessionToken)
	addIdempotencyKey(fightRequest, "dev-reset-fight-001")
	handler.ServeHTTP(fightResponse, fightRequest)
	if fightResponse.Code != http.StatusOK {
		t.Fatalf("expected fight status 200, got %d", fightResponse.Code)
	}

	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/dev/player/reset", nil)
	addAuth(request, login.SessionToken)
	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}

	var body struct {
		Status         string             `json:"status"`
		PlayerID       string             `json:"playerId"`
		PlayerSnapshot api.PlayerSnapshot `json:"playerSnapshot"`
	}
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if body.Status != "ok" || body.PlayerID != login.PlayerID {
		t.Fatalf("expected reset status for login player, got %#v", body)
	}
	if body.PlayerSnapshot.State.CampaignStage != 1 || body.PlayerSnapshot.State.Gems != 35 {
		t.Fatalf("expected fresh player snapshot after reset, got %#v", body.PlayerSnapshot.State)
	}

	stateResponse := httptest.NewRecorder()
	stateRequest := httptest.NewRequest(http.MethodGet, "/player/state", nil)
	addAuth(stateRequest, login.SessionToken)
	handler.ServeHTTP(stateResponse, stateRequest)
	if stateResponse.Code != http.StatusOK {
		t.Fatalf("expected post-reset state status 200, got %d", stateResponse.Code)
	}
	var state api.PlayerSnapshot
	if err := json.NewDecoder(stateResponse.Body).Decode(&state); err != nil {
		t.Fatalf("decode state response: %v", err)
	}
	if state.State.CampaignStage != 1 || len(state.Heroes) == 0 {
		t.Fatalf("expected reset state to stay fresh through active session, got %#v", state)
	}
}

func TestAccessoryBodyValidation(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/gear/accessories/equip", strings.NewReader(`{}`))
	addAuth(request, login.SessionToken)
	addIdempotencyKey(request, "accessory-equip-001")

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusBadRequest {
		t.Fatalf("expected status 400, got %d", response.Code)
	}
}

func newTestHandler() http.Handler {
	return newTestHandlerWithConfig(config.Config{})
}

func newTestHandlerWithConfig(cfg config.Config) http.Handler {
	if cfg.ServiceName == "" {
		cfg.ServiceName = "test-api"
	}
	if cfg.Addr == "" {
		cfg.Addr = ":0"
	}
	if cfg.Environment == "" {
		cfg.Environment = "test"
	}
	if cfg.Version == "" {
		cfg.Version = "test"
	}
	cfg.RequireIdempotency = true

	return NewRouter(
		cfg,
		log.New(testWriter{}, "", 0),
		nil,
		player.NewManager(nil),
	)
}

func loginGuest(t testing.TB, handler http.Handler) api.GuestAuthResponse {
	t.Helper()

	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/auth/guest", nil)
	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected guest login status 200, got %d", response.Code)
	}

	var body api.GuestAuthResponse
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode guest login response: %v", err)
	}
	if body.SessionToken == "" {
		t.Fatalf("expected session token in guest login response, got %#v", body)
	}

	return body
}

func addAuth(request *http.Request, token string) {
	request.Header.Set("Authorization", "Bearer "+token)
}

func addIdempotencyKey(request *http.Request, key string) {
	request.Header.Set("Idempotency-Key", key)
}

type testWriter struct{}

func (testWriter) Write(bytes []byte) (int, error) {
	return len(bytes), nil
}

type fixedDefinitionProvider struct{}

func (fixedDefinitionProvider) Snapshot(_ context.Context, apiVersion string) (api.DefinitionSnapshot, error) {
	snapshot := api.DefinitionSnapshot{
		SchemaVersion: definitions.SchemaVersion,
		APIVersion:    apiVersion,
		Dungeons: []api.DungeonDefinition{
			{DungeonID: "db_gold", DisplayName: "DB Gold"},
		},
		SummonBanners: []api.SummonBannerDefinition{
			{BannerID: "db_banner", DisplayName: "DB Banner"},
		},
		GameplayActions: []api.GameplayActionDefinition{
			{ActionID: gameplay.ActionCampaignFight, Domain: "campaign", RequiresIdempotency: true},
		},
	}
	snapshot.ContentHash = definitions.ContentHash(snapshot)
	return snapshot, nil
}

type failingDefinitionProvider struct{}

func (failingDefinitionProvider) Snapshot(context.Context, string) (api.DefinitionSnapshot, error) {
	return api.DefinitionSnapshot{}, errors.New("definition provider failed")
}
