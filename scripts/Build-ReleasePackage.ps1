#requires -Version 5.1
<#
.SYNOPSIS
    Builds, obfuscates, and packages NodeRadar Pro into an Inno Setup installer.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/Build-ReleasePackage.ps1
#>
[CmdletBinding()]
param(
    [switch]$SkipInno
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$projectName = "NodeRadarPro"
$projectPath = Join-Path $root "src\NodeRadarPro\NodeRadarPro.csproj"
$publishDir = Join-Path $root "src\NodeRadarPro\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
$obfuscatedDir = Join-Path $publishDir "Obfuscated"
$releasesDir = Join-Path $root "releases"
$innoScript = Join-Path $root "Inno\installer.iss"
$isccCandidates = @(
    (Get-Command "ISCC.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe"),
    "C:\Program Files\Inno Setup 7\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 7\ISCC.exe"
)
$isccPath = $isccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $isccPath) {
    $isccPath = (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe")
}


$sw = [System.Diagnostics.Stopwatch]::StartNew()
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "   NodeRadar Pro: Release Packaging & Protection Pipeline   " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# Step 1: Clean & Publish
Write-Host "`n[1/3] Publishing .NET 10 (win-x64, Self-Contained, Trimmed)..." -ForegroundColor Yellow
if (Test-Path $releasesDir) {
    Remove-Item -Path $releasesDir -Recurse -Force | Out-Null
}
New-Item -ItemType Directory -Path $releasesDir -Force | Out-Null

Push-Location $root
try {
    & dotnet publish $projectPath -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:PublishTrimmed=false --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Dotnet publish failed with exit code $LASTEXITCODE"
    }
} finally {
    Pop-Location
}

# Step 2: Obfuscation with Obfuscar
Write-Host "`n[2/3] Applying Obfuscar IL Protection..." -ForegroundColor Yellow
$nugetPackages = Join-Path $env:USERPROFILE ".nuget\packages\obfuscar"
$obfExe = (Get-ChildItem -Path $nugetPackages -Filter "Obfuscar.Console.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1).FullName

if (-not $obfExe -or -not (Test-Path $obfExe)) {
    Write-Host "[WARNING] Obfuscar.Console.exe not found in NuGet cache. Skipping IL obfuscation." -ForegroundColor DarkYellow
} else {
    Write-Host "Using Obfuscar: $obfExe" -ForegroundColor DarkGray
    Push-Location $root
    try {
        & "$obfExe" "obfuscar.xml"
        if ($LASTEXITCODE -ne 0) {
            throw "Obfuscar failed with exit code $LASTEXITCODE"
        }
        $obfDll = Join-Path $obfuscatedDir "$projectName.dll"
        if (Test-Path $obfDll) {
            Copy-Item -Path $obfDll -Destination (Join-Path $publishDir "$projectName.dll") -Force
            Write-Host "Obfuscated binary successfully replaced in publish directory." -ForegroundColor Green
        }
    } finally {
        Pop-Location
    }
}

# Step 3: Compile Inno Setup Installer
if (-not $SkipInno) {
    Write-Host "`n[3/3] Compiling Inno Setup Installer..." -ForegroundColor Yellow
    if (Test-Path $isccPath) {
        & "$isccPath" "$innoScript"
        if ($LASTEXITCODE -ne 0) {
            throw "Inno Setup compiler failed with exit code $LASTEXITCODE"
        }
        Write-Host "Installer successfully built in $releasesDir." -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Inno Setup compiler not found at $isccPath. Skipping installer compilation." -ForegroundColor DarkYellow
    }
} else {
    Write-Host "`n[3/3] Skipped Inno Setup (SkipInno switch)." -ForegroundColor DarkGray
}

$sw.Stop()
Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host " RELEASE PIPELINE COMPLETE ($([math]::Round($sw.Elapsed.TotalSeconds, 1))s) " -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan
