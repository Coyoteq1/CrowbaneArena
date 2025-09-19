using System;
using System.Collections.Generic;
using Unity.Mathematics;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace CrowbaneArena.Models
{
    /// <summary>
    /// Represents an invisible area that triggers actions when players enter or exit
    /// Core component for managing event gates, safe zones, and interactive areas
    /// </summary>
    [Serializable]
    public class TriggerZone
    {
        public string ZoneId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TriggerZoneType Type { get; set; }
        public TriggerZoneShape Shape { get; set; }
        public bool IsActive { get; set; }

        // Position and Dimensions
        public float3 Position { get; set; }
        public float3 Rotation { get; set; }
        public float3 Size { get; set; } // For box zones
        public float Radius { get; set; } // For sphere/cylinder zones
        public float Height { get; set; } // For cylinder zones

        // Trigger Configuration
        public List<TriggerAction> EntryActions { get; set; }
        public List<TriggerAction> ExitActions { get; set; }
        public List<TriggerCondition> EntryConditions { get; set; }
        public List<TriggerCondition> ExitConditions { get; set; }

        // Player Tracking
        public HashSet<ulong> PlayersInZone { get; set; }
        public Dictionary<ulong, DateTime> PlayerEntryTimes { get; set; }

        // Zone Properties
        public Dictionary<string, object> Properties { get; set; }
        public string EventId { get; set; }
        public int Priority { get; set; } // Higher priority zones trigger first

        // Cooldown and Limits
        public float CooldownSeconds { get; set; }
        public Dictionary<ulong, DateTime> PlayerCooldowns { get; set; }
        public int MaxActivations { get; set; }
        public int CurrentActivations { get; set; }

        // Visual and Audio
        public string VisualEffect { get; set; }
        public string SoundEffect { get; set; }
        public bool ShowBoundaries { get; set; }

        public TriggerZone()
        {
            IsActive = true;
            EntryActions = new List<TriggerAction>();
            ExitActions = new List<TriggerAction>();
            EntryConditions = new List<TriggerCondition>();
            ExitConditions = new List<TriggerCondition>();
            PlayersInZone = new HashSet<ulong>();
            PlayerEntryTimes = new Dictionary<ulong, DateTime>();
            Properties = new Dictionary<string, object>();
            PlayerCooldowns = new Dictionary<ulong, DateTime>();
            Priority = 0;
            CooldownSeconds = 0f;
            MaxActivations = -1; // Unlimited
            CurrentActivations = 0;
            ShowBoundaries = false;
        }

        public TriggerZone(string zoneId, string name, TriggerZoneType type) : this()
        {
            ZoneId = zoneId;
            Name = name;
            Type = type;
        }

        public bool IsPlayerInZone(ulong playerId) => PlayersInZone.Contains(playerId);
        public bool CanActivate => MaxActivations < 0 || CurrentActivations < MaxActivations;
        public bool IsOnCooldown(ulong playerId) => PlayerCooldowns.ContainsKey(playerId) &&
            DateTime.UtcNow < PlayerCooldowns[playerId].AddSeconds(CooldownSeconds);
    }

    [Serializable]
    public enum TriggerZoneType
    {
        EventGateEntry,
        EventGateExit,
        SafeZone,
        Checkpoint,
        QuestTrigger,
        BossArena,
        Teleporter,
        ItemSpawn,
        Hazard,
        Custom
    }

    [Serializable]
    public enum TriggerZoneShape
    {
        Box,
        Sphere,
        Cylinder,
        Plane
    }

    /// <summary>
    /// Represents an action to be executed when a trigger zone is activated
    /// </summary>
    [Serializable]
    public class TriggerAction
    {
        public string ActionId { get; set; }
        public TriggerActionType Type { get; set; }
        public string Target { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public float Delay { get; set; }
        public bool RequiresConfirmation { get; set; }
        public string ConfirmationMessage { get; set; }

        public TriggerAction()
        {
            Parameters = new Dictionary<string, object>();
            Delay = 0f;
            RequiresConfirmation = false;
        }

        public TriggerAction(string actionId, TriggerActionType type, string target) : this()
        {
            ActionId = actionId;
            Type = type;
            Target = target;
        }
    }

    [Serializable]
    public enum TriggerActionType
    {
        Teleport,
        SaveInventory,
        RestoreInventory,
        SwapCharacter,
        RestoreCharacter,
        AssignQuest,
        CompleteQuest,
        SetFlag,
        SpawnBoss,
        DespawnBoss,
        SendMessage,
        PlayEffect,
        PlaySound,
        GiveItem,
        RemoveItem,
        Heal,
        Damage,
        ApplyBuff,
        RemoveBuff,
        CreateCheckpoint,
        LoadCheckpoint,
        Custom
    }

    /// <summary>
    /// Represents a condition that must be met for a trigger to activate
    /// </summary>
    [Serializable]
    public class TriggerCondition
    {
        public string ConditionId { get; set; }
        public TriggerConditionType Type { get; set; }
        public string Target { get; set; }
        public object ExpectedValue { get; set; }
        public TriggerConditionOperator Operator { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public TriggerCondition()
        {
            Parameters = new Dictionary<string, object>();
            Operator = TriggerConditionOperator.Equals;
        }

        public TriggerCondition(string conditionId, TriggerConditionType type, string target, object expectedValue) : this()
        {
            ConditionId = conditionId;
            Type = type;
            Target = target;
            ExpectedValue = expectedValue;
        }
    }

    [Serializable]
    public enum TriggerConditionType
    {
        HasFlag,
        QuestCompleted,
        QuestActive,
        HasItem,
        PlayerLevel,
        PlayerHealth,
        TeamSize,
        TimeInZone,
        Custom
    }

    [Serializable]
    public enum TriggerConditionOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Contains,
        NotContains
    }

    /// <summary>
    /// Predefined trigger zone templates for common use cases
    /// </summary>
    public static class TriggerZoneTemplates
    {
        /// <summary>
        /// Creates an event entry gate that saves inventory and swaps character
        /// </summary>
        public static TriggerZone CreateEventEntryGate(string zoneId, float3 position, float3 size, string eventId, string templateId)
        {
            var zone = new TriggerZone(zoneId, "Event Entry Gate", TriggerZoneType.EventGateEntry)
            {
                Position = position,
                Size = size,
                Shape = TriggerZoneShape.Box,
                EventId = eventId,
                Description = "Enter the event arena",
                VisualEffect = "portal_entry_effect",
                SoundEffect = "portal_enter_sound"
            };

            // Entry actions
            zone.EntryActions.Add(new TriggerAction("save_inventory", TriggerActionType.SaveInventory, "player"));
            zone.EntryActions.Add(new TriggerAction("swap_character", TriggerActionType.SwapCharacter, templateId));
            zone.EntryActions.Add(new TriggerAction("teleport_to_event", TriggerActionType.Teleport, "event_start_position"));
            zone.EntryActions.Add(new TriggerAction("assign_first_quest", TriggerActionType.AssignQuest, "quest_01_collect_gems"));
            zone.EntryActions.Add(new TriggerAction("set_event_started", TriggerActionType.SetFlag, "event_started")
            {
                Parameters = { { "value", true } }
            });
            zone.EntryActions.Add(new TriggerAction("welcome_message", TriggerActionType.SendMessage, "player")
            {
                Parameters = { { "message", "Welcome to the Arena! Complete your quests to progress." } }
            });

            return zone;
        }

        /// <summary>
        /// Creates an event exit gate that restores inventory and character
        /// </summary>
        public static TriggerZone CreateEventExitGate(string zoneId, float3 position, float3 size, string eventId)
        {
            var zone = new TriggerZone(zoneId, "Event Exit Gate", TriggerZoneType.EventGateExit)
            {
                Position = position,
                Size = size,
                Shape = TriggerZoneShape.Box,
                EventId = eventId,
                Description = "Exit the event arena",
                VisualEffect = "portal_exit_effect",
                SoundEffect = "portal_exit_sound"
            };

            // Exit conditions - must have defeated boss
            zone.EntryConditions.Add(new TriggerCondition("boss_defeated", TriggerConditionType.HasFlag, "boss_defeated", true));

            // Exit actions
            zone.EntryActions.Add(new TriggerAction("restore_character", TriggerActionType.RestoreCharacter, "player"));
            zone.EntryActions.Add(new TriggerAction("restore_inventory", TriggerActionType.RestoreInventory, "player"));
            zone.EntryActions.Add(new TriggerAction("teleport_to_world", TriggerActionType.Teleport, "world_spawn_position"));
            zone.EntryActions.Add(new TriggerAction("completion_message", TriggerActionType.SendMessage, "player")
            {
                Parameters = { { "message", "Congratulations! You have completed the event!" } }
            });
            zone.EntryActions.Add(new TriggerAction("set_event_completed", TriggerActionType.SetFlag, "event_completed")
            {
                Parameters = { { "value", true } }
            });

            return zone;
        }

        /// <summary>
        /// Creates a safe zone that teleports players to a spawn point
        /// </summary>
        public static TriggerZone CreateSafeZone(string zoneId, float3 position, float radius, float3 spawnPosition)
        {
            var zone = new TriggerZone(zoneId, "Safe Zone", TriggerZoneType.SafeZone)
            {
                Position = position,
                Radius = radius,
                Shape = TriggerZoneShape.Sphere,
                Description = "A safe area that teleports you to safety",
                CooldownSeconds = 5f
            };

            zone.EntryActions.Add(new TriggerAction("teleport_to_safety", TriggerActionType.Teleport, "safe_spawn")
            {
                Parameters = { { "position", spawnPosition } }
            });
            zone.EntryActions.Add(new TriggerAction("heal_player", TriggerActionType.Heal, "player")
            {
                Parameters = { { "amount", 1000f } }
            });

            return zone;
        }

        /// <summary>
        /// Creates a boss arena trigger zone
        /// </summary>
        public static TriggerZone CreateBossArena(string zoneId, float3 position, float3 size, string bossId)
        {
            var zone = new TriggerZone(zoneId, "Boss Arena", TriggerZoneType.BossArena)
            {
                Position = position,
                Size = size,
                Shape = TriggerZoneShape.Box,
                Description = "Boss encounter area",
                MaxActivations = 1 // Boss can only be spawned once
            };

            // Conditions - must have completed prerequisite quests
            zone.EntryConditions.Add(new TriggerCondition("levers_activated", TriggerConditionType.HasFlag, "all_levers_activated", true));

            // Actions
            zone.EntryActions.Add(new TriggerAction("spawn_boss", TriggerActionType.SpawnBoss, bossId));
            zone.EntryActions.Add(new TriggerAction("boss_intro_message", TriggerActionType.SendMessage, "all")
            {
                Parameters = { { "message", "The Guardian awakens! Prepare for battle!" } }
            });
            zone.EntryActions.Add(new TriggerAction("set_boss_encountered", TriggerActionType.SetFlag, "boss_encountered")
            {
                Parameters = { { "value", true } }
            });

            return zone;
        }

        /// <summary>
        /// Creates a checkpoint trigger zone
        /// </summary>
        public static TriggerZone CreateCheckpoint(string zoneId, float3 position, float radius, string checkpointName)
        {
            var zone = new TriggerZone(zoneId, $"Checkpoint: {checkpointName}", TriggerZoneType.Checkpoint)
            {
                Position = position,
                Radius = radius,
                Shape = TriggerZoneShape.Sphere,
                Description = "Progress checkpoint",
                CooldownSeconds = 30f
            };

            zone.EntryActions.Add(new TriggerAction("create_checkpoint", TriggerActionType.CreateCheckpoint, checkpointName)
            {
                Parameters = { { "position", position } }
            });
            zone.EntryActions.Add(new TriggerAction("checkpoint_message", TriggerActionType.SendMessage, "player")
            {
                Parameters = { { "message", $"Checkpoint reached: {checkpointName}" } }
            });

            return zone;
        }

        /// <summary>
        /// Creates a quest trigger zone
        /// </summary>
        public static TriggerZone CreateQuestTrigger(string zoneId, float3 position, float3 size, string questId, string objectiveId)
        {
            var zone = new TriggerZone(zoneId, "Quest Trigger", TriggerZoneType.QuestTrigger)
            {
                Position = position,
                Size = size,
                Shape = TriggerZoneShape.Box,
                Description = "Quest objective area"
            };

            // Condition - must have the quest active
            zone.EntryConditions.Add(new TriggerCondition("quest_active", TriggerConditionType.QuestActive, questId, true));

            zone.EntryActions.Add(new TriggerAction("update_quest", TriggerActionType.Custom, "update_quest_progress")
            {
                Parameters = { { "questId", questId }, { "objectiveId", objectiveId }, { "amount", 1 } }
            });

            return zone;
        }

        /// <summary>
        /// Creates a teleporter zone
        /// </summary>
        public static TriggerZone CreateTeleporter(string zoneId, float3 position, float radius, float3 destination)
        {
            var zone = new TriggerZone(zoneId, "Teleporter", TriggerZoneType.Teleporter)
            {
                Position = position,
                Radius = radius,
                Shape = TriggerZoneShape.Sphere,
                Description = "Teleportation platform",
                VisualEffect = "teleport_effect",
                SoundEffect = "teleport_sound",
                CooldownSeconds = 2f
            };

            zone.EntryActions.Add(new TriggerAction("teleport", TriggerActionType.Teleport, "destination")
            {
                Parameters = { { "position", destination } },
                RequiresConfirmation = true,
                ConfirmationMessage = "Do you want to teleport?"
            });

            return zone;
        }

        /// <summary>
        /// Creates a hazard zone that damages players
        /// </summary>
        public static TriggerZone CreateHazardZone(string zoneId, float3 position, float3 size, float damagePerSecond)
        {
            var zone = new TriggerZone(zoneId, "Hazard Zone", TriggerZoneType.Hazard)
            {
                Position = position,
                Size = size,
                Shape = TriggerZoneShape.Box,
                Description = "Dangerous area",
                VisualEffect = "hazard_effect",
                ShowBoundaries = true
            };

            zone.EntryActions.Add(new TriggerAction("hazard_warning", TriggerActionType.SendMessage, "player")
            {
                Parameters = { { "message", "Warning: You are in a hazardous area!" } }
            });

            // Note: Continuous damage would be handled by a separate system
            zone.Properties["damagePerSecond"] = damagePerSecond;

            return zone;
        }
    }

    /// <summary>
    /// Utility class for trigger zone operations
    /// </summary>
    public static class TriggerZoneUtilities
    {
        /// <summary>
        /// Checks if a position is within a trigger zone
        /// </summary>
        public static bool IsPositionInZone(TriggerZone zone, float3 position)
        {
            switch (zone.Shape)
            {
                case TriggerZoneShape.Box:
                    return IsPositionInBox(position, zone.Position, zone.Size, zone.Rotation);
                case TriggerZoneShape.Sphere:
                    return math.distance(position, zone.Position) <= zone.Radius;
                case TriggerZoneShape.Cylinder:
                    return IsPositionInCylinder(position, zone.Position, zone.Radius, zone.Height);
                case TriggerZoneShape.Plane:
                    return IsPositionOnPlane(position, zone.Position, zone.Size, zone.Rotation);
                default:
                    return false;
            }
        }

        private static bool IsPositionInBox(float3 position, float3 center, float3 size, float3 rotation)
        {
            // Simple AABB check (rotation not implemented for simplicity)
            var min = center - size * 0.5f;
            var max = center + size * 0.5f;
            return position.x >= min.x && position.x <= max.x &&
                   position.y >= min.y && position.y <= max.y &&
                   position.z >= min.z && position.z <= max.z;
        }

        private static bool IsPositionInCylinder(float3 position, float3 center, float radius, float height)
        {
            var horizontalDistance = math.distance(new float2(position.x, position.z), new float2(center.x, center.z));
            var verticalDistance = math.abs(position.y - center.y);
            return horizontalDistance <= radius && verticalDistance <= height * 0.5f;
        }

        private static bool IsPositionOnPlane(float3 position, float3 center, float3 size, float3 rotation)
        {
            // Simple 2D plane check on XZ axis
            return math.abs(position.x - center.x) <= size.x * 0.5f &&
                   math.abs(position.z - center.z) <= size.z * 0.5f;
        }

        /// <summary>
        /// Evaluates if all conditions are met for a trigger activation
        /// </summary>
        public static bool EvaluateConditions(List<TriggerCondition> conditions, PlayerProgressTracker tracker)
        {
            foreach (var condition in conditions)
            {
                if (!EvaluateCondition(condition, tracker))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool EvaluateCondition(TriggerCondition condition, PlayerProgressTracker tracker)
        {
            switch (condition.Type)
            {
                case TriggerConditionType.HasFlag:
                    var flagValue = tracker.HasFlag(condition.Target);
                    return CompareValues(flagValue, condition.ExpectedValue, condition.Operator);

                case TriggerConditionType.QuestCompleted:
                    var questCompleted = tracker.IsQuestCompleted(condition.Target);
                    return CompareValues(questCompleted, condition.ExpectedValue, condition.Operator);

                case TriggerConditionType.QuestActive:
                    var questActive = tracker.IsQuestActive(condition.Target);
                    return CompareValues(questActive, condition.ExpectedValue, condition.Operator);

                case TriggerConditionType.HasItem:
                    var hasItem = tracker.CollectedItems.ContainsKey(condition.Target) &&
                                  tracker.CollectedItems[condition.Target] > 0;
                    return CompareValues(hasItem, condition.ExpectedValue, condition.Operator);

                default:
                    return true; // Unknown conditions pass by default
            }
        }

        private static bool CompareValues(object actual, object expected, TriggerConditionOperator op)
        {
            switch (op)
            {
                case TriggerConditionOperator.Equals:
                    return Equals(actual, expected);
                case TriggerConditionOperator.NotEquals:
                    return !Equals(actual, expected);
                case TriggerConditionOperator.GreaterThan:
                    return Comparer<object>.Default.Compare(actual, expected) > 0;
                case TriggerConditionOperator.LessThan:
                    return Comparer<object>.Default.Compare(actual, expected) < 0;
                case TriggerConditionOperator.GreaterThanOrEqual:
                    return Comparer<object>.Default.Compare(actual, expected) >= 0;
                case TriggerConditionOperator.LessThanOrEqual:
                    return Comparer<object>.Default.Compare(actual, expected) <= 0;
                default:
                    return true;
            }
        }
    }
}