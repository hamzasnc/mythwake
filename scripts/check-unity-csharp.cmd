@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-unity-csharp.ps1" %*
set "MYTHWAKE_UNITY_CSHARP_EXIT=%ERRORLEVEL%"

endlocal & exit /b %MYTHWAKE_UNITY_CSHARP_EXIT%
