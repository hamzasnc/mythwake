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
- Graceful shutdown

Not included yet:
- Redis connection
- Real auth/session persistence
- Full normalized player state persistence
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

Optional script modes:

```powershell
.\scripts\start-backend.cmd -NoDatabase
.\scripts\start-backend.cmd -DatabaseUrl "postgres://mythwake:mythwake@localhost:5432/mythwake?sslmode=disable"
.\scripts\check-backend.cmd -BaseUrl "http://localhost:8080"
```

Default address:
- `:8080`

Optional environment variables:
- `MYTHWAKE_API_ADDR`
- `MYTHWAKE_ENV`
- `MYTHWAKE_API_VERSION`
- `MYTHWAKE_DATABASE_URL`

Database behavior:
- If `MYTHWAKE_DATABASE_URL` is empty, the API uses the current in-memory dev state.
- If `MYTHWAKE_DATABASE_URL` is set, startup connects to PostgreSQL, runs embedded migrations, and stores the dev player state in normalized progression tables.
- The JSON player state snapshot is still written as a fallback/debug mirror.
- Currency changes are written to `logs.economy_transactions`.
- `GET /health` reports `database: disabled` or `database: connected`.

Endpoints:
- `POST /auth/guest`
- `GET /health`
- `GET /player/state`
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
