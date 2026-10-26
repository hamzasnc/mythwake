// Package e2e tests a running server end to end (API + rules service + PostgreSQL).
//
// Start the stack (server/dev.ps1 or dev.sh), then:
//
//	MYTHWAKE_E2E_URL=http://127.0.0.1:8080 MYTHWAKE_DATABASE_URL=postgres://mythwake@127.0.0.1:55432/mythwake go test ./e2e
package e2e

import (
	"bytes"
	"context"
	"crypto/rand"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"testing"

	"github.com/jackc/pgx/v5"
)

type client struct {
	t     *testing.T
	base  string
	token string
}

func base(t *testing.T) string {
	url := os.Getenv("MYTHWAKE_E2E_URL")
	if url == "" {
		t.Skip("MYTHWAKE_E2E_URL not set (start server/dev.ps1 first)")
	}
	return url
}

func random(n int) string {
	b := make([]byte, n)
	_, _ = rand.Read(b)
	return hex.EncodeToString(b)
}

func uuid() string {
	h := random(16)
	return h[0:8] + "-" + h[8:12] + "-4" + h[13:16] + "-a" + h[17:20] + "-" + h[20:32]
}

func (c *client) do(method, path string, body any, out any) int {
	c.t.Helper()
	var reader io.Reader
	if body != nil {
		buf, _ := json.Marshal(body)
		reader = bytes.NewReader(buf)
	}
	req, _ := http.NewRequest(method, c.base+path, reader)
	req.Header.Set("Content-Type", "application/json")
	if c.token != "" {
		req.Header.Set("Authorization", "Bearer "+c.token)
	}
	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		c.t.Fatalf("%s %s: %v", method, path, err)
	}
	defer resp.Body.Close()
	data, _ := io.ReadAll(resp.Body)
	if out != nil && len(data) > 0 {
		if err := json.Unmarshal(data, out); err != nil {
			c.t.Fatalf("%s %s: %v: %s", method, path, err, data)
		}
	}
	return resp.StatusCode
}

type login struct {
	Token     string `json:"token"`
	AccountID string `json:"accountId"`
	Created   bool   `json:"created"`
}

type player struct {
	State     json.RawMessage `json:"state"`
	StateHash string          `json:"stateHash"`
	Version   int64           `json:"version"`
}

type command struct {
	ID   string `json:"id"`
	Type string `json:"type"`
	Data any    `json:"data"`
}

type commandsResult struct {
	Version   int64  `json:"version"`
	StateHash string `json:"stateHash"`
	Duplicate bool   `json:"duplicate"`
	Error     string `json:"error"`
}

func newPlayer(t *testing.T) (*client, login) {
	c := &client{t: t, base: base(t)}
	device, secret := "dev-"+random(12), random(20)
	var l login
	if s := c.do("POST", "/v1/auth/device", map[string]string{"deviceId": device, "deviceSecret": secret}, &l); s != 200 || !l.Created {
		t.Fatalf("first login: %d %+v", s, l)
	}
	var again login
	c.do("POST", "/v1/auth/device", map[string]string{"deviceId": device, "deviceSecret": secret}, &again)
	if again.Created || again.AccountID != l.AccountID {
		t.Fatalf("second login must reuse the account: %+v", again)
	}
	if s := c.do("POST", "/v1/auth/device", map[string]string{"deviceId": device, "deviceSecret": random(20)}, nil); s != 401 {
		t.Fatalf("wrong secret: want 401, got %d", s)
	}
	c.token = l.Token
	return c, l
}

func TestPlayerAndCommands(t *testing.T) {
	c, _ := newPlayer(t)
	if s := (&client{t: t, base: c.base}).do("GET", "/v1/player", nil, nil); s != 401 {
		t.Fatalf("no token: want 401, got %d", s)
	}
	var p player
	if s := c.do("GET", "/v1/player", nil, &p); s != 200 || p.Version != 1 {
		t.Fatalf("player: %d %+v", s, p.Version)
	}
	sum := sha256.Sum256(p.State)
	if hex.EncodeToString(sum[:]) != p.StateHash {
		t.Fatal("stored state text changed: hash mismatch")
	}

	batch := []command{{ID: uuid(), Type: "BeginStageBattle", Data: map[string]any{}}, {ID: uuid(), Type: "FinishStageBattle", Data: map[string]any{}}}
	var r commandsResult
	if s := c.do("POST", "/v1/commands", map[string]any{"baseVersion": 1, "commands": batch}, &r); s != 200 || r.Version != 3 {
		t.Fatalf("commands: %d %+v", s, r)
	}
	var retry commandsResult
	if s := c.do("POST", "/v1/commands", map[string]any{"baseVersion": 1, "commands": batch}, &retry); s != 200 || !retry.Duplicate || retry.Version != 3 {
		t.Fatalf("retry must be idempotent: %d %+v", s, retry)
	}
	stale := []command{{ID: uuid(), Type: "ClaimAfkRewards", Data: map[string]any{}}}
	if s := c.do("POST", "/v1/commands", map[string]any{"baseVersion": 1, "commands": stale}, &r); s != 409 || r.Error != "version_conflict" {
		t.Fatalf("stale base version: want 409, got %d %+v", s, r)
	}
	bad := []command{{ID: uuid(), Type: "LevelUpHero", Data: map[string]any{"heroId": "dorn", "levels": 90}}}
	if s := c.do("POST", "/v1/commands", map[string]any{"baseVersion": 3, "commands": bad}, &r); s != 422 || r.Error != "level_cap" {
		t.Fatalf("rule violation: want 422 level_cap, got %d %+v", s, r)
	}
	cheat := []command{{ID: uuid(), Type: "GrantRewards", Data: map[string]any{"rewards": []map[string]any{{"id": "gems", "value": 999999}}}}}
	if s := c.do("POST", "/v1/commands", map[string]any{"baseVersion": 3, "commands": cheat}, &r); s != 422 || r.Error != "forbidden" {
		t.Fatalf("server-only command from a client: want 422 forbidden, got %d %+v", s, r)
	}
	c.do("GET", "/v1/player", nil, &p)
	if p.Version != 3 {
		t.Fatalf("rejected commands must not change the state: version %d", p.Version)
	}
	var cleared struct {
		Campaign struct {
			HighestCleared int `json:"highestCleared"`
		} `json:"campaign"`
	}
	_ = json.Unmarshal(p.State, &cleared)
	if cleared.Campaign.HighestCleared != 1 {
		t.Fatalf("stage 1 should be cleared by the server's re-simulation, got %d", cleared.Campaign.HighestCleared)
	}
}

