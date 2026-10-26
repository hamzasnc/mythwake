package e2e

import (
	"encoding/json"
	"testing"
)

type guildView struct {
	InGuild bool `json:"inGuild"`
	Guild   *struct {
		ID         string `json:"id"`
		Name       string `json:"name"`
		Members    int    `json:"members"`
		MaxMembers int    `json:"maxMembers"`
	} `json:"guild"`
	Exp       int64  `json:"exp"`
	Role      string `json:"role"`
	Donated   bool   `json:"donated"`
	RaidBoss  string `json:"raidBoss"`
	RaidLevel int    `json:"raidLevel"`
	RaidHp    int64  `json:"raidHp"`
	RaidLeft  int    `json:"raidLeft"`
	Members   []struct {
		Name       string `json:"name"`
		Role       string `json:"role"`
		RaidDamage int64  `json:"raidDamage"`
	} `json:"members"`
}

func guildIDOf(t *testing.T, c *client) string {
	var p player
	c.do("GET", "/v1/player", nil, &p)
	var s struct {
		GuildID *string `json:"guildId"`
	}
	_ = json.Unmarshal(p.State, &s)
	if s.GuildID == nil {
		return ""
	}
	return *s.GuildID
}

func TestGuildLifecycleChatDonationAndRaid(t *testing.T) {
	leader, _ := newPlayer(t)
	member, _ := newPlayer(t)

	var none guildView
	leader.do("GET", "/v1/guild", nil, &none)
	if none.InGuild {
		t.Fatal("a new player has no guild")
	}
	if s := leader.do("POST", "/v1/guilds", map[string]string{"name": "x"}, nil); s != 422 {
		t.Fatalf("too short name: want 422, got %d", s)
	}
	name := "Wacht " + random(4)
	var created struct {
		ID string `json:"id"`
	}
	if s := leader.do("POST", "/v1/guilds", map[string]string{"name": name}, &created); s != 200 || created.ID == "" {
		t.Fatalf("create: %d %+v", s, created)
	}
	if guildIDOf(t, leader) != created.ID {
		t.Fatal("SetGuild must mark the founder's save")
	}
	if s := member.do("POST", "/v1/guilds", map[string]string{"name": name}, nil); s != 422 {
		t.Fatalf("duplicate name: want 422, got %d", s)
	}

	var list struct {
		Guilds []struct {
			ID string `json:"id"`
		} `json:"guilds"`
	}
	member.do("GET", "/v1/guilds?q="+name[len(name)-4:], nil, &list)
	if len(list.Guilds) != 1 || list.Guilds[0].ID != created.ID {
		t.Fatalf("search: %+v", list)
	}
	if s := member.do("POST", "/v1/guilds/"+created.ID+"/join", map[string]any{}, nil); s != 200 {
		t.Fatalf("join: %d", s)
	}
	if s := member.do("POST", "/v1/guilds/"+created.ID+"/join", map[string]any{}, nil); s != 422 {
		t.Fatalf("join twice: want 422, got %d", s)
	}

	if s := member.do("POST", "/v1/guild/chat", map[string]string{"text": "Hallo Dawnhollow"}, nil); s != 200 {
		t.Fatalf("chat: %d", s)
	}
	var chat struct {
		Messages []struct {
			Text string `json:"text"`
		} `json:"messages"`
	}
	leader.do("GET", "/v1/guild/chat", nil, &chat)
	if len(chat.Messages) != 1 || chat.Messages[0].Text != "Hallo Dawnhollow" {
		t.Fatalf("chat read: %+v", chat)
	}

	if s := member.do("POST", "/v1/guild/donate", map[string]string{"donationId": "gems"}, nil); s != 200 {
		t.Fatalf("donate: %d", s)
	}
	if s := member.do("POST", "/v1/guild/donate", map[string]string{"donationId": "gems"}, nil); s != 422 {
		t.Fatalf("donate twice a day: want 422, got %d", s)
	}

	var before guildView
	member.do("GET", "/v1/guild", nil, &before)
	if !before.InGuild || before.Guild.Members != 2 || before.Exp <= 0 || before.RaidBoss != "celestial_turtle" || before.RaidLeft != 2 {
		t.Fatalf("guild view: %+v", before)
	}
	var raid struct {
		Damage   int64  `json:"damage"`
		Seed     string `json:"seed"`
		RaidLeft int    `json:"raidLeft"`
		Rewards  []struct {
			ID string `json:"id"`
		} `json:"rewards"`
	}
	if s := member.do("POST", "/v1/guild/raid", map[string]any{}, &raid); s != 200 || raid.Damage <= 0 || raid.RaidLeft != 1 || raid.Seed == "" {
		t.Fatalf("raid: %d %+v", s, raid)
	}
	var after guildView
	member.do("GET", "/v1/guild", nil, &after)
	if after.RaidHp != before.RaidHp-raid.Damage {
		t.Fatalf("shared boss HP: %d → %d after %d damage", before.RaidHp, after.RaidHp, raid.Damage)
	}
	member.do("POST", "/v1/guild/raid", map[string]any{}, nil)
	if s := member.do("POST", "/v1/guild/raid", map[string]any{}, nil); s != 422 {
		t.Fatalf("third attempt: want 422, got %d", s)
	}

	// the founder leaves: the member becomes leader; when the last one leaves the guild is gone
	if s := leader.do("POST", "/v1/guild/leave", map[string]any{}, nil); s != 200 {
		t.Fatalf("leave: %d", s)
	}
	if guildIDOf(t, leader) != "" {
		t.Fatal("leaving clears the guild in the save")
	}
	var promoted guildView
	member.do("GET", "/v1/guild", nil, &promoted)
	if promoted.Role != "leader" {
		t.Fatalf("next leader: %+v", promoted.Role)
	}
	member.do("POST", "/v1/guild/leave", map[string]any{}, nil)
	member.do("GET", "/v1/guilds?q="+name[len(name)-4:], nil, &list)
	if len(list.Guilds) != 0 {
		t.Fatal("an empty guild is removed")
	}
}
