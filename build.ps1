# CrowbaneArena - Remote Event Controller Build Script v2.0.0
# PowerShell build script for cross-platform compatibility

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CrowbaneArena - Remote Event Controller" -ForegroundColor Cyan
Write-Host "PowerShell Build Script v2.0.0" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check if dotnet is available
try {
    $dotnetVersion = dotnet --version
    Write-Host "Using .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: .NET SDK not found. Please install .NET 6.0 SDK or later." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }
if (Test-Path "release") { Remove-Item -Recurse -Force "release" }

# Create release directory
New-Item -ItemType Directory -Path "release" -Force | Out-Null

# Build the project
Write-Host "Building CrowbaneArena..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Release --output "release/build" --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Copy necessary files to release
Write-Host "Preparing release package..." -ForegroundColor Yellow
Copy-Item "README.md" "release/"
Copy-Item ".gitignore" "release/"

# Create libs directory structure
New-Item -ItemType Directory -Path "release/libs" -Force | Out-Null

# Create dependency list
$dependencies = @(
    "BepInEx.Core.dll",
    "BepInEx.Unity.IL2CPP.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "Unity.Mathematics.dll",
    "Unity.Entities.dll",
    "Unity.Collections.dll",
    "ProjectM.dll",
    "ProjectM.Shared.dll",
    "Stunlock.Core.dll",
    "VampireCommandFramework.dll",
    "0Harmony.dll"
)

Write-Host "NOTE: Copy the following required V Rising DLL files to release/libs/ directory:" -ForegroundColor Magenta
foreach ($dep in $dependencies) {
    Write-Host "  - $dep" -ForegroundColor White
}

# Create installation instructions
Write-Host "Creating installation instructions..." -ForegroundColor Yellow
$installInstructions = @"
# CrowbaneArena Installation Instructions

## Prerequisites
1. V Rising server with BepInEx installed
2. VampireCommandFramework (VCF) mod installed

## Installation Steps
1. Copy CrowbaneArena.dll to your BepInEx/plugins/ directory
2. Restart your V Rising server
3. Check logs for successful initialization

## Verification
Use the command: .event status
You should see system status information

## Quick Start Commands
1. `.event create demo_arena "My Arena" "Test arena"`
2. `.event enter demo_arena event_warrior`
3. `.event help`

## Available Character Templates
- `event_warrior` - Balanced melee fighter (1000 HP, sword & shield)
- `event_mage` - Magical spellcaster (600 HP, high energy, staff)
- `event_archer` - Ranged fighter (800 HP, bow & arrows)

## Default Demo Event
The system automatically creates 'demo_arena' with:
- Complete quest chain (collect gems → activate levers → defeat boss)
- Guardian boss encounter
- Entry/exit gates with character swapping

## Command Categories (20+ commands available)
- **Event Management**: create, stop, list, info, reset, status
- **Player Management**: enter, exit, kick, players, heal, damage
- **Character Templates**: template list/create/delete/info
- **Teleportation**: teleport, setteleporter, tphere
- **Boss Management**: spawnboss, killboss, bossinfo
- **Quest Management**: assignquest, completequest, questprogress
- **Trigger Zones**: createzone, deletezone, listzones
- **Character Swapping**: swap, restore, swapstats
- **Utilities**: setflag, getflag, giveitem, removeitem, message, broadcast, checkpoint, loadcheckpoint

For complete documentation, see README.md

## Troubleshooting
- Check BepInEx logs for initialization messages
- Verify VampireCommandFramework is installed
- Use `.event status` to check system health
- Use `.event help` for command reference

## System Features
- ✅ Complete character swapping with safe state management
- ✅ Dynamic quest system with objective tracking
- ✅ Multi-phase boss encounters with loot tables
- ✅ Trigger zones for seamless event flow
- ✅ Real-time monitoring and health checks
- ✅ Emergency recovery and backup systems
- ✅ Comprehensive administrative tools
"@

$installInstructions | Out-File -FilePath "release/INSTALLATION.md" -Encoding UTF8

# Create version info file
$versionInfo = @"
CrowbaneArena - Remote Event Controller v2.0.0

Build Information:
- Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC")
- .NET Version: $dotnetVersion
- Target Framework: net6.0
- Build Configuration: Release

System Requirements:
- V Rising server with BepInEx
- VampireCommandFramework (VCF)
- .NET 6.0 Runtime

Features:
- Character Swapping System
- Quest Management
- Boss Encounters
- Teleportation & Trigger Zones
- 20+ Administrative Commands
- Real-time Monitoring
- Emergency Recovery Tools

For support and documentation, see README.md
"@

$versionInfo | Out-File -FilePath "release/VERSION.txt" -Encoding UTF8

# Check if main DLL was built
$mainDll = "release/build/CrowbaneArena.dll"
if (Test-Path $mainDll) {
    $dllSize = (Get-Item $mainDll).Length
    Write-Host "✅ CrowbaneArena.dll built successfully ($([math]::Round($dllSize/1KB, 2)) KB)" -ForegroundColor Green
} else {
    Write-Host "❌ CrowbaneArena.dll not found in build output!" -ForegroundColor Red
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Release files are in the 'release' directory:" -ForegroundColor White
Write-Host "- CrowbaneArena.dll (main plugin file)" -ForegroundColor White
Write-Host "- README.md (complete documentation)" -ForegroundColor White
Write-Host "- INSTALLATION.md (installation guide)" -ForegroundColor White
Write-Host "- VERSION.txt (build information)" -ForegroundColor White
Write-Host ""
Write-Host "IMPORTANT: Copy the required V Rising DLL files to release/libs/" -ForegroundColor Yellow
Write-Host "before distributing the mod." -ForegroundColor Yellow
Write-Host ""

# Pause for user input
Read-Host "Press Enter to exit"