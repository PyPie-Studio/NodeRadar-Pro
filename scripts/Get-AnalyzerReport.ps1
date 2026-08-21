#requires -Version 5.1
<#
.SYNOPSIS
    Generates a structured report for specific Roslyn analyzer diagnostic IDs across NodeRadar Pro.
    Uses 'dotnet format' analyzer severity reporting to highlight code debt.

.PARAMETER Diagnostics
    Array of Roslyn diagnostic IDs to inspect (default: S3776, S107, S1541, CA1506).

.PARAMETER OutputPath
    File path where the diagnostic report will be saved (default: docs/analyzer-report.txt).

.EXAMPLE
    pwsh -File scripts/Get-AnalyzerReport.ps1
#>
[CmdletBinding()]
param(
    [string[]]$Diagnostics = @("S3776", "S107", "S1541", "CA1506"),
    [string]$OutputPath = "docs/analyzer-report.txt"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "== Generating NodeRadar Pro Roslyn Analyzer Report ==" -ForegroundColor Cyan
Write-Host "Target Diagnostics: $($Diagnostics -join ', ')" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow

Push-Location $root
try {
    $outPathFull = Join-Path $root $OutputPath
    $outDir = Split-Path -Parent $outPathFull
    if (-not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }

    $diagArgs = @()
    foreach ($d in $Diagnostics) {
        $diagArgs += "--diagnostics"
        $diagArgs += $d
    }

    Write-Host "Running dotnet format analyzer verification..." -ForegroundColor DarkGray
    $rawOutput = & dotnet format NodeRadarPro.slnx analyzers --no-restore --verify-no-changes @diagArgs 2>&1
    $outputLines = $rawOutput -split "`r?`n"

    $report = [System.Collections.Generic.List[string]]::new()
    $report.Add("# NodeRadar Pro Roslyn Analyzer Diagnostic Report")
    $report.Add("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    $report.Add("Diagnostic Rule IDs: $($Diagnostics -join ', ')")
    $report.Add("--------------------------------------------------------------------------------")
    $report.Add("")

    $findingCount = 0
    foreach ($line in $outputLines) {
        foreach ($diag in $Diagnostics) {
            if ($line -match "\b$diag\b") {
                $report.Add($line.Trim())
                $findingCount++
                break
            }
        }
    }

    $report.Add("")
    $report.Add("--------------------------------------------------------------------------------")
    $report.Add("Total Findings: $findingCount")

    $report | Out-File -FilePath $outPathFull -Encoding utf8 -Force
    $sw.Stop()

    Write-Host "Report saved to $OutputPath ($findingCount findings, $([math]::Round($sw.Elapsed.TotalSeconds, 1))s)." -ForegroundColor Green
    if ($findingCount -gt 0) {
        Write-Host "Summary of Findings:" -ForegroundColor Yellow
        $report | Select-Object -First 25 | ForEach-Object { Write-Host $_ }
    }
} finally {
    Pop-Location
}
