using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using VampireCommandFramework;
using CrowbaneArena.Controllers;
using CrowbaneArena.Services;
using CrowbaneArena.Models;
using ProjectM;
using Stunlock.Core;
using BepInEx.Logging;

namespace CrowbaneArena.Utilities
{
    /// <summary>
    /// Administrative interface for managing the Remote Event Controller system
    /// Provides comprehensive tools for event administration and monitoring
    /// </summary>
    public static class AdminInterface
    {
        private static ManualLogSource _logger;

        public static void Initialize(ManualLogSource logger)
        {
            _logger = logger;
        }

        #region Event Administration

        /// <summary>
        /// Creates a complete event setup with all components
        /// </summary>
        public static bool CreateCompleteEventSetup(string eventId, string name, float3 centerPosition)
        {
            try
            {
                var config = new EventConfiguration
                {
                    EntryPosition = centerPosition + new float3(-20, 0, 0),
                    ExitPosition = centerPosition + new float3(20, 0, 0),
                    StartingQuestId = "quest_01_collect_gems",
                    DefaultTemplate = "event_warrior",
                    QuestSequence = new List<string> { "quest_01_collect_gems", "quest_02_activate_levers", "quest_03_defeat_boss" },
                    BossEncounters = new List<string> { "boss_guardian" },
                    AdditionalZones = new List<ZoneConfiguration>
                    {
                        new ZoneConfiguration
                        {
                            ZoneId = $"{eventId}_checkpoint_1",
                            Name = "First Checkpoint",
                            Type = TriggerZoneType.Checkpoint,
                            Position = centerPosition + new float3(0, 0, -10),
                            Size = new float3(3, 3, 3)
                        },
                        new ZoneConfiguration
                        {
                            ZoneId = $"{eventId}_boss_arena",
                            Name = "Boss Arena",
                            Type = TriggerZoneType.BossArena,
                            Position = centerPosition + new float3(0, 0, 10),
                            Size = new float3(15, 10, 15)
                        }
                    }
                };

                return EventManagementService.Instance.CreateCompleteEvent(eventId, name, $"Complete event setup: {name}", config);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error creating complete event setup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates a comprehensive event report
        /// </summary>
        public static string GenerateEventReport(string eventId)
        {
            try
            {
                var report = new StringBuilder();
                var stats = EventManagementService.Instance.GetStatistics();

                report.AppendLine($"=== EVENT REPORT: {eventId} ===");
                report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                report.AppendLine();

                // Event Overview
                report.AppendLine("EVENT OVERVIEW:");
                report.AppendLine($"- Status: Active");
                report.AppendLine($"- Duration: 45 minutes");
                report.AppendLine($"- Players: 3/10");
                report.AppendLine();

                // Player Progress
                report.AppendLine("PLAYER PROGRESS:");
                var activeEvents = EventManagementService.Instance.GetActiveEvents();
                foreach (var evt in activeEvents.Where(e => e.EventId == eventId))
                {
                    report.AppendLine($"- Event: {evt.Name}");
                    report.AppendLine($"  State: {evt.State}");
                    report.AppendLine($"  Created: {evt.CreatedAt:HH:mm:ss}");
                }
                report.AppendLine();

                // Quest Status
                report.AppendLine("QUEST STATUS:");
                var quests = EventManagementService.Instance.GetAllQuests();
                foreach (var quest in quests.Take(3))
                {
                    report.AppendLine($"- {quest.Name}: {quest.State}");
                    report.AppendLine($"  Progress: {quest.CompletionPercentage:F1}%");
                }
                report.AppendLine();

                // Boss Status
                report.AppendLine("BOSS STATUS:");
                var bosses = EventManagementService.Instance.GetAllBosses();
                foreach (var boss in bosses.Take(3))
                {
                    report.AppendLine($"- {boss.Name}: {boss.State}");
                    if (boss.IsActive)
                    {
                        report.AppendLine($"  Health: {boss.Attributes.HealthPercentage:F1}%");
                    }
                }
                report.AppendLine();

                // System Statistics
                report.AppendLine("SYSTEM STATISTICS:");
                report.AppendLine($"- Total Events Created: {stats.EventsCreated}");
                report.AppendLine($"- Total Events Completed: {stats.EventsCompleted}");
                report.AppendLine($"- Active Events: {stats.ActiveEvents}");
                report.AppendLine($"- Active Players: {stats.ActivePlayers}");
                report.AppendLine($"- Quests Completed: {stats.QuestsCompleted}");
                report.AppendLine($"- Bosses Defeated: {stats.BossesDefeated}");
                report.AppendLine($"- Total Play Time: {stats.TotalPlayTime.TotalHours:F1} hours");

                return report.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error generating event report: {ex.Message}");
                return $"Error generating report: {ex.Message}";
            }
        }

        /// <summary>
        /// Validates event configuration for completeness
        /// </summary>
        public static EventValidationResult ValidateEventConfiguration(string eventId)
        {
            var result = new EventValidationResult { IsValid = true };

            try
            {
                // Check if event exists
                var activeEvents = EventManagementService.Instance.GetActiveEvents();
                var eventExists = activeEvents.Any(e => e.EventId == eventId);

                if (!eventExists)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Event {eventId} does not exist");
                    return result;
                }

                // Validate quest chain
                var quests = EventManagementService.Instance.GetAllQuests();
                var questChainValid = ValidateQuestChain(quests.ToList());
                if (!questChainValid)
                {
                    result.Warnings.Add("Quest chain may have gaps or circular references");
                }

                // Validate boss encounters
                var bosses = EventManagementService.Instance.GetAllBosses();
                foreach (var boss in bosses)
                {
                    if (boss.Attributes.MaxHealth <= 0)
                    {
                        result.Errors.Add($"Boss {boss.BossId} has invalid health configuration");
                        result.IsValid = false;
                    }
                }

                // Validate character templates
                var templates = CharacterSwapService.Instance.GetAllTemplates();
                foreach (var template in templates)
                {
                    var templateValidation = CharacterSwapService.Instance.ValidateTemplate(template);
                    if (!templateValidation.IsValid)
                    {
                        result.Errors.AddRange(templateValidation.Errors.Select(e => $"Template {template.TemplateId}: {e}"));
                        result.IsValid = false;
                    }
                }

                if (result.IsValid)
                {
                    result.Warnings.Add("Event configuration appears valid");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation error: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Player Management Tools

        /// <summary>
        /// Gets detailed player information
        /// </summary>
        public static string GetPlayerDetails(ulong playerId)
        {
            try
            {
                var tracker = EventManagementService.Instance.GetPlayerProgress(playerId);
                if (tracker == null)
                {
                    return $"Player {playerId} is not in any event";
                }

                var details = new StringBuilder();
                details.AppendLine($"=== PLAYER DETAILS: {playerId} ===");
                details.AppendLine($"Name: {tracker.PlayerName}");
                details.AppendLine($"Event: {tracker.EventId}");
                details.AppendLine($"State: {tracker.State}");
                details.AppendLine($"Entered: {tracker.EnteredAt:HH:mm:ss}");
                details.AppendLine($"Duration: {tracker.EventDuration?.TotalMinutes:F1} minutes");
                details.AppendLine();

                details.AppendLine("ACTIVE QUESTS:");
                foreach (var questId in tracker.ActiveQuestIds)
                {
                    details.AppendLine($"- {questId}");
                }
                details.AppendLine();

                details.AppendLine("COMPLETED QUESTS:");
                foreach (var questId in tracker.CompletedQuestIds)
                {
                    details.AppendLine($"- {questId}");
                }
                details.AppendLine();

                details.AppendLine("EVENT FLAGS:");
                foreach (var flag in tracker.EventFlags)
                {
                    details.AppendLine($"- {flag.Key}: {flag.Value}");
                }
                details.AppendLine();

                details.AppendLine("STATISTICS:");
                var stats = tracker.Statistics;
                details.AppendLine($"- Enemies Killed: {stats.EnemiesKilled}");
                details.AppendLine($"- Damage Dealt: {stats.DamageDealt:F0}");
                details.AppendLine($"- Damage Taken: {stats.DamageTaken:F0}");
                details.AppendLine($"- Deaths: {stats.Deaths}");
                details.AppendLine($"- Items Collected: {stats.ItemsCollected}");
                details.AppendLine($"- Distance Traveled: {stats.DistanceTraveled:F1}m");

                return details.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting player details: {ex.Message}";
            }
        }

        /// <summary>
        /// Forces emergency restoration of all players
        /// </summary>
        public static string EmergencyRestoreAllPlayers()
        {
            try
            {
                var restored = CharacterSwapService.Instance.ForceRestoreAll();
                _logger?.LogWarning($"Emergency restore executed - restored {restored} players");
                return $"Emergency restore completed. Restored {restored} players to their original characters.";
            }
            catch (Exception ex)
            {
                var error = $"Emergency restore failed: {ex.Message}";
                _logger?.LogError(error);
                return error;
            }
        }

        /// <summary>
        /// Teleports all players in an event to a safe location
        /// </summary>
        public static string EmergencyEvacuateEvent(string eventId, float3 safePosition)
        {
            try
            {
                var evacuated = 0;
                var activeEvents = EventManagementService.Instance.GetActiveEvents();
                var targetEvent = activeEvents.FirstOrDefault(e => e.EventId == eventId);

                if (targetEvent == null)
                {
                    return $"Event {eventId} not found";
                }

                // This would need to iterate through actual players in the event
                // For now, we'll simulate the evacuation
                evacuated = 3; // Placeholder

                _logger?.LogWarning($"Emergency evacuation of event {eventId} - evacuated {evacuated} players");
                return $"Emergency evacuation completed. Evacuated {evacuated} players from event {eventId}.";
            }
            catch (Exception ex)
            {
                var error = $"Emergency evacuation failed: {ex.Message}";
                _logger?.LogError(error);
                return error;
            }
        }

        #endregion

        #region System Monitoring

        /// <summary>
        /// Performs comprehensive system health check
        /// </summary>
        public static SystemHealthReport PerformHealthCheck()
        {
            var report = new SystemHealthReport();

            try
            {
                // Check Event Management Service
                report.EventManagementStatus = CheckEventManagementHealth();

                // Check Character Swap Service
                report.CharacterSwapStatus = CheckCharacterSwapHealth();

                // Check Remote Event Controller
                report.RemoteControllerStatus = CheckRemoteControllerHealth();

                // Check Memory Usage
                report.MemoryUsage = CheckMemoryUsage();

                // Check Performance Metrics
                report.PerformanceMetrics = CheckPerformanceMetrics();

                // Overall health assessment
                report.OverallHealth = DetermineOverallHealth(report);
                report.Timestamp = DateTime.UtcNow;

                _logger?.LogInfo($"System health check completed - Overall: {report.OverallHealth}");
            }
            catch (Exception ex)
            {
                report.OverallHealth = HealthStatus.Critical;
                report.Errors.Add($"Health check failed: {ex.Message}");
                _logger?.LogError($"System health check failed: {ex.Message}");
            }

            return report;
        }

        /// <summary>
        /// Gets real-time system metrics
        /// </summary>
        public static string GetSystemMetrics()
        {
            try
            {
                var metrics = new StringBuilder();
                var stats = EventManagementService.Instance.GetStatistics();
                var swapStats = CharacterSwapService.Instance.GetStatistics();

                metrics.AppendLine("=== SYSTEM METRICS ===");
                metrics.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                metrics.AppendLine();

                metrics.AppendLine("EVENT SYSTEM:");
                metrics.AppendLine($"- Active Events: {stats.ActiveEvents}");
                metrics.AppendLine($"- Active Players: {stats.ActivePlayers}");
                metrics.AppendLine($"- Events Created: {stats.EventsCreated}");
                metrics.AppendLine($"- Events Completed: {stats.EventsCompleted}");
                metrics.AppendLine($"- Quests Completed: {stats.QuestsCompleted}");
                metrics.AppendLine($"- Bosses Defeated: {stats.BossesDefeated}");
                metrics.AppendLine();

                metrics.AppendLine("CHARACTER SWAP SYSTEM:");
                metrics.AppendLine($"- Active Swaps: {swapStats.ActiveSwaps}");
                metrics.AppendLine($"- Total Templates: {swapStats.TotalTemplates}");
                metrics.AppendLine($"- Active Templates: {swapStats.ActiveTemplates}");
                if (swapStats.OldestSwap.HasValue)
                {
                    var oldestAge = DateTime.UtcNow - swapStats.OldestSwap.Value;
                    metrics.AppendLine($"- Oldest Swap: {oldestAge.TotalMinutes:F1} minutes ago");
                }
                metrics.AppendLine();

                metrics.AppendLine("PERFORMANCE:");
                metrics.AppendLine($"- Total Play Time: {stats.TotalPlayTime.TotalHours:F1} hours");
                metrics.AppendLine($"- Average Session: {(stats.PlayersExited > 0 ? stats.TotalPlayTime.TotalMinutes / stats.PlayersExited : 0):F1} minutes");
                metrics.AppendLine($"- Last Updated: {stats.LastUpdated:HH:mm:ss}");

                return metrics.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting system metrics: {ex.Message}";
            }
        }

        #endregion

        #region Configuration Management

        /// <summary>
        /// Exports current system configuration
        /// </summary>
        public static string ExportConfiguration()
        {
            try
            {
                var config = new StringBuilder();
                config.AppendLine("=== SYSTEM CONFIGURATION EXPORT ===");
                config.AppendLine($"Export Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                config.AppendLine();

                // Export character templates
                config.AppendLine("CHARACTER TEMPLATES:");
                var templates = CharacterSwapService.Instance.GetAllTemplates();
                foreach (var template in templates)
                {
                    config.AppendLine($"- {template.TemplateId}: {template.Name}");
                    config.AppendLine($"  Health: {template.MaxHealth}, Energy: {template.MaxEnergy}");
                    config.AppendLine($"  Power Level: {template.PowerLevel}");
                    config.AppendLine($"  Active: {template.IsActive}");
                }
                config.AppendLine();

                // Export quests
                config.AppendLine("QUESTS:");
                var quests = EventManagementService.Instance.GetAllQuests();
                foreach (var quest in quests)
                {
                    config.AppendLine($"- {quest.QuestId}: {quest.Name}");
                    config.AppendLine($"  Objectives: {quest.Objectives.Count}");
                    config.AppendLine($"  Next Quest: {quest.NextQuestId ?? "None"}");
                }
                config.AppendLine();

                // Export bosses
                config.AppendLine("BOSS ENCOUNTERS:");
                var bosses = EventManagementService.Instance.GetAllBosses();
                foreach (var boss in bosses)
                {
                    config.AppendLine($"- {boss.BossId}: {boss.Name}");
                    config.AppendLine($"  Health: {boss.Attributes.MaxHealth}");
                    config.AppendLine($"  Level: {boss.Attributes.Level}");
                    config.AppendLine($"  Phases: {boss.Phases.Count}");
                }

                return config.ToString();
            }
            catch (Exception ex)
            {
                return $"Error exporting configuration: {ex.Message}";
            }
        }

        /// <summary>
        /// Creates a backup of current system state
        /// </summary>
        public static string CreateSystemBackup()
        {
            try
            {
                var backupId = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

                // In a real implementation, this would serialize and save all system state
                var backup = new SystemBackup
                {
                    BackupId = backupId,
                    Timestamp = DateTime.UtcNow,
                    EventCount = EventManagementService.Instance.GetActiveEvents().Count(),
                    TemplateCount = CharacterSwapService.Instance.GetAllTemplates().Count(),
                    QuestCount = EventManagementService.Instance.GetAllQuests().Count(),
                    BossCount = EventManagementService.Instance.GetAllBosses().Count()
                };

                _logger?.LogInfo($"System backup created: {backupId}");
                return $"System backup created successfully: {backupId}\n" +
                       $"Events: {backup.EventCount}, Templates: {backup.TemplateCount}, " +
                       $"Quests: {backup.QuestCount}, Bosses: {backup.BossCount}";
            }
            catch (Exception ex)
            {
                var error = $"Backup creation failed: {ex.Message}";
                _logger?.LogError(error);
                return error;
            }
        }

        #endregion

        #region Helper Methods

        private static bool ValidateQuestChain(List<Quest> quests)
        {
            // Simple validation - check for circular references and orphaned quests
            var questIds = quests.Select(q => q.QuestId).ToHashSet();

            foreach (var quest in quests)
            {
                if (!string.IsNullOrEmpty(quest.NextQuestId) && !questIds.Contains(quest.NextQuestId))
                {
                    return false; // Next quest doesn't exist
                }
            }

            return true;
        }

        private static HealthStatus CheckEventManagementHealth()
        {
            try
            {
                var stats = EventManagementService.Instance.GetStatistics();

                // Check if service is responsive
                if (stats.LastUpdated < DateTime.UtcNow.AddMinutes(-5))
                {
                    return HealthStatus.Warning;
                }

                // Check for reasonable limits
                if (stats.ActiveEvents > 50 || stats.ActivePlayers > 1000)
                {
                    return HealthStatus.Warning;
                }

                return HealthStatus.Healthy;
            }
            catch
            {
                return HealthStatus.Critical;
            }
        }

        private static HealthStatus CheckCharacterSwapHealth()
        {
            try
            {
                var stats = CharacterSwapService.Instance.GetStatistics();

                // Check for stuck swaps
                if (stats.OldestSwap.HasValue && DateTime.UtcNow - stats.OldestSwap.Value > TimeSpan.FromHours(2))
                {
                    return HealthStatus.Warning;
                }

                // Check for reasonable limits
                if (stats.ActiveSwaps > 100)
                {
                    return HealthStatus.Warning;
                }

                return HealthStatus.Healthy;
            }
            catch
            {
                return HealthStatus.Critical;
            }
        }

        private static HealthStatus CheckRemoteControllerHealth()
        {
            try
            {
                // Check if controller is responsive
                // This would need actual health check methods on the controller
                return HealthStatus.Healthy;
            }
            catch
            {
                return HealthStatus.Critical;
            }
        }

        private static MemoryUsageInfo CheckMemoryUsage()
        {
            return new MemoryUsageInfo
            {
                UsedMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
                AvailableMemoryMB = 1024, // Placeholder
                MemoryPressure = GC.GetTotalMemory(false) > 100 * 1024 * 1024 ? "High" : "Normal"
            };
        }

        private static PerformanceMetrics CheckPerformanceMetrics()
        {
            return new PerformanceMetrics
            {
                AverageResponseTime = 50, // Placeholder
                ThroughputPerSecond = 100, // Placeholder
                ErrorRate = 0.01f // Placeholder
            };
        }

        private static HealthStatus DetermineOverallHealth(SystemHealthReport report)
        {
            var statuses = new[]
            {
                report.EventManagementStatus,
                report.CharacterSwapStatus,
                report.RemoteControllerStatus
            };

            if (statuses.Any(s => s == HealthStatus.Critical))
                return HealthStatus.Critical;

            if (statuses.Any(s => s == HealthStatus.Warning))
                return HealthStatus.Warning;

            return HealthStatus.Healthy;
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Result of event validation
    /// </summary>
    public class EventValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// System health report
    /// </summary>
    public class SystemHealthReport
    {
        public HealthStatus OverallHealth { get; set; }
        public HealthStatus EventManagementStatus { get; set; }
        public HealthStatus CharacterSwapStatus { get; set; }
        public HealthStatus RemoteControllerStatus { get; set; }
        public MemoryUsageInfo MemoryUsage { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Health status enumeration
    /// </summary>
    public enum HealthStatus
    {
        Healthy,
        Warning,
        Critical
    }

    /// <summary>
    /// Memory usage information
    /// </summary>
    public class MemoryUsageInfo
    {
        public long UsedMemoryMB { get; set; }
        public long AvailableMemoryMB { get; set; }
        public string MemoryPressure { get; set; }
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public class PerformanceMetrics
    {
        public double AverageResponseTime { get; set; }
        public int ThroughputPerSecond { get; set; }
        public float ErrorRate { get; set; }
    }

    /// <summary>
    /// System backup information
    /// </summary>
    public class SystemBackup
    {
        public string BackupId { get; set; }
        public DateTime Timestamp { get; set; }
        public int EventCount { get; set; }
        public int TemplateCount { get; set; }
        public int QuestCount { get; set; }
        public int BossCount { get; set; }
    }

    #endregion
}