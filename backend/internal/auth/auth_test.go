package auth

import (
	"context"
	"strings"
	"testing"
	"time"
)

type fakeAccountStore struct {
	identity Identity
	session  Session
	findHits int
	touches  int
}

func (store *fakeAccountStore) EnsureIdentity(_ context.Context, identity Identity) error {
	store.identity = identity
	return nil
}

func (store *fakeAccountStore) SaveSession(_ context.Context, session Session) error {
	store.session = session
	return nil
}

func (store *fakeAccountStore) FindSessionByTokenHash(_ context.Context, tokenHash string, _ time.Time) (Session, bool, error) {
	store.findHits++
	if store.session.TokenHash != tokenHash {
		return Session{}, false, nil
	}

	return store.session, true, nil
}

func (store *fakeAccountStore) TouchSession(_ context.Context, sessionID string, seenAt time.Time) error {
	store.touches++
	if store.session.SessionID == sessionID {
		store.session.LastSeenAt = seenAt
	}
	return nil
}

func (store *fakeAccountStore) RevokeSession(_ context.Context, sessionID string, revokedAt time.Time) error {
	if store.session.SessionID == sessionID {
		store.session.ExpiresAt = revokedAt
		store.session.TokenHash = ""
	}
	return nil
}

func TestProviderDefinitionsIncludePlannedLoginVariants(t *testing.T) {
	definitions := ProviderDefinitions()

	for _, providerID := range []string{ProviderGuest, ProviderEmail, ProviderGoogle, ProviderApple} {
		if !hasProvider(definitions, providerID) {
			t.Fatalf("expected provider %s in %#v", providerID, definitions)
		}
	}
}

func TestIssueGuestSessionReturnsTokenAndPersistsHash(t *testing.T) {
	store := &fakeAccountStore{}
	service := NewService(store)

	session, err := service.IssueGuestSessionForPlayer(context.Background(), "player-1", "test-agent")
	if err != nil {
		t.Fatalf("issue guest session: %v", err)
	}

	if session.PlayerID != "player-1" || session.Provider != ProviderGuest {
		t.Fatalf("unexpected session: %#v", session)
	}
	if !strings.HasPrefix(session.Token, "mw_sess_") {
		t.Fatalf("expected session token prefix, got %s", session.Token)
	}
	if session.TokenHash == "" || session.TokenHash == session.Token {
		t.Fatalf("expected stored token hash, got %#v", session)
	}
	if store.identity.ProviderSubject != "player-1" || store.identity.Provider != ProviderGuest {
		t.Fatalf("expected guest identity to be persisted, got %#v", store.identity)
	}
	if store.session.TokenHash != session.TokenHash || store.session.Token != session.Token {
		t.Fatalf("expected session to be persisted, got %#v", store.session)
	}
}

func TestIssueGuestSessionRejectsEmptyPlayerID(t *testing.T) {
	service := NewService(nil)

	if _, err := service.IssueGuestSessionForPlayer(context.Background(), " ", ""); err == nil {
		t.Fatal("expected empty player id to fail")
	}
}

func TestIssueGuestSessionGeneratesPlayerID(t *testing.T) {
	service := NewService(nil)

	session, err := service.IssueGuestSession(context.Background(), "")
	if err != nil {
		t.Fatalf("issue guest session: %v", err)
	}

	if !strings.HasPrefix(session.PlayerID, "player_") {
		t.Fatalf("expected generated player id, got %#v", session)
	}
}

func TestValidateSessionUsesStoredTokenHash(t *testing.T) {
	store := &fakeAccountStore{}
	service := NewService(store)

	issued, err := service.IssueGuestSessionForPlayer(context.Background(), "player-1", "")
	if err != nil {
		t.Fatalf("issue guest session: %v", err)
	}

	validated, err := service.ValidateSession(context.Background(), issued.Token)
	if err != nil {
		t.Fatalf("validate session: %v", err)
	}

	if validated.PlayerID != issued.PlayerID || validated.TokenHash != issued.TokenHash {
		t.Fatalf("expected validated stored session, got %#v", validated)
	}
}

