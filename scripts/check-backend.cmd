@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-backend.ps1" %*
set "MYTHWAKE_CHECK_BACKEND_EXIT=%ERRORLEVEL%"

endlocal & exit /b %MYTHWAKE_CHECK_BACKEND_EXIT%
