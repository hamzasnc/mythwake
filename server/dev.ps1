<#
.SYNOPSIS
  Starts the Mythwake server locally: private PostgreSQL cluster, rules service, Go API.

.DESCRIPTION
  Uses the installed PostgreSQL binaries (C:\Program Files\PostgreSQL\<n>\bin or $env:MYTHWAKE_PG_BIN) to run a
  separate cluster in server\.local\pg on port 55432 - an installed PostgreSQL server is not touched.
  Ctrl+C stops the API and the rules service; the database keeps running (stop it with -Stop).

.EXAMPLE
  .\server\dev.ps1          # start everything
  .\server\dev.ps1 -Stop    # stop the local database
  .\server\dev.ps1 -Reset   # delete the local database and start fresh
#>
param([switch]$Stop, [switch]$Reset)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$local = Join-Path $PSScriptRoot '.local'
$data = Join-Path $local 'pg'
$port = 55432

function Find-PgBin {
    if ($env:MYTHWAKE_PG_BIN) { return $env:MYTHWAKE_PG_BIN }
    $dirs = Get-ChildItem 'C:\Program Files\PostgreSQL' -Directory -ErrorAction SilentlyContinue |
        Sort-Object { [int]($_.Name -replace '\D', '') } -Descending
    foreach ($d in $dirs) {
        $bin = Join-Path $d.FullName 'bin'
        if (Test-Path (Join-Path $bin 'pg_ctl.exe')) { return $bin }
    }
    throw 'PostgreSQL binaries not found. Install PostgreSQL or set MYTHWAKE_PG_BIN.'
}

$pg = Find-PgBin
$pgCtl = Join-Path $pg 'pg_ctl.exe'

if ($Stop -or $Reset) {
    if (Test-Path $data) { & $pgCtl -D $data stop -m fast 2>$null | Out-Null }
    if ($Reset -and (Test-Path $data)) { Remove-Item -Recurse -Force $data; Write-Host 'local database deleted' }
    if ($Stop) { Write-Host 'local database stopped'; return }
}

New-Item -ItemType Directory -Force $local | Out-Null
if (-not (Test-Path (Join-Path $data 'PG_VERSION'))) {
    Write-Host "creating local PostgreSQL cluster in $data"
    & (Join-Path $pg 'initdb.exe') -D $data -U mythwake -A trust -E UTF8 --locale=C | Out-Null
}
& $pgCtl -D $data status *> $null
if ($LASTEXITCODE -ne 0) {
    # pg_ctl gets its own hidden console: the postmaster must not share this terminal (its child processes fail with
    # 0xC0000142 once this console closes) and must not inherit our output pipe (Out-Null would wait forever)
    $proc = Start-Process $pgCtl -WindowStyle Hidden -PassThru -ArgumentList @(
        '-D', "`"$data`"", '-l', "`"$(Join-Path $local 'pg.log')`"", '-o', "`"-p $port -h 127.0.0.1`"", '-w', 'start')
    $proc.WaitForExit()
    if ($proc.ExitCode -ne 0) { throw "PostgreSQL did not start, see server\.local\pg.log" }
}
$exists = & (Join-Path $pg 'psql.exe') -h 127.0.0.1 -p $port -U mythwake -d postgres -tAc "select 1 from pg_database where datname = 'mythwake'"
if ($exists -ne '1') { & (Join-Path $pg 'createdb.exe') -h 127.0.0.1 -p $port -U mythwake mythwake }
Write-Host "PostgreSQL ready on 127.0.0.1:$port"

# a random JWT secret per machine, created once
$secretFile = Join-Path $local 'jwt-secret.txt'
if (-not (Test-Path $secretFile)) {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    [Convert]::ToBase64String($bytes) | Set-Content -NoNewline -Encoding ascii $secretFile
}

$env:MYTHWAKE_CONTENT = Join-Path $root 'Assets\Mythwake\Content'
$env:MYTHWAKE_RULES_ADDR = 'http://127.0.0.1:5081'
$env:MYTHWAKE_RULES_URL = 'http://127.0.0.1:5081'
$env:MYTHWAKE_DATABASE_URL = "postgres://mythwake@127.0.0.1:$port/mythwake?sslmode=disable"
$env:MYTHWAKE_JWT_SECRET = Get-Content -Raw $secretFile
$env:MYTHWAKE_ADDR = if ($env:MYTHWAKE_ADDR) { $env:MYTHWAKE_ADDR } else { '127.0.0.1:8080' }

Write-Host 'building rules service'
dotnet build (Join-Path $PSScriptRoot 'rules') -nologo -v q | Out-Null
$rules = Start-Process dotnet -ArgumentList @('run', '--no-build', '--project', "`"$(Join-Path $PSScriptRoot 'rules')`"") `
    -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $local 'rules.log') -RedirectStandardError (Join-Path $local 'rules.err.log')
try {
    Write-Host "starting API on http://$($env:MYTHWAKE_ADDR)  (rules log: server\.local\rules.log)"
    Push-Location (Join-Path $PSScriptRoot 'api')
    go run ./cmd/mythwake-api
}
finally {
    Pop-Location
    if (-not $rules.HasExited) { Stop-Process -Id $rules.Id -Force }
    Get-Process Mythwake.Rules -ErrorAction SilentlyContinue | Stop-Process -Force
}
