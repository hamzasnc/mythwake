package postgres

import (
	"context"
	"database/sql"
	"encoding/json"
	"errors"

	"github.com/hamzasnc/mythwake/backend/internal/api"
)

type PlayerStateStore struct {
	db *sql.DB
}

func NewPlayerStateStore(db *sql.DB) *PlayerStateStore {
	return &PlayerStateStore{db: db}
}

func (store *PlayerStateStore) LoadState(ctx context.Context, playerID string) (api.PlayerState, bool, error) {
	var rawState []byte
	err := store.db.QueryRowContext(ctx, `
		SELECT state
		FROM player_state_snapshots
		WHERE player_id = $1
	`, playerID).Scan(&rawState)
	if errors.Is(err, sql.ErrNoRows) {
		return api.PlayerState{}, false, nil
	}
	if err != nil {
		return api.PlayerState{}, false, err
	}

	var state api.PlayerState
	if err := json.Unmarshal(rawState, &state); err != nil {
		return api.PlayerState{}, false, err
	}

	return state, true, nil
}

func (store *PlayerStateStore) SaveState(ctx context.Context, playerID string, state api.PlayerState) error {
	rawState, err := json.Marshal(state)
	if err != nil {
		return err
	}

	tx, err := store.db.BeginTx(ctx, nil)
	if err != nil {
		return err
	}
	defer tx.Rollback()

	if _, err := tx.ExecContext(ctx, `
		INSERT INTO players (id)
		VALUES ($1)
		ON CONFLICT (id) DO UPDATE SET updated_at = now()
	`, playerID); err != nil {
		return err
	}

	if _, err := tx.ExecContext(ctx, `
		INSERT INTO player_state_snapshots (player_id, state, updated_at)
		VALUES ($1, $2, now())
		ON CONFLICT (player_id) DO UPDATE SET
			state = EXCLUDED.state,
			updated_at = now()
	`, playerID, rawState); err != nil {
		return err
	}

	return tx.Commit()
}
