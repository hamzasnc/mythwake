@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-postgres-e2e.ps1" %*
set "MYTHWAKE_CHECK_POSTGRES_EXIT=%ERRORLEVEL%"

endlocal & exit /b %MYTHWAKE_CHECK_POSTGRES_EXIT%
