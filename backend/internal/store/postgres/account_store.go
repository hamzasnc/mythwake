package postgres

import (
	"context"
	"database/sql"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/auth"
)

type AccountStore struct {
	db *sql.DB
}

func NewAccountStore(db *sql.DB) *AccountStore {
	return &AccountStore{db: db}
}

func (store *AccountStore) EnsureIdentity(ctx context.Context, identity auth.Identity) error {
	tx, err := store.db.BeginTx(ctx, nil)
	if err != nil {
		return err
	}
	defer tx.Rollback()

	if _, err := tx.ExecContext(ctx, `
		INSERT INTO account.players (id)
		VALUES ($1)
		ON CONFLICT (id) DO UPDATE SET updated_at = now()
	`, identity.PlayerID); err != nil {
		return err
	}

	if _, err := tx.ExecContext(ctx, `
		INSERT INTO account.player_auth_identities (
			player_id,
			provider,
			provider_subject,
			email,
			email_verified,
			last_login_at
		)
		VALUES ($1, $2, $3, NULLIF($4, ''), $5, $6)
		ON CONFLICT (provider, provider_subject) DO UPDATE SET
			player_id = EXCLUDED.player_id,
			email = EXCLUDED.email,
			email_verified = EXCLUDED.email_verified,
			last_login_at = EXCLUDED.last_login_at,
			updated_at = now()
	`, identity.PlayerID, identity.Provider, identity.ProviderSubject, identity.Email, identity.EmailVerified, identity.LastLoginAt); err != nil {
		return err
	}

	return tx.Commit()
}

func (store *AccountStore) SaveSession(ctx context.Context, session auth.Session) error {
	_, err := store.db.ExecContext(ctx, `
		INSERT INTO account.player_sessions (
			id,
			player_id,
			provider,
			token_hash,
			issued_at,
			expires_at,
			user_agent
		)
		VALUES ($1, $2, $3, $4, $5, $6, NULLIF($7, ''))
	`, session.SessionID, session.PlayerID, session.Provider, session.TokenHash, session.IssuedAt, session.ExpiresAt, session.UserAgent)
	return err
}

func (store *AccountStore) FindSessionByTokenHash(ctx context.Context, tokenHash string, now time.Time) (auth.Session, bool, error) {
	var session auth.Session
	var lastSeenAt sql.NullTime
	var userAgent sql.NullString

	err := store.db.QueryRowContext(ctx, `
		SELECT
			id,
			player_id,
			provider,
			token_hash,
			issued_at,
			expires_at,
			last_seen_at,
			user_agent
		FROM account.player_sessions
		WHERE token_hash = $1
			AND revoked_at IS NULL
			AND expires_at > $2
	`, tokenHash, now).Scan(
		&session.SessionID,
		&session.PlayerID,
		&session.Provider,
		&session.TokenHash,
		&session.IssuedAt,
		&session.ExpiresAt,
		&lastSeenAt,
		&userAgent,
	)
	if err == sql.ErrNoRows {
		return auth.Session{}, false, nil
	}
	if err != nil {
		return auth.Session{}, false, err
	}
	if lastSeenAt.Valid {
		session.LastSeenAt = lastSeenAt.Time
	}
	if userAgent.Valid {
		session.UserAgent = userAgent.String
	}

	return session, true, nil
}

func (store *AccountStore) TouchSession(ctx context.Context, sessionID string, seenAt time.Time) error {
	_, err := store.db.ExecContext(ctx, `
		UPDATE account.player_sessions
		SET last_seen_at = $2
		WHERE id = $1
			AND revoked_at IS NULL
	`, sessionID, seenAt)
	return err
}
