package postgres

import (
	"context"
	"database/sql"
	"errors"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/auth"
	"github.com/jackc/pgx/v5/pgconn"
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

func (store *AccountStore) CreateEmailCredential(ctx context.Context, identity auth.Identity, passwordHash string) error {
	normalizedEmail := auth.NormalizeEmail(identity.Email)
	identity.Provider = auth.ProviderEmail
	identity.ProviderSubject = normalizedEmail
	identity.Email = normalizedEmail

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
		VALUES ($1, $2, $3, $4, $5, $6)
	`, identity.PlayerID, identity.Provider, identity.ProviderSubject, identity.Email, identity.EmailVerified, identity.LastLoginAt); err != nil {
		if isUniqueViolation(err) {
			return auth.ErrEmailAlreadyRegistered
		}
		return err
	}

	if _, err := tx.ExecContext(ctx, `
		INSERT INTO account.player_email_credentials (
			player_id,
			email,
			normalized_email,
			password_hash,
			last_login_at
		)
		VALUES ($1, $2, $3, $4, $5)
	`, identity.PlayerID, identity.Email, normalizedEmail, passwordHash, identity.LastLoginAt); err != nil {
		if isUniqueViolation(err) {
			return auth.ErrEmailAlreadyRegistered
		}
		return err
	}

	return tx.Commit()
}

func (store *AccountStore) FindEmailCredential(ctx context.Context, normalizedEmail string) (auth.EmailCredential, bool, error) {
	normalizedEmail = auth.NormalizeEmail(normalizedEmail)

	var credential auth.EmailCredential
	var lastLoginAt sql.NullTime
	err := store.db.QueryRowContext(ctx, `
		SELECT
			player_id,
			email,
			normalized_email,
			password_hash,
			last_login_at
		FROM account.player_email_credentials
		WHERE normalized_email = $1
	`, normalizedEmail).Scan(
		&credential.PlayerID,
		&credential.Email,
		&credential.NormalizedEmail,
		&credential.PasswordHash,
		&lastLoginAt,
	)
	if err == sql.ErrNoRows {
		return auth.EmailCredential{}, false, nil
	}
	if err != nil {
		return auth.EmailCredential{}, false, err
	}
	if lastLoginAt.Valid {
		credential.LastLoginAt = lastLoginAt.Time
	}

	return credential, true, nil
}

func (store *AccountStore) RecordEmailLogin(ctx context.Context, normalizedEmail string, loginAt time.Time) error {
	normalizedEmail = auth.NormalizeEmail(normalizedEmail)
	tx, err := store.db.BeginTx(ctx, nil)
	if err != nil {
		return err
	}
	defer tx.Rollback()

	if _, err := tx.ExecContext(ctx, `
		UPDATE account.player_email_credentials
		SET last_login_at = $2,
			updated_at = now()
		WHERE normalized_email = $1
	`, normalizedEmail, loginAt); err != nil {
		return err
	}

	if _, err := tx.ExecContext(ctx, `
		UPDATE account.player_auth_identities
		SET last_login_at = $2,
			updated_at = now()
		WHERE provider = $3
			AND provider_subject = $1
	`, normalizedEmail, loginAt, auth.ProviderEmail); err != nil {
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

func isUniqueViolation(err error) bool {
	var pgErr *pgconn.PgError
	return errors.As(err, &pgErr) && pgErr.Code == "23505"
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

func (store *AccountStore) RevokeSession(ctx context.Context, sessionID string, revokedAt time.Time) error {
	_, err := store.db.ExecContext(ctx, `
		UPDATE account.player_sessions
		SET revoked_at = $2
		WHERE id = $1
			AND revoked_at IS NULL
	`, sessionID, revokedAt)
	return err
}