func TestArena(t *testing.T) {
	c, l := newPlayer(t)
	var p player
	c.do("GET", "/v1/player", nil, &p)

	var arena struct {
		Unlocked   bool   `json:"unlocked"`
		HasDefense bool   `json:"hasDefense"`
		Rating     int    `json:"rating"`
		Rank       string `json:"rank"`
		FightsLeft int    `json:"fightsLeft"`
		Opponents  []struct {
			ID     string   `json:"id"`
			Rating int      `json:"rating"`
			Heroes []string `json:"heroes"`
		} `json:"opponents"`
	}
	c.do("GET", "/v1/arena", nil, &arena)
	if arena.Unlocked {
		t.Fatal("a new player must not have the arena")
	}
	if s := c.do("POST", "/v1/arena/defense", map[string]any{}, nil); s != 403 {
		t.Fatalf("locked defense: want 403, got %d", s)
	}

	unlock(t, l.AccountID)
	var def struct {
		Power int64 `json:"power"`
	}
	if s := c.do("POST", "/v1/arena/defense", map[string]any{}, &def); s != 200 || def.Power <= 0 {
		t.Fatalf("defense: %d %+v", s, def)
	}
	c.do("GET", "/v1/arena", nil, &arena)
	if !arena.Unlocked || !arena.HasDefense || arena.Rank != "Bronze" || len(arena.Opponents) != 3 {
		t.Fatalf("arena after defense: %+v", arena)
	}

	var fight struct {
		Victory      bool            `json:"victory"`
		RatingChange int             `json:"ratingChange"`
		Rating       int             `json:"rating"`
		FightsLeft   int             `json:"fightsLeft"`
		Attacker     json.RawMessage `json:"attacker"`
		Defender     json.RawMessage `json:"defender"`
		Reward       command         `json:"reward"`
		Version      int64           `json:"version"`
	}
	if s := c.do("POST", "/v1/arena/fight", map[string]string{"opponentId": arena.Opponents[0].ID}, &fight); s != 200 {
		t.Fatalf("fight: %d", s)
	}
	if fight.Rating != arena.Rating+fight.RatingChange || fight.FightsLeft != arena.FightsLeft-1 || fight.Reward.Type != "GrantRewards" {
		t.Fatalf("fight result: %+v", fight)
	}
	if len(fight.Attacker) == 0 || len(fight.Defender) == 0 {
		t.Fatal("the client needs both teams to replay the fight")
	}
	var after player
	c.do("GET", "/v1/player", nil, &after)
	if after.Version != fight.Version || after.Version <= p.Version {
		t.Fatalf("rewards must bump the player version: %d → %d (fight says %d)", p.Version, after.Version, fight.Version)
	}

	for i := 0; i < 10 && fight.FightsLeft > 0; i++ {
		c.do("POST", "/v1/arena/fight", map[string]string{"opponentId": arena.Opponents[1].ID}, &fight)
	}
	var e struct {
		Error string `json:"error"`
	}
	if s := c.do("POST", "/v1/arena/fight", map[string]string{"opponentId": arena.Opponents[1].ID}, &e); s != 422 || e.Error != "arena_no_fights" {
		t.Fatalf("daily limit: want 422 arena_no_fights, got %d %+v", s, e)
	}
	var ranking struct {
		Players []struct {
			ID string `json:"id"`
		} `json:"players"`
	}
	if s := c.do("GET", "/v1/arena/ranking", nil, &ranking); s != 200 || len(ranking.Players) == 0 {
		t.Fatalf("ranking: %d", s)
	}
}

// unlock sets the campaign far enough for the arena straight in the database (test shortcut).
func unlock(t *testing.T, account string) {
	url := os.Getenv("MYTHWAKE_DATABASE_URL")
	if url == "" {
		t.Skip("MYTHWAKE_DATABASE_URL not set")
	}
	ctx := context.Background()
	conn, err := pgx.Connect(ctx, url)
	if err != nil {
		t.Fatal(err)
	}
	defer conn.Close(ctx)
	// the text is rewritten by the next command anyway; keep the stored hash consistent for this test
	tag, err := conn.Exec(ctx, `update players set state = jsonb_set(state::jsonb, '{campaign,highestCleared}', '20')::text::json
		where account_id = $1`, account)
	if err != nil || tag.RowsAffected() != 1 {
		t.Fatal(fmt.Sprint("unlock: ", err))
	}
	var state string
	_ = conn.QueryRow(ctx, "select state::text from players where account_id = $1", account).Scan(&state)
	sum := sha256.Sum256([]byte(state))
	_, _ = conn.Exec(ctx, "update players set state_hash = $2 where account_id = $1", account, hex.EncodeToString(sum[:]))
}
