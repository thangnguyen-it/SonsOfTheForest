@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-R2Perf1Pair.ps1" %*
exit /b %ERRORLEVEL%
