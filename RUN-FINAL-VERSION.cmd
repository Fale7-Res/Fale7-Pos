@echo off
setlocal
cd /d "%~dp0"
title Falah Restaurant OS - Final Version
echo.
echo Starting the FINAL modified version...
echo Open: http://127.0.0.1:3123
echo.
start "" powershell -NoProfile -WindowStyle Hidden -Command "Start-Sleep -Seconds 5; Start-Process 'http://127.0.0.1:3123'"
node node_modules\next\dist\bin\next dev -H 127.0.0.1 -p 3123
if errorlevel 1 (
  echo.
  echo The application could not start. Make sure Node.js is installed.
  pause
)
