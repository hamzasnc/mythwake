# Mythwake Backend

Small Go API skeleton for the future server-authoritative Mythwake backend.

Current scope:
- Standard-library HTTP server
- Environment-based config
- Health endpoint
- Server clock endpoint with UTC daily and weekly reset boundaries
- Read-only definitions endpoint
- Dev player state endpoint
- Guest auth with random session tokens
- Logout endpoint that revokes the active session token
- Short read-through session cache for PostgreSQL-backed auth validation
- Request ID middleware for client/server log correlation
- JSON panic recovery for unexpected HTTP handler failures
- Configurable in-memory rate limiting for auth and gameplay mutation requests
- PostgreSQL account identities and hashed session token persistence for guest, email, Google, and Apple login providers
- Bearer session validation for player state, flush, and gameplay mutation endpoints
- Per-player service contexts resolved from the authenticated session token
- In-memory action endpoints for campaign, dungeons, heroes, equipment, accessories, summons, missions, and mission track
- Optional PostgreSQL connection via `MYTHWAKE_DATABASE_URL`
- Embedded SQL migrations
- PostgreSQL player progress tables for currencies, campaign, dungeons, heroes, ascensions, and hero shards
- JSON player state snapshot mirror for fallback/debugging
- Seeded definition tables for currencies, dungeons, accessory slots, accessory rarities, accessories, heroes, campaign stages, rewards, progression costs, summons, missions, and Mission Track rewards
- PostgreSQL schemas:
  - `account`
  - `common`
  - `player`
  - `logs`
  - `debug`
- Navicat-friendly debug views:
  - `debug.v_player_overview`
  - `debug.v_player_hero_overview`
  - `debug.v_player_economy_overview`
- Economy transaction logging for currency deltas
- Accessory inventory, accessory levels, and equipped accessories persisted in PostgreSQL
- Navicat-friendly accessory view:
  - `debug.v_player_accessory_overview`
- Starter Weapon and Armor training persisted in PostgreSQL
- Navicat-friendly equipment view:
  - `debug.v_player_equipment_overview`
- Summon count, UTC daily mission progress/claims, and Battle Pass claims persisted in PostgreSQL
- Navicat-friendly meta views:
  - `debug.v_player_claim_overview`
  - `debug.v_player_daily_progress_overview`
  - `debug.v_player_summon_overview`
- Navicat-friendly account and persistence views:
  - `debug.v_account_player_overview`
  - `debug.v_account_identity_overview`
  - `debug.v_account_session_overview`
  - `debug.v_player_persistence_overview`
- Durable state cache wrapper in front of PostgreSQL
- Ledger write-behind saves by default: successful economy actions durably write the action/result first, then batch materialized player state
- Write-through mode is still available for debugging full state persistence on every action
- Optional write-behind mode for local/dev-only batching experiments
- `GET /player/state` returns a full client-ready snapshot.
- `POST /player/state/flush` forces the current hot player state through the persistence/cache flush path.
- `POST /player/offline/claim` claims server-authoritative AFK Gold and Myth Essence using server time.
- Daily mission claims require server-tracked progress for the active UTC daily reset window.
- Campaign and dungeon actions return server-authored combat results with rounds, HP, damage, and win/loss state.
- `POST /auth/logout` revokes the active session token.
- Logout flushes the loaded player state before returning when that player is hot in memory.
- All responses include `X-Request-ID`; clients may send their own valid `X-Request-ID`.
- Error responses use `{ "errorCode", "message", "requestId" }`.
- Rate-limited requests return HTTP 429, `errorCode=rate_limited`, and `Retry-After`.
- `GET /time` returns the authoritative server time, Unix milliseconds, and upcoming UTC daily/weekly reset times.
- `GET /player/core-state` returns the compact numeric state only.
- Player state, flush, and gameplay mutation routes reject missing or invalid sessions with `401`.
- Guest auth and action responses include `playerSnapshot` for direct client refresh.
- Guest auth returns the raw session token once; PostgreSQL stores only `token_hash`.
- The Unity prototype can ping, guest-login, store the session token, sync this snapshot, cache `/definitions` with ETag revalidation, and route manual gameplay buttons from the Shop tab Backend panel's Server Mode.
- Unity automatically sends `Authorization: Bearer <sessionToken>` and retries protected calls once after a `401` by refreshing guest auth.
- Server gameplay POSTs require valid `Idempotency-Key` headers by default.
- Gameplay action IDs are centralized in `internal/gameplay` so routing, persistence, ledgers, and tests share the same names.
- Currency IDs, spends, grants, display names, and deltas are centralized in `internal/economy`.
- Early balance definitions for campaign, dungeons, costs, summons, and simple rewards are centralized in `internal/balance`.
- Player gameplay services read balance through an injectable catalog boundary, so a database-backed gameplay catalog can replace the static catalog without changing route handlers.
- When PostgreSQL is enabled, `/definitions` is loaded from `common.*` definition tables; no-database mode keeps using the static Go catalog.
- Player service gameplay actions route through explicit domain action services while keeping the existing API surface stable.
- Daily Mission, Mission Track, and Summon actions validate against server-owned definitions instead of arbitrary client IDs.
- Campaign, dungeon, and summon actions advance server-owned daily mission counters for the active UTC day.
- `GET /definitions` exposes the current server-owned balance/action catalog for client and admin tooling, including auth providers, currencies, heroes, rewards, campaign stages, dungeons, accessories, costs, summons, missions, and action metadata, with content hashes and ETag revalidation.
- Campaign and dungeon definitions include combat curves/stats so client previews can match server combat.
- Navicat-friendly common definition views:
  - `debug.v_common_reward_overview`
  - `debug.v_common_progression_cost_overview`
  - `debug.v_common_meta_definition_overview`
  - `debug.v_common_combat_definition_overview`
