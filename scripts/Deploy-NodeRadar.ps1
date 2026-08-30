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
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$results = New-Object System.Collections.Generic.List[object]

$logDir = Join-Path $root "deploy-logs"
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
}

if (-not $LogPath) {
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

if (-not $SkipRelease) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Fail-Run "0" "GitHub CLI (gh) not found on system PATH. Install gh or use -SkipRelease."
    }
    $null = (& gh auth status 2>&1)
    if ($LASTEXITCODE -ne 0) {
        Fail-Run "0" "GitHub CLI (gh) is not authenticated. Run 'gh auth login' or use -SkipRelease."
    }
}
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
    $ErrorActionPreference = "Stop"
    $changelogFile = Join-Path $root "CHANGELOG.md"
    $dateStr = Get-Date -Format "yyyy-MM-dd"

    # Discover commits since previous version tag
    $allTags = git tag -l "v*" --sort=-v:refname 2>$null
    $prevTag = $allTags | Where-Object { $_ -ne $versionTag } | Select-Object -First 1
    $commitRange = if ($prevTag) { "$prevTag..HEAD" } else { "HEAD~20..HEAD" }
    $rawCommits = git log $commitRange --oneline --no-merges 2>$null
    if (-not $rawCommits) { $rawCommits = git log -n 20 --oneline --no-merges 2>$null }

    $features = [System.Collections.Generic.List[string]]::new()
    $fixes = [System.Collections.Generic.List[string]]::new()
    $security = [System.Collections.Generic.List[string]]::new()
    $perf = [System.Collections.Generic.List[string]]::new()
    $tests = [System.Collections.Generic.List[string]]::new()
    $refactor = [System.Collections.Generic.List[string]]::new()

    if ($rawCommits) {
        $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($line in $rawCommits) {
            $cleaned = $line -replace "^[a-f0-9]+\s+", ""
            if ($cleaned -match "chore\(release\)" -or [string]::IsNullOrWhiteSpace($cleaned)) { continue }

            # Normalize emoji prefixes and classify
            if ($cleaned -match "^(?:🔒|🛡️|sec(?:urity)?(?:\([^)]+\))?:)\s*(.*)" -or $cleaned -match "(?i)(?:certificate revocation|command injection|vulnerability|cve|credential|encryption)") {
                $item = if ($matches[1]) { $matches[1].Trim() } else { $cleaned }
                $item = "- " + $item.TrimStart("- ")
                if ($seen.Add($item)) { $security.Add($item) }
            } elseif ($cleaned -match "^(?:✨|feat(?:ure)?(?:\([^)]+\))?:)\s*(.*)") {
                $item = if ($matches[1]) { $matches[1].Trim() } else { $cleaned }
                $item = "- " + $item.TrimStart("- ")
                if ($seen.Add($item)) { $features.Add($item) }
            } elseif ($cleaned -match "^(?:🐛|🚑|fix(?:\([^)]+\))?:|bug(?:\([^)]+\))?:)\s*(.*)") {
                $item = if ($matches[1]) { $matches[1].Trim() } else { $cleaned }
                $item = "- " + $item.TrimStart("- ")
                if ($seen.Add($item)) { $fixes.Add($item) }
            } elseif ($cleaned -match "^(?:⚡|🚀|perf(?:\([^)]+\))?:)\s*(.*)") {
                $item = if ($matches[1]) { $matches[1].Trim() } else { $cleaned }
                $item = "- " + $item.TrimStart("- ")
                if ($seen.Add($item)) { $perf.Add($item) }
            } elseif ($cleaned -match "^(?:🧪|test(?:\([^)]+\))?:)\s*(.*)") {
                $item = if ($matches[1]) { $matches[1].Trim() } else { $cleaned }
                $item = "- " + $item.TrimStart("- ")
                if ($seen.Add($item)) { $tests.Add($item) }
            } elseif ($cleaned -match "^(?:🧹|♻️|style(?:\([^)]+\))?:|refactor(?:\([^)]+\))?:)\s*(.*)") {
                $item = if ($matches[1]) { $matches[1].Trim() } else { $cleaned }
                $item = "- " + $item.TrimStart("- ")
                if ($seen.Add($item)) { $refactor.Add($item) }
            } else {
                $item = "- " + $cleaned.TrimStart("- ")
                if ($seen.Add($item)) { $refactor.Add($item) }
            }
        }
    }

    # Format release notes markdown
    $notesLines = @()
    $notesLines += "## NodeRadar Pro $versionTag ($dateStr)`n"

    if ($ReleaseNotes) {
        $notesLines += "$ReleaseNotes`n"
    } else {
        if ($security.Count -gt 0) {
            $notesLines += "### 🔒 Security Enhancements`n" + ($security -join "`n") + "`n"
        }
        if ($features.Count -gt 0) {
            $notesLines += "### ✨ New Features`n" + ($features -join "`n") + "`n"
        }
        if ($fixes.Count -gt 0) {
            $notesLines += "### 🐛 Bug Fixes`n" + ($fixes -join "`n") + "`n"
        }
        if ($perf.Count -gt 0) {
            $notesLines += "### ⚡ Performance and Optimization`n" + ($perf -join "`n") + "`n"
        }
        if ($tests.Count -gt 0) {
            $notesLines += "### 🧪 Test Coverage & Diagnostics`n" + ($tests -join "`n") + "`n"
        }
        if ($refactor.Count -gt 0) {
            $notesLines += "### 🧹 Maintenance and Refactoring`n" + ($refactor -join "`n") + "`n"
        }
    }

    $releaseNotesText = $notesLines -join "`n"

    # Insert entry into CHANGELOG.md (UTF-8 without BOM, replace existing tag entry if present)
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    if (Test-Path $changelogFile) {
        $existing = [System.IO.File]::ReadAllText($changelogFile)
        $escapedTag = [regex]::Escape($versionTag)
        $sectionPattern = "(?ms)## NodeRadar Pro $escapedTag\s*\([^\)]+\).*?(?=(## NodeRadar Pro v|\Z))"
        if ($existing -match $sectionPattern) {
            $existing = $existing -replace $sectionPattern, "$releaseNotesText`n"
            [System.IO.File]::WriteAllText($changelogFile, $existing, $utf8NoBom)
        } elseif ($existing -match "(?ms)^(# NodeRadar Pro Changelog\s*\r?\n)(.*)$") {
            $header = $matches[1]
            $body = $matches[2]
            [System.IO.File]::WriteAllText($changelogFile, "$header`n$releaseNotesText`n$body", $utf8NoBom)
        } else {
            [System.IO.File]::WriteAllText($changelogFile, "# NodeRadar Pro Changelog`n`n$releaseNotesText`n$existing", $utf8NoBom)
        }
    } else {
        [System.IO.File]::WriteAllText($changelogFile, "# NodeRadar Pro Changelog`n`n$releaseNotesText", $utf8NoBom)
    }

    # Stage all modifications and commit
    git add -A
    $hasUncommitted = (git status --porcelain)
    if ($hasUncommitted) {
        git commit -m "chore(release): release $versionTag"
        if ($LASTEXITCODE -ne 0) { Fail-Run "6" "Failed to commit release changes" }
    }

    # Create annotated tag
    git tag -a $versionTag -m "Release $versionTag" -f
    if ($LASTEXITCODE -ne 0) { Fail-Run "6" "Failed to create git tag $versionTag" }
    Complete-Step "6" $true "tagged $versionTag & updated CHANGELOG.md"
} catch {
    Fail-Run "6" "Changelog or tag failure: $($_.Exception.Message)"
} finally {
    $ErrorActionPreference = $prevEap
}

