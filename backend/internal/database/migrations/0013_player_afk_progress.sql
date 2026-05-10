CREATE TABLE IF NOT EXISTS player.player_afk_progress (
	player_id text PRIMARY KEY REFERENCES account.players(id) ON DELETE CASCADE,
	last_claimed_at timestamptz NOT NULL,
	updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE OR REPLACE VIEW debug.v_player_afk_overview AS
SELECT
	p.player_id,
	p.last_claimed_at,
	GREATEST(0, FLOOR(EXTRACT(EPOCH FROM (now() - p.last_claimed_at))))::integer AS unclaimed_seconds,
	LEAST(21600, GREATEST(0, FLOOR(EXTRACT(EPOCH FROM (now() - p.last_claimed_at)))))::integer AS claimable_seconds_capped,
	p.updated_at
FROM player.player_afk_progress p;

DROP VIEW IF EXISTS debug.v_player_persistence_overview;

CREATE OR REPLACE VIEW debug.v_player_persistence_overview AS
SELECT
	p.id AS player_id,
	snap.updated_at AS snapshot_updated_at,
	action_result.updated_at AS latest_action_result_at,
	action_result.action_id AS latest_action_id,
	action_result.idempotency_key AS latest_idempotency_key,
	COALESCE(ledger.action_count, 0) AS action_ledger_count,
	ledger.latest_ledger_at,
	afk.last_claimed_at AS afk_last_claimed_at,
	COALESCE(progress.campaign_stage, 1) AS campaign_stage,
	COALESCE(progress.gold, 0) AS gold,
	COALESCE(progress.myth_essence, 0) AS myth_essence
FROM account.players p
LEFT JOIN player.player_state_snapshots snap ON snap.player_id = p.id
LEFT JOIN LATERAL (
	SELECT
		idempotency_key,
		action_id,
		updated_at
	FROM player.player_action_results
	WHERE player_id = p.id
	ORDER BY updated_at DESC
	LIMIT 1
) action_result ON true
LEFT JOIN (
	SELECT
		player_id,
		COUNT(*) AS action_count,
		MAX(created_at) AS latest_ledger_at
	FROM logs.player_action_ledger
	GROUP BY player_id
) ledger ON ledger.player_id = p.id
LEFT JOIN player.player_afk_progress afk ON afk.player_id = p.id
LEFT JOIN debug.v_player_overview progress ON progress.player_id = p.id
ORDER BY COALESCE(action_result.updated_at, snap.updated_at, p.updated_at) DESC;
