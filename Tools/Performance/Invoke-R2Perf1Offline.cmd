@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-R2Perf1Offline.ps1" %*
exit /b %ERRORLEVEL%
