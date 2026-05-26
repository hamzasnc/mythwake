@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0capture-portrait-screenshots.ps1" %*
set "EXITCODE=%ERRORLEVEL%"

endlocal & exit /b %EXITCODE%
