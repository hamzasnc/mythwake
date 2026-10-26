package api

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"math/rand/v2"
	"net/http"
	"sort"

	"mythwake/api/internal/rules"
	"mythwake/api/internal/store"
)

// Arena (doc: "Arena System"): asynchronous PvP against stored defense teams. The server picks the opponent, the rules
// service simulates the fight, the server keeps ratings and pays rewards. See docs/systems/ARENA.md.

const dayMs = 86_400_000

// arenaConfig loads the arena settings from the rules service once; failures are not cached.
func (s *Server) arenaConfig(ctx context.Context) (rules.ArenaConfig, error) {
	s.arenaMu.Lock()
	defer s.arenaMu.Unlock()
	if s.arenaLoaded {
		return s.arena, nil
	}
	c, err := s.rules.ArenaConfig(ctx)
	if err != nil {
		return rules.ArenaConfig{}, err
	}
	s.arena, s.arenaLoaded = c, true
	return c, nil
}

func (s *Server) season(c rules.ArenaConfig, nowMs int64) int {
	return int(nowMs / dayMs / int64(max(1, c.SeasonDays)))
}

func (s *Server) seasonEndMs(c rules.ArenaConfig, season int) int64 {
	return int64(season+1) * int64(max(1, c.SeasonDays)) * dayMs
}

func dayIndex(c rules.ArenaConfig, nowMs int64) int64 {
	return (nowMs - int64(c.ResetHourUtc)*3_600_000) / dayMs
}

func rankOf(c rules.ArenaConfig, rating int) string {
	name := ""
	best := -1
	for _, r := range c.Ranks {
		if rating >= r.MinRating && r.MinRating > best {
			name, best = r.Name, r.MinRating
		}
	}
	return name
}

// highestCleared reads the campaign progress from a save without decoding the whole state.
func highestCleared(state string) int {
	var s struct {
		Campaign struct {
			HighestCleared int `json:"highestCleared"`
		} `json:"campaign"`
	}
	_ = json.Unmarshal([]byte(state), &s)
	return s.Campaign.HighestCleared
}

// grant applies a server-only GrantRewards command to the locked player and logs it like any command.
func (s *Server) grant(ctx context.Context, tx *store.Tx, reason string, rewards []rules.Amount) (rules.Command, error) {
	data, _ := json.Marshal(map[string]any{"reason": reason, "rewards": rewards})
	cmd := rules.Command{ID: newUUID(), Type: "GrantRewards", Data: data}
	p := tx.Player
	result, err := s.rules.Apply(ctx, p.State, s.nowMs(), []rules.Command{cmd}, true)
	if err != nil {
		return cmd, err
	}
	p.State, p.StateHash, p.Version = result.State, result.StateHash, p.Version+1
	if err := tx.SavePlayer(ctx); err != nil {
		return cmd, err
	}
	return cmd, tx.LogCommands(ctx, []store.LoggedCommand{{ID: cmd.ID, Type: cmd.Type, Data: cmd.Data}}, p.Version)
}

// rollSeason moves an arena row into the current season. A player's first visit of a new season pays the
// end-of-season rewards of the old rank and softly resets the rating halfway towards the start.
func (s *Server) rollSeason(ctx context.Context, tx *store.Tx, c rules.ArenaConfig, a *store.ArenaPlayer, season int) (*rules.Command, error) {
	if a.Season >= season {
		return nil, nil
	}
	var paid *rules.Command
	if !a.IsBot && tx.Player != nil && tx.Player.AccountID == a.AccountID {
		if rewards := c.SeasonRewards[rankOf(c, a.Rating)]; len(rewards) > 0 {
			cmd, err := s.grant(ctx, tx, "arenaSeason", rewards)
			if err != nil {
				return nil, err
			}
			paid = &cmd
		}
	}
	a.Season = season
	a.Rating = c.StartRating + (a.Rating-c.StartRating)/2
	a.Wins, a.Losses = 0, 0
	return paid, tx.SaveArena(ctx, a)
}

// ------------------------------------------------------------------ GET /v1/arena

