using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using Unity.Collections;
using CrowbaneArena.Models;
using BepInEx.Logging;

namespace CrowbaneArena.Controllers
{
    /// <summary>
    /// Central controller for managing remote events, teleportation, and player state
    /// Follows V Rising's ECS/GameObject hybrid approach with proper entity management
    /// </summary>
    public class RemoteEventController
    {
        private static RemoteEventController _instance;
        public static RemoteEventController Instance => _instance ??= new RemoteEventController();

        private readonly Dictionary<string, RemoteEvent> _activeEvents;
        private readonly Dictionary<string, TeleporterNode> _teleporterNodes;
        private readonly Dictionary<string, TriggerZone> _triggerZones;
        private readonly Dictionary<ulong, PlayerProgressTracker> _playerProgress;
        private readonly Dictionary<ulong, PlayerStateSnapshot> _playerSnapshots;
        private readonly Dictionary<string, EventCharacterTemplate> _characterTemplates;
        private readonly Dictionary<string, BossEncounter> _bossEncounters;

        private ManualLogSource _logger;
        private EntityManager _entityManager;
        private World _world;

        public RemoteEventController()
        {
            _activeEvents = new Dictionary<string, RemoteEvent>();
            _teleporterNodes = new Dictionary<string, TeleporterNode>();
            _triggerZones = new Dictionary<string, TriggerZone>();
            _playerProgress = new Dictionary<ulong, PlayerProgressTracker>();
            _playerSnapshots = new Dictionary<ulong, PlayerStateSnapshot>();
            _characterTemplates = new Dictionary<string, EventCharacterTemplate>();
            _bossEncounters = new Dictionary<string, BossEncounter>();
        }

        public void Initialize(ManualLogSource logger, EntityManager entityManager, World world)
        {
            _logger = logger;
            _entityManager = entityManager;
            _world = world;

            LoadDefaultTemplates();
            Plugin.LogInstance.LogInfo("CrowbaneArena - Remote Event Controller initialized successfully");
        }

        #region Event Management

