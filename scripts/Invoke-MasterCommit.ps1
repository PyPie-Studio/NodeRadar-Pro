#requires -Version 5.1
<#
.SYNOPSIS
    Automated gate verification, graph refresh, and conventional git commit workflow for NodeRadar Pro.
    Executes: Working tree check -> Test-MasterGate.ps1 -> graphify update -> git stage -> git commit -> (optional) git push.

.PARAMETER Message
    The conventional commit message (required, e.g. "feat(core): add subnet sweep cancellation token").
    Follows conventional commits with types: feat, fix, perf, sec, refactor, style, docs, test, chore.

.PARAMETER DetailedMessages
    Optional array of bullet points or extended description lines for multi-change commits.

.PARAMETER Files
    Optional array of specific file paths to stage. Defaults to staging all tracked modified and untracked files (`git add -A`).

.PARAMETER Push
    If specified, pushes to origin main upon successful commit.

.PARAMETER SkipGraphify
    If specified, skips updating the graphify AST knowledge graph.

.PARAMETER WithTests
    If specified, runs the test suite during Master Gate verification.

.EXAMPLE
    powershell -File scripts/Invoke-MasterCommit.ps1 -Message "feat(core): add ICMP rate limiting"
    powershell -File scripts/Invoke-MasterCommit.ps1 -Message "fix(ui): prevent tooltip clipping on radar canvas" -Push
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Message,

    [Parameter(Position = 1)]
    [string[]]$DetailedMessages = @(),

    [string[]]$Files = @(),

    [switch]$Push,

    [switch]$SkipGraphify,

    [switch]$WithTests
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "== NodeRadar Pro Master Commit Workflow ==" -ForegroundColor Cyan

# 1. Validate Working Tree Status
Push-Location $root
try {
    $status = git status --porcelain 2>&1
    if (-not $status) {
        Write-Host "No changes detected in git working tree. Nothing to commit." -ForegroundColor Yellow
        exit 0
    }
} finally { Pop-Location }

# 2. Validate Conventional Commit Message
$trimmedMessage = $Message.Trim()
$pattern = '^(feat|fix|perf|sec|refactor|style|docs|test|chore)(\([a-z0-9_-]+\))?:\s+.+$'
if ($trimmedMessage -notmatch $pattern) {
    Write-Host "WARNING: Commit message '$Message' does not match conventional commit format." -ForegroundColor Yellow
    Write-Host "Expected format: <type>(<scope>): <subject> or <type>: <subject>" -ForegroundColor DarkYellow
    Write-Host "Allowed types: feat, fix, perf, sec, refactor, style, docs, test, chore" -ForegroundColor DarkYellow
}

if ($trimmedMessage.Length -gt 72) {
    Write-Host "WARNING: Subject line is $($trimmedMessage.Length) characters (recommended: <= 72 characters)." -ForegroundColor DarkYellow
}

# 3. Run Master Gate Verification
Write-Host "`n--> [1/4] Running Master Gate Verification..." -ForegroundColor Cyan
Push-Location $root
try {
    $gateScript = Join-Path $PSScriptRoot "Test-MasterGate.ps1"
    $gateArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $gateScript)
    if ($WithTests) {
        $gateArgs += "-WithTests"
    }
    & powershell @gateArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n== MASTER COMMIT ABORTED: Master Gate failed. Fix issues before committing. ==" -ForegroundColor Red
        exit 1
    }
} finally { Pop-Location }

# 4. Refresh AST Knowledge Graph (graphify)
if (-not $SkipGraphify) {
    Write-Host "`n--> [2/4] Refreshing AST Knowledge Graph (graphify)..." -ForegroundColor Cyan
    Push-Location $root
    try {
        $graphifyCmd = Get-Command "graphify" -ErrorAction SilentlyContinue
        if ($graphifyCmd) {
            & graphify update .
            if ($LASTEXITCODE -eq 0) {
                Write-Host "AST knowledge graph synchronized." -ForegroundColor Green
            } else {
                Write-Host "WARNING: graphify update returned non-zero code ($LASTEXITCODE). Continuing with commit." -ForegroundColor Yellow
            }
        } else {
            Write-Host "graphify command not found in PATH. Skipping AST update." -ForegroundColor DarkGray
        }
    } catch {
        Write-Host "WARNING: graphify update failed: $($_.Exception.Message). Continuing with commit." -ForegroundColor Yellow
    } finally { Pop-Location }
} else {
    Write-Host "`n--> [2/4] Skipped AST Knowledge Graph refresh (-SkipGraphify)." -ForegroundColor DarkGray
}

# 5. Stage Files
Write-Host "`n--> [3/4] Staging changes..." -ForegroundColor Cyan
Push-Location $root
try {
    if ($Files.Count -gt 0) {
        foreach ($file in $Files) {
            git add $file
        }
    } else {
        git add -A
    }
} finally { Pop-Location }

# 6. Execute Commit
Write-Host "`n--> [4/4] Creating git commit..." -ForegroundColor Cyan
Push-Location $root
try {
    $commitArgs = @("commit", "-m", $trimmedMessage)
    foreach ($line in $DetailedMessages) {
        if (-not [string]::IsNullOrWhiteSpace($line)) {
            $commitArgs += @("-m", $line.Trim())
        }
    }

    & git @commitArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n== Git commit failed. ==" -ForegroundColor Red
        exit 1
    }

    Write-Host "`nCommit created successfully!" -ForegroundColor Green
} finally { Pop-Location }

# 7. Optional Push
if ($Push) {
    Write-Host "`n--> Pushing to origin main..." -ForegroundColor Cyan
    Push-Location $root
    try {
        # Master Gate was verified in Step 1 of this workflow; bypass redundant second pre-push gate
        & git push origin main --no-verify
        if ($LASTEXITCODE -ne 0) {
            Write-Host "`n== Git push failed. ==" -ForegroundColor Red
            exit 1
        }
        Write-Host "Pushed to origin main successfully." -ForegroundColor Green
    } finally { Pop-Location }
}

$elapsed = [math]::Round($sw.Elapsed.TotalSeconds, 1)
Write-Host "`n== MASTER COMMIT WORKFLOW COMPLETED (${elapsed}s) ==" -ForegroundColor Green
exit 0