# Phase 7: GitHub Release Publishing & Live API Verification
if (-not $SkipRelease) {
    Write-Step "7" "GitHub Release Publishing: Uploading installer & publishing release"
    $prevEap = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Stop"

        # 1. Push commit & tag to origin with strict verification
        Write-Host "  > Pushing commit and tag $versionTag to origin..." -ForegroundColor Yellow
        & git push origin HEAD
        if ($LASTEXITCODE -ne 0) {
            Fail-Run "7" "Failed to push release commit to origin"
        }

        & git push origin $versionTag --force
        if ($LASTEXITCODE -ne 0) {
            Fail-Run "7" "Failed to push git tag $versionTag to origin"
        }

        # 2. Check installer existence
        $targetInstaller = if ($installerChecksums.Count -gt 0) { $installerChecksums[0].FullPath } else { $null }
        if (-not $targetInstaller -or -not (Test-Path $targetInstaller)) {
            Fail-Run "7" "Installer binary not found for release publishing ($targetInstaller)"
        }

        $installerFileName = [System.IO.Path]::GetFileName($targetInstaller)
        $installerDir = [System.IO.Path]::GetDirectoryName($targetInstaller)

        $notesTmpFile = Join-Path $logDir "release-notes-$versionTag.md"
        [System.IO.File]::WriteAllText($notesTmpFile, $releaseNotesText, [System.Text.UTF8Encoding]::new($false))

        Write-Host "  > Creating/updating GitHub Release via gh CLI..." -ForegroundColor Yellow

        Push-Location $installerDir
        try {
            $null = (& gh release view $versionTag 2>&1)
            if ($LASTEXITCODE -eq 0) {
                # Release already exists: update notes and upload installer
                & gh release edit $versionTag --title "NodeRadar Pro $versionTag" --notes-file "$notesTmpFile"
                if ($LASTEXITCODE -ne 0) { Fail-Run "7" "Failed to edit existing release $versionTag via gh CLI" }

                & gh release upload $versionTag ".\$installerFileName" --clobber
                if ($LASTEXITCODE -ne 0) { Fail-Run "7" "Failed to upload installer to release $versionTag" }
            } else {
                # Create brand new release with installer asset
                & gh release create $versionTag ".\$installerFileName" --title "NodeRadar Pro $versionTag" --notes-file "$notesTmpFile"
                if ($LASTEXITCODE -ne 0) { Fail-Run "7" "Failed to create release $versionTag via gh CLI" }
            }
        } finally {
            Pop-Location
        }

        # 3. CRITICAL: LIVE CONFIRMATION QUERY (Zero False Positives)
        Write-Host "  > Verifying release and attached assets live on GitHub..." -ForegroundColor Yellow
        $verificationRaw = (& gh release view $versionTag --json tagName,url,assets 2>&1 | Out-String)
        if ($LASTEXITCODE -ne 0) {
            Fail-Run "7" "Live confirmation failed: Release $versionTag was not found on GitHub after creation attempt"
        }

        $releaseInfo = $verificationRaw | ConvertFrom-Json
        $assetNames = @($releaseInfo.assets | ForEach-Object { $_.name })
        $normalizedAssetName = $installerFileName -replace ' ', '.'
        $matchedAsset = $assetNames | Where-Object { $_ -eq $installerFileName -or $_ -eq $normalizedAssetName }
        if (-not $matchedAsset) {
            Fail-Run "7" "Live confirmation failed: Asset '$installerFileName' is missing from GitHub release assets (found: $($assetNames -join ', '))"
        }

        $releaseUrl = $releaseInfo.url
        Complete-Step "7" $true "Verified release $versionTag on GitHub with asset '$($matchedAsset -join ', ')' ($releaseUrl)"
    } catch {
        Fail-Run "7" "Release publishing failed: $($_.Exception.Message)"
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
