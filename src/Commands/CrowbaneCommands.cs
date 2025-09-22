using System;
using System.IO;
using System.Text.Json;
using VampireCommandFramework;
using Unity.Mathematics;
using Unity.Entities;
using Stunlock.Core;

namespace CrowbaneArena.Commands
{
    [CommandGroup("cb")]
    public class CrowbaneCommands
    {
        private static bool EnsureInArena(ChatCommandContext ctx)
        {
            var character = ctx.Event.SenderCharacterEntity;
            // var session = CrowbanePackPlugin.EndArenaSession(character); 
            if (session != null)
            {
                
                CrowbanePackPlugin.StartArenaSession(character, session);
                return true;
            }

            ctx.Reply("This command only works inside the arena. Use .arena enter first.");
            return false;
        }

        [Command("heal", "h", description: "Instant heal (arena only)")]
        public void Heal(ChatCommandContext ctx)
        {
            if (!EnsureInArena(ctx)) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            // TODO: Implement ReviveCharacter logic
            ctx.Reply("Healed (or revived if needed).");
        }

        [Command("kit", "k", description: "Apply a kit by number (arena only)")]
        public void Kit(ChatCommandContext ctx, int kitNumber)
        {
            if (!EnsureInArena(ctx)) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            // TODO: Implement ApplyKit logic
            ctx.Reply($"Applied kit {kitNumber}");
        }

        [Command("bindhelp", "bh", description: "Show how to bind F4 to heal (arena only)")]
        public void BindHelp(ChatCommandContext ctx)
        {
            if (!EnsureInArena(ctx)) return;
            ctx.Reply("Open console and run: bind F4 .cb heal");
        }

        [Command("spells all", "sa", description: "Unlock all spells while in arena")]
        public void SpellsAll(ChatCommandContext ctx)
        {
            if (!EnsureInArena(ctx)) return;
            ctx.Reply("Spells unlock is not wired for this server build; will remain placeholder unless game API is available.");
        }

        [Command("blood", "b", description: "Set 100% blood of a type (arena only)")]
        public void Blood(ChatCommandContext ctx, string type)
        {
            if (!EnsureInArena(ctx)) return;
            ctx.Reply($"Requested 100% blood: {type}. This will be enabled when the blood API is identified for this server build.");
        }

        

        [Command("give", "g", description: "Give an item by prefab id (arena only)")]
        public void Give(ChatCommandContext ctx, int prefabId, int quantity = 1)
        {
            if (!EnsureInArena(ctx)) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            // TODO: Implement AddItemAndGetEntity logic
            var ent = Entity.Null;
            if (ent == Entity.Null) ctx.Reply("Inventory full or item not found.");
            else ctx.Reply($"Gave item {prefabId} x{quantity}.");
        }

        [Command("bloodpotion", "bp", description: "Give Blood Merlot by type name or id (arena only)")]
        public void BloodPotion(ChatCommandContext ctx, string typeOrId, float quality = 100f, int quantity = 1)
        {
            if (!EnsureInArena(ctx)) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            quality = math.clamp(quality, 0, 100);
            int given = 0;
            int bloodTypePrefabId;
            if (!int.TryParse(typeOrId, out bloodTypePrefabId))
            {
                if (!CrowbanePackPlugin.BloodTypes.TryGetValue(typeOrId, out bloodTypePrefabId) || bloodTypePrefabId <= 0)
                {
                    ctx.Reply($"Unknown blood type '{typeOrId}'. Update {CrowbanePackPlugin.BloodTypesPath} or use an ID.");
                    return;
                }
            }
            for (int i = 0; i < quantity; i++)
            {
                var entity = PluginHelpers.AddItemAndGetEntity(em, ctx.Event.SenderCharacterEntity, new PrefabGUID(1223264867), 1);
                if (entity == Entity.Null)
                {
                    if (i == 0) ctx.Reply("Inventory full.");
                    break;
                }
                given++;
                var blood = new StoredBlood
                {
                    BloodQuality = quality,
                    PrimaryBloodType = new PrefabGUID(bloodTypePrefabId)
                };
                em.SetComponentData(entity, blood);
            }
            ctx.Reply($"Gave {given} Blood Merlot of type {bloodTypePrefabId} at {quality}%.");
        }

