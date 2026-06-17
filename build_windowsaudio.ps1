#!/usr/bin/env pwsh
# build_windowsaudio.ps1 - Cleans, restores, and builds the WindowsAudioWrapper solution.

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [switch]$SkipClean,
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

function Write-Section {
    param([string]$Text)
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host $Text -ForegroundColor Cyan
    Write-Host "============================================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Get-ScriptRoot {
    if ($PSScriptRoot) {
        return $PSScriptRoot
    }
    return Split-Path -Parent $MyInvocation.MyCommand.Path
}

function Get-WindowsAudioSolutionPath {
    param([string]$Root)

    $slnxPath = Join-Path $Root "WindowsAudioWrapper.slnx"
    $slnPath = Join-Path $Root "WindowsAudioWrapper.sln"

    if (Test-Path $slnxPath) {
        return $slnxPath
    }

    if (Test-Path $slnPath) {
        return $slnPath
    }

    return $null
}

function Get-ProjectVersion {
    param([string]$Root)

    $versionFile = Join-Path $Root "VERSION"
    $major = "1"
    $minor = "0"

    if (Test-Path $versionFile) {
        $versionContent = Get-Content $versionFile
        foreach ($line in $versionContent) {
            if ($line -match "^MAJOR=(\d+)") {
                $major = $matches[1]
            } elseif ($line -match "^MINOR=(\d+)") {
                $minor = $matches[1]
            }
        }
    } else {
        Write-Host "Warning: VERSION file not found. Using MAJOR=1 and MINOR=0." -ForegroundColor Yellow
    }

    $patch = "0"
    try {
        $gitPath = Get-Command git -ErrorAction SilentlyContinue
        if ($gitPath) {
            $commitCount = & git rev-list --count HEAD 2>&1
            if ($LASTEXITCODE -eq 0 -and $commitCount -match "^\d+$") {
                $patch = $commitCount
            } else {
                Write-Host "Warning: Could not get git commit count. Using PATCH=0." -ForegroundColor Yellow
            }
        } else {
            Write-Host "Warning: git not found in PATH. Using PATCH=0." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Warning: Error reading git commit count: $_" -ForegroundColor Yellow
        Write-Host "Using PATCH=0." -ForegroundColor Yellow
    }

    return "$major.$minor.$patch"
}

function Test-DotNet10Sdk {
    Write-Host "Checking for .NET CLI..." -ForegroundColor Yellow

    $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetPath) {
        Write-Host "ERROR: dotnet CLI not found in PATH" -ForegroundColor Red
        Write-Host "Install the .NET 10.0 SDK from: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Cyan
        return $false
    }

    $dotnetVersion = & dotnet --version 2>&1
    Write-Host ".NET CLI found: $($dotnetPath.Source)" -ForegroundColor Green
    Write-Host ".NET version: $dotnetVersion" -ForegroundColor Green

    Write-Host ""
    Write-Host "Checking for .NET 10.0 SDK..." -ForegroundColor Yellow
    $sdks = & dotnet --list-sdks 2>&1
    $net10Sdk = $sdks | Where-Object { $_ -match "^10\.0\." }

    if (-not $net10Sdk) {
        Write-Host "ERROR: .NET 10.0 SDK not found" -ForegroundColor Red
        Write-Host "Available SDKs:" -ForegroundColor Yellow
        $sdks | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        Write-Host ""
        Write-Host "Install the .NET 10.0 SDK from: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Cyan
        return $false
    }

    Write-Host ".NET 10.0 SDK found:" -ForegroundColor Green
    $net10Sdk | ForEach-Object { Write-Host "  $_" -ForegroundColor Green }
    return $true
}

$scriptRoot = Get-ScriptRoot
Set-Location $scriptRoot

Write-Section "WindowsAudioWrapper Build Script"
Write-Host "Working directory: $scriptRoot" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan

if (-not (Test-DotNet10Sdk)) {
    Read-Host "Press Enter to exit"
    exit 1
}

$solutionPath = Get-WindowsAudioSolutionPath -Root $scriptRoot
if (-not $solutionPath) {
    Write-Host "ERROR: Solution file not found. Expected WindowsAudioWrapper.slnx or WindowsAudioWrapper.sln in $scriptRoot" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "Solution: $solutionPath" -ForegroundColor Green

$version = Get-ProjectVersion -Root $scriptRoot
Write-Host "Version: $version" -ForegroundColor Green

if (-not $SkipRestore) {
    Write-Section "Restoring NuGet packages"
    & dotnet restore $solutionPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Restore failed with exit code $LASTEXITCODE" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
    Write-Host "NuGet packages restored successfully." -ForegroundColor Green
}

if (-not $SkipClean) {
    Write-Section "Cleaning solution"
    & dotnet clean $solutionPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Clean failed with exit code $LASTEXITCODE" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
    Write-Host "Clean completed successfully." -ForegroundColor Green
}

Write-Section "Building solution"
& dotnet build $solutionPath --configuration $Configuration --no-restore /p:Version=$version /p:AssemblyVersion=$version /p:FileVersion=$version
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  - Ensure .NET 10.0 SDK is installed" -ForegroundColor Gray
    Write-Host "  - Run .\prepare_windowsaudio.ps1" -ForegroundColor Gray
    Write-Host "  - Check project references and NuGet packages" -ForegroundColor Gray
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Section "Build successful"
Write-Host "Projects built:" -ForegroundColor Cyan
Write-Host "  - WindowsAudioWrapper" -ForegroundColor Green
Write-Host "  - WindowsAudioWrapper.Tests" -ForegroundColor Green
Write-Host "  - WindowsAudioWrapper.SampleApp" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  - Run tests: .\test_windowsaudio.ps1" -ForegroundColor Gray
Write-Host "  - Use in another project: add a reference to WindowsAudioWrapper\WindowsAudioWrapper.csproj" -ForegroundColor Gray
Write-Host ""
Read-Host "Press Enter to exit"
