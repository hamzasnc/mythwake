param(
    [string]$BaseUrl = "http://localhost:8080",
    [switch]$FlushState,
    [switch]$CheckIdempotency,
    [switch]$CheckUnauthorized,
    [switch]$CheckLogout
)

$ErrorActionPreference = "Stop"

function New-IdempotencyHeaders {
    param(
        [string]$Prefix
    )

    return @{ "Idempotency-Key" = "$Prefix-$([guid]::NewGuid().ToString("N"))" }
}

Write-Host "Checking Mythwake API at $BaseUrl"

$healthResponse = Invoke-WebRequest -UseBasicParsing "$BaseUrl/health"
$requestId = $healthResponse.Headers["X-Request-ID"]
if ([string]::IsNullOrWhiteSpace($requestId)) {
    throw "Expected X-Request-ID header on health response."
}
$health = $healthResponse.Content | ConvertFrom-Json
Write-Host "Health:"
$health | Format-List
Write-Host "Request ID: $requestId"

$timeResponse = Invoke-WebRequest -UseBasicParsing "$BaseUrl/time"
$timeRequestId = $timeResponse.Headers["X-Request-ID"]
if ([string]::IsNullOrWhiteSpace($timeRequestId)) {
    throw "Expected X-Request-ID header on time response."
}
$serverTime = $timeResponse.Content | ConvertFrom-Json
Write-Host "Server Clock:"
$serverTime | Format-List

if ($CheckUnauthorized) {
    $unauthorizedStatus = $null
    try {
        Invoke-RestMethod "$BaseUrl/player/state" | Out-Null
    }
    catch {
        if ($_.Exception.Response) {
            $unauthorizedStatus = [int]$_.Exception.Response.StatusCode
        }
        else {
            throw
        }
    }

    Write-Host "Unauthorized State Check:"
    [pscustomobject]@{
        Expected = 401
        Actual = $unauthorizedStatus
        Passed = $unauthorizedStatus -eq 401
    } | Format-List
}

$login = Invoke-RestMethod -Method Post "$BaseUrl/auth/guest"
$sessionHeaders = @{ "Authorization" = "Bearer $($login.sessionToken)" }
Write-Host "Guest Login:"
[pscustomobject]@{
    PlayerId = $login.playerId
    SessionPrefix = if ($login.sessionToken.Length -ge 8) { $login.sessionToken.Substring(0, 8) } else { $login.sessionToken }
    Heroes = @($login.playerSnapshot.heroes).Count
} | Format-List

$state = Invoke-RestMethod -Headers $sessionHeaders "$BaseUrl/player/state"
Write-Host "Player State:"
$state | Format-List

$fightHeaders = New-IdempotencyHeaders "campaign-fight"
foreach ($key in $sessionHeaders.Keys) {
    $fightHeaders[$key] = $sessionHeaders[$key]
}
$fight = Invoke-RestMethod -Method Post -Headers $fightHeaders "$BaseUrl/campaign/fight"
Write-Host "Campaign Fight:"
$fight | Format-List

if ($CheckIdempotency) {
    $key = [guid]::NewGuid().ToString("N")
    $headers = @{
        "Idempotency-Key" = $key
        "Authorization" = $sessionHeaders["Authorization"]
    }
    $first = Invoke-RestMethod -Method Post -Headers $headers "$BaseUrl/heroes/hero_astra/level-up"
    $second = Invoke-RestMethod -Method Post -Headers $headers "$BaseUrl/heroes/hero_astra/level-up"
    Write-Host "Idempotency Check:"
    [pscustomobject]@{
        Key = $key
        FirstSuccess = $first.success
        FirstAction = $first.actionId
        SecondSuccess = $second.success
        SecondReplay = $second.replay
        SecondAction = $second.actionId
    } | Format-List
}

if ($FlushState) {
    $flush = Invoke-RestMethod -Method Post -Headers $sessionHeaders "$BaseUrl/player/state/flush"
    Write-Host "State Flush:"
    $flush | Format-List
}

if ($CheckLogout) {
    $logout = Invoke-RestMethod -Method Post -Headers $sessionHeaders "$BaseUrl/auth/logout"

    $revokedStatus = $null
    try {
        Invoke-RestMethod -Headers $sessionHeaders "$BaseUrl/player/state" | Out-Null
    }
    catch {
        if ($_.Exception.Response) {
            $revokedStatus = [int]$_.Exception.Response.StatusCode
        }
        else {
            throw
        }
    }

    Write-Host "Logout Revoke Check:"
    [pscustomobject]@{
        LogoutStatus = $logout.status
        PlayerId = $logout.playerId
        StateFlushed = $logout.stateFlushed
        ExpectedAfterLogout = 401
        ActualAfterLogout = $revokedStatus
        Passed = $revokedStatus -eq 401
    } | Format-List
}
