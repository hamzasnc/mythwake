param(
    [string]$DatabaseUrl = "postgres://mythwake:mythwake@localhost:5432/mythwake?sslmode=disable",
    [string]$ApiAddr = ":8080",
    [string]$StateFlushInterval = "30s",
    [string]$SessionCacheTTL = "30s",
    [string]$SessionTouchWindow = "30s",
    [string]$RedisAddr = "",
    [string]$RedisPassword = "",
    [int]$RedisDB = 0,
    [string]$RateLimitWindow = "1m",
    [int]$RateLimitAuth = 30,
    [int]$RateLimitGameplay = 240,
    [string]$PlayerLockTTL = "5s",
    [string]$PlayerContextIdleTTL = "30m",
    [string]$PlayerContextSweepInterval = "5m",
    [ValidateSet("ledger_write_behind", "write_through", "write_behind")]
    [string]$StateWriteMode = "ledger_write_behind",
    [switch]$NoDatabase,
    [switch]$AllowMissingIdempotency,
    [switch]$DisableDevTools,
    [switch]$DisableRateLimit
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$backendPath = Join-Path $repoRoot "backend"

function Find-Go {
    $goCommand = Get-Command "go" -ErrorAction SilentlyContinue
    if ($goCommand) {
        return $goCommand.Source
    }

    $defaultGo = "C:\Program Files\Go\bin\go.exe"
    if (Test-Path $defaultGo) {
        return $defaultGo
    }

    throw "Go was not found. Install Go or add it to PATH."
}

function Test-TcpPort {
    param(
        [string]$HostName,
        [int]$Port
    )

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $connect = $client.BeginConnect($HostName, $Port, $null, $null)
        if (-not $connect.AsyncWaitHandle.WaitOne(1500, $false)) {
            return $false
        }

        $client.EndConnect($connect)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
}

function Start-PostgresServiceIfNeeded {
    $services = @(Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue)
    if ($services.Count -eq 0) {
        return
    }

    foreach ($service in $services) {
        if ($service.Status -eq "Running") {
            return
        }
    }

    $serviceToStart = $services[0]
    Write-Host "Starting Windows service: $($serviceToStart.Name)"
    Start-Service -Name $serviceToStart.Name
    $serviceToStart.WaitForStatus("Running", "00:00:10")
}

$goExe = Find-Go
$env:MYTHWAKE_API_ADDR = $ApiAddr
$env:MYTHWAKE_ENV = "local"
$env:MYTHWAKE_API_VERSION = "0.2.55"
$env:MYTHWAKE_STATE_FLUSH_INTERVAL = $StateFlushInterval
$env:MYTHWAKE_STATE_FLUSH_TIMEOUT = "5s"
$env:MYTHWAKE_STATE_WRITE_MODE = $StateWriteMode
$env:MYTHWAKE_REDIS_ADDR = $RedisAddr
$env:MYTHWAKE_REDIS_PASSWORD = $RedisPassword
$env:MYTHWAKE_REDIS_DB = "$RedisDB"
$env:MYTHWAKE_SESSION_CACHE_STORE = if ($RedisAddr) { "redis" } else { "memory" }
$env:MYTHWAKE_SESSION_CACHE_TTL = $SessionCacheTTL
$env:MYTHWAKE_SESSION_TOUCH_WINDOW = $SessionTouchWindow
$env:MYTHWAKE_RATE_LIMIT_STORE = if ($RedisAddr) { "redis" } else { "memory" }
$env:MYTHWAKE_RATE_LIMIT_ENABLED = if ($DisableRateLimit) { "false" } else { "true" }
$env:MYTHWAKE_RATE_LIMIT_WINDOW = $RateLimitWindow
$env:MYTHWAKE_RATE_LIMIT_AUTH = [string]$RateLimitAuth
$env:MYTHWAKE_RATE_LIMIT_GAMEPLAY = [string]$RateLimitGameplay
$env:MYTHWAKE_PLAYER_LOCK_STORE = if ($RedisAddr) { "redis" } else { "memory" }
$env:MYTHWAKE_PLAYER_LOCK_TTL = $PlayerLockTTL
$env:MYTHWAKE_PLAYER_CONTEXT_IDLE_TTL = $PlayerContextIdleTTL
$env:MYTHWAKE_PLAYER_CONTEXT_SWEEP_INTERVAL = $PlayerContextSweepInterval
$env:MYTHWAKE_REQUIRE_IDEMPOTENCY = if ($AllowMissingIdempotency) { "false" } else { "true" }
$env:MYTHWAKE_DEV_TOOLS_ENABLED = if ($DisableDevTools) { "false" } else { "true" }

if ($NoDatabase) {
    Remove-Item Env:\MYTHWAKE_DATABASE_URL -ErrorAction SilentlyContinue
    Write-Host "Starting Mythwake API without PostgreSQL." -ForegroundColor Yellow
}
else {
    $databaseUri = [Uri]$DatabaseUrl
    $databaseHost = $databaseUri.Host
    $databasePort = $databaseUri.Port
    if ($databasePort -lt 1) {
        $databasePort = 5432
    }

    if (-not (Test-TcpPort -HostName $databaseHost -Port $databasePort)) {
        Start-PostgresServiceIfNeeded
    }

    if (-not (Test-TcpPort -HostName $databaseHost -Port $databasePort)) {
        throw "PostgreSQL is not reachable on ${databaseHost}:${databasePort}. Start the PostgreSQL Windows service first."
    }

    $env:MYTHWAKE_DATABASE_URL = $DatabaseUrl
    Write-Host "Starting Mythwake API with PostgreSQL." -ForegroundColor Green
}

Write-Host "Backend: $backendPath"
Write-Host "Address: $ApiAddr"
Write-Host "State write mode: $StateWriteMode"
Write-Host "State flush interval: $StateFlushInterval"
Write-Host "Redis: $(if ($RedisAddr) { $RedisAddr } else { 'disabled' })"
Write-Host "Session cache store: $(if ($RedisAddr) { 'redis' } else { 'memory' })"
Write-Host "Session cache TTL: $SessionCacheTTL"
Write-Host "Session touch window: $SessionTouchWindow"
Write-Host "Rate limit store: $(if ($RedisAddr) { 'redis' } else { 'memory' })"
Write-Host "Rate limit enabled: $($env:MYTHWAKE_RATE_LIMIT_ENABLED)"
Write-Host "Rate limit window: $RateLimitWindow"
Write-Host "Rate limit auth/gameplay: $RateLimitAuth / $RateLimitGameplay"
Write-Host "Player lock store: $(if ($RedisAddr) { 'redis' } else { 'memory' })"
Write-Host "Player lock TTL: $PlayerLockTTL"
Write-Host "Require idempotency: $($env:MYTHWAKE_REQUIRE_IDEMPOTENCY)"
Write-Host "Dev tools enabled: $($env:MYTHWAKE_DEV_TOOLS_ENABLED)"
Write-Host "Stop server with Ctrl+C."
Write-Host ""

Push-Location $backendPath
try {
    & $goExe run ./cmd/api
}
finally {
    Pop-Location
}
