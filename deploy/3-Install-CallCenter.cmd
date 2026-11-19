@echo off
rem Falah installer - runs the PowerShell script beside this file; Windows will ask for administrator permission.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Station.ps1" -Role CallCenter
pause
