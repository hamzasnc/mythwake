package api

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"net/http"
	"strconv"
	"strings"
	"unicode"
	"unicode/utf8"

	"mythwake/api/internal/rules"
	"mythwake/api/internal/store"
)

// Guilds (doc: "Guild System") and Guardian Raids (doc: "Guardian Raid System"). The server owns guilds; the player
// state only knows its guild id and Guild Coins, both changed through server-only commands. See docs/systems/GUILDS.md.

const maxChatLength = 200

// guildConfig loads the guild settings from the rules service once; failures are not cached.
func (s *Server) guildConfig(ctx context.Context) (rules.GuildConfig, error) {
	s.guildMu.Lock()
	defer s.guildMu.Unlock()
	if s.guildLoaded {
		return s.guild, nil
	}
	c, err := s.rules.GuildConfig(ctx)
	if err != nil {
		return rules.GuildConfig{}, err
	}
	s.guild, s.guildLoaded = c, true
	return c, nil
}

func guildDay(c rules.GuildConfig, nowMs int64) int64 {
	return (nowMs - int64(c.ResetHourUtc)*3_600_000) / dayMs
}

// guildLevel: LevelExp holds the total EXP needed for level 2, 3, …
func guildLevel(c rules.GuildConfig, exp int64) int {
	level := 1
	for _, need := range c.LevelExp {
		if exp >= need {
			level++
		}
	}
	return level
}

func maxMembers(c rules.GuildConfig, level int) int {
	return c.MembersBase + c.MembersPerLevel*max(0, level-1)
}

// validGuildName trims and checks a guild name: letters, digits, spaces, '-' and '_'.
func validGuildName(c rules.GuildConfig, name string) (string, bool) {
	name = strings.Join(strings.Fields(name), " ")
	n := utf8.RuneCountInString(name)
	if n < max(1, c.NameMinLength) || n > max(1, c.NameMaxLength) {
		return name, false
	}
	for _, r := range name {
		if !unicode.IsLetter(r) && !unicode.IsDigit(r) && r != ' ' && r != '-' && r != '_' {
			return name, false
		}
	}
	return name, true
}

// serverCommands applies trusted commands to the locked player in one step and logs them like client commands.
func (s *Server) serverCommands(ctx context.Context, tx *store.Tx, commands ...rules.Command) error {
	p := tx.Player
	logged := make([]store.LoggedCommand, len(commands))
	for i := range commands {
		commands[i].ID = newUUID()
		commands[i].AtMs = max(s.nowMs(), p.LastCommandMs)
		p.LastCommandMs = commands[i].AtMs
		logged[i] = store.LoggedCommand{ID: commands[i].ID, Type: commands[i].Type, Data: commands[i].Data}
	}
	result, err := s.rules.Apply(ctx, p.State, s.nowMs(), commands, true)
	if err != nil {
		return err
	}
	first := p.Version + 1
	p.State, p.StateHash, p.Version = result.State, result.StateHash, p.Version+int64(len(commands))
	if err := tx.SavePlayer(ctx); err != nil {
		return err
	}
	return tx.LogCommands(ctx, logged, first)
}

func command(kind string, data any) rules.Command {
	buf, _ := json.Marshal(data)
	return rules.Command{Type: kind, Data: buf}
}

// ------------------------------------------------------------------ list, create, join, leave

type guildSummary struct {
	ID         string `json:"id"`
	Name       string `json:"name"`
	Level      int    `json:"level"`
	Members    int    `json:"members"`
	MaxMembers int    `json:"maxMembers"`
	RaidLevel  int    `json:"raidLevel"`
}

func (s *Server) summary(c rules.GuildConfig, g *store.Guild) guildSummary {
	level := guildLevel(c, g.Exp)
	return guildSummary{ID: g.ID, Name: g.Name, Level: level, Members: g.Members, MaxMembers: maxMembers(c, level), RaidLevel: g.RaidLevel}
}

func (s *Server) listGuilds(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	c, err := s.guildConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	list, err := s.store.Guilds(ctx, strings.TrimSpace(r.URL.Query().Get("q")), 30)
	if err != nil {
		s.internal(w, err)
		return
	}
	out := make([]guildSummary, 0, len(list))
	for _, g := range list {
		out = append(out, s.summary(c, g))
	}
	writeJSON(w, http.StatusOK, map[string]any{"guilds": out})
}

type createGuildRequest struct {
	Name string `json:"name"`
}