type arenaOpponent struct {
	ID     string   `json:"id"`
	Name   string   `json:"name"`
	Rating int      `json:"rating"`
	Rank   string   `json:"rank"`
	Power  int64    `json:"power"`
	Heroes []string `json:"heroes"`
	IsBot  bool     `json:"isBot"`
}

type arenaResponse struct {
	Unlocked     bool              `json:"unlocked"`
	UnlockStage  int               `json:"unlockStage"`
	Season       int               `json:"season"`
	SeasonEndsMs int64             `json:"seasonEndsMs"`
	HasDefense   bool              `json:"hasDefense"`
	Rating       int               `json:"rating"`
	Rank         string            `json:"rank"`
	Wins         int               `json:"wins"`
	Losses       int               `json:"losses"`
	FightsLeft   int               `json:"fightsLeft"`
	DefensePower int64             `json:"defensePower"`
	Opponents    []arenaOpponent   `json:"opponents"`
	Ranks        []rules.ArenaRank `json:"ranks"`
	// Set when visiting paid season rewards: the client must reload the player.
	Version   int64  `json:"version"`
	StateHash string `json:"stateHash"`
}

func (s *Server) getArena(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	c, err := s.arenaConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	account := accountID(r)
	if _, err := s.loadOrCreatePlayer(ctx, account); err != nil {
		s.internal(w, err)
		return
	}
	now := s.nowMs()
	season := s.season(c, now)
	resp := arenaResponse{UnlockStage: c.UnlockStage, Season: season, SeasonEndsMs: s.seasonEndMs(c, season), Ranks: c.Ranks, Rating: c.StartRating}
	var me *store.ArenaPlayer
	err = s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		resp.Unlocked = highestCleared(tx.Player.State) >= c.UnlockStage
		a, err := tx.ArenaForUpdate(ctx, account)
		if errors.Is(err, store.ErrNotFound) {
			resp.Version, resp.StateHash = tx.Player.Version, tx.Player.StateHash
			return nil
		}
		if err != nil {
			return err
		}
		if _, err := s.rollSeason(ctx, tx, c, a, season); err != nil {
			return err
		}
		me = a
		resp.Version, resp.StateHash = tx.Player.Version, tx.Player.StateHash
		return nil
	})
	if err != nil {
		s.internal(w, err)
		return
	}
	resp.FightsLeft = c.FreeFightsPerDay
	if me != nil {
		resp.HasDefense = true
		resp.Rating, resp.Wins, resp.Losses, resp.DefensePower = me.Rating, me.Wins, me.Losses, me.Power
		if me.FightsDay == dayIndex(c, now) {
			resp.FightsLeft = max(0, c.FreeFightsPerDay-me.FightsUsed)
		}
	}
	resp.Rank = rankOf(c, resp.Rating)
	if resp.Unlocked && resp.HasDefense {
		resp.Opponents, err = s.pickOpponents(ctx, c, season, resp.Rating, account)
		if err != nil {
			s.internal(w, err)
			return
		}
	}
	writeJSON(w, http.StatusOK, resp)
}

// pickOpponents offers three opponents from the eight closest in rating: one below, one close, one above if possible.
func (s *Server) pickOpponents(ctx context.Context, c rules.ArenaConfig, season, rating int, account string) ([]arenaOpponent, error) {
	near, err := s.store.Opponents(ctx, season, rating, account, 8)
	if err != nil {
		return nil, err
	}
	sort.Slice(near, func(i, j int) bool { return near[i].Rating < near[j].Rating })
	var picked []*store.ArenaPlayer
	if len(near) <= 3 {
		picked = near
	} else {
		third := len(near) / 3
		picked = []*store.ArenaPlayer{
			near[rand.IntN(third)],
			near[third+rand.IntN(third)],
			near[2*third+rand.IntN(len(near)-2*third)],
		}
	}
	out := make([]arenaOpponent, 0, len(picked))
	for _, a := range picked {
		out = append(out, arenaOpponent{ID: a.AccountID, Name: a.Name, Rating: a.Rating, Rank: rankOf(c, a.Rating),
			Power: a.Power, Heroes: heroesOf(a.Defense), IsBot: a.IsBot})
	}
	return out, nil
}

