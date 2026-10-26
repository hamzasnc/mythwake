// Package api is the HTTP interface of the Mythwake server (docs/SERVER.md).
package api

import (
	"context"
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"errors"
	"log/slog"
	"net/http"
	"strings"
	"sync"
	"time"

	"mythwake/api/internal/auth"
	"mythwake/api/internal/rules"
	"mythwake/api/internal/store"
)

// Rules is what the API needs from the rules service (an interface so handlers can be tested without it).
type Rules interface {
	Health(ctx context.Context) (string, error)
	Create(ctx context.Context, seed uint64, nowMs int64) (rules.Result, error)
	Apply(ctx context.Context, state string, nowMs int64, commands []rules.Command, trusted bool) (rules.Result, error)
	ArenaTeam(ctx context.Context, state string) (rules.ArenaTeam, error)
	ArenaBattle(ctx context.Context, attacker, defender string, seed uint64, attackerRating, defenderRating int) (rules.ArenaBattle, error)
	ArenaConfig(ctx context.Context) (rules.ArenaConfig, error)
	BotTeam(ctx context.Context, stage int, seed uint64) (rules.ArenaTeam, error)
	GuildConfig(ctx context.Context) (rules.GuildConfig, error)
	RaidBattle(ctx context.Context, team string, raidLevel int, hpLeft int64, seed uint64) (rules.RaidBattle, error)
}

type Server struct {
	store  *store.Store
	rules  Rules
	tokens *auth.Tokens
	log    *slog.Logger
	now    func() time.Time
	limits *rateLimiter

	arenaMu     sync.Mutex
	arenaLoaded bool
	arena       rules.ArenaConfig

	guildMu     sync.Mutex
	guildLoaded bool
	guild       rules.GuildConfig
}

func New(st *store.Store, r Rules, tokens *auth.Tokens, log *slog.Logger) *Server {
	return &Server{store: st, rules: r, tokens: tokens, log: log, now: time.Now, limits: newRateLimiter(20, 40)}
}

func (s *Server) nowMs() int64 { return s.now().UnixMilli() }

func (s *Server) Handler() http.Handler {
	mux := http.NewServeMux()
	mux.HandleFunc("GET /healthz", func(w http.ResponseWriter, r *http.Request) { writeJSON(w, http.StatusOK, map[string]bool{"ok": true}) })
	mux.HandleFunc("GET /readyz", s.ready)
	mux.HandleFunc("GET /v1/time", func(w http.ResponseWriter, r *http.Request) {
		writeJSON(w, http.StatusOK, map[string]int64{"nowMs": s.nowMs()})
	})
	mux.HandleFunc("POST /v1/auth/device", s.deviceLogin)
	mux.Handle("GET /v1/player", s.authed(s.getPlayer))
	mux.Handle("POST /v1/player/name", s.authed(s.setName))
	mux.Handle("POST /v1/commands", s.authed(s.postCommands))
	mux.Handle("GET /v1/arena", s.authed(s.getArena))
	mux.Handle("POST /v1/arena/defense", s.authed(s.setDefense))
	mux.Handle("POST /v1/arena/fight", s.authed(s.fight))
	mux.Handle("GET /v1/arena/ranking", s.authed(s.ranking))
	mux.Handle("GET /v1/guilds", s.authed(s.listGuilds))
	mux.Handle("POST /v1/guilds", s.authed(s.createGuild))
	mux.Handle("POST /v1/guilds/{id}/join", s.authed(s.joinGuild))
	mux.Handle("GET /v1/guild", s.authed(s.getGuild))
	mux.Handle("POST /v1/guild/leave", s.authed(s.leaveGuild))
	mux.Handle("GET /v1/guild/chat", s.authed(s.guildChat))
	mux.Handle("POST /v1/guild/chat", s.authed(s.postGuildChat))
	mux.Handle("POST /v1/guild/donate", s.authed(s.donate))
	mux.Handle("POST /v1/guild/raid", s.authed(s.raid))
	return s.middleware(mux)
}

