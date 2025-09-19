using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;
using Stunlock.Core;
using Unity.Mathematics;

namespace CrowbaneArena.Models
{
    /// <summary>
    /// Represents a quest within the event system
    /// </summary>
    [Serializable]
    public class Quest
    {
        public string QuestId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public QuestState State { get; set; }
        public List<QuestObjective> Objectives { get; set; }
        public List<QuestReward> Rewards { get; set; }
        public string NextQuestId { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public Quest()
        {
            State = QuestState.NotStarted;
            Objectives = new List<QuestObjective>();
            Rewards = new List<QuestReward>();
            Properties = new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
        }

        public Quest(string questId, string name, string description) : this()
        {
            QuestId = questId;
            Name = name;
            Description = description;
        }

        public bool IsCompleted => Objectives.All(obj => obj.IsCompleted);
        public float CompletionPercentage => Objectives.Count > 0 ? (float)Objectives.Count(obj => obj.IsCompleted) / Objectives.Count * 100f : 0f;
    }

    [Serializable]
    public enum QuestState
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        Abandoned
    }

    /// <summary>
    /// Represents an individual objective within a quest
    /// </summary>
    [Serializable]
    public class QuestObjective
    {
        public string ObjectiveId { get; set; }
        public string Description { get; set; }
        public QuestObjectiveType Type { get; set; }
        public string TargetId { get; set; }
        public int RequiredAmount { get; set; }
        public int CurrentAmount { get; set; }
        public bool IsCompleted => CurrentAmount >= RequiredAmount;
        public Dictionary<string, object> Properties { get; set; }

        public QuestObjective()
        {
            Properties = new Dictionary<string, object>();
        }

        public QuestObjective(string objectiveId, string description, QuestObjectiveType type, string targetId, int requiredAmount) : this()
        {
            ObjectiveId = objectiveId;
            Description = description;
            Type = type;
            TargetId = targetId;
            RequiredAmount = requiredAmount;
            CurrentAmount = 0;
        }
    }

    [Serializable]
    public enum QuestObjectiveType
    {
        Kill,
        Collect,
        Interact,
        Reach,
        Survive,
        Escort,
        Defend,
        Custom
    }

    /// <summary>
    /// Represents a reward given upon quest completion
    /// </summary>
    [Serializable]
    public class QuestReward
    {
        public QuestRewardType Type { get; set; }
        public PrefabGUID ItemGuid { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public QuestReward()
        {
            Properties = new Dictionary<string, object>();
        }

        public QuestReward(QuestRewardType type, int amount, string description) : this()
        {
            Type = type;
            Amount = amount;
            Description = description;
        }
    }

    [Serializable]
    public enum QuestRewardType
    {
        Item,
        Experience,
        Currency,
        Unlock,
        Custom
    }

    /// <summary>
    /// Manages quest templates and creation
    /// </summary>
    public static class QuestTemplates
    {
        public static Quest CreateCollectGemsQuest()
        {
            var quest = new Quest("quest_01_collect_gems", "Gather Power Gems", "Collect 3 Power Gems scattered throughout the arena");
            quest.Objectives.Add(new QuestObjective("collect_gems", "Collect Power Gems", QuestObjectiveType.Collect, "power_gem", 3));
            quest.Rewards.Add(new QuestReward(QuestRewardType.Item, 1, "Magic Sword"));
            quest.NextQuestId = "quest_02_activate_levers";
            return quest;
        }

        public static Quest CreateActivateLeversQuest()
        {
            var quest = new Quest("quest_02_activate_levers", "Activate the Switches", "Find and activate the three lever switches to unlock the boss chamber");
            quest.Objectives.Add(new QuestObjective("activate_lever_1", "Activate First Lever", QuestObjectiveType.Interact, "lever_switch_1", 1));
            quest.Objectives.Add(new QuestObjective("activate_lever_2", "Activate Second Lever", QuestObjectiveType.Interact, "lever_switch_2", 1));
            quest.Objectives.Add(new QuestObjective("activate_lever_3", "Activate Third Lever", QuestObjectiveType.Interact, "lever_switch_3", 1));
            quest.NextQuestId = "quest_03_defeat_boss";
            return quest;
        }

        public static Quest CreateDefeatBossQuest()
        {
            var quest = new Quest("quest_03_defeat_boss", "Defeat the Guardian", "Defeat the powerful Guardian that protects the arena");
            quest.Objectives.Add(new QuestObjective("defeat_guardian", "Defeat the Guardian", QuestObjectiveType.Kill, "boss_guardian", 1));
            quest.Rewards.Add(new QuestReward(QuestRewardType.Item, 1, "Guardian's Crown"));
            quest.Rewards.Add(new QuestReward(QuestRewardType.Experience, 1000, "1000 Experience Points"));
            return quest;
        }

        public static Quest CreateSurvivalQuest()
        {
            var quest = new Quest("quest_survival", "Survive the Onslaught", "Survive waves of enemies for 5 minutes");
            quest.Objectives.Add(new QuestObjective("survive_time", "Survive for 5 minutes", QuestObjectiveType.Survive, "survival_timer", 300));
            return quest;
        }

        public static Quest CreateEscortQuest()
        {
            var quest = new Quest("quest_escort", "Escort the Merchant", "Safely escort the merchant to the exit");
            quest.Objectives.Add(new QuestObjective("escort_npc", "Escort Merchant", QuestObjectiveType.Escort, "merchant_npc", 1));
            return quest;
        }
    }
}