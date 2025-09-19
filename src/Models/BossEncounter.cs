using System;
using System.Collections.Generic;
using Unity.Mathematics;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace CrowbaneArena.Models
{
    /// <summary>
    /// Represents a boss encounter within the event system
    /// Following V Rising's ECS/GameObject hybrid approach
    /// </summary>
    [Serializable]
    public class BossEncounter
    {
        public string BossId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public PrefabGUID BossPrefabGuid { get; set; }
        public BossState State { get; set; }

        // Spawn Configuration
        public float3 SpawnPosition { get; set; }
        public float3 SpawnRotation { get; set; }
        public string SpawnZone { get; set; }

        // Boss Attributes
        public BossAttributes Attributes { get; set; }
        public List<BossPhase> Phases { get; set; }
        public List<BossAbility> Abilities { get; set; }

        // Loot and Rewards
        public List<BossLootItem> LootTable { get; set; }
        public List<QuestReward> CompletionRewards { get; set; }

        // Event Integration
        public List<string> SpawnConditions { get; set; }
        public List<string> DefeatTriggers { get; set; }

        // Runtime Data
        public Entity? SpawnedEntity { get; set; }
        public DateTime? SpawnedAt { get; set; }
        public DateTime? DefeatedAt { get; set; }
        public List<ulong> ParticipatingPlayers { get; set; }

        // Additional Properties
        public Dictionary<string, object> Properties { get; set; }

        public BossEncounter()
        {
            State = BossState.NotSpawned;
            Attributes = new BossAttributes();
            Phases = new List<BossPhase>();
            Abilities = new List<BossAbility>();
            LootTable = new List<BossLootItem>();
            CompletionRewards = new List<QuestReward>();
            SpawnConditions = new List<string>();
            DefeatTriggers = new List<string>();
            ParticipatingPlayers = new List<ulong>();
            Properties = new Dictionary<string, object>();
        }

        public BossEncounter(string bossId, string name, PrefabGUID bossPrefabGuid) : this()
        {
            BossId = bossId;
            Name = name;
            BossPrefabGuid = bossPrefabGuid;
        }

        public bool IsActive => State == BossState.Spawned || State == BossState.InCombat;
        public bool IsDefeated => State == BossState.Defeated;
        public TimeSpan? CombatDuration => SpawnedAt.HasValue && DefeatedAt.HasValue ?
            DefeatedAt.Value - SpawnedAt.Value : null;
    }

    [Serializable]
    public enum BossState
    {
        NotSpawned,
        Spawning,
        Spawned,
        InCombat,
        Defeated,
        Despawned,
        Error
    }

    /// <summary>
    /// Boss attributes for scaling and balance
    /// </summary>
    [Serializable]
    public class BossAttributes
    {
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }
        public float PhysicalResistance { get; set; }
        public float MagicalResistance { get; set; }
        public float MovementSpeed { get; set; }
        public float AttackDamage { get; set; }
        public float AttackSpeed { get; set; }
        public float CriticalChance { get; set; }
        public float CriticalMultiplier { get; set; }
        public int Level { get; set; }
        public Dictionary<string, float> CustomAttributes { get; set; }

        public BossAttributes()
        {
            MaxHealth = 10000f;
            CurrentHealth = 10000f;
            PhysicalResistance = 0.2f;
            MagicalResistance = 0.2f;
            MovementSpeed = 1.0f;
            AttackDamage = 500f;
            AttackSpeed = 1.0f;
            CriticalChance = 0.1f;
            CriticalMultiplier = 2.0f;
            Level = 50;
            CustomAttributes = new Dictionary<string, float>();
        }

        public float HealthPercentage => MaxHealth > 0 ? (CurrentHealth / MaxHealth) * 100f : 0f;
    }

    /// <summary>
    /// Represents different phases of a boss fight
    /// </summary>
    [Serializable]
    public class BossPhase
    {
        public string PhaseId { get; set; }
        public string Name { get; set; }
        public float HealthThreshold { get; set; } // Percentage of health when this phase activates
        public List<string> EnabledAbilities { get; set; }
        public List<string> DisabledAbilities { get; set; }
        public BossAttributes AttributeModifiers { get; set; }
        public List<string> SpawnMinions { get; set; }
        public string PhaseMessage { get; set; }
        public Dictionary<string, object> PhaseProperties { get; set; }

        public BossPhase()
        {
            EnabledAbilities = new List<string>();
            DisabledAbilities = new List<string>();
            AttributeModifiers = new BossAttributes();
            SpawnMinions = new List<string>();
            PhaseProperties = new Dictionary<string, object>();
        }

        public BossPhase(string phaseId, string name, float healthThreshold) : this()
        {
            PhaseId = phaseId;
            Name = name;
            HealthThreshold = healthThreshold;
        }
    }

    /// <summary>
    /// Boss abilities and special attacks
    /// </summary>
    [Serializable]
    public class BossAbility
    {
        public string AbilityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public BossAbilityType Type { get; set; }
        public float Cooldown { get; set; }
        public float CastTime { get; set; }
        public float Range { get; set; }
        public float Damage { get; set; }
        public List<string> Effects { get; set; }
        public Dictionary<string, object> AbilityProperties { get; set; }

        public BossAbility()
        {
            Effects = new List<string>();
            AbilityProperties = new Dictionary<string, object>();
        }

        public BossAbility(string abilityId, string name, BossAbilityType type) : this()
        {
            AbilityId = abilityId;
            Name = name;
            Type = type;
        }
    }

    [Serializable]
    public enum BossAbilityType
    {
        MeleeAttack,
        RangedAttack,
        AreaOfEffect,
        Buff,
        Debuff,
        Summon,
        Teleport,
        Shield,
        Heal,
        Custom
    }

    /// <summary>
    /// Boss loot configuration
    /// </summary>
    [Serializable]
    public class BossLootItem
    {
        public PrefabGUID ItemGuid { get; set; }
        public string ItemName { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public float DropChance { get; set; } // 0.0 to 1.0
        public bool IsGuaranteed { get; set; }
        public Dictionary<string, object> ItemProperties { get; set; }

        public BossLootItem()
        {
            MinQuantity = 1;
            MaxQuantity = 1;
            DropChance = 1.0f;
            IsGuaranteed = false;
            ItemProperties = new Dictionary<string, object>();
        }

        public BossLootItem(PrefabGUID itemGuid, string itemName, float dropChance) : this()
        {
            ItemGuid = itemGuid;
            ItemName = itemName;
            DropChance = dropChance;
        }
    }

    /// <summary>
    /// Predefined boss templates for common encounter types
    /// </summary>
    public static class BossTemplates
    {
        public static BossEncounter CreateGuardianBoss()
        {
            var boss = new BossEncounter("boss_guardian", "The Ancient Guardian", new PrefabGUID(-1234567890))
            {
                Description = "A powerful ancient guardian that protects the arena's secrets",
                SpawnPosition = new float3(0, 0, 0),
                SpawnRotation = new float3(0, 0, 0),
                SpawnZone = "boss_arena"
            };

            // Configure attributes
            boss.Attributes.MaxHealth = 15000f;
            boss.Attributes.CurrentHealth = 15000f;
            boss.Attributes.AttackDamage = 750f;
            boss.Attributes.PhysicalResistance = 0.3f;
            boss.Attributes.MagicalResistance = 0.2f;
            boss.Attributes.Level = 60;

            // Add phases
            boss.Phases.Add(new BossPhase("phase_1", "Awakening", 100f) { PhaseMessage = "The Guardian awakens!" });
            boss.Phases.Add(new BossPhase("phase_2", "Enraged", 50f) { PhaseMessage = "The Guardian becomes enraged!" });
            boss.Phases.Add(new BossPhase("phase_3", "Desperate", 25f) { PhaseMessage = "The Guardian fights desperately!" });

            // Add abilities
            boss.Abilities.Add(new BossAbility("slam_attack", "Ground Slam", BossAbilityType.AreaOfEffect)
            {
                Cooldown = 8f,
                CastTime = 2f,
                Range = 10f,
                Damage = 1000f
            });

            boss.Abilities.Add(new BossAbility("charge_attack", "Devastating Charge", BossAbilityType.MeleeAttack)
            {
                Cooldown = 12f,
                CastTime = 1f,
                Range = 15f,
                Damage = 1200f
            });

            // Add loot
            boss.LootTable.Add(new BossLootItem(new PrefabGUID(-1111111111), "Guardian's Crown", 1.0f) { IsGuaranteed = true });
            boss.LootTable.Add(new BossLootItem(new PrefabGUID(-2222222222), "Ancient Gem", 0.5f));
            boss.LootTable.Add(new BossLootItem(new PrefabGUID(-3333333333), "Guardian's Essence", 0.3f));

            // Set spawn conditions
            boss.SpawnConditions.Add("all_levers_activated");
            boss.SpawnConditions.Add("quest_02_completed");

            // Set defeat triggers
            boss.DefeatTriggers.Add("spawn_exit_portal");
            boss.DefeatTriggers.Add("complete_event");

            return boss;
        }

        public static BossEncounter CreateDragonBoss()
        {
            var boss = new BossEncounter("boss_dragon", "Crimson Wyrm", new PrefabGUID(-1234567891))
            {
                Description = "A fearsome dragon that rules the skies above the arena",
                SpawnPosition = new float3(0, 20, 0),
                SpawnRotation = new float3(0, 0, 0),
                SpawnZone = "dragon_lair"
            };

            boss.Attributes.MaxHealth = 25000f;
            boss.Attributes.CurrentHealth = 25000f;
            boss.Attributes.AttackDamage = 1000f;
            boss.Attributes.MagicalResistance = 0.4f;
            boss.Attributes.Level = 75;

            // Flying boss with fire abilities
            boss.Abilities.Add(new BossAbility("fire_breath", "Dragon's Breath", BossAbilityType.RangedAttack)
            {
                Cooldown = 6f,
                CastTime = 3f,
                Range = 25f,
                Damage = 800f
            });

            boss.Abilities.Add(new BossAbility("meteor_strike", "Meteor Strike", BossAbilityType.AreaOfEffect)
            {
                Cooldown = 15f,
                CastTime = 4f,
                Range = 30f,
                Damage = 1500f
            });

            return boss;
        }

        public static BossEncounter CreateNecromancerBoss()
        {
            var boss = new BossEncounter("boss_necromancer", "Dark Archlich", new PrefabGUID(-1234567892))
            {
                Description = "A powerful necromancer who commands the undead",
                SpawnPosition = new float3(0, 0, 0),
                SpawnRotation = new float3(0, 0, 0),
                SpawnZone = "necromancer_chamber"
            };

            boss.Attributes.MaxHealth = 12000f;
            boss.Attributes.CurrentHealth = 12000f;
            boss.Attributes.AttackDamage = 600f;
            boss.Attributes.MagicalResistance = 0.5f;
            boss.Attributes.PhysicalResistance = 0.1f;
            boss.Attributes.Level = 65;

            // Summoning and dark magic abilities
            boss.Abilities.Add(new BossAbility("summon_skeletons", "Raise Undead", BossAbilityType.Summon)
            {
                Cooldown = 20f,
                CastTime = 3f,
                Range = 0f,
                Damage = 0f
            });

            boss.Abilities.Add(new BossAbility("dark_bolt", "Shadow Bolt", BossAbilityType.RangedAttack)
            {
                Cooldown = 3f,
                CastTime = 1f,
                Range = 20f,
                Damage = 700f
            });

            boss.Abilities.Add(new BossAbility("life_drain", "Life Drain", BossAbilityType.Debuff)
            {
                Cooldown = 10f,
                CastTime = 2f,
                Range = 15f,
                Damage = 400f
            });

            return boss;
        }
    }
}