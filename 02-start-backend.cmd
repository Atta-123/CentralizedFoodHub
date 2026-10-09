@echo off
setlocal
cd /d "%~dp0"
title FoodHub Backend - port 5180
if not exist "src\FoodHub.Api\FoodHub.Api.csproj" goto missingfiles
where dotnet >nul 2>nul
if errorlevel 1 goto missingdotnet
dotnet --version | findstr /b /c:"10." >nul
if errorlevel 1 goto missingdotnet
echo Restoring FoodHub API and its referenced projects...
dotnet restore "src\FoodHub.Api\FoodHub.Api.csproj"
if errorlevel 1 goto restorefailed
echo.
echo Building FoodHub...
dotnet build "src\FoodHub.Api\FoodHub.Api.csproj" --no-restore
if errorlevel 1 goto buildfailed
set "ASPNETCORE_ENVIRONMENT=Development"
echo.
echo Starting API: http://localhost:5180
echo Swagger: http://localhost:5180/swagger
echo SQL Server must be running. Configure ConnectionStrings:SqlServer in appsettings.json.
echo Development startup applies included migrations and creates demo data.
echo Keep this window open while using the app.
dotnet run --no-build --no-launch-profile --project "src\FoodHub.Api\FoodHub.Api.csproj" --urls "http://localhost:5180"
set "foodhub_exit=%errorlevel%"
echo.
echo API stopped. If an error appeared above, copy the full message.
echo If port 5180 is busy, stop the app using it and retry. This script does not kill other apps.
pause
exit /b %foodhub_exit%
:missingfiles
echo ERROR: Extract the complete ZIP. The src\FoodHub.Api project is missing.
goto failed
:missingdotnet
echo ERROR: The .NET 10 SDK is required. Run 01-check-setup.cmd for details.
echo Download: https://dotnet.microsoft.com/download/dotnet/10.0
goto failed
:restorefailed
echo ERROR: Package restore failed. Check internet access and the NuGet error printed above.
goto failed
:buildfailed
echo ERROR: Build failed. Copy the first compiler error above, including its file and line number.
goto failed
:failed
pause
exit /b 1
