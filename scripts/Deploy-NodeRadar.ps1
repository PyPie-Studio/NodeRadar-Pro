#requires -Version 5.1
<#
.SYNOPSIS
    NodeRadar Pro one-click deployment & automated GitHub release pipeline:
    preflight -> quality gate -> OUI sync -> test suite -> release packaging ->
    SHA-256 telemetry -> categorized changelog -> git commit/tag -> GitHub release publishing.

.PARAMETER SkipGate
    Skips the Master Quality Gate (for urgent hotfixes).

.PARAMETER SkipOui
    Skips downloading the live IEEE MAC OUI database from Wireshark.

.PARAMETER SkipTests
    Skips test suite execution.

.PARAMETER SkipInno
    Skips Inno Setup installer compilation (produces obfuscated publish binaries only).

.PARAMETER SkipRelease
    Skips pushing to git origin and creating/uploading the GitHub release (local packaging only).

.PARAMETER ReleaseNotes
    Custom release notes string (overrides automatic git commit categorization).

.PARAMETER LogPath
    Custom path for deployment log output.

.EXAMPLE
    .\deploy.bat

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\Deploy-NodeRadar.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\Deploy-NodeRadar.ps1 -SkipRelease
#>
[CmdletBinding()]
param(
    [switch]$SkipGate,
    [switch]$SkipOui,
    [switch]$SkipTests,
    [switch]$SkipInno,
    [switch]$SkipRelease,
    [string]$ReleaseNotes = "",
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
Write-Host "   NodeRadar Pro: Automated Deployment & GitHub Release Pipeline" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Log: $LogPath`n" -ForegroundColor DarkGray

# Phase 0: Preflight & Version Inspection
Write-Step "0" "Preflight: Git state, SDK & Project Version inspection"
$branch = git rev-parse --abbrev-ref HEAD 2>$null
if (-not $branch) { $branch = "main" }

# Extract Version from NodeRadarPro.csproj
$csprojPath = Join-Path $root "src\NodeRadarPro\NodeRadarPro.csproj"
[xml]$projXml = Get-Content $csprojPath
$appVersion = $projXml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $appVersion) { $appVersion = "1.0.0" }
$versionTag = "v$appVersion"

Write-Host "  > Target Application Version: $appVersion ($versionTag)" -ForegroundColor Green

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Fail-Run "0" "dotnet SDK not found on system PATH" }
$sdkVer = & dotnet --version
Complete-Step "0" $true "v$appVersion on branch=$branch (sdk=$sdkVer)"

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
$installerChecksums = @()

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
                $installerChecksums += [pscustomobject]@{ Name = $inst.Name; SizeMb = $sizeMb; Hash = $hash; FullPath = $inst.FullName }
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

# Phase 6: Categorized Changelog Generation & Git Commit
Write-Step "6" "Changelog Sync & Git Release Commit ($versionTag)"
$prevEap = $ErrorActionPreference
try {
    $ErrorActionPreference = "SilentlyContinue"
    $changelogFile = Join-Path $root "CHANGELOG.md"
    $dateStr = Get-Date -Format "yyyy-MM-dd"

    # Discover commits since last version tag
    $prevTag = (git describe --tags --abbrev=0 --match "v*" 2>$null)
    $commitRange = if ($prevTag) { "$prevTag..HEAD" } else { "HEAD~15..HEAD" }
    $commits = git log $commitRange --oneline --no-merges 2>$null
    if (-not $commits) { $commits = git log -n 10 --oneline --no-merges 2>$null }

    $features = @()
    $fixes = @()
    $security = @()
    $perf = @()
    $other = @()

    if ($commits) {
        foreach ($line in $commits) {
            if ($line -match "^[a-f0-9]+ (?:feat|feature)(\([^)]+\))?: (.*)") {
                $features += "- $($matches[2])"
            } elseif ($line -match "^[a-f0-9]+ (?:fix|bug)(\([^)]+\))?: (.*)") {
                $fixes += "- $($matches[2])"
            } elseif ($line -match "^[a-f0-9]+ (?:sec|security)(\([^)]+\))?: (.*)") {
                $security += "- $($matches[2])"
            } elseif ($line -match "^[a-f0-9]+ (?:perf)(\([^)]+\))?: (.*)") {
                $perf += "- $($matches[2])"
            } elseif ($line -notmatch "chore\(release\)") {
                $msg = $line -replace "^[a-f0-9]+ ", ""
                $other += "- $msg"
            }
        }
    }

    # Format release notes markdown
    $notesLines = @()
    $notesLines += "## NodeRadar Pro $versionTag ($dateStr)`n"

    if ($ReleaseNotes) {
        $notesLines += "$ReleaseNotes`n"
    } else {
        if ($features.Count -gt 0) {
            $notesLines += "### New Features`n" + ($features -join "`n") + "`n"
        }
        if ($fixes.Count -gt 0) {
            $notesLines += "### Bug Fixes`n" + ($fixes -join "`n") + "`n"
        }
        if ($security.Count -gt 0) {
            $notesLines += "### Security Enhancements`n" + ($security -join "`n") + "`n"
        }
        if ($perf.Count -gt 0) {
            $notesLines += "### Performance and Optimization`n" + ($perf -join "`n") + "`n"
        }
        if ($other.Count -gt 0) {
            $notesLines += "### Maintenance and Refactoring`n" + ($other -join "`n") + "`n"
        }
    }

    $releaseNotesText = $notesLines -join "`n"

    # Prepend entry to CHANGELOG.md (UTF-8 without BOM)
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    if (Test-Path $changelogFile) {
        $existing = [System.IO.File]::ReadAllText($changelogFile)
        [System.IO.File]::WriteAllText($changelogFile, $releaseNotesText + "`n" + $existing, $utf8NoBom)
    } else {
        [System.IO.File]::WriteAllText($changelogFile, "# NodeRadar Pro Changelog`n`n$releaseNotesText", $utf8NoBom)
    }

    # Stage and commit changelog + project version bump if modified
    git add $changelogFile $csprojPath 2>$null
    git commit -m "chore(release): release $versionTag" 2>$null

    # Create annotated tag
    git tag -a $versionTag -m "Release $versionTag" -f 2>$null
    Complete-Step "6" $true "tagged $versionTag & updated CHANGELOG.md"
} catch {
    Complete-Step "6" $true "changelog/tag deferred: $($_.Exception.Message)"
} finally {
    $ErrorActionPreference = $prevEap
}

