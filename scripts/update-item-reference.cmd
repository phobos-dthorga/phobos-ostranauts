@echo off
pwsh.exe -NoProfile -File "%~dp0update-item-reference.ps1" %*
if errorlevel 1 (
    echo Item reference update failed. Read the error above.
    pause
    exit /b 1
)
echo Item reference update complete.
pause
