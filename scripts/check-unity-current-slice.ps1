param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
    [string]$ProjectPath = "",
    [string]$LogFile = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $LogFile = Join-Path $ProjectPath "Temp\current-slice-validation.log"
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
    "-executeMethod",
    "CurrentSliceValidation.RunCurrentSliceValidation",
    "-logFile",
    $LogFile
)

Write-Host "Unity current slice validation"
Write-Host "Project: $ProjectPath"
Write-Host "Unity:   $UnityPath"
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
    Write-Host "Unity validation failed with exit code $exitCode."
    if (Test-Path -LiteralPath $LogFile) {
        Write-Host "Last log lines:"
        Get-Content -Path $LogFile -Tail 80
    }

    throw "Unity current slice validation failed. Close any open Unity instance for this project and retry."
}

if (Test-Path -LiteralPath $LogFile) {
    $fatalLog = Select-String -Path $LogFile -Pattern "Aborting batchmode due to fatal error|Multiple Unity instances cannot open the same project|Exception:"
    if ($fatalLog) {
        Write-Host "Unity validation log contains a fatal error or exception."
        Get-Content -Path $LogFile -Tail 80
        throw "Unity current slice validation failed. Close any open Unity instance for this project and retry."
    }
}

Write-Host "Unity current slice validation passed."