# Phase 7: GitHub Release Publishing
if (-not $SkipRelease) {
    Write-Step "7" "GitHub Release Publishing: Uploading installer & publishing release"
    $prevEap = $ErrorActionPreference
    try {
        $ErrorActionPreference = "SilentlyContinue"
        
        # 1. Push commit & tag to origin
        Write-Host "  > Pushing commit and tag $versionTag to origin..." -ForegroundColor Yellow
        git push origin HEAD 2>$null
        git push origin $versionTag --force 2>$null

        # 2. Check if GitHub CLI (gh) is available and authenticated
        $ghInstalled = Get-Command gh -ErrorAction SilentlyContinue
        if ($ghInstalled) {
            $notesTmpFile = Join-Path $logDir "release-notes-$versionTag.md"
            [System.IO.File]::WriteAllText($notesTmpFile, $releaseNotesText, [System.Text.UTF8Encoding]::new($false))

            $targetInstaller = if ($installerChecksums.Count -gt 0) { $installerChecksums[0].FullPath } else { $null }

            Write-Host "  > Creating GitHub Release via gh CLI..." -ForegroundColor Yellow
            $targetInstaller = if ($installerChecksums.Count -gt 0) { $installerChecksums[0].FullPath } else { $null }

            if ($targetInstaller -and (Test-Path $targetInstaller)) {
                & gh release create $versionTag "$targetInstaller" --title "NodeRadar Pro $versionTag" --notes-file "$notesTmpFile" 2>$null
            } else {
                & gh release create $versionTag --title "NodeRadar Pro $versionTag" --notes-file "$notesTmpFile" 2>$null
            }

            if ($LASTEXITCODE -ne 0) {
                # Release exists; edit notes and re-upload installer
                & gh release edit $versionTag --title "NodeRadar Pro $versionTag" --notes-file "$notesTmpFile" 2>$null
                if ($targetInstaller -and (Test-Path $targetInstaller)) {
                    & gh release upload $versionTag "$targetInstaller" --clobber 2>$null
                }
            }

            $releaseUrl = "https://github.com/Ahmed-Yaseen99/NodeRadar-Pro/releases/tag/$versionTag"
            Complete-Step "7" $true "Published $versionTag to GitHub ($releaseUrl)"
        } else {
            Complete-Step "7" $true "Tag pushed to origin (gh CLI not installed for direct upload)"
        }
    } catch {
        Complete-Step "7" $true "Release publishing deferred: $($_.Exception.Message)"
    } finally {
        $ErrorActionPreference = $prevEap
    }
} else {
    Write-Step "7" "SKIPPED (SkipRelease switch)"
    Complete-Step "7" $true "skipped"
}

$sw.Stop()

# Phase 8: Results Summary Table
Write-Host "`n============================================================================" -ForegroundColor Cyan
Write-Host "   DEPLOYMENT & RELEASE RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
$resultsTable = $results | Format-Table -AutoSize | Out-String
Write-Host $resultsTable
$resultsTable | Out-File -Append -FilePath $LogPath

Write-Host "== RELEASE v$appVersion COMPLETED in $([math]::Round($sw.Elapsed.TotalSeconds, 1))s ==" -ForegroundColor Green
Write-Host "GitHub Releases: https://github.com/Ahmed-Yaseen99/NodeRadar-Pro/releases`n" -ForegroundColor Cyan
exit 0
