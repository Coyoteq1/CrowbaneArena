# CrowbaneArena - Remote Event Controller v2.0.0

A comprehensive V Rising mod that provides a complete Remote Event Controller system with character swapping, quest management, boss encounters, and teleportation functionality.

## 🎯 Overview

The Remote Event Controller is an advanced administrative tool that allows game masters to create and manage special event zones where players can participate in custom adventures with temporary character transformations. The system provides complete control over player experience through dynamic teleportation, character swapping, quest progression, and boss encounters.

## ✨ Key Features

### 🔄 Character Swapping System
- **Complete Character Replacement**: Players' characters are temporarily replaced with pre-designed event templates
- **Perfect Balance**: All players have identical stats and equipment during events
- **Safe State Management**: Original character data is safely stored and restored upon exit
- **Multiple Templates**: Warrior, Mage, Archer, and custom templates available

### 🎮 Quest Management
- **Dynamic Quest System**: Assign and track quest progression in real-time
- **Objective Tracking**: Support for kill, collect, interact, survival, and custom objectives
- **Quest Chains**: Automatic progression through connected quest sequences
- **Reward System**: Automatic reward distribution upon quest completion

### 👹 Boss Encounter System
- **Dynamic Boss Spawning**: Bosses spawn based on player progress and conditions
- **Multi-Phase Encounters**: Support for complex boss fights with multiple phases
- **Loot Distribution**: Configurable loot tables with drop chances
- **Participant Tracking**: Track all players involved in boss encounters

### 🌐 Teleportation & Trigger Zones
- **Dynamic Teleporter Nodes**: Remotely configurable teleportation points
- **Trigger Zones**: Invisible areas that execute actions when players enter/exit
- **Event Gates**: Seamless entry/exit system for events
- **Safe Zones**: Emergency teleportation to safety areas

### 📊 Comprehensive Administration
- **20+ Admin Commands**: Complete control over all system aspects
- **Real-time Monitoring**: Live statistics and health monitoring
- **Event Validation**: Automatic validation of event configurations
- **Backup & Recovery**: System state backup and emergency recovery tools

## 🚀 Installation

1. **Prerequisites**:
   - V Rising server with BepInEx installed
   - VampireCommandFramework (VCF) mod installed

2. **Installation Steps**:
   ```
   1. Download the CrowbaneArena mod files
   2. Extract to your BepInEx/plugins/ directory
   3. Restart your V Rising server
   4. Check logs for successful initialization
   ```

3. **Verification**:
   ```
   Use the command: event status
   You should see system status information
   ```

## 🎮 Quick Start Guide

### Creating Your First Event

1. **Create an Event**:
   ```
   event create demo_arena "My First Arena" "A test arena event"
   ```

2. **Enter the Event**:
   ```
   event enter demo_arena event_warrior
   ```

3. **Check Your Progress**:
   ```
   event questprogress YourPlayerName
   ```

4. **Exit the Event**:
   ```
   event exit demo_arena
   ```

### Default Demo Event

The system automatically creates a demo event called `demo_arena` with:
- Entry and exit gates
- A quest sequence (collect gems → activate levers → defeat boss)
- A guardian boss encounter
- Multiple character templates to choose from

## 📋 Command Reference

### Event Management
- `event create <eventId> <name> <description>` - Create a new event
- `event stop <eventId>` - Stop an active event
- `event list` - List all active events
- `event info <eventId>` - Show event details
- `event reset <eventId>` - Reset event to initial state

### Player Management
- `event enter <eventId> <templateId>` - Enter an event with character template
- `event exit <eventId>` - Exit from an event
- `event kick <playerName> <eventId>` - Kick a player from event
- `event players <eventId>` - List players in event

### Character Templates
- `event template list` - Show available templates
- `event template create <templateId> <name>` - Create new template
- `event template delete <templateId>` - Delete template
- `event template info <templateId>` - Show template details

### Teleportation
- `event teleport <playerName> <x> <y> <z>` - Teleport player to coordinates
- `event setteleporter <nodeId> <x> <y> <z>` - Set teleporter destination
- `event tphere <playerName>` - Teleport player to your location

### Boss Management
- `event spawnboss <bossId>` - Spawn a boss encounter
- `event killboss <bossId>` - Force kill a boss
- `event bossinfo <bossId>` - Get boss information

### Quest Management
- `event assignquest <playerName> <questId>` - Assign quest to player
- `event completequest <playerName> <questId>` - Force complete quest
- `event questprogress <playerName>` - Show quest progress

### Trigger Zones
- `event createzone <zoneId> <type> <x> <y> <z> <size>` - Create trigger zone
- `event deletezone <zoneId>` - Delete trigger zone
- `event listzones` - List all trigger zones

### Character Swapping
- `event swap <playerName> <templateId>` - Swap player character
- `event restore <playerName>` - Restore original character
- `event swapstats` - Show swap statistics

### Utilities
- `event setflag <playerName> <flagName> <value>` - Set event flag
- `event getflag <playerName> <flagName>` - Get event flag value
- `event heal <playerName> [amount]` - Heal player
- `event damage <playerName> <amount>` - Damage player
- `event giveitem <playerName> <itemId> [quantity]` - Give item to player
- `event removeitem <playerName> <itemId> [quantity]` - Remove item from player
- `event message <playerName> <message>` - Send message to player
- `event broadcast <eventId> <message>` - Broadcast to all players in event
- `event checkpoint <playerName> <checkpointId>` - Create checkpoint
- `event loadcheckpoint <playerName> <checkpointId>` - Load checkpoint
- `event status` - Show system status
- `event help [command]` - Show help information

