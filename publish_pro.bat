@echo off
setlocal enabledelayedexpansion

set "PROJECT_NAME=NodeRadar Pro"
set "PROJECT_DIR=%~dp0"
set "PUBLISH_DIR=%PROJECT_DIR%bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
set "OBFUSCATED_DIR=%PUBLISH_DIR%\Obfuscated"
set "RELEASES_DIR=%PROJECT_DIR%releases"
set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo [1/3] Cleaning and Publishing (.NET 10 x64)...
if exist "%RELEASES_DIR%" rd /s /q "%RELEASES_DIR%"
mkdir "%RELEASES_DIR%"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:PublishTrimmed=true

echo.
echo [2/3] Applying Obfuscar Protection...
set "OBF_EXE=%USERPROFILE%\.nuget\packages\obfuscar\2.2.50\tools\Obfuscar.Console.exe"
if not exist "!OBF_EXE!" set "OBF_EXE=%USERPROFILE%\.nuget\packages\obfuscar\2.2.39\tools\Obfuscar.Console.exe"
"!OBF_EXE!" "%PROJECT_DIR%obfuscar.xml"
copy /y "%OBFUSCATED_DIR%\%PROJECT_NAME%.dll" "%PUBLISH_DIR%\%PROJECT_NAME%.dll"

echo.
echo [3/3] Compiling Inno Setup Installer...
if exist "%ISCC%" (
    "!ISCC!" "!PROJECT_DIR!Inno\installer.iss"
) else (
    echo [ERROR] Inno Setup compiler not found at "%ISCC%"
)

echo.
echo ============================================================================
echo RELEASE COMPLETE: Check the 'releases' folder.
echo ============================================================================
pause