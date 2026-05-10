CREATE TABLE IF NOT EXISTS player.player_state_revisions (
	player_id text PRIMARY KEY REFERENCES account.players(id) ON DELETE CASCADE,
	revision bigint NOT NULL CHECK (revision >= 1),
	updated_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO player.player_state_revisions (player_id, revision, updated_at)
SELECT
	p.id,
	1,
	COALESCE(snap.updated_at, p.updated_at, now())
FROM account.players p
LEFT JOIN player.player_state_snapshots snap ON snap.player_id = p.id
ON CONFLICT (player_id) DO NOTHING;

CREATE OR REPLACE VIEW debug.v_player_revision_overview AS
SELECT
	p.id AS player_id,
	COALESCE(revisions.revision, 1) AS revision,
	revisions.updated_at AS revision_updated_at,
	action_result.action_id AS latest_action_id,
	action_result.idempotency_key AS latest_idempotency_key,
	action_result.updated_at AS latest_action_result_at
FROM account.players p
LEFT JOIN player.player_state_revisions revisions ON revisions.player_id = p.id
LEFT JOIN LATERAL (
	SELECT
		action_id,
		idempotency_key,
		updated_at
	FROM player.player_action_results
	WHERE player_id = p.id
	ORDER BY updated_at DESC
	LIMIT 1
) action_result ON true
ORDER BY COALESCE(revisions.updated_at, action_result.updated_at, p.updated_at) DESC;

DROP VIEW IF EXISTS debug.v_player_persistence_overview;

CREATE OR REPLACE VIEW debug.v_player_persistence_overview AS
SELECT
	p.id AS player_id,
	COALESCE(revisions.revision, 1) AS revision,
	revisions.updated_at AS revision_updated_at,
	snap.updated_at AS snapshot_updated_at,
	action_result.updated_at AS latest_action_result_at,
	action_result.action_id AS latest_action_id,
	action_result.idempotency_key AS latest_idempotency_key,
	COALESCE(ledger.action_count, 0) AS action_ledger_count,
	ledger.latest_ledger_at,
	afk.last_claimed_at AS afk_last_claimed_at,
	daily.daily_date,
	COALESCE(daily.fight_count, 0) AS daily_fight_count,
	COALESCE(daily.stage_clear_count, 0) AS daily_stage_clear_count,
	COALESCE(daily.summon_count, 0) AS daily_summon_count,
	COALESCE(progress.campaign_stage, 1) AS campaign_stage,
	COALESCE(progress.gold, 0) AS gold,
	COALESCE(progress.myth_essence, 0) AS myth_essence
FROM account.players p
LEFT JOIN player.player_state_revisions revisions ON revisions.player_id = p.id
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
LEFT JOIN LATERAL (
	SELECT
		daily_date,
		fight_count,
		stage_clear_count,
		summon_count
	FROM player.player_daily_progress
	WHERE player_id = p.id
	ORDER BY daily_date DESC
	LIMIT 1
) daily ON true
LEFT JOIN debug.v_player_overview progress ON progress.player_id = p.id
ORDER BY COALESCE(action_result.updated_at, snap.updated_at, revisions.updated_at, p.updated_at) DESC;
