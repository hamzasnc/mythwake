# Mythwake v2 – Server

## Decision

**Go API (platform) + stateless .NET rules service (the same `Mythwake.Core` the client runs) + PostgreSQL.**

| Option | Pro | Contra |
|---|---|---|
| A. Everything in Go, rules ported | one runtime, Go strengths | every rule exists twice (C# client, Go server); the deterministic battle simulator must match bit for bit; every new system costs double and drifts |
| B. Everything in .NET | rules shared directly | the team's backend plan is Go; .NET is weaker for the platform work ahead (realtime chat, guild fan-out, ops tooling) |
| **C. Go platform + .NET rules sidecar** | Go owns what Go is good at (HTTP, auth, Postgres, concurrency, social/PvP features); rules exist once and run identically on client and server | two processes; one extra local hop (~1 ms) |
| D. Go + rules compiled to WASM / NativeAOT library | single binary | .NET WASI is experimental, NativeAOT + cgo needs native toolchains on every dev machine and CI |

C keeps the core promise of the architecture: **the server re-runs the exact client rules**, so battles, idle rewards and
summons are validated without a second implementation. The rules service is stateless (state in, state out) and scales
horizontally; everything stateful lives in Go + Postgres.

## Components

```
Unity client ──HTTPS/JSON──► Go API (server/api)  ──SQL──► PostgreSQL
   │  predicts locally            │ auth, sessions, versioning, idempotency,
   │  (same rules)                │ server clock, rate limits, PvP, guilds
   │                              └──HTTP/JSON──► rules service (server/rules, .NET)
   │                                                runs Mythwake.Core on (state, commands, now)
   └─ reconciles with the server's state hash
```

### Go API (`server/api`)

- `net/http` (Go 1.22 routing), `pgx/v5`, `golang-jwt/jwt/v5`, `x/crypto/bcrypt`, `log/slog`.
- **Auth:** device login. The client creates a random device id + secret once; the server stores a bcrypt hash and
  issues a JWT (HS256, 7 days). Accounts can get more devices / platform logins later.
- **Player state:** one row per account, `state jsonb` + `version`. Commands run in a transaction with
  `SELECT … FOR UPDATE`; the client sends the version it predicted from (`baseVersion`), a mismatch returns 409 and the
  current state.
- **Idempotency:** every command carries a client UUID; `(account_id, command_id)` is unique in `command_log`, so a retried
  request never applies twice.
- **Server time:** `now` for every command comes from the server clock (idle rewards cannot be cheated with the device
  clock). `GET /v1/time` lets the client predict with the same clock.
- **Migrations:** embedded SQL, applied at start under an advisory lock.
- **Rate limit:** token bucket per account (in memory; Redis when there is more than one API instance).

### Rules service (`server/rules`)

- ASP.NET Core minimal API compiling `Assets/Mythwake/Runtime/Core` and `Runtime/Content` directly – no copy.
- Loads the content folder at start; its **content version** (hash of all content files) must match the client's.
- `POST /create`, `POST /apply` (batch of commands, all-or-nothing), `POST /arena/battle`, `GET /health`.
- Returns the new state and its SHA-256 hash (hash of the save JSON). The client computes the same hash on its
  predicted state; only on a mismatch it downloads the full state.

### Client (`ServerAuthority`)

- Executes every command locally first (instant UI, identical rules), queues it, and sends the queue in order.
- Uses server time (offset from `/v1/time`) for predictions.
- On 409 / rule error / hash mismatch: replaces the local state with the server's and shows a short notice.
- Falls back to the local, offline authority when no server is configured (development).

## API

| Method | Path | |
|---|---|---|
| POST | `/v1/auth/device` | `{deviceId, deviceSecret}` → `{token, accountId, created}` |
| GET | `/v1/time` | `{nowMs}` |
| GET | `/v1/player` | `{state, version, stateHash, contentVersion, nowMs}` (creates the player on first call) |
| POST | `/v1/commands` | `{baseVersion, commands:[{id, type, data}]}` → `{version, stateHash}`; 409 / 422 with `{error, state, version}` |
| GET | `/v1/arena` | rating, rank, season, defense team, three opponents |
| POST | `/v1/arena/defense` | set the defense formation |
| POST | `/v1/arena/fight` | `{opponentId}` → battle setup + result, rating change |
| GET | `/healthz`, `/readyz` | liveness / database + rules reachable |

## Data (PostgreSQL)

`accounts`, `devices`, `players (state jsonb, version)`, `command_log`, `arena_players`, `arena_battles`.
See `server/api/internal/db/migrations`.

## Local start

`server/dev.ps1` (Windows) or `server/dev.sh`: creates a private Postgres cluster in `server/.local/pg` on port 55432
(uses the installed PostgreSQL binaries, does not touch an installed server), starts the rules service and the API.
Alternative with Docker: `docker compose -f server/docker-compose.yml up`.
