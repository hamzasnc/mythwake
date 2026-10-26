// Package rules talks to the stateless .NET rules service that runs Mythwake.Core (see docs/SERVER.md).
package rules

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"time"
)

// Command is one player action in wire format.
type Command struct {
	ID   string          `json:"id,omitempty"`
	Type string          `json:"type"`
	Data json.RawMessage `json:"data"`
	// AtMs is the command's time. The client sends its predicted time; the API checks it against the server clock.
	AtMs int64 `json:"atMs,omitempty"`
}

// Result of applying commands or creating a player. State is the save JSON exactly as the rules service wrote it.
type Result struct {
	OK        bool   `json:"ok"`
	State     string `json:"state"`
	StateHash string `json:"stateHash"`
	Error     string `json:"error"`
	Index     int    `json:"index"`
}

// RuleError is a rule violation (not enough gold, hero not owned, …). The code is shown to the player.
type RuleError struct {
	Code  string
	Index int
}

func (e *RuleError) Error() string {
	return fmt.Sprintf("rule violation %q at command %d", e.Code, e.Index)
}

type ArenaTeam struct {
	OK    bool   `json:"ok"`
	Error string `json:"error"`
	Team  string `json:"team"`
	Power int64  `json:"power"`
}

type ArenaBattle struct {
	OK           bool `json:"ok"`
	Victory      bool `json:"victory"`
	DurationMs   int  `json:"durationMs"`
	RatingChange int  `json:"ratingChange"`
}

type Client struct {
	base string
	http *http.Client
}

func New(base string) *Client {
	return &Client{base: base, http: &http.Client{Timeout: 10 * time.Second}}
}

// Health returns the content version the rules service loaded.
func (c *Client) Health(ctx context.Context) (string, error) {
	var out struct {
		OK             bool   `json:"ok"`
		ContentVersion string `json:"contentVersion"`
	}
	if err := c.call(ctx, http.MethodGet, "/health", nil, &out); err != nil {
		return "", err
	}
	return out.ContentVersion, nil
}

func (c *Client) Create(ctx context.Context, seed uint64, nowMs int64) (Result, error) {
	var out Result
	err := c.call(ctx, http.MethodPost, "/create", map[string]any{"seed": fmt.Sprint(seed), "nowMs": nowMs}, &out)
	return out, err
}

// Apply runs commands on a state. trusted allows server-only commands (never set it for client input).
func (c *Client) Apply(ctx context.Context, state string, nowMs int64, commands []Command, trusted bool) (Result, error) {
	var out Result
	err := c.call(ctx, http.MethodPost, "/apply", map[string]any{
		"state": state, "nowMs": nowMs, "commands": commands, "trusted": trusted,
	}, &out)
	if err != nil {
		return out, err
	}
	if !out.OK {
		return out, &RuleError{Code: out.Error, Index: out.Index}
	}
	return out, nil
}

func (c *Client) ArenaTeam(ctx context.Context, state string) (ArenaTeam, error) {
	var out ArenaTeam
	err := c.call(ctx, http.MethodPost, "/arena/team", map[string]any{"state": state}, &out)
	if err == nil && !out.OK {
		return out, &RuleError{Code: out.Error}
	}
	return out, err
}

func (c *Client) ArenaBattle(ctx context.Context, attacker, defender string, seed uint64, attackerRating, defenderRating int) (ArenaBattle, error) {
	var out ArenaBattle
	err := c.call(ctx, http.MethodPost, "/arena/battle", map[string]any{
		"attacker": attacker, "defender": defender, "seed": fmt.Sprint(seed),
		"attackerRating": attackerRating, "defenderRating": defenderRating,
	}, &out)
	return out, err
}

// ArenaConfig is the arena part of the game content (ranks, rewards, limits).
type ArenaConfig struct {
	StartRating      int                 `json:"startRating"`
	FreeFightsPerDay int                 `json:"freeFightsPerDay"`
	UnlockStage      int                 `json:"unlockStage"`
	SeasonDays       int                 `json:"seasonDays"`
	SeasonEpochMs    int64               `json:"seasonEpochMs"`
	Ranks            []ArenaRank         `json:"ranks"`
	WinRewards       []Amount            `json:"winRewards"`
	LossRewards      []Amount            `json:"lossRewards"`
	SeasonRewards    map[string][]Amount `json:"seasonRewards"`
	ResetHourUtc     int                 `json:"resetHourUtc"`
}

type ArenaRank struct {
	Name      string `json:"name"`
	MinRating int    `json:"minRating"`
}

type Amount struct {
	ID    string `json:"id"`
	Value int64  `json:"value"`
}

func (c *Client) ArenaConfig(ctx context.Context) (ArenaConfig, error) {
	var out ArenaConfig
	err := c.call(ctx, http.MethodGet, "/config/arena", nil, &out)
	return out, err
}

// BotTeam returns a generated defense team for an arena bot at the given campaign stage.
func (c *Client) BotTeam(ctx context.Context, stage int, seed uint64) (ArenaTeam, error) {
	var out ArenaTeam
	err := c.call(ctx, http.MethodPost, "/arena/bot", map[string]any{"stage": stage, "seed": fmt.Sprint(seed)}, &out)
	return out, err
}

func (c *Client) call(ctx context.Context, method, path string, body any, out any) error {
	var reader io.Reader
	if body != nil {
		buf, err := json.Marshal(body)
		if err != nil {
			return err
		}
		reader = bytes.NewReader(buf)
	}
	req, err := http.NewRequestWithContext(ctx, method, c.base+path, reader)
	if err != nil {
		return err
	}
	req.Header.Set("Content-Type", "application/json")
	resp, err := c.http.Do(req)
	if err != nil {
		return fmt.Errorf("rules service: %w", err)
	}
	defer resp.Body.Close()
	data, err := io.ReadAll(io.LimitReader(resp.Body, 32<<20))
	if err != nil {
		return err
	}
	// 422 carries a rule violation in the normal body; everything else non-2xx is an infrastructure error
	if resp.StatusCode >= 300 && resp.StatusCode != http.StatusUnprocessableEntity {
		return fmt.Errorf("rules service %s %s: %s: %s", method, path, resp.Status, truncate(data, 200))
	}
	if err := json.Unmarshal(data, out); err != nil {
		return fmt.Errorf("rules service %s: %w", path, err)
	}
	return nil
}

func truncate(b []byte, n int) string {
	if len(b) > n {
		return string(b[:n]) + "…"
	}
	return string(b)
}

// IsRuleError reports whether err is a rule violation and returns it.
func IsRuleError(err error) (*RuleError, bool) {
	var re *RuleError
	ok := errors.As(err, &re)
	return re, ok
}
