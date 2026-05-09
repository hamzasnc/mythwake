param(
    [string]$BaseUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking Mythwake API at $BaseUrl"

$health = Invoke-RestMethod "$BaseUrl/health"
Write-Host "Health:"
$health | Format-List

$state = Invoke-RestMethod "$BaseUrl/player/state"
Write-Host "Player State:"
$state | Format-List

$fight = Invoke-RestMethod -Method Post "$BaseUrl/campaign/fight"
Write-Host "Campaign Fight:"
$fight | Format-List
