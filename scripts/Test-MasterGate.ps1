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
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$fail = $false

Write-Host "== NodeRadar Pro Master Gate ==" -ForegroundColor Cyan

if (-not $SkipBuild) {
    Write-Host "[1/3] Release build (NodeRadarPro.slnx)..." -ForegroundColor Yellow
    Push-Location $root
    try {
        & dotnet build NodeRadarPro.slnx --configuration Release --nologo
        if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[1/3] BUILD FAILED" -ForegroundColor Red }
    } finally { Pop-Location }
} else {
    Write-Host "[1/3] Skipped (SkipBuild)" -ForegroundColor DarkGray
}

Write-Host "[2/3] Roslyn format verification (style + analyzers)..." -ForegroundColor Yellow
Push-Location $root
try {
    & dotnet format NodeRadarPro.slnx --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[2/3] FORMAT VIOLATIONS - run 'dotnet format NodeRadarPro.slnx' and re-verify" -ForegroundColor Red }
} finally { Pop-Location }

Write-Host "[3/3] NuGet package vulnerability check..." -ForegroundColor Yellow
Push-Location $root
try {
    $out = dotnet list NodeRadarPro.slnx package --vulnerable --include-transitive 2>&1
    if ($LASTEXITCODE -ne 0) {
        $fail = $true
        Write-Host "[3/3] NUGET VULNERABILITY CHECK COMMAND FAILED (exit code $LASTEXITCODE):" -ForegroundColor Red
        Write-Host ($out -join "`n") -ForegroundColor Red
    } elseif ($out -match "has the following vulnerable packages") {
        $fail = $true
        Write-Host "[3/3] VULNERABLE NUGET PACKAGES DETECTED:" -ForegroundColor Red
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
