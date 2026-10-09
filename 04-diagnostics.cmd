@echo off
setlocal
cd /d "%~dp0"
(
  echo FoodHub setup diagnostics
  echo.
  echo Installed SDKs:
  dotnet --list-sdks
  echo Selected SDK:
  dotnet --version
  echo Node:
  node --version
  echo npm:
  call npm --version
  echo.
  if exist "FoodHub.sln" echo Solution file found.
  if exist "src\FoodHub.Api\FoodHub.Api.csproj" echo API project found.
  if exist "frontend\package.json" echo Angular package file found.
  echo.
  echo API health:
  curl --max-time 5 --silent --show-error http://localhost:5180/health
) > setup-diagnostics.txt 2>&1
type setup-diagnostics.txt
echo.
echo Saved: setup-diagnostics.txt. Copy the exact backend startup error too.
pause
