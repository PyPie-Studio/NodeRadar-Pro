@echo off
setlocal enabledelayedexpansion

title NodeRadar Pro - Automated Deployment Pipeline

echo ============================================================================
echo   NodeRadar Pro: Automated Build, Test, Protection and Release Pipeline
echo ============================================================================
echo.

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

where pwsh.exe >nul 2>&1
if %errorlevel% equ 0 (
    set "PS_EXE=pwsh.exe"
) else (
    set "PS_EXE=powershell.exe"
)

echo [INFO] Using PowerShell Host: !PS_EXE!
echo.

echo [1/4] Updating IEEE MAC OUI Device Vendor Database...
!PS_EXE! -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\Update-OuiDatabase.ps1"
if !errorlevel! neq 0 (
    echo.
    echo [ERROR] OUI Database update failed!
    goto :FAILED
)
echo.

echo [2/4] Executing Master Quality Gate (Build + Style + Analyzers + Security)...
!PS_EXE! -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\Test-MasterGate.ps1"
if !errorlevel! neq 0 (
    echo.
    echo [ERROR] Master Quality Gate failed! Fix findings before deploying.
    goto :FAILED
)
echo.

echo [3/4] Executing Complete Test Suite (Unit + Integration Tests)...
dotnet test "%SCRIPT_DIR%NodeRadarPro.slnx" -c Release --nologo
if !errorlevel! neq 0 (
    echo.
    echo [ERROR] Test suite execution failed!
    goto :FAILED
)
echo.

echo [4/4] Building Release Package (Publish + Obfuscar IL Shield + Inno Setup)...
!PS_EXE! -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\Build-ReleasePackage.ps1"
if !errorlevel! neq 0 (
    echo.
    echo [ERROR] Release packaging failed!
    goto :FAILED
)
echo.

echo ============================================================================
echo   DEPLOYMENT ARTIFACTS GENERATED SUCCESSFULLY
echo ============================================================================
echo.
echo Artifacts in releases\:
!PS_EXE! -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%SCRIPT_DIR%releases' -Filter '*.exe' | ForEach-Object { $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash; Write-Host ('  - ' + $_.Name + ' (' + [math]::Round($_.Length / 1MB, 2) + ' MB)') -ForegroundColor Green; Write-Host ('    SHA-256: ' + $hash) -ForegroundColor DarkGray }"
echo.
echo [READY] Upload installer and SHA-256 hash to GitHub Releases:
echo         https://github.com/PyPie-Studio/NodeRadar-Pro/releases/new
echo.
goto :SUCCESS

:FAILED
echo.
echo ============================================================================
echo   [DEPLOYMENT FAILED] Aborted pipeline due to errors above.
echo ============================================================================
echo.
if "%~1"=="" pause
exit /b 1

:SUCCESS
if "%~1"=="" pause
exit /b 0
