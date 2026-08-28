#requires -Version 5.1
<#
.SYNOPSIS
    NodeRadar Pro one-click deployment pipeline: preflight -> quality gate -> OUI sync -> test suite -> release packaging -> SHA-256 telemetry -> changelog & tagging.

.PARAMETER SkipGate
    Skips the Master Quality Gate (for urgent hotfixes).

.PARAMETER SkipOui
    Skips downloading the live IEEE MAC OUI database from Wireshark.

.PARAMETER SkipTests
    Skips test suite execution.

.PARAMETER SkipInno
    Skips Inno Setup installer compilation (produces obfuscated publish binaries only).

.PARAMETER SkipTag
    Skips creating a git deploy tag and updating CHANGELOG.md.

.PARAMETER LogPath
    Custom path for deployment log output.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\Deploy-NodeRadar.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\Deploy-NodeRadar.ps1 -SkipOui -SkipGate
#>
[CmdletBinding()]
param(
    [switch]$SkipGate,
    [switch]$SkipOui,
    [switch]$SkipTests,
    [switch]$SkipInno,
    [switch]$SkipTag,
    [string]$LogPath = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = New-Object System.Collections.Generic.List[object]

if (-not $LogPath) {
    $logDir = Join-Path $root "deploy-logs"
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    }
    $LogPath = Join-Path $logDir "noderadar-deploy-$(Get-Date -Format yyyyMMdd-HHmmss).log"
}

function Write-Step([string]$name, [string]$message) {
    Write-Host "[$name] $message" -ForegroundColor Cyan
    Write-Output "[$(Get-Date -Format HH:mm:ss)] [$name] $message" | Out-File -Append -FilePath $LogPath
}

function Complete-Step([string]$name, [bool]$ok, [string]$detail = "") {
    $results.Add([pscustomobject]@{ Phase = $name; Status = $(if ($ok) { "PASS" } else { "FAIL" }); Detail = $detail })
    if ($ok) { Write-Host "[$name] PASS - $detail" -ForegroundColor Green }
    else { Write-Host "[$name] FAIL - $detail" -ForegroundColor Red }
    Write-Output "[$(Get-Date -Format HH:mm:ss)] [$name] $(if ($ok) {'PASS'} else {'FAIL'}) - $detail" | Out-File -Append -FilePath $LogPath
}

