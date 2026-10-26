#!/usr/bin/env bash
# Starts the Mythwake server locally: private PostgreSQL cluster (port 55432), rules service, Go API.
# Usage: server/dev.sh [--stop | --reset]
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
root="$(dirname "$here")"
local_dir="$here/.local"
data="$local_dir/pg"
port=55432

pg_bin="${MYTHWAKE_PG_BIN:-$(dirname "$(command -v pg_ctl 2>/dev/null || echo /usr/lib/postgresql/18/bin/pg_ctl)")}"

if [[ "${1:-}" == "--stop" || "${1:-}" == "--reset" ]]; then
  [[ -d "$data" ]] && "$pg_bin/pg_ctl" -D "$data" stop -m fast >/dev/null 2>&1 || true
  [[ "${1:-}" == "--reset" ]] && rm -rf "$data" && echo "local database deleted"
  [[ "${1:-}" == "--stop" ]] && { echo "local database stopped"; exit 0; }
fi

mkdir -p "$local_dir"
if [[ ! -f "$data/PG_VERSION" ]]; then
  echo "creating local PostgreSQL cluster in $data"
  "$pg_bin/initdb" -D "$data" -U mythwake -A trust -E UTF8 --locale=C >/dev/null
fi
if ! "$pg_bin/pg_ctl" -D "$data" status >/dev/null 2>&1; then
  "$pg_bin/pg_ctl" -D "$data" -l "$local_dir/pg.log" -o "-p $port -h 127.0.0.1" -w start >/dev/null
fi
if [[ "$("$pg_bin/psql" -h 127.0.0.1 -p $port -U mythwake -d postgres -tAc "select 1 from pg_database where datname='mythwake'")" != "1" ]]; then
  "$pg_bin/createdb" -h 127.0.0.1 -p $port -U mythwake mythwake
fi
echo "PostgreSQL ready on 127.0.0.1:$port"

secret_file="$local_dir/jwt-secret.txt"
[[ -f "$secret_file" ]] || head -c 48 /dev/urandom | base64 | tr -d '\n' > "$secret_file"

export MYTHWAKE_CONTENT="$root/Assets/Mythwake/Content"
export MYTHWAKE_RULES_ADDR="http://127.0.0.1:5081"
export MYTHWAKE_RULES_URL="http://127.0.0.1:5081"
export MYTHWAKE_DATABASE_URL="postgres://mythwake@127.0.0.1:$port/mythwake?sslmode=disable"
export MYTHWAKE_JWT_SECRET="$(cat "$secret_file")"
export MYTHWAKE_ADDR="${MYTHWAKE_ADDR:-127.0.0.1:8080}"

dotnet build "$here/rules" -nologo -v q >/dev/null
dotnet run --no-build --project "$here/rules" >"$local_dir/rules.log" 2>&1 &
rules_pid=$!
trap 'kill $rules_pid 2>/dev/null || true' EXIT

echo "starting API on http://$MYTHWAKE_ADDR (rules log: server/.local/rules.log)"
cd "$here/api" && go run ./cmd/mythwake-api
