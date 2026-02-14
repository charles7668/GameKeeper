@echo off
setlocal enabledelayedexpansion

REM ==============================================================================
REM Config
REM ==============================================================================
set "PROJECT_FILE=GameKeeper\GameKeeper.csproj"
set "OUTPUT_DIR=output"

REM ==============================================================================
REM 1. Get Project Version
REM ==============================================================================
echo [Info] Extracting version from %PROJECT_FILE%...

REM Using a PowerShell one-liner to extract Version or AssemblyVersion
REM Note: If <Version> is not explicitly set in csproj, it defaults to 1.0.0
set "VERSION=1.0.0"

for /f "usebackq tokens=*" %%v in (`powershell -Command "$xml = [xml](Get-Content '%PROJECT_FILE%'); if ($xml.Project.PropertyGroup.Version) { $xml.Project.PropertyGroup.Version } else { '1.0.0' }"`) do (
  set "VERSION=%%v"
)

echo [Info] Detected version: %VERSION%

set "ZIP_NAME=GameKeeper-v%VERSION%.zip"

REM ==============================================================================
REM 2. Check Output Directory
REM ==============================================================================
if not exist "%OUTPUT_DIR%" (
    echo [Error] Output directory "%OUTPUT_DIR%" does not exist. Please run build.bat first.
    exit /b 1
)

REM ==============================================================================
REM 3. Create Zip Archive
REM ==============================================================================
echo [Pack] Creating archive: %ZIP_NAME%...

if exist "%ZIP_NAME%" (
    del "%ZIP_NAME%"
)

REM Use PowerShell to zip the folder
powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\*' -DestinationPath '%ZIP_NAME%' -Force"

if %ERRORLEVEL% NEQ 0 (
    echo [Error] Failed to create zip archive.
    exit /b %ERRORLEVEL%
)

echo [Success] Successfully created %ZIP_NAME%
pause
