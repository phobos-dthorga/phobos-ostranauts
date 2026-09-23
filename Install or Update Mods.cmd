@echo off
where pwsh.exe >nul 2>nul
if errorlevel 1 (
    echo PowerShell 7 is required. Install it, then run this file again.
    pause
    exit /b 1
)
echo Installing/updating prepared Phobos Auto Nav and Shipbreaker packages.
echo Please close Ostranauts first. Other mods and saves will be left alone.
pwsh.exe -NoLogo -NoProfile -File "%~dp0scripts\install-mods.ps1" %*
set "PhobosInstallExit=%ERRORLEVEL%"
echo.
if not "%PhobosInstallExit%"=="0" echo Installation did not complete. See the message above.
pause
exit /b %PhobosInstallExit%
