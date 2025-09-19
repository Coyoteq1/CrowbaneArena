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

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service responsible for character swapping functionality
    /// Handles the complete replacement of player characters with event templates
    /// Following V Rising's ECS patterns with proper entity management
    /// </summary>
    public class CharacterSwapService
    {
        private static CharacterSwapService _instance;
        public static CharacterSwapService Instance => _instance ??= new CharacterSwapService();

        private readonly Dictionary<ulong, PlayerStateSnapshot> _activeSnapshots;
        private readonly Dictionary<ulong, string> _playerTemplates;
        private readonly Dictionary<string, EventCharacterTemplate> _templates;

        private ManualLogSource _logger;
        private EntityManager _entityManager;
        private World _world;

        public CharacterSwapService()
        {
            _activeSnapshots = new Dictionary<ulong, PlayerStateSnapshot>();
            _playerTemplates = new Dictionary<ulong, string>();
            _templates = new Dictionary<string, EventCharacterTemplate>();
        }

        public void Initialize(ManualLogSource logger, EntityManager entityManager, World world)
        {
            _logger = logger;
            _entityManager = entityManager;
            _world = world;

            LoadDefaultTemplates();
            // Following V Rising guideline 7.4: Use Plugin.LogInstance for centralized logging
            Plugin.LogInstance.LogInfo("CrowbaneArena - Character Swap Service initialized successfully");
        }

        #region Template Management

        /// <summary>
        /// Registers a character template for use in events
        /// </summary>
        public void RegisterTemplate(EventCharacterTemplate template)
        {
            if (template == null)
            {
                Plugin.LogInstance.LogError("CrowbaneArena - Cannot register null template");
                return;
            }

            _templates[template.TemplateId] = template;
            template.ModifiedAt = DateTime.UtcNow;
            Plugin.LogInstance.LogInfo($"CrowbaneArena - Registered character template: {template.TemplateId} - {template.Name}");
        }

        /// <summary>
        /// Gets a character template by ID
        /// </summary>
        public EventCharacterTemplate GetTemplate(string templateId)
        {
            return _templates.GetValueOrDefault(templateId);
        }

        /// <summary>
        /// Gets all available templates
        /// </summary>
        public IEnumerable<EventCharacterTemplate> GetAllTemplates()
        {
            return _templates.Values.Where(t => t.IsActive);
        }

        /// <summary>
        /// Updates an existing template
        /// </summary>
        public bool UpdateTemplate(EventCharacterTemplate template)
        {
            if (template == null || !_templates.ContainsKey(template.TemplateId))
            {
                return false;
            }

            template.ModifiedAt = DateTime.UtcNow;
            _templates[template.TemplateId] = template;
            Plugin.LogInstance.LogInfo($"CrowbaneArena - Updated character template: {template.TemplateId}");
            return true;
        }

        /// <summary>
        /// Removes a template
        /// </summary>
        public bool RemoveTemplate(string templateId)
        {
            if (_templates.Remove(templateId))
            {
                Plugin.LogInstance.LogInfo($"CrowbaneArena - Removed character template: {templateId}");
                return true;
            }
            return false;
        }

        #endregion

        #region Character Swapping

        /// <summary>
        /// Swaps a player's character with an event template
        /// </summary>
        public bool SwapPlayerCharacter(ulong playerId, string templateId)
        {
            try
            {
                if (!_templates.ContainsKey(templateId))
                {
                    _logger.LogError($"Template {templateId} not found");
                    return false;
                }

                var template = _templates[templateId];
                if (!template.IsActive)
                {
                    _logger.LogError($"Template {templateId} is not active");
                    return false;
                }

                // Check if player already has a swap active
                if (_activeSnapshots.ContainsKey(playerId))
                {
                    _logger.LogWarning($"Player {playerId} already has an active character swap");
                    return false;
                }

                // Create snapshot of current character
                var snapshot = CreatePlayerSnapshot(playerId);
                if (snapshot == null)
                {
                    _logger.LogError($"Failed to create snapshot for player {playerId}");
                    return false;
                }

                // Store the snapshot
                _activeSnapshots[playerId] = snapshot;
                _playerTemplates[playerId] = templateId;

                // Apply the template
                if (!ApplyTemplate(playerId, template))
                {
                    _logger.LogError($"Failed to apply template {templateId} to player {playerId}");
                    // Restore on failure
                    RestorePlayerCharacter(playerId);
                    return false;
                }

                _logger.LogInfo($"Successfully swapped player {playerId} to template {templateId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error swapping character for player {playerId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores a player's original character
        /// </summary>
        public bool RestorePlayerCharacter(ulong playerId)
        {
            try
            {
                if (!_activeSnapshots.ContainsKey(playerId))
                {
                    _logger.LogWarning($"No active character swap found for player {playerId}");
                    return false;
                }

                var snapshot = _activeSnapshots[playerId];

                // Restore the character
                if (!RestoreFromSnapshot(playerId, snapshot))
                {
                    _logger.LogError($"Failed to restore character for player {playerId}");
                    return false;
                }

                // Clean up
                _activeSnapshots.Remove(playerId);
                _playerTemplates.Remove(playerId);

                _logger.LogInfo($"Successfully restored character for player {playerId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring character for player {playerId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a player has an active character swap
        /// </summary>
        public bool HasActiveSwap(ulong playerId)
        {
            return _activeSnapshots.ContainsKey(playerId);
        }

        /// <summary>
        /// Gets the template ID for a player's current swap
        /// </summary>
        public string GetPlayerTemplate(ulong playerId)
        {
            return _playerTemplates.GetValueOrDefault(playerId);
        }

        #endregion

        #region Snapshot Management

        /// <summary>
        /// Creates a complete snapshot of a player's current state
        /// </summary>
        private PlayerStateSnapshot CreatePlayerSnapshot(ulong playerId)
        {
            try
            {
                var playerEntity = GetPlayerEntity(playerId);
                if (playerEntity == Entity.Null)
                {
                    _logger.LogError($"Player entity not found for {playerId}");
                    return null;
                }

                var snapshot = new PlayerStateSnapshot
                {
                    PlayerId = playerId,
                    CreatedAt = DateTime.UtcNow,
                    CharacterName = GetPlayerName(playerId)
                };

                // Capture position and rotation
                CapturePlayerTransform(playerEntity, snapshot);

                // Capture character appearance
                CapturePlayerAppearance(playerEntity, snapshot);

                // Capture stats
                CapturePlayerStats(playerEntity, snapshot);

                // Capture inventory
                CapturePlayerInventory(playerEntity, snapshot);

                // Capture equipment
                CapturePlayerEquipment(playerEntity, snapshot);

                // Capture abilities
                CapturePlayerAbilities(playerEntity, snapshot);

                // Capture progression data
                CapturePlayerProgression(playerEntity, snapshot);

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create player snapshot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Restores a player from a snapshot
        /// </summary>
        private bool RestoreFromSnapshot(ulong playerId, PlayerStateSnapshot snapshot)
        {
            try
            {
                var playerEntity = GetPlayerEntity(playerId);
                if (playerEntity == Entity.Null)
                {
                    _logger.LogError($"Player entity not found for {playerId}");
                    return false;
                }

                // Restore in reverse order of capture
                RestorePlayerProgression(playerEntity, snapshot);
                RestorePlayerAbilities(playerEntity, snapshot);
                RestorePlayerEquipment(playerEntity, snapshot);
                RestorePlayerInventory(playerEntity, snapshot);
                RestorePlayerStats(playerEntity, snapshot);
                RestorePlayerAppearance(playerEntity, snapshot);
                RestorePlayerTransform(playerEntity, snapshot);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to restore from snapshot: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Template Application

        /// <summary>
        /// Applies a character template to a player
        /// </summary>
        private bool ApplyTemplate(ulong playerId, EventCharacterTemplate template)
        {
            try
            {
                var playerEntity = GetPlayerEntity(playerId);
                if (playerEntity == Entity.Null)
                {
                    return false;
                }

                // Apply template appearance
                ApplyTemplateAppearance(playerEntity, template);

                // Apply template stats
                ApplyTemplateStats(playerEntity, template);

                // Clear and apply template inventory
                ClearPlayerInventory(playerEntity);
                ApplyTemplateInventory(playerEntity, template);

                // Clear and apply template equipment
                ClearPlayerEquipment(playerEntity);
                ApplyTemplateEquipment(playerEntity, template);

                // Apply template abilities
                ApplyTemplateAbilities(playerEntity, template);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to apply template: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Entity Operations

        /// <summary>
        /// Gets the player entity for a given player ID
        /// Following V Rising guidelines: use existing queries, safe component access, proper disposal
        /// </summary>
        private Entity GetPlayerEntity(ulong playerId)
        {
            NativeArray<Entity> entities = default;
            try
            {
                // Following V Rising guideline 7.1: Use existing system queries when possible
                // In actual implementation, this would use a predefined query from the system
                var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>());
                entities = query.ToEntityArray(Allocator.Temp);

                foreach (var entity in entities)
                {
                    // Following V Rising guideline 7.2: Always check component presence
                    if (!_entityManager.HasComponent<PlayerCharacter>(entity))
                        continue;

                    // Following V Rising guideline 7.2: Verify entity existence before access
                    if (!_entityManager.Exists(entity))
                        continue;

                    var playerChar = _entityManager.GetComponentData<PlayerCharacter>(entity);
                    // In actual V Rising implementation, this would use proper player ID component
                    // Example: if (playerChar.PlayerId == playerId) return entity;
                }

                return Entity.Null;
            }
            catch (Exception ex)
            {
                // Following V Rising guideline 7.4: Use Plugin.LogInstance for centralized logging
                Plugin.LogInstance.LogError($"CharacterSwapService - Error getting player entity {playerId}: {ex.Message}");
                return Entity.Null;
            }
            finally
            {
                // Following V Rising guideline 7.3: Use try-finally for NativeArray disposal
                if (entities.IsCreated)
                    entities.Dispose();
            }
        }

        /// <summary>
        /// Gets the player name for a given player ID
        /// </summary>
        private string GetPlayerName(ulong playerId)
        {
            // Placeholder implementation
            return $"Player_{playerId}";
        }

        #endregion

        #region Capture Methods

        private void CapturePlayerTransform(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Following V Rising guideline 7.2: Always check entity existence before component access
                if (!_entityManager.Exists(playerEntity))
                {
                    Plugin.LogInstance.LogError($"CharacterSwapService - Cannot capture transform: Entity {playerEntity} does not exist");
                    return;
                }

                // Following V Rising guideline 7.2: Check component presence before accessing data
                if (_entityManager.HasComponent<Translation>(playerEntity))
                {
                    snapshot.Position = _entityManager.GetComponentData<Translation>(playerEntity).Value;
                }

                if (_entityManager.HasComponent<Rotation>(playerEntity))
                {
                    var rotation = _entityManager.GetComponentData<Rotation>(playerEntity).Value;
                    snapshot.Rotation = new float3(rotation.value.x, rotation.value.y, rotation.value.z);
                }
            }
            catch (Exception ex)
            {
                // Following V Rising guideline 7.4: Centralized logging with context
                Plugin.LogInstance.LogError($"CharacterSwapService - Error capturing player transform for entity {playerEntity}: {ex.Message}");
            }
        }

        private void CapturePlayerAppearance(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Capture character model and customization data
                // This would depend on V Rising's character appearance system
                snapshot.CharacterModel = 0; // Placeholder
                snapshot.CustomizationData = new byte[0]; // Placeholder
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error capturing player appearance: {ex.Message}");
            }
        }

        private void CapturePlayerStats(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Following V Rising guideline 7.2: Always verify entity existence
                if (!_entityManager.Exists(playerEntity))
                {
                    Plugin.LogInstance.LogError($"CharacterSwapService - Cannot capture stats: Entity {playerEntity} does not exist");
                    return;
                }

                // Following V Rising guideline 7.2: Check component presence before accessing data
                if (_entityManager.HasComponent<Health>(playerEntity))
                {
                    var health = _entityManager.GetComponentData<Health>(playerEntity);
                    snapshot.Health = health.Value;
                    snapshot.MaxHealth = health.MaxHealth;
                }

                // Additional stats would be captured here following the same pattern
                snapshot.PowerLevel = 50; // Placeholder
            }
            catch (Exception ex)
            {
                // Following V Rising guideline 7.4: Include entity context in logging
                Plugin.LogInstance.LogError($"CharacterSwapService - Error capturing player stats for entity {playerEntity}: {ex.Message}");
            }
        }

        private void CapturePlayerInventory(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Capture inventory items
                // This would iterate through V Rising's inventory system
                snapshot.Inventory.Clear();

                // Placeholder implementation
                // In actual V Rising, this would query inventory components
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error capturing player inventory: {ex.Message}");
            }
        }

        private void CapturePlayerEquipment(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Capture equipped items
                // This would iterate through V Rising's equipment system
                snapshot.EquippedItems.Clear();

                // Placeholder implementation
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error capturing player equipment: {ex.Message}");
            }
        }

        private void CapturePlayerAbilities(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Capture abilities and skills
                snapshot.Abilities.Clear();
                snapshot.Skills.Clear();

                // Placeholder implementation
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error capturing player abilities: {ex.Message}");
            }
        }

        private void CapturePlayerProgression(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Capture experience, level, and progression data
                snapshot.Experience = 0; // Placeholder
                snapshot.Level = 1; // Placeholder
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error capturing player progression: {ex.Message}");
            }
        }

        #endregion

        #region Restore Methods

        private void RestorePlayerTransform(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Following V Rising guideline 7.2: Always verify entity existence
                if (!_entityManager.Exists(playerEntity))
                {
                    Plugin.LogInstance.LogError($"CharacterSwapService - Cannot restore transform: Entity {playerEntity} does not exist");
                    return;
                }

                // Following V Rising guideline 7.2: Check component presence before accessing data
                if (_entityManager.HasComponent<Translation>(playerEntity))
                {
                    var translation = _entityManager.GetComponentData<Translation>(playerEntity);
                    translation.Value = snapshot.Position;
                    _entityManager.SetComponentData(playerEntity, translation);
                }

                if (_entityManager.HasComponent<Rotation>(playerEntity))
                {
                    var rotation = _entityManager.GetComponentData<Rotation>(playerEntity);
                    // Restore rotation from snapshot if available
                    _entityManager.SetComponentData(playerEntity, rotation);
                }
            }
            catch (Exception ex)
            {
                // Following V Rising guideline 7.4: Include entity context in logging
                Plugin.LogInstance.LogError($"CharacterSwapService - Error restoring player transform for entity {playerEntity}: {ex.Message}");
            }
        }

        private void RestorePlayerAppearance(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Restore character appearance
                // Implementation would depend on V Rising's character system
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring player appearance: {ex.Message}");
            }
        }

        private void RestorePlayerStats(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Following V Rising guideline 7.2: Always verify entity existence
                if (!_entityManager.Exists(playerEntity))
                {
                    Plugin.LogInstance.LogError($"CharacterSwapService - Cannot restore stats: Entity {playerEntity} does not exist");
                    return;
                }

                // Following V Rising guideline 7.2: Check component presence before accessing data
                if (_entityManager.HasComponent<Health>(playerEntity))
                {
                    var health = _entityManager.GetComponentData<Health>(playerEntity);
                    health.Value = snapshot.Health;
                    health.MaxHealth = snapshot.MaxHealth;
                    _entityManager.SetComponentData(playerEntity, health);
                }
            }
            catch (Exception ex)
            {
                // Following V Rising guideline 7.4: Include entity context in logging
                Plugin.LogInstance.LogError($"CharacterSwapService - Error restoring player stats for entity {playerEntity}: {ex.Message}");
            }
        }

        private void RestorePlayerInventory(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Clear current inventory and restore from snapshot
                ClearPlayerInventory(playerEntity);

                foreach (var item in snapshot.Inventory)
                {
                    // Add item back to inventory
                    // Implementation would use V Rising's inventory system
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring player inventory: {ex.Message}");
            }
        }

        private void RestorePlayerEquipment(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Clear current equipment and restore from snapshot
                ClearPlayerEquipment(playerEntity);

                foreach (var item in snapshot.EquippedItems)
                {
                    // Equip item
                    // Implementation would use V Rising's equipment system
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring player equipment: {ex.Message}");
            }
        }

        private void RestorePlayerAbilities(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Restore abilities and skills
                // Implementation would use V Rising's ability system
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring player abilities: {ex.Message}");
            }
        }

        private void RestorePlayerProgression(Entity playerEntity, PlayerStateSnapshot snapshot)
        {
            try
            {
                // Restore experience and level
                // Implementation would use V Rising's progression system
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring player progression: {ex.Message}");
            }
        }

        #endregion

        #region Template Application Methods

        private void ApplyTemplateAppearance(Entity playerEntity, EventCharacterTemplate template)
        {
            try
            {
                // Apply template character model and customization
                // Implementation would depend on V Rising's character appearance system
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error applying template appearance: {ex.Message}");
            }
        }

        private void ApplyTemplateStats(Entity playerEntity, EventCharacterTemplate template)
        {
            try
            {
                // Apply template stats
                if (_entityManager.HasComponent<Health>(playerEntity))
                {
                    var health = _entityManager.GetComponentData<Health>(playerEntity);
                    health.MaxHealth = template.MaxHealth;
                    health.Value = template.MaxHealth; // Full health
                    _entityManager.SetComponentData(playerEntity, health);
                }

                // Apply other stats like energy, power level, etc.
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error applying template stats: {ex.Message}");
            }
        }

        private void ApplyTemplateInventory(Entity playerEntity, EventCharacterTemplate template)
        {
            try
            {
                // Add template inventory items
                foreach (var item in template.StartingInventory)
                {
                    // Add item to player inventory
                    // Implementation would use V Rising's inventory system
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error applying template inventory: {ex.Message}");
            }
        }

        private void ApplyTemplateEquipment(Entity playerEntity, EventCharacterTemplate template)
        {
            try
            {
                // Equip template equipment
                foreach (var item in template.StartingEquipment)
                {
                    // Equip item on player
                    // Implementation would use V Rising's equipment system
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error applying template equipment: {ex.Message}");
            }
        }

        private void ApplyTemplateAbilities(Entity playerEntity, EventCharacterTemplate template)
        {
            try
            {
                // Apply template abilities
                foreach (var ability in template.FixedAbilities)
                {
                    // Grant ability to player
                    // Implementation would use V Rising's ability system
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error applying template abilities: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        private void ClearPlayerInventory(Entity playerEntity)
        {
            try
            {
                // Clear all items from player inventory
                // Implementation would use V Rising's inventory system
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error clearing player inventory: {ex.Message}");
            }
        }

        private void ClearPlayerEquipment(Entity playerEntity)
        {
            try
            {
                // Unequip all items from player
                // Implementation would use V Rising's equipment system
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error clearing player equipment: {ex.Message}");
            }
        }

        private void LoadDefaultTemplates()
        {
            // Load default character templates
            RegisterTemplate(DefaultEventTemplates.CreateWarriorTemplate());
            RegisterTemplate(DefaultEventTemplates.CreateMageTemplate());
            RegisterTemplate(DefaultEventTemplates.CreateArcherTemplate());
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Gets statistics about active character swaps
        /// </summary>
        public CharacterSwapStatistics GetStatistics()
        {
            return new CharacterSwapStatistics
            {
                ActiveSwaps = _activeSnapshots.Count,
                TotalTemplates = _templates.Count,
                ActiveTemplates = _templates.Values.Count(t => t.IsActive),
                OldestSwap = _activeSnapshots.Values.Any() ?
                    _activeSnapshots.Values.Min(s => s.CreatedAt) : (DateTime?)null
            };
        }

        /// <summary>
        /// Forces restoration of all active swaps (emergency cleanup)
        /// </summary>
        public int ForceRestoreAll()
        {
            var restored = 0;
            var playerIds = _activeSnapshots.Keys.ToList();

            foreach (var playerId in playerIds)
            {
                if (RestorePlayerCharacter(playerId))
                {
                    restored++;
                }
            }

            Plugin.LogInstance.LogInfo($"CrowbaneArena - Force restored {restored} character swaps");
            return restored;
        }

        /// <summary>
        /// Validates a template for completeness and correctness
        /// </summary>
        public TemplateValidationResult ValidateTemplate(EventCharacterTemplate template)
        {
            var result = new TemplateValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(template.TemplateId))
            {
                result.IsValid = false;
                result.Errors.Add("Template ID is required");
            }

            if (string.IsNullOrEmpty(template.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Template name is required");
            }

            if (template.MaxHealth <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("Max health must be greater than 0");
            }

            if (template.MaxEnergy < 0)
            {
                result.IsValid = false;
                result.Errors.Add("Max energy cannot be negative");
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Statistics about character swap operations
    /// </summary>
    public class CharacterSwapStatistics
    {
        public int ActiveSwaps { get; set; }
        public int TotalTemplates { get; set; }
        public int ActiveTemplates { get; set; }
        public DateTime? OldestSwap { get; set; }
    }

    /// <summary>
    /// Result of template validation
    /// </summary>
    public class TemplateValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}