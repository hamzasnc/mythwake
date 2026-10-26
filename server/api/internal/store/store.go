// Package store is the PostgreSQL persistence of accounts, players and the arena.
package store

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
)

var ErrNotFound = errors.New("not found")

type Store struct {
	pool *pgxpool.Pool
}

func New(pool *pgxpool.Pool) *Store { return &Store{pool: pool} }

func (s *Store) Ping(ctx context.Context) error { return s.pool.Ping(ctx) }

// ------------------------------------------------------------------ accounts and devices

type Device struct {
	AccountID  string
	SecretHash string
}

func (s *Store) Device(ctx context.Context, deviceID string) (Device, error) {
	var d Device
	err := s.pool.QueryRow(ctx, "select account_id::text, secret_hash from devices where device_id = $1", deviceID).
		Scan(&d.AccountID, &d.SecretHash)
	if errors.Is(err, pgx.ErrNoRows) {
		return d, ErrNotFound
	}
	return d, err
}

// CreateAccount creates an account with its first device. Returns ErrConflict if the device id is taken meanwhile.
func (s *Store) CreateAccount(ctx context.Context, accountID, deviceID, secretHash string) error {
	return pgx.BeginFunc(ctx, s.pool, func(tx pgx.Tx) error {
		if _, err := tx.Exec(ctx, "insert into accounts (id) values ($1)", accountID); err != nil {
			return err
		}
		_, err := tx.Exec(ctx, "insert into devices (device_id, account_id, secret_hash) values ($1, $2, $3)", deviceID, accountID, secretHash)
		return err
	})
}

func (s *Store) TouchDevice(ctx context.Context, deviceID string) error {
	_, err := s.pool.Exec(ctx, "update devices set last_seen_at = now() where device_id = $1", deviceID)
	return err
}

// ------------------------------------------------------------------ players

type Player struct {
	AccountID      string
	DisplayName    string
	State          string
	StateHash      string
	Version        int64
	ContentVersion string
}

const playerColumns = "account_id::text, display_name, state::text, state_hash, version, content_version"

func scanPlayer(row pgx.Row) (*Player, error) {
	p := &Player{}
	err := row.Scan(&p.AccountID, &p.DisplayName, &p.State, &p.StateHash, &p.Version, &p.ContentVersion)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	return p, err
}

func (s *Store) Player(ctx context.Context, accountID string) (*Player, error) {
	return scanPlayer(s.pool.QueryRow(ctx, "select "+playerColumns+" from players where account_id = $1", accountID))
}

// InsertPlayer stores a new player; if one exists already (concurrent first login) the existing row wins.
func (s *Store) InsertPlayer(ctx context.Context, p *Player) error {
	_, err := s.pool.Exec(ctx, `insert into players (account_id, display_name, state, state_hash, version, content_version)
		values ($1, $2, $3::json, $4, $5, $6) on conflict (account_id) do nothing`,
		p.AccountID, p.DisplayName, p.State, p.StateHash, p.Version, p.ContentVersion)
	return err
}

// Tx is a transaction holding a lock on one player row.
type Tx struct {
	tx     pgx.Tx
	Player *Player
}

// WithPlayer runs fn in a transaction with the player's row locked (SELECT … FOR UPDATE). Changes made through
// the Tx commit together; an error from fn rolls everything back.
func (s *Store) WithPlayer(ctx context.Context, accountID string, fn func(*Tx) error) error {
	return pgx.BeginFunc(ctx, s.pool, func(tx pgx.Tx) error {
		p, err := scanPlayer(tx.QueryRow(ctx, "select "+playerColumns+" from players where account_id = $1 for update", accountID))
		if err != nil {
			return err
		}
		return fn(&Tx{tx: tx, Player: p})
	})
}

// SavePlayer writes the locked player's new state.
func (t *Tx) SavePlayer(ctx context.Context) error {
	p := t.Player
	_, err := t.tx.Exec(ctx, `update players set state = $2::json, state_hash = $3, version = $4, content_version = $5,
		display_name = $6, updated_at = now() where account_id = $1`,
		p.AccountID, p.State, p.StateHash, p.Version, p.ContentVersion, p.DisplayName)
	return err
}

type LoggedCommand struct {
	ID   string
	Type string
	Data json.RawMessage
}

// LogCommands records applied commands; versions are firstVersion, firstVersion+1, …
func (t *Tx) LogCommands(ctx context.Context, commands []LoggedCommand, firstVersion int64) error {
	batch := &pgx.Batch{}
	for i, c := range commands {
		data := c.Data
		if len(data) == 0 {
			data = json.RawMessage("{}")
		}
		batch.Queue(`insert into command_log (account_id, command_id, version, type, data) values ($1, $2, $3, $4, $5)`,
			t.Player.AccountID, c.ID, firstVersion+int64(i), c.Type, data)
	}
	return t.tx.SendBatch(ctx, batch).Close()
}

