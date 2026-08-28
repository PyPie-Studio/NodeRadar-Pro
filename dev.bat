@echo off
cd /d "%~dp0"
echo Starting NodeRadar Pro in local development mode...
dotnet run --project "NodeRadar Pro.csproj" %*
