CREATE TABLE IF NOT EXISTS account.player_email_credentials (
	player_id text PRIMARY KEY REFERENCES account.players(id) ON DELETE CASCADE,
	email text NOT NULL,
	normalized_email text NOT NULL UNIQUE,
	password_hash text NOT NULL,
	last_login_at timestamptz,
	last_password_changed_at timestamptz NOT NULL DEFAULT now(),
	created_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_player_email_credentials_normalized_email
	ON account.player_email_credentials (normalized_email);

DROP VIEW IF EXISTS debug.v_account_identity_overview;

CREATE OR REPLACE VIEW debug.v_account_identity_overview AS
SELECT
	i.player_id,
	i.provider,
	i.provider_subject,
	i.email,
	i.email_verified,
	CASE WHEN c.player_id IS NULL THEN false ELSE true END AS has_password,
	i.last_login_at,
	i.created_at,
	i.updated_at
FROM account.player_auth_identities i
LEFT JOIN account.player_email_credentials c
	ON c.player_id = i.player_id
	AND i.provider = 'email'
ORDER BY i.player_id, i.provider;
