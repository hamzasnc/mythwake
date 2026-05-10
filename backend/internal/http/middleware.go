package apihttp

import (
	"context"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
	"log"
	"net"
	"net/http"
	"runtime/debug"
	"strconv"
	"strings"
	"sync"
	"time"

	"github.com/hamzasnc/mythwake/backend/internal/config"
)

type requestIDContextKey struct{}

const (
	rateLimitCategoryAuth     = "auth"
	rateLimitCategoryGameplay = "gameplay"
)

type statusRecorder struct {
	http.ResponseWriter
	statusCode int
	bytes      int
}

func (recorder *statusRecorder) WriteHeader(statusCode int) {
	if recorder.statusCode != 0 {
		return
	}

	recorder.statusCode = statusCode
	recorder.ResponseWriter.WriteHeader(statusCode)
}

func (recorder *statusRecorder) Write(bytes []byte) (int, error) {
	if recorder.statusCode == 0 {
		recorder.statusCode = http.StatusOK
	}

	written, err := recorder.ResponseWriter.Write(bytes)
	recorder.bytes += written
	return written, err
}

func (recorder *statusRecorder) StatusCode() int {
	if recorder.statusCode == 0 {
		return http.StatusOK
	}

	return recorder.statusCode
}

func withRequestID(next http.Handler) http.Handler {
	return http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		requestID := strings.TrimSpace(request.Header.Get("X-Request-ID"))
		if !validRequestID(requestID) {
			requestID = newRequestID()
		}

		response.Header().Set("X-Request-ID", requestID)
		ctx := context.WithValue(request.Context(), requestIDContextKey{}, requestID)
		next.ServeHTTP(response, request.WithContext(ctx))
	})
}

func recoverPanic(logger *log.Logger, next http.Handler) http.Handler {
	return http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		defer func() {
			if recovered := recover(); recovered != nil {
				requestID := requestIDFromContext(request.Context())
				logger.Printf("panic request_id=%s method=%s path=%s error=%v stack=%s", requestID, request.Method, request.URL.Path, recovered, string(debug.Stack()))
				writeError(response, request, http.StatusInternalServerError, "internal_error", "Internal server error.")
			}
		}()

		next.ServeHTTP(response, request)
	})
}

func logRequests(logger *log.Logger, next http.Handler) http.Handler {
	return http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		startedAt := time.Now()
		recorder := &statusRecorder{ResponseWriter: response}
		next.ServeHTTP(recorder, request)
		logger.Printf(
			"request_id=%s method=%s path=%s status=%d bytes=%d duration=%s",
			requestIDFromContext(request.Context()),
			request.Method,
			request.URL.Path,
			recorder.StatusCode(),
			recorder.bytes,
			time.Since(startedAt),
		)
	})
}

type rateLimiter struct {
	mu      sync.Mutex
	now     func() time.Time
	window  time.Duration
	entries map[string]rateLimitEntry
}

type rateLimitEntry struct {
	count   int
	resetAt time.Time
}

func withRateLimit(cfg config.Config, next http.Handler) http.Handler {
	limiter := newRateLimiter(cfg.RateLimitWindow)
	return http.HandlerFunc(func(response http.ResponseWriter, request *http.Request) {
		category, limit := rateLimitCategory(cfg, request)
		if !cfg.RateLimitEnabled || category == "" || limit <= 0 {
			next.ServeHTTP(response, request)
			return
		}

		key := rateLimitKey(category, request)
		allowed, retryAfter := limiter.Allow(key, limit)
		if !allowed {
			response.Header().Set("Retry-After", strconv.Itoa(int(retryAfter.Seconds()+0.999)))
			writeError(response, request, http.StatusTooManyRequests, "rate_limited", "Too many requests. Try again shortly.")
			return
		}

		next.ServeHTTP(response, request)
	})
}

func newRateLimiter(window time.Duration) *rateLimiter {
	if window <= 0 {
		window = time.Minute
	}

	return &rateLimiter{
		now:     time.Now,
		window:  window,
		entries: map[string]rateLimitEntry{},
	}
}

func (limiter *rateLimiter) Allow(key string, limit int) (bool, time.Duration) {
	if limit <= 0 {
		return true, 0
	}

	now := limiter.now()
	limiter.mu.Lock()
	defer limiter.mu.Unlock()

	entry := limiter.entries[key]
	if entry.resetAt.IsZero() || !entry.resetAt.After(now) {
		entry = rateLimitEntry{resetAt: now.Add(limiter.window)}
	}

	if entry.count >= limit {
		return false, entry.resetAt.Sub(now)
	}

	entry.count++
	limiter.entries[key] = entry
	return true, 0
}

func rateLimitCategory(cfg config.Config, request *http.Request) (string, int) {
	if request.Method != http.MethodPost {
		return "", 0
	}

	switch request.URL.Path {
	case "/auth/guest", "/auth/logout":
		return rateLimitCategoryAuth, cfg.RateLimitAuth
	default:
		return rateLimitCategoryGameplay, cfg.RateLimitGameplay
	}
}

func rateLimitKey(category string, request *http.Request) string {
	token := sessionTokenFromRequest(request)
	if token != "" {
		hash := sha256.Sum256([]byte(token))
		return category + ":session:" + hex.EncodeToString(hash[:8])
	}

	host, _, err := net.SplitHostPort(request.RemoteAddr)
	if err != nil || host == "" {
		host = request.RemoteAddr
	}
	if host == "" {
		host = "unknown"
	}

	return category + ":ip:" + host
}

func requestIDFromContext(ctx context.Context) string {
	requestID, ok := ctx.Value(requestIDContextKey{}).(string)
	if !ok || requestID == "" {
		return "unknown"
	}

	return requestID
}

func validRequestID(requestID string) bool {
	if len(requestID) < 8 || len(requestID) > 128 {
		return false
	}

	for _, char := range requestID {
		if char >= 'a' && char <= 'z' {
			continue
		}
		if char >= 'A' && char <= 'Z' {
			continue
		}
		if char >= '0' && char <= '9' {
			continue
		}
		if char == '.' || char == '_' || char == ':' || char == '-' {
			continue
		}
		return false
	}

	return true
}

func newRequestID() string {
	bytes := make([]byte, 12)
	if _, err := rand.Read(bytes); err != nil {
		return "req_fallback"
	}

	return "req_" + base64.RawURLEncoding.EncodeToString(bytes)
}
