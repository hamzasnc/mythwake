package auth

import (
	"context"
	"strings"
	"testing"
)

type fakeAccountStore struct {
	identity Identity
	session  Session
}

func (store *fakeAccountStore) EnsureIdentity(_ context.Context, identity Identity) error {
	store.identity = identity
	return nil
}

func (store *fakeAccountStore) SaveSession(_ context.Context, session Session) error {
	store.session = session
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

	session, err := service.IssueGuestSession(context.Background(), "player-1", "test-agent")
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

	if _, err := service.IssueGuestSession(context.Background(), " ", ""); err == nil {
		t.Fatal("expected empty player id to fail")
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
