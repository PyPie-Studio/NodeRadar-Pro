#requires -Version 5.1
<#
.SYNOPSIS
    Local master-branch quality gate for NodeRadar Pro agents and developers.
    Runs: Release build (TreatWarningsAsErrors) -> Roslyn/style format verification -> NuGet vulnerability check.
    Exit code 0 = safe to push to master, 1+ = do NOT push; fix reported findings first.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$WithTests
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$fail = $false
$totalSteps = if ($WithTests) { 4 } else { 3 }

Write-Host "== NodeRadar Pro Master Gate ==" -ForegroundColor Cyan

if (-not $SkipBuild) {
    Write-Host "[1/$totalSteps] Release build (NodeRadarPro.slnx)..." -ForegroundColor Yellow
    Push-Location $root
    try {
        & dotnet build NodeRadarPro.slnx --configuration Release --nologo
        if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[1/$totalSteps] BUILD FAILED" -ForegroundColor Red }
    } finally { Pop-Location }
} else {
    Write-Host "[1/$totalSteps] Skipped (SkipBuild)" -ForegroundColor DarkGray
}

if ($WithTests) {
    Write-Host "[2/$totalSteps] Running unit and integration tests (xUnit v3)..." -ForegroundColor Yellow
    Push-Location $root
    try {
        if (-not $SkipBuild) {
            & dotnet test NodeRadarPro.slnx --configuration Release --no-build --nologo
        } else {
            & dotnet test NodeRadarPro.slnx --configuration Release --nologo
        }
        if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[2/$totalSteps] TEST SUITE FAILED" -ForegroundColor Red }
    } finally { Pop-Location }
}

$formatStep = if ($WithTests) { 3 } else { 2 }
Write-Host "[$formatStep/$totalSteps] Roslyn format verification (style + analyzers)..." -ForegroundColor Yellow
Push-Location $root
try {
    & dotnet format NodeRadarPro.slnx --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[$formatStep/$totalSteps] FORMAT VIOLATIONS - run 'dotnet format NodeRadarPro.slnx' and re-verify" -ForegroundColor Red }
} finally { Pop-Location }

$vulnStep = if ($WithTests) { 4 } else { 3 }
Write-Host "[$vulnStep/$totalSteps] NuGet package vulnerability check..." -ForegroundColor Yellow
Push-Location $root
try {
    $out = dotnet list NodeRadarPro.slnx package --vulnerable --include-transitive 2>&1
    if ($LASTEXITCODE -ne 0) {
        $fail = $true
        Write-Host "[$vulnStep/$totalSteps] NUGET VULNERABILITY CHECK COMMAND FAILED (exit code $LASTEXITCODE):" -ForegroundColor Red
        Write-Host ($out -join "`n") -ForegroundColor Red
    } elseif ($out -match "has the following vulnerable packages") {
        $fail = $true
        Write-Host "[$vulnStep/$totalSteps] VULNERABLE NUGET PACKAGES DETECTED:" -ForegroundColor Red
        Write-Host ($out -join "`n") -ForegroundColor Red
    }
} finally { Pop-Location }

$sw.Stop()
if ($fail) {
    Write-Host "== GATE BLOCKED ($([math]::Round($sw.Elapsed.TotalSeconds, 1))s) - fix the findings above before pushing ==" -ForegroundColor Red
    exit 1
}
Write-Host "== GATE PASSED ($([math]::Round($sw.Elapsed.TotalSeconds, 1))s) - safe to push ==" -ForegroundColor Green
exit 0
