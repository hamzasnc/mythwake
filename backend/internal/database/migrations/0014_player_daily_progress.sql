CREATE TABLE IF NOT EXISTS player.player_daily_progress (
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	daily_date date NOT NULL,
	fight_count integer NOT NULL DEFAULT 0 CHECK (fight_count >= 0),
	stage_clear_count integer NOT NULL DEFAULT 0 CHECK (stage_clear_count >= 0),
	summon_count integer NOT NULL DEFAULT 0 CHECK (summon_count >= 0),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, daily_date)
);

ALTER TABLE player.player_daily_mission_claims
	ADD COLUMN IF NOT EXISTS daily_date date;

UPDATE player.player_daily_mission_claims
SET daily_date = (claimed_at AT TIME ZONE 'UTC')::date
WHERE daily_date IS NULL;

ALTER TABLE player.player_daily_mission_claims
	ALTER COLUMN daily_date SET NOT NULL;

ALTER TABLE player.player_daily_mission_claims
	DROP CONSTRAINT IF EXISTS player_daily_mission_claims_pkey;

ALTER TABLE player.player_daily_mission_claims
	ADD PRIMARY KEY (player_id, daily_date, mission_id);

INSERT INTO player.player_daily_progress (player_id, daily_date, updated_at)
SELECT
	player_id,
	MAX(daily_date) AS daily_date,
	now()
FROM player.player_daily_mission_claims
GROUP BY player_id
ON CONFLICT (player_id, daily_date) DO NOTHING;

DROP VIEW IF EXISTS debug.v_player_claim_overview;
DROP VIEW IF EXISTS debug.v_player_persistence_overview;

CREATE OR REPLACE VIEW debug.v_player_claim_overview AS
SELECT
	'daily_mission' AS claim_type,
	player_id,
	daily_date::text AS reset_key,
	mission_id AS claim_id,
	claimed,
	claimed_at,
	updated_at
FROM player.player_daily_mission_claims
UNION ALL
SELECT
	'battle_pass' AS claim_type,
	player_id,
	'account' AS reset_key,
	reward_id AS claim_id,
	claimed,
	claimed_at,
	updated_at
FROM player.player_battle_pass_claims;

CREATE OR REPLACE VIEW debug.v_player_daily_progress_overview AS
SELECT
	p.player_id,
	p.daily_date,
	p.fight_count,
	p.stage_clear_count,
	p.summon_count,
	COALESCE(claims.claimed_daily_count, 0) AS claimed_daily_count,
	p.updated_at
FROM player.player_daily_progress p
LEFT JOIN (
	SELECT
		player_id,
		daily_date,
		COUNT(*) FILTER (WHERE claimed) AS claimed_daily_count
	FROM player.player_daily_mission_claims
	GROUP BY player_id, daily_date
) claims ON claims.player_id = p.player_id AND claims.daily_date = p.daily_date
ORDER BY p.updated_at DESC;

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
	daily.daily_date,
	COALESCE(daily.fight_count, 0) AS daily_fight_count,
	COALESCE(daily.stage_clear_count, 0) AS daily_stage_clear_count,
	COALESCE(daily.summon_count, 0) AS daily_summon_count,
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
ORDER BY COALESCE(action_result.updated_at, snap.updated_at, p.updated_at) DESC;
