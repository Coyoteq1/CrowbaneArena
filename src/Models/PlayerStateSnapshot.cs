using System;
using System.Collections.Generic;
using Unity.Mathematics;
using ProjectM;
using Stunlock.Core;

namespace CrowbaneArena.Models
{
    /// <summary>
    /// Represents a complete snapshot of a player's character state before entering an event.
    /// This includes all character data needed to restore the player to their original state.
    /// </summary>
    [Serializable]
    public class PlayerStateSnapshot
    {
        public ulong PlayerId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Character Appearance
        public string CharacterName { get; set; }
        public int CharacterModel { get; set; }
        public byte[] CustomizationData { get; set; }

        // Character Stats
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Energy { get; set; }
        public float MaxEnergy { get; set; }
        public int PowerLevel { get; set; }

        // Position and Location
        public float3 Position { get; set; }
        public float3 Rotation { get; set; }
        public string MapZone { get; set; }

        // Inventory Data
        public List<InventoryItem> Inventory { get; set; }
        public List<EquippedItem> EquippedItems { get; set; }

        // Abilities and Skills
        public List<AbilityData> Abilities { get; set; }
        public List<SkillData> Skills { get; set; }

        // Progression Data
        public int Experience { get; set; }
        public int Level { get; set; }
        public Dictionary<string, object> ProgressionData { get; set; }

        // Additional Game State
        public Dictionary<string, object> AdditionalData { get; set; }

        public PlayerStateSnapshot()
        {
            CreatedAt = DateTime.UtcNow;
            Inventory = new List<InventoryItem>();
            EquippedItems = new List<EquippedItem>();
            Abilities = new List<AbilityData>();
            Skills = new List<SkillData>();
            ProgressionData = new Dictionary<string, object>();
            AdditionalData = new Dictionary<string, object>();
        }
    }

    [Serializable]
    public class InventoryItem
    {
        public PrefabGUID ItemGuid { get; set; }
        public int Quantity { get; set; }
        public int SlotIndex { get; set; }
        public Dictionary<string, object> ItemData { get; set; }

        public InventoryItem()
        {
            ItemData = new Dictionary<string, object>();
        }
    }

    [Serializable]
    public class EquippedItem
    {
        public PrefabGUID ItemGuid { get; set; }
        public string SlotType { get; set; } // "Weapon", "Armor", "Accessory", etc.
        public Dictionary<string, object> ItemData { get; set; }

        public EquippedItem()
        {
            ItemData = new Dictionary<string, object>();
        }
    }

    [Serializable]
    public class AbilityData
    {
        public PrefabGUID AbilityGuid { get; set; }
        public int Level { get; set; }
        public float Cooldown { get; set; }
        public Dictionary<string, object> AbilityProperties { get; set; }

        public AbilityData()
        {
            AbilityProperties = new Dictionary<string, object>();
        }
    }

    [Serializable]
    public class SkillData
    {
        public string SkillType { get; set; }
        public int Level { get; set; }
        public float Experience { get; set; }
        public Dictionary<string, object> SkillProperties { get; set; }

        public SkillData()
        {
            SkillProperties = new Dictionary<string, object>();
        }
    }
}