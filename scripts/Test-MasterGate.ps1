#requires -Version 5.1
<#
.SYNOPSIS
    Local master-branch quality gate for NodeRadar Pro agents and developers.
    Runs: Release build (TreatWarningsAsErrors) -> [optional] unit tests -> [optional] code coverage -> Roslyn/style format verification -> NuGet vulnerability check.
    Exit code 0 = safe to push to master, 1+ = do NOT push; fix reported findings first.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1
    powershell -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1 -WithTests
    powershell -ExecutionPolicy Bypass -File scripts/Test-MasterGate.ps1 -CheckCoverage
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$WithTests,
    [switch]$CheckCoverage
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$fail = $false

if ($CheckCoverage) {
    $WithTests = $true
}

$step = 1
$totalSteps = 3
if ($WithTests) { $totalSteps++ }
if ($CheckCoverage) { $totalSteps++ }

Write-Host "== NodeRadar Pro Master Gate ==" -ForegroundColor Cyan

if (-not $SkipBuild) {
    Write-Host "[$step/$totalSteps] Release build (NodeRadarPro.slnx)..." -ForegroundColor Yellow
    Push-Location $root
    try {
        & dotnet build NodeRadarPro.slnx --configuration Release --nologo
        if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[$step/$totalSteps] BUILD FAILED" -ForegroundColor Red }
    } finally { Pop-Location }
} else {
    Write-Host "[$step/$totalSteps] Skipped (SkipBuild)" -ForegroundColor DarkGray
}
$step++

$coverageDir = Join-Path $root "TestResults"

if ($WithTests) {
    Write-Host "[$step/$totalSteps] Running unit and integration tests (xUnit v3)..." -ForegroundColor Yellow
    Push-Location $root
    try {
        $testArgs = @("test", "NodeRadarPro.slnx", "--configuration", "Release", "--nologo")
        if (-not $SkipBuild) { $testArgs += "--no-build" }
        if ($CheckCoverage) {
            if (Test-Path $coverageDir) { Remove-Item $coverageDir -Recurse -Force -ErrorAction SilentlyContinue }
            $testArgs += @('--collect:"XPlat Code Coverage"', "--results-directory", $coverageDir)
        }
        & dotnet @testArgs
        if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[$step/$totalSteps] TEST SUITE FAILED" -ForegroundColor Red }
    } finally { Pop-Location }
    $step++
}

if ($CheckCoverage) {
    Write-Host "[$step/$totalSteps] Evaluating code coverage against threshold..." -ForegroundColor Yellow
    Push-Location $root
    try {
        $thresholdFile = Join-Path (Join-Path $root "scripts") "coverage-threshold.txt"
        $threshold = 50.0
        if (Test-Path $thresholdFile) {
            $raw = (Get-Content $thresholdFile -Raw).Trim()
            if ($raw -match '^\d+(\.\d+)?$') { $threshold = [double]$raw }
        }

        $coverageScript = Join-Path (Join-Path $root "scripts") "Get-CoverageRate.ps1"
        if (Test-Path $coverageScript) {
            $rate = & powershell -NoProfile -ExecutionPolicy Bypass -File $coverageScript -Path $coverageDir 2>&1
            if ($rate -match '^\d+(\.\d+)?$') {
                $rateNum = [double]$rate
                Write-Host "[$step/$totalSteps] Coverage: ${rateNum}% (threshold: ${threshold}%)" -ForegroundColor Cyan
                if ($rateNum -lt $threshold) {
                    $fail = $true
                    Write-Host "[$step/$totalSteps] COVERAGE BELOW THRESHOLD: ${rateNum}% < ${threshold}%" -ForegroundColor Red
                }
            } else {
                $fail = $true
                Write-Host "[$step/$totalSteps] ERROR: Could not parse coverage rate: $rate" -ForegroundColor Red
            }
        } else {
            Write-Host "[$step/$totalSteps] WARNING: Get-CoverageRate.ps1 not found - skipping coverage check" -ForegroundColor Yellow
        }
    } finally { Pop-Location }
    $step++
}

Write-Host "[$step/$totalSteps] Roslyn format verification (style + analyzers)..." -ForegroundColor Yellow
Push-Location $root
try {
    & dotnet format NodeRadarPro.slnx --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[$step/$totalSteps] FORMAT VIOLATIONS - run 'dotnet format NodeRadarPro.slnx' and re-verify" -ForegroundColor Red }
} finally { Pop-Location }
$step++

Write-Host "[$step/$totalSteps] NuGet package vulnerability check..." -ForegroundColor Yellow
Push-Location $root
try {
    $out = dotnet list NodeRadarPro.slnx package --vulnerable --include-transitive 2>&1
    if ($LASTEXITCODE -ne 0) {
        $fail = $true
        Write-Host "[$step/$totalSteps] NUGET VULNERABILITY CHECK COMMAND FAILED (exit code $LASTEXITCODE):" -ForegroundColor Red
        Write-Host ($out -join "`n") -ForegroundColor Red
    } elseif ($out -match "has the following vulnerable packages") {
        $fail = $true
        Write-Host "[$step/$totalSteps] VULNERABLE NUGET PACKAGES DETECTED:" -ForegroundColor Red
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
