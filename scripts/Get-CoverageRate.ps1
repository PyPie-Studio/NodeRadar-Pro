#requires -Version 5.1
<#
.SYNOPSIS
    Computes the overall line-coverage percentage from all Cobertura files under a directory.
    Used by ci.yml and quality gates for the coverage ratchet gate.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/Get-CoverageRate.ps1 -Path TestResults
#>
[CmdletBinding()]
param(
    [string]$Path = "TestResults"
)

$total = 0
$covered = 0

Get-ChildItem -Path $Path -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue | ForEach-Object {
    [xml]$x = Get-Content -LiteralPath $_.FullName
    foreach ($line in $x.SelectNodes("//line")) {
        $total++
        if ([int]$line.hits -gt 0) { $covered++ }
    }
}

if ($total -eq 0) {
    Write-Error "No coverage.cobertura.xml files found under '$Path'"
    exit 2
}

$rate = [math]::Round(100.0 * $covered / $total, 2)
Write-Output $rate
