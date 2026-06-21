#!/usr/bin/env pwsh
# build_samples.ps1 - Builds the WindowsAudioWrapper Sample Application

# Determine script root
$scriptRoot = $PSScriptRoot
if (-not $scriptRoot) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

Set-Location $scriptRoot
Write-Host "Working directory: $scriptRoot" -ForegroundColor Cyan
Write-Host ""

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "WindowsAudioWrapper SampleApp Build" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# -----------------------------------------------------------------------------
# Prerequisites
# -----------------------------------------------------------------------------
Write-Host "Checking for .NET CLI..." -ForegroundColor Yellow
try {
    $dotnetPath = Get-Command dotnet -ErrorAction Stop
    $dotnetVersion = & dotnet --version 2>&1
    Write-Host ".NET CLI found: $($dotnetPath.Source)" -ForegroundColor Green
    Write-Host ".NET version: $dotnetVersion" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "ERROR: dotnet CLI not found in PATH" -ForegroundColor Red
    Write-Host "Please install .NET 10.0 SDK from https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Cyan
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Checking for .NET 10.0 SDK..." -ForegroundColor Yellow
try {
    $sdks = & dotnet --list-sdks 2>&1
    $net10Sdk = $sdks | Where-Object { $_ -match "^10\.0\." }
    if (-not $net10Sdk) {
        Write-Host "ERROR: .NET 10.0 SDK not found." -ForegroundColor Red
        Write-Host "Available SDKs:" -ForegroundColor Yellow
        $sdks | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        Read-Host "Press Enter to exit"
        exit 1
    }
    Write-Host ".NET 10.0 SDK found:" -ForegroundColor Green
    $net10Sdk | ForEach-Object { Write-Host "  $_" -ForegroundColor Green }
    Write-Host ""
} catch {
    Write-Host "ERROR: Failed to check .NET SDKs: $_" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# -----------------------------------------------------------------------------
# Restore and build Sample Project
# -----------------------------------------------------------------------------
$sampleProject = Join-Path $scriptRoot "WindowsAudioWrapper.SampleApp\WindowsAudioWrapper.SampleApp.csproj"
if (-not (Test-Path $sampleProject)) {
    Write-Host "ERROR: Sample application project not found at $sampleProject" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Restoring NuGet packages for Sample Application..." -ForegroundColor Cyan
try {
    dotnet restore $sampleProject
    if ($LASTEXITCODE -ne 0) { throw "Restore failed with exit code $LASTEXITCODE" }
    Write-Host "Restore completed successfully." -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "ERROR: Restore failed. $_" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Building Sample Application..." -ForegroundColor Cyan
try {
    dotnet build $sampleProject --configuration Debug --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
    
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Green
    Write-Host "*** SAMPLE APP BUILD SUCCESSFUL! ***" -ForegroundColor Green
    Write-Host "============================================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Executable Binary Location:" -ForegroundColor Cyan
    Write-Host "  - WindowsAudioWrapper.SampleApp/bin/Debug/net10.0-windows10.0.22000.0/WindowsAudioWrapper.SampleApp.exe" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host ""
    Write-Host "ERROR: Sample application build failed. $_" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Read-Host "Press Enter to exit..."