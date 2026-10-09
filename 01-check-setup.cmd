@echo off
setlocal
cd /d "%~dp0"
echo FoodHub setup check
echo Folder: %CD%
echo.
if not exist "FoodHub.sln" goto missingfiles
where dotnet >nul 2>nul
if errorlevel 1 goto missingdotnet
echo Installed .NET SDKs:
dotnet --list-sdks
echo.
echo Selected SDK for this project:
dotnet --version
if errorlevel 1 goto missingdotnet
dotnet --version | findstr /b /c:"10." >nul
if errorlevel 1 goto wrongsdk
where node >nul 2>nul
if errorlevel 1 goto missingnode
node scripts\check-node.cjs
if errorlevel 1 goto missingnode
where npm >nul 2>nul
if errorlevel 1 goto missingnpm
call npm --version
echo.
echo Setup checks passed. Run 02-start-backend.cmd, then 03-start-frontend.cmd.
echo API: http://localhost:5180/swagger
echo App: http://localhost:4300
pause
exit /b 0
:missingfiles
echo ERROR: Extract the entire ZIP. This folder must contain FoodHub.sln, src and frontend.
goto failed
:missingdotnet
echo ERROR: Install the .NET 10 SDK, then reopen this window. Runtime alone is insufficient.
echo Download: https://dotnet.microsoft.com/download/dotnet/10.0
goto failed
:wrongsdk
echo ERROR: This project targets .NET 10. Keep the included global.json and install a .NET 10 SDK.
goto failed
:missingnode
echo ERROR: Install compatible Node.js: 20.19+, 22.12+, or 24.x. Node 22 LTS is recommended.
echo Download: https://nodejs.org/en/download
goto failed
:missingnpm
echo ERROR: npm was not found. Reinstall Node.js with npm enabled.
goto failed
:failed
echo.
echo Read START-HERE.md. Copy the exact error message if you need help.
pause
exit /b 1
