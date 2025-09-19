// -------------------------------------------------------------------------------- //
// A V Rising mod that adds a custom item with a custom icon.
//
// INSTRUCTIONS:
// 1.  Make sure your Visual Studio project has references to the game's DLLs.
//     (See previous instructions: Assembly-CSharp.dll, ProjectM.dll, etc.)
// 2.  Change the GUID in the [BepInPlugin] attribute to be unique.
//     (e.g., "YourName.YourModName")
// 3.  Add your icon image (e.g., "MyPic.png") to your project.
// 4.  In Visual Studio, click the image file in the Solution Explorer, go to its
//     Properties, and set "Build Action" to "Embedded Resource".
// 5.  Update the constants in the `ItemCreator` class to match your item's desired
//     name, description, and the resource path to your icon.
// -------------------------------------------------------------------------------- //

using BepInEx;
using HarmonyLib;
using ProjectM;
using Unity.Entities;
using UnityEngine;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using Stunlock.Core;

// This is the namespace of your project.
namespace Items__.NET_Framework_
{
    // The BepInPlugin attribute tells the game how to load your mod.
    // The first parameter (GUID) MUST be unique. Replace "YourName.CustomItemMod".
    [BepInPlugin("YourName.CustomItemMod", "My Custom Item Mod", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        // Harmony is the library we use to patch the game's methods.
        private Harmony _harmony;

        // A static logger instance so we can log messages from anywhere in our mod.
        internal static ManualLogSource Log;

        /// <summary>
        //  The Load method is called by BepInEx when the plugin is loaded.
        /// </summary>
        public override void Load()
        {
            Log = Logger;
            _harmony = new Harmony("YourName.CustomItemMod");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.LogInfo("Custom Item Mod loaded successfully!");
        }

        /// <summary>
        //  The Unload method is called by BepInEx when the plugin is unloaded.
        /// </summary>
        public override bool Unload()
        {
            _harmony?.UnpatchSelf();
            Log.LogInfo("Custom Item Mod unloaded successfully!");
            return true;
        }

        /// <summary>
        /// Loads an embedded image from the mod's DLL and converts it into a Sprite.
        /// </summary>
        /// <param name="resourcePath">The path to the embedded resource. Format: YourProject.Namespace.FolderName.ImageName.png</param>
        /// <returns>A Sprite created from the image, or null if it fails.</returns>
        public static Sprite LoadImageAsSprite(string resourcePath)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(resourcePath);
                if (stream == null)
                {
                    Log.LogError($"Cannot find embedded resource at path: {resourcePath}");
                    return null;
                }

                byte[] imageData = new byte[stream.Length];
                stream.Read(imageData, 0, (int)stream.Length);

                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageData))
                {
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
                else
                {
                    Log.LogError("Failed to load image data into texture.");
                    return null;
                }
            }
            catch (System.Exception e)
            {
                Log.LogError($"Error loading sprite from resource path '{resourcePath}': {e.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// This Harmony Patch targets the moment the game loads its data.
    /// We inject our code here to add our custom item to the game's data registries.
    /// Following V Rising guideline 7.6: HarmonyPostfix methods return void unless control flow needs altering
    /// </summary>
    [HarmonyPatch(typeof(GameDataManager), nameof(GameDataManager.OnCreate))]
    public static class GameDataManager_Patch
    {
        // Following V Rising guideline 7.6: HarmonyPostfix method returns void
        public static void Postfix(GameDataManager __instance)
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                var gameDataSystem = world.GetExistingSystem<GameDataSystem>();

                // --- MODIFICATION SECTION ---
                // Change these values to define your item.
                const string itemName = "The Legendary Picture Frame";
                const string itemDescription = "A frame holding a mysterious image from another world.";
                const int customItemID = -13371337; // Must be a unique NEGATIVE number.

                // This is the path to your embedded icon file.
                // It follows the format: [YourProjectNamespace].[FolderName].[FileName.extension]
                // If your namespace is "Items__.NET_Framework_" and your image is "MyIcon.png" in the root,
                // the path is "Items__.NET_Framework_.MyIcon.png".
                const string iconResourcePath = "Items__.NET_Framework_.MyPic.png"; // IMPORTANT: CHANGE THIS
                                                                                    // --------------------------

                // We will clone a Stone (-1491220300) to act as a base for our new item.
                var baseItemPrefabGuid = new PrefabGUID(-1491220300);
                var customItemPrefabGuid = new PrefabGUID(customItemID);

                // This is the core logic: clone the prefab and register it with our new ID.
                // This pattern is common in V Rising modding frameworks.
                if (gameDataSystem.ClonePrefab(baseItemPrefabGuid, customItemPrefabGuid))
                {
                    // Now, we modify the data associated with our newly cloned item.
                    var managedItemData = new ManagedItemData()
                    {
                        Name = itemName,
                        Description = itemDescription,
                        Icon = Plugin.LoadImageAsSprite(iconResourcePath)
                    };

                    // We must add our custom managed data to the central registry.
                    __instance.ManagedDataRegistry.Register(customItemPrefabGuid, managedItemData);

                    // Following V Rising guideline 7.4: Use centralized logging with context
                    Plugin.Log.LogInfo($"CustomItemMod - Successfully created and registered custom item: '{itemName}' (ID: {customItemID})");
                }
                else
                {
                    // Following V Rising guideline 7.4: Include relevant component data for debugging
                    Plugin.Log.LogError($"CustomItemMod - Failed to clone base item {baseItemPrefabGuid} for custom item creation.");
                }
            }
            catch (System.Exception e)
            {
                // Following V Rising guideline 7.4: Centralized logging with system context
                Plugin.Log.LogError($"CustomItemMod - Error occurred while creating custom item: {e.Message}");
                Plugin.Log.LogError($"CustomItemMod - Stack trace: {e.StackTrace}");
            }
        }
    }
}