func (s *Server) createGuild(w http.ResponseWriter, r *http.Request) {
	var req createGuildRequest
	if err := readJSON(r, &req); err != nil {
		writeError(w, http.StatusBadRequest, "bad_request", "name required")
		return
	}
	ctx := r.Context()
	c, err := s.guildConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	name, ok := validGuildName(c, req.Name)
	if !ok {
		writeError(w, http.StatusUnprocessableEntity, "guild_bad_name", "")
		return
	}
	account := accountID(r)
	if _, err := s.loadOrCreatePlayer(ctx, account); err != nil {
		s.internal(w, err)
		return
	}
	id := newUUID()
	var ruleErr string
	var version int64
	var hash string
	err = s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		if _, err := tx.MemberForUpdate(ctx, account); err == nil {
			ruleErr = "guild_already_member"
			return nil
		} else if !errors.Is(err, store.ErrNotFound) {
			return err
		}
		if err := tx.InsertGuild(ctx, &store.Guild{ID: id, Name: name, Leader: account, RaidLevel: 1, RaidHp: c.Raid.BossHpBase}); err != nil {
			if errors.Is(err, store.ErrConflict) {
				ruleErr = "guild_name_taken"
				return errRollback
			}
			return err
		}
		if err := tx.SaveMember(ctx, &store.GuildMember{AccountID: account, GuildID: id, Name: tx.Player.DisplayName, Role: "leader",
			DonateDay: -1, RaidDay: -1, Pending: "[]"}); err != nil {
			return err
		}
		err := s.serverCommands(ctx, tx, command("PayGuildCreation", map[string]any{}), command("SetGuild", map[string]any{"guildId": id}))
		if re, ok := rules.IsRuleError(err); ok {
			ruleErr = re.Code
			return errRollback
		}
		version, hash = tx.Player.Version, tx.Player.StateHash
		return err
	})
	s.finishGuildWrite(w, err, ruleErr, map[string]any{"id": id, "version": version, "stateHash": hash})
}

// errRollback aborts a transaction after a rule error was recorded.
var errRollback = errors.New("rollback")

func (s *Server) finishGuildWrite(w http.ResponseWriter, err error, ruleErr string, ok any) {
	switch {
	case ruleErr != "":
		writeError(w, http.StatusUnprocessableEntity, ruleErr, "")
	case err != nil:
		s.internal(w, err)
	default:
		writeJSON(w, http.StatusOK, ok)
	}
}

func (s *Server) joinGuild(w http.ResponseWriter, r *http.Request) {
	id := r.PathValue("id")
	if !uuidPattern.MatchString(id) {
		writeError(w, http.StatusBadRequest, "bad_request", "guild id")
		return
	}
	ctx := r.Context()
	c, err := s.guildConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	account := accountID(r)
	if _, err := s.loadOrCreatePlayer(ctx, account); err != nil {
		s.internal(w, err)
		return
	}
	var ruleErr string
	var version int64
	var hash string
	err = s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		if _, err := tx.MemberForUpdate(ctx, account); err == nil {
			ruleErr = "guild_already_member"
			return nil
		} else if !errors.Is(err, store.ErrNotFound) {
			return err
		}
		g, err := tx.GuildForUpdate(ctx, id)
		if errors.Is(err, store.ErrNotFound) {
			ruleErr = "guild_not_found"
			return nil
		}
		if err != nil {
			return err
		}
		if g.Members >= maxMembers(c, guildLevel(c, g.Exp)) {
			ruleErr = "guild_full"
			return nil
		}
		if err := tx.SaveMember(ctx, &store.GuildMember{AccountID: account, GuildID: id, Name: tx.Player.DisplayName, Role: "member",
			DonateDay: -1, RaidDay: -1, Pending: "[]"}); err != nil {
			return err
		}
		if err := s.serverCommands(ctx, tx, command("SetGuild", map[string]any{"guildId": id})); err != nil {
			return err
		}
		version, hash = tx.Player.Version, tx.Player.StateHash
		return nil
	})
	s.finishGuildWrite(w, err, ruleErr, map[string]any{"id": id, "version": version, "stateHash": hash})
}

