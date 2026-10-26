package auth

import (
	"strings"
	"testing"
	"time"
)

func TestTokenRoundTrip(t *testing.T) {
	tokens := NewTokens([]byte(strings.Repeat("k", 32)), time.Hour)
	tok, err := tokens.Issue("acc-1")
	if err != nil {
		t.Fatal(err)
	}
	got, err := tokens.Verify(tok)
	if err != nil || got != "acc-1" {
		t.Fatalf("verify: %q %v", got, err)
	}
}

func TestTokenRejectsTamperingOtherKeysAndExpiry(t *testing.T) {
	tokens := NewTokens([]byte(strings.Repeat("k", 32)), time.Hour)
	tok, _ := tokens.Issue("acc-1")
	if _, err := tokens.Verify(tok[:len(tok)-2] + "xx"); err == nil {
		t.Fatal("tampered token accepted")
	}
	other := NewTokens([]byte(strings.Repeat("z", 32)), time.Hour)
	if _, err := other.Verify(tok); err == nil {
		t.Fatal("token from another key accepted")
	}
	tokens.now = func() time.Time { return time.Now().Add(2 * time.Hour) }
	if _, err := tokens.Verify(tok); err == nil {
		t.Fatal("expired token accepted")
	}
}

func TestSecretHash(t *testing.T) {
	h, err := HashSecret("correct horse battery staple")
	if err != nil {
		t.Fatal(err)
	}
	if !CheckSecret(h, "correct horse battery staple") || CheckSecret(h, "wrong") {
		t.Fatal("bcrypt check")
	}
}
