param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
    [string]$ProjectPath = "",
    [string]$OutputPath = "",
    [string]$BackendBaseUrl = "",
    [string]$LogFile = "",
    [switch]$AppBundle,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $LogFile = Join-Path $ProjectPath "Temp\android-build.log"
}

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity executable not found: $UnityPath"
}

$logDirectory = Split-Path -Parent $LogFile
if (-not [string]::IsNullOrWhiteSpace($logDirectory) -and -not (Test-Path -LiteralPath $logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory | Out-Null
}

$arguments = @(
    "-batchmode",
    "-quit",
    "-projectPath",
    $ProjectPath,
    "-buildTarget",
    "Android",
    "-executeMethod",
    "AndroidBuildAutomation.BuildAndroidApk",
    "-logFile",
    $LogFile
)

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $arguments += @("-mythwakeAndroidOutput", $OutputPath)
}

if ($AppBundle) {
    $arguments += @("-mythwakeAndroidArtifact", "aab")
}

if (-not [string]::IsNullOrWhiteSpace($BackendBaseUrl)) {
    $arguments += @("-mythwakeBackendBaseUrl", $BackendBaseUrl)
}

$artifactLabel = if ($AppBundle) { "AAB" } else { "APK" }

Write-Host "Unity Android $artifactLabel build"
Write-Host "Project: $ProjectPath"
Write-Host "Unity:   $UnityPath"
Write-Host "Log:     $LogFile"
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    Write-Host "Output:  $OutputPath"
}
if (-not [string]::IsNullOrWhiteSpace($BackendBaseUrl)) {
    Write-Host "Backend: $BackendBaseUrl"
}

if ($DryRun) {
    Write-Host "Dry run only. Command:"
    Write-Host "`"$UnityPath`" $($arguments -join ' ')"
    exit 0
}

& $UnityPath @arguments 2>&1 | Tee-Object -Variable unityOutput
$exitCode = $LASTEXITCODE
$unityText = $unityOutput -join "`n"

if ($exitCode -ne 0 -or $unityText.Contains("Aborting batchmode due to fatal error") -or $unityText.Contains("Multiple Unity instances cannot open the same project")) {
    Write-Host "Unity Android $artifactLabel build failed with exit code $exitCode."
    if (Test-Path -LiteralPath $LogFile) {
        Write-Host "Last log lines:"
        Get-Content -Path $LogFile -Tail 120
    }

    throw "Unity Android $artifactLabel build failed. Check the build log above."
}

if (Test-Path -LiteralPath $LogFile) {
    $fatalLog = Select-String -Path $LogFile -Pattern "BuildFailedException|Android build failed|Exception:|Aborting batchmode due to fatal error|Multiple Unity instances cannot open the same project"
    if ($fatalLog) {
        Write-Host "Unity Android build log contains a fatal error or exception."
        Get-Content -Path $LogFile -Tail 120
        throw "Unity Android $artifactLabel build failed. Check the build log above."
    }
}

Write-Host "Unity Android $artifactLabel build passed."