- Successful idempotent action results are stored in PostgreSQL before the materialized player-state flush.
- Retrying the same action with the same key returns the stored result with `replay: true` instead of applying rewards/spends again.
- Graceful shutdown flushes loaded player contexts before closing the state cache.

Not included yet:
- Redis connection
- Email, Google, and Apple token verification endpoints
- Production-ready balance tooling/admin flow

Run local API without PostgreSQL:

```powershell
cd backend
go run ./cmd/api
```

Run local API with PostgreSQL:

```powershell
cd "C:\Users\Hamza\Desktop\Idle Game\Mythwake\backend"
$env:MYTHWAKE_DATABASE_URL='postgres://mythwake:mythwake@localhost:5432/mythwake?sslmode=disable'
go run ./cmd/api
```

Shortcut scripts from the repo root:

```powershell
.\scripts\start-backend.cmd
.\scripts\check-backend.cmd
.\scripts\check-postgres-e2e.cmd
```

`start-backend.cmd` sets the local environment variables, checks PostgreSQL, tries to start the local `postgresql*` Windows service if needed, then runs the API.

Unity client notes:
- Editor/Desktop default backend URL: `http://localhost:8080`
- Android emulator default backend URL: `http://10.0.2.2:8080`
- In the prototype, open `Shop` and use `Ping`, `Login`, `Sync`, or the `Local`/`Server` mode button in the Backend panel.
- Server Mode sends manual gameplay button actions to this API and applies the returned `playerSnapshot`.

Optional script modes:

```powershell
.\scripts\start-backend.cmd -NoDatabase
.\scripts\start-backend.cmd -AllowMissingIdempotency
.\scripts\start-backend.cmd -DisableRateLimit
.\scripts\start-backend.cmd -RateLimitAuth 60 -RateLimitGameplay 600
.\scripts\start-backend.cmd -SessionCacheTTL "30s" -SessionTouchWindow "30s"
.\scripts\start-backend.cmd -DatabaseUrl "postgres://mythwake:mythwake@localhost:5432/mythwake?sslmode=disable"
.\scripts\check-backend.cmd -BaseUrl "http://localhost:8080"
.\scripts\check-backend.cmd -FlushState
.\scripts\check-backend.cmd -CheckIdempotency
.\scripts\check-backend.cmd -CheckUnauthorized
.\scripts\check-backend.cmd -CheckLogout
.\scripts\check-postgres-e2e.cmd
```

Default address:
- `:8080`

Optional environment variables:
- `MYTHWAKE_API_ADDR`
- `MYTHWAKE_ENV`
- `MYTHWAKE_API_VERSION`
- `MYTHWAKE_DATABASE_URL`
- `MYTHWAKE_STATE_WRITE_MODE`, default `ledger_write_behind`, optional `write_through` or `write_behind`
- `MYTHWAKE_STATE_FLUSH_INTERVAL` such as `30s`, `2m`, or `5m`
- `MYTHWAKE_STATE_FLUSH_TIMEOUT` such as `5s`
- `MYTHWAKE_SESSION_CACHE_TTL`, default `30s`; set to `0s` to force DB lookup on every validation
- `MYTHWAKE_SESSION_TOUCH_WINDOW`, default `30s`; controls how often cached active sessions update `last_seen_at`
- `MYTHWAKE_RATE_LIMIT_ENABLED`, default `true`
- `MYTHWAKE_RATE_LIMIT_WINDOW`, default `1m`
- `MYTHWAKE_RATE_LIMIT_AUTH`, default `30` requests per window for auth endpoints
- `MYTHWAKE_RATE_LIMIT_GAMEPLAY`, default `240` requests per window for gameplay mutation endpoints
- `MYTHWAKE_REQUIRE_IDEMPOTENCY`, default `true`; set to `false` only for local debugging

