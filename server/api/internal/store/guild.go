package store

import (
	"context"
	"errors"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgconn"
)

// ErrConflict is returned when a unique value (e.g. a guild name) is taken.
var ErrConflict = errors.New("conflict")

type Guild struct {
	ID        string
	Name      string
	Leader    string
	Exp       int64
	RaidLevel int
	RaidHp    int64
	Members   int
}

type GuildMember struct {
	AccountID  string
	GuildID    string
	Name       string
	Role       string
	DonateDay  int64
	RaidDay    int64
	RaidUsed   int
	RaidDamage int64
	Pending    string
}

type GuildMessage struct {
	ID        int64  `json:"id"`
	AccountID string `json:"accountId"`
	Name      string `json:"name"`
	Text      string `json:"text"`
	AtMs      int64  `json:"atMs"`
}

const guildColumns = `g.id::text, g.name, g.leader::text, g.exp, g.raid_level, g.raid_hp,
	(select count(*) from guild_members m where m.guild_id = g.id)`

func scanGuild(row pgx.Row) (*Guild, error) {
	g := &Guild{}
	err := row.Scan(&g.ID, &g.Name, &g.Leader, &g.Exp, &g.RaidLevel, &g.RaidHp, &g.Members)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	return g, err
}

const memberColumns = "account_id::text, guild_id::text, name, role, donate_day, raid_day, raid_used, raid_damage, pending::text"

func scanMember(row pgx.Row) (*GuildMember, error) {
	m := &GuildMember{}
	err := row.Scan(&m.AccountID, &m.GuildID, &m.Name, &m.Role, &m.DonateDay, &m.RaidDay, &m.RaidUsed, &m.RaidDamage, &m.Pending)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	return m, err
}

// Guilds lists guilds, biggest raid progress first; q filters by name.
func (s *Store) Guilds(ctx context.Context, q string, limit int) ([]*Guild, error) {
	rows, err := s.pool.Query(ctx, "select "+guildColumns+` from guilds g
		where $1 = '' or g.name_key like '%' || lower($1) || '%'
		order by g.raid_level desc, g.exp desc, g.name_key limit $2`, q, limit)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var list []*Guild
	for rows.Next() {
		g, err := scanGuild(rows)
		if err != nil {
			return nil, err
		}
		list = append(list, g)
	}
	return list, rows.Err()
}

func (s *Store) Messages(ctx context.Context, guildID string, after int64, limit int) ([]GuildMessage, error) {
	rows, err := s.pool.Query(ctx, `select id, account_id::text, name, text, (extract(epoch from created_at) * 1000)::bigint
		from (select * from guild_messages where guild_id = $1 and id > $2 order by id desc limit $3) m order by id`, guildID, after, limit)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var list []GuildMessage
	for rows.Next() {
		var m GuildMessage
		if err := rows.Scan(&m.ID, &m.AccountID, &m.Name, &m.Text, &m.AtMs); err != nil {
			return nil, err
		}
		list = append(list, m)
	}
	return list, rows.Err()
}

func (s *Store) Members(ctx context.Context, guildID string) ([]*GuildMember, error) {
	rows, err := s.pool.Query(ctx, "select "+memberColumns+" from guild_members where guild_id = $1 order by raid_damage desc, joined_at", guildID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var list []*GuildMember
	for rows.Next() {
		m, err := scanMember(rows)
		if err != nil {
			return nil, err
		}
		list = append(list, m)
	}
	return list, rows.Err()
}

// ------------------------------------------------------------------ inside a player transaction

func (t *Tx) MemberForUpdate(ctx context.Context, accountID string) (*GuildMember, error) {
	return scanMember(t.tx.QueryRow(ctx, "select "+memberColumns+" from guild_members where account_id = $1 for update", accountID))
}

func (t *Tx) GuildForUpdate(ctx context.Context, id string) (*Guild, error) {
	return scanGuild(t.tx.QueryRow(ctx, "select "+guildColumns+" from guilds g where g.id = $1 for update", id))
}

// InsertGuild creates a guild; ErrConflict when the name is taken.
func (t *Tx) InsertGuild(ctx context.Context, g *Guild) error {
	_, err := t.tx.Exec(ctx, `insert into guilds (id, name, name_key, leader, raid_level, raid_hp) values ($1, $2, lower($2), $3, $4, $5)`,
		g.ID, g.Name, g.Leader, g.RaidLevel, g.RaidHp)
	var pg *pgconn.PgError
	if errors.As(err, &pg) && pg.Code == "23505" {
		return ErrConflict
	}
	return err
}

func (t *Tx) SaveGuild(ctx context.Context, g *Guild) error {
	_, err := t.tx.Exec(ctx, "update guilds set leader = $2, exp = $3, raid_level = $4, raid_hp = $5 where id = $1",
		g.ID, g.Leader, g.Exp, g.RaidLevel, g.RaidHp)
	return err
}

func (t *Tx) DeleteGuild(ctx context.Context, id string) error {
	_, err := t.tx.Exec(ctx, "delete from guilds where id = $1", id)
	return err
}

func (t *Tx) SaveMember(ctx context.Context, m *GuildMember) error {
	_, err := t.tx.Exec(ctx, `insert into guild_members (account_id, guild_id, name, role, donate_day, raid_day, raid_used, raid_damage, pending)
		values ($1, $2, $3, $4, $5, $6, $7, $8, $9::json)
		on conflict (account_id) do update set guild_id = excluded.guild_id, name = excluded.name, role = excluded.role,
			donate_day = excluded.donate_day, raid_day = excluded.raid_day, raid_used = excluded.raid_used,
			raid_damage = excluded.raid_damage, pending = excluded.pending`,
		m.AccountID, m.GuildID, m.Name, m.Role, m.DonateDay, m.RaidDay, m.RaidUsed, m.RaidDamage, m.Pending)
	return err
}

func (t *Tx) DeleteMember(ctx context.Context, accountID string) error {
	_, err := t.tx.Exec(ctx, "delete from guild_members where account_id = $1", accountID)
	return err
}

// OldestMember returns the member who joined first (the next leader), or ErrNotFound.
func (t *Tx) OldestMember(ctx context.Context, guildID string) (*GuildMember, error) {
	return scanMember(t.tx.QueryRow(ctx, "select "+memberColumns+" from guild_members where guild_id = $1 order by joined_at limit 1 for update", guildID))
}

// AddPending appends rewards (a JSON array) to every member of the guild except one.
func (t *Tx) AddPending(ctx context.Context, guildID, except, rewards string) error {
	_, err := t.tx.Exec(ctx, `update guild_members set pending = (pending::jsonb || $3::jsonb)::json
		where guild_id = $1 and account_id <> $2`, guildID, except, rewards)
	return err
}

func (t *Tx) AddMessage(ctx context.Context, guildID, accountID, name, text string) error {
	_, err := t.tx.Exec(ctx, "insert into guild_messages (guild_id, account_id, name, text) values ($1, $2, $3, $4)", guildID, accountID, name, text)
	return err
}