func (s *Server) ready(w http.ResponseWriter, r *http.Request) {
	if err := s.store.Ping(r.Context()); err != nil {
		writeError(w, http.StatusServiceUnavailable, "database", err.Error())
		return
	}
	version, err := s.rules.Health(r.Context())
	if err != nil {
		writeError(w, http.StatusServiceUnavailable, "rules", err.Error())
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"ok": true, "contentVersion": version})
}

// ------------------------------------------------------------------ middleware

type ctxKey int

const accountKey ctxKey = 1

func accountID(r *http.Request) string { return r.Context().Value(accountKey).(string) }

func (s *Server) middleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		id := r.Header.Get("X-Request-Id")
		if id == "" {
			id = randomHex(8)
		}
		w.Header().Set("X-Request-Id", id)
		rec := &statusRecorder{ResponseWriter: w, status: http.StatusOK}
		defer func() {
			if p := recover(); p != nil {
				s.log.Error("panic", "request", id, "panic", p)
				writeError(rec, http.StatusInternalServerError, "internal", "internal error")
			}
			s.log.Info("http", "request", id, "method", r.Method, "path", r.URL.Path, "status", rec.status,
				"ms", time.Since(start).Milliseconds())
		}()
		r.Body = http.MaxBytesReader(w, r.Body, 1<<20)
		next.ServeHTTP(rec, r)
	})
}

// authed checks the bearer token and the per-account rate limit.
func (s *Server) authed(h http.HandlerFunc) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		token, ok := strings.CutPrefix(r.Header.Get("Authorization"), "Bearer ")
		if !ok {
			writeError(w, http.StatusUnauthorized, "unauthorized", "missing bearer token")
			return
		}
		account, err := s.tokens.Verify(token)
		if err != nil {
			writeError(w, http.StatusUnauthorized, "unauthorized", "invalid token")
			return
		}
		if !s.limits.allow(account) {
			writeError(w, http.StatusTooManyRequests, "rate_limited", "too many requests")
			return
		}
		h(w, r.WithContext(context.WithValue(r.Context(), accountKey, account)))
	})
}

type statusRecorder struct {
	http.ResponseWriter
	status int
}

func (r *statusRecorder) WriteHeader(code int) {
	r.status = code
	r.ResponseWriter.WriteHeader(code)
}

// ------------------------------------------------------------------ rate limit

// rateLimiter is a token bucket per account (in memory; replace with Redis when running several API instances).
type rateLimiter struct {
	mu      sync.Mutex
	rate    float64
	burst   float64
	buckets map[string]*bucket
}

type bucket struct {
	tokens float64
	last   time.Time
}

func newRateLimiter(perSecond, burst float64) *rateLimiter {
	return &rateLimiter{rate: perSecond, burst: burst, buckets: map[string]*bucket{}}
}

func (l *rateLimiter) allow(key string) bool {
	l.mu.Lock()
	defer l.mu.Unlock()
	now := time.Now()
	b, ok := l.buckets[key]
	if !ok {
		b = &bucket{tokens: l.burst, last: now}
		l.buckets[key] = b
	}
	b.tokens = min(l.burst, b.tokens+now.Sub(b.last).Seconds()*l.rate)
	b.last = now
	if b.tokens < 1 {
		return false
	}
	b.tokens--
	return true
}

// ------------------------------------------------------------------ helpers

type apiError struct {
	Error   string `json:"error"`
	Message string `json:"message,omitempty"`
}

func writeJSON(w http.ResponseWriter, status int, body any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(body)
}

func writeError(w http.ResponseWriter, status int, code, message string) {
	writeJSON(w, status, apiError{Error: code, Message: message})
}

func readJSON(r *http.Request, v any) error {
	dec := json.NewDecoder(r.Body)
	dec.DisallowUnknownFields()
	if err := dec.Decode(v); err != nil {
		return errors.New("invalid json: " + err.Error())
	}
	return nil
}

func randomHex(n int) string {
	b := make([]byte, n)
	_, _ = rand.Read(b)
	return hex.EncodeToString(b)
}

func randomUint64() uint64 {
	b := make([]byte, 8)
	_, _ = rand.Read(b)
	var v uint64
	for _, x := range b {
		v = v<<8 | uint64(x)
	}
	return v
}
