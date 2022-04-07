using Bot.Api;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpSetupSlashCommands : ApplicationCommandModule
    {
        public IBotSetup? BotSetup { get; set; }

        [SlashCommand("createTown", "Create a new Town on this server")]
        public async Task CreateTownCommand(InteractionContext ctx,
            [Option("townName", "Town Name")] string townName,
            [Option("playerRole", "Server Player Role - only they can see the town")] DiscordRole? playerRole = null,
            [Option("storytellerRole", "Server Storyteller Role - only they can see control channels")] DiscordRole? stRole = null,
            [Option("useNight", "If true, a Night category full of cottages will be created")] bool useNight = true)
        {
            var wrappedPlayerRole = playerRole == null ? null : new DSharpRole(playerRole);
            var wrappedStRole = stRole == null ? null : new DSharpRole(stRole);

            await BotSetup!.CreateTownAsync(new DSharpInteractionContext(ctx), townName, wrappedPlayerRole, wrappedStRole, useNight);
        }

        [SlashCommand("townInfo", "Get info about any town registered for this server & channel")]
        public async Task TownInfoCommand(InteractionContext ctx)
        {
            await BotSetup!.TownInfoAsync(new DSharpInteractionContext(ctx));
        }

        [SlashCommand("destroyTown", "Destroy any channels and roles created by /createtown for the town with the given name")]
        public async Task DestroyTownCommand(InteractionContext ctx,
            [Option("townName", "Town Name")] string townName)
        {
            await BotSetup!.DestroyTownAsync(new DSharpInteractionContext(ctx), townName);
        }

        [SlashCommand("removeTown", "Unregister a town on this server without deleting any channels or roles")]
        public async Task RemoveTownCommand(InteractionContext ctx,
            [Option("townName", "Town Name - if left blank, will delete the town associated with the channel issuing the command.")] string? townName = null)
        {
            await BotSetup!.RemoveTownAsync(new DSharpInteractionContext(ctx), townName);
        }
    }
}
