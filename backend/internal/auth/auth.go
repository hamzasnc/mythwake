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
	"time"
)

const (
	ProviderGuest  = "guest"
	ProviderEmail  = "email"
	ProviderGoogle = "google"
	ProviderApple  = "apple"
)

const (
	defaultSessionTTL           = 30 * 24 * time.Hour
	defaultSessionCacheTTL      = 30 * time.Second
	defaultSessionTouchInterval = 30 * time.Second
)

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

type ServiceOption func(*Service)

type Service struct {
	store         AccountStore
	now           func() time.Time
	ttl           time.Duration
	cacheTTL      time.Duration
	touchInterval time.Duration
	sessionCache  SessionCache
}

func NewService(store AccountStore, options ...ServiceOption) *Service {
	service := &Service{
		store:         store,
		now:           time.Now,
		ttl:           defaultSessionTTL,
		cacheTTL:      defaultSessionCacheTTL,
		touchInterval: defaultSessionTouchInterval,
		sessionCache:  NewMemorySessionCache(),
	}
	for _, option := range options {
		option(service)
	}

	return service
}

func WithSessionCache(cache SessionCache) ServiceOption {
	return func(service *Service) {
		if cache != nil {
			service.sessionCache = cache
		}
	}
}

func WithSessionCacheTTL(ttl time.Duration) ServiceOption {
	return func(service *Service) {
		if ttl < 0 {
			ttl = 0
		}
		service.cacheTTL = ttl
	}
}

func WithSessionTouchInterval(interval time.Duration) ServiceOption {
	return func(service *Service) {
		if interval < 0 {
			interval = 0
		}
		service.touchInterval = interval
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
		if err := service.rememberSession(ctx, session, now, now); err != nil {
			return Session{}, err
		}
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
	if err := service.rememberSession(ctx, session, now, now); err != nil {
		return Session{}, err
	}

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
		if session, found, err := service.cachedSession(ctx, tokenHash, token, now); err != nil {
			return Session{}, err
		} else if found {
			return session, nil
		}

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
		if err := service.rememberSession(ctx, session, now, now); err != nil {
			return Session{}, err
		}
		return session, nil
	}

	entry, ok, err := service.sessionCache.Load(ctx, tokenHash)
	if err != nil {
		return Session{}, err
	}
	if !ok {
		return Session{}, ErrInvalidSession
	}
	session := entry.Session
	if !session.ExpiresAt.After(now) {
		_ = service.sessionCache.Delete(ctx, tokenHash)
		return Session{}, ErrExpiredSession
	}

	session.Token = token
	session.LastSeenAt = now
	entry.Session = session
	if err := service.sessionCache.Store(ctx, tokenHash, entry); err != nil {
		return Session{}, err
	}
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
		session, found, err := service.cachedSession(ctx, tokenHash, token, now)
		if err != nil {
			return Session{}, err
		}
		if !found {
			session, found, err = service.store.FindSessionByTokenHash(ctx, tokenHash, now)
			if err != nil {
				return Session{}, err
			}
			if !found {
				return Session{}, ErrInvalidSession
			}
		}
		if err := service.store.RevokeSession(ctx, session.SessionID, now); err != nil {
			return Session{}, err
		}

		_ = service.forgetSession(ctx, tokenHash)
		session.Token = token
		return session, nil
	}

	entry, ok, err := service.sessionCache.Load(ctx, tokenHash)
	if err != nil {
		return Session{}, err
	}
	if !ok {
		return Session{}, ErrInvalidSession
	}

	if err := service.sessionCache.Delete(ctx, tokenHash); err != nil {
		return Session{}, err
	}
	session := entry.Session
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

func (service *Service) cachedSession(ctx context.Context, tokenHash string, token string, now time.Time) (Session, bool, error) {
	entry, ok, err := service.sessionCache.Load(ctx, tokenHash)
	if err != nil {
		if service.store != nil {
			return Session{}, false, nil
		}

		return Session{}, false, err
	}
	if !ok {
		return Session{}, false, nil
	}
	if !entry.Session.ExpiresAt.After(now) {
		_ = service.sessionCache.Delete(ctx, tokenHash)
		return Session{}, false, nil
	}
	if service.cacheTTL == 0 || now.Sub(entry.CachedAt) >= service.cacheTTL {
		_ = service.sessionCache.Delete(ctx, tokenHash)
		return Session{}, false, nil
	}

	shouldTouch := service.touchInterval == 0 || now.Sub(entry.LastStoreTouchAt) >= service.touchInterval
	session := entry.Session

	if shouldTouch {
		if err := service.store.TouchSession(ctx, session.SessionID, now); err != nil {
			return Session{}, false, err
		}

		session.LastSeenAt = now
		current, ok, err := service.sessionCache.Load(ctx, tokenHash)
		if err != nil {
			if service.store != nil {
				return Session{}, false, nil
			}

			return Session{}, false, err
		}
		if ok && current.Session.SessionID == session.SessionID {
			current.Session = session
			current.LastStoreTouchAt = now
			if err := service.sessionCache.Store(ctx, tokenHash, current); err != nil && service.store == nil {
				return Session{}, false, err
			}
		}
	}

	session.Token = token
	return session, true, nil
}

func (service *Service) rememberSession(ctx context.Context, session Session, cachedAt time.Time, lastStoreTouchAt time.Time) error {
	err := service.sessionCache.Store(ctx, session.TokenHash, SessionCacheEntry{
		Session:          session,
		CachedAt:         cachedAt,
		LastStoreTouchAt: lastStoreTouchAt,
	})
	if err != nil && service.store == nil {
		return err
	}

	return nil
}

func (service *Service) forgetSession(ctx context.Context, tokenHash string) error {
	return service.sessionCache.Delete(ctx, tokenHash)
}