// AppliedCommands returns how many of the ids are already in the log (idempotent retries).
func (t *Tx) AppliedCommands(ctx context.Context, ids []string) (int, error) {
	var n int
	err := t.tx.QueryRow(ctx, "select count(*) from command_log where account_id = $1 and command_id::text = any($2)",
		t.Player.AccountID, ids).Scan(&n)
	return n, err
}

// ------------------------------------------------------------------ arena

type ArenaPlayer struct {
	AccountID  string
	Name       string
	IsBot      bool
	Season     int
	Rating     int
	Defense    string
	Power      int64
	Wins       int
	Losses     int
	FightsDay  int64
	FightsUsed int
}

const arenaColumns = "account_id::text, name, is_bot, season, rating, defense::text, power, wins, losses, fights_day, fights_used"

func scanArena(row pgx.Row) (*ArenaPlayer, error) {
	a := &ArenaPlayer{}
	err := row.Scan(&a.AccountID, &a.Name, &a.IsBot, &a.Season, &a.Rating, &a.Defense, &a.Power, &a.Wins, &a.Losses, &a.FightsDay, &a.FightsUsed)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	return a, err
}

func (s *Store) Arena(ctx context.Context, accountID string) (*ArenaPlayer, error) {
	return scanArena(s.pool.QueryRow(ctx, "select "+arenaColumns+" from arena_players where account_id = $1", accountID))
}

// ArenaForUpdate locks an arena row inside a player transaction.
func (t *Tx) ArenaForUpdate(ctx context.Context, accountID string) (*ArenaPlayer, error) {
	return scanArena(t.tx.QueryRow(ctx, "select "+arenaColumns+" from arena_players where account_id = $1 for update", accountID))
}

func (t *Tx) SaveArena(ctx context.Context, a *ArenaPlayer) error {
	_, err := t.tx.Exec(ctx, `insert into arena_players (account_id, name, is_bot, season, rating, defense, power, wins, losses, fights_day, fights_used)
		values ($1, $2, $3, $4, $5, $6::json, $7, $8, $9, $10, $11)
		on conflict (account_id) do update set name = excluded.name, season = excluded.season, rating = excluded.rating,
			defense = excluded.defense, power = excluded.power, wins = excluded.wins, losses = excluded.losses,
			fights_day = excluded.fights_day, fights_used = excluded.fights_used, updated_at = now()`,
		a.AccountID, a.Name, a.IsBot, a.Season, a.Rating, a.Defense, a.Power, a.Wins, a.Losses, a.FightsDay, a.FightsUsed)
	return err
}

func (t *Tx) LogArenaBattle(ctx context.Context, season int, attacker, defender string, seed uint64, victory bool, change int) error {
	_, err := t.tx.Exec(ctx, `insert into arena_battles (season, attacker, defender, seed, victory, rating_change)
		values ($1, $2, $3, $4, $5, $6)`, season, attacker, defender, int64(seed), victory, change)
	return err
}

// Opponents returns up to limit arena players of the season closest in rating, never the player itself.
func (s *Store) Opponents(ctx context.Context, season, rating int, exclude string, limit int) ([]*ArenaPlayer, error) {
	rows, err := s.pool.Query(ctx, "select "+arenaColumns+` from arena_players
		where account_id <> $3 and (season = $1 or is_bot)
		order by abs(rating - $2), account_id limit $4`, season, rating, exclude, limit)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var list []*ArenaPlayer
	for rows.Next() {
		a, err := scanArena(rows)
		if err != nil {
			return nil, err
		}
		list = append(list, a)
	}
	return list, rows.Err()
}

// Ranking returns the top players of a season.
func (s *Store) Ranking(ctx context.Context, season, limit int) ([]*ArenaPlayer, error) {
	rows, err := s.pool.Query(ctx, "select "+arenaColumns+` from arena_players
		where season = $1 or is_bot order by rating desc, account_id limit $2`, season, limit)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var list []*ArenaPlayer
	for rows.Next() {
		a, err := scanArena(rows)
		if err != nil {
			return nil, err
		}
		list = append(list, a)
	}
	return list, rows.Err()
}

func (s *Store) BotCount(ctx context.Context) (int, error) {
	var n int
	err := s.pool.QueryRow(ctx, "select count(*) from arena_players where is_bot").Scan(&n)
	return n, err
}

// InsertBot creates a bot account and its arena row.
func (s *Store) InsertBot(ctx context.Context, a *ArenaPlayer) error {
	return pgx.BeginFunc(ctx, s.pool, func(tx pgx.Tx) error {
		if _, err := tx.Exec(ctx, "insert into accounts (id) values ($1)", a.AccountID); err != nil {
			return fmt.Errorf("bot account: %w", err)
		}
		t := &Tx{tx: tx}
		return t.SaveArena(ctx, a)
	})
}

// LockArenaRow locks another player's arena row in this transaction (the defender of a fight).
func (t *Tx) LockArenaRow(ctx context.Context, accountID string) (*ArenaPlayer, error) {
	return t.ArenaForUpdate(ctx, accountID)
}
