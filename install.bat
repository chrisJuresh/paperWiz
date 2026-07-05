@echo off
title Install paperWiz (Windows)
cd /d "%~dp0"
echo Installing paperWiz for Windows...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0windows\install.ps1" %*
echo.
pause
