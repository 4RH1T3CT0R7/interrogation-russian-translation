@echo off
chcp 65001 >nul
setlocal

echo ============================================================
echo   Interrogation Russian Localization - Build Script
echo ============================================================
echo.

set REPO_DIR=%~dp0
set INSTALLER_SRC=%REPO_DIR%src\Installer

:: ---- Step 1: Package data.zip ----
echo [1/2] Упаковка data.zip (переводы + шрифты)...
if exist "%INSTALLER_SRC%\data.zip" del "%INSTALLER_SRC%\data.zip"

%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -Command ^
    "Compress-Archive -Path '%REPO_DIR%translated', '%REPO_DIR%font_test' -DestinationPath '%INSTALLER_SRC%\data.zip' -Force"

if errorlevel 1 (
    echo [!] Не удалось создать data.zip!
    goto :error
)
echo      OK
echo.

:: ---- Step 2: Build Installer ----
echo [2/2] Сборка установщика...
dotnet build "%INSTALLER_SRC%" -c Release
if errorlevel 1 (
    echo [!] Сборка установщика не удалась!
    goto :error
)

:: Copy output to repo root
copy /Y "%INSTALLER_SRC%\bin\Release\net472\InterrogationRussian-Setup.exe" "%REPO_DIR%" >nul 2>&1
echo      OK
echo.

echo ============================================================
echo   Готово!
echo ============================================================
echo.
echo   Установщик: %REPO_DIR%InterrogationRussian-Setup.exe
echo.
goto :end

:error
echo.
echo Сборка прервана из-за ошибки.
echo.

:end
pause
