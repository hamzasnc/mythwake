package postgres

import (
	"context"
	"database/sql"
	"encoding/json"
	"errors"

	"github.com/hamzasnc/mythwake/backend/internal/api"
	"github.com/hamzasnc/mythwake/backend/internal/player"
)

type PlayerStateStore struct {
	db *sql.DB
}

func NewPlayerStateStore(db *sql.DB) *PlayerStateStore {
	return &PlayerStateStore{db: db}
}

func (store *PlayerStateStore) LoadState(ctx context.Context, playerID string) (player.PersistentState, bool, error) {
	state, found, err := store.loadNormalizedState(ctx, playerID)
	if err != nil || found {
		return state, found, err
	}

	return store.loadSnapshotState(ctx, playerID)
}

func (store *PlayerStateStore) SaveState(ctx context.Context, playerID string, state player.PersistentState, source player.StateSaveSource) error {
	rawState, err := json.Marshal(state.PlayerState)
	if err != nil {
		return err
	}

	tx, err := store.db.BeginTx(ctx, nil)
	if err != nil {
		return err
	}
	defer tx.Rollback()

	if _, err := tx.ExecContext(ctx, `
		INSERT INTO account.players (id)
		VALUES ($1)
		ON CONFLICT (id) DO UPDATE SET updated_at = now()
	`, playerID); err != nil {
		return err
	}

	previousCurrencies, err := store.loadCurrenciesForUpdate(ctx, tx, playerID)
	if err != nil {
		return err
	}

	if err := store.saveCoreState(ctx, tx, playerID, state.PlayerState); err != nil {
		return err
	}
	if err := store.saveHeroState(ctx, tx, playerID, state); err != nil {
		return err
	}
	if err := store.saveEconomyTransaction(ctx, tx, playerID, previousCurrencies, state.PlayerState, source); err != nil {
		return err
	}
	if _, err := tx.ExecContext(ctx, `
		INSERT INTO player.player_state_snapshots (player_id, state, updated_at)
		VALUES ($1, $2, now())
		ON CONFLICT (player_id) DO UPDATE SET
			state = EXCLUDED.state,
			updated_at = now()
	`, playerID, rawState); err != nil {
		return err
	}

	return tx.Commit()
}

func (store *PlayerStateStore) loadSnapshotState(ctx context.Context, playerID string) (player.PersistentState, bool, error) {
	var rawState []byte
	err := store.db.QueryRowContext(ctx, `
		SELECT state
		FROM player.player_state_snapshots
		WHERE player_id = $1
	`, playerID).Scan(&rawState)
	if errors.Is(err, sql.ErrNoRows) {
		return player.PersistentState{}, false, nil
	}
	if err != nil {
		return player.PersistentState{}, false, err
	}

	var state api.PlayerState
	if err := json.Unmarshal(rawState, &state); err != nil {
		return player.PersistentState{}, false, err
	}

	return player.PersistentState{PlayerState: state}, true, nil
}

func (store *PlayerStateStore) loadNormalizedState(ctx context.Context, playerID string) (player.PersistentState, bool, error) {
	var state api.PlayerState
	err := store.db.QueryRowContext(ctx, `
		SELECT save_version, team_power, team_attack, team_health
		FROM player.player_combat_stats
		WHERE player_id = $1
	`, playerID).Scan(&state.SaveVersion, &state.TeamPower, &state.TeamAttack, &state.TeamHealth)
	if errors.Is(err, sql.ErrNoRows) {
		return player.PersistentState{}, false, nil
	}
	if err != nil {
		return player.PersistentState{}, false, err
	}

	if err := store.db.QueryRowContext(ctx, `
		SELECT current_stage
		FROM player.player_campaign_progress
		WHERE player_id = $1
	`, playerID).Scan(&state.CampaignStage); err != nil {
		return player.PersistentState{}, false, err
	}

	currencies, err := store.loadCurrencies(ctx, playerID)
	if err != nil {
		return player.PersistentState{}, false, err
	}
	state.Gold = currencies["gold"]
	state.Gems = currencies["gems"]
	state.MythEssence = currencies["myth_essence"]
	state.PassXP = currencies["pass_xp"]

	dungeons, err := store.loadDungeons(ctx, playerID)
	if err != nil {
		return player.PersistentState{}, false, err
	}
	state.GoldDungeonFloor = dungeons["gold_dungeon"]
	state.EssenceDungeonFloor = dungeons["essence_dungeon"]
	state.GearDungeonFloor = dungeons["gear_dungeon"]

	heroLevels, heroAscensions, err := store.loadHeroes(ctx, playerID)
	if err != nil {
		return player.PersistentState{}, false, err
	}
	heroShards, err := store.loadHeroShards(ctx, playerID)
	if err != nil {
		return player.PersistentState{}, false, err
	}

	return player.PersistentState{
		PlayerState:    state,
		HeroLevels:     heroLevels,
		HeroShards:     heroShards,
		HeroAscensions: heroAscensions,
	}, true, nil
}

