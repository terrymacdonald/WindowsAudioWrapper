#!/usr/bin/env pwsh
# test_windowsaudio.ps1 - Builds and runs the WindowsAudioWrapper xUnit tests.

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$Filter = "",

    [switch]$NoBuild
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
    }

    $patch = "0"
    try {
        $gitPath = Get-Command git -ErrorAction SilentlyContinue
        if ($gitPath) {
            $commitCount = & git rev-list --count HEAD 2>&1
            if ($LASTEXITCODE -eq 0 -and $commitCount -match "^\d+$") {
                $patch = $commitCount
            }
        }
    } catch {
        $patch = "0"
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

Write-Section "WindowsAudioWrapper Test Script"
Write-Host "Working directory: $scriptRoot" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan

if (-not (Test-DotNet10Sdk)) {
    Read-Host "Press Enter to exit"
    exit 1
}

$testProjectPath = Join-Path $scriptRoot "WindowsAudioWrapper.Tests\WindowsAudioWrapper.Tests.csproj"
if (-not (Test-Path $testProjectPath)) {
    Write-Host "ERROR: Test project not found at: $testProjectPath" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "Test project found: $testProjectPath" -ForegroundColor Green

$version = Get-ProjectVersion -Root $scriptRoot
Write-Host "Testing version: $version" -ForegroundColor Green

Write-Section "Running xUnit tests"

$testArgs = @(
    "test",
    $testProjectPath,
    "--configuration",
    $Configuration,
    "--verbosity",
    "normal",
    "/p:Version=$version",
    "/p:AssemblyVersion=$version",
    "/p:FileVersion=$version"
)

if ($NoBuild) {
    $testArgs += "--no-build"
}

if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $testArgs += @("--filter", $Filter)
}

& dotnet @testArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Section "All tests passed"
Write-Host "WindowsAudioWrapper.Tests completed successfully." -ForegroundColor Green
Write-Host ""
Read-Host "Press Enter to exit"