func (s *Server) leaveGuild(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	account := accountID(r)
	var ruleErr string
	var version int64
	var hash string
	err := s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		m, err := tx.MemberForUpdate(ctx, account)
		if errors.Is(err, store.ErrNotFound) {
			ruleErr = "no_guild"
			return nil
		}
		if err != nil {
			return err
		}
		g, err := tx.GuildForUpdate(ctx, m.GuildID)
		if err != nil {
			return err
		}
		if err := tx.DeleteMember(ctx, account); err != nil {
			return err
		}
		if g.Leader == account {
			next, err := tx.OldestMember(ctx, g.ID)
			switch {
			case errors.Is(err, store.ErrNotFound):
				if err := tx.DeleteGuild(ctx, g.ID); err != nil {
					return err
				}
			case err != nil:
				return err
			default:
				g.Leader, next.Role = next.AccountID, "leader"
				if err := tx.SaveMember(ctx, next); err != nil {
					return err
				}
				if err := tx.SaveGuild(ctx, g); err != nil {
					return err
				}
			}
		}
		if err := s.serverCommands(ctx, tx, command("SetGuild", map[string]any{"guildId": nil})); err != nil {
			return err
		}
		version, hash = tx.Player.Version, tx.Player.StateHash
		return nil
	})
	s.finishGuildWrite(w, err, ruleErr, map[string]any{"version": version, "stateHash": hash})
}

// ------------------------------------------------------------------ GET /v1/guild

type guildMemberView struct {
	ID         string `json:"id"`
	Name       string `json:"name"`
	Role       string `json:"role"`
	RaidDamage int64  `json:"raidDamage"`
	Donated    bool   `json:"donated"`
}

type guildView struct {
	InGuild      bool                  `json:"inGuild"`
	Guild        *guildSummary         `json:"guild,omitempty"`
	Exp          int64                 `json:"exp"`
	NextLevelExp int64                 `json:"nextLevelExp"`
	Members      []guildMemberView     `json:"members"`
	Role         string                `json:"role"`
	Donated      bool                  `json:"donated"`
	Donations    []rules.GuildDonation `json:"donations"`
	RaidBoss     string                `json:"raidBoss"`
	RaidLevel    int                   `json:"raidLevel"`
	RaidHp       int64                 `json:"raidHp"`
	RaidHpMax    int64                 `json:"raidHpMax"`
	RaidLeft     int                   `json:"raidLeft"`
	Paid         []rules.Amount        `json:"paid"`
	CreateCost   []rules.Amount        `json:"createCost"`
	Version      int64                 `json:"version"`
	StateHash    string                `json:"stateHash"`
}

func raidBoss(c rules.GuildConfig, level int) string {
	if len(c.Raid.Bosses) == 0 {
		return ""
	}
	return c.Raid.Bosses[(max(1, level)-1)%len(c.Raid.Bosses)]
}

func (s *Server) getGuild(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	c, err := s.guildConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	account := accountID(r)
	if _, err := s.loadOrCreatePlayer(ctx, account); err != nil {
		s.internal(w, err)
		return
	}
	view := guildView{Donations: c.Donations, CreateCost: c.CreateCost, Members: []guildMemberView{}}
	var me *store.GuildMember
	var g *store.Guild
	err = s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		m, err := tx.MemberForUpdate(ctx, account)
		if errors.Is(err, store.ErrNotFound) {
			view.Version, view.StateHash = tx.Player.Version, tx.Player.StateHash
			return nil
		}
		if err != nil {
			return err
		}
		// raid kill rewards earned while away
		var pending []rules.Amount
		_ = json.Unmarshal([]byte(m.Pending), &pending)
		if len(pending) > 0 {
			if _, err := s.grant(ctx, tx, "guildRaidKill", pending); err != nil {
				return err
			}
			m.Pending = "[]"
			if err := tx.SaveMember(ctx, m); err != nil {
				return err
			}
			view.Paid = pending
		}
		if g, err = tx.GuildForUpdate(ctx, m.GuildID); err != nil {
			return err
		}
		me = m
		view.Version, view.StateHash = tx.Player.Version, tx.Player.StateHash
		return nil
	})
	if err != nil {
		s.internal(w, err)
		return
	}
	if me == nil {
		writeJSON(w, http.StatusOK, view)
		return
	}
	members, err := s.store.Members(ctx, g.ID)
	if err != nil {
		s.internal(w, err)
		return
	}
	day := guildDay(c, s.nowMs())
	sum := s.summary(c, g)
	view.InGuild, view.Guild, view.Exp, view.Role = true, &sum, g.Exp, me.Role
	if sum.Level-1 < len(c.LevelExp) {
		view.NextLevelExp = c.LevelExp[sum.Level-1]
	}
	view.Donated = me.DonateDay == day
	for _, m := range members {
		view.Members = append(view.Members, guildMemberView{ID: m.AccountID, Name: m.Name, Role: m.Role, RaidDamage: m.RaidDamage, Donated: m.DonateDay == day})
	}
	view.RaidBoss, view.RaidLevel, view.RaidHp = raidBoss(c, g.RaidLevel), g.RaidLevel, g.RaidHp
	view.RaidHpMax = raidHpMax(c, g.RaidLevel)
	view.RaidLeft = c.Raid.AttemptsPerDay
	if me.RaidDay == day {
		view.RaidLeft = max(0, c.Raid.AttemptsPerDay-me.RaidUsed)
	}
	writeJSON(w, http.StatusOK, view)
}

