﻿using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Interaction;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using static Bot.Api.IBaseChannel;

namespace Bot.Core
{
    public class BotSetup : IBotSetup
    {
        public IEnumerable<string> DefaultExtraDayChannels => new[] { "Dark Alley", "Library", "Graveyard", "Pie Shop" };

        private readonly ITownDatabase m_townDb;
        private readonly ITownResolver m_townResolver;
        private readonly IBotSystem m_botSystem;
        private readonly IComponentService m_componentService;
        private readonly ICommandMetricDatabase m_commandMetricsDatabase;
        private readonly IDateTime m_dateTime;
        private readonly IGuildInteractionWrapper m_interactionWrapper;

        public BotSetup(IServiceProvider sp)
        {
            sp.Inject(out m_townDb);
            sp.Inject(out m_townResolver);
            sp.Inject(out m_botSystem);
            sp.Inject(out m_componentService);
            sp.Inject(out m_commandMetricsDatabase);
            sp.Inject(out m_dateTime);
            sp.Inject(out m_interactionWrapper);
        }

        public Task CreateTownAsync(IBotInteractionContext ctx, string townName, IRole? guildPlayerRole, IRole? guildStRole, bool useNight) => 
            m_interactionWrapper.WrapInteractionAsync($"Creating town...", ctx, 
                l => PerformCreateTown(l, ctx, townName, guildPlayerRole, guildStRole, useNight));
        public Task TownInfoAsync(IBotInteractionContext ctx) => 
            m_interactionWrapper.WrapInteractionAsync($"Looking up town...", ctx, 
                l => PerformTownInfo(l, ctx));
        public Task DestroyTownAsync(IBotInteractionContext ctx, string townName) =>
            m_interactionWrapper.WrapInteractionAsync($"Destroying channels and roles for town {townName}...", ctx,
                l => PerformDestroyTown(l, ctx, townName));
        
        private async Task<bool> TryDestroyChannel(IGuild g, string name)
        {
            var chan = g.GetChannelByName(name);
            if(chan != null)
            {
                await chan.DeleteAsync();
                return true;
            }

            return false;
        }

        private async Task<InteractionResult> PerformDestroyTown(IProcessLogger _, IBotInteractionContext ctx, string townName)
        {
            var guild = ctx.Guild;

            var tdesc = new TownDescription();
            tdesc.PopulateFromTownName(townName, guild, ctx.Member, true);

            List<string> notDeletedUnfamiliar = new();
            List<string> notDeleted = new();

            bool success = true;

            // Night Category
            var nightCat = guild.GetCategoryByName(tdesc.NightCategoryName!);
            if(nightCat != null)
            {
                var toDestroy = new List<IChannel>();
                foreach(var channel in nightCat.Channels)
                {
                    if(channel.IsVoice && channel.Name == IBotSetup.DefaultCottageName)
                    {
                        toDestroy.Add(channel);
                    }
                }

                foreach(var channel in toDestroy)
                {
                    await channel.DeleteAsync();
                }

                if(nightCat.Channels.Count == 0)
                {
                    await nightCat.DeleteAsync();
                }
                else
                {
                    foreach(var channel in nightCat.Channels)
                    {
                        notDeletedUnfamiliar.Add($"**{nightCat.Name}** / **{channel.Name}**");
                    }
                    notDeleted.Add($"**{nightCat.Name}** (category)");
                    success = false;
                }
            }

            var dayCat = guild.GetCategoryByName(tdesc.DayCategoryName);
            if(dayCat != null)
            {
                var chatChan = dayCat.GetChannelByName(ChannelHelper.MakeTextChannelName(tdesc.ChatChannelName));
                if(chatChan != null && chatChan.IsText)
                {
                    await chatChan.DeleteAsync();
                }

                var townSquare = dayCat.GetChannelByName(tdesc.TownSquareName);
                if(townSquare != null && townSquare.IsVoice)
                {
                    await townSquare.DeleteAsync();
                }

                foreach(string s in DefaultExtraDayChannels)
                {
                    var chan = dayCat.GetChannelByName(s);
                    if(chan != null && chan.IsVoice)
                    {
                        await chan.DeleteAsync();
                    }
                }

                var controlChan = dayCat.GetChannelByName(ChannelHelper.MakeTextChannelName(tdesc.ControlChannelName));
                if(controlChan != null && controlChan.IsText)
                {
                    if(success && dayCat.Channels.Count == 1)
                    {
                        await controlChan.DeleteAsync();
                    }
                    else
                    {
                        notDeleted.Add($"**{tdesc.DayCategoryName}** / **{tdesc.ControlChannelName}**");
                        success = false;
                    }
                }

                if(dayCat.Channels.Count == 0)
                {
                    await dayCat.DeleteAsync();
                }
                else
                {
                    notDeleted.Add($"**{dayCat.Name}** (category)");
                    success = false;
                }
            }

            // Don't try to remove roles if we had an issue with something else - we might need them to see channels that didn't delete!
            if(success)
            {
                var gameVillager = guild.GetRoleByName(tdesc.VillagerRoleName);
                if(gameVillager != null)
                {
                    await gameVillager.DeleteAsync();
                }
                else
                {
                    notDeleted.Add($"**{tdesc.VillagerRoleName}** (role)");
                }

                var gameStoryteller = guild.GetRoleByName(tdesc.StorytellerRoleName);
                if( gameStoryteller != null)
                {
                    await gameStoryteller.DeleteAsync();
                }
                else
                {
                    notDeleted.Add($"**{tdesc.StorytellerRoleName}** (role)");
                }
            }

            var message = $"Town **{townName}** has been destroyed.";
            if(!success)
            {
                message = $"I destroyed what I knew about for Town **{townName}**.";
                if(notDeletedUnfamiliar.Count > 0)
                {
                    message += "\n\nI did not destroy some things I was unfamiliar with:";
                    foreach(var s in notDeletedUnfamiliar)
                    {
                        message += "\n * " + s;
                    }
                    message += "\n\nYou can destroy them manually and run this command again.\n";
                }
                if(notDeleted.Count > 0)
                {
                    message += "\nI did not destroy these things yet, just in case you still need them:";
                    foreach(var s in notDeleted)
                    {
                        message += "\n * " + s;
                    }
                }
            }

            if(success)
            {
                // TODO
                // Remove from town DB
            }

            return InteractionResult.FromMessage(message);
        }
        
