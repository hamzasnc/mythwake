# Mythwake Backend

Small Go API skeleton for the future server-authoritative Mythwake backend.

Current scope:
- Standard-library HTTP server
- Environment-based config
- Health endpoint
- Dev player state endpoint
- In-memory guest auth
- In-memory action endpoints for campaign, dungeons, heroes, equipment, accessories, summons, missions, and mission track
- Optional PostgreSQL connection via `MYTHWAKE_DATABASE_URL`
- Embedded SQL migrations
- PostgreSQL player progress tables for currencies, campaign, dungeons, heroes, ascensions, and hero shards
- JSON player state snapshot mirror for fallback/debugging
- Seeded definition tables for currencies, dungeons, accessory slots, accessory rarities, and accessories
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
- Summon count, daily mission claims, and Battle Pass claims persisted in PostgreSQL
- Navicat-friendly meta views:
  - `debug.v_player_claim_overview`
  - `debug.v_player_summon_overview`
- Durable state cache wrapper in front of PostgreSQL
- Ledger write-behind saves by default: successful economy actions durably write the action/result first, then batch materialized player state
- Write-through mode is still available for debugging full state persistence on every action
- Optional write-behind mode for local/dev-only batching experiments
- `GET /player/state` returns a full client-ready snapshot.
- `POST /player/state/flush` forces the current hot player state through the persistence/cache flush path.
- `GET /player/core-state` returns the compact numeric state only.
- Guest auth and action responses include `playerSnapshot` for direct client refresh.
- The Unity prototype can ping, guest-login, sync this snapshot, and route manual gameplay buttons from the Shop tab Backend panel's Server Mode.
- Server gameplay POSTs require valid `Idempotency-Key` headers by default.
- Gameplay action IDs are centralized in `internal/gameplay` so routing, persistence, ledgers, and tests share the same names.
- Currency IDs, spends, grants, display names, and deltas are centralized in `internal/economy`.
- Successful idempotent action results are stored in PostgreSQL before the materialized player-state flush.
- Retrying the same action with the same key returns the stored result with `replay: true` instead of applying rewards/spends again.
- Graceful shutdown

Not included yet:
- Redis connection
- Real auth/session persistence
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
.\scripts\start-backend.cmd -DatabaseUrl "postgres://mythwake:mythwake@localhost:5432/mythwake?sslmode=disable"
.\scripts\check-backend.cmd -BaseUrl "http://localhost:8080"
.\scripts\check-backend.cmd -FlushState
.\scripts\check-backend.cmd -CheckIdempotency
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
- `MYTHWAKE_REQUIRE_IDEMPOTENCY`, default `true`; set to `false` only for local debugging

Database behavior:
- If `MYTHWAKE_DATABASE_URL` is empty, the API uses the current in-memory dev state.
- If `MYTHWAKE_DATABASE_URL` is set, startup connects to PostgreSQL, runs embedded migrations, and stores the dev player state in normalized progression tables.
- By default, idempotent gameplay actions update hot server state, synchronously write a durable action ledger/result to PostgreSQL, then queue the materialized player state for flush.
- This default protects critical economy state from hard process crashes after a successful API response without forcing every materialized table to update immediately.
- If the API restarts before a materialized flush, startup restores from the latest durable action result snapshot before falling back to normalized tables.
- `MYTHWAKE_STATE_WRITE_MODE=write_through` forces the full normalized player state to be saved on every successful action.
- `MYTHWAKE_STATE_WRITE_MODE=write_behind` is for local/dev-only experiments.
- Queued materialized state flushes every `MYTHWAKE_STATE_FLUSH_INTERVAL` and once more during graceful shutdown.
- `POST /player/state/flush` exists as the future app-pause/disconnect hook.
- New player seed state still writes immediately so first login/startup is durable.
- The JSON player state snapshot is still written as a fallback/debug mirror.
- Currency changes are written to `logs.economy_transactions` during DB save.
- Successful action responses with an `Idempotency-Key` are written to `player.player_action_results`.
- Per-action currency deltas are written to `logs.player_action_ledger`.
- Reusing a key for a different endpoint/body returns an `idempotency_conflict` action result.
- Missing or malformed keys on gameplay mutations return HTTP 400 before the action is applied.
- `GET /health` reports `database`, `state_cache`, `state_write_mode`, `state_flush_interval`, and `require_idempotency`.

Endpoints:
- `POST /auth/guest`
- `GET /health`
- `GET /player/state`
- `POST /player/state/flush`
- `GET /player/core-state`
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
