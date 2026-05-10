param(
    [string]$DatabaseUrl = "postgres://mythwake:mythwake@localhost:5432/mythwake?sslmode=disable",
    [int]$Port = 18092,
    [ValidateSet("ledger_write_behind", "write_through", "write_behind")]
    [string]$StateWriteMode = "ledger_write_behind"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$backendPath = Join-Path $repoRoot "backend"
$apiExe = Join-Path $env:TEMP "mythwake-api-postgres-e2e.exe"
$stdoutLog = Join-Path $env:TEMP "mythwake-api-postgres-e2e.out.log"
$stderrLog = Join-Path $env:TEMP "mythwake-api-postgres-e2e.err.log"
$baseUrl = "http://localhost:$Port"

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

function Invoke-Json {
    param(
        [string]$Method = "GET",
        [string]$Path,
        [hashtable]$Headers = @{}
    )

    return Invoke-RestMethod -Method $Method -Uri "$baseUrl$Path" -Headers $Headers
}

function Wait-Api {
    param(
        [System.Diagnostics.Process]$Process
    )

    $deadline = (Get-Date).AddSeconds(15)
    do {
        if ($Process -and $Process.HasExited) {
            $stdout = if (Test-Path $stdoutLog) { (Get-Content $stdoutLog -Tail 80) -join [Environment]::NewLine } else { "" }
            $stderr = if (Test-Path $stderrLog) { (Get-Content $stderrLog -Tail 80) -join [Environment]::NewLine } else { "" }
            throw "API exited before health check. ExitCode=$($Process.ExitCode)`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }

        try {
            return Invoke-Json -Path "/health"
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    } while ((Get-Date) -lt $deadline)

    $stdout = if (Test-Path $stdoutLog) { (Get-Content $stdoutLog -Tail 80) -join [Environment]::NewLine } else { "" }
    $stderr = if (Test-Path $stderrLog) { (Get-Content $stderrLog -Tail 80) -join [Environment]::NewLine } else { "" }
    throw "API did not become healthy at $baseUrl.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
}

function Start-Api {
    $env:MYTHWAKE_API_ADDR = ":$Port"
    $env:MYTHWAKE_ENV = "local-e2e"
    $env:MYTHWAKE_API_VERSION = "0.2.25-e2e"
    $env:MYTHWAKE_DATABASE_URL = $DatabaseUrl
    $env:MYTHWAKE_STATE_WRITE_MODE = $StateWriteMode
    $env:MYTHWAKE_STATE_FLUSH_INTERVAL = "10m"
    $env:MYTHWAKE_STATE_FLUSH_TIMEOUT = "5s"
    $env:MYTHWAKE_REQUIRE_IDEMPOTENCY = "true"

    Remove-Item -LiteralPath $stdoutLog, $stderrLog -Force -ErrorAction SilentlyContinue
    $process = Start-Process -FilePath $apiExe -WorkingDirectory $backendPath -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog -PassThru -WindowStyle Hidden
    $health = Wait-Api -Process $process
    if ($health.database -ne "connected") {
        Stop-Api $process
        throw "Expected PostgreSQL-backed API, got database=$($health.database)."
    }

    return $process
}

function Stop-Api {
    param(
        [System.Diagnostics.Process]$Process
    )

    if ($Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force
        $Process.WaitForExit(5000) | Out-Null
    }
}

function Assert-Equal {
    param(
        [object]$Actual,
        [object]$Expected,
        [string]$Message
    )

    if ($Actual -ne $Expected) {
        throw "$Message Expected=[$Expected] Actual=[$Actual]"
    }
}

Write-Host "Building Mythwake API..."
$goExe = Find-Go
Push-Location $backendPath
try {
    & $goExe build -o $apiExe .\cmd\api
}
finally {
    Pop-Location
}

$firstProcess = $null
$secondProcess = $null

try {
    Write-Host "Starting first API instance on $baseUrl..."
    $firstProcess = Start-Api

    $unauthorizedStatus = $null
    try {
        Invoke-Json -Path "/player/state" | Out-Null
    }
    catch {
        if ($_.Exception.Response) {
            $unauthorizedStatus = [int]$_.Exception.Response.StatusCode
        }
        else {
            throw
        }
    }
    Assert-Equal $unauthorizedStatus 401 "Protected player state should reject missing sessions."

    $login = Invoke-Json -Method "POST" -Path "/auth/guest"
    $authHeaders = @{ "Authorization" = "Bearer $($login.sessionToken)" }
    $stateBefore = Invoke-Json -Path "/player/state" -Headers $authHeaders
    Assert-Equal $stateBefore.playerId $login.playerId "State player should match guest login."

    $idempotencyKey = "e2e-campaign-$([guid]::NewGuid().ToString("N"))"
    $actionHeaders = @{
        "Authorization" = $authHeaders["Authorization"]
        "Idempotency-Key" = $idempotencyKey
    }
    $fight = Invoke-Json -Method "POST" -Path "/campaign/fight" -Headers $actionHeaders
    if (-not $fight.success) {
        throw "Expected campaign fight to succeed. Response: $($fight | ConvertTo-Json -Depth 8)"
    }
    Assert-Equal $fight.playerSnapshot.playerId $login.playerId "Action snapshot player should match guest login."

    $flush = Invoke-Json -Method "POST" -Path "/player/state/flush" -Headers $authHeaders
    Assert-Equal $flush.status "ok" "Manual state flush should succeed."

    $stageAfterFight = [int]$fight.playerState.campaignStage
    Stop-Api $firstProcess
    $firstProcess = $null

    Write-Host "Starting second API instance for restart persistence check..."
    $secondProcess = Start-Api

    $stateAfterRestart = Invoke-Json -Path "/player/state" -Headers $authHeaders
    Assert-Equal $stateAfterRestart.playerId $login.playerId "Restarted API should resolve the same session player."
    Assert-Equal ([int]$stateAfterRestart.state.campaignStage) $stageAfterFight "Restarted API should load flushed campaign progress."

    $replay = Invoke-Json -Method "POST" -Path "/campaign/fight" -Headers $actionHeaders
    if (-not $replay.replay) {
        throw "Expected campaign replay after restart for idempotency key $idempotencyKey."
    }
    Assert-Equal ([int]$replay.playerState.campaignStage) $stageAfterFight "Replay should not apply campaign fight twice."

    Write-Host "PostgreSQL E2E smoke passed."
    [pscustomobject]@{
        PlayerId = $login.playerId
        SessionPrefix = $login.sessionToken.Substring(0, [Math]::Min(8, $login.sessionToken.Length))
        CampaignStage = $stageAfterFight
        Replay = $replay.replay
        StateWriteMode = $StateWriteMode
    } | Format-List
}
finally {
    Stop-Api $firstProcess
    Stop-Api $secondProcess
    Remove-Item -LiteralPath $apiExe -Force -ErrorAction SilentlyContinue
}
