using System;
using System.Collections.Generic;
using Unity.Mathematics;
using ProjectM;
using Stunlock.Core;

namespace CrowbaneArena.Models
{
    /// <summary>
    /// Represents a pre-configured character template that players will use during events.
    /// This ensures balanced gameplay and consistent experience for all participants.
    /// </summary>
    [Serializable]
    public class EventCharacterTemplate
    {
        public string TemplateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        // Character Appearance
        public int CharacterModel { get; set; }
        public byte[] CustomizationData { get; set; }

        // Fixed Stats for Balance
        public float MaxHealth { get; set; }
        public float MaxEnergy { get; set; }
        public int PowerLevel { get; set; }
        public float MovementSpeed { get; set; }

        // Fixed Inventory and Equipment
        public List<TemplateInventoryItem> StartingInventory { get; set; }
        public List<TemplateEquippedItem> StartingEquipment { get; set; }

        // Fixed Abilities
        public List<TemplateAbility> FixedAbilities { get; set; }

        // Event-Specific Properties
        public Dictionary<string, object> EventProperties { get; set; }

        // Creation and Modification Tracking
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string CreatedBy { get; set; }

        public EventCharacterTemplate()
        {
            IsActive = true;
            StartingInventory = new List<TemplateInventoryItem>();
            StartingEquipment = new List<TemplateEquippedItem>();
            FixedAbilities = new List<TemplateAbility>();
            EventProperties = new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        public EventCharacterTemplate(string templateId, string name) : this()
        {
            TemplateId = templateId;
            Name = name;
        }
    }

    [Serializable]
    public class TemplateInventoryItem
    {
        public PrefabGUID ItemGuid { get; set; }
        public int Quantity { get; set; }
        public int SlotIndex { get; set; }
        public string ItemName { get; set; } // For admin reference
        public Dictionary<string, object> ItemProperties { get; set; }

        public TemplateInventoryItem()
        {
            ItemProperties = new Dictionary<string, object>();
        }

        public TemplateInventoryItem(PrefabGUID itemGuid, int quantity, string itemName = "") : this()
        {
            ItemGuid = itemGuid;
            Quantity = quantity;
            ItemName = itemName;
        }
    }

    [Serializable]
    public class TemplateEquippedItem
    {
        public PrefabGUID ItemGuid { get; set; }
        public string SlotType { get; set; } // "MainHand", "OffHand", "Chest", "Legs", etc.
        public string ItemName { get; set; } // For admin reference
        public Dictionary<string, object> ItemProperties { get; set; }

        public TemplateEquippedItem()
        {
            ItemProperties = new Dictionary<string, object>();
        }

        public TemplateEquippedItem(PrefabGUID itemGuid, string slotType, string itemName = "") : this()
        {
            ItemGuid = itemGuid;
            SlotType = slotType;
            ItemName = itemName;
        }
    }

    [Serializable]
    public class TemplateAbility
    {
        public PrefabGUID AbilityGuid { get; set; }
        public string AbilityName { get; set; } // For admin reference
        public int Level { get; set; }
        public string SlotType { get; set; } // "Primary", "Secondary", "Ultimate", etc.
        public Dictionary<string, object> AbilityProperties { get; set; }

        public TemplateAbility()
        {
            Level = 1;
            AbilityProperties = new Dictionary<string, object>();
        }

        public TemplateAbility(PrefabGUID abilityGuid, string slotType, string abilityName = "") : this()
        {
            AbilityGuid = abilityGuid;
            SlotType = slotType;
            AbilityName = abilityName;
        }
    }

    /// <summary>
    /// Predefined character templates for common event types
    /// </summary>
    public static class DefaultEventTemplates
    {
        public static EventCharacterTemplate CreateWarriorTemplate()
        {
            var template = new EventCharacterTemplate("event_warrior", "Event Warrior")
            {
                Description = "A balanced melee fighter with sword and shield",
                MaxHealth = 1000f,
                MaxEnergy = 100f,
                PowerLevel = 50,
                MovementSpeed = 1.0f
            };

            // Add starting equipment (example GUIDs - replace with actual V Rising item GUIDs)
            template.StartingEquipment.Add(new TemplateEquippedItem(new PrefabGUID(-1569825471), "MainHand", "Iron Sword"));
            template.StartingEquipment.Add(new TemplateEquippedItem(new PrefabGUID(-1569825472), "OffHand", "Iron Shield"));
            template.StartingEquipment.Add(new TemplateEquippedItem(new PrefabGUID(-1569825473), "Chest", "Iron Chestplate"));

            // Add starting inventory
            template.StartingInventory.Add(new TemplateInventoryItem(new PrefabGUID(-1491220300), 5, "Health Potion"));
            template.StartingInventory.Add(new TemplateInventoryItem(new PrefabGUID(-1491220301), 3, "Energy Potion"));

            return template;
        }

        public static EventCharacterTemplate CreateMageTemplate()
        {
            var template = new EventCharacterTemplate("event_mage", "Event Mage")
            {
                Description = "A magical spellcaster with powerful abilities",
                MaxHealth = 600f,
                MaxEnergy = 200f,
                PowerLevel = 50,
                MovementSpeed = 0.9f
            };

            // Add starting equipment
            template.StartingEquipment.Add(new TemplateEquippedItem(new PrefabGUID(-1569825474), "MainHand", "Magic Staff"));
            template.StartingEquipment.Add(new TemplateEquippedItem(new PrefabGUID(-1569825475), "Chest", "Mage Robes"));

            // Add starting inventory
            template.StartingInventory.Add(new TemplateInventoryItem(new PrefabGUID(-1491220302), 3, "Mana Potion"));
            template.StartingInventory.Add(new TemplateInventoryItem(new PrefabGUID(-1491220303), 10, "Spell Components"));

            return template;
        }

        public static EventCharacterTemplate CreateArcherTemplate()
        {
            var template = new EventCharacterTemplate("event_archer", "Event Archer")
            {
                Description = "A ranged fighter with bow and arrows",
                MaxHealth = 800f,
                MaxEnergy = 120f,
                PowerLevel = 50,
                MovementSpeed = 1.1f
            };

            // Add starting equipment
            template.StartingEquipment.Add(new TemplateEquippedItem(new PrefabGUID(-1569825476), "MainHand", "Hunting Bow"));
            template.StartingEquipment.Add(new TemplateEquippedItem(new PrefabGUID(-1569825477), "Chest", "Leather Armor"));

            // Add starting inventory
            template.StartingInventory.Add(new TemplateInventoryItem(new PrefabGUID(-1491220304), 50, "Iron Arrows"));
            template.StartingInventory.Add(new TemplateInventoryItem(new PrefabGUID(-1491220305), 3, "Stamina Potion"));

            return template;
        }
    }
}