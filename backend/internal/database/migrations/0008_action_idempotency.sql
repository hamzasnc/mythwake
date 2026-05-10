CREATE TABLE IF NOT EXISTS player.player_action_results (
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	idempotency_key text NOT NULL,
	action_id text NOT NULL,
	request_hash text NOT NULL,
	response jsonb NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),
	PRIMARY KEY (player_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS idx_player_action_results_created_at
	ON player.player_action_results (created_at);

CREATE OR REPLACE VIEW debug.v_player_action_result_overview AS
SELECT
	player_id,
	idempotency_key,
	action_id,
	request_hash,
	response ->> 'message' AS message,
	(response ->> 'success')::boolean AS success,
	created_at,
	updated_at
FROM player.player_action_results
ORDER BY updated_at DESC;
