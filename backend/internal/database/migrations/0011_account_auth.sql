CREATE TABLE IF NOT EXISTS account.player_auth_identities (
	id bigserial PRIMARY KEY,
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	provider text NOT NULL CHECK (provider IN ('guest', 'email', 'google', 'apple')),
	provider_subject text NOT NULL,
	email text,
	email_verified boolean NOT NULL DEFAULT false,
	created_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),
	last_login_at timestamptz,
	UNIQUE (provider, provider_subject),
	UNIQUE (player_id, provider, provider_subject)
);

CREATE INDEX IF NOT EXISTS idx_player_auth_identities_player_id
	ON account.player_auth_identities (player_id);

CREATE INDEX IF NOT EXISTS idx_player_auth_identities_email
	ON account.player_auth_identities (lower(email))
	WHERE email IS NOT NULL;

CREATE TABLE IF NOT EXISTS account.player_sessions (
	id text PRIMARY KEY,
	player_id text NOT NULL REFERENCES account.players(id) ON DELETE CASCADE,
	provider text NOT NULL CHECK (provider IN ('guest', 'email', 'google', 'apple')),
	token_hash text NOT NULL UNIQUE,
	issued_at timestamptz NOT NULL,
	expires_at timestamptz NOT NULL,
	last_seen_at timestamptz,
	revoked_at timestamptz,
	user_agent text,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_player_sessions_player_active
	ON account.player_sessions (player_id, expires_at)
	WHERE revoked_at IS NULL;

CREATE OR REPLACE VIEW debug.v_account_identity_overview AS
SELECT
	i.player_id,
	i.provider,
	i.provider_subject,
	i.email,
	i.email_verified,
	i.last_login_at,
	i.created_at,
	i.updated_at
FROM account.player_auth_identities i
ORDER BY i.player_id, i.provider;

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
	s.created_at
FROM account.player_sessions s
ORDER BY s.created_at DESC;
