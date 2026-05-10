param(
    [string]$BaseUrl = "http://localhost:8080",
    [switch]$FlushState,
    [switch]$CheckIdempotency
)

$ErrorActionPreference = "Stop"

function New-IdempotencyHeaders {
    param(
        [string]$Prefix
    )

    return @{ "Idempotency-Key" = "$Prefix-$([guid]::NewGuid().ToString("N"))" }
}

Write-Host "Checking Mythwake API at $BaseUrl"

$health = Invoke-RestMethod "$BaseUrl/health"
Write-Host "Health:"
$health | Format-List

$state = Invoke-RestMethod "$BaseUrl/player/state"
Write-Host "Player State:"
$state | Format-List

$fight = Invoke-RestMethod -Method Post -Headers (New-IdempotencyHeaders "campaign-fight") "$BaseUrl/campaign/fight"
Write-Host "Campaign Fight:"
$fight | Format-List

if ($CheckIdempotency) {
    $key = [guid]::NewGuid().ToString("N")
    $headers = @{ "Idempotency-Key" = $key }
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
    $flush = Invoke-RestMethod -Method Post "$BaseUrl/player/state/flush"
    Write-Host "State Flush:"
    $flush | Format-List
}
