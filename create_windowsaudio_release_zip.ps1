# Create a release zip for NVAPIWrapper
param(
    [string]$Configuration = "Release",
    [switch]$IncludeSources
)

$scriptRoot = $PSScriptRoot
if (-not $scriptRoot) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

Set-Location $scriptRoot

# ---------------------------------------------------------------------------
# Versioning (match build_windowsaudio.ps1: MAJOR/MINOR from VERSION, PATCH = git rev-list --count HEAD)
# ---------------------------------------------------------------------------
$versionFile = Join-Path $scriptRoot "VERSION"
$major = 1
$minor = 0
if (Test-Path $versionFile) {
    foreach ($line in Get-Content $versionFile) {
        if ($line -match "^MAJOR=(\d+)") { $major = [int]$matches[1] }
        elseif ($line -match "^MINOR=(\d+)") { $minor = [int]$matches[1] }
    }
}
$patch = 0
try {
    $gitPath = Get-Command git -ErrorAction SilentlyContinue
    if ($gitPath) {
        $commitCount = & git rev-list --count HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $commitCount -match "^\d+$") {
            $patch = [int]$commitCount
        }
    }
} catch {}
$version = "$major.$minor.$patch"

$projectPath = Join-Path $scriptRoot "WindowsAudioWrapper/WindowsAudioWrapper.csproj"
Write-Host "Building WindowsAudioWrapper ($Configuration) version $version..." -ForegroundColor Cyan
dotnet build $projectPath -c $Configuration /p:Version=$version /p:AssemblyVersion=$version /p:FileVersion=$version
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed; aborting packaging." -ForegroundColor Red
    exit $LASTEXITCODE
}

$buildOutput = Join-Path $scriptRoot "WindowsAudioWrapper/bin/$Configuration/net10.0-windows10.0.22000.0"
$assemblyPath = Join-Path $buildOutput "WindowsAudioWrapper.dll"
if (-not (Test-Path $assemblyPath)) {
    Write-Host "Build output not found at $assemblyPath" -ForegroundColor Red
    exit 1
}

$artifactsDir = Join-Path $scriptRoot "release-zip"
New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null
$zipPath = Join-Path $artifactsDir "WindowsAudioWrapper-$version-$Configuration.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath }

$pathsToPack = @()
$pathsToPack += $assemblyPath
$pathsToPack += Join-Path $buildOutput "WindowsAudioWrapper.pdb"
$pathsToPack += Join-Path $buildOutput "WindowsAudioWrapper.deps.json"
$pathsToPack += Join-Path $buildOutput "WindowsAudioWrapper.xml"
$pathsToPack += Join-Path $scriptRoot "LICENSE"
$pathsToPack += Join-Path $scriptRoot "README.md"
$pathsToPack = $pathsToPack | Where-Object { Test-Path $_ }

if ($IncludeSources) {
    $sourceFiles = Get-ChildItem -Path (Join-Path $scriptRoot "WindowsAudioWrapper") -Filter *.cs -File
    $pathsToPack += $sourceFiles.FullName
    $pathsToPack += (Join-Path $scriptRoot "WindowsAudioWrapper/WindowsAudioWrapper.csproj")
}

$pathsToPack = $pathsToPack | Select-Object -Unique

Write-Host "Creating $zipPath..." -ForegroundColor Cyan
Compress-Archive -Path $pathsToPack -DestinationPath $zipPath -Force
Write-Host "Release zip created at $zipPath" -ForegroundColor Green
