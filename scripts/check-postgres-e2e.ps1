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
        [hashtable]$Headers = @{},
        [object]$Body = $null
    )

    if ($null -ne $Body) {
        return Invoke-RestMethod -Method $Method -Uri "$baseUrl$Path" -Headers $Headers -Body ([string]$Body) -ContentType "application/json"
    }

    return Invoke-RestMethod -Method $Method -Uri "$baseUrl$Path" -Headers $Headers
}

function Invoke-JsonExpectError {
    param(
        [string]$Method = "GET",
        [string]$Path,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int]$ExpectedStatus
    )

    $request = [System.Net.HttpWebRequest]::Create("$baseUrl$Path")
    $request.Method = $Method
    $request.Accept = "application/json"
    foreach ($header in $Headers.GetEnumerator()) {
        $request.Headers[$header.Key] = [string]$header.Value
    }

    if ($null -ne $Body) {
        $request.ContentType = "application/json"
        $bytes = [System.Text.Encoding]::UTF8.GetBytes([string]$Body)
        $request.ContentLength = $bytes.Length
        $requestStream = $request.GetRequestStream()
        try {
            $requestStream.Write($bytes, 0, $bytes.Length)
        }
        finally {
            $requestStream.Close()
        }
    }

    try {
        $response = $request.GetResponse()
        if ($response) {
            $response.Close()
        }
    }
    catch [System.Net.WebException] {
        if (-not $_.Exception.Response) {
            throw
        }

        $statusCode = [int]$_.Exception.Response.StatusCode
        $responseStream = $_.Exception.Response.GetResponseStream()
        $reader = [System.IO.StreamReader]::new($responseStream)
        $bodyText = $reader.ReadToEnd()
        $parsedBody = if ([string]::IsNullOrWhiteSpace($bodyText)) { $null } else { $bodyText | ConvertFrom-Json }

        if ($statusCode -ne $ExpectedStatus) {
            throw "Expected $Method $Path to fail with HTTP $ExpectedStatus, got $statusCode. Body: $bodyText"
        }

        return [pscustomobject]@{
            StatusCode = $statusCode
            Body = $parsedBody
        }
    }

    throw "Expected $Method $Path to fail with HTTP $ExpectedStatus, but it succeeded."
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
    $env:MYTHWAKE_API_VERSION = "0.2.56-e2e"
    $env:MYTHWAKE_DATABASE_URL = $DatabaseUrl
    $env:MYTHWAKE_REDIS_ADDR = ""
    $env:MYTHWAKE_REDIS_PASSWORD = ""
    $env:MYTHWAKE_REDIS_DB = "0"
    $env:MYTHWAKE_SESSION_CACHE_STORE = "memory"
    $env:MYTHWAKE_RATE_LIMIT_STORE = "memory"
    $env:MYTHWAKE_PLAYER_LOCK_STORE = "memory"
    $env:MYTHWAKE_STATE_WRITE_MODE = $StateWriteMode
    $env:MYTHWAKE_STATE_FLUSH_INTERVAL = "10m"
    $env:MYTHWAKE_STATE_FLUSH_TIMEOUT = "5s"
    $env:MYTHWAKE_SESSION_CACHE_TTL = "30s"
    $env:MYTHWAKE_SESSION_TOUCH_WINDOW = "30s"
    $env:MYTHWAKE_RATE_LIMIT_ENABLED = "true"
    $env:MYTHWAKE_RATE_LIMIT_WINDOW = "1m"
    $env:MYTHWAKE_RATE_LIMIT_AUTH = "30"
    $env:MYTHWAKE_RATE_LIMIT_GAMEPLAY = "0"
    $env:MYTHWAKE_PLAYER_LOCK_TTL = "5s"
    $env:MYTHWAKE_PLAYER_CONTEXT_IDLE_TTL = "30m"
    $env:MYTHWAKE_PLAYER_CONTEXT_SWEEP_INTERVAL = "5m"
    $env:MYTHWAKE_REQUIRE_IDEMPOTENCY = "true"
    $env:MYTHWAKE_DEV_TOOLS_ENABLED = "true"

    Remove-Item -LiteralPath $stdoutLog, $stderrLog -Force -ErrorAction SilentlyContinue
    $process = Start-Process -FilePath $apiExe -WorkingDirectory $backendPath -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog -PassThru -WindowStyle Hidden
    $health = Wait-Api -Process $process
    if ($health.database -ne "connected") {
        Stop-Api $process
        throw "Expected PostgreSQL-backed API, got database=$($health.database)."
    }
    if ($health.balance_catalog -ne "postgres_snapshot") {
        Stop-Api $process
        throw "Expected PostgreSQL-backed gameplay balance, got balance_catalog=$($health.balance_catalog)."
    }
    if ($health.dev_tools -ne "true") {
        Stop-Api $process
        throw "Expected local E2E API to expose dev tools, got dev_tools=$($health.dev_tools)."
    }
    if ($health.rate_limit_gameplay -ne "0") {
        Stop-Api $process
        throw "Expected local E2E API gameplay rate limit to be disabled, got rate_limit_gameplay=$($health.rate_limit_gameplay)."
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

function Assert-GreaterOrEqual {
    param(
        [int64]$Actual,
        [int64]$Expected,
        [string]$Message
    )

    if ($Actual -lt $Expected) {
        throw "$Message Expected at least=[$Expected] Actual=[$Actual]"
    }
}

function New-GameplayHeaders {
    param(
        [hashtable]$AuthHeaders,
        [int64]$Revision,
        [string]$Prefix
    )

    return @{
        "Authorization" = $AuthHeaders["Authorization"]
        "X-Player-State-Revision" = [string]$Revision
        "Idempotency-Key" = "e2e-$Prefix-$([guid]::NewGuid().ToString("N"))"
    }
}

function Invoke-GameplayPost {
    param(
        [string]$Path,
        [hashtable]$AuthHeaders,
        [ref]$Revision,
        [string]$Label,
        [object]$Body = $null
    )

    $previousRevision = [int64]$Revision.Value
    $headers = New-GameplayHeaders -AuthHeaders $AuthHeaders -Revision $previousRevision -Prefix ($Label.ToLowerInvariant() -replace '[^a-z0-9]+', '-')
    if ($null -ne $Body) {
        $result = Invoke-Json -Method "POST" -Path $Path -Headers $headers -Body $Body
    }
    else {
        $result = Invoke-Json -Method "POST" -Path $Path -Headers $headers
    }
    if (-not $result.success) {
        throw "$Label should succeed. Response: $($result | ConvertTo-Json -Depth 10)"
    }

    Assert-GreaterOrEqual ([int64]$result.playerSnapshot.revision) ($previousRevision + 1) "$Label should advance the server state revision."
    Assert-Equal ([int64]$result.receipt.stateRevision) ([int64]$result.playerSnapshot.revision) "$Label receipt should match snapshot revision."
    $Revision.Value = [int64]$result.playerSnapshot.revision

    return $result
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
    Assert-Equal ([int]$definitions.schemaVersion) 5 "Definitions schema should include DB-owned AFK reward fields."
    $starterHeroDefinitions = @($definitions.heroes | Where-Object { $_.starterOwned -eq $true })
    $astraDefinition = $definitions.heroes | Where-Object { $_.heroId -eq "hero_astra" } | Select-Object -First 1
    if (-not $astraDefinition -or [int]$astraDefinition.maxLevel -le 0 -or [int]$astraDefinition.baseAttack -le 0 -or [int]$astraDefinition.baseHealth -le 0) {
        throw "Expected hero definitions to include stat scaling fields. Response: $($definitions | ConvertTo-Json -Depth 8)"
    }
    $weaponDefinition = $definitions.equipment | Where-Object { $_.equipmentId -eq "equipment_weapon" } | Select-Object -First 1
    if (-not $weaponDefinition -or [int]$weaponDefinition.maxLevel -le 0 -or [int]$weaponDefinition.attackPerLevel -le 0) {
        throw "Expected equipment definitions to include stat scaling fields. Response: $($definitions | ConvertTo-Json -Depth 8)"
    }
    $starterAccessoryDefinition = $definitions.accessories | Where-Object { $_.accessoryId -eq "accessory_earrings_r0" } | Select-Object -First 1
    if (-not $starterAccessoryDefinition -or $starterAccessoryDefinition.slotId -ne "earrings") {
        throw "Expected definitions to include starter accessory drop accessory_earrings_r0. Response: $($definitions | ConvertTo-Json -Depth 8)"
    }
    $afkDefinition = $definitions.afkRewards | Where-Object { $_.rewardId -eq "reward_afk_claim" } | Select-Object -First 1
    if (-not $afkDefinition -or [int]$afkDefinition.minClaimSeconds -ne 60 -or [int]$afkDefinition.maxClaimSeconds -ne 21600 -or [int]$afkDefinition.tickSeconds -ne 60) {
        throw "Expected definitions to include DB-owned AFK reward settings. Response: $($definitions | ConvertTo-Json -Depth 8)"
    }
    $stageOneDefinition = $definitions.campaignStages | Where-Object { [int]$_.stageNumber -eq 1 } | Select-Object -First 1
    if (-not $stageOneDefinition -or [int]$stageOneDefinition.enemyMaxHp -le 0 -or [int]$stageOneDefinition.enemyDamage -le 0) {
        throw "Expected definitions to include campaign combat stats. Response: $($definitions | ConvertTo-Json -Depth 8)"
    }
    $goldDungeonDefinition = $definitions.dungeons | Where-Object { $_.dungeonId -eq "gold_dungeon" } | Select-Object -First 1
    if (-not $goldDungeonDefinition -or [int]$goldDungeonDefinition.enemyBaseHp -le 0 -or [int]$goldDungeonDefinition.enemyDamagePowerDivisor -le 0) {
        throw "Expected definitions to include dungeon combat curves. Response: $($definitions | ConvertTo-Json -Depth 8)"
    }

    $missingSession = Invoke-JsonExpectError -Path "/player/state" -Headers @{ "X-Request-ID" = "e2e-missing-session-001" } -ExpectedStatus 401
    Assert-Equal $missingSession.Body.errorCode "missing_session" "Protected player state should reject missing sessions with a structured error code."
    Assert-Equal $missingSession.Body.requestId "e2e-missing-session-001" "Missing-session errors should preserve the client request id."

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
    if (@($stateBefore.heroes).Count -ne $starterHeroDefinitions.Count) {
        throw "Expected player state starter heroes to match definitions. Heroes=$(@($stateBefore.heroes).Count) Starters=$($starterHeroDefinitions.Count)"
    }
    if (@($stateBefore.heroShards).Count -lt @($definitions.heroes).Count) {
        throw "Expected player state to include initial shard rows for known heroes."
    }
    Assert-GreaterOrEqual ([int64]$stateBefore.revision) 1 "Player state should include a server revision."
    if ([string]::IsNullOrWhiteSpace($stateBefore.updatedAtUtc)) {
        throw "Expected player state to include updatedAtUtc."
    }

    $bootstrap = Invoke-Json -Path "/client/bootstrap" -Headers $authHeaders
    Assert-Equal $bootstrap.playerSnapshot.playerId $login.playerId "Bootstrap snapshot player should match guest login."
    if ([string]::IsNullOrWhiteSpace($bootstrap.serverClock.serverTimeUtc) -or [int64]$bootstrap.serverClock.serverUnixMs -le 0) {
        throw "Expected bootstrap to include server clock. Response: $($bootstrap | ConvertTo-Json -Depth 8)"
    }
    if ([string]::IsNullOrWhiteSpace($bootstrap.definitions.contentHash) -or @($bootstrap.definitions.gameplayActions).Count -eq 0) {
        throw "Expected bootstrap to include definition snapshot. Response: $($bootstrap | ConvertTo-Json -Depth 8)"
    }
    Assert-Equal $bootstrap.definitions.contentHash $definitions.contentHash "Bootstrap definitions should match /definitions content hash."
    Assert-Equal ([int64]$bootstrap.playerSnapshot.revision) ([int64]$stateBefore.revision) "Bootstrap snapshot should include the current state revision."

    $afkHeaders = @{
        "Authorization" = $authHeaders["Authorization"]
        "X-Player-State-Revision" = [string]$stateBefore.revision
        "Idempotency-Key" = "e2e-afk-$([guid]::NewGuid().ToString("N"))"
    }
    $afk = Invoke-Json -Method "POST" -Path "/player/offline/claim" -Headers $afkHeaders
    Assert-Equal $afk.actionId "afk_reward_claim" "AFK claim should return the afk action id."
    if ([string]::IsNullOrWhiteSpace($afk.playerSnapshot.lastAfkClaimUtc)) {
        throw "Expected AFK action snapshot to include lastAfkClaimUtc."
    }

    $missingIdempotency = Invoke-JsonExpectError -Method "POST" -Path "/campaign/fight" -Headers @{
        "Authorization" = $authHeaders["Authorization"]
        "X-Player-State-Revision" = [string]$stateBefore.revision
        "X-Request-ID" = "e2e-missing-idempotency-001"
    } -ExpectedStatus 400
    Assert-Equal $missingIdempotency.Body.errorCode "missing_idempotency_key" "Gameplay mutations should reject missing idempotency keys with a structured error code."
    Assert-Equal $missingIdempotency.Body.requestId "e2e-missing-idempotency-001" "Missing-idempotency errors should preserve the client request id."

    $idempotencyKey = "e2e-campaign-$([guid]::NewGuid().ToString("N"))"
    $actionHeaders = @{
        "Authorization" = $authHeaders["Authorization"]
        "X-Player-State-Revision" = [string]$stateBefore.revision
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
    Assert-Equal ([int64]$fight.receipt.stateRevision) ([int64]$fight.playerSnapshot.revision) "Action receipt should match snapshot revision."
    Assert-GreaterOrEqual ([int64]$fight.playerSnapshot.revision) (([int64]$stateBefore.revision) + 1) "Campaign fight should advance the server state revision."
    $fightDailyProgress = $fight.playerSnapshot.dailyProgress | Where-Object { $_.missionId -eq "daily_stage_clears_3" } | Select-Object -First 1
    Assert-Equal ([int]$fightDailyProgress.progress) 1 "Campaign fight should advance daily stage-clear progress."

    $staleHeaders = @{
        "Authorization" = $authHeaders["Authorization"]
        "X-Player-State-Revision" = [string]$stateBefore.revision
        "Idempotency-Key" = "e2e-stale-$([guid]::NewGuid().ToString("N"))"
    }
    $staleFight = Invoke-Json -Method "POST" -Path "/campaign/fight" -Headers $staleHeaders
    if ($staleFight.success -or $staleFight.errorCode -ne "stale_player_state") {
        throw "Expected stale revision rejection after campaign fight. Response: $($staleFight | ConvertTo-Json -Depth 8)"
    }
    Assert-Equal ([int64]$staleFight.playerSnapshot.revision) ([int64]$fight.playerSnapshot.revision) "Stale fight should return latest snapshot revision."
    Assert-Equal ([int]$staleFight.playerState.campaignStage) ([int]$fight.playerState.campaignStage) "Stale fight should not mutate campaign progress."

    $currentRevision = [int64]$fight.playerSnapshot.revision
    $goldDungeon = Invoke-GameplayPost -Path "/dungeons/gold_dungeon/run" -AuthHeaders $authHeaders -Revision ([ref]$currentRevision) -Label "Gold dungeon"
    Assert-Equal $goldDungeon.actionId "gold_dungeon_run" "Gold dungeon should return the gold dungeon action id."
    Assert-Equal $goldDungeon.combat.won $true "Gold dungeon should win in the starter smoke path."
    Assert-GreaterOrEqual ([int64]$goldDungeon.reward.gold) 1 "Gold dungeon should grant gold."

    $essenceDungeon = Invoke-GameplayPost -Path "/dungeons/essence_dungeon/run" -AuthHeaders $authHeaders -Revision ([ref]$currentRevision) -Label "Essence dungeon"
    Assert-Equal $essenceDungeon.actionId "essence_dungeon_run" "Essence dungeon should return the essence dungeon action id."
    Assert-Equal $essenceDungeon.combat.won $true "Essence dungeon should win in the starter smoke path."
    Assert-GreaterOrEqual ([int64]$essenceDungeon.reward.mythEssence) 1 "Essence dungeon should grant Myth Essence."

    $gearDungeon = Invoke-GameplayPost -Path "/dungeons/gear_dungeon/run" -AuthHeaders $authHeaders -Revision ([ref]$currentRevision) -Label "Gear dungeon"
    Assert-Equal $gearDungeon.actionId "gear_dungeon_run" "Gear dungeon should return the gear dungeon action id."
    Assert-Equal $gearDungeon.combat.won $true "Gear dungeon should win in the starter smoke path."
    $droppedAccessory = $gearDungeon.playerSnapshot.accessories | Where-Object { $_.accessoryId -eq "accessory_earrings_r0" } | Select-Object -First 1
    if (-not $droppedAccessory -or [int]$droppedAccessory.copies -lt 1) {
        throw "Expected gear dungeon to add accessory_earrings_r0. Response: $($gearDungeon | ConvertTo-Json -Depth 10)"
    }

    $accessoryEquip = Invoke-GameplayPost -Path "/gear/accessories/equip" -AuthHeaders $authHeaders -Revision ([ref]$currentRevision) -Label "Accessory equip" -Body '{"accessoryId":"accessory_earrings_r0"}'
    Assert-Equal $accessoryEquip.actionId "accessory_equip" "Accessory equip should return the accessory equip action id."
    $equippedAccessory = $accessoryEquip.playerSnapshot.equippedAccessories | Where-Object { $_.slotId -eq "earrings" -and $_.accessoryId -eq "accessory_earrings_r0" } | Select-Object -First 1
    if (-not $equippedAccessory) {
        throw "Expected accessory_equip to equip accessory_earrings_r0. Response: $($accessoryEquip | ConvertTo-Json -Depth 10)"
    }

    $heroLevel = Invoke-GameplayPost -Path "/heroes/hero_astra/level-up" -AuthHeaders $authHeaders -Revision ([ref]$currentRevision) -Label "Hero level"
    Assert-Equal $heroLevel.actionId "hero_level" "Hero level should return the hero level action id."
    $astraState = $heroLevel.playerSnapshot.heroes | Where-Object { $_.heroId -eq "hero_astra" } | Select-Object -First 1
    Assert-Equal ([int]$astraState.level) 2 "Hero level endpoint should level Astra to 2."

    $weaponLevel = Invoke-GameplayPost -Path "/equipment/equipment_weapon/level-up" -AuthHeaders $authHeaders -Revision ([ref]$currentRevision) -Label "Weapon level"
    Assert-Equal $weaponLevel.actionId "equipment_level" "Weapon level should return the equipment level action id."
    $weaponState = $weaponLevel.playerSnapshot.equipment | Where-Object { $_.equipmentId -eq "equipment_weapon" } | Select-Object -First 1
    Assert-Equal ([int]$weaponState.level) 1 "Weapon level endpoint should train weapon to server level 1."

    $summon = Invoke-GameplayPost -Path "/summons/hero_shard_standard/pull" -AuthHeaders $authHeaders -Revision ([ref]$currentRevision) -Label "Summon"
    Assert-Equal $summon.actionId "summon_pull" "Summon should return the summon action id."
    Assert-Equal ([int]$summon.playerSnapshot.summonCount) 1 "Summon should increment summon count."
    $summonDailyProgress = $summon.playerSnapshot.dailyProgress | Where-Object { $_.missionId -eq "daily_summon_1" } | Select-Object -First 1
    Assert-Equal ([int]$summonDailyProgress.progress) 1 "Summon should advance daily summon progress."

    $dailyClaim = Invoke-GameplayPost -Path "/missions/daily_summon_1/claim" -AuthHeaders $authHeaders -Revision ([ref]$currentRevision) -Label "Daily mission"
    Assert-Equal $dailyClaim.actionId "daily_mission_claim" "Daily mission claim should return the mission action id."
    Assert-GreaterOrEqual ([int64]$dailyClaim.playerSnapshot.state.passXp) 40 "Daily mission claim should grant Battle Pass XP."

    $battlePass = Invoke-GameplayPost -Path "/battle-pass/mission_track_reward_01/claim" -AuthHeaders $authHeaders -Revision ([ref]$currentRevision) -Label "Battle pass"
    Assert-Equal $battlePass.actionId "battle_pass_claim" "Battle Pass claim should return the battle pass action id."
    $battlePassClaim = $battlePass.playerSnapshot.battlePassClaims | Where-Object { $_.claimId -eq "mission_track_reward_01" } | Select-Object -First 1
    if (-not $battlePassClaim -or -not $battlePassClaim.claimed) {
        throw "Expected Battle Pass reward 01 to be claimed. Response: $($battlePass | ConvertTo-Json -Depth 10)"
    }
    $finalSnapshotBeforeFlush = $battlePass.playerSnapshot

    $cacheBeforeFlush = Invoke-Json -Path "/health"
    Assert-GreaterOrEqual ([int64]$cacheBeforeFlush.loaded_players) 1 "Health should report loaded player contexts after authenticated gameplay."
    Assert-Equal $cacheBeforeFlush.player_context_idle_ttl "30m0s" "Health should expose player context idle TTL."
    Assert-Equal $cacheBeforeFlush.player_context_sweep_interval "5m0s" "Health should expose player context sweep interval."
    if ($StateWriteMode -ne "write_through") {
        Assert-GreaterOrEqual ([int64]$cacheBeforeFlush.state_cache_dirty) 1 "Write-behind cache should show dirty player state before manual flush."
        Assert-GreaterOrEqual ([int64]$cacheBeforeFlush.state_cache_queued) 1 "Write-behind cache should report queued saves before manual flush."
    }

    $flush = Invoke-Json -Method "POST" -Path "/player/state/flush" -Headers $authHeaders
    Assert-Equal $flush.status "ok" "Manual state flush should succeed."

    $cacheAfterFlush = Invoke-Json -Path "/health"
    Assert-Equal ([int]$cacheAfterFlush.state_cache_dirty) 0 "Manual state flush should clear dirty cache state."
    if ($StateWriteMode -ne "write_through") {
        Assert-GreaterOrEqual ([int64]$cacheAfterFlush.state_cache_flushed) 1 "Manual state flush should increase flushed cache saves."
    }

    $stageAfterFight = [int]$fight.playerState.campaignStage
    Stop-Api $firstProcess
    $firstProcess = $null

    Write-Host "Starting second API instance for restart persistence check..."
    $secondProcess = Start-Api

    $stateAfterRestart = Invoke-Json -Path "/player/state" -Headers $authHeaders
    Assert-Equal $stateAfterRestart.playerId $login.playerId "Restarted API should resolve the same session player."
    Assert-Equal ([int]$stateAfterRestart.state.campaignStage) $stageAfterFight "Restarted API should load flushed campaign progress."
    Assert-Equal ([int64]$stateAfterRestart.revision) ([int64]$finalSnapshotBeforeFlush.revision) "Restarted API should load the flushed state revision."
    Assert-Equal ([int]$stateAfterRestart.state.goldDungeonFloor) ([int]$finalSnapshotBeforeFlush.state.goldDungeonFloor) "Restarted API should load gold dungeon progress."
    Assert-Equal ([int]$stateAfterRestart.state.essenceDungeonFloor) ([int]$finalSnapshotBeforeFlush.state.essenceDungeonFloor) "Restarted API should load essence dungeon progress."
    Assert-Equal ([int]$stateAfterRestart.state.gearDungeonFloor) ([int]$finalSnapshotBeforeFlush.state.gearDungeonFloor) "Restarted API should load gear dungeon progress."
    Assert-Equal ([int]$stateAfterRestart.summonCount) ([int]$finalSnapshotBeforeFlush.summonCount) "Restarted API should load summon progress."
    $restartBattlePassClaim = $stateAfterRestart.battlePassClaims | Where-Object { $_.claimId -eq "mission_track_reward_01" } | Select-Object -First 1
    if (-not $restartBattlePassClaim -or -not $restartBattlePassClaim.claimed) {
        throw "Restarted API should load Battle Pass reward 01 claim. Response: $($stateAfterRestart | ConvertTo-Json -Depth 10)"
    }
    $restartEquippedAccessory = $stateAfterRestart.equippedAccessories | Where-Object { $_.slotId -eq "earrings" -and $_.accessoryId -eq "accessory_earrings_r0" } | Select-Object -First 1
    if (-not $restartEquippedAccessory) {
        throw "Restarted API should load equipped accessory_earrings_r0. Response: $($stateAfterRestart | ConvertTo-Json -Depth 10)"
    }

    $replay = Invoke-Json -Method "POST" -Path "/campaign/fight" -Headers $actionHeaders
    if (-not $replay.replay) {
        throw "Expected campaign replay after restart for idempotency key $idempotencyKey."
    }
    Assert-Equal ([int]$replay.playerState.campaignStage) $stageAfterFight "Replay should not apply campaign fight twice."
    Assert-Equal ([int64]$replay.receipt.stateRevision) ([int64]$fight.receipt.stateRevision) "Replay should return the original action receipt revision."

    $reset = Invoke-Json -Method "POST" -Path "/dev/player/reset" -Headers $authHeaders
    Assert-Equal $reset.status "ok" "Dev player reset should return ok."
    Assert-Equal $reset.playerId $login.playerId "Dev player reset should target the logged-in player."
    Assert-Equal ([int]$reset.playerSnapshot.state.campaignStage) 1 "Dev player reset should return fresh campaign progress."
    Assert-Equal ([int64]$reset.playerSnapshot.revision) 1 "Dev player reset should return a fresh state revision."

    $stateAfterReset = Invoke-Json -Path "/player/state" -Headers $authHeaders
    Assert-Equal ([int]$stateAfterReset.state.campaignStage) 1 "State after dev reset should stay fresh through the active session."

    $fightAfterReset = Invoke-Json -Method "POST" -Path "/campaign/fight" -Headers $actionHeaders
    if ($fightAfterReset.replay) {
        throw "Expected old idempotency key to be cleared by dev reset."
    }
    Assert-Equal ([int]$fightAfterReset.playerState.campaignStage) 2 "Campaign fight after dev reset should apply from fresh stage one."

    $logout = Invoke-Json -Method "POST" -Path "/auth/logout" -Headers $authHeaders
    Assert-Equal $logout.status "ok" "Logout should return ok."
    Assert-Equal $logout.stateFlushed $true "Logout should flush the loaded player state."

    $revoked = Invoke-JsonExpectError -Path "/player/state" -Headers @{
        "Authorization" = $authHeaders["Authorization"]
        "X-Request-ID" = "e2e-revoked-session-001"
    } -ExpectedStatus 401
    Assert-Equal $revoked.Body.errorCode "invalid_session" "Revoked session should reject protected state with a structured error code."
    Assert-Equal $revoked.Body.requestId "e2e-revoked-session-001" "Revoked-session errors should preserve the client request id."

    Write-Host "PostgreSQL E2E smoke passed."
    [pscustomobject]@{
        PlayerId = $login.playerId
        SessionPrefix = $login.sessionToken.Substring(0, [Math]::Min(8, $login.sessionToken.Length))
        CampaignStage = [int]$fightAfterReset.playerState.campaignStage
        Replay = $replay.replay
        DevReset = $stateAfterReset.state.campaignStage -eq 1
        LogoutRevoked = $revoked.StatusCode -eq 401
        LogoutStateFlushed = $logout.stateFlushed
        StateWriteMode = $StateWriteMode
        BalanceCatalog = "postgres_snapshot"
    } | Format-List
}
finally {
    Stop-Api $firstProcess
    Stop-Api $secondProcess
    Remove-Item -LiteralPath $apiExe -Force -ErrorAction SilentlyContinue
}
