@echo off
setlocal

:menu
cls
echo ================================================
echo   ScreenNap Build Menu
echo ================================================
echo.
echo   1. Build                        (NET10.0)
echo   2. Installer                    (1 required)
echo   3. Full Build                   (1-2 sequential)
echo.
echo   9. Exit
echo.
echo ================================================
echo.

set "choice="
set /p choice="Select option (1-9): "

if "%choice%"=="1" goto build
if "%choice%"=="2" goto installer
if "%choice%"=="3" goto full_build
if "%choice%"=="9" goto exit
echo.
echo [ERROR] Invalid selection
timeout /t 2 > nul
goto menu

:build
echo.
echo ================================================
echo   Building ScreenNap...
echo ================================================
echo.
powershell -ExecutionPolicy Bypass -File Build.ps1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] ScreenNap build failed
    pause
    goto menu
)
echo.
echo [SUCCESS] ScreenNap build completed
echo Output: Build\ScreenNap\
pause
goto menu

:installer
echo.
echo ================================================
echo   Creating Installer...
echo ================================================
echo.
powershell -ExecutionPolicy Bypass -File Installer.ps1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Installer creation failed
    pause
    goto menu
)
echo.
echo [SUCCESS] Installer created
pause
goto menu

:full_build
echo.
echo ================================================
echo   Full Build: Build + Installer
echo ================================================
echo.

echo [1/2] Building ScreenNap...
echo.
powershell -ExecutionPolicy Bypass -File Build.ps1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] ScreenNap build failed
    pause
    goto menu
)
echo.
echo [SUCCESS] ScreenNap build completed
echo.

echo [2/2] Creating Installer...
echo.
powershell -ExecutionPolicy Bypass -File Installer.ps1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Installer creation failed
    pause
    goto menu
)

echo.
echo ================================================
echo   Full Build Completed
echo ================================================
echo.
echo [SUCCESS] All build and installer tasks completed
echo.
echo Output: Build\Installer\
pause
goto menu

:exit
echo.
echo Exiting...
exit /b 0
