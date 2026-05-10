package apihttp

import (
	"encoding/json"
	"log"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/config"
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
	return NewRouter(
		config.Config{
			ServiceName:        "test-api",
			Addr:               ":0",
			Environment:        "test",
			Version:            "test",
			RequireIdempotency: true,
		},
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
