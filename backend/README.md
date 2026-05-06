# Mythwake Backend

Small Go API skeleton for the future server-authoritative Mythwake backend.

Current scope:
- Standard-library HTTP server
- Environment-based config
- Health endpoint
- Dev player state endpoint
- In-memory guest auth
- In-memory action endpoints for campaign, dungeons, heroes, equipment, accessories, summons, missions, and mission track
- Graceful shutdown

Not included yet:
- PostgreSQL connection
- Redis connection
- Real auth/session persistence
- Player state persistence
- Production balance definitions

Run later with Go installed:

```powershell
cd backend
go run ./cmd/api
```

Default address:
- `:8080`

Optional environment variables:
- `MYTHWAKE_API_ADDR`
- `MYTHWAKE_ENV`
- `MYTHWAKE_API_VERSION`

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
