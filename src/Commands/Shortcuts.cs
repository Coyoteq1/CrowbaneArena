
using VampireCommandFramework;

namespace CrowbaneArena.Commands
{
    // Root-level short aliases that forward to existing command groups
    public static class Shortcuts
    {
        // Arena shortcuts
        // Arena shortcuts
        // Arena shortcuts
        [Command("en", description: "Enter arena (main) [kit]")]
        public static void En(ChatCommandContext ctx, string arenaName = "main", int kitNumber = -1)
            => new ArenaCommands().EnterArena(ctx, arenaName, kitNumber);

        [Command("le", description: "Leave arena")]
        public static void Le(ChatCommandContext ctx)
            => new ArenaCommands().LeaveArena(ctx);

        [Command("te", description: "Teleport to arena")]
        public static void Te(ChatCommandContext ctx, string arenaName = "main")
            => new ArenaCommands().TeleportToArena(ctx, arenaName);

        // Crowbane shortcuts
        [Command("he", description: "Heal (arena only)")]
        public static void He(ChatCommandContext ctx)
            => new CrowbaneCommands().Heal(ctx);

        [Command("ki", description: "Kit (arena only)")]
        public static void Ki(ChatCommandContext ctx, int kitNumber)
            => new CrowbaneCommands().Kit(ctx, kitNumber);

        [Command("gi", description: "Give item (arena only)")]
        public static void Gi(ChatCommandContext ctx, int prefabId, int quantity = 1)
            => new CrowbaneCommands().Give(ctx, prefabId, quantity);

        [Command("bl", description: "Blood Merlot (arena only)")]
        public static void Bl(ChatCommandContext ctx, string typeOrId, float quality = 100f, int quantity = 1)
            => new CrowbaneCommands().BloodPotion(ctx, typeOrId, quality, quantity);

        [Command("lo", description: "Loadout (arena only)")]
        public static void Lo(ChatCommandContext ctx, string name)
            => new CrowbaneCommands().Loadout(ctx, name);
    }
}

// ////coyote////
