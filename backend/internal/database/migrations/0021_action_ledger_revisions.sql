ALTER TABLE logs.player_action_ledger
	ADD COLUMN IF NOT EXISTS state_revision bigint;

UPDATE logs.player_action_ledger
SET state_revision = NULLIF(response #>> '{playerSnapshot,revision}', '')::bigint
WHERE state_revision IS NULL
	AND NULLIF(response #>> '{playerSnapshot,revision}', '') IS NOT NULL;

DROP VIEW IF EXISTS debug.v_player_action_ledger_overview;

CREATE OR REPLACE VIEW debug.v_player_action_ledger_overview AS
SELECT
	player_id,
	idempotency_key,
	action_id,
	state_revision,
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

DROP VIEW IF EXISTS debug.v_player_revision_overview;

CREATE OR REPLACE VIEW debug.v_player_revision_overview AS
SELECT
	p.id AS player_id,
	COALESCE(revisions.revision, 1) AS revision,
	revisions.updated_at AS revision_updated_at,
	action_result.action_id AS latest_action_id,
	action_result.idempotency_key AS latest_idempotency_key,
	action_result.updated_at AS latest_action_result_at,
	ledger.state_revision AS latest_ledger_revision,
	ledger.created_at AS latest_ledger_at
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
LEFT JOIN LATERAL (
	SELECT
		state_revision,
		created_at
	FROM logs.player_action_ledger
	WHERE player_id = p.id
	ORDER BY created_at DESC
	LIMIT 1
) ledger ON true
ORDER BY COALESCE(revisions.updated_at, action_result.updated_at, ledger.created_at, p.updated_at) DESC;
