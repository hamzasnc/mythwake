CREATE TABLE IF NOT EXISTS player.player_summon_state (
	player_id text PRIMARY KEY REFERENCES account.players(id) ON DELETE CASCADE,
	summon_count integer NOT NULL CHECK (summon_count >= 0),
	updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS player.player_daily_mission_claims (
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	mission_id text NOT NULL,
	claimed boolean NOT NULL DEFAULT true,
	claimed_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, mission_id)
);

CREATE TABLE IF NOT EXISTS player.player_battle_pass_claims (
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	reward_id text NOT NULL,
	claimed boolean NOT NULL DEFAULT true,
	claimed_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, reward_id)
);

CREATE TABLE IF NOT EXISTS logs.summon_history (
	id bigserial PRIMARY KEY,
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	banner_id text NOT NULL,
	summon_count integer NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE OR REPLACE VIEW debug.v_player_claim_overview AS
SELECT
	'daily_mission' AS claim_type,
	player_id,
	mission_id AS claim_id,
	claimed,
	claimed_at,
	updated_at
FROM player.player_daily_mission_claims
UNION ALL
SELECT
	'battle_pass' AS claim_type,
	player_id,
	reward_id AS claim_id,
	claimed,
	claimed_at,
	updated_at
FROM player.player_battle_pass_claims;

CREATE OR REPLACE VIEW debug.v_player_summon_overview AS
SELECT
	s.player_id,
	s.summon_count,
	COUNT(h.id) AS summon_history_rows,
	MAX(h.created_at) AS last_summon_at,
	s.updated_at
FROM player.player_summon_state s
LEFT JOIN logs.summon_history h ON h.player_id = s.player_id
GROUP BY s.player_id, s.summon_count, s.updated_at;
