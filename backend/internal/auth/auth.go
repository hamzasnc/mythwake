package auth

import (
	"context"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
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

const defaultSessionTTL = 30 * 24 * time.Hour

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
	SessionID string
	PlayerID  string
	Provider  string
	Token     string
	TokenHash string
	IssuedAt  time.Time
	ExpiresAt time.Time
	UserAgent string
}

type AccountStore interface {
	EnsureIdentity(ctx context.Context, identity Identity) error
	SaveSession(ctx context.Context, session Session) error
}

type Service struct {
	store AccountStore
	now   func() time.Time
	ttl   time.Duration
}

func NewService(store AccountStore) *Service {
	return &Service{
		store: store,
		now:   time.Now,
		ttl:   defaultSessionTTL,
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

func (service *Service) IssueGuestSession(ctx context.Context, playerID string, userAgent string) (Session, error) {
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
