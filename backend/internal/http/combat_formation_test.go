package apihttp

import (
	"context"
	"encoding/json"
	"log"
	"net/http"
	"net/http/httptest"
	"reflect"
	"strings"
	"testing"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/config"
	"github.com/hamzasnc/mythwake/backend/internal/player"
)

func TestCombatEndpointsHonorOrderedFormationAndBodyIdempotency(t *testing.T) {
	for _, path := range []string{"/campaign/fight", "/dungeons/gold_dungeon/run", "/dungeons/tower_dungeon/run?floor=1"} {
		t.Run(path, func(t *testing.T) {
			store := &combatHTTPTestStore{records: map[string]player.StoredActionResult{}}
			handler := NewRouter(config.Config{ServiceName: "test", Environment: "test", RequireIdempotency: true}, log.New(testWriter{}, "", 0), nil, player.NewManager(store))
			login := loginGuest(t, handler)
			send := func(body string) api.ActionResult {
				t.Helper()
				response := httptest.NewRecorder()
				request := httptest.NewRequest(http.MethodPost, path, strings.NewReader(body))
				addAuth(request, login.SessionToken)
				addIdempotencyKey(request, "formation-combat-001")
				handler.ServeHTTP(response, request)
				if response.Code != http.StatusOK {
					t.Fatalf("unexpected HTTP result: %d %s", response.Code, response.Body.String())
				}
				var result api.ActionResult
				if err := json.Unmarshal(response.Body.Bytes(), &result); err != nil {
					t.Fatal(err)
				}
				return result
			}
			body := `{"heroIds":["hero_kael","hero_astra"]}`
			first := send(body)
			if first.Combat == nil || len(first.Combat.Heroes) != 2 || first.Combat.Heroes[0].HeroID != "hero_kael" || first.Combat.Heroes[1].HeroID != "hero_astra" {
				t.Fatalf("endpoint did not use frozen formation: %#v", first)
			}
			if first.Combat.TeamAttack != 36 || first.Combat.TeamMaxHP != 300 || first.PlayerState.TeamAttack <= first.Combat.TeamAttack {
				t.Fatalf("combat stats leaked to account or included bench: %#v", first)
			}
			replayed := send(body)
			if !replayed.Replay || !reflect.DeepEqual(first.Combat, replayed.Combat) || first.Receipt.StateRevision != replayed.Receipt.StateRevision {
				t.Fatal("identical retry repeated action or changed replay")
			}
			conflict := send(`{"heroIds":["hero_astra","hero_kael"]}`)
			if conflict.ErrorCode != "idempotency_conflict" {
				t.Fatalf("formation body/order was absent from request hash: %#v", conflict)
			}
		})
	}
}

// This fixture includes the action-result store used in deployed persistence;
// the general router fixture intentionally has no store and cannot replay keys.
type combatHTTPTestStore struct {
	records map[string]player.StoredActionResult
}

func (store *combatHTTPTestStore) LoadState(context.Context, string) (player.PersistentState, bool, error) {
	return player.PersistentState{}, false, nil
}

func (store *combatHTTPTestStore) SaveState(_ context.Context, playerID string, _ player.PersistentState, source player.StateSaveSource) error {
	if source.ActionResult != nil {
		store.records[playerID+":"+source.IdempotencyKey] = player.StoredActionResult{ActionID: source.ActionID, RequestHash: source.RequestHash, ActionResult: *source.ActionResult}
	}
	return nil
}

func (store *combatHTTPTestStore) LoadActionResult(_ context.Context, playerID, key string) (player.StoredActionResult, bool, error) {
	record, ok := store.records[playerID+":"+key]
	return record, ok, nil
}

func TestCombatBodyRejectsMalformedJSONAndInvalidFormation(t *testing.T) {
	for _, body := range []string{`{`, `[]`, `null`, `{"heroIds":1}`, `{"heroIds":[]}`, `{"heroIds":["unknown"]}`, `{"heroIds":["hero_kael","hero_kael"]}`} {
		t.Run(body, func(t *testing.T) {
			handler := newTestHandler()
			login := loginGuest(t, handler)
			response := httptest.NewRecorder()
			request := httptest.NewRequest(http.MethodPost, "/campaign/fight", strings.NewReader(body))
			addAuth(request, login.SessionToken)
			addIdempotencyKey(request, "formation-invalid-001")
			handler.ServeHTTP(response, request)
			if response.Code == http.StatusBadRequest {
				return
			}
			var result api.ActionResult
			if err := json.Unmarshal(response.Body.Bytes(), &result); err != nil {
				t.Fatal(err)
			}
			if result.Success || result.ErrorCode != "invalid_formation" || result.Combat != nil || result.PlayerState.CampaignStage != 1 {
				t.Fatalf("invalid body produced combat or changed state: %#v", result)
			}
		})
	}
}

func TestOmittedCombatBodyPreservesLegacySeven(t *testing.T) {
	handler := newTestHandler()
	login := loginGuest(t, handler)
	request := httptest.NewRequest(http.MethodPost, "/campaign/fight", nil)
	addAuth(request, login.SessionToken)
	addIdempotencyKey(request, "legacy-seven-001")
	response := httptest.NewRecorder()
	handler.ServeHTTP(response, request)
	var result api.ActionResult
	if err := json.Unmarshal(response.Body.Bytes(), &result); err != nil {
		t.Fatal(err)
	}
	if result.Combat == nil || len(result.Combat.Heroes) != 7 || len(result.PlayerSnapshot.Heroes) != 8 {
		t.Fatalf("starter grant changed legacy combat slots: %#v", result)
	}
	for _, hero := range result.Combat.Heroes {
		if hero.HeroID == "hero_kael" {
			t.Fatal("old request silently fielded Kael")
		}
	}
}
