using HarmonyLib;
using ProjectM;
using Unity.Entities;
using BepInEx.Logging;

namespace CrowbaneArena.Patches
{
    /// <summary>
    /// Harmony patch to initialize the event system when the game world is ready
    /// Following V Rising's ECS patterns and initialization lifecycle
    /// </summary>
    [HarmonyPatch]
    public static class SystemInitializationPatch
    {
        private static bool _hasInitialized = false;

        /// <summary>
        /// Patch the GameDataSystem.OnCreate method to initialize our event system
        /// This ensures we initialize after the game's core systems are ready
        /// Following V Rising guideline 7.6: HarmonyPrefix methods return void unless control flow needs altering
        /// </summary>
        [HarmonyPatch(typeof(GameDataSystem), nameof(GameDataSystem.OnCreate))]
        [HarmonyPostfix]
        public static void GameDataSystem_OnCreate_Postfix(GameDataSystem __instance)
        {
            try
            {
                if (_hasInitialized)
                {
                    return;
                }

                // Following V Rising guideline 7.4: Use Plugin.LogInstance for centralized logging
                Plugin.LogInstance.LogInfo("CrowbaneArena - GameDataSystem initialized, starting event system initialization...");

                // Small delay to ensure all systems are fully ready
                UnityEngine.Coroutines.CoroutineManager.StartCoroutine(DelayedInitialization());
            }
            catch (System.Exception ex)
            {
                // Following V Rising guideline 7.4: Include system context in logging
                Plugin.LogInstance.LogError($"CrowbaneArena - Error in GameDataSystem patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Delayed initialization to ensure all game systems are ready
        /// Following V Rising guidelines for proper initialization timing
        /// </summary>
        private static System.Collections.IEnumerator DelayedInitialization()
        {
            // Wait a few frames to ensure everything is ready
            yield return new UnityEngine.WaitForSeconds(2f);

            try
            {
                if (!_hasInitialized && Plugin.Instance != null)
                {
                    // Following V Rising guideline 7.4: Use Plugin.LogInstance for centralized logging
                    Plugin.LogInstance.LogInfo("CrowbaneArena - Starting delayed event system initialization...");
                    Plugin.Instance.InitializeEventSystem();
                    _hasInitialized = true;
                }
            }
            catch (System.Exception ex)
            {
                // Following V Rising guideline 7.4: Include system context in logging
                Plugin.LogInstance.LogError($"CrowbaneArena - Error during delayed initialization: {ex.Message}");
            }
        }

        /// <summary>
        /// Patch to handle world cleanup when shutting down
        /// Following V Rising guideline 7.6: HarmonyPrefix methods return void unless control flow needs altering
        /// </summary>
        [HarmonyPatch(typeof(World), nameof(World.Dispose))]
        [HarmonyPrefix]
        public static void World_Dispose_Prefix(World __instance)
        {
            try
            {
                if (__instance == World.DefaultGameObjectInjectionWorld && _hasInitialized)
                {
                    // Following V Rising guideline 7.4: Use Plugin.LogInstance for centralized logging
                    Plugin.LogInstance.LogInfo("CrowbaneArena - World disposing, cleaning up event system...");
                    _hasInitialized = false;
                }
            }
            catch (System.Exception ex)
            {
                // Following V Rising guideline 7.4: Include system context in logging
                Plugin.LogInstance.LogError($"CrowbaneArena - Error in World dispose patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset initialization flag for testing purposes
        /// </summary>
        public static void ResetInitialization()
        {
            _hasInitialized = false;
            Logger?.LogInfo("Event system initialization flag reset");
        }

        /// <summary>
        /// Check if the system has been initialized
        /// </summary>
        public static bool HasInitialized => _hasInitialized;
    }
}