func heroesOf(team string) []string {
	var t struct {
		Units []struct {
			HeroID string `json:"heroId"`
		} `json:"units"`
	}
	_ = json.Unmarshal([]byte(team), &t)
	ids := make([]string, len(t.Units))
	for i, u := range t.Units {
		ids[i] = u.HeroID
	}
	return ids
}

// ------------------------------------------------------------------ POST /v1/arena/defense

func (s *Server) setDefense(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	c, err := s.arenaConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	account := accountID(r)
	p, err := s.loadOrCreatePlayer(ctx, account)
	if err != nil {
		s.internal(w, err)
		return
	}
	if highestCleared(p.State) < c.UnlockStage {
		writeError(w, http.StatusForbidden, "arena_locked", fmt.Sprintf("clear stage %d first", c.UnlockStage))
		return
	}
	season := s.season(c, s.nowMs())
	var power int64
	err = s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		team, err := s.rules.ArenaTeam(ctx, tx.Player.State)
		if err != nil {
			return err
		}
		a, err := tx.ArenaForUpdate(ctx, account)
		if errors.Is(err, store.ErrNotFound) {
			a = &store.ArenaPlayer{AccountID: account, Season: season, Rating: c.StartRating, FightsDay: -1}
		} else if err != nil {
			return err
		} else if _, err := s.rollSeason(ctx, tx, c, a, season); err != nil {
			return err
		}
		a.Name, a.Defense, a.Power = tx.Player.DisplayName, team.Team, team.Power
		power = team.Power
		return tx.SaveArena(ctx, a)
	})
	if re, ok := rules.IsRuleError(err); ok {
		writeError(w, http.StatusUnprocessableEntity, re.Code, "")
		return
	}
	if err != nil {
		s.internal(w, err)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"power": power})
}

// ------------------------------------------------------------------ POST /v1/arena/fight

type fightRequest struct {
	OpponentID string `json:"opponentId"`
}

type fightResponse struct {
	Victory      bool            `json:"victory"`
	RatingChange int             `json:"ratingChange"`
	Rating       int             `json:"rating"`
	Rank         string          `json:"rank"`
	FightsLeft   int             `json:"fightsLeft"`
	Seed         string          `json:"seed"`
	Attacker     json.RawMessage `json:"attacker"`
	Defender     json.RawMessage `json:"defender"`
	DefenderName string          `json:"defenderName"`
	Rewards      []rules.Amount  `json:"rewards"`
	// Reward is the server-only command that paid the rewards; the client applies it to its prediction.
	Reward    rules.Command `json:"reward"`
	Version   int64         `json:"version"`
	StateHash string        `json:"stateHash"`
}