func (store *PlayerStateStore) saveCoreState(ctx context.Context, tx *sql.Tx, playerID string, state api.PlayerState) error {
	if _, err := tx.ExecContext(ctx, `
		INSERT INTO player.player_combat_stats (player_id, save_version, team_power, team_attack, team_health, updated_at)
		VALUES ($1, $2, $3, $4, $5, now())
		ON CONFLICT (player_id) DO UPDATE SET
			save_version = EXCLUDED.save_version,
			team_power = EXCLUDED.team_power,
			team_attack = EXCLUDED.team_attack,
			team_health = EXCLUDED.team_health,
			updated_at = now()
	`, playerID, state.SaveVersion, state.TeamPower, state.TeamAttack, state.TeamHealth); err != nil {
		return err
	}

	if _, err := tx.ExecContext(ctx, `
		INSERT INTO player.player_campaign_progress (player_id, current_stage, updated_at)
		VALUES ($1, $2, now())
		ON CONFLICT (player_id) DO UPDATE SET
			current_stage = EXCLUDED.current_stage,
			updated_at = now()
	`, playerID, state.CampaignStage); err != nil {
		return err
	}

	currencies := map[string]int{
		"gold":         state.Gold,
		"gems":         state.Gems,
		"myth_essence": state.MythEssence,
		"pass_xp":      state.PassXP,
	}
	for currencyID, amount := range currencies {
		if _, err := tx.ExecContext(ctx, `
			INSERT INTO player.player_currencies (player_id, currency_id, amount, updated_at)
			VALUES ($1, $2, $3, now())
			ON CONFLICT (player_id, currency_id) DO UPDATE SET
				amount = EXCLUDED.amount,
				updated_at = now()
		`, playerID, currencyID, amount); err != nil {
			return err
		}
	}

	dungeons := map[string]int{
		"gold_dungeon":    state.GoldDungeonFloor,
		"essence_dungeon": state.EssenceDungeonFloor,
		"gear_dungeon":    state.GearDungeonFloor,
	}
	for dungeonID, floor := range dungeons {
		if _, err := tx.ExecContext(ctx, `
			INSERT INTO player.player_dungeon_progress (player_id, dungeon_id, current_floor, updated_at)
			VALUES ($1, $2, $3, now())
			ON CONFLICT (player_id, dungeon_id) DO UPDATE SET
				current_floor = EXCLUDED.current_floor,
				updated_at = now()
		`, playerID, dungeonID, floor); err != nil {
			return err
		}
	}

	return nil
}

func (store *PlayerStateStore) saveHeroState(ctx context.Context, tx *sql.Tx, playerID string, state player.PersistentState) error {
	for heroID, level := range state.HeroLevels {
		if _, err := tx.ExecContext(ctx, `
			INSERT INTO player.player_heroes (player_id, hero_id, level, ascension, updated_at)
			VALUES ($1, $2, $3, $4, now())
			ON CONFLICT (player_id, hero_id) DO UPDATE SET
				level = EXCLUDED.level,
				ascension = EXCLUDED.ascension,
				updated_at = now()
		`, playerID, heroID, level, state.HeroAscensions[heroID]); err != nil {
			return err
		}
	}

	for heroID, shards := range state.HeroShards {
		if _, err := tx.ExecContext(ctx, `
			INSERT INTO player.player_hero_shards (player_id, hero_id, shards, updated_at)
			VALUES ($1, $2, $3, now())
			ON CONFLICT (player_id, hero_id) DO UPDATE SET
				shards = EXCLUDED.shards,
				updated_at = now()
		`, playerID, heroID, shards); err != nil {
			return err
		}
	}

	return nil
}

