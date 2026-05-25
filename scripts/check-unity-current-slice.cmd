@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-unity-current-slice.ps1" %*
set "MYTHWAKE_UNITY_VALIDATION_EXIT=%ERRORLEVEL%"

endlocal & exit /b %MYTHWAKE_UNITY_VALIDATION_EXIT%
