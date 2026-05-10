package redis

import (
	"context"
	"crypto/rand"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"strings"
	"time"

	goredis "github.com/redis/go-redis/v9"

	"github.com/hamzasnc/mythwake/backend/internal/auth"
	"github.com/hamzasnc/mythwake/backend/internal/cache/actionlock"
	"github.com/hamzasnc/mythwake/backend/internal/cache/ratelimit"
)

const defaultKeyPrefix = "mythwake"

type Config struct {
	Addr      string
	Password  string
	DB        int
	KeyPrefix string
}

func NewClient(config Config) *goredis.Client {
	return goredis.NewClient(&goredis.Options{
		Addr:     strings.TrimSpace(config.Addr),
		Password: config.Password,
		DB:       config.DB,
	})
}

func Ping(ctx context.Context, client *goredis.Client) error {
	if client == nil {
		return fmt.Errorf("redis client is nil")
	}

	return client.Ping(ctx).Err()
}

type SessionCache struct {
	client *goredis.Client
	prefix string
	now    func() time.Time
}

func NewSessionCache(client *goredis.Client, keyPrefix string) *SessionCache {
	return &SessionCache{
		client: client,
		prefix: normalizePrefix(keyPrefix),
		now:    time.Now,
	}
}

func (cache *SessionCache) Load(ctx context.Context, tokenHash string) (auth.SessionCacheEntry, bool, error) {
	raw, err := cache.client.Get(ctx, cache.key("session", tokenHash)).Bytes()
	if err == goredis.Nil {
		return auth.SessionCacheEntry{}, false, nil
	}
	if err != nil {
		return auth.SessionCacheEntry{}, false, err
	}

	var entry auth.SessionCacheEntry
	if err := json.Unmarshal(raw, &entry); err != nil {
		return auth.SessionCacheEntry{}, false, err
	}

	return entry, true, nil
}

func (cache *SessionCache) Store(ctx context.Context, tokenHash string, entry auth.SessionCacheEntry) error {
	ttl := time.Until(entry.Session.ExpiresAt)
	if cache.now != nil {
		ttl = entry.Session.ExpiresAt.Sub(cache.now().UTC())
	}
	if ttl <= 0 {
		return cache.Delete(ctx, tokenHash)
	}

	raw, err := json.Marshal(entry)
	if err != nil {
		return err
	}

	return cache.client.Set(ctx, cache.key("session", tokenHash), raw, ttl).Err()
}

func (cache *SessionCache) Delete(ctx context.Context, tokenHash string) error {
	return cache.client.Del(ctx, cache.key("session", tokenHash)).Err()
}

func (cache *SessionCache) key(parts ...string) string {
	return cache.prefix + ":" + strings.Join(parts, ":")
}

type RateLimiter struct {
	client *goredis.Client
	prefix string
	script *goredis.Script
}

func NewRateLimiter(client *goredis.Client, keyPrefix string) *RateLimiter {
	return &RateLimiter{
		client: client,
		prefix: normalizePrefix(keyPrefix),
		script: goredis.NewScript(`
local current = redis.call("INCR", KEYS[1])
if current == 1 then
	redis.call("PEXPIRE", KEYS[1], ARGV[1])
end
local ttl = redis.call("PTTL", KEYS[1])
return { current, ttl }
`),
	}
}

func (limiter *RateLimiter) Allow(ctx context.Context, key string, limit int, window time.Duration) (ratelimit.Decision, error) {
	if limit <= 0 {
		return ratelimit.Decision{Allowed: true}, nil
	}
	if window <= 0 {
		window = time.Minute
	}

	windowMs := window.Milliseconds()
	if windowMs < 1 {
		windowMs = 1
	}

	result, err := limiter.script.Run(ctx, limiter.client, []string{limiter.key(key)}, windowMs).Result()
	if err != nil {
		return ratelimit.Decision{}, err
	}

	values, ok := result.([]any)
	if !ok || len(values) != 2 {
		return ratelimit.Decision{}, fmt.Errorf("unexpected redis rate limit result: %#v", result)
	}

	current, ok := values[0].(int64)
	if !ok {
		return ratelimit.Decision{}, fmt.Errorf("unexpected redis rate limit count: %#v", values[0])
	}
	ttlMs, ok := values[1].(int64)
	if !ok {
		return ratelimit.Decision{}, fmt.Errorf("unexpected redis rate limit ttl: %#v", values[1])
	}

	if current > int64(limit) {
		retryAfter := time.Duration(ttlMs) * time.Millisecond
		if retryAfter <= 0 {
			retryAfter = window
		}

		return ratelimit.Decision{Allowed: false, RetryAfter: retryAfter}, nil
	}

	return ratelimit.Decision{Allowed: true}, nil
}

func (limiter *RateLimiter) key(key string) string {
	return limiter.prefix + ":ratelimit:" + key
}

type Locker struct {
	client        *goredis.Client
	prefix        string
	releaseScript *goredis.Script
}

func NewLocker(client *goredis.Client, keyPrefix string) *Locker {
	return &Locker{
		client: client,
		prefix: normalizePrefix(keyPrefix),
		releaseScript: goredis.NewScript(`
if redis.call("GET", KEYS[1]) == ARGV[1] then
	return redis.call("DEL", KEYS[1])
end
return 0
`),
	}
}

func (locker *Locker) Acquire(ctx context.Context, key string, ttl time.Duration) (actionlock.Lock, bool, error) {
	if ttl <= 0 {
		ttl = 5 * time.Second
	}

	token, err := lockToken()
	if err != nil {
		return nil, false, err
	}

	redisKey := locker.key(key)
	ok, err := locker.client.SetNX(ctx, redisKey, token, ttl).Result()
	if err != nil {
		return nil, false, err
	}
	if !ok {
		return nil, false, nil
	}

	return actionlock.ReleaseFunc(func(ctx context.Context) error {
		return locker.releaseScript.Run(ctx, locker.client, []string{redisKey}, token).Err()
	}), true, nil
}

func (locker *Locker) key(key string) string {
	return locker.prefix + ":lock:" + key
}

func lockToken() (string, error) {
	bytes := make([]byte, 16)
	if _, err := rand.Read(bytes); err != nil {
		return "", fmt.Errorf("generate redis lock token: %w", err)
	}

	return base64.RawURLEncoding.EncodeToString(bytes), nil
}

func normalizePrefix(prefix string) string {
	prefix = strings.Trim(strings.TrimSpace(prefix), ":")
	if prefix == "" {
		return defaultKeyPrefix
	}

	return prefix
}