        private async Task<InteractionResult> PerformTownInfo(IProcessLogger _, IBotInteractionContext ctx)
        {
            var townRecord = await m_townDb.GetTownRecordAsync(ctx.Guild.Id, ctx.Channel.Id);

            if (townRecord != null)
            {
                var town = await m_townResolver.ResolveTownAsync(townRecord);

                if (town != null)
                {
                    var u = "Unknown";

                    var embed = m_botSystem.CreateEmbedBuilder();
                    embed.WithTitle($"{town.Guild?.Name ?? u} // {town.DayCategory?.Name ?? u}")
                        .WithDescription($"Created {townRecord.Timestamp} by {townRecord.AuthorName}");
                    embed.AddField("Control Channel", town.ControlChannel?.Name ?? u);
                    embed.AddField("Town Square", town.TownSquare?.Name ?? u);
                    if(town.ChatChannel != null) embed.AddField("Chat Channel", town.ChatChannel.Name);
                    embed.AddField("Day Category", town.DayCategory?.Name ?? u);
                    if (town.NightCategory != null) embed.AddField("Night Category", town.NightCategory.Name);
                    embed.AddField("Storyteller Role", town.StorytellerRole?.Name ?? u);
                    embed.AddField("Villager Role", town.VillagerRole?.Name ?? u);

                    return InteractionResult.FromMessageAndEmbeds("Town found!", embed.Build());
                }
            }

            return InteractionResult.FromMessage("Couldn't find an active town for this server & channel!");
        }

        private async Task<InteractionResult> PerformCreateTown(IProcessLogger _, IBotInteractionContext ctx, string townName, IRole? guildPlayerRole, IRole? guildStRole, bool useNight)
        {
            TownDescription tdesc = new TownDescription();
            tdesc.PopulateFromTownName(townName, ctx.Guild, ctx.Member, useNight);

            await CreateTown(tdesc, ctx.Member, guildStRole, guildPlayerRole);

            await m_commandMetricsDatabase.RecordCommand("createtown", m_dateTime.Now);

            return InteractionResult.FromMessage($"Created new town **{townName}**!");
        }

        public Task AddTown(ITown town, IMember author)
        {
            return m_townDb.AddTownAsync(town, author);
        }


        public async Task DestroyTown(ulong guildId, ulong channelId)
        {
            var townRec = await m_townDb.GetTownRecordAsync(guildId, channelId);

            await m_commandMetricsDatabase.RecordCommand("destroytown", m_dateTime.Now);

            // TODO
        }

