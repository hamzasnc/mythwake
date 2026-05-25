@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-unity-current-slice.ps1" %*

endlocal
