@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Deploy-NodeRadar.ps1 %*
if "%~1"=="" (
    echo.
    pause
)
