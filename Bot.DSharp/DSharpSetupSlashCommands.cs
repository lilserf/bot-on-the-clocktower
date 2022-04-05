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

            await BotSetup!.CommandCreateTown(new DSharpInteractionContext(ctx), townName, wrappedPlayerRole, wrappedStRole, useNight);
        }
    }
}
