CREATE TABLE IF NOT EXISTS logs.player_action_ledger (
	id bigserial PRIMARY KEY,
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	idempotency_key text,
	action_id text NOT NULL,
	request_hash text,
	success boolean NOT NULL,
	error_code text,
	reward_id text,
	gold_delta integer NOT NULL DEFAULT 0,
	gems_delta integer NOT NULL DEFAULT 0,
	myth_essence_delta integer NOT NULL DEFAULT 0,
	pass_xp_delta integer NOT NULL DEFAULT 0,
	response jsonb NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now(),
	UNIQUE (player_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS idx_player_action_ledger_player_created_at
	ON logs.player_action_ledger (player_id, created_at DESC);

CREATE OR REPLACE VIEW debug.v_player_action_ledger_overview AS
SELECT
	player_id,
	idempotency_key,
	action_id,
	success,
	error_code,
	reward_id,
	gold_delta,
	gems_delta,
	myth_essence_delta,
	pass_xp_delta,
	created_at
FROM logs.player_action_ledger
ORDER BY created_at DESC;