func TestValidateSessionCachesStoredSession(t *testing.T) {
	store := &fakeAccountStore{}
	issuer := NewService(store)

	issued, err := issuer.IssueGuestSessionForPlayer(context.Background(), "player-1", "")
	if err != nil {
		t.Fatalf("issue guest session: %v", err)
	}

	service := NewService(store, WithSessionCacheTTL(time.Minute), WithSessionTouchInterval(time.Minute))
	if _, err := service.ValidateSession(context.Background(), issued.Token); err != nil {
		t.Fatalf("first validate session: %v", err)
	}
	if _, err := service.ValidateSession(context.Background(), issued.Token); err != nil {
		t.Fatalf("second validate session: %v", err)
	}

	if store.findHits != 1 {
		t.Fatalf("expected one stored session lookup, got %d", store.findHits)
	}
	if store.touches != 1 {
		t.Fatalf("expected one stored session touch, got %d", store.touches)
	}
}

func TestValidateSessionRefreshesExpiredCache(t *testing.T) {
	store := &fakeAccountStore{}
	now := time.Date(2026, 5, 10, 12, 0, 0, 0, time.UTC)
	issuer := NewService(store)
	issuer.now = func() time.Time { return now }

	issued, err := issuer.IssueGuestSessionForPlayer(context.Background(), "player-1", "")
	if err != nil {
		t.Fatalf("issue guest session: %v", err)
	}

	service := NewService(store, WithSessionCacheTTL(time.Second), WithSessionTouchInterval(time.Minute))
	service.now = func() time.Time { return now }
	if _, err := service.ValidateSession(context.Background(), issued.Token); err != nil {
		t.Fatalf("first validate session: %v", err)
	}

	now = now.Add(2 * time.Second)
	if _, err := service.ValidateSession(context.Background(), issued.Token); err != nil {
		t.Fatalf("second validate session: %v", err)
	}

	if store.findHits != 2 {
		t.Fatalf("expected cache expiry to force second store lookup, got %d", store.findHits)
	}
}

func TestValidateSessionTouchesStoreAfterWindow(t *testing.T) {
	store := &fakeAccountStore{}
	now := time.Date(2026, 5, 10, 12, 0, 0, 0, time.UTC)
	issuer := NewService(store)
	issuer.now = func() time.Time { return now }

	issued, err := issuer.IssueGuestSessionForPlayer(context.Background(), "player-1", "")
	if err != nil {
		t.Fatalf("issue guest session: %v", err)
	}

	service := NewService(store, WithSessionCacheTTL(time.Minute), WithSessionTouchInterval(time.Second))
	service.now = func() time.Time { return now }
	if _, err := service.ValidateSession(context.Background(), issued.Token); err != nil {
		t.Fatalf("first validate session: %v", err)
	}

	now = now.Add(2 * time.Second)
	if _, err := service.ValidateSession(context.Background(), issued.Token); err != nil {
		t.Fatalf("second validate session: %v", err)
	}

	if store.findHits != 1 {
		t.Fatalf("expected cached session lookup, got %d store lookups", store.findHits)
	}
	if store.touches != 2 {
		t.Fatalf("expected touch after window, got %d touches", store.touches)
	}
}

func TestValidateSessionRejectsUnknownToken(t *testing.T) {
	service := NewService(nil)

	if _, err := service.ValidateSession(context.Background(), "mw_sess_unknown"); err == nil {
		t.Fatal("expected unknown token to fail")
	}
}

func TestRevokeSessionInvalidatesInMemorySession(t *testing.T) {
	service := NewService(nil)

	issued, err := service.IssueGuestSessionForPlayer(context.Background(), "player-1", "")
	if err != nil {
		t.Fatalf("issue guest session: %v", err)
	}

	if _, err := service.RevokeSession(context.Background(), issued.Token); err != nil {
		t.Fatalf("revoke session: %v", err)
	}

	if _, err := service.ValidateSession(context.Background(), issued.Token); err == nil {
		t.Fatal("expected revoked session to be invalid")
	}
}

func TestTokenHashIsStableAndNotRawToken(t *testing.T) {
	first := TokenHash("token")
	second := TokenHash("token")

	if first == "" || first != second || first == "token" {
		t.Fatalf("unexpected token hash values: %q and %q", first, second)
	}
}

func hasProvider(definitions []ProviderDefinition, providerID string) bool {
	for _, definition := range definitions {
		if definition.ID == providerID {
			return true
		}
	}
	return false
}
