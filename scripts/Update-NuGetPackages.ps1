#requires -Version 5.1
<#
.SYNOPSIS
    Checks for outdated NuGet packages and automatically upgrades them across the NodeRadar Pro solution.

.DESCRIPTION
    Uses native `dotnet list package --outdated --format json` to inspect all projects in the solution.
    Supports Central Package Management (Directory.Packages.props), automated upgrades, version constraint types
    (Minor, Patch, Auto, Always), and post-upgrade master gate validation.

.PARAMETER Upgrade
    When specified, automatically applies upgrades to the project files or Directory.Packages.props.

.PARAMETER UpgradeType
    Upgrade version target constraint: 'Auto' (default, upgrades all to latest), 'Minor' (safe minor/patch only), or 'Patch'.

.PARAMETER IncludePinned
    Includes packages normally pinned for runner compatibility (e.g. xUnit v4 MTP migration).

.PARAMETER Verify
    When specified, automatically runs the full Master Quality Gate (with test suite and coverage) after upgrading.

.EXAMPLE
    # Check for outdated packages only:
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Update-NuGetPackages.ps1

    # Upgrade minor and patch versions safely with validation:
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Update-NuGetPackages.ps1 -Upgrade -UpgradeType Minor -Verify

    # Upgrade all packages to latest:
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Update-NuGetPackages.ps1 -Upgrade -Verify
#>
[CmdletBinding()]
param(
    [switch]$Upgrade,
    [ValidateSet("Auto", "Minor", "Patch", "Always")]
    [string]$UpgradeType = "Auto",
    [switch]$IncludePinned,
    [switch]$Verify
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $root "NodeRadarPro.slnx"
$cpmPath = Join-Path $root "Directory.Packages.props"

Write-Host "== NodeRadar Pro NuGet Package Manager ==" -ForegroundColor Cyan

Push-Location $root
try {
    Write-Host "Analyzing dependencies across solution..." -ForegroundColor Yellow
    $rawJson = & dotnet list $solutionPath package --outdated --format json
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to list outdated packages." -ForegroundColor Red
        exit 1
    }

    $data = $rawJson | ConvertFrom-Json
    $outdatedCount = 0
    $packagesToUpgrade = @{}

    foreach ($proj in $data.projects) {
        $projName = [System.IO.Path]::GetFileName($proj.path)
        $projectOutdated = @()

        foreach ($fw in $proj.frameworks) {
            foreach ($pkg in $fw.topLevelPackages) {
                if ($pkg.latestVersion -and $pkg.resolvedVersion -ne $pkg.latestVersion) {
                    $projectOutdated += $pkg
                }
            }
        }

        if ($projectOutdated.Count -eq 0) {
            continue
        }

        Write-Host "`nProject: $projName" -ForegroundColor Cyan

        foreach ($pkg in $projectOutdated) {
            $isMajor = $false
            if ($pkg.resolvedVersion -and $pkg.latestVersion) {
                $curMajor = [int]($pkg.resolvedVersion.Split('.')[0])
                $newMajor = [int]($pkg.latestVersion.Split('.')[0])
                if ($curMajor -ne $newMajor) { $isMajor = $true }
            }

            # Pin protection: xUnit v4 drops VSTest runner support on .NET 10 SDK
            $isPinned = $false
            if ($pkg.id -like "xunit*" -and $pkg.latestVersion.StartsWith("4.")) {
                $isPinned = $true
            }

            if ($isPinned -and -not $IncludePinned) {
                Write-Host "  > $($pkg.id): $($pkg.resolvedVersion) -> $($pkg.latestVersion) [PINNED: xUnit v4 drops VSTest on .NET 10]" -ForegroundColor DarkYellow
                continue
            }

            $outdatedCount++
            $color = if ($isMajor) { "Red" } else { "Green" }
            Write-Host "  > $($pkg.id): $($pkg.resolvedVersion) -> $($pkg.latestVersion)" -ForegroundColor $color

            if ($Upgrade) {
                if ($UpgradeType -eq "Minor" -and $isMajor) {
                    Write-Host "    [SKIPPED] Major upgrade prevented by -UpgradeType Minor" -ForegroundColor DarkGray
                    continue
                }

                if (-not $packagesToUpgrade.ContainsKey($pkg.id)) {
                    $packagesToUpgrade[$pkg.id] = @{
                        Id = $pkg.id
                        TargetVersion = $pkg.latestVersion
                        ProjectPath = $proj.path
                    }
                }
            }
        }
    }

    if ($outdatedCount -eq 0) {
        Write-Host "`nAll NuGet packages across the solution are up to date!" -ForegroundColor Green
        return
    }

    if (-not $Upgrade) {
        Write-Host "`nFound $outdatedCount upgradeable package(s)." -ForegroundColor Yellow
        Write-Host "To automatically upgrade packages, run:" -ForegroundColor Cyan
        Write-Host "  powershell -ExecutionPolicy Bypass -File scripts/Update-NuGetPackages.ps1 -Upgrade -Verify" -ForegroundColor DarkGray
        Write-Host "  powershell -ExecutionPolicy Bypass -File scripts/Update-NuGetPackages.ps1 -Upgrade -UpgradeType Minor -Verify" -ForegroundColor DarkGray
        return
    }

    if ($packagesToUpgrade.Count -gt 0) {
        if (Test-Path $cpmPath) {
            Write-Host "`nUpdating Central Package Management (Directory.Packages.props)..." -ForegroundColor Yellow
            $cpmContent = Get-Content $cpmPath -Raw
            foreach ($entry in $packagesToUpgrade.Values) {
                $pkgId = [regex]::Escape($entry.Id)
                $targetVer = $entry.TargetVersion
                $pattern = '(?i)(<PackageVersion\s+Include="' + $pkgId + '"\s+Version=")[^"]+("\s*/>)'
                if ($cpmContent -match $pattern) {
                    $replacement = '$1' + $targetVer + '$2'
                    $cpmContent = [regex]::Replace($cpmContent, $pattern, $replacement)
                    Write-Host "  [CPM] Updated $($entry.Id) -> $targetVer" -ForegroundColor Green
                } else {
                    Write-Host "  [CPM] Package $($entry.Id) not found in CPM, updating project..." -ForegroundColor DarkYellow
                    & dotnet add "$($entry.ProjectPath)" package "$($entry.Id)" --version "$targetVer"
                }
            }
            Set-Content -Path $cpmPath -Value $cpmContent -Encoding utf8
        } else {
            foreach ($entry in $packagesToUpgrade.Values) {
                Write-Host "  [UPGRADING] $($entry.Id) to $($entry.TargetVersion)..." -ForegroundColor Yellow
                & dotnet add "$($entry.ProjectPath)" package "$($entry.Id)" --version "$($entry.TargetVersion)"
            }
        }

        Write-Host "`nRestoring solution packages..." -ForegroundColor Yellow
        & dotnet restore $solutionPath --nologo
    }

    if ($Verify) {
        Write-Host "`nRunning Master Gate validation (with full test suite & coverage)..." -ForegroundColor Yellow
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Test-MasterGate.ps1") -WithTests -CheckCoverage
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Master Gate failed after package upgrade! Review breaking API changes." -ForegroundColor Red
            exit 1
        }
    }

    Write-Host "`n== NuGet Package Upgrade Complete ==" -ForegroundColor Green
}
finally {
    Pop-Location
}
