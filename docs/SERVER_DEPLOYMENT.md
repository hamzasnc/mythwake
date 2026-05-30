# Mythwake Server Deployment

Last updated: 2026-05-31

This moves the current Mythwake backend from local Windows testing to a small Linux server. It serves only the Go API and runtime services; Unity builds still happen locally on the dev machine.

## What The Server Runs

- Mythwake Go API on port `8080` inside Docker.
- PostgreSQL as durable source of truth.
- Redis for temporary session cache, rate-limit counters, and player locks.
- Caddy as HTTPS reverse proxy in front of the API.

Public traffic should be:

`Android/iOS client -> https://api.your-domain.example -> Caddy -> Mythwake API -> PostgreSQL`

PostgreSQL and Redis must not be exposed to the public internet.

## Recommended Server Size

For the next internal tester phase:

- Recommended: `2 vCPU`, `4 GB RAM`, `40-60 GB SSD/NVMe`.
- Minimum for a tiny smoke server: `1 vCPU`, `2 GB RAM`, `30 GB SSD`, but this is tight once Docker, PostgreSQL, Redis, Caddy, logs, and OS updates share memory.
- Upgrade point: move to `4 vCPU`, `8 GB RAM`, or managed PostgreSQL when more than a small tester group is online, logs grow quickly, or API latency becomes visible.

CPU is not the bottleneck right now. PostgreSQL memory, reliable disk, backups, and safe upgrades matter more.

## Operating System

Use Ubuntu Server `24.04 LTS` for the first small server. Ubuntu `26.04 LTS` is already released, but `24.04 LTS` is the calmer choice for this prototype because hosting images, packages, and operational notes are more settled. Debian 12 is also fine if you prefer Debian.

Do not use Windows Server for this first deployment. The backend itself is portable, but the Linux + Docker path is simpler to maintain and document.

## DNS And Ports

Create a DNS `A` record:

```text
api.example.com -> SERVER_PUBLIC_IP
```

Open only:

- `22/tcp` for SSH, ideally restricted to your IP if your hoster supports it.
- `80/tcp` and `443/tcp` for Caddy and HTTPS certificates.

Keep these closed publicly:

- `5432/tcp` PostgreSQL.
- `6379/tcp` Redis.
- `8080/tcp` direct API port.

## First Server Setup

SSH into the server:

```bash
ssh root@SERVER_PUBLIC_IP
```

Update the OS:

```bash
apt update
apt upgrade -y
reboot
```

Reconnect after reboot, then install Docker:

```bash
ssh root@SERVER_PUBLIC_IP
apt install -y git
git clone https://github.com/hamzasnc/mythwake.git /opt/mythwake
cd /opt/mythwake
bash deploy/install-docker-ubuntu.sh
```

## Configure Mythwake

Create the production env file:

```bash
cd /opt/mythwake
cp deploy/.env.example deploy/.env
nano deploy/.env
```

Set at least:

```text
MYTHWAKE_DOMAIN=api.example.com
POSTGRES_PASSWORD=a-long-random-password-without-url-special-chars
MYTHWAKE_DEV_TOOLS_ENABLED=false
```

Use an alphanumeric random password or URL-encode special characters, because the password is used inside `MYTHWAKE_DATABASE_URL`.

## Start The Stack

```bash
cd /opt/mythwake
docker compose --env-file deploy/.env -f deploy/docker-compose.prod.yml up -d --build
```

Watch startup:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.prod.yml ps
docker compose --env-file deploy/.env -f deploy/docker-compose.prod.yml logs -f api
```

The API runs embedded migrations automatically on startup. First boot may take a moment while PostgreSQL initializes.

## Smoke Test

From the server:

```bash
curl -fsS https://api.example.com/health
curl -fsS https://api.example.com/time
```

From your local machine:

```powershell
.\scripts\check-backend.cmd -BaseUrl "https://api.example.com" -CheckUnauthorized
```

Expected `/health` signals:

- `database` is `connected`.
- `balance_catalog` is `postgres_snapshot`.
- `state_write_mode` is `ledger_write_behind`.
- `dev_tools` is `false` on the public server.

## Build Android Against The Server

The local Android tester APK still defaults to `http://127.0.0.1:8080` unless you pass a backend URL. For a server build, run:

```powershell
.\scripts\build-android.cmd -BackendBaseUrl "https://api.example.com" -OutputPath "Builds\Android\Mythwake-0.2.170-server.apk"
```

For Play Internal Testing:

```powershell
.\scripts\build-android.cmd -AppBundle -BackendBaseUrl "https://api.example.com" -OutputPath "Builds\Android\Mythwake-0.2.170-play-internal-server.aab"
```

This generates a temporary Unity `Resources` config file during the build and removes it afterward. The local dev default stays unchanged.

## Updating The Server

```bash
cd /opt/mythwake
git pull
docker compose --env-file deploy/.env -f deploy/docker-compose.prod.yml up -d --build
docker compose --env-file deploy/.env -f deploy/docker-compose.prod.yml logs -f api
```

Before larger tester pushes, keep a manual DB dump:

```bash
mkdir -p /opt/mythwake-backups
set -a
. deploy/.env
set +a
docker compose --env-file deploy/.env -f deploy/docker-compose.prod.yml exec -T postgres pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" > "/opt/mythwake-backups/mythwake-$(date +%Y%m%d-%H%M%S).sql"
```

## Restore A Dump

Only restore into an intentionally stopped or disposable environment:

```bash
cd /opt/mythwake
set -a
. deploy/.env
set +a
docker compose --env-file deploy/.env -f deploy/docker-compose.prod.yml stop api
cat /opt/mythwake-backups/mythwake-YYYYMMDD-HHMMSS.sql | docker compose --env-file deploy/.env -f deploy/docker-compose.prod.yml exec -T postgres psql -U "$POSTGRES_USER" "$POSTGRES_DB"
docker compose --env-file deploy/.env -f deploy/docker-compose.prod.yml start api
```

## Operational Rules

- Keep `MYTHWAKE_DEV_TOOLS_ENABLED=false` on public servers so `/dev/player/reset` stays unavailable.
- Keep gameplay rate limiting at `0` for now; player-visible fight/dungeon spam is handled by Unity request gating, idempotency, and per-player locks.
- Keep Redis treated as temporary only. PostgreSQL is the durable source of truth.
- Back up PostgreSQL before updates, tester waves, or balance migrations.
- Do not expose PostgreSQL, Redis, or direct port `8080`.

## Official References

- Ubuntu releases: https://releases.ubuntu.com/
- Docker Engine on Ubuntu: https://docs.docker.com/installation/ubuntulinux/
- Caddy reverse proxy: https://caddyserver.com/docs/quick-starts/reverse-proxy