func (store *PlayerStateStore) saveEconomyTransaction(ctx context.Context, tx *sql.Tx, playerID string, previous map[string]int, state api.PlayerState, source player.StateSaveSource) error {
	if source.ActionID == "" {
		source.ActionID = "player_state_save"
	}

	goldDelta := state.Gold - previous["gold"]
	gemsDelta := state.Gems - previous["gems"]
	mythEssenceDelta := state.MythEssence - previous["myth_essence"]
	passXPDelta := state.PassXP - previous["pass_xp"]
	if goldDelta == 0 && gemsDelta == 0 && mythEssenceDelta == 0 && passXPDelta == 0 {
		return nil
	}

	_, err := tx.ExecContext(ctx, `
		INSERT INTO logs.economy_transactions (
			player_id,
			action_id,
			reward_id,
			gold_delta,
			gems_delta,
			myth_essence_delta,
			pass_xp_delta
		)
		VALUES ($1, $2, NULLIF($3, ''), $4, $5, $6, $7)
	`, playerID, source.ActionID, source.RewardID, goldDelta, gemsDelta, mythEssenceDelta, passXPDelta)
	return err
}

func (store *PlayerStateStore) loadCurrencies(ctx context.Context, playerID string) (map[string]int, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT currency_id, amount
		FROM player.player_currencies
		WHERE player_id = $1
	`, playerID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	currencies := map[string]int{}
	for rows.Next() {
		var currencyID string
		var amount int
		if err := rows.Scan(&currencyID, &amount); err != nil {
			return nil, err
		}
		currencies[currencyID] = amount
	}

	return currencies, rows.Err()
}

func (store *PlayerStateStore) loadCurrenciesForUpdate(ctx context.Context, tx *sql.Tx, playerID string) (map[string]int, error) {
	rows, err := tx.QueryContext(ctx, `
		SELECT currency_id, amount
		FROM player.player_currencies
		WHERE player_id = $1
		FOR UPDATE
	`, playerID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	currencies := map[string]int{}
	for rows.Next() {
		var currencyID string
		var amount int
		if err := rows.Scan(&currencyID, &amount); err != nil {
			return nil, err
		}
		currencies[currencyID] = amount
	}

	return currencies, rows.Err()
}

func (store *PlayerStateStore) loadDungeons(ctx context.Context, playerID string) (map[string]int, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT dungeon_id, current_floor
		FROM player.player_dungeon_progress
		WHERE player_id = $1
	`, playerID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	dungeons := map[string]int{}
	for rows.Next() {
		var dungeonID string
		var floor int
		if err := rows.Scan(&dungeonID, &floor); err != nil {
			return nil, err
		}
		dungeons[dungeonID] = floor
	}

	return dungeons, rows.Err()
}

func (store *PlayerStateStore) loadHeroes(ctx context.Context, playerID string) (map[string]int, map[string]int, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT hero_id, level, ascension
		FROM player.player_heroes
		WHERE player_id = $1
	`, playerID)
	if err != nil {
		return nil, nil, err
	}
	defer rows.Close()

	levels := map[string]int{}
	ascensions := map[string]int{}
	for rows.Next() {
		var heroID string
		var level int
		var ascension int
		if err := rows.Scan(&heroID, &level, &ascension); err != nil {
			return nil, nil, err
		}
		levels[heroID] = level
		ascensions[heroID] = ascension
	}

	return levels, ascensions, rows.Err()
}

func (store *PlayerStateStore) loadHeroShards(ctx context.Context, playerID string) (map[string]int, error) {
	rows, err := store.db.QueryContext(ctx, `
		SELECT hero_id, shards
		FROM player.player_hero_shards
		WHERE player_id = $1
	`, playerID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	shards := map[string]int{}
	for rows.Next() {
		var heroID string
		var amount int
		if err := rows.Scan(&heroID, &amount); err != nil {
			return nil, err
		}
		shards[heroID] = amount
	}

	return shards, rows.Err()
}
