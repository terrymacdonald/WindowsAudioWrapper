#!/usr/bin/env pwsh
# prepare_windowsaudio.ps1 - Prepares the WindowsAudioWrapper project for build/test use.

[CmdletBinding()]
param(
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

Write-Section "WindowsAudioWrapper Prepare Script"
Write-Host "Working directory: $scriptRoot" -ForegroundColor Cyan

if (-not (Test-DotNet10Sdk)) {
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Section "Checking project structure"

$solutionPath = Get-WindowsAudioSolutionPath -Root $scriptRoot
if ($solutionPath) {
    Write-Host "Solution found: $solutionPath" -ForegroundColor Green
} else {
    Write-Host "WARNING: WindowsAudioWrapper.slnx or WindowsAudioWrapper.sln was not found." -ForegroundColor Yellow
    Write-Host "Create one with: dotnet new sln -n WindowsAudioWrapper" -ForegroundColor Gray
}

$expectedProjects = @(
    "WindowsAudioWrapper\WindowsAudioWrapper.csproj",
    "WindowsAudioWrapper.Tests\WindowsAudioWrapper.Tests.csproj",
    "WindowsAudioWrapper.SampleApp\WindowsAudioWrapper.SampleApp.csproj"
)

foreach ($relativePath in $expectedProjects) {
    $path = Join-Path $scriptRoot $relativePath
    if (Test-Path $path) {
        Write-Host "Project found: $relativePath" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Project not found: $relativePath" -ForegroundColor Yellow
    }
}

Write-Section "Checking VERSION file"

$versionFile = Join-Path $scriptRoot "VERSION"
if (-not (Test-Path $versionFile)) {
    Write-Host "VERSION file not found. Creating default VERSION file." -ForegroundColor Yellow
    @(
        "MAJOR=1",
        "MINOR=0"
    ) | Set-Content -Path $versionFile -Encoding UTF8
    Write-Host "Created: $versionFile" -ForegroundColor Green
} else {
    Write-Host "VERSION file found: $versionFile" -ForegroundColor Green
}

if (-not $SkipRestore -and $solutionPath) {
    Write-Section "Restoring NuGet packages"
    & dotnet restore $solutionPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: dotnet restore failed with exit code $LASTEXITCODE" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
    Write-Host "NuGet restore completed successfully." -ForegroundColor Green
}

Write-Section "Prepare complete"
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  - Build: .\build_windowsaudio.ps1" -ForegroundColor Gray
Write-Host "  - Test:  .\test_windowsaudio.ps1" -ForegroundColor Gray
Write-Host "  - Open the solution in Visual Studio: WindowsAudioWrapper.slnx or WindowsAudioWrapper.sln" -ForegroundColor Gray
Write-Host ""
Read-Host "Press Enter to exit"