        public async Task CreateTown(TownDescription townDesc, IMember author, IRole? guildStRole = null, IRole? guildPlayerRole = null)
        {
            IGuild guild = townDesc.Guild;

            Town newTown = new Town();
            newTown.Guild = guild;

            // Get bot role
            var botRole = guild.BotRole;
            if (botRole == null)
                throw new CreateTownException($"Could not find bot role!");
            var everyoneRole = guild.EveryoneRole;

            // Make sure things aren't null
            townDesc.FallbackToDefaults();

            // First create the roles for this town
            newTown.StorytellerRole = await RoleHelper.GetOrCreateRole(guild, townDesc.StorytellerRoleName, Color.Magenta);
            if (newTown.StorytellerRole == null)
                throw new CreateTownException($"Could not find or create Storyteller role '{townDesc.StorytellerRoleName}'");

            newTown.VillagerRole = await RoleHelper.GetOrCreateRole(guild, townDesc.VillagerRoleName, Color.DarkMagenta);
            if (newTown.VillagerRole == null)
                throw new CreateTownException($"Could not find or create Villager role '{townDesc.VillagerRoleName}'");


            // Create Day Category and set up visibility
            newTown.DayCategory = await ChannelHelper.GetOrCreateCategory(guild, townDesc.DayCategoryName!);
            if (newTown.DayCategory == null)
                throw new CreateTownException($"Could not find or create day category '{townDesc.DayCategoryName}'");
            await newTown.DayCategory.AddOverwriteAsync(newTown.VillagerRole, Permissions.AccessChannels);
            await newTown.DayCategory.AddOverwriteAsync(botRole, Permissions.AccessChannels | Permissions.MoveMembers);

            newTown.ControlChannel = await ChannelHelper.GetOrCreateTextChannel(guild, newTown.DayCategory, townDesc.ControlChannelName!);
            if (newTown.ControlChannel == null)
                throw new CreateTownException($"Could not find or create control channel '{townDesc.ControlChannelName}'");
            await newTown.ControlChannel.AddOverwriteAsync(botRole, Permissions.AccessChannels);
            await newTown.ControlChannel.AddOverwriteAsync(newTown.VillagerRole, allow: Permissions.None, deny: Permissions.AccessChannels);

            if (guildStRole != null)
            {
                await newTown.ControlChannel.AddOverwriteAsync(guildStRole, Permissions.AccessChannels);
                await newTown.ControlChannel.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
            }

            newTown.TownSquare = await ChannelHelper.GetOrCreateVoiceChannel(guild, newTown.DayCategory, townDesc.TownSquareName!);
            if (newTown.TownSquare == null)
                throw new CreateTownException($"Could not find or create town square '{townDesc.TownSquareName}'");

            if (guildPlayerRole != null)
            {
                await newTown.TownSquare.AddOverwriteAsync(guildPlayerRole, Permissions.AccessChannels);
                await newTown.DayCategory.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
            }

            // Chat channel is optional
            if (townDesc.ChatChannelName != null)
            {
                newTown.ChatChannel = await ChannelHelper.GetOrCreateTextChannel(guild, newTown.DayCategory, townDesc.ChatChannelName);
                if (newTown.ChatChannel == null)
                    throw new CreateTownException($"Could not find or create chat channel '{townDesc.ChatChannelName}'");

                await newTown.ChatChannel.AddOverwriteAsync(botRole, Permissions.AccessChannels);

                if (guildPlayerRole == null)
                    await newTown.ChatChannel.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
            }

            foreach (var chanName in DefaultExtraDayChannels)
            {
                var newChan = await ChannelHelper.GetOrCreateVoiceChannel(guild, newTown.DayCategory, chanName);
                if (newChan == null)
                    throw new CreateTownException($"Could not find or create extra day channel '{chanName}'");

                if (guildPlayerRole == null)
                    await newChan.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
            }

            // Night category is optional
            if (townDesc.NightCategoryName != null)
            {
                newTown.NightCategory = await ChannelHelper.GetOrCreateCategory(guild, townDesc.NightCategoryName);
                if (newTown.NightCategory == null)
                    throw new CreateTownException($"Could not find or create night category '{townDesc.NightCategoryName}'");

                await newTown.NightCategory.AddOverwriteAsync(newTown.StorytellerRole, Permissions.AccessChannels);
                await newTown.NightCategory.AddOverwriteAsync(botRole, Permissions.AccessChannels | Permissions.MoveMembers);
                await newTown.NightCategory.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);

                for (int i = 0; i < IBotSetup.NumCottages; i++)
                {
                    await guild.CreateVoiceChannelAsync(IBotSetup.DefaultCottageName, newTown.NightCategory);
                }
            }

            await AddTown(newTown, author);
        }


    }

    public class CreateTownException : Exception
    {
        public CreateTownException(string message)
            : base(message)
        {
        }
    }
}