// raidHpMax mirrors Guilds.RaidBossHp in the rules (base × growth per level); only used for the HP bar.
func raidHpMax(c rules.GuildConfig, level int) int64 {
	hp := c.Raid.BossHpBase
	for l := 1; l < level; l++ {
		hp = hp * c.Raid.HpGrowth / 1000
	}
	return hp
}

// ------------------------------------------------------------------ chat

func (s *Server) guildChat(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	m, err := s.memberOf(ctx, accountID(r))
	if errors.Is(err, store.ErrNotFound) {
		writeError(w, http.StatusUnprocessableEntity, "no_guild", "")
		return
	}
	if err != nil {
		s.internal(w, err)
		return
	}
	after, _ := strconv.ParseInt(r.URL.Query().Get("after"), 10, 64)
	list, err := s.store.Messages(ctx, m.GuildID, after, 50)
	if err != nil {
		s.internal(w, err)
		return
	}
	if list == nil {
		list = []store.GuildMessage{}
	}
	writeJSON(w, http.StatusOK, map[string]any{"messages": list})
}

func (s *Server) memberOf(ctx context.Context, account string) (*store.GuildMember, error) {
	var m *store.GuildMember
	if _, err := s.loadOrCreatePlayer(ctx, account); err != nil {
		return nil, err
	}
	err := s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		var err error
		m, err = tx.MemberForUpdate(ctx, account)
		return err
	})
	return m, err
}

type chatRequest struct {
	Text string `json:"text"`
}

func (s *Server) postGuildChat(w http.ResponseWriter, r *http.Request) {
	var req chatRequest
	if err := readJSON(r, &req); err != nil {
		writeError(w, http.StatusBadRequest, "bad_request", "text required")
		return
	}
	text := strings.TrimSpace(req.Text)
	if text == "" || utf8.RuneCountInString(text) > maxChatLength {
		writeError(w, http.StatusUnprocessableEntity, "guild_bad_message", fmt.Sprintf("1–%d characters", maxChatLength))
		return
	}
	ctx := r.Context()
	account := accountID(r)
	var ruleErr string
	err := s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		m, err := tx.MemberForUpdate(ctx, account)
		if errors.Is(err, store.ErrNotFound) {
			ruleErr = "no_guild"
			return nil
		}
		if err != nil {
			return err
		}
		return tx.AddMessage(ctx, m.GuildID, account, tx.Player.DisplayName, text)
	})
	s.finishGuildWrite(w, err, ruleErr, map[string]any{"ok": true})
}

// ------------------------------------------------------------------ donations

type donateRequest struct {
	DonationID string `json:"donationId"`
}

func (s *Server) donate(w http.ResponseWriter, r *http.Request) {
	var req donateRequest
	if err := readJSON(r, &req); err != nil || req.DonationID == "" {
		writeError(w, http.StatusBadRequest, "bad_request", "donationId required")
		return
	}
	ctx := r.Context()
	c, err := s.guildConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	var donation *rules.GuildDonation
	for i := range c.Donations {
		if c.Donations[i].ID == req.DonationID {
			donation = &c.Donations[i]
		}
	}
	if donation == nil {
		writeError(w, http.StatusUnprocessableEntity, "unknown_donation", "")
		return
	}
	account := accountID(r)
	day := guildDay(c, s.nowMs())
	var ruleErr string
	var version int64
	var hash string
	err = s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		m, err := tx.MemberForUpdate(ctx, account)
		if errors.Is(err, store.ErrNotFound) {
			ruleErr = "no_guild"
			return nil
		}
		if err != nil {
			return err
		}
		if m.DonateDay == day {
			ruleErr = "guild_donated"
			return nil
		}
		g, err := tx.GuildForUpdate(ctx, m.GuildID)
		if err != nil {
			return err
		}
		err = s.serverCommands(ctx, tx, command("DonateToGuild", map[string]any{"donationId": donation.ID}))
		if re, ok := rules.IsRuleError(err); ok {
			ruleErr = re.Code
			return errRollback
		}
		if err != nil {
			return err
		}
		m.DonateDay = day
		g.Exp += donation.GuildExp
		if err := tx.SaveMember(ctx, m); err != nil {
			return err
		}
		version, hash = tx.Player.Version, tx.Player.StateHash
		return tx.SaveGuild(ctx, g)
	})
	if errors.Is(err, errRollback) {
		err = nil
	}
	s.finishGuildWrite(w, err, ruleErr, map[string]any{"guildExp": donation.GuildExp, "version": version, "stateHash": hash})
}

