@echo off
REM Launch create-release.ps1 - double-click this file to create a release
cd /d "%~dp0\.."
powershell -ExecutionPolicy Bypass -File "%~dp0create-release.ps1" %*
pause
