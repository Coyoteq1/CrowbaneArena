using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace CrowbaneArena.Models
{
    /// <summary>
    /// Tracks player progress throughout an event, including quests, flags, and statistics
    /// Critical component for managing player state during events
    /// </summary>
    [Serializable]
    public class PlayerProgressTracker
    {
        public ulong PlayerId { get; set; }
        public string EventId { get; set; }
        public string PlayerName { get; set; }
        public DateTime EnteredAt { get; set; }
        public DateTime? ExitedAt { get; set; }
        public PlayerEventState State { get; set; }

        // Quest Progress
        public List<string> ActiveQuestIds { get; set; }
        public List<string> CompletedQuestIds { get; set; }
        public Dictionary<string, QuestProgress> QuestProgressData { get; set; }

        // Event Flags - Boolean markers for major accomplishments
        public Dictionary<string, bool> EventFlags { get; set; }

        // Statistics and Metrics
        public PlayerEventStatistics Statistics { get; set; }

        // Checkpoints and Save Points
        public List<PlayerCheckpoint> Checkpoints { get; set; }
        public string CurrentCheckpointId { get; set; }

        // Team and Group Information
        public string TeamId { get; set; }
        public List<ulong> TeamMembers { get; set; }

        // Custom Data Storage
        public Dictionary<string, object> CustomData { get; set; }

        // Event-Specific Inventory Tracking
        public List<EventInventoryItem> EventInventory { get; set; }
        public Dictionary<string, int> CollectedItems { get; set; }

        public PlayerProgressTracker()
        {
            EnteredAt = DateTime.UtcNow;
            State = PlayerEventState.Active;
            ActiveQuestIds = new List<string>();
            CompletedQuestIds = new List<string>();
            QuestProgressData = new Dictionary<string, QuestProgress>();
            EventFlags = new Dictionary<string, bool>();
            Statistics = new PlayerEventStatistics();
            Checkpoints = new List<PlayerCheckpoint>();
            TeamMembers = new List<ulong>();
            CustomData = new Dictionary<string, object>();
            EventInventory = new List<EventInventoryItem>();
            CollectedItems = new Dictionary<string, int>();
        }

        public PlayerProgressTracker(ulong playerId, string eventId, string playerName) : this()
        {
            PlayerId = playerId;
            EventId = eventId;
            PlayerName = playerName;
        }

        // Helper Methods
        public bool HasFlag(string flagName) => EventFlags.ContainsKey(flagName) && EventFlags[flagName];
        public void SetFlag(string flagName, bool value) => EventFlags[flagName] = value;
        public bool IsQuestActive(string questId) => ActiveQuestIds.Contains(questId);
        public bool IsQuestCompleted(string questId) => CompletedQuestIds.Contains(questId);
        public TimeSpan? EventDuration => ExitedAt.HasValue ? ExitedAt.Value - EnteredAt : DateTime.UtcNow - EnteredAt;
        public float CompletionPercentage => CompletedQuestIds.Count > 0 ?
            (float)CompletedQuestIds.Count / (CompletedQuestIds.Count + ActiveQuestIds.Count) * 100f : 0f;
    }

    [Serializable]
    public enum PlayerEventState
    {
        Active,
        Completed,
        Failed,
        Abandoned,
        Disconnected,
        Suspended
    }

    /// <summary>
    /// Detailed quest progress for individual objectives
    /// </summary>
    [Serializable]
    public class QuestProgress
    {
        public string QuestId { get; set; }
        public QuestState State { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, ObjectiveProgress> ObjectiveProgress { get; set; }
        public Dictionary<string, object> QuestData { get; set; }

        public QuestProgress()
        {
            State = QuestState.InProgress;
            StartedAt = DateTime.UtcNow;
            ObjectiveProgress = new Dictionary<string, ObjectiveProgress>();
            QuestData = new Dictionary<string, object>();
        }

        public QuestProgress(string questId) : this()
        {
            QuestId = questId;
        }

        public bool IsCompleted => ObjectiveProgress.Values.All(obj => obj.IsCompleted);
        public float CompletionPercentage => ObjectiveProgress.Count > 0 ?
            (float)ObjectiveProgress.Values.Count(obj => obj.IsCompleted) / ObjectiveProgress.Count * 100f : 0f;
    }

    /// <summary>
    /// Progress tracking for individual quest objectives
    /// </summary>
    [Serializable]
    public class ObjectiveProgress
    {
        public string ObjectiveId { get; set; }
        public int RequiredAmount { get; set; }
        public int CurrentAmount { get; set; }
        public bool IsCompleted => CurrentAmount >= RequiredAmount;
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> ObjectiveData { get; set; }

        public ObjectiveProgress()
        {
            ObjectiveData = new Dictionary<string, object>();
        }

        public ObjectiveProgress(string objectiveId, int requiredAmount) : this()
        {
            ObjectiveId = objectiveId;
            RequiredAmount = requiredAmount;
            CurrentAmount = 0;
        }

        public void IncrementProgress(int amount = 1)
        {
            CurrentAmount = Math.Min(CurrentAmount + amount, RequiredAmount);
            if (IsCompleted && !CompletedAt.HasValue)
            {
                CompletedAt = DateTime.UtcNow;
            }
        }

        public float ProgressPercentage => RequiredAmount > 0 ?
            Math.Min((float)CurrentAmount / RequiredAmount * 100f, 100f) : 0f;
    }

    /// <summary>
    /// Statistical data for player performance during events
    /// </summary>
    [Serializable]
    public class PlayerEventStatistics
    {
        // Combat Statistics
        public int EnemiesKilled { get; set; }
        public int BossesDefeated { get; set; }
        public float DamageDealt { get; set; }
        public float DamageTaken { get; set; }
        public int Deaths { get; set; }
        public int Resurrections { get; set; }

        // Exploration Statistics
        public float DistanceTraveled { get; set; }
        public int AreasDiscovered { get; set; }
        public int SecretsFound { get; set; }
        public int InteractionsCompleted { get; set; }

        // Item and Resource Statistics
        public int ItemsCollected { get; set; }
        public int ItemsUsed { get; set; }
        public int ChestsOpened { get; set; }
        public Dictionary<string, int> ResourcesGathered { get; set; }

        // Time Statistics
        public TimeSpan TotalCombatTime { get; set; }
        public TimeSpan TotalExplorationTime { get; set; }
        public DateTime? FirstKill { get; set; }
        public DateTime? LastActivity { get; set; }

        // Performance Metrics
        public float AverageDPS { get; set; }
        public float SurvivalRate { get; set; }
        public int PerfectObjectives { get; set; }
        public Dictionary<string, float> CustomMetrics { get; set; }

        public PlayerEventStatistics()
        {
            ResourcesGathered = new Dictionary<string, int>();
            CustomMetrics = new Dictionary<string, float>();
            TotalCombatTime = TimeSpan.Zero;
            TotalExplorationTime = TimeSpan.Zero;
            SurvivalRate = 100f;
        }

        public float KillDeathRatio => Deaths > 0 ? (float)EnemiesKilled / Deaths : EnemiesKilled;
        public float EfficiencyRating => (EnemiesKilled + ItemsCollected + AreasDiscovered) / Math.Max(Deaths + 1, 1);
    }

    /// <summary>
    /// Checkpoint system for saving player progress at key moments
    /// </summary>
    [Serializable]
    public class PlayerCheckpoint
    {
        public string CheckpointId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public float3 Position { get; set; }
        public float3 Rotation { get; set; }
        public string ZoneId { get; set; }

        // Progress State at Checkpoint
        public List<string> ActiveQuestsAtCheckpoint { get; set; }
        public List<string> CompletedQuestsAtCheckpoint { get; set; }
        public Dictionary<string, bool> EventFlagsAtCheckpoint { get; set; }
        public PlayerEventStatistics StatisticsAtCheckpoint { get; set; }

        // Inventory State at Checkpoint
        public List<EventInventoryItem> InventoryAtCheckpoint { get; set; }

        public PlayerCheckpoint()
        {
            CreatedAt = DateTime.UtcNow;
            ActiveQuestsAtCheckpoint = new List<string>();
            CompletedQuestsAtCheckpoint = new List<string>();
            EventFlagsAtCheckpoint = new Dictionary<string, bool>();
            StatisticsAtCheckpoint = new PlayerEventStatistics();
            InventoryAtCheckpoint = new List<EventInventoryItem>();
        }

        public PlayerCheckpoint(string checkpointId, string name, float3 position) : this()
        {
            CheckpointId = checkpointId;
            Name = name;
            Position = position;
        }
    }

    /// <summary>
    /// Event-specific inventory item tracking
    /// </summary>
    [Serializable]
    public class EventInventoryItem
    {
        public PrefabGUID ItemGuid { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public DateTime AcquiredAt { get; set; }
        public string AcquiredFrom { get; set; } // "quest_reward", "boss_loot", "chest", etc.
        public Dictionary<string, object> ItemProperties { get; set; }

        public EventInventoryItem()
        {
            AcquiredAt = DateTime.UtcNow;
            ItemProperties = new Dictionary<string, object>();
        }

        public EventInventoryItem(PrefabGUID itemGuid, string itemName, int quantity, string acquiredFrom) : this()
        {
            ItemGuid = itemGuid;
            ItemName = itemName;
            Quantity = quantity;
            AcquiredFrom = acquiredFrom;
        }
    }

    /// <summary>
    /// Utility class for managing player progress operations
    /// </summary>
    public static class ProgressTrackerUtilities
    {
        /// <summary>
        /// Creates a new progress tracker for a player entering an event
        /// </summary>
        public static PlayerProgressTracker CreateForPlayer(ulong playerId, string eventId, string playerName)
        {
            var tracker = new PlayerProgressTracker(playerId, eventId, playerName);

            // Initialize common event flags
            tracker.SetFlag("event_started", true);
            tracker.SetFlag("tutorial_completed", false);
            tracker.SetFlag("first_quest_assigned", false);
            tracker.SetFlag("boss_encountered", false);
            tracker.SetFlag("boss_defeated", false);
            tracker.SetFlag("event_completed", false);

            return tracker;
        }

        /// <summary>
        /// Updates quest progress for a specific objective
        /// </summary>
        public static bool UpdateQuestProgress(PlayerProgressTracker tracker, string questId, string objectiveId, int amount = 1)
        {
            if (!tracker.QuestProgressData.ContainsKey(questId))
            {
                tracker.QuestProgressData[questId] = new QuestProgress(questId);
            }

            var questProgress = tracker.QuestProgressData[questId];
            if (!questProgress.ObjectiveProgress.ContainsKey(objectiveId))
            {
                return false; // Objective not found
            }

            questProgress.ObjectiveProgress[objectiveId].IncrementProgress(amount);

            // Check if quest is now completed
            if (questProgress.IsCompleted && questProgress.State != QuestState.Completed)
            {
                questProgress.State = QuestState.Completed;
                questProgress.CompletedAt = DateTime.UtcNow;

                // Move from active to completed
                tracker.ActiveQuestIds.Remove(questId);
                if (!tracker.CompletedQuestIds.Contains(questId))
                {
                    tracker.CompletedQuestIds.Add(questId);
                }

                return true; // Quest completed
            }

            return false; // Quest still in progress
        }

        /// <summary>
        /// Assigns a new quest to the player
        /// </summary>
        public static void AssignQuest(PlayerProgressTracker tracker, Quest quest)
        {
            if (!tracker.ActiveQuestIds.Contains(quest.QuestId))
            {
                tracker.ActiveQuestIds.Add(quest.QuestId);

                var questProgress = new QuestProgress(quest.QuestId);
                foreach (var objective in quest.Objectives)
                {
                    questProgress.ObjectiveProgress[objective.ObjectiveId] =
                        new ObjectiveProgress(objective.ObjectiveId, objective.RequiredAmount);
                }

                tracker.QuestProgressData[quest.QuestId] = questProgress;
            }
        }

        /// <summary>
        /// Creates a checkpoint for the player's current progress
        /// </summary>
        public static PlayerCheckpoint CreateCheckpoint(PlayerProgressTracker tracker, string checkpointId, string name, float3 position)
        {
            var checkpoint = new PlayerCheckpoint(checkpointId, name, position);

            // Copy current state
            checkpoint.ActiveQuestsAtCheckpoint = new List<string>(tracker.ActiveQuestIds);
            checkpoint.CompletedQuestsAtCheckpoint = new List<string>(tracker.CompletedQuestIds);
            checkpoint.EventFlagsAtCheckpoint = new Dictionary<string, bool>(tracker.EventFlags);
            checkpoint.StatisticsAtCheckpoint = tracker.Statistics; // Note: This is a reference, consider deep copy if needed
            checkpoint.InventoryAtCheckpoint = new List<EventInventoryItem>(tracker.EventInventory);

            tracker.Checkpoints.Add(checkpoint);
            tracker.CurrentCheckpointId = checkpointId;

            return checkpoint;
        }

        /// <summary>
        /// Restores player progress from a checkpoint
        /// </summary>
        public static void RestoreFromCheckpoint(PlayerProgressTracker tracker, string checkpointId)
        {
            var checkpoint = tracker.Checkpoints.FirstOrDefault(cp => cp.CheckpointId == checkpointId);
            if (checkpoint == null) return;

            // Restore progress state
            tracker.ActiveQuestIds = new List<string>(checkpoint.ActiveQuestsAtCheckpoint);
            tracker.CompletedQuestIds = new List<string>(checkpoint.CompletedQuestsAtCheckpoint);
            tracker.EventFlags = new Dictionary<string, bool>(checkpoint.EventFlagsAtCheckpoint);
            tracker.EventInventory = new List<EventInventoryItem>(checkpoint.InventoryAtCheckpoint);
            tracker.CurrentCheckpointId = checkpointId;
        }
    }
}