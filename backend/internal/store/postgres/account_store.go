package postgres

import (
	"context"
	"database/sql"

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
