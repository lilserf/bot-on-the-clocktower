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

        [SlashCommand("modifyTown", "Modify one of the optional details of a town")]
        public async Task ModifyTownCommand(InteractionContext ctx,
            [Option("chatChannel", "Set the (text) chat channel for this town")] DiscordChannel? chatChannel = null,
            [Option("nightCategory", "Set the Night category for this town")] DiscordChannel? nightCat = null,
            [Option("storytellerRole", "Set the storyteller role for this town")] DiscordRole? stRole = null,
            [Option("villagerRole", "Set the villager role for this town")] DiscordRole? villagerRole = null)
        {
            // Need to do error handling here at the argument level before we try to create wrappers for stuff that's
            // totally the wrong type
            List<string> errors = new();

            if(chatChannel != null && chatChannel.Type != DSharpPlus.ChannelType.Text)
            {
                errors.Add($"- Chat channel `{chatChannel.Name}` is not a text channel!");
            }
            if(nightCat != null && nightCat.Type != DSharpPlus.ChannelType.Category)
            {
                errors.Add($"- Night category `{nightCat.Name}` is not a category!");
            }

            var chatChanWrapped = chatChannel == null ? null : new DSharpChannel(chatChannel);
            var nightCatWrapped = nightCat == null ? null : new DSharpChannelCategory(nightCat);
            var stRoleWrapped = stRole == null ? null : new DSharpRole(stRole);
            var villagerRoleWrapped = villagerRole == null ? null : new DSharpRole(villagerRole);

            await BotSetup!.ModifyTownAsync(new DSharpInteractionContext(ctx), chatChanWrapped, nightCatWrapped, stRoleWrapped, villagerRoleWrapped);
        }


        [SlashCommand("addTown", "Add a new town composed of existing channel and roles on this server")]
        public async Task AddTownCommand(InteractionContext ctx,
            [Option("controlChannel", "Control channel (must be text)")] DiscordChannel controlChannel,
            [Option("townSquare", "Town Square channel (must be voice)")] DiscordChannel townSquare,
            [Option("dayCategory", "Day Category (must contain control and town square channels)")] DiscordChannel dayCategory,
            [Option("storytellerRole", "Role to grant storytellers in this town during an active game")] DiscordRole stRole,
            [Option("villagerRole", "Role to grant villagers in this town during an active game")] DiscordRole villagerRole,
            [Option("nightCategory", "Night Category (optional)")] DiscordChannel? nightCategory = null,
            [Option("chatChannel", "Chat channel (optional, must be text)")] DiscordChannel? chatChannel = null
            )
        {
            // Need to do error handling here at the argument level before we try to create wrappers for stuff that's
            // totally the wrong type
            List<string> errors = new();

            if (controlChannel.Type != DSharpPlus.ChannelType.Text)
            {
                errors.Add($"- Control channel `{controlChannel.Name}` is not a text channel!");
            }
            if (dayCategory.Type != DSharpPlus.ChannelType.Category)
            {
                errors.Add($"- Day category `{dayCategory.Name}` is not a category!");
            }
            if (townSquare.Type != DSharpPlus.ChannelType.Voice)
            {
                errors.Add($"- Town Square channel `{townSquare.Name}` is not a voice channel!");
            }
            if (dayCategory.Type == DSharpPlus.ChannelType.Category && !dayCategory.Children.Contains(controlChannel))
            {
                errors.Add($"- Control channel `{controlChannel.Name}` is not in the **{dayCategory.Name}** category!");
            }
            if (dayCategory.Type == DSharpPlus.ChannelType.Category && !dayCategory.Children.Contains(townSquare))
            {
                errors.Add($"- Town Square channel `{townSquare.Name}` is not in the **{dayCategory.Name}** category!");
            }
            if(nightCategory != null && nightCategory.Type != DSharpPlus.ChannelType.Category)
            {
                errors.Add($"- Night category `{nightCategory.Name}` is not a category!");
            }
            if(chatChannel != null && chatChannel.Type != DSharpPlus.ChannelType.Text)
            {
                errors.Add($"- Chat channel `{chatChannel.Name}` is not a text channel!");
            }
            if(chatChannel != null && dayCategory.Type == DSharpPlus.ChannelType.Category && !dayCategory.Children.Contains(chatChannel))
            {
                errors.Add($"- Chat channel `{chatChannel.Name}` is not in the **{dayCategory.Name}** category!");
            }

            if(errors.Count > 0)
            {
                string msg = "Couldn't add a town due to the following errors:\n" + string.Join("\n", errors);
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(msg));
                return;
            }

            var controlChan = new DSharpChannel(controlChannel);
            var townChan = new DSharpChannel(townSquare);
            var dayCat = new DSharpChannelCategory(dayCategory);

            var nightCat = nightCategory == null ? null : new DSharpChannelCategory(nightCategory);
            var stRoleWrapped = new DSharpRole(stRole);
            var villagerRoleWrapped = new DSharpRole(villagerRole);
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
