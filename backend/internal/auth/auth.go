package auth

import (
	"context"
	"crypto/pbkdf2"
	"crypto/rand"
	"crypto/sha256"
	"crypto/subtle"
	"encoding/base64"
	"encoding/hex"
	"errors"
	"fmt"
	"strconv"
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

const (
	defaultSessionTTL           = 30 * 24 * time.Hour
	defaultSessionCacheTTL      = 30 * time.Second
	defaultSessionTouchInterval = 30 * time.Second
)

var (
	ErrMissingSession         = errors.New("session token is required")
	ErrInvalidSession         = errors.New("session token is invalid")
	ErrExpiredSession         = errors.New("session token has expired")
	ErrMissingEmail           = errors.New("email address is required")
	ErrInvalidEmail           = errors.New("email address is invalid")
	ErrMissingPassword        = errors.New("password is required")
	ErrInvalidPassword        = errors.New("password does not meet minimum requirements")
	ErrEmailAlreadyRegistered = errors.New("email address is already registered")
	ErrInvalidCredentials     = errors.New("email or password is invalid")
)

const (
	minPasswordLength      = 8
	maxPasswordLength      = 256
	passwordHashAlgorithm  = "pbkdf2_sha256"
	passwordHashVersion    = "v=1"
	passwordHashIterations = 210000
	passwordHashSaltBytes  = 16
	passwordHashKeyBytes   = 32
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

type EmailCredential struct {
	PlayerID        string
	Email           string
	NormalizedEmail string
	PasswordHash    string
	LastLoginAt     time.Time
}

type AccountStore interface {
	EnsureIdentity(ctx context.Context, identity Identity) error
	CreateEmailCredential(ctx context.Context, identity Identity, passwordHash string) error
	FindEmailCredential(ctx context.Context, normalizedEmail string) (EmailCredential, bool, error)
	RecordEmailLogin(ctx context.Context, normalizedEmail string, loginAt time.Time) error
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
	emailMu       sync.Mutex
	emailAccounts map[string]EmailCredential
}

func NewService(store AccountStore, options ...ServiceOption) *Service {
	service := &Service{
		store:         store,
		now:           time.Now,
		ttl:           defaultSessionTTL,
		cacheTTL:      defaultSessionCacheTTL,
		touchInterval: defaultSessionTouchInterval,
		sessionCache:  NewMemorySessionCache(),
		emailAccounts: map[string]EmailCredential{},
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
	return service.saveSession(ctx, session, now)
}

func (service *Service) RegisterEmail(ctx context.Context, email string, password string, userAgent string) (Session, error) {
	normalizedEmail, err := normalizeEmailForAuth(email)
	if err != nil {
		return Session{}, err
	}
	passwordHash, err := HashPassword(password)
	if err != nil {
		return Session{}, err
	}

	playerID, err := randomToken("player_", 16)
	if err != nil {
		return Session{}, err
	}

	now := service.now().UTC()
	identity := Identity{
		PlayerID:        playerID,
		Provider:        ProviderEmail,
		ProviderSubject: normalizedEmail,
		Email:           normalizedEmail,
		EmailVerified:   false,
		LastLoginAt:     now,
	}

	if service.store != nil {
		if err := service.store.CreateEmailCredential(ctx, identity, passwordHash); err != nil {
			return Session{}, err
		}
	} else {
		service.emailMu.Lock()
		if _, exists := service.emailAccounts[normalizedEmail]; exists {
			service.emailMu.Unlock()
			return Session{}, ErrEmailAlreadyRegistered
		}
		service.emailAccounts[normalizedEmail] = EmailCredential{
			PlayerID:        playerID,
			Email:           normalizedEmail,
			NormalizedEmail: normalizedEmail,
			PasswordHash:    passwordHash,
			LastLoginAt:     now,
		}
		service.emailMu.Unlock()
	}

	session, err := service.newSession(playerID, ProviderEmail, userAgent, now)
	if err != nil {
		return Session{}, err
	}
	return service.saveSession(ctx, session, now)
}

func (service *Service) LoginEmail(ctx context.Context, email string, password string, userAgent string) (Session, error) {
	normalizedEmail, err := normalizeEmailForAuth(email)
	if err != nil {
		return Session{}, err
	}
	if err := validatePassword(password); err != nil {
		return Session{}, err
	}

	var credential EmailCredential
	var found bool
	if service.store != nil {
		credential, found, err = service.store.FindEmailCredential(ctx, normalizedEmail)
		if err != nil {
			return Session{}, err
		}
	} else {
		service.emailMu.Lock()
		credential, found = service.emailAccounts[normalizedEmail]
		service.emailMu.Unlock()
	}
	if !found || !VerifyPassword(password, credential.PasswordHash) {
		return Session{}, ErrInvalidCredentials
	}

	now := service.now().UTC()
	if service.store != nil {
		if err := service.store.RecordEmailLogin(ctx, normalizedEmail, now); err != nil {
			return Session{}, err
		}
	} else {
		service.emailMu.Lock()
		credential.LastLoginAt = now
		service.emailAccounts[normalizedEmail] = credential
		service.emailMu.Unlock()
	}

	session, err := service.newSession(credential.PlayerID, ProviderEmail, userAgent, now)
	if err != nil {
		return Session{}, err
	}
	return service.saveSession(ctx, session, now)
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

func NormalizeEmail(email string) string {
	return strings.ToLower(strings.TrimSpace(email))
}

func HashPassword(password string) (string, error) {
	if err := validatePassword(password); err != nil {
		return "", err
	}

	salt, err := randomBytes(passwordHashSaltBytes)
	if err != nil {
		return "", err
	}
	key, err := pbkdf2.Key(sha256.New, password, salt, passwordHashIterations, passwordHashKeyBytes)
	if err != nil {
		return "", err
	}

	return fmt.Sprintf(
		"%s$%s$i=%d$s=%s$h=%s",
		passwordHashAlgorithm,
		passwordHashVersion,
		passwordHashIterations,
		base64.RawStdEncoding.EncodeToString(salt),
		base64.RawStdEncoding.EncodeToString(key),
	), nil
}

func VerifyPassword(password string, encodedHash string) bool {
	parts := strings.Split(encodedHash, "$")
	if len(parts) != 5 || parts[0] != passwordHashAlgorithm || parts[1] != passwordHashVersion {
		return false
	}

	iterationsRaw := strings.TrimPrefix(parts[2], "i=")
	if iterationsRaw == parts[2] {
		return false
	}
	iterations, err := strconv.Atoi(iterationsRaw)
	if err != nil || iterations <= 0 {
		return false
	}

	saltRaw := strings.TrimPrefix(parts[3], "s=")
	hashRaw := strings.TrimPrefix(parts[4], "h=")
	if saltRaw == parts[3] || hashRaw == parts[4] {
		return false
	}
	salt, err := base64.RawStdEncoding.DecodeString(saltRaw)
	if err != nil || len(salt) == 0 {
		return false
	}
	expected, err := base64.RawStdEncoding.DecodeString(hashRaw)
	if err != nil || len(expected) == 0 {
		return false
	}

	actual, err := pbkdf2.Key(sha256.New, password, salt, iterations, len(expected))
	if err != nil {
		return false
	}

	return subtle.ConstantTimeCompare(actual, expected) == 1
}

func validatePassword(password string) error {
	if strings.TrimSpace(password) == "" {
		return ErrMissingPassword
	}
	if len(password) < minPasswordLength || len(password) > maxPasswordLength {
		return ErrInvalidPassword
	}

	return nil
}

func normalizeEmailForAuth(email string) (string, error) {
	normalized := NormalizeEmail(email)
	if normalized == "" {
		return "", ErrMissingEmail
	}
	if strings.ContainsAny(normalized, " \t\r\n") {
		return "", ErrInvalidEmail
	}

	at := strings.Index(normalized, "@")
	if at <= 0 || at != strings.LastIndex(normalized, "@") || at == len(normalized)-1 {
		return "", ErrInvalidEmail
	}
	domain := normalized[at+1:]
	if !strings.Contains(domain, ".") {
		return "", ErrInvalidEmail
	}

	return normalized, nil
}

func randomToken(prefix string, byteCount int) (string, error) {
	bytes, err := randomBytes(byteCount)
	if err != nil {
		return "", fmt.Errorf("generate token: %w", err)
	}

	return prefix + base64.RawURLEncoding.EncodeToString(bytes), nil
}

func randomBytes(byteCount int) ([]byte, error) {
	bytes := make([]byte, byteCount)
	if _, err := rand.Read(bytes); err != nil {
		return nil, err
	}

	return bytes, nil
}

func (service *Service) newSession(playerID string, provider string, userAgent string, now time.Time) (Session, error) {
	token, err := randomToken("mw_sess_", 32)
	if err != nil {
		return Session{}, err
	}
	sessionID, err := randomToken("sess_", 16)
	if err != nil {
		return Session{}, err
	}

	return Session{
		SessionID: sessionID,
		PlayerID:  playerID,
		Provider:  provider,
		Token:     token,
		TokenHash: TokenHash(token),
		IssuedAt:  now,
		ExpiresAt: now.Add(service.ttl),
		UserAgent: strings.TrimSpace(userAgent),
	}, nil
}

func (service *Service) saveSession(ctx context.Context, session Session, now time.Time) (Session, error) {
	if service.store != nil {
		if err := service.store.SaveSession(ctx, session); err != nil {
			return Session{}, err
		}
	}
	if err := service.rememberSession(ctx, session, now, now); err != nil {
		return Session{}, err
	}

	return session, nil
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
	session.Token = ""
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