// ------------------------------------------------------------------ Guardian Raid

type raidResponse struct {
	Boss      string          `json:"boss"`
	RaidLevel int             `json:"raidLevel"`
	HpLeft    int64           `json:"hpLeft"`
	Damage    int64           `json:"damage"`
	Killed    bool            `json:"killed"`
	Seed      string          `json:"seed"`
	Team      json.RawMessage `json:"team"`
	Rewards   []rules.Amount  `json:"rewards"`
	RaidLeft  int             `json:"raidLeft"`
	Version   int64           `json:"version"`
	StateHash string          `json:"stateHash"`
}

func (s *Server) raid(w http.ResponseWriter, r *http.Request) {
	ctx := r.Context()
	c, err := s.guildConfig(ctx)
	if err != nil {
		s.internal(w, err)
		return
	}
	account := accountID(r)
	day := guildDay(c, s.nowMs())
	var resp raidResponse
	var ruleErr string
	err = s.store.WithPlayer(ctx, account, func(tx *store.Tx) error {
		m, err := tx.MemberForUpdate(ctx, account)
		if errors.Is(err, store.ErrNotFound) {
			ruleErr = "no_guild"
			return nil
		}
		if err != nil {
			return err
		}
		if m.RaidDay != day {
			m.RaidDay, m.RaidUsed = day, 0
		}
		if m.RaidUsed >= c.Raid.AttemptsPerDay {
			ruleErr = "raid_no_attempts"
			return nil
		}
		g, err := tx.GuildForUpdate(ctx, m.GuildID)
		if err != nil {
			return err
		}
		team, err := s.rules.ArenaTeam(ctx, tx.Player.State)
		if re, ok := rules.IsRuleError(err); ok {
			ruleErr = re.Code
			return nil
		}
		if err != nil {
			return err
		}
		seed := randomUint64()
		battle, err := s.rules.RaidBattle(ctx, team.Team, g.RaidLevel, g.RaidHp, seed)
		if err != nil {
			return err
		}
		resp = raidResponse{Boss: battle.Boss, RaidLevel: g.RaidLevel, HpLeft: g.RaidHp, Damage: battle.Damage, Killed: battle.Killed,
			Seed: fmt.Sprint(seed), Team: json.RawMessage(team.Team)}
		m.RaidUsed++
		m.RaidDamage += battle.Damage
		rewards := append([]rules.Amount{}, battle.Rewards...)
		g.RaidHp -= battle.Damage
		if battle.Killed || g.RaidHp <= 0 {
			// everyone in the guild gets the kill rewards; the others on their next visit
			rewards = append(rewards, battle.KillRewards...)
			pending, _ := json.Marshal(battle.KillRewards)
			if err := tx.AddPending(ctx, g.ID, account, string(pending)); err != nil {
				return err
			}
			g.RaidLevel++
			g.RaidHp = battle.NextHp
			resp.Killed = true
		}
		if err := tx.SaveGuild(ctx, g); err != nil {
			return err
		}
		if err := tx.SaveMember(ctx, m); err != nil {
			return err
		}
		if _, err := s.grant(ctx, tx, "guildRaid", rewards); err != nil {
			return err
		}
		resp.Rewards, resp.RaidLeft = rewards, c.Raid.AttemptsPerDay-m.RaidUsed
		resp.Version, resp.StateHash = tx.Player.Version, tx.Player.StateHash
		return nil
	})
	s.finishGuildWrite(w, err, ruleErr, resp)
}
