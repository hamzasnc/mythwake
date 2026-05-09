@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-backend.ps1" %*

endlocal
