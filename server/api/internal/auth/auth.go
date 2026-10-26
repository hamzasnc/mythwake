// Package auth issues and checks session tokens and hashes device secrets.
package auth

import (
	"errors"
	"time"

	"github.com/golang-jwt/jwt/v5"
	"golang.org/x/crypto/bcrypt"
)

const issuer = "mythwake"

type Tokens struct {
	secret []byte
	ttl    time.Duration
	now    func() time.Time
}

func NewTokens(secret []byte, ttl time.Duration) *Tokens {
	return &Tokens{secret: secret, ttl: ttl, now: time.Now}
}

// Issue returns a signed session token for an account.
func (t *Tokens) Issue(accountID string) (string, error) {
	now := t.now()
	claims := jwt.RegisteredClaims{
		Issuer:    issuer,
		Subject:   accountID,
		IssuedAt:  jwt.NewNumericDate(now),
		ExpiresAt: jwt.NewNumericDate(now.Add(t.ttl)),
	}
	return jwt.NewWithClaims(jwt.SigningMethodHS256, claims).SignedString(t.secret)
}

// Verify checks signature, issuer and expiry and returns the account id.
func (t *Tokens) Verify(token string) (string, error) {
	claims := &jwt.RegisteredClaims{}
	parsed, err := jwt.ParseWithClaims(token, claims, func(*jwt.Token) (any, error) { return t.secret, nil },
		jwt.WithValidMethods([]string{jwt.SigningMethodHS256.Alg()}),
		jwt.WithIssuer(issuer),
		jwt.WithTimeFunc(t.now))
	if err != nil || !parsed.Valid {
		return "", errors.New("invalid token")
	}
	if claims.Subject == "" {
		return "", errors.New("token without subject")
	}
	return claims.Subject, nil
}

func HashSecret(secret string) (string, error) {
	b, err := bcrypt.GenerateFromPassword([]byte(secret), bcrypt.DefaultCost)
	return string(b), err
}

func CheckSecret(hash, secret string) bool {
	return bcrypt.CompareHashAndPassword([]byte(hash), []byte(secret)) == nil
}
