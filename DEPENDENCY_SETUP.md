# V Rising CrowbaneArena - Dependency Setup Guide

```
   ▄████▄   ██▀███   ▒█████   █     █░ ▄▄▄▄    ▄▄▄       ███▄    █ ▓█████
  ▒██▀ ▀█  ▓██ ▒ ██▒▒██▒  ██▒▓█░ █ ░█░▓█████▄ ▒████▄     ██ ▀█   █ ▓█   ▀
  ▒▓█    ▄ ▓██ ░▄█ ▒▒██░  ██▒▒█░ █ ░█ ▒██▒ ▄██▒██  ▀█▄  ▓██  ▀█ ██▒▒███
  ▒▓▓▄ ▄██▒▒██▀▀█▄  ▒██   ██░░█░ █ ░█ ▒██░█▀  ░██▄▄▄▄██ ▓██▒  ▐▌██▒▒▓█  ▄
  ▒ ▓███▀ ░░██▓ ▒██▒░ ████▓▒░░░██▒██▓ ░▓█  ▀█▓ ▓█   ▓██▒▒██░   ▓██░░▒████▒
  ░ ░▒ ▒  ░░ ▒▓ ░▒▓░░ ▒░▒░▒░ ░ ▓░▒ ▒  ░▒▓███▀▒ ▒▒   ▓▒█░░ ▒░   ▒ ▒ ░░ ▒░ ░
    ░  ▒     ░▒ ░ ▒░  ░ ▒ ▒░   ▒ ░ ░  ▒░▒   ░   ▒   ▒▒ ░░ ░░   ░ ▒░ ░ ░  ░
  ░          ░░   ░ ░ ░ ░ ▒    ░   ░   ░    ░   ░   ▒      ░   ░ ░    ░
  ░ ░         ░         ░ ░      ░     ░            ░  ░         ░    ░  ░
  ░                                         ░
                        🏟️ ARENA EVENT CONTROLLER 🏟️
```

![CrowbaneArena Logo](Logo.png)

*Remote Event Controller for V Rising - Complete event management system*

## Overview
This guide will help you set up all required dependencies for the CrowbaneArena V Rising mod, following V Rising technical guidelines and best practices.

## Required Dependencies

### 1. BepInEx Framework
**Location**: V Rising game installation folder
- `BepInEx.Core.dll`
- `BepInEx.Unity.IL2CPP.dll`

**Path**: `[V Rising Install]/BepInEx/core/`

### 2. Unity Engine Libraries
**Location**: V Rising game installation folder
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `Unity.Mathematics.dll`
- `Unity.Entities.dll`
- `Unity.Collections.dll`

**Path**: `[V Rising Install]/VRising_Data/Managed/`

### 3. V Rising Game Libraries
**Location**: V Rising game installation folder
- `ProjectM.dll` - Core V Rising game logic
- `ProjectM.Shared.dll` - Shared V Rising components
- `Stunlock.Core.dll` - Stunlock Studios core utilities

**Path**: `[V Rising Install]/VRising_Data/Managed/`

### 4. Modding Framework
**Location**: BepInEx plugins or manual download
- `VampireCommandFramework.dll` - Command framework for V Rising
- `0Harmony.dll` - Harmony patching library

**Paths**:
- VCF: `[V Rising Install]/BepInEx/plugins/VampireCommandFramework/`
- Harmony: `[V Rising Install]/BepInEx/core/`

## Setup Instructions

### Step 1: Locate Your V Rising Installation
1. Find your V Rising installation directory (usually via Steam)
2. Common paths:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\VRising\`
   - Game Pass: `C:\XboxGames\V Rising\Content\`

### Step 2: Create libs Directory
```bash
mkdir "D:\Crowbane\CrowbaneArena\libs"
```

### Step 3: Copy Required Libraries
Copy the following files to `D:\Crowbane\CrowbaneArena\libs\`:

**From `[V Rising Install]\BepInEx\core\`:**
- BepInEx.Core.dll
- BepInEx.Unity.IL2CPP.dll
- 0Harmony.dll

**From `[V Rising Install]\VRising_Data\Managed\`:**
- UnityEngine.dll
- UnityEngine.CoreModule.dll
- Unity.Mathematics.dll
- Unity.Entities.dll
- Unity.Collections.dll
- ProjectM.dll
- ProjectM.Shared.dll
- Stunlock.Core.dll

**From `[V Rising Install]\BepInEx\plugins\VampireCommandFramework\`:**
- VampireCommandFramework.dll

### Step 4: Verify Dependencies
After copying, your `libs` folder should contain:
```
libs/
├── BepInEx.Core.dll
├── BepInEx.Unity.IL2CPP.dll
├── UnityEngine.dll
├── UnityEngine.CoreModule.dll
├── Unity.Mathematics.dll
├── Unity.Entities.dll
├── Unity.Collections.dll
├── ProjectM.dll
├── ProjectM.Shared.dll
├── Stunlock.Core.dll
├── VampireCommandFramework.dll
└── 0Harmony.dll
```

## Troubleshooting

### Missing VampireCommandFramework
If VCF is not installed:
1. Download from: https://github.com/decaprime/VampireCommandFramework
2. Install to `[V Rising Install]\BepInEx\plugins\VampireCommandFramework\`
3. Copy VampireCommandFramework.dll to your libs folder

### Version Compatibility
- Ensure V Rising is updated to latest version
- BepInEx version should be 6.0.0-pre.1 or higher for IL2CPP
- VCF version should be compatible with your V Rising version

### Build Errors
- Verify all DLLs are present in libs folder
- Check file permissions on copied libraries
- Ensure V Rising is not running when copying files

## Next Steps
After setting up dependencies:
1. Build the project: `dotnet build`
2. Copy output DLL to BepInEx plugins folder
3. Test in V Rising server environment

## Support
For V Rising modding support:
- **CrowbaneArena Support**: Join our Discord server at https://discord.gg/ZnGGfj69zv
- **Developer Contact**: coyoteq1
- V Rising Modding Discord
- Official V Rising documentation
- BepInEx documentation for IL2CPP

## Community
- **Discord Server**: https://discord.gg/ZnGGfj69zv
- **Developed by**: coyoteq1
- Get help with installation, configuration, and troubleshooting
- Share feedback and suggestions for the CrowbaneArena mod

---
*This setup follows V Rising Technical Guidelines v2024 for optimal ECS/GameObject integration and performance.*