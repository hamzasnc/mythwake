@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-postgres-e2e.ps1" %*
