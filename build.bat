@echo off
setlocal

REM ==============================================================================
REM 1. Find MSBuild
REM ==============================================================================
set "MSBUILD_PATH="
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
  set "MSBUILD_PATH=%%i"
)

if not defined MSBUILD_PATH (
  echo [Error] MSBuild not found. Please ensure Visual Studio is installed.
  exit /b 1
)

echo [Info] Found MSBuild at: %MSBUILD_PATH%
echo.

REM ==============================================================================
REM 1.1. Read GameKeeper target framework
REM ==============================================================================
set "GAMEKEEPER_TARGET_FRAMEWORK="
for /f "tokens=3 delims=<>" %%i in ('findstr /i /c:"<TargetFramework>" "GameKeeper\GameKeeper.csproj"') do (
  set "GAMEKEEPER_TARGET_FRAMEWORK=%%i"
)

if not defined GAMEKEEPER_TARGET_FRAMEWORK (
  echo [Error] Failed to read TargetFramework from GameKeeper\GameKeeper.csproj.
  exit /b 1
)

echo [Info] GameKeeper target framework: %GAMEKEEPER_TARGET_FRAMEWORK%
echo.

REM ==============================================================================
REM 1.2. Resolve GameKeeper output directory
REM ==============================================================================
set "GAMEKEEPER_OUTPUT_DIR=GameKeeper\bin\x86\Release\%GAMEKEEPER_TARGET_FRAMEWORK%"

REM ==============================================================================
REM 1.5. Prepare Output Directory (Clean before build)
REM ==============================================================================
echo [Info] Cleaning output folder...
if exist "output" (
  rmdir /s /q "output"
)
mkdir "output"

echo [Info] Cleaning release folders...
if exist "Release" (
    rmdir /s /q "Release"
)
if exist "x64\Release" (
    rmdir /s /q "x64\Release"
)

REM ==============================================================================
REM 2. Build DllLoader (C++)
REM    x86 (Win32) and x64
REM ==============================================================================
echo [Build] DllLoader (x86)...
"%MSBUILD_PATH%" "DllLoader\DllLoader.vcxproj" -t:build -p:Configuration=Release -p:Platform=Win32
if %ERRORLEVEL% NEQ 0 ( echo [Error] Failed to build DllLoader x86 & exit /b %ERRORLEVEL% )

echo [Build] DllLoader (x64)...
"%MSBUILD_PATH%" "DllLoader\DllLoader.vcxproj" -t:build -p:Configuration=Release -p:Platform=x64
if %ERRORLEVEL% NEQ 0 ( echo [Error] Failed to build DllLoader x64 & exit /b %ERRORLEVEL% )

REM ==============================================================================
REM 3. Build GameKeeperCore (C++)
REM    x86 (Win32) and x64
REM ==============================================================================
echo [Build] GameKeeperCore (x86)...
"%MSBUILD_PATH%" "GameKeeperCore\GameKeeperCore.vcxproj" -t:build -p:Configuration=Release -p:Platform=Win32
if %ERRORLEVEL% NEQ 0 ( echo [Error] Failed to build GameKeeperCore x86 & exit /b %ERRORLEVEL% )

echo [Build] GameKeeperCore (x64)...
"%MSBUILD_PATH%" "GameKeeperCore\GameKeeperCore.vcxproj" -t:build -p:Configuration=Release -p:Platform=x64
if %ERRORLEVEL% NEQ 0 ( echo [Error] Failed to build GameKeeperCore x64 & exit /b %ERRORLEVEL% )

REM ==============================================================================
REM 4. Build Injector (C# .NET 8)
REM    x86 and x64
REM ==============================================================================
echo [Build] Injector (x86)...
"%MSBUILD_PATH%" "Injector\Injector.csproj" -t:build -p:Configuration=Release -p:Platform=x86 -restore
if %ERRORLEVEL% NEQ 0 ( echo [Error] Failed to build Injector x86 & exit /b %ERRORLEVEL% )

echo [Build] Injector (x64)...
"%MSBUILD_PATH%" "Injector\Injector.csproj" -t:build -p:Configuration=Release -p:Platform=x64 -restore
if %ERRORLEVEL% NEQ 0 ( echo [Error] Failed to build Injector x64 & exit /b %ERRORLEVEL% )

REM ==============================================================================
REM 5. Build GameKeeper (C# .NET 8)
REM    x86 only
REM ==============================================================================
echo [Build] GameKeeper (x86)...
"%MSBUILD_PATH%" "GameKeeper\GameKeeper.csproj" -t:build -p:Configuration=Release -p:Platform=x86 -restore
if %ERRORLEVEL% NEQ 0 ( echo [Error] Failed to build GameKeeper x86 & exit /b %ERRORLEVEL% )

REM ==============================================================================
REM 6. Prepare Output Directory (Already done)
REM ==============================================================================
echo.

REM ==============================================================================
REM 7. Copy Artifacts
REM ==============================================================================

REM 7.1 Copy Main App (GameKeeper x86)
echo [Deploy] Copying GameKeeper (x86)...
if not exist "%GAMEKEEPER_OUTPUT_DIR%\GameKeeper.exe" (
  set "GAMEKEEPER_OUTPUT_DIR="
  for /d %%d in ("GameKeeper\bin\x86\Release\*") do (
    if not defined GAMEKEEPER_OUTPUT_DIR if exist "%%~fd\GameKeeper.exe" set "GAMEKEEPER_OUTPUT_DIR=%%~fd"
  )
)
if not defined GAMEKEEPER_OUTPUT_DIR (
  echo [Error] GameKeeper x86 output not found under GameKeeper\bin\x86\Release.
  exit /b 1
)
if not exist "%GAMEKEEPER_OUTPUT_DIR%\GameKeeper.exe" (
  echo [Error] GameKeeper.exe not found in "%GAMEKEEPER_OUTPUT_DIR%".
  exit /b 1
)
xcopy /Y /S /E "%GAMEKEEPER_OUTPUT_DIR%\*.*" "output\" >nul
if %ERRORLEVEL% GEQ 2 ( echo [Error] Failed to copy GameKeeper x86 output & exit /b %ERRORLEVEL% )

REM 7.2 Copy x64 Components (Manually, since GameKeeper x86 build won't copy them)
echo [Deploy] Copying x64 components...
copy /Y "x64\Release\DllLoader_x64.dll" "output\" >nul
copy /Y "x64\Release\GameKeeperCore_x64.dll" "output\" >nul
copy /Y "x64\Release\Injector_x64.exe" "output\" >nul
copy /Y "x64\Release\Injector_x64.dll" "output\" >nul
copy /Y "x64\Release\Injector_x64.deps.json" "output\" >nul
copy /Y "x64\Release\Injector_x64.runtimeconfig.json" "output\" >nul

REM 7.3 Ensure x86 components are present (GameKeeper build should have them, but overwrite to be safe)
echo [Deploy] Verifying x86 components...
copy /Y "Release\DllLoader_x86.dll" "output\" >nul
copy /Y "Release\GameKeeperCore_x86.dll" "output\" >nul
copy /Y "Release\Injector_x86.exe" "output\" >nul
copy /Y "Release\Injector_x86.dll" "output\" >nul
copy /Y "Release\Injector_x86.deps.json" "output\" >nul
copy /Y "Release\Injector_x86.runtimeconfig.json" "output\" >nul

echo.
echo [Success] Build and deployment complete. Output is in "output" folder.
pause
