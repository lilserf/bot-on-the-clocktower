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


        //   Usage: {COMMAND_PREFIX}addTown <control channel> <town square channel> <day category> <night category> <storyteller role> <villager role> [chat channel]\n\nAlternate usage: {COMMAND_PREFIX}addTown control=<control channel> townSquare=<town square channel> dayCategory=<day category> [nightCategory=<night category>] stRole=<storyteller role> villagerRole=<villager role> [chatChannel=<chat channel>]')
        [SlashCommand("addTown", "Add a new town composed of existing channel and roles on this server")]
        public async Task AddTownCommand(InteractionContext ctx,
            [Option("controlChannel", "Control channel (must be text)")] DiscordChannel controlChannel,
            [Option("townSquare", "Town Square channel (must be voice)")] DiscordChannel townSquare,
            [Option("dayCategory", "Day Category (must contain control and town square channels)")] DiscordChannel dayCategory,
            [Option("nightCategory", "Night Category (optional)")] DiscordChannel? nightCategory = null,
            [Option("storytellerRole", "Storyteller role (optional)")] DiscordRole? stRole = null,
            [Option("villagerRole", "Villager role (optional)")] DiscordRole? villagerRole = null,
            [Option("chatChannel", "Chat channel (optional, must be text)")] DiscordChannel? chatChannel = null
            )
        {
            List<string> errors = new();

            if (controlChannel.Type != DSharpPlus.ChannelType.Text)
            {
                errors.Add($"Control channel `{controlChannel.Name}` is not a text channel!");
            }
            if (dayCategory.Type != DSharpPlus.ChannelType.Category)
            {
                errors.Add($"Day category `{dayCategory.Name}` is not a category!");
            }
            if (townSquare.Type != DSharpPlus.ChannelType.Voice)
            {
                errors.Add($"Town Square channel `{townSquare.Name}` is not a voice channel!");
            }
            if (!dayCategory.Children.Contains(controlChannel))
            {
                errors.Add($"Control channel `{controlChannel.Name}` is not in the {dayCategory.Name} category!");
            }
            if (!dayCategory.Children.Contains(townSquare))
            {
                errors.Add($"Town Square channel `{townSquare.Name}` is not in the {dayCategory.Name} category!");
            }
            if(nightCategory != null && nightCategory.Type != DSharpPlus.ChannelType.Category)
            {
                errors.Add($"Night category `{nightCategory.Name}` is not a category!");
            }
            if(chatChannel != null && !dayCategory.Children.Contains(chatChannel))
            {
                errors.Add($"Chat channel `{chatChannel.Name}` is not in the {dayCategory.Name} category!");
            }

            if(errors.Count > 0)
            {
                string msg = "Sorry, I found errors:\n" + string.Join("\n", errors);
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(msg));
                return;
            }

            var controlChan = new DSharpChannel(controlChannel);
            var townChan = new DSharpChannel(townSquare);
            var dayCat = new DSharpChannelCategory(dayCategory);

            var nightCat = nightCategory == null ? null : new DSharpChannelCategory(nightCategory);
            var stRoleWrapped = stRole == null ? null : new DSharpRole(stRole);
            var villagerRoleWrapped = villagerRole == null ? null : new DSharpRole(villagerRole);
            var chatChan = chatChannel == null ? null : new DSharpChannel(chatChannel);

            await BotSetup!.AddTownAsync(new DSharpInteractionContext(ctx), controlChan, townChan, dayCat, nightCat, stRoleWrapped, villagerRoleWrapped, chatChan);
        }

        [SlashCommand("removeTown", "Unregister a town on this server without deleting any channels or roles")]
        public async Task RemoveTownCommand(InteractionContext ctx,
            [Option("dayCategory", "Town to remove - if blank, must be run from the town's control channel")] DiscordChannel? dayCat = null)
        {
            if (dayCat != null && dayCat.Type != DSharpPlus.ChannelType.Category)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"**Error:** Channel `{dayCat.Name}` isn't a category!"));
            }

            DSharpChannelCategory? dayCatWrapped = dayCat == null ? null : new DSharpChannelCategory(dayCat);

            await BotSetup!.RemoveTownAsync(new DSharpInteractionContext(ctx), dayCatWrapped);
        }
    }
}
