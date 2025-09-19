@echo off
echo ========================================
echo CrowbaneArena - Remote Event Controller
echo Build Script v2.0.0
echo ========================================

:: Check if dotnet is available
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET 6.0 SDK or later.
    pause
    exit /b 1
)

:: Clean previous builds
echo Cleaning previous builds...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
if exist "release" rmdir /s /q "release"

:: Create release directory
mkdir release

:: Build the project
echo Building CrowbaneArena...
dotnet build --configuration Release --output "release\build"

if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

:: Copy necessary files to release
echo Preparing release package...
copy "README.md" "release\"
copy ".gitignore" "release\"

:: Create libs directory structure
mkdir "release\libs"
echo NOTE: Copy the required V Rising DLL files to release\libs\ directory:
echo - BepInEx.Core.dll
echo - BepInEx.Unity.IL2CPP.dll
echo - UnityEngine.dll
echo - UnityEngine.CoreModule.dll
echo - Unity.Mathematics.dll
echo - Unity.Entities.dll
echo - Unity.Collections.dll
echo - ProjectM.dll
echo - ProjectM.Shared.dll
echo - Stunlock.Core.dll
echo - VampireCommandFramework.dll
echo - 0Harmony.dll

:: Create installation instructions
echo Creating installation instructions...
(
echo # CrowbaneArena Installation Instructions
echo.
echo ## Prerequisites
echo 1. V Rising server with BepInEx installed
echo 2. VampireCommandFramework ^(VCF^) mod installed
echo.
echo ## Installation Steps
echo 1. Copy CrowbaneArena.dll to your BepInEx/plugins/ directory
echo 2. Restart your V Rising server
echo 3. Check logs for successful initialization
echo.
echo ## Verification
echo Use the command: .event status
echo You should see system status information
echo.
echo ## Quick Start
echo 1. .event create demo_arena "My Arena" "Test arena"
echo 2. .event enter demo_arena event_warrior
echo 3. .event help
echo.
echo For complete documentation, see README.md
) > "release\INSTALLATION.md"

echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Release files are in the 'release' directory:
echo - CrowbaneArena.dll ^(main plugin file^)
echo - README.md ^(complete documentation^)
echo - INSTALLATION.md ^(installation guide^)
echo.
echo IMPORTANT: Copy the required V Rising DLL files to release\libs\
echo before distributing the mod.
echo.
pause