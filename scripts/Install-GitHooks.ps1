#requires -Version 5.1
<#
.SYNOPSIS
    Installs or uninstalls local Git pre-push hooks to enforce Master Gate verification before every push.

.DESCRIPTION
    Creates a `.git/hooks/pre-push` hook that automatically executes `scripts/Test-MasterGate.ps1`
    whenever `git push` is invoked. Blocks pushing if there are compiler warnings, formatting violations,
    failing tests, or NuGet vulnerabilities.

.PARAMETER Uninstall
    When specified, removes the pre-push hook.

.PARAMETER Test
    When specified, tests the pre-push hook immediately after installation.

.EXAMPLE
    # Install the pre-push hook:
    powershell -ExecutionPolicy Bypass -File scripts/Install-GitHooks.ps1

    # Install and test:
    powershell -ExecutionPolicy Bypass -File scripts/Install-GitHooks.ps1 -Test

    # Uninstall:
    powershell -ExecutionPolicy Bypass -File scripts/Install-GitHooks.ps1 -Uninstall
#>
[CmdletBinding()]
param(
    [switch]$Uninstall,
    [switch]$Test
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$gitDir = Join-Path $root ".git"
$hooksDir = Join-Path $gitDir "hooks"
$prePushHook = Join-Path $hooksDir "pre-push"

Write-Host "== NodeRadar Pro: Local Git Hooks Manager ==" -ForegroundColor Cyan

if (-not (Test-Path $gitDir)) {
    Write-Host "[ERROR] .git directory not found at $root" -ForegroundColor Red
    exit 1
}

if ($Uninstall) {
    if (Test-Path $prePushHook) {
        Remove-Item -Path $prePushHook -Force
        Write-Host "Pre-push hook successfully uninstalled from $prePushHook." -ForegroundColor Green
    } else {
        Write-Host "No pre-push hook found to uninstall." -ForegroundColor Yellow
    }
    exit 0
}

# Ensure hooks directory exists
if (-not (Test-Path $hooksDir)) {
    New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null
}

# Create pre-push POSIX script with LF line endings
$hookContent = @'
#!/bin/sh
# NodeRadar Pro - Local Git Pre-Push Gate Hook
# Automatically executes Test-MasterGate.ps1 before every git push.

echo ""
echo "============================================================"
echo " [Git Hook] Running NodeRadar Pro Master Gate Verification..."
echo "============================================================"

# Resolve PowerShell executable (prefer pwsh over Windows powershell)
if command -v pwsh >/dev/null 2>&1; then
    PS_EXEC="pwsh"
elif command -v powershell.exe >/dev/null 2>&1; then
    PS_EXEC="powershell.exe"
else
    PS_EXEC="powershell"
fi

$PS_EXEC -NoProfile -ExecutionPolicy Bypass -File "scripts/Test-MasterGate.ps1"
RESULT=$?

if [ $RESULT -ne 0 ]; then
    echo ""
    echo "============================================================"
    echo " [PUSH ABORTED] Master Gate verification failed ($RESULT)."
    echo " Please fix the errors/warnings above before pushing."
    echo "============================================================"
    echo ""
    exit 1
fi

echo " [Git Hook] Master Gate PASSED. Proceeding with push."
echo ""
exit 0
'@

# Normalize to LF endings for POSIX shell compatibility in Git
$lfContent = $hookContent.Replace("`r`n", "`n")
[System.IO.File]::WriteAllText($prePushHook, $lfContent, [System.Text.UTF8Encoding]::new($false))

Write-Host "Pre-push hook successfully installed at: $prePushHook" -ForegroundColor Green
Write-Host "Master Gate verification will now automatically protect every local 'git push'." -ForegroundColor Cyan

if ($Test) {
    Write-Host "`nTesting pre-push hook execution..." -ForegroundColor Yellow
    Push-Location $root
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Test-MasterGate.ps1")
    }
    finally {
        Pop-Location
    }
}
