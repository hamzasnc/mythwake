param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
    [string]$ProjectPath = "",
    [string]$OutputDirectory = "",
    [string]$LogFile = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $LogFile = Join-Path $ProjectPath "Temp\portrait-screenshots.log"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $ProjectPath "Temp\android-fallback-screenshots"
}

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity executable not found: $UnityPath"
}

$logDirectory = Split-Path -Parent $LogFile
if (-not [string]::IsNullOrWhiteSpace($logDirectory) -and -not (Test-Path -LiteralPath $logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory | Out-Null
}

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$arguments = @(
    "-batchmode",
    "-quit",
    "-projectPath",
    $ProjectPath,
    "-executeMethod",
    "PortraitScreenshotAutomation.CapturePortraitScreenshotSet",
    "-logFile",
    $LogFile,
    "-mythwakeScreenshotOutput",
    $OutputDirectory
)

Write-Host "Unity portrait screenshot capture"
Write-Host "Project: $ProjectPath"
Write-Host "Unity:   $UnityPath"
Write-Host "Output:  $OutputDirectory"
Write-Host "Log:     $LogFile"

if ($DryRun) {
    Write-Host "Dry run only. Command:"
    Write-Host "`"$UnityPath`" $($arguments -join ' ')"
    exit 0
}

& $UnityPath @arguments 2>&1 | Tee-Object -Variable unityOutput
$exitCode = $LASTEXITCODE
$unityText = $unityOutput -join "`n"

if ($exitCode -ne 0 -or $unityText.Contains("Aborting batchmode due to fatal error") -or $unityText.Contains("Multiple Unity instances cannot open the same project")) {
    Write-Host "Unity screenshot capture failed with exit code $exitCode."
    if (Test-Path -LiteralPath $LogFile) {
        Write-Host "Last log lines:"
        Get-Content -Path $LogFile -Tail 120
    }

    throw "Unity portrait screenshot capture failed. Check the log above."
}

if (Test-Path -LiteralPath $LogFile) {
    $fatalLog = Select-String -Path $LogFile -Pattern "Exception:|Aborting batchmode due to fatal error|Multiple Unity instances cannot open the same project"
    if ($fatalLog) {
        Write-Host "Unity screenshot log contains an exception."
        Get-Content -Path $LogFile -Tail 120
        throw "Unity portrait screenshot capture failed. Check the log above."
    }
}

Write-Host "Unity portrait screenshot capture passed."
