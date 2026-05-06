# Mythwake Backend

Small Go API skeleton for the future server-authoritative Mythwake backend.

Current scope:
- Standard-library HTTP server
- Environment-based config
- Health endpoint
- Dev player state endpoint
- Graceful shutdown

Not included yet:
- PostgreSQL connection
- Redis connection
- Auth
- Player state persistence
- Economy endpoints

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
- `GET /health`
- `GET /player/state`
