DROP VIEW IF EXISTS debug.v_player_persistence_overview;
DROP VIEW IF EXISTS debug.v_account_player_overview;
DROP VIEW IF EXISTS debug.v_account_session_overview;

CREATE OR REPLACE VIEW debug.v_account_session_overview AS
SELECT
	s.id,
	s.player_id,
	s.provider,
	left(s.token_hash, 12) AS token_hash_preview,
	s.issued_at,
	s.expires_at,
	s.last_seen_at,
	s.revoked_at,
	(s.revoked_at IS NULL AND s.expires_at > now()) AS is_active,
	GREATEST(0, FLOOR(EXTRACT(EPOCH FROM (s.expires_at - now()))))::integer AS seconds_until_expiry,
	s.user_agent,
	s.created_at
FROM account.player_sessions s
ORDER BY s.created_at DESC;

CREATE OR REPLACE VIEW debug.v_account_player_overview AS
SELECT
	p.id AS player_id,
	p.display_name,
	COALESCE(auth.provider_count, 0) AS auth_provider_count,
	COALESCE(auth.providers, '') AS auth_providers,
	auth.last_login_at,
	COALESCE(sessions.active_session_count, 0) AS active_session_count,
	sessions.last_seen_at,
	COALESCE(progress.campaign_stage, 1) AS campaign_stage,
	COALESCE(progress.gold, 0) AS gold,
	COALESCE(progress.gems, 0) AS gems,
	COALESCE(progress.myth_essence, 0) AS myth_essence,
	COALESCE(progress.pass_xp, 0) AS pass_xp,
	COALESCE(progress.team_power, 0) AS team_power,
	p.created_at,
	p.updated_at
FROM account.players p
LEFT JOIN (
	SELECT
		player_id,
		COUNT(DISTINCT provider) AS provider_count,
		string_agg(DISTINCT provider, ',' ORDER BY provider) AS providers,
		MAX(last_login_at) AS last_login_at
	FROM account.player_auth_identities
	GROUP BY player_id
) auth ON auth.player_id = p.id
LEFT JOIN (
	SELECT
		player_id,
		COUNT(*) FILTER (WHERE revoked_at IS NULL AND expires_at > now()) AS active_session_count,
		MAX(last_seen_at) AS last_seen_at
	FROM account.player_sessions
	GROUP BY player_id
) sessions ON sessions.player_id = p.id
LEFT JOIN debug.v_player_overview progress ON progress.player_id = p.id
ORDER BY p.created_at DESC;

CREATE OR REPLACE VIEW debug.v_player_persistence_overview AS
SELECT
	p.id AS player_id,
	snap.updated_at AS snapshot_updated_at,
	action_result.updated_at AS latest_action_result_at,
	action_result.action_id AS latest_action_id,
	action_result.idempotency_key AS latest_idempotency_key,
	COALESCE(ledger.action_count, 0) AS action_ledger_count,
	ledger.latest_ledger_at,
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
LEFT JOIN debug.v_player_overview progress ON progress.player_id = p.id
ORDER BY COALESCE(action_result.updated_at, snap.updated_at, p.updated_at) DESC;
