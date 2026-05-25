@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-backend.ps1" %*
set "MYTHWAKE_START_BACKEND_EXIT=%ERRORLEVEL%"

endlocal & exit /b %MYTHWAKE_START_BACKEND_EXIT%
