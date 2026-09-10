param(
    [Parameter(Mandatory=$true)][string]$Method,
    [Parameter(Mandatory=$true)][string]$LogName,
    [string]$CaptureDirectory,
    [string]$AndroidOutput,
    [string]$BodyReviewFolder,
    [string]$MotionReviewFolder,
    [switch]$StudyPixelAudit,
    [ValidateSet('primary','all','none')][string]$MotionReviewCapture = 'primary',
    [ValidateRange(128,2048)][int]$MotionReviewSize = 512
)
$ErrorActionPreference = 'Stop'
$kaelProject = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$kaelUnity = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe'
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'An existing Unity process is running. Do not open the project concurrently.' }
$kaelLog = Join-Path $kaelProject ('artifacts\kael\' + $LogName)
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $kaelLog) | Out-Null
$kaelArgs = @('-batchmode','-projectPath',('"' + $kaelProject + '"'),'-executeMethod',$Method,
    '-mythwakeTestProfile',('kael-v2-' + [Guid]::NewGuid().ToString('N')),'-logFile',('"' + $kaelLog + '"'))
if ($CaptureDirectory) {
    $kaelCapture = Join-Path $kaelProject $CaptureDirectory
    if ((Test-Path -LiteralPath $kaelCapture) -and (Get-ChildItem -LiteralPath $kaelCapture)) { throw 'Use an empty, unique capture directory.' }
    $kaelArgs += @('-mythwakeGameplayAcceptance','-mythwakeCaptureFps','30','-mythwakeCaptureDir',('"' + $kaelCapture + '"'))
} else { $kaelArgs += '-quit' }
if ($AndroidOutput) { $kaelArgs += @('-buildTarget','Android','-mythwakeAndroidOutput',('"' + (Join-Path $kaelProject $AndroidOutput) + '"')) }
if ($BodyReviewFolder) {
    if ($BodyReviewFolder -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Body review folder must be a single simple directory name.' }
    $kaelArgs += @('-kaelBodyReviewFolder',$BodyReviewFolder)
}
if ($MotionReviewFolder) {
    if ($MotionReviewFolder -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Motion review folder must be a single simple directory name.' }
    $kaelArgs += @('-kaelMotionReviewFolder',$MotionReviewFolder,
        '-kaelMotionReviewCapture',$MotionReviewCapture,'-kaelMotionReviewSize',[string]$MotionReviewSize)
}
if ($StudyPixelAudit) { $kaelArgs += '-kaelStudyPixelAudit' }
$kaelProcess = Start-Process -FilePath $kaelUnity -ArgumentList $kaelArgs -WorkingDirectory $kaelProject -WindowStyle Hidden -PassThru
try { $kaelProcess.PriorityClass = 'BelowNormal' } catch { Write-Verbose 'Unity exited before priority assignment.' }
Write-Output ('Unity background PID ' + $kaelProcess.Id + '; log ' + $kaelLog)
$kaelProcess.WaitForExit()
$kaelProcess.Refresh()
Write-Output ('Unity exit code: ' + $kaelProcess.ExitCode)
Get-Content -LiteralPath $kaelLog -Tail 18
exit $kaelProcess.ExitCode