func (s *Server) fight(w http.ResponseWriter, r *http.Request) {
	var req fightRequest
	if err := readJSON(r, &req); err != nil || !uuidPattern.MatchString(req.OpponentID) {
		writeError(w, http.StatusBadRequest, "bad_request", "opponentId required")
		return
	}
	ctx := r.Context()
	c, err := s.arenaConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	account := accountID(r)
	if req.OpponentID == account {
		writeError(w, http.StatusBadRequest, "bad_request", "cannot fight yourself")
		return
	}
	now := s.nowMs()
	season, day := s.season(c, now), dayIndex(c, now)
	var resp fightResponse
	var ruleErr string
	err = s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		if highestCleared(tx.Player.State) < c.UnlockStage {
			ruleErr = "arena_locked"
			return nil
		}
		me, err := tx.ArenaForUpdate(ctx, account)
		if errors.Is(err, store.ErrNotFound) {
			ruleErr = "arena_no_defense"
			return nil
		}
		if err != nil {
			return err
		}
		if _, err := s.rollSeason(ctx, tx, c, me, season); err != nil {
			return err
		}
		if me.FightsDay != day {
			me.FightsDay, me.FightsUsed = day, 0
		}
		if me.FightsUsed >= c.FreeFightsPerDay {
			ruleErr = "arena_no_fights"
			return nil
		}
		them, err := tx.LockArenaRow(ctx, req.OpponentID)
		if errors.Is(err, store.ErrNotFound) {
			ruleErr = "arena_unknown_opponent"
			return nil
		}
		if err != nil {
			return err
		}
		if !them.IsBot {
			if them.Season < season {
				them.Season = season
				them.Rating = c.StartRating + (them.Rating-c.StartRating)/2
				them.Wins, them.Losses = 0, 0
			}
		}

		attack, err := s.rules.ArenaTeam(ctx, tx.Player.State)
		if re, ok := rules.IsRuleError(err); ok {
			ruleErr = re.Code
			return nil
		}
		if err != nil {
			return err
		}
		seed := randomUint64()
		battle, err := s.rules.ArenaBattle(ctx, attack.Team, them.Defense, seed, me.Rating, them.Rating)
		if err != nil {
			return err
		}
		me.Rating = max(0, me.Rating+battle.RatingChange)
		me.FightsUsed++
		if battle.Victory {
			me.Wins++
			them.Losses++
		} else {
			me.Losses++
			them.Wins++
		}
		if !them.IsBot {
			them.Rating = max(0, them.Rating-battle.RatingChange)
		}
		if err := tx.SaveArena(ctx, me); err != nil {
			return err
		}
		if err := tx.SaveArena(ctx, them); err != nil {
			return err
		}
		if err := tx.LogArenaBattle(ctx, season, account, them.AccountID, seed, battle.Victory, battle.RatingChange); err != nil {
			return err
		}
		rewards := c.LossRewards
		reason := "arenaLoss"
		if battle.Victory {
			rewards, reason = c.WinRewards, "arenaWin"
		}
		cmd, err := s.grant(ctx, tx, reason, rewards)
		if err != nil {
			return err
		}
		resp = fightResponse{
			Victory: battle.Victory, RatingChange: battle.RatingChange, Rating: me.Rating, Rank: rankOf(c, me.Rating),
			FightsLeft: c.FreeFightsPerDay - me.FightsUsed, Seed: fmt.Sprint(seed),
			Attacker: json.RawMessage(attack.Team), Defender: json.RawMessage(them.Defense), DefenderName: them.Name,
			Rewards: rewards, Reward: cmd, Version: tx.Player.Version, StateHash: tx.Player.StateHash,
		}
		return nil
	})
	switch {
	case err != nil:
		s.internal(w, err)
	case ruleErr != "":
		writeError(w, http.StatusUnprocessableEntity, ruleErr, "")
	default:
		writeJSON(w, http.StatusOK, resp)
	}
}

// ------------------------------------------------------------------ GET /v1/arena/ranking

func (s *Server) ranking(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	c, err := s.arenaConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	season := s.season(c, s.nowMs())
	list, err := s.store.Ranking(ctx, season, 50)
	if err != nil {
		s.internal(w, err)
		return
	}
	out := make([]arenaOpponent, 0, len(list))
	for _, a := range list {
		out = append(out, arenaOpponent{ID: a.AccountID, Name: a.Name, Rating: a.Rating, Rank: rankOf(c, a.Rating), Power: a.Power,
			Heroes: heroesOf(a.Defense), IsBot: a.IsBot})
	}
	writeJSON(w, http.StatusOK, map[string]any{"season": season, "players": out})
}

// ------------------------------------------------------------------ bots

// EnsureBots fills the arena with training opponents spread over the rating range, so a fresh server is playable.
func (s *Server) EnsureBots(ctx context.Context, want int) error {
	c, err := s.arenaConfig(ctx)
	if err != nil {
		return err
	}
	have, err := s.store.BotCount(ctx)
	if err != nil {
		return err
	}
	season := s.season(c, s.nowMs())
	for i := have; i < want; i++ {
		rating := 850 + i*1100/max(1, want-1)
		stage := min(200, max(15, (rating-800)/8))
		team, err := s.rules.BotTeam(ctx, stage, randomUint64())
		if err != nil {
			return err
		}
		bot := &store.ArenaPlayer{AccountID: newUUID(), Name: fmt.Sprintf("Trainingsgegner %02d", i+1), IsBot: true,
			Season: season, Rating: rating, Defense: team.Team, Power: team.Power, FightsDay: -1}
		if err := s.store.InsertBot(ctx, bot); err != nil {
			return err
		}
	}
	if want > have {
		s.log.Info("arena bots created", "count", want-have)
	}
	return nil
}