        /// <summary>
        /// Creates and starts a new remote event
        /// </summary>
        public bool CreateEvent(string eventId, string name, string description, float3 entryPosition, float3 exitPosition)
        {
            try
            {
                if (_activeEvents.ContainsKey(eventId))
                {
                    _logger.LogWarning($"Event {eventId} already exists");
                    return false;
                }

                var remoteEvent = new RemoteEvent(eventId, name, description)
                {
                    EntryPosition = entryPosition,
                    ExitPosition = exitPosition,
                    State = EventState.Active
                };

                _activeEvents[eventId] = remoteEvent;

                // Create default entry and exit gates
                CreateDefaultEventGates(eventId, entryPosition, exitPosition);

                _logger.LogInfo($"Created event: {eventId} - {name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create event {eventId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stops and removes an event
        /// </summary>
        public bool StopEvent(string eventId)
        {
            try
            {
                if (!_activeEvents.ContainsKey(eventId))
                {
                    _logger.LogWarning($"Event {eventId} not found");
                    return false;
                }

                var eventData = _activeEvents[eventId];
                eventData.State = EventState.Stopped;

                // Remove all players from the event
                var playersInEvent = _playerProgress.Values
                    .Where(p => p.EventId == eventId)
                    .ToList();

                foreach (var player in playersInEvent)
                {
                    ExitPlayerFromEvent(player.PlayerId, eventId);
                }

                // Clean up trigger zones
                var eventZones = _triggerZones.Values
                    .Where(z => z.EventId == eventId)
                    .ToList();

                foreach (var zone in eventZones)
                {
                    _triggerZones.Remove(zone.ZoneId);
                }

                _activeEvents.Remove(eventId);
                _logger.LogInfo($"Stopped event: {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to stop event {eventId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Handles player entry into an event
        /// </summary>
        public bool EnterPlayerIntoEvent(ulong playerId, string eventId, string templateId)
        {
            try
            {
                if (!_activeEvents.ContainsKey(eventId))
                {
                    _logger.LogWarning($"Event {eventId} not found");
                    return false;
                }

                if (!_characterTemplates.ContainsKey(templateId))
                {
                    _logger.LogWarning($"Character template {templateId} not found");
                    return false;
                }

                var eventData = _activeEvents[eventId];
                var template = _characterTemplates[templateId];

                // Create player state snapshot
                var snapshot = CreatePlayerSnapshot(playerId);
                if (snapshot == null)
                {
                    _logger.LogError($"Failed to create snapshot for player {playerId}");
                    return false;
                }

                _playerSnapshots[playerId] = snapshot;

                // Apply character template
                if (!ApplyCharacterTemplate(playerId, template))
                {
                    _logger.LogError($"Failed to apply character template for player {playerId}");
                    RestorePlayerSnapshot(playerId);
                    return false;
                }

                // Teleport to event start position
                TeleportPlayer(playerId, eventData.EntryPosition);

                // Create progress tracker
                var tracker = ProgressTrackerUtilities.CreateForPlayer(playerId, eventId, GetPlayerName(playerId));
                _playerProgress[playerId] = tracker;

                // Assign starting quest if available
                if (eventData.StartingQuestId != null)
                {
                    var startingQuest = GetQuest(eventData.StartingQuestId);
                    if (startingQuest != null)
                    {
                        ProgressTrackerUtilities.AssignQuest(tracker, startingQuest);
                    }
                }

                _logger.LogInfo($"Player {playerId} entered event {eventId} with template {templateId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to enter player {playerId} into event {eventId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles player exit from an event
        /// </summary>
        public bool ExitPlayerFromEvent(ulong playerId, string eventId)
        {
            try
            {
                if (!_playerProgress.ContainsKey(playerId))
                {
                    _logger.LogWarning($"Player {playerId} not in any event");
                    return false;
                }

                var tracker = _playerProgress[playerId];
                if (tracker.EventId != eventId)
                {
                    _logger.LogWarning($"Player {playerId} not in event {eventId}");
                    return false;
                }

                // Restore character and inventory
                if (!RestorePlayerSnapshot(playerId))
                {
                    _logger.LogError($"Failed to restore snapshot for player {playerId}");
                    return false;
                }

                // Teleport to exit position
                if (_activeEvents.ContainsKey(eventId))
                {
                    TeleportPlayer(playerId, _activeEvents[eventId].ExitPosition);
                }

                // Clean up progress tracker
                tracker.ExitedAt = DateTime.UtcNow;
                tracker.State = PlayerEventState.Completed;
                _playerProgress.Remove(playerId);

                _logger.LogInfo($"Player {playerId} exited event {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to exit player {playerId} from event {eventId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Character Template Management

        /// <summary>
        /// Registers a character template
        /// </summary>
        public void RegisterCharacterTemplate(EventCharacterTemplate template)
        {
            _characterTemplates[template.TemplateId] = template;
            _logger.LogInfo($"Registered character template: {template.TemplateId}");
        }

        /// <summary>
        /// Applies a character template to a player
        /// </summary>
        private bool ApplyCharacterTemplate(ulong playerId, EventCharacterTemplate template)
        {
            try
            {
                // Get player entity
                var playerEntity = GetPlayerEntity(playerId);
                if (playerEntity == Entity.Null)
                {
                    _logger.LogError($"Player entity not found for {playerId}");
                    return false;
                }

                // Apply template stats
                ApplyTemplateStats(playerEntity, template);

                // Apply template equipment
                ApplyTemplateEquipment(playerEntity, template);

                // Apply template inventory
                ApplyTemplateInventory(playerEntity, template);

                // Apply template abilities
                ApplyTemplateAbilities(playerEntity, template);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to apply character template: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Teleportation

        /// <summary>
        /// Creates or updates a teleporter node
        /// </summary>
        public bool SetTeleporterDestination(string nodeId, float3 destination)
        {
            try
            {
                if (!_teleporterNodes.ContainsKey(nodeId))
                {
                    _teleporterNodes[nodeId] = new TeleporterNode(nodeId, "Teleporter", destination);
                }
                else
                {
                    _teleporterNodes[nodeId].Destination = destination;
                }

                _logger.LogInfo($"Set teleporter {nodeId} destination to {destination}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to set teleporter destination: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Teleports a player to a specific position
        /// </summary>
        public bool TeleportPlayer(ulong playerId, float3 position)
        {
            try
            {
                var playerEntity = GetPlayerEntity(playerId);
                if (playerEntity == Entity.Null)
                {
                    _logger.LogError($"Player entity not found for {playerId}");
                    return false;
                }

                // Use V Rising's teleportation system
                if (_entityManager.HasComponent<Translation>(playerEntity))
                {
                    var translation = _entityManager.GetComponentData<Translation>(playerEntity);
                    translation.Value = position;
                    _entityManager.SetComponentData(playerEntity, translation);
                }

                _logger.LogInfo($"Teleported player {playerId} to {position}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to teleport player {playerId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Trigger Zone Management

        /// <summary>
        /// Registers a trigger zone
        /// </summary>
        public void RegisterTriggerZone(TriggerZone zone)
        {
            _triggerZones[zone.ZoneId] = zone;
            _logger.LogInfo($"Registered trigger zone: {zone.ZoneId}");
        }

        /// <summary>
        /// Processes player movement and checks for trigger zone activation
        /// </summary>
        public void ProcessPlayerMovement(ulong playerId, float3 position)
        {
            try
            {
                foreach (var zone in _triggerZones.Values.Where(z => z.IsActive))
                {
                    var wasInZone = zone.IsPlayerInZone(playerId);
                    var isInZone = TriggerZoneUtilities.IsPositionInZone(zone, position);

                    if (!wasInZone && isInZone)
                    {
                        // Player entered zone
                        OnPlayerEnterZone(playerId, zone);
                    }
                    else if (wasInZone && !isInZone)
                    {
                        // Player exited zone
                        OnPlayerExitZone(playerId, zone);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing player movement: {ex.Message}");
            }
        }

        private void OnPlayerEnterZone(ulong playerId, TriggerZone zone)
        {
            try
            {
                // Check cooldown
                if (zone.IsOnCooldown(playerId))
                {
                    return;
                }

                // Check conditions
                if (_playerProgress.ContainsKey(playerId))
                {
                    var tracker = _playerProgress[playerId];
                    if (!TriggerZoneUtilities.EvaluateConditions(zone.EntryConditions, tracker))
                    {
                        return;
                    }
                }

                // Add player to zone
                zone.PlayersInZone.Add(playerId);
                zone.PlayerEntryTimes[playerId] = DateTime.UtcNow;

                // Execute entry actions
                ExecuteTriggerActions(playerId, zone.EntryActions, zone);

                // Set cooldown
                if (zone.CooldownSeconds > 0)
                {
                    zone.PlayerCooldowns[playerId] = DateTime.UtcNow;
                }

                zone.CurrentActivations++;
                _logger.LogInfo($"Player {playerId} entered trigger zone {zone.ZoneId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling player enter zone: {ex.Message}");
            }
        }

        private void OnPlayerExitZone(ulong playerId, TriggerZone zone)
        {
            try
            {
                zone.PlayersInZone.Remove(playerId);
                zone.PlayerEntryTimes.Remove(playerId);

                // Execute exit actions
                ExecuteTriggerActions(playerId, zone.ExitActions, zone);

                _logger.LogInfo($"Player {playerId} exited trigger zone {zone.ZoneId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling player exit zone: {ex.Message}");
            }
        }

        #endregion

        #region Boss Management

        /// <summary>
        /// Spawns a boss encounter
        /// </summary>
        public bool SpawnBoss(string bossId)
        {
            try
            {
                if (!_bossEncounters.ContainsKey(bossId))
                {
                    _logger.LogWarning($"Boss encounter {bossId} not found");
                    return false;
                }

                var boss = _bossEncounters[bossId];
                if (boss.IsActive)
                {
                    _logger.LogWarning($"Boss {bossId} is already active");
                    return false;
                }

                // Spawn boss entity
                var bossEntity = SpawnBossEntity(boss);
                if (bossEntity == Entity.Null)
                {
                    _logger.LogError($"Failed to spawn boss entity for {bossId}");
                    return false;
                }

                boss.SpawnedEntity = bossEntity;
                boss.SpawnedAt = DateTime.UtcNow;
                boss.State = BossState.Spawned;

                _logger.LogInfo($"Spawned boss: {bossId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to spawn boss {bossId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles boss defeat
        /// </summary>
        public void OnBossDefeated(string bossId)
        {
            try
            {
                if (!_bossEncounters.ContainsKey(bossId))
                {
                    return;
                }

                var boss = _bossEncounters[bossId];
                boss.State = BossState.Defeated;
                boss.DefeatedAt = DateTime.UtcNow;

                // Update player progress for all participants
                foreach (var playerId in boss.ParticipatingPlayers)
                {
                    if (_playerProgress.ContainsKey(playerId))
                    {
                        var tracker = _playerProgress[playerId];
                        tracker.SetFlag("boss_defeated", true);
                        tracker.Statistics.BossesDefeated++;
                    }
                }

                // Execute defeat triggers
                ExecuteDefeatTriggers(boss);

                _logger.LogInfo($"Boss defeated: {bossId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling boss defeat: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private void LoadDefaultTemplates()
        {
            // Load default character templates
            RegisterCharacterTemplate(DefaultEventTemplates.CreateWarriorTemplate());
            RegisterCharacterTemplate(DefaultEventTemplates.CreateMageTemplate());
            RegisterCharacterTemplate(DefaultEventTemplates.CreateArcherTemplate());

            // Load default boss encounters
            _bossEncounters["boss_guardian"] = BossTemplates.CreateGuardianBoss();
            _bossEncounters["boss_dragon"] = BossTemplates.CreateDragonBoss();
            _bossEncounters["boss_necromancer"] = BossTemplates.CreateNecromancerBoss();
        }

        private void CreateDefaultEventGates(string eventId, float3 entryPosition, float3 exitPosition)
        {
            // Create entry gate
            var entryGate = TriggerZoneTemplates.CreateEventEntryGate(
                $"{eventId}_entry",
                entryPosition,
                new float3(5, 5, 5),
                eventId,
                "event_warrior"
            );
            RegisterTriggerZone(entryGate);

            // Create exit gate
            var exitGate = TriggerZoneTemplates.CreateEventExitGate(
                $"{eventId}_exit",
                exitPosition,
                new float3(5, 5, 5),
                eventId
            );
            RegisterTriggerZone(exitGate);
        }

        private PlayerStateSnapshot CreatePlayerSnapshot(ulong playerId)
        {
            try
            {
                var playerEntity = GetPlayerEntity(playerId);
                if (playerEntity == Entity.Null)
                {
                    return null;
                }

                var snapshot = new PlayerStateSnapshot
                {
                    PlayerId = playerId,
                    CharacterName = GetPlayerName(playerId)
                };

                // Save player position
                if (_entityManager.HasComponent<Translation>(playerEntity))
                {
                    snapshot.Position = _entityManager.GetComponentData<Translation>(playerEntity).Value;
                }

                // Save player stats
                SavePlayerStats(playerEntity, snapshot);

                // Save player inventory
                SavePlayerInventory(playerEntity, snapshot);

                // Save player equipment
                SavePlayerEquipment(playerEntity, snapshot);

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create player snapshot: {ex.Message}");
                return null;
            }
        }

        private bool RestorePlayerSnapshot(ulong playerId)
        {
            try
            {
                if (!_playerSnapshots.ContainsKey(playerId))
                {
                    _logger.LogWarning($"No snapshot found for player {playerId}");
                    return false;
                }

                var snapshot = _playerSnapshots[playerId];
                var playerEntity = GetPlayerEntity(playerId);
                if (playerEntity == Entity.Null)
                {
                    return false;
                }

                // Restore player stats
                RestorePlayerStats(playerEntity, snapshot);

                // Restore player inventory
                RestorePlayerInventory(playerEntity, snapshot);

                // Restore player equipment
                RestorePlayerEquipment(playerEntity, snapshot);

                // Clean up snapshot
                _playerSnapshots.Remove(playerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to restore player snapshot: {ex.Message}");
                return false;
            }
        }

        private Entity GetPlayerEntity(ulong playerId)
        {
            // Implementation would depend on V Rising's player entity system
            // This is a placeholder that would need to be implemented based on actual V Rising APIs
            return Entity.Null;
        }

        private string GetPlayerName(ulong playerId)
        {
            // Implementation would depend on V Rising's player name system
            return $"Player_{playerId}";
        }

        private Quest GetQuest(string questId)
        {
            // Implementation would retrieve quest from quest system
            return null;
        }

        private void ExecuteTriggerActions(ulong playerId, List<TriggerAction> actions, TriggerZone zone)
        {
            foreach (var action in actions)
            {
                ExecuteTriggerAction(playerId, action, zone);
            }
        }

        private void ExecuteTriggerAction(ulong playerId, TriggerAction action, TriggerZone zone)
        {
            try
            {
                switch (action.Type)
                {
                    case TriggerActionType.Teleport:
                        if (action.Parameters.ContainsKey("position"))
                        {
                            var position = (float3)action.Parameters["position"];
                            TeleportPlayer(playerId, position);
                        }
                        break;

                    case TriggerActionType.SaveInventory:
                        CreatePlayerSnapshot(playerId);
                        break;

                    case TriggerActionType.RestoreInventory:
                        RestorePlayerSnapshot(playerId);
                        break;

                    case TriggerActionType.SwapCharacter:
                        if (_characterTemplates.ContainsKey(action.Target))
                        {
                            ApplyCharacterTemplate(playerId, _characterTemplates[action.Target]);
                        }
                        break;

                    case TriggerActionType.SetFlag:
                        if (_playerProgress.ContainsKey(playerId) && action.Parameters.ContainsKey("value"))
                        {
                            var tracker = _playerProgress[playerId];
                            tracker.SetFlag(action.Target, (bool)action.Parameters["value"]);
                        }
                        break;

                    case TriggerActionType.SpawnBoss:
                        SpawnBoss(action.Target);
                        break;

                    case TriggerActionType.SendMessage:
                        SendMessageToPlayer(playerId, action.Parameters.GetValueOrDefault("message", "").ToString());
                        break;

                    // Add more action types as needed
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to execute trigger action {action.Type}: {ex.Message}");
            }
        }

        // Placeholder methods that would need V Rising-specific implementations
        private void ApplyTemplateStats(Entity playerEntity, EventCharacterTemplate template) { }
        private void ApplyTemplateEquipment(Entity playerEntity, EventCharacterTemplate template) { }
        private void ApplyTemplateInventory(Entity playerEntity, EventCharacterTemplate template) { }
        private void ApplyTemplateAbilities(Entity playerEntity, EventCharacterTemplate template) { }
        private void SavePlayerStats(Entity playerEntity, PlayerStateSnapshot snapshot) { }
        private void SavePlayerInventory(Entity playerEntity, PlayerStateSnapshot snapshot) { }
        private void SavePlayerEquipment(Entity playerEntity, PlayerStateSnapshot snapshot) { }
        private void RestorePlayerStats(Entity playerEntity, PlayerStateSnapshot snapshot) { }
        private void RestorePlayerInventory(Entity playerEntity, PlayerStateSnapshot snapshot) { }
        private void RestorePlayerEquipment(Entity playerEntity, PlayerStateSnapshot snapshot) { }
        private Entity SpawnBossEntity(BossEncounter boss) { return Entity.Null; }
        private void ExecuteDefeatTriggers(BossEncounter boss) { }
        private void SendMessageToPlayer(ulong playerId, string message) { }

        #endregion
    }

    /// <summary>
    /// Represents a remote event instance
    /// </summary>
    [Serializable]
    public class RemoteEvent
    {
        public string EventId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EventState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public float3 EntryPosition { get; set; }
        public float3 ExitPosition { get; set; }
        public string StartingQuestId { get; set; }
        public List<string> RequiredTemplates { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public RemoteEvent()
        {
            State = EventState.Created;
            CreatedAt = DateTime.UtcNow;
            RequiredTemplates = new List<string>();
            Properties = new Dictionary<string, object>();
        }

        public RemoteEvent(string eventId, string name, string description) : this()
        {
            EventId = eventId;
            Name = name;
            Description = description;
        }
    }

    [Serializable]
    public enum EventState
    {
        Created,
        Active,
        Paused,
        Stopped,
        Completed
    }

    /// <summary>
    /// Represents a teleporter node
    /// </summary>
    [Serializable]
    public class TeleporterNode
    {
        public string NodeId { get; set; }
        public string Name { get; set; }
        public float3 Position { get; set; }
        public float3 Destination { get; set; }
        public bool IsActive { get; set; }
        public float CooldownSeconds { get; set; }
        public Dictionary<ulong, DateTime> PlayerCooldowns { get; set; }

        public TeleporterNode()
        {
            IsActive = true;
            CooldownSeconds = 2f;
            PlayerCooldowns = new Dictionary<ulong, DateTime>();
        }

        public TeleporterNode(string nodeId, string name, float3 destination) : this()
        {
            NodeId = nodeId;
            Name = name;
            Destination = destination;
        }

        public bool IsOnCooldown(ulong playerId) => PlayerCooldowns.ContainsKey(playerId) &&
            DateTime.UtcNow < PlayerCooldowns[playerId].AddSeconds(CooldownSeconds);
    }
}