Database behavior:
- If `MYTHWAKE_DATABASE_URL` is empty, the API uses the current in-memory dev state.
- If `MYTHWAKE_DATABASE_URL` is set, startup connects to PostgreSQL, runs embedded migrations, and stores the dev player state in normalized progression tables.
- PostgreSQL-backed sessions are cached briefly in-process to avoid a database round trip on every protected request.
- The touch window updates `account.player_sessions.last_seen_at` at a controlled rate instead of on every request.
- By default, idempotent gameplay actions update hot server state, synchronously write a durable action ledger/result to PostgreSQL, then queue the materialized player state for flush.
- This default protects critical economy state from hard process crashes after a successful API response without forcing every materialized table to update immediately.
- If the API restarts before a materialized flush, startup restores from the latest durable action result snapshot before falling back to normalized tables.
- `MYTHWAKE_STATE_WRITE_MODE=write_through` forces the full normalized player state to be saved on every successful action.
- `MYTHWAKE_STATE_WRITE_MODE=write_behind` is for local/dev-only experiments.
- Queued materialized state flushes every `MYTHWAKE_STATE_FLUSH_INTERVAL` and once more during graceful shutdown.
- Loaded in-memory player contexts are flushed during API shutdown before the cache is closed.
- `POST /player/state/flush` is the app-pause/disconnect hook used by the Unity prototype.
- AFK reward claims are capped at 6 hours, require at least 60 seconds, and persist `last_claimed_at` in PostgreSQL plus the action-result snapshot for crash-safe replay.
- Daily progress is keyed by UTC date in `player.player_daily_progress`; a new server day clears daily mission counters and daily claims before the next snapshot/action.
- New player seed state still writes immediately so first login/startup is durable.
- The JSON player state snapshot is still written as a fallback/debug mirror.
- Currency changes are written to `logs.economy_transactions` during DB save.
- Successful action responses with an `Idempotency-Key` are written to `player.player_action_results`.
- Per-action currency deltas are written to `logs.player_action_ledger`.
- Reusing a key for a different endpoint/body returns an `idempotency_conflict` action result.
- Missing or malformed keys on gameplay mutations return HTTP 400 before the action is applied.
- `GET /health` reports `database`, `state_cache`, `state_write_mode`, `state_flush_interval`, session cache settings, and `require_idempotency`.
- `GET /time` is the source of truth for offline reward windows, daily missions, weekly systems, and client clock drift checks.
- Request logs include request id, method, path, status, bytes, and duration.
- Rate limiting is currently process-local for development and single-node testing; Redis should replace the counter storage before multi-instance production.
- `scripts/check-backend.cmd` performs guest login, sends Bearer auth for protected endpoints, and can verify missing-session `401`s.
- `scripts/check-postgres-e2e.cmd` starts the API twice against PostgreSQL and verifies login, protected state, campaign persistence, manual flush, restart reload, idempotency replay, and logout revocation.

Endpoints:
- `POST /auth/guest`
- `POST /auth/logout`
- `GET /health`
- `GET /time`
- `GET /definitions`
- `GET /player/state`
- `POST /player/state/flush`
- `GET /player/core-state`
- `POST /player/offline/claim`
- `POST /campaign/fight`
- `POST /dungeons/{dungeon_id}/run`
- `POST /heroes/{hero_id}/level-up`
- `POST /heroes/{hero_id}/ascend`
- `POST /equipment/{equipment_id}/level-up`
- `POST /gear/accessories/equip`
- `POST /gear/accessories/level-up`
- `POST /gear/accessories/fuse`
- `POST /summons/{banner_id}/pull`
- `POST /missions/{mission_id}/claim`
- `POST /battle-pass/{reward_id}/claim`

Idempotent action retry:

```powershell
$key = [guid]::NewGuid().ToString("N")
$headers = @{ "Idempotency-Key" = $key }
Invoke-RestMethod -Method Post -Headers $headers "http://localhost:8080/heroes/hero_astra/level-up"
Invoke-RestMethod -Method Post -Headers $headers "http://localhost:8080/heroes/hero_astra/level-up"
```