## 🏗️ System Architecture

### Core Components

1. **RemoteEventController**: Central orchestration of all event operations
2. **CharacterSwapService**: Handles character template application and restoration
3. **EventManagementService**: Manages quest progression, boss encounters, and event lifecycle
4. **TriggerZone System**: Handles area-based event triggers and actions
5. **AdminInterface**: Provides administrative tools and system monitoring

### Data Models

- **PlayerStateSnapshot**: Complete backup of player's original character
- **EventCharacterTemplate**: Pre-configured character setups for events
- **Quest & QuestObjective**: Quest system with trackable objectives
- **BossEncounter**: Multi-phase boss fight configurations
- **TriggerZone**: Area-based event triggers with conditions and actions
- **PlayerProgressTracker**: Real-time tracking of player progress through events

## 🔧 Configuration

### Character Templates

Default templates included:
- **event_warrior**: Balanced melee fighter (1000 HP, sword & shield)
- **event_mage**: Magical spellcaster (600 HP, high energy, staff)
- **event_archer**: Ranged fighter (800 HP, bow & arrows)

### Quest Templates

Default quest chain:
1. **Collect Gems**: Gather 3 power gems (teaches exploration)
2. **Activate Levers**: Find and activate 3 switches (teaches interaction)
3. **Defeat Boss**: Defeat the Guardian (teaches combat)

### Boss Templates

Default bosses:
- **Guardian**: Balanced boss with ground slam and charge attacks
- **Dragon**: Flying boss with fire breath and meteor strikes
- **Necromancer**: Summoning boss with undead minions

## 📊 Monitoring & Administration

### System Health Monitoring

The system provides comprehensive health monitoring:
- **Service Status**: Real-time status of all core services
- **Memory Usage**: Memory consumption tracking
- **Performance Metrics**: Response times and throughput
- **Error Tracking**: Automatic error detection and reporting

### Statistics Tracking

Comprehensive statistics are maintained:
- Events created/completed
- Players entered/exited
- Quests completed
- Bosses defeated
- Total playtime
- Character swap operations

### Backup & Recovery

- **Automatic Backups**: System state is automatically backed up
- **Emergency Recovery**: Force restore all players in case of issues
- **Configuration Export**: Export current system configuration
- **Event Validation**: Automatic validation of event setups

## 🛠️ Development & Customization

### Adding Custom Character Templates

```csharp
var customTemplate = new EventCharacterTemplate("custom_tank", "Heavy Tank")
{
    Description = "A heavily armored tank character",
    MaxHealth = 2000f,
    MaxEnergy = 50f,
    PowerLevel = 60,
    MovementSpeed = 0.8f
};

// Add equipment
customTemplate.StartingEquipment.Add(
    new TemplateEquippedItem(new PrefabGUID(-123456), "MainHand", "Heavy Shield")
);

// Register the template
CharacterSwapService.Instance.RegisterTemplate(customTemplate);
```

### Creating Custom Quests

```csharp
var customQuest = new Quest("custom_survival", "Survive the Onslaught",
    "Survive waves of enemies for 10 minutes");

customQuest.Objectives.Add(
    new QuestObjective("survive_time", "Survive 10 minutes",
        QuestObjectiveType.Survive, "survival_timer", 600)
);

EventManagementService.Instance.RegisterQuest(customQuest);
```

### Adding Custom Boss Encounters

```csharp
var customBoss = new BossEncounter("custom_elemental", "Elemental Lord",
    new PrefabGUID(-789012))
{
    Description = "A powerful elemental with fire and ice attacks"
};

customBoss.Attributes.MaxHealth = 20000f;
customBoss.Attributes.Level = 80;

// Add custom abilities
customBoss.Abilities.Add(new BossAbility("fire_storm", "Fire Storm",
    BossAbilityType.AreaOfEffect));

EventManagementService.Instance.RegisterBoss(customBoss);
```

## 🐛 Troubleshooting

### Common Issues

1. **Event System Not Initializing**:
   - Check that VampireCommandFramework is installed
   - Verify BepInEx is working correctly
   - Check server logs for initialization errors

2. **Character Swap Not Working**:
   - Ensure player is not already in an event
   - Verify character template exists
   - Check for conflicting mods

3. **Commands Not Working**:
   - Verify you have admin permissions
   - Check command syntax
   - Ensure event system is initialized

### Debug Commands

- `event status` - Check overall system health
- `event swapstats` - Check character swap service status
- `event help` - Show all available commands

### Log Files

Check BepInEx logs for detailed error information:
- Look for "CrowbaneArena" entries
- Check for initialization success messages
- Monitor for error patterns

## 🤝 Support & Contributing

### Getting Help

1. Check the troubleshooting section above
2. Review server logs for error messages
3. Verify all prerequisites are installed
4. Test with the default demo event first

### Contributing

This mod is designed to be extensible. Key areas for contribution:
- Additional character templates
- New quest types and objectives
- Custom boss encounters
- Enhanced trigger zone actions
- Performance optimizations

## 📄 License

This mod is provided as-is for V Rising server administration. Please respect the game's terms of service and use responsibly.

## 🔄 Version History

### v2.0.0 (Current)
- Complete rewrite with comprehensive event system
- Character swapping with template system
- Quest management with objective tracking
- Boss encounter system with multi-phase support
- 20+ administrative commands
- Real-time monitoring and health checks
- Automatic backup and recovery systems

### v1.0.0 (Legacy)
- Basic teleportation functionality
- Simple item management

---

**CrowbaneArena - Remote Event Controller v2.0.0**
*Transforming V Rising server events with professional-grade tools*