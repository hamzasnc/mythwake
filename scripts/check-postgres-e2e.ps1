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
    $env:MYTHWAKE_API_VERSION = "0.2.34-e2e"
    $env:MYTHWAKE_DATABASE_URL = $DatabaseUrl
    $env:MYTHWAKE_STATE_WRITE_MODE = $StateWriteMode
    $env:MYTHWAKE_STATE_FLUSH_INTERVAL = "10m"
    $env:MYTHWAKE_STATE_FLUSH_TIMEOUT = "5s"
    $env:MYTHWAKE_SESSION_CACHE_TTL = "30s"
    $env:MYTHWAKE_SESSION_TOUCH_WINDOW = "30s"
    $env:MYTHWAKE_RATE_LIMIT_ENABLED = "true"
    $env:MYTHWAKE_RATE_LIMIT_WINDOW = "1m"
    $env:MYTHWAKE_RATE_LIMIT_AUTH = "30"
    $env:MYTHWAKE_RATE_LIMIT_GAMEPLAY = "240"
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

    $definitions = Invoke-Json -Path "/definitions"
    $stageOneDefinition = $definitions.campaignStages | Where-Object { [int]$_.stageNumber -eq 1 } | Select-Object -First 1
    if (-not $stageOneDefinition -or [int]$stageOneDefinition.enemyMaxHp -le 0 -or [int]$stageOneDefinition.enemyDamage -le 0) {
        throw "Expected definitions to include campaign combat stats. Response: $($definitions | ConvertTo-Json -Depth 8)"
    }
    $goldDungeonDefinition = $definitions.dungeons | Where-Object { $_.dungeonId -eq "gold_dungeon" } | Select-Object -First 1
    if (-not $goldDungeonDefinition -or [int]$goldDungeonDefinition.enemyBaseHp -le 0 -or [int]$goldDungeonDefinition.enemyDamagePowerDivisor -le 0) {
        throw "Expected definitions to include dungeon combat curves. Response: $($definitions | ConvertTo-Json -Depth 8)"
    }

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
    if ([string]::IsNullOrWhiteSpace($stateBefore.lastAfkClaimUtc)) {
        throw "Expected player state to include lastAfkClaimUtc."
    }
    if (@($stateBefore.dailyProgress).Count -lt 3) {
        throw "Expected player state to include daily mission progress."
    }

    $afkHeaders = @{
        "Authorization" = $authHeaders["Authorization"]
        "Idempotency-Key" = "e2e-afk-$([guid]::NewGuid().ToString("N"))"
    }
    $afk = Invoke-Json -Method "POST" -Path "/player/offline/claim" -Headers $afkHeaders
    Assert-Equal $afk.actionId "afk_reward_claim" "AFK claim should return the afk action id."
    if ([string]::IsNullOrWhiteSpace($afk.playerSnapshot.lastAfkClaimUtc)) {
        throw "Expected AFK action snapshot to include lastAfkClaimUtc."
    }

    $idempotencyKey = "e2e-campaign-$([guid]::NewGuid().ToString("N"))"
    $actionHeaders = @{
        "Authorization" = $authHeaders["Authorization"]
        "Idempotency-Key" = $idempotencyKey
    }
    $fight = Invoke-Json -Method "POST" -Path "/campaign/fight" -Headers $actionHeaders
    if (-not $fight.success) {
        throw "Expected campaign fight to succeed. Response: $($fight | ConvertTo-Json -Depth 8)"
    }
    if (-not $fight.combat -or -not $fight.combat.won -or [int]$fight.combat.elapsedSeconds -le 0 -or [int]$fight.combat.maxSeconds -ne 30) {
        throw "Expected campaign fight to include a successful server combat result. Response: $($fight | ConvertTo-Json -Depth 8)"
    }
    Assert-Equal $fight.playerSnapshot.playerId $login.playerId "Action snapshot player should match guest login."
    $fightDailyProgress = $fight.playerSnapshot.dailyProgress | Where-Object { $_.missionId -eq "daily_stage_clears_3" } | Select-Object -First 1
    Assert-Equal ([int]$fightDailyProgress.progress) 1 "Campaign fight should advance daily stage-clear progress."

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

    $logout = Invoke-Json -Method "POST" -Path "/auth/logout" -Headers $authHeaders
    Assert-Equal $logout.status "ok" "Logout should return ok."
    Assert-Equal $logout.stateFlushed $true "Logout should flush the loaded player state."

    $revokedStatus = $null
    try {
        Invoke-Json -Path "/player/state" -Headers $authHeaders | Out-Null
    }
    catch {
        if ($_.Exception.Response) {
            $revokedStatus = [int]$_.Exception.Response.StatusCode
        }
        else {
            throw
        }
    }
    Assert-Equal $revokedStatus 401 "Revoked session should reject protected state."

    Write-Host "PostgreSQL E2E smoke passed."
    [pscustomobject]@{
        PlayerId = $login.playerId
        SessionPrefix = $login.sessionToken.Substring(0, [Math]::Min(8, $login.sessionToken.Length))
        CampaignStage = $stageAfterFight
        Replay = $replay.replay
        LogoutRevoked = $revokedStatus -eq 401
        LogoutStateFlushed = $logout.stateFlushed
        StateWriteMode = $StateWriteMode
    } | Format-List
}
finally {
    Stop-Api $firstProcess
    Stop-Api $secondProcess
    Remove-Item -LiteralPath $apiExe -Force -ErrorAction SilentlyContinue
}
