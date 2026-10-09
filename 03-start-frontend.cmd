@echo off
setlocal
cd /d "%~dp0"
title FoodHub Angular - port 4300
where node >nul 2>nul
if errorlevel 1 goto missingnode
node scripts\check-node.cjs
if errorlevel 1 goto missingnode
where npm >nul 2>nul
if errorlevel 1 goto missingnode
cd frontend
if not exist "package.json" goto missingfiles
echo Installing the pinned Angular dependencies...
call npm ci
if errorlevel 1 goto installfailed
echo.
echo Open http://localhost:4300 after Angular starts.
echo Keep the backend window open too.
call npm start
set "foodhub_exit=%errorlevel%"
echo Angular stopped. Copy any error above if needed.
pause
exit /b %foodhub_exit%
:missingnode
echo ERROR: Compatible Node.js and npm are required. Run 01-check-setup.cmd.
goto failed
:missingfiles
echo ERROR: Extract the complete ZIP; frontend\package.json is missing.
goto failed
:installfailed
echo ERROR: npm ci failed. Copy the error above; check Node version and internet access.
goto failed
:failed
pause
exit /b 1
