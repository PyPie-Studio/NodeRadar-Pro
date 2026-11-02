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
    [switch]$CheckCoverage,
    [string]$TestResultsDirectory = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$fail = $false

if ($CheckCoverage) {
    $WithTests = $true
}

$step = 1
$totalSteps = 4
if ($WithTests) { $totalSteps++ }
if ($CheckCoverage) { $totalSteps++ }

Write-Host "== NodeRadar Pro Master Gate ==" -ForegroundColor Cyan

function Start-GateStep([string]$title) {
    if ($env:GITHUB_ACTIONS -eq "true") {
        Write-Host "::group::$title"
    }
    Write-Host $title -ForegroundColor Yellow
}

function End-GateStep {
    if ($env:GITHUB_ACTIONS -eq "true") {
        Write-Host "::endgroup::"
    }
}

if (-not $SkipBuild) {
    Start-GateStep "[$step/$totalSteps] Release build (NodeRadarPro.slnx)..."
    Push-Location $root
    try {
        & dotnet build NodeRadarPro.slnx --configuration Release --nologo
        if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[$step/$totalSteps] BUILD FAILED" -ForegroundColor Red }
    } finally {
        Pop-Location
        End-GateStep
    }
} else {
    Write-Host "[$step/$totalSteps] Skipped (SkipBuild)" -ForegroundColor DarkGray
}
$step++

Start-GateStep "[$step/$totalSteps] Application startup smoke verification..."
Push-Location $root
try {
    & dotnet run --project "src\NodeRadarPro\NodeRadarPro.csproj" --no-build -c Release -- --smoke-test
    if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[$step/$totalSteps] STARTUP SMOKE TEST FAILED" -ForegroundColor Red }
} finally {
    Pop-Location
    End-GateStep
}
$step++

$coverageDir = if ($TestResultsDirectory) {
    if ([System.IO.Path]::IsPathRooted($TestResultsDirectory)) {
        $TestResultsDirectory
    } else {
        Join-Path $root $TestResultsDirectory
    }
} else {
    Join-Path $root "artifacts\test-results"
}

if ($WithTests) {
    Start-GateStep "[$step/$totalSteps] Running unit and integration tests (xUnit v3)..."
    Push-Location $root
    try {
        $testArgs = @("test", "NodeRadarPro.slnx", "--configuration", "Release", "--nologo")
        if (-not $SkipBuild) { $testArgs += "--no-build" }
        $runsettingsPath = Join-Path $root "test.runsettings"
        if (Test-Path $runsettingsPath) {
            $testArgs += @("--settings", $runsettingsPath)
        }
        $resolvedResultsDir = $null
        if ($TestResultsDirectory) {
            $resolvedResultsDir = $coverageDir
            if (-not (Test-Path $resolvedResultsDir)) {
                New-Item -ItemType Directory -Path $resolvedResultsDir -Force | Out-Null
            }
            $trxFile = Join-Path $resolvedResultsDir "test-results.trx"
            if (Test-Path $trxFile) { Remove-Item $trxFile -Force -ErrorAction SilentlyContinue }
            $testArgs += @("--logger", "trx;LogFileName=test-results.trx")
        }
        if ($CheckCoverage) {
            if (-not $resolvedResultsDir) {
                $resolvedResultsDir = $coverageDir
            }
            if (Test-Path $resolvedResultsDir) {
                Get-ChildItem -Path $resolvedResultsDir -Filter "*.cobertura.xml" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
            }
            $testArgs += @('--collect:"XPlat Code Coverage"')
        }
        if ($resolvedResultsDir) {
            $testArgs += @("--results-directory", $resolvedResultsDir)
        }
        & dotnet @testArgs
        if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[$step/$totalSteps] TEST SUITE FAILED" -ForegroundColor Red }
    } finally {
        Pop-Location
        End-GateStep
    }
    $step++
}

if ($CheckCoverage) {
    Start-GateStep "[$step/$totalSteps] Evaluating code coverage against threshold..."
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
            $rateOutput = (& powershell -NoProfile -ExecutionPolicy Bypass -File $coverageScript -Path $coverageDir 2>&1 | Out-String).Trim()
            if ($LASTEXITCODE -eq 0 -and $rateOutput -match '\b(\d+(\.\d+)?)\b') {
                $rateNum = [double]$matches[1]
                Write-Host "[$step/$totalSteps] Coverage: ${rateNum}% (threshold: ${threshold}%)" -ForegroundColor Cyan
                if ($rateNum -lt $threshold) {
                    $fail = $true
                    Write-Host "[$step/$totalSteps] COVERAGE BELOW THRESHOLD: ${rateNum}% < ${threshold}%" -ForegroundColor Red
                }
            } else {
                $fail = $true
                Write-Host "[$step/$totalSteps] ERROR: Could not parse coverage rate: $rateOutput" -ForegroundColor Red
            }
        } else {
            Write-Host "[$step/$totalSteps] WARNING: Get-CoverageRate.ps1 not found - skipping coverage check" -ForegroundColor Yellow
        }
    } finally {
        Pop-Location
        End-GateStep
    }
    $step++
}

Start-GateStep "[$step/$totalSteps] Roslyn format verification (style + analyzers)..."
Push-Location $root
try {
    & dotnet format NodeRadarPro.slnx --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { $fail = $true; Write-Host "[$step/$totalSteps] FORMAT VIOLATIONS - run 'dotnet format NodeRadarPro.slnx' and re-verify" -ForegroundColor Red }
} finally {
    Pop-Location
    End-GateStep
}
$step++

Start-GateStep "[$step/$totalSteps] NuGet package vulnerability check..."
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
} finally {
    Pop-Location
    End-GateStep
}

$sw.Stop()
if ($fail) {
    Write-Host "== GATE BLOCKED ($([math]::Round($sw.Elapsed.TotalSeconds, 1))s) - fix the findings above before pushing ==" -ForegroundColor Red
    exit 1
}
Write-Host "== GATE PASSED ($([math]::Round($sw.Elapsed.TotalSeconds, 1))s) - safe to push ==" -ForegroundColor Green
exit 0
