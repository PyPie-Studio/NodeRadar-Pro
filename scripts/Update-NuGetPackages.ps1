#requires -Version 5.1
<#
.SYNOPSIS
    Checks for outdated NuGet packages and automatically upgrades them across the NodeRadar Pro solution.

.DESCRIPTION
    Uses `dotnet-outdated` (or `dotnet list package --outdated`) to inspect all projects in the solution.
    Supports automated upgrades, version constraint types (Minor, Patch, or Latest), and post-upgrade gate validation.

.PARAMETER Upgrade
    When specified, automatically applies upgrades to the project files.

.PARAMETER UpgradeType
    Upgrade version target constraint: 'Auto' (default), 'Minor' (safe minor/patch only), or 'Always' (includes major).

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

# 1. Ensure dotnet-outdated is available
$hasOutdatedTool = Get-Command "dotnet-outdated" -ErrorAction SilentlyContinue
if (-not $hasOutdatedTool) {
    Write-Host "Installing dotnet-outdated-tool globally..." -ForegroundColor Yellow
    & dotnet tool install --global dotnet-outdated-tool
}

Push-Location $root
try {
    if (-not $Upgrade) {
        Write-Host "Scanning for outdated NuGet packages across solution..." -ForegroundColor Yellow
        & dotnet-outdated $solutionPath
        Write-Host "`nTo automatically upgrade packages, run:" -ForegroundColor Cyan
        Write-Host "  powershell -ExecutionPolicy Bypass -File scripts/Update-NuGetPackages.ps1 -Upgrade -Verify" -ForegroundColor DarkGray
        Write-Host "  powershell -ExecutionPolicy Bypass -File scripts/Update-NuGetPackages.ps1 -Upgrade -UpgradeType Minor -Verify" -ForegroundColor DarkGray
        return
    }

    Write-Host "Upgrading outdated NuGet packages (Constraint: $UpgradeType)..." -ForegroundColor Yellow
    $outdatedArgs = @($solutionPath, "-u")
    if ($UpgradeType -eq "Minor") {
        $outdatedArgs += @("-version-lock", "Major")
    } elseif ($UpgradeType -eq "Patch") {
        $outdatedArgs += @("-version-lock", "Minor")
    }

    & dotnet-outdated @outdatedArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Package upgrade encountered warnings or errors." -ForegroundColor Yellow
    }

    Write-Host "`nRestoring solution packages..." -ForegroundColor Yellow
    & dotnet restore $solutionPath --nologo

    if ($Verify) {
        Write-Host "`nRunning test suite verification..." -ForegroundColor Yellow
        & dotnet test $solutionPath --configuration Release --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Test execution failed after package upgrade! Review broken APIs." -ForegroundColor Red
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