        [Command("revive", "r", description: "Revive/Heal yourself (arena only)")]
        public void Revive(ChatCommandContext ctx)
        {
            if (!EnsureInArena(ctx)) return;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            PluginHelpers.ReviveCharacter(em, ctx.Event.SenderUserEntity, ctx.Event.SenderCharacterEntity);
            ctx.Reply("Revived.");
        }

        [Command("blood list", "bl", description: "List configured blood type names (from blood_types.json)")]
        public void BloodList(ChatCommandContext ctx)
        {
            if (!EnsureInArena(ctx)) return;
            foreach (var kv in CrowbanePackPlugin.BloodTypes)
            {
                ctx.Reply($"{kv.Key} -> {kv.Value}");
            }
        }

        [Command("blood reload", "br", description: "Reload blood types map", adminOnly: true)]
        public void BloodReload(ChatCommandContext ctx)
        {
            CrowbanePackPlugin.LoadBloodTypes();
            ctx.Reply("Blood types reloaded.");
        }

        [Command("blood defaults", "bd", description: "Write default blood type map (overwrites)", adminOnly: true)]
        public void BloodDefaults(ChatCommandContext ctx)
        {
            var map = new System.Collections.Generic.Dictionary<string,int>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["frailed"] = 447918373,
                ["creature"] = 524822543,
                ["warrior"] = -516976528,
                ["rogue"] = -1620185637,
                ["brute"] = 804798592,
                ["scholar"] = 1476452791,
                ["worker"] = -1776904174
            };
            // System.IO.File.WriteAllText(CrowbanePackPlugin.BloodTypesPath, System.Text.Json.JsonSerializer.Serialize(map, new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
            // CrowbanePackPlugin.LoadBloodTypes();
            ctx.Reply("Default blood types written and reloaded.");
        }

        [Command("loadout", "lo", description: "Apply a named loadout (arena only)")]
        public void Loadout(ChatCommandContext ctx, string name)
        {
            if (!EnsureInArena(ctx)) return;
            if (!CrowbanePackPlugin.Loadouts.TryGetValue(name, out var loadout))
            {
                ctx.Reply($"Loadout '{name}' not found. Edit {CrowbanePackPlugin.LoadoutsPath}.");
                return;
            }

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var character = ctx.Event.SenderCharacterEntity;

            if (loadout.Kit.HasValue)
            {
                PluginHelpers.ApplyKit(em, character, loadout.Kit.Value, silent: false);
            }

            if (loadout.Items != null)
            {
                foreach (var it in loadout.Items)
                {
                    PluginHelpers.AddItemAndGetEntity(em, character, new PrefabGUID(it.PrefabId), it.Amount);
                }
            }

            if (!string.IsNullOrEmpty(loadout.BloodType))
            {
                var typeKey = loadout.BloodType;
                if (CrowbanePackPlugin.BloodTypes.TryGetValue(typeKey, out var id) && id > 0)
                {
                    int qty = loadout.BloodAmount ?? 1;
                    float qual = loadout.BloodQuality ?? 100f;
                    BloodPotion(ctx, id.ToString(), qual, qty);
                }
                else
                {
                    ctx.Reply($"Blood type '{typeKey}' not found in map. Edit {CrowbanePackPlugin.BloodTypesPath}.");
                }
            }

            ctx.Reply($"Loadout '{name}' applied.");
        }

        [Command("loadout list", "ll", description: "List available loadouts")]
        public void LoadoutList(ChatCommandContext ctx)
        {
            if (!EnsureInArena(ctx)) return;
            if (CrowbanePackPlugin.Loadouts.Count == 0)
            {
                ctx.Reply("No loadouts defined. Edit " + CrowbanePackPlugin.LoadoutsPath);
                return;
            }
            ctx.Reply("Loadouts:");
            foreach (var key in CrowbanePackPlugin.Loadouts.Keys)
            {
                ctx.Reply("- " + key);
            }
        }

        [Command("loadout reload", "lr", description: "Reload loadouts from disk", adminOnly: true)]
        public void LoadoutReload(ChatCommandContext ctx)
        {
            CrowbanePackPlugin.LoadLoadouts();
            ctx.Reply("Loadouts reloaded.");
        }
    }
}

// ////coyote////
