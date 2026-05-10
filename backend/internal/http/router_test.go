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
	if len(body.PlayerSnapshot.Heroes) == 0 {
		t.Fatalf("expected guest auth to include player snapshot, got %#v", body)
	}
}

func TestCampaignFightEndpoint(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/campaign/fight", nil)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}

	var body api.ActionResult
	if err := json.NewDecoder(response.Body).Decode(&body); err != nil {
		t.Fatalf("decode response: %v", err)
	}

	if !body.Success || body.ActionID != "campaign_fight" {
		t.Fatalf("expected successful campaign fight, got %#v", body)
	}
	if len(body.PlayerSnapshot.Heroes) == 0 || body.PlayerSnapshot.State.CampaignStage != body.PlayerState.CampaignStage {
		t.Fatalf("expected action result snapshot to match player state, got %#v", body)
	}
}

func TestPlayerStateEndpointReturnsSnapshot(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodGet, "/player/state", nil)

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
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/player/state/flush", nil)

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusOK {
		t.Fatalf("expected status 200, got %d", response.Code)
	}
}

func TestAccessoryBodyValidation(t *testing.T) {
	handler := newTestHandler()
	response := httptest.NewRecorder()
	request := httptest.NewRequest(http.MethodPost, "/gear/accessories/equip", strings.NewReader(`{}`))

	handler.ServeHTTP(response, request)

	if response.Code != http.StatusBadRequest {
		t.Fatalf("expected status 400, got %d", response.Code)
	}
}

func newTestHandler() http.Handler {
	return NewRouter(
		config.Config{ServiceName: "test-api", Addr: ":0", Environment: "test", Version: "test"},
		log.New(testWriter{}, "", 0),
		player.NewService(),
	)
}

type testWriter struct{}

func (testWriter) Write(bytes []byte) (int, error) {
	return len(bytes), nil
}