function Fail-Run([string]$phase, [string]$detail) {
    Complete-Step $phase $false $detail
    Write-Host "`n== DEPLOYMENT ABORTED ==" -ForegroundColor Red
    Write-Output "[$(Get-Date -Format HH:mm:ss)] == DEPLOYMENT ABORTED ==" | Out-File -Append -FilePath $LogPath
    exit 1
}

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "   NodeRadar Pro: Phase-Gated Deployment & Release Pipeline" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Log: $LogPath`n" -ForegroundColor DarkGray

Write-Step "0" "Preflight: Git state & SDK validation"
$branch = git rev-parse --abbrev-ref HEAD 2>$null
if (-not $branch) { $branch = "unknown" }
$dirty = git status --porcelain 2>$null
if ($dirty) {
    Write-Host "[0] NOTE: Uncommitted changes in working tree:" -ForegroundColor Yellow
    $dirty | ForEach-Object { Write-Host "      $_" -ForegroundColor Yellow }
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Fail-Run "0" "dotnet SDK not found on system PATH" }
$sdkVer = & dotnet --version
Complete-Step "0" $true "branch=$branch, sdk=$sdkVer"

# Phase 1: Master Quality Gate
if (-not $SkipGate) {
    Write-Step "1" "Master Quality Gate: Release build + style + analyzers + security audit"
    $gateSw = [System.Diagnostics.Stopwatch]::StartNew()
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "scripts\Test-MasterGate.ps1")
    $gateSw.Stop()
    if ($LASTEXITCODE -ne 0) { Fail-Run "1" "Master Quality Gate failed - resolve analyzer/build issues before deploying" }
    Complete-Step "1" $true "gate clean ($([math]::Round($gateSw.Elapsed.TotalSeconds, 1))s)"
} else {
    Write-Step "1" "SKIPPED (SkipGate switch)"
    Complete-Step "1" $true "skipped"
}

# Phase 2: OUI Vendor Sync
if (-not $SkipOui) {
    Write-Step "2" "IEEE MAC OUI Sync: Fetching live manufacturer registry from Wireshark"
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "scripts\Update-OuiDatabase.ps1")
    if ($LASTEXITCODE -ne 0) { Fail-Run "2" "OUI Database update failed" }
    Complete-Step "2" $true "OUI database refreshed"
} else {
    Write-Step "2" "SKIPPED (SkipOui switch)"
    Complete-Step "2" $true "skipped"
}

# Phase 3: Test Suite Verification
if (-not $SkipTests) {
    Write-Step "3" "Automated Test Suite: Running unit & integration tests on xUnit v3"
    $testSw = [System.Diagnostics.Stopwatch]::StartNew()
    & dotnet test (Join-Path $root "NodeRadarPro.slnx") -c Release --nologo
    $testSw.Stop()
    if ($LASTEXITCODE -ne 0) { Fail-Run "3" "Test suite execution failed" }
    Complete-Step "3" $true "all tests passed ($([math]::Round($testSw.Elapsed.TotalSeconds, 1))s)"
} else {
    Write-Step "3" "SKIPPED (SkipTests switch)"
    Complete-Step "3" $true "skipped"
}

# Phase 4: Release Packaging & Obfuscation
Write-Step "4" "Release Packaging: Self-contained publish + Obfuscar IL shield + Inno Setup installer"
$pkgSw = [System.Diagnostics.Stopwatch]::StartNew()
$innoArg = if ($SkipInno) { @("-SkipInno") } else { @() }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "scripts\Build-ReleasePackage.ps1") @innoArg
$pkgSw.Stop()
if ($LASTEXITCODE -ne 0) { Fail-Run "4" "Release packaging failed" }
Complete-Step "4" $true "package built ($([math]::Round($pkgSw.Elapsed.TotalSeconds, 1))s)"

# Phase 5: Artifact Verification & Cryptographic Telemetry
Write-Step "5" "Artifact Telemetry: Computing SHA-256 verification hash"
$releasesDir = Join-Path $root "releases"
$installers = Get-ChildItem -Path $releasesDir -Filter "*.exe" -ErrorAction SilentlyContinue
$telemetryDetail = ""
if ($installers) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        foreach ($inst in $installers) {
            $stream = [System.IO.File]::OpenRead($inst.FullName)
            try {
                $hashBytes = $sha.ComputeHash($stream)
                $hash = [System.BitConverter]::ToString($hashBytes).Replace("-", "")
                $sizeMb = [math]::Round($inst.Length / 1MB, 2)
                Write-Host "  > $($inst.Name) ($sizeMb MB)" -ForegroundColor Green
                Write-Host "    SHA-256: $hash" -ForegroundColor DarkGray
                Write-Output "Installer: $($inst.Name) ($sizeMb MB) | SHA-256: $hash" | Out-File -Append -FilePath $LogPath
                $telemetryDetail = "$($inst.Name) ($sizeMb MB)"
            } finally {
                $stream.Dispose()
            }
        }
    } finally {
        $sha.Dispose()
    }
    Complete-Step "5" $true $telemetryDetail
} else {
    Complete-Step "5" $true "published binaries generated (no installer)"
}

# Phase 6: Changelog Sync & Git Tagging
if (-not $SkipTag) {
    Write-Step "6" "Changelog & Git Release Tagging"
    try {
        $changelogFile = Join-Path $root "CHANGELOG.md"
        $deployTag = "deploy-$(Get-Date -Format yyyy.MM.dd-HHmm)"
        $existingTags = (git tag --list)
        $lastTag = if ($existingTags) { (git describe --tags --abbrev=0 2>$null) } else { $null }
        $commitRange = if ($lastTag) { "$lastTag..HEAD" } else { "HEAD~10..HEAD" }
        $commits = git log $commitRange --oneline --no-merges 2>$null
        if (-not $commits) { $commits = git log -n 10 --oneline --no-merges 2>$null }

        if ($commits) {
            $header = "`n## $deployTag`n"
            $body = ($commits | ForEach-Object { "- $_" }) -join "`n"
            $entry = "$header$body`n"

            if (Test-Path $changelogFile) {
                $existing = Get-Content $changelogFile -Raw
                $entry + $existing | Set-Content $changelogFile -Encoding utf8
            } else {
                "# NodeRadar Pro Changelog`n$entry" | Set-Content $changelogFile -Encoding utf8
            }
        }

        git tag $deployTag 2>$null
        Complete-Step "6" $true "tagged $deployTag"
    } catch {
        Complete-Step "6" $true "changelog/tag skipped: $($_.Exception.Message)"
    }
} else {
    Write-Step "6" "SKIPPED (SkipTag switch)"
    Complete-Step "6" $true "skipped"
}

$sw.Stop()

# Phase 7: Results Summary Table
Write-Host "`n============================================================================" -ForegroundColor Cyan
Write-Host "   DEPLOYMENT RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
$resultsTable = $results | Format-Table -AutoSize | Out-String
Write-Host $resultsTable
$resultsTable | Out-File -Append -FilePath $LogPath

Write-Host "== DEPLOYMENT COMPLETED in $([math]::Round($sw.Elapsed.TotalSeconds, 1))s ==" -ForegroundColor Green
Write-Host "Ready for GitHub Releases: https://github.com/PyPie-Studio/NodeRadar-Pro/releases/new`n" -ForegroundColor Cyan
exit 0
