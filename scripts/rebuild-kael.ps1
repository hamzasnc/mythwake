param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe',
    [string]$PythonPath = 'C:\Users\Hamza\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
)
$ErrorActionPreference = 'Stop'
$kaelProject = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$kaelVersionFile = Join-Path $kaelProject 'ProjectSettings\ProjectVersion.txt'
if ((Get-Content -LiteralPath $kaelVersionFile -Raw) -notmatch 'm_EditorVersion: 6000\.4\.5f1') {
    throw 'This exporter is versioned for Unity 6000.4.5f1. Review the pipeline before changing editor version.'
}
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity executable missing: $UnityPath" }
if (-not (Test-Path -LiteralPath $PythonPath)) { throw 'Pass -PythonPath with a Python installation containing Pillow.' }
Push-Location $kaelProject
try {
    foreach ($kaelPreparation in @('ArtSource\Kael\V2\prepare_assets.py', 'ArtSource\Kael\V2\prepare_effects.py')) {
        if (-not (Test-Path -LiteralPath $kaelPreparation)) { throw "Kael V2 source preparation is missing: $kaelPreparation" }
        & $PythonPath $kaelPreparation
        if ($LASTEXITCODE -ne 0) { throw "Kael V2 preprocessing failed: $kaelPreparation" }
    }
    $kaelLogDirectory = Join-Path $kaelProject 'artifacts\kael'
    New-Item -ItemType Directory -Force -Path $kaelLogDirectory | Out-Null
    $kaelLog = Join-Path $kaelLogDirectory 'rebuild-v2.log'
    $kaelTestProfile = 'kael-export-' + [Guid]::NewGuid().ToString('N')
    $kaelArguments = @('-batchmode','-quit','-projectPath',('"' + $kaelProject + '"'),
        '-executeMethod','KaelAssetReview.Run','-mythwakeTestProfile',$kaelTestProfile,
        '-logFile',('"' + $kaelLog + '"'))
    $kaelProcess = Start-Process -FilePath $UnityPath -ArgumentList $kaelArguments -WindowStyle Hidden -PassThru
    try { $kaelProcess.PriorityClass = 'BelowNormal' } catch { Write-Verbose 'Unity exited before its background priority could be set.' }
    $kaelProcess.WaitForExit()
    $kaelProcess.Refresh()
    $kaelOutput = Get-Content -LiteralPath $kaelLog -Raw
    if ($kaelProcess.ExitCode -ne 0 -or $kaelOutput -notmatch 'KAEL_RIG_REVIEW_OK' -or $kaelOutput -match 'Aborting batchmode|Exception:|error CS\d+') {
        throw "Kael rebuild/validation failed. Read $kaelLog"
    }
    Write-Host "Kael rebuild and rig checks passed. Log: $kaelLog"
} finally { Pop-Location }
