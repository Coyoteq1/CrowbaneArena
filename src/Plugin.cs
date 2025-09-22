using BepInEx;
using HarmonyLib;
using System.Reflection;
using Unity.Entities;
using CrowbaneArena.Controllers;
using CrowbaneArena.Services;
using CrowbaneArena.Utilities;
using CrowbaneFramework.Core;
using CrowbaneFramework.Commands;

namespace CrowbaneArena
{
    [BepInPlugin("com.crowbane.arena", "CrowbaneArena - Remote Event Controller", "2.0.0")]
    [BepInDependency("com.crowbane.framework")]
    public class Plugin : CrowbanePlugin
    {
            private CrowbaneArena.Commands.CrowbaneCommands _crowbaneCommands = new CrowbaneArena.Commands.CrowbaneCommands();

        public void Update()
        {
            // Only run if initialized
            if (!_initialized) return;

            // Check for 'O' key press
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.O))
            {
                // Create a ChatCommandContext for the local player
                var ctx = CreateLocalPlayerContext();
                if (ctx != null)
                {
                    // Only heal if inside the arena
                    if (IsPlayerInArena(ctx))
                    {
                        _crowbaneCommands.Heal(ctx);
                        Log.LogInfo("O key pressed: Self-heal triggered inside arena.");
                    }
                    else
                    {
                        Log.LogInfo("O key pressed: Not in arena, heal not triggered.");
                    }
                }
            }
        }

        private ChatCommandContext CreateLocalPlayerContext()
        {
            // TODO: Implement context creation for the local player
            // This may require accessing the current player entity and user entity
            return null;
        }

        private bool IsPlayerInArena(ChatCommandContext ctx)
        {
            // Use the same logic as EnsureInArena in CrowbaneCommands
            // This may require checking the player's current zone or session
            return true; // TODO: Implement actual arena check
        }
        public static Plugin Instance { get; private set; }
        public static ManualLogSource LogInstance { get; private set; }

        private Harmony _harmony;
        private bool _initialized = false;

        public override void Initialize()
        {
            Instance = this;
            LogInstance = Log;

            Log.LogInfo("=== CrowbaneArena - Remote Event Controller v2.0.0 ===");
            Log.LogInfo("Initializing comprehensive event management system...");

            try
            {
                // Initialize Harmony
                _harmony = new Harmony("com.crowbane.arena");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                // Register commands with CBC
                CBC.RegisterCommands(Assembly.GetExecutingAssembly(), this);

                Log.LogInfo("Plugin CrowbaneArena loaded successfully!");
                Log.LogInfo("Event system will initialize when the game world is ready.");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to load CrowbaneArena plugin: {ex.Message}");
                Log.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        public override void OnUnload()
        {
            try
            {
                Log.LogInfo("Unloading CrowbaneArena plugin...");

                // Cleanup services
                if (_initialized)
                {
                    CleanupServices();
                }

                // Unpatch Harmony
                _harmony?.UnpatchSelf();

                Log.LogInfo("CrowbaneArena plugin unloaded successfully!");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Error during plugin unload: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes all event system services when the game world is ready
        /// </summary>
        public void InitializeEventSystem()
        {
            if (_initialized)
            {
                Log.LogWarning("Event system already initialized");
                return;
            }

            try
            {
                Log.LogInfo("Initializing Remote Event Controller system...");

                // Get World and EntityManager
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                {
                    Log.LogError("Default world not available, retrying later...");
                    return;
                }

                var entityManager = world.EntityManager;

                // Initialize core services
                Log.LogInfo("Initializing Character Swap Service...");
                CharacterSwapService.Instance.Initialize(Log, entityManager, world);

                Log.LogInfo("Initializing Event Management Service...");
                EventManagementService.Instance.Initialize(Log, entityManager, world);

                Log.LogInfo("Initializing Remote Event Controller...");
                RemoteEventController.Instance.Initialize(Log, entityManager, world);

                Log.LogInfo("Initializing Admin Interface...");
                AdminInterface.Initialize(Log);

                // Create default event setup
                CreateDefaultEventSetup();

                _initialized = true;
                Log.LogInfo("=== Remote Event Controller System Initialized Successfully ===");
                Log.LogInfo("Available Commands:");
                Log.LogInfo("- event help : Show all available commands");
                Log.LogInfo("- event create <id> <name> : Create a new event");
                Log.LogInfo("- event status : Show system status");
                Log.LogInfo("- event template list : Show available character templates");
                Log.LogInfo("Use 'event help' for complete command list");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Failed to initialize event system: {ex.Message}");
                Log.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Creates a default event setup for testing and demonstration
        /// </summary>
        private void CreateDefaultEventSetup()
        {
            try
            {
                Log.LogInfo("Creating default event setup...");

                // Create a demo event
                var success = AdminInterface.CreateCompleteEventSetup(
                    "demo_arena",
                    "Demo Arena Event",
                    new Unity.Mathematics.float3(0, 0, 0)
                );

                if (success)
                {
                    Log.LogInfo("Default demo event 'demo_arena' created successfully!");
                    Log.LogInfo("Use 'event enter demo_arena' to test the system");
                }
                else
                {
                    Log.LogWarning("Failed to create default demo event");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Error creating default event setup: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup all services during shutdown
        /// </summary>
        private void CleanupServices()
        {
            try
            {
                Log.LogInfo("Cleaning up event system services...");

                // Force restore all character swaps
                var restored = CharacterSwapService.Instance.ForceRestoreAll();
                if (restored > 0)
                {
                    Log.LogInfo($"Emergency restored {restored} character swaps during shutdown");
                }

                // Stop all active events
                var activeEvents = EventManagementService.Instance.GetActiveEvents().ToList();
                foreach (var evt in activeEvents)
                {
                    RemoteEventController.Instance.StopEvent(evt.EventId);
                }

                Log.LogInfo("Event system cleanup completed");
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Error during service cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current initialization status
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Forces re-initialization of the event system
        /// </summary>
        public void ForceReinitialize()
        {
            Log.LogInfo("Forcing event system re-initialization...");
            _initialized = false;
            InitializeEventSystem();
        }

        /// <summary>
        /// Gets comprehensive system status
        /// </summary>
        public string GetSystemStatus()
        {
            if (!_initialized)
            {
                return "Event system not initialized";
            }

            try
            {
                var status = new System.Text.StringBuilder();
                status.AppendLine("=== CROWBANE ARENA SYSTEM STATUS ===");
                status.AppendLine($"Plugin Version: 2.0.0");
                status.AppendLine($"Initialized: {_initialized}");
                status.AppendLine($"Status Check: {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                status.AppendLine();

                // Get system metrics
                var metrics = AdminInterface.GetSystemMetrics();
                status.AppendLine(metrics);

                // Get health report
                var healthReport = AdminInterface.PerformHealthCheck();
                status.AppendLine($"Overall Health: {healthReport.OverallHealth}");
                status.AppendLine($"Event Management: {healthReport.EventManagementStatus}");
                status.AppendLine($"Character Swap: {healthReport.CharacterSwapStatus}");
                status.AppendLine($"Remote Controller: {healthReport.RemoteControllerStatus}");

                return status.ToString();
            }
            catch (System.Exception ex)
            {
                return $"Error getting system status: {ex.Message}";
            }
        }
    }
}
