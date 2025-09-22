
## CrowbaneArena 
Remote Event Controller is a powerful tool for V Rising server admins. It lets you create and manage special events where players can participate in custom adventures. You can set up unique challenges like boss fights, quests, and more, all while giving players temporary, balanced characters.

Note: This mod is a pre-release version. It is highly recommended that you test all features on a separate, non-production server before using it for live events.

## Top Features

Character Swapping: Instantly switch players into pre-made characters like a Warrior, Mage, or Archer for events. Everyone gets the same gear and stats, making the event fair and balanced. When they leave the event, their original character is safely restored.

Quest System: Create quests with different objectives, like killing enemies, collecting items, or surviving for a certain amount of time. You can even create a chain of quests that players must complete one after another.

Boss Encounters: Spawn custom bosses with multiple phases and unique abilities. You can configure what loot they drop and track who is participating.

Teleportation & Event Zones: Set up special areas that players can teleport to. You can also create invisible Trigger Zones that do things like start a quest or spawn a monster when a player enters.

Admin Commands: You get over 20 commands to control every part of the system, including creating events, managing players, and spawning bosses.

## How to Install

Get the necessities: You need a V Rising server with BepInEx and the VampireCommandFramework (VCF) mod already installed.

Download and copy: Download the CrowbaneArena mod files and copy them into your BepInEx/plugins/ folder.

Restart: Restart your V Rising server. Check the server logs to make sure the mod started correctly.

Verify: Log in and type event status. If you see system info, you're good to go!

Quick Guide: Your First Event

The mod comes with a default event called demo_arena you can use to test everything out.

Enter the event: Type event enter demo_arena event_warrior to enter the arena as a warrior.

Check your progress: Type event questprogress playername to see what you need to do.

Exit the event: Type event exit demo_arena to return to your normal character.

## important Admin Commands

Here are some of the most useful commands to get you started:

event create <eventId> <name> <description>: Makes a new event.

event list: Shows all events you've created.

event players <eventId>: Lists all players currently in a specific event.

event spawnboss <bossId>: Spawns a boss.

event assignquest <playerName> <questId>: Gives a player a quest.

event teleport <playerName> <x> <y> <z>: Teleports a player to a specific spot.

## Troubleshooting

Mod not working: Make sure VampireCommandFramework is installed first. Check your server logs for any errors.

Character swapping is broken: Double-check that the character template you're trying to use exists. Also, make sure the player isn't already in another event.

Commands don't work: You must have admin permissions on the server to use these commands.

## Changelog 2.1.2

Added Configuration and JSON Files

This update introduces several new configuration and JSON files to provide more granular control over events, zones, and history tracking.


arena_history.json: This new file has been added to log a history of events, tracking information such as the date and time a user entered an arena.

area.json: This new file defines specific event zones with unique IDs, names, coordinates, and sizes. These zones include:

arena_entry

arena_exit

arena_checkpoint_1

arena_boss

arena.config: This file now defines a range of configurable settings for the event system. You can now specify:

Event templates (event_warrior, event_mage, event_archer)

Quests (quest_01_collect_gems, quest_02_activate_levers, quest_03_defeat_boss, quest_survival, quest_escort)

Trigger zones (arena_entry, arena_exit, arena_checkpoint_1, arena_boss_arena)

arena_config.json: This JSON version of the configuration file contains the same details as arena.config, listing event templates, quests, bosses, and trigger zones. It also includes a default description and specifies trigger zones with a demo prefix, such as demo_arena_entry and demo_arena_exit.

## Get Help & Contribute

If you're stuck, check the troubleshooting section above.

Join the CrowBane Discord server: https://discord.gg/ZnGGfj69zv

Join the V Rising modding Discord: https://discord.gg/ZrYAbDCF55

This mod is designed to be expanded, so if you're a developer, you can help by creating new character templates, quests, or boss encounters.
