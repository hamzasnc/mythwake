package auth

import (
	"context"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
	"errors"
	"fmt"
	"strings"
	"sync"
	"time"
)

const (
	ProviderGuest  = "guest"
	ProviderEmail  = "email"
	ProviderGoogle = "google"
	ProviderApple  = "apple"
)

const defaultSessionTTL = 30 * 24 * time.Hour

var (
	ErrMissingSession = errors.New("session token is required")
	ErrInvalidSession = errors.New("session token is invalid")
	ErrExpiredSession = errors.New("session token has expired")
)

type ProviderDefinition struct {
	ID                string
	DisplayName       string
	ExternalProvider  bool
	SupportsLinking   bool
	SupportsMobileSSO bool
}

type Identity struct {
	PlayerID        string
	Provider        string
	ProviderSubject string
	Email           string
	EmailVerified   bool
	LastLoginAt     time.Time
}

type Session struct {
	SessionID  string
	PlayerID   string
	Provider   string
	Token      string
	TokenHash  string
	IssuedAt   time.Time
	ExpiresAt  time.Time
	LastSeenAt time.Time
	UserAgent  string
}

type AccountStore interface {
	EnsureIdentity(ctx context.Context, identity Identity) error
	SaveSession(ctx context.Context, session Session) error
	FindSessionByTokenHash(ctx context.Context, tokenHash string, now time.Time) (Session, bool, error)
	TouchSession(ctx context.Context, sessionID string, seenAt time.Time) error
	RevokeSession(ctx context.Context, sessionID string, revokedAt time.Time) error
}

type Service struct {
	store          AccountStore
	now            func() time.Time
	ttl            time.Duration
	mu             sync.Mutex
	sessionsByHash map[string]Session
}

func NewService(store AccountStore) *Service {
	return &Service{
		store:          store,
		now:            time.Now,
		ttl:            defaultSessionTTL,
		sessionsByHash: map[string]Session{},
	}
}

func ProviderDefinitions() []ProviderDefinition {
	return []ProviderDefinition{
		{ID: ProviderGuest, DisplayName: "Guest", SupportsLinking: true},
		{ID: ProviderEmail, DisplayName: "Email", SupportsLinking: true},
		{ID: ProviderGoogle, DisplayName: "Google", ExternalProvider: true, SupportsLinking: true, SupportsMobileSSO: true},
		{ID: ProviderApple, DisplayName: "Apple", ExternalProvider: true, SupportsLinking: true, SupportsMobileSSO: true},
	}
}

func (service *Service) IssueGuestSession(ctx context.Context, userAgent string) (Session, error) {
	playerID, err := randomToken("player_", 16)
	if err != nil {
		return Session{}, err
	}

	return service.IssueGuestSessionForPlayer(ctx, playerID, userAgent)
}

func (service *Service) IssueGuestSessionForPlayer(ctx context.Context, playerID string, userAgent string) (Session, error) {
	playerID = strings.TrimSpace(playerID)
	if playerID == "" {
		return Session{}, fmt.Errorf("player id is required")
	}

	now := service.now().UTC()
	token, err := randomToken("mw_sess_", 32)
	if err != nil {
		return Session{}, err
	}
	sessionID, err := randomToken("sess_", 16)
	if err != nil {
		return Session{}, err
	}

	session := Session{
		SessionID: sessionID,
		PlayerID:  playerID,
		Provider:  ProviderGuest,
		Token:     token,
		TokenHash: TokenHash(token),
		IssuedAt:  now,
		ExpiresAt: now.Add(service.ttl),
		UserAgent: strings.TrimSpace(userAgent),
	}

	if service.store == nil {
		service.rememberSession(session)
		return session, nil
	}

	identity := Identity{
		PlayerID:        playerID,
		Provider:        ProviderGuest,
		ProviderSubject: playerID,
		LastLoginAt:     now,
	}
	if err := service.store.EnsureIdentity(ctx, identity); err != nil {
		return Session{}, err
	}
	if err := service.store.SaveSession(ctx, session); err != nil {
		return Session{}, err
	}
	service.rememberSession(session)

	return session, nil
}

func (service *Service) ValidateSession(ctx context.Context, token string) (Session, error) {
	token = strings.TrimSpace(token)
	if token == "" {
		return Session{}, ErrMissingSession
	}

	now := service.now().UTC()
	tokenHash := TokenHash(token)

	if service.store != nil {
		session, found, err := service.store.FindSessionByTokenHash(ctx, tokenHash, now)
		if err != nil {
			return Session{}, err
		}
		if !found {
			return Session{}, ErrInvalidSession
		}
		if err := service.store.TouchSession(ctx, session.SessionID, now); err != nil {
			return Session{}, err
		}

		session.Token = token
		session.LastSeenAt = now
		service.rememberSession(session)
		return session, nil
	}

	service.mu.Lock()
	defer service.mu.Unlock()

	session, ok := service.sessionsByHash[tokenHash]
	if !ok {
		return Session{}, ErrInvalidSession
	}
	if !session.ExpiresAt.After(now) {
		delete(service.sessionsByHash, tokenHash)
		return Session{}, ErrExpiredSession
	}

	session.Token = token
	session.LastSeenAt = now
	service.sessionsByHash[tokenHash] = session
	return session, nil
}

func (service *Service) RevokeSession(ctx context.Context, token string) (Session, error) {
	token = strings.TrimSpace(token)
	if token == "" {
		return Session{}, ErrMissingSession
	}

	now := service.now().UTC()
	tokenHash := TokenHash(token)

	if service.store != nil {
		session, found, err := service.store.FindSessionByTokenHash(ctx, tokenHash, now)
		if err != nil {
			return Session{}, err
		}
		if !found {
			return Session{}, ErrInvalidSession
		}
		if err := service.store.RevokeSession(ctx, session.SessionID, now); err != nil {
			return Session{}, err
		}

		service.forgetSession(tokenHash)
		session.Token = token
		return session, nil
	}

	service.mu.Lock()
	defer service.mu.Unlock()

	session, ok := service.sessionsByHash[tokenHash]
	if !ok {
		return Session{}, ErrInvalidSession
	}

	delete(service.sessionsByHash, tokenHash)
	session.Token = token
	return session, nil
}

func TokenHash(token string) string {
	sum := sha256.Sum256([]byte(token))
	return hex.EncodeToString(sum[:])
}

func randomToken(prefix string, byteCount int) (string, error) {
	bytes := make([]byte, byteCount)
	if _, err := rand.Read(bytes); err != nil {
		return "", fmt.Errorf("generate token: %w", err)
	}

	return prefix + base64.RawURLEncoding.EncodeToString(bytes), nil
}

func (service *Service) rememberSession(session Session) {
	service.mu.Lock()
	defer service.mu.Unlock()

	service.sessionsByHash[session.TokenHash] = session
}

func (service *Service) forgetSession(tokenHash string) {
	service.mu.Lock()
	defer service.mu.Unlock()

	delete(service.sessionsByHash, tokenHash)
}
