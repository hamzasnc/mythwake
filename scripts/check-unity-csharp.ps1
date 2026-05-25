param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
    [string]$ProjectPath = "",
    [switch]$RuntimeOnly,
    [switch]$EditorOnly
)

$ErrorActionPreference = "Stop"

if ($RuntimeOnly -and $EditorOnly) {
    throw "Choose either -RuntimeOnly or -EditorOnly, not both."
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity executable not found: $UnityPath"
}

$unityEditorDirectory = Split-Path -Parent $UnityPath
$frameworkPath = Join-Path $unityEditorDirectory "Data\MonoBleedingEdge\lib\mono\xbuild-frameworks\.NETFramework\v4.7.1"
if (-not (Test-Path -LiteralPath $frameworkPath)) {
    throw "Unity .NET Framework reference path not found: $frameworkPath"
}

function Invoke-UnityCSharpBuild {
    param(
        [string]$ProjectFile,
        [string]$Label
    )

    $projectFilePath = Join-Path $ProjectPath $ProjectFile
    if (-not (Test-Path -LiteralPath $projectFilePath)) {
        throw "$Label project file not found: $projectFilePath"
    }

    Write-Host "Building $Label C# project"
    $arguments = @(
        "msbuild",
        $projectFilePath,
        "/p:FrameworkPathOverride=$frameworkPath",
        "/p:LangVersion=latest",
        "/v:minimal"
    )

    & dotnet @arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "$Label C# build failed with exit code $exitCode."
    }
}

Write-Host "Unity C# validation"
Write-Host "Project: $ProjectPath"
Write-Host "Unity:   $UnityPath"
Write-Host "Refs:    $frameworkPath"

if (-not $EditorOnly) {
    Invoke-UnityCSharpBuild -ProjectFile "Assembly-CSharp.csproj" -Label "Runtime"
}

if (-not $RuntimeOnly) {
    Invoke-UnityCSharpBuild -ProjectFile "Assembly-CSharp-Editor.csproj" -Label "Editor"
}

Write-Host "Unity C# validation passed."
