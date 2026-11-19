@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
py -3 "%SCRIPT_DIR%Falah-Installer-Workshop.py"
if errorlevel 1 (
    echo.
    echo Could not start the workshop. Install Python 3 with Tcl/Tk, or run the .py file with Python.
    pause
)
endlocal
