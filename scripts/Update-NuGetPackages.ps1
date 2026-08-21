#requires -Version 5.1
<#
.SYNOPSIS
    Checks for outdated NuGet packages and automatically upgrades them across the NodeRadar Pro solution.

.DESCRIPTION
    Uses native `dotnet list package --outdated --format json` to inspect all projects in the solution.
    Supports automated upgrades, version constraint types (Minor, Patch, or Latest), and post-upgrade gate validation.

.PARAMETER Upgrade
    When specified, automatically applies upgrades to the project files.

.PARAMETER UpgradeType
    Upgrade version target constraint: 'Auto' (default, upgrades all to latest), 'Minor' (safe minor/patch only), or 'Patch'.

.PARAMETER Verify
    When specified, automatically runs test suites and the master gate after upgrading.

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
    [switch]$Verify
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $root "NodeRadarPro.slnx"

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

    foreach ($proj in $data.projects) {
        $projName = [System.IO.Path]::GetFileName($proj.path)
        $packagesToUpdate = @()

        foreach ($fw in $proj.frameworks) {
            foreach ($pkg in $fw.topLevelPackages) {
                if ($pkg.latestVersion -and $pkg.resolvedVersion -ne $pkg.latestVersion) {
                    $packagesToUpdate += $pkg
                }
            }
        }

        if ($packagesToUpdate.Count -eq 0) {
            continue
        }

        $outdatedCount += $packagesToUpdate.Count
        Write-Host "`nProject: $projName" -ForegroundColor Cyan

        foreach ($pkg in $packagesToUpdate) {
            $isMajor = $false
            if ($pkg.resolvedVersion -and $pkg.latestVersion) {
                $curMajor = ($pkg.resolvedVersion.Split('.')[0])
                $newMajor = ($pkg.latestVersion.Split('.')[0])
                if ($curMajor -ne $newMajor) { $isMajor = $true }
            }

            $color = if ($isMajor) { "Red" } else { "Green" }
            Write-Host "  > $($pkg.id): $($pkg.resolvedVersion) -> $($pkg.latestVersion)" -ForegroundColor $color

            if ($Upgrade) {
                if ($UpgradeType -eq "Minor" -and $isMajor) {
                    Write-Host "    [SKIPPED] Major upgrade prevented by -UpgradeType Minor" -ForegroundColor DarkGray
                    continue
                }

                Write-Host "    [UPGRADING] $($pkg.id) to $($pkg.latestVersion)..." -ForegroundColor Yellow
                & dotnet add "$($proj.path)" package "$($pkg.id)" --version "$($pkg.latestVersion)"
                if ($LASTEXITCODE -ne 0) {
                    Write-Host "    [ERROR] Failed to upgrade $($pkg.id)" -ForegroundColor Red
                }
            }
        }
    }

    if ($outdatedCount -eq 0) {
        Write-Host "`nAll NuGet packages across the solution are up to date!" -ForegroundColor Green
        return
    }

    if (-not $Upgrade) {
        Write-Host "`nFound $outdatedCount outdated package(s)." -ForegroundColor Yellow
        Write-Host "To automatically upgrade packages, run:" -ForegroundColor Cyan
        Write-Host "  powershell -ExecutionPolicy Bypass -File scripts/Update-NuGetPackages.ps1 -Upgrade -Verify" -ForegroundColor DarkGray
        Write-Host "  powershell -ExecutionPolicy Bypass -File scripts/Update-NuGetPackages.ps1 -Upgrade -UpgradeType Minor -Verify" -ForegroundColor DarkGray
        return
    }

    Write-Host "`nRestoring solution packages..." -ForegroundColor Yellow
    & dotnet restore $solutionPath --nologo

    if ($Verify) {
        Write-Host "`nRunning test suite verification..." -ForegroundColor Yellow
        & dotnet test $solutionPath --configuration Release --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Test execution failed after package upgrade! Review breaking API changes." -ForegroundColor Red
            exit 1
        }

        Write-Host "`nRunning Master Gate validation..." -ForegroundColor Yellow
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Test-MasterGate.ps1")
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Master Gate failed after package upgrade." -ForegroundColor Red
            exit 1
        }
    }

    Write-Host "`n== NuGet Package Upgrade Complete ==" -ForegroundColor Green
}
finally {
    Pop-Location
}
