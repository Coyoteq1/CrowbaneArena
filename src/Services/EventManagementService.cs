using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using Unity.Collections;
using CrowbaneArena.Models;
using CrowbaneArena.Controllers;
using BepInEx.Logging;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Comprehensive event management service that orchestrates all event-related operations
    /// Provides high-level management for events, quests, bosses, and player progression
    /// </summary>
    public class EventManagementService
    {
        private static EventManagementService _instance;
        public static EventManagementService Instance => _instance ??= new EventManagementService();

        private readonly Dictionary<string, Quest> _questDatabase;
        private readonly Dictionary<string, BossEncounter> _bossDatabase;
        private readonly Dictionary<string, RemoteEvent> _eventDatabase;
        private readonly Dictionary<ulong, PlayerProgressTracker> _playerProgress;
        private readonly EventScheduler _scheduler;
        private readonly EventStatistics _statistics;

        private ManualLogSource _logger;
        private EntityManager _entityManager;
        private World _world;

        public EventManagementService()
        {
            _questDatabase = new Dictionary<string, Quest>();
            _bossDatabase = new Dictionary<string, BossEncounter>();
            _eventDatabase = new Dictionary<string, RemoteEvent>();
            _playerProgress = new Dictionary<ulong, PlayerProgressTracker>();
            _scheduler = new EventScheduler();
            _statistics = new EventStatistics();
        }

        public void Initialize(ManualLogSource logger, EntityManager entityManager, World world)
        {
            _logger = logger;
            _entityManager = entityManager;
            _world = world;

            LoadDefaultQuests();
            LoadDefaultBosses();
            _scheduler.Initialize(logger);

            Plugin.LogInstance.LogInfo("CrowbaneArena - Event Management Service initialized successfully");
        }

        #region Quest Management

        /// <summary>
        /// Registers a quest in the database
        /// </summary>
        public void RegisterQuest(Quest quest)
        {
            if (quest == null)
            {
                _logger.LogError("Cannot register null quest");
                return;
            }

            _questDatabase[quest.QuestId] = quest;
            _logger.LogInfo($"Registered quest: {quest.QuestId} - {quest.Name}");
        }

        /// <summary>
        /// Gets a quest by ID
        /// </summary>
        public Quest GetQuest(string questId)
        {
            return _questDatabase.GetValueOrDefault(questId);
        }

        /// <summary>
        /// Assigns a quest to a player
        /// </summary>
        public bool AssignQuestToPlayer(ulong playerId, string questId)
        {
            try
            {
                var quest = GetQuest(questId);
                if (quest == null)
                {
                    _logger.LogError($"Quest {questId} not found");
                    return false;
                }

                if (!_playerProgress.ContainsKey(playerId))
                {
                    _logger.LogError($"Player {playerId} not in any event");
                    return false;
                }

                var tracker = _playerProgress[playerId];
                ProgressTrackerUtilities.AssignQuest(tracker, quest);

                _logger.LogInfo($"Assigned quest {questId} to player {playerId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error assigning quest: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates quest progress for a player
        /// </summary>
        public bool UpdateQuestProgress(ulong playerId, string questId, string objectiveId, int amount = 1)
        {
            try
            {
                if (!_playerProgress.ContainsKey(playerId))
                {
                    return false;
                }

                var tracker = _playerProgress[playerId];
                var questCompleted = ProgressTrackerUtilities.UpdateQuestProgress(tracker, questId, objectiveId, amount);

                if (questCompleted)
                {
                    OnQuestCompleted(playerId, questId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating quest progress: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles quest completion
        /// </summary>
        private void OnQuestCompleted(ulong playerId, string questId)
        {
            try
            {
                var quest = GetQuest(questId);
                var tracker = _playerProgress[playerId];

                // Grant rewards
                foreach (var reward in quest.Rewards)
                {
                    GrantReward(playerId, reward);
                }

                // Assign next quest if available
                if (!string.IsNullOrEmpty(quest.NextQuestId))
                {
                    AssignQuestToPlayer(playerId, quest.NextQuestId);
                }

                // Update statistics
                _statistics.QuestsCompleted++;

                _logger.LogInfo($"Player {playerId} completed quest {questId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling quest completion: {ex.Message}");
            }
        }

        #endregion

        #region Boss Management

        /// <summary>
        /// Registers a boss encounter
        /// </summary>
        public void RegisterBoss(BossEncounter boss)
        {
            if (boss == null)
            {
                _logger.LogError("Cannot register null boss");
                return;
            }

            _bossDatabase[boss.BossId] = boss;
            _logger.LogInfo($"Registered boss: {boss.BossId} - {boss.Name}");
        }

        /// <summary>
        /// Gets a boss encounter by ID
        /// </summary>
        public BossEncounter GetBoss(string bossId)
        {
            return _bossDatabase.GetValueOrDefault(bossId);
        }

        /// <summary>
        /// Spawns a boss if conditions are met
        /// </summary>
        public bool SpawnBoss(string bossId, string eventId)
        {
            try
            {
                var boss = GetBoss(bossId);
                if (boss == null)
                {
                    _logger.LogError($"Boss {bossId} not found");
                    return false;
                }

                if (boss.IsActive)
                {
                    _logger.LogWarning($"Boss {bossId} is already active");
                    return false;
                }

                // Check spawn conditions
                if (!EvaluateBossSpawnConditions(boss, eventId))
                {
                    _logger.LogInfo($"Boss {bossId} spawn conditions not met");
                    return false;
                }

                // Spawn the boss
                if (RemoteEventController.Instance.SpawnBoss(bossId))
                {
                    _statistics.BossesSpawned++;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error spawning boss: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles boss defeat
        /// </summary>
        public void OnBossDefeated(string bossId, List<ulong> participants)
        {
            try
            {
                var boss = GetBoss(bossId);
                if (boss == null) return;

                boss.State = BossState.Defeated;
                boss.DefeatedAt = DateTime.UtcNow;
                boss.ParticipatingPlayers = participants;

                // Update player progress
                foreach (var playerId in participants)
                {
                    if (_playerProgress.ContainsKey(playerId))
                    {
                        var tracker = _playerProgress[playerId];
                        tracker.SetFlag("boss_defeated", true);
                        tracker.Statistics.BossesDefeated++;

                        // Update quest progress if applicable
                        UpdateQuestProgress(playerId, "quest_03_defeat_boss", "defeat_guardian", 1);
                    }
                }

                // Drop loot
                DropBossLoot(boss, participants);

                // Execute defeat triggers
                ExecuteBossDefeatTriggers(boss);

                _statistics.BossesDefeated++;
                _logger.LogInfo($"Boss {bossId} defeated by {participants.Count} players");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling boss defeat: {ex.Message}");
            }
        }

        #endregion

        #region Event Lifecycle Management

        /// <summary>
        /// Creates and initializes a complete event
        /// </summary>
        public bool CreateCompleteEvent(string eventId, string name, string description, EventConfiguration config)
        {
            try
            {
                // Create the base event
                if (!RemoteEventController.Instance.CreateEvent(eventId, name, description, config.EntryPosition, config.ExitPosition))
                {
                    return false;
                }

                var remoteEvent = new RemoteEvent(eventId, name, description)
                {
                    StartingQuestId = config.StartingQuestId,
                    RequiredTemplates = config.RequiredTemplates
                };

                _eventDatabase[eventId] = remoteEvent;

                // Create event-specific trigger zones
                CreateEventTriggerZones(eventId, config);

                // Setup event quests
                SetupEventQuests(eventId, config);

                // Setup event bosses
                SetupEventBosses(eventId, config);

                _statistics.EventsCreated++;
                _logger.LogInfo($"Created complete event: {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating complete event: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles player entry into an event
        /// </summary>
        public bool HandlePlayerEntry(ulong playerId, string eventId, string templateId)
        {
            try
            {
                if (!_eventDatabase.ContainsKey(eventId))
                {
                    _logger.LogError($"Event {eventId} not found");
                    return false;
                }

                // Enter player into event
                if (!RemoteEventController.Instance.EnterPlayerIntoEvent(playerId, eventId, templateId))
                {
                    return false;
                }

                // Get or create progress tracker
                var tracker = _playerProgress.GetValueOrDefault(playerId);
                if (tracker == null)
                {
                    tracker = ProgressTrackerUtilities.CreateForPlayer(playerId, eventId, GetPlayerName(playerId));
                    _playerProgress[playerId] = tracker;
                }

                // Assign starting quest
                var eventData = _eventDatabase[eventId];
                if (!string.IsNullOrEmpty(eventData.StartingQuestId))
                {
                    AssignQuestToPlayer(playerId, eventData.StartingQuestId);
                }

                _statistics.PlayersEntered++;
                _logger.LogInfo($"Player {playerId} entered event {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling player entry: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles player exit from an event
        /// </summary>
        public bool HandlePlayerExit(ulong playerId, string eventId)
        {
            try
            {
                // Exit player from event
                if (!RemoteEventController.Instance.ExitPlayerFromEvent(playerId, eventId))
                {
                    return false;
                }

                // Clean up progress tracker
                if (_playerProgress.ContainsKey(playerId))
                {
                    var tracker = _playerProgress[playerId];
                    tracker.ExitedAt = DateTime.UtcNow;
                    tracker.State = PlayerEventState.Completed;

                    // Archive progress for statistics
                    ArchivePlayerProgress(tracker);

                    _playerProgress.Remove(playerId);
                }

                _statistics.PlayersExited++;
                _logger.LogInfo($"Player {playerId} exited event {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling player exit: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Event Monitoring and Updates

        /// <summary>
        /// Updates all active events (called periodically)
        /// </summary>
        public void UpdateEvents()
        {
            try
            {
                // Update player progress
                UpdatePlayerProgress();

                // Check boss spawn conditions
                CheckBossSpawnConditions();

                // Update event states
                UpdateEventStates();

                // Process scheduled events
                _scheduler.ProcessScheduledEvents();

                // Update statistics
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating events: {ex.Message}");
            }
        }

        private void UpdatePlayerProgress()
        {
            foreach (var tracker in _playerProgress.Values)
            {
                // Update time-based objectives
                UpdateTimeBasedObjectives(tracker);

                // Check for automatic quest progression
                CheckAutomaticQuestProgression(tracker);
            }
        }

        private void CheckBossSpawnConditions()
        {
            foreach (var boss in _bossDatabase.Values.Where(b => b.State == BossState.NotSpawned))
            {
                foreach (var eventId in _eventDatabase.Keys)
                {
                    if (EvaluateBossSpawnConditions(boss, eventId))
                    {
                        SpawnBoss(boss.BossId, eventId);
                    }
                }
            }
        }

        private void UpdateEventStates()
        {
            foreach (var eventData in _eventDatabase.Values)
            {
                // Check if event should be completed
                if (ShouldCompleteEvent(eventData))
                {
                    CompleteEvent(eventData.EventId);
                }
            }
        }

        #endregion

        #region Utility Methods

        private void LoadDefaultQuests()
        {
            RegisterQuest(QuestTemplates.CreateCollectGemsQuest());
            RegisterQuest(QuestTemplates.CreateActivateLeversQuest());
            RegisterQuest(QuestTemplates.CreateDefeatBossQuest());
            RegisterQuest(QuestTemplates.CreateSurvivalQuest());
            RegisterQuest(QuestTemplates.CreateEscortQuest());
        }

        private void LoadDefaultBosses()
        {
            RegisterBoss(BossTemplates.CreateGuardianBoss());
            RegisterBoss(BossTemplates.CreateDragonBoss());
            RegisterBoss(BossTemplates.CreateNecromancerBoss());
        }

        private void CreateEventTriggerZones(string eventId, EventConfiguration config)
        {
            // Create entry gate
            var entryGate = TriggerZoneTemplates.CreateEventEntryGate(
                $"{eventId}_entry",
                config.EntryPosition,
                new float3(5, 5, 5),
                eventId,
                config.DefaultTemplate
            );
            RemoteEventController.Instance.RegisterTriggerZone(entryGate);

            // Create exit gate
            var exitGate = TriggerZoneTemplates.CreateEventExitGate(
                $"{eventId}_exit",
                config.ExitPosition,
                new float3(5, 5, 5),
                eventId
            );
            RemoteEventController.Instance.RegisterTriggerZone(exitGate);

            // Create additional zones from config
            foreach (var zoneConfig in config.AdditionalZones)
            {
                var zone = CreateZoneFromConfig(zoneConfig);
                zone.EventId = eventId;
                RemoteEventController.Instance.RegisterTriggerZone(zone);
            }
        }

        private void SetupEventQuests(string eventId, EventConfiguration config)
        {
            foreach (var questId in config.QuestSequence)
            {
                var quest = GetQuest(questId);
                if (quest != null)
                {
                    // Quest is already registered, just link it to the event
                    quest.Properties["eventId"] = eventId;
                }
            }
        }

        private void SetupEventBosses(string eventId, EventConfiguration config)
        {
            foreach (var bossId in config.BossEncounters)
            {
                var boss = GetBoss(bossId);
                if (boss != null)
                {
                    boss.Properties["eventId"] = eventId;
                }
            }
        }

        private bool EvaluateBossSpawnConditions(BossEncounter boss, string eventId)
        {
            var playersInEvent = _playerProgress.Values.Where(p => p.EventId == eventId);

            foreach (var condition in boss.SpawnConditions)
            {
                if (!EvaluateSpawnCondition(condition, playersInEvent))
                {
                    return false;
                }
            }

            return true;
        }

        private bool EvaluateSpawnCondition(string condition, IEnumerable<PlayerProgressTracker> players)
        {
            switch (condition)
            {
                case "all_levers_activated":
                    return players.Any(p => p.HasFlag("all_levers_activated"));
                case "quest_02_completed":
                    return players.Any(p => p.IsQuestCompleted("quest_02_activate_levers"));
                default:
                    return true;
            }
        }

        private void GrantReward(ulong playerId, QuestReward reward)
        {
            try
            {
                switch (reward.Type)
                {
                    case QuestRewardType.Item:
                        // Grant item to player
                        break;
                    case QuestRewardType.Experience:
                        // Grant experience to player
                        break;
                    case QuestRewardType.Currency:
                        // Grant currency to player
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error granting reward: {ex.Message}");
            }
        }

        private void DropBossLoot(BossEncounter boss, List<ulong> participants)
        {
            try
            {
                foreach (var lootItem in boss.LootTable)
                {
                    if (lootItem.IsGuaranteed || UnityEngine.Random.value <= lootItem.DropChance)
                    {
                        var quantity = UnityEngine.Random.Range(lootItem.MinQuantity, lootItem.MaxQuantity + 1);
                        // Drop item for participants
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error dropping boss loot: {ex.Message}");
            }
        }

        private void ExecuteBossDefeatTriggers(BossEncounter boss)
        {
            foreach (var trigger in boss.DefeatTriggers)
            {
                switch (trigger)
                {
                    case "spawn_exit_portal":
                        // Spawn exit portal
                        break;
                    case "complete_event":
                        // Mark event as completable
                        break;
                }
            }
        }

        private void UpdateTimeBasedObjectives(PlayerProgressTracker tracker)
        {
            // Update survival quests and time-based objectives
            foreach (var questProgress in tracker.QuestProgressData.Values)
            {
                foreach (var objective in questProgress.ObjectiveProgress.Values)
                {
                    if (objective.ObjectiveData.ContainsKey("timeBasedType"))
                    {
                        // Update time-based progress
                    }
                }
            }
        }

        private void CheckAutomaticQuestProgression(PlayerProgressTracker tracker)
        {
            // Check for automatic quest progression based on flags and conditions
        }

        private bool ShouldCompleteEvent(RemoteEvent eventData)
        {
            var playersInEvent = _playerProgress.Values.Where(p => p.EventId == eventData.EventId);
            return playersInEvent.Any(p => p.HasFlag("event_completed"));
        }

        private void CompleteEvent(string eventId)
        {
            try
            {
                if (_eventDatabase.ContainsKey(eventId))
                {
                    var eventData = _eventDatabase[eventId];
                    eventData.State = EventState.Completed;
                    eventData.EndedAt = DateTime.UtcNow;

                    _statistics.EventsCompleted++;
                    _logger.LogInfo($"Event {eventId} completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error completing event: {ex.Message}");
            }
        }

        private void ArchivePlayerProgress(PlayerProgressTracker tracker)
        {
            // Archive player progress for statistics and analysis
            _statistics.TotalPlayTime += tracker.EventDuration ?? TimeSpan.Zero;
        }

        private void UpdateStatistics()
        {
            _statistics.ActiveEvents = _eventDatabase.Values.Count(e => e.State == EventState.Active);
            _statistics.ActivePlayers = _playerProgress.Count;
            _statistics.LastUpdated = DateTime.UtcNow;
        }

        private string GetPlayerName(ulong playerId)
        {
            return $"Player_{playerId}";
        }

        private TriggerZone CreateZoneFromConfig(ZoneConfiguration config)
        {
            return new TriggerZone(config.ZoneId, config.Name, config.Type)
            {
                Position = config.Position,
                Size = config.Size,
                Shape = config.Shape
            };
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets current event statistics
        /// </summary>
        public EventStatistics GetStatistics()
        {
            return _statistics;
        }

        /// <summary>
        /// Gets all active events
        /// </summary>
        public IEnumerable<RemoteEvent> GetActiveEvents()
        {
            return _eventDatabase.Values.Where(e => e.State == EventState.Active);
        }

        /// <summary>
        /// Gets player progress for a specific player
        /// </summary>
        public PlayerProgressTracker GetPlayerProgress(ulong playerId)
        {
            return _playerProgress.GetValueOrDefault(playerId);
        }

        /// <summary>
        /// Gets all quests
        /// </summary>
        public IEnumerable<Quest> GetAllQuests()
        {
            return _questDatabase.Values;
        }

        /// <summary>
        /// Gets all bosses
        /// </summary>
        public IEnumerable<BossEncounter> GetAllBosses()
        {
            return _bossDatabase.Values;
        }

        #endregion
    }

    /// <summary>
    /// Configuration for creating a complete event
    /// </summary>
    public class EventConfiguration
    {
        public float3 EntryPosition { get; set; }
        public float3 ExitPosition { get; set; }
        public string StartingQuestId { get; set; }
        public string DefaultTemplate { get; set; } = "event_warrior";
        public List<string> RequiredTemplates { get; set; } = new List<string>();
        public List<string> QuestSequence { get; set; } = new List<string>();
        public List<string> BossEncounters { get; set; } = new List<string>();
        public List<ZoneConfiguration> AdditionalZones { get; set; } = new List<ZoneConfiguration>();
    }

    /// <summary>
    /// Configuration for trigger zones
    /// </summary>
    public class ZoneConfiguration
    {
        public string ZoneId { get; set; }
        public string Name { get; set; }
        public TriggerZoneType Type { get; set; }
        public float3 Position { get; set; }
        public float3 Size { get; set; }
        public TriggerZoneShape Shape { get; set; } = TriggerZoneShape.Box;
    }

    /// <summary>
    /// Event system statistics
    /// </summary>
    public class EventStatistics
    {
        public int EventsCreated { get; set; }
        public int EventsCompleted { get; set; }
        public int ActiveEvents { get; set; }
        public int ActivePlayers { get; set; }
        public int PlayersEntered { get; set; }
        public int PlayersExited { get; set; }
        public int QuestsCompleted { get; set; }
        public int BossesSpawned { get; set; }
        public int BossesDefeated { get; set; }
        public TimeSpan TotalPlayTime { get; set; }
        public DateTime LastUpdated { get; set; }

        public EventStatistics()
        {
            LastUpdated = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event scheduler for timed events
    /// </summary>
    public class EventScheduler
    {
        private readonly List<ScheduledEvent> _scheduledEvents;
        private ManualLogSource _logger;

        public EventScheduler()
        {
            _scheduledEvents = new List<ScheduledEvent>();
        }

        public void Initialize(ManualLogSource logger)
        {
            _logger = logger;
        }

        public void ScheduleEvent(string eventId, DateTime scheduledTime, Action action)
        {
            _scheduledEvents.Add(new ScheduledEvent
            {
                EventId = eventId,
                ScheduledTime = scheduledTime,
                Action = action
            });
        }

        public void ProcessScheduledEvents()
        {
            var now = DateTime.UtcNow;
            var eventsToProcess = _scheduledEvents.Where(e => e.ScheduledTime <= now).ToList();

            foreach (var scheduledEvent in eventsToProcess)
            {
                try
                {
                    scheduledEvent.Action?.Invoke();
                    _scheduledEvents.Remove(scheduledEvent);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error processing scheduled event: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Represents a scheduled event
    /// </summary>
    public class ScheduledEvent
    {
        public string EventId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public Action Action { get; set; }
    }
}