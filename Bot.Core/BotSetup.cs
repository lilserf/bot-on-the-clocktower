using Bot.Api;
using Bot.Api.Database;
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
        private readonly IBotSystem m_botSystem;
        private readonly IComponentService m_componentService;

        public BotSetup(IServiceProvider sp)
        {
            sp.Inject(out m_townDb);
            sp.Inject(out m_botSystem);
            sp.Inject(out m_componentService);
        }

        public async Task CommandCreateTown(IBotInteractionContext ctx, string townName, IRole? guildPlayerRole, IRole? guildStRole, bool useNight)
        {
            await ctx.DeferInteractionResponse();

            TownDescription tdesc = new TownDescription();
            tdesc.Guild = ctx.Guild;
            tdesc.Author = ctx.Member;
            tdesc.TownName = townName;
            tdesc.ChatChannelName = IBotSetup.DefaultChatChannelName;
            if (useNight)
                tdesc.NightCategoryName = string.Format(IBotSetup.DefaultNightCategoryFormat, townName);

            await CreateTown(tdesc, guildStRole, guildPlayerRole);

            var builder = m_botSystem.CreateWebhookBuilder().WithContent($"Created new town *{townName}*!");
            await ctx.EditResponseAsync(builder);
        }

        public Task AddTown(ITown town, IMember author)
        {
            return m_townDb.AddTownAsync(town, author);
        }

        private static TownDescription FallbackToDefaults(TownDescription desc)
        {
            if (desc.DayCategoryName == null)
                desc.DayCategoryName = string.Format(IBotSetup.DefaultDayCategoryFormat, desc.TownName);
            if (desc.TownSquareName == null)
                desc.TownSquareName = IBotSetup.DefaultTownSquareChannelName;
            if (desc.ControlChannelName == null)
                desc.ControlChannelName = string.Format(IBotSetup.DefaultControlChannelFormat, desc.TownName);
            if (desc.StorytellerRoleName == null)
                desc.StorytellerRoleName = string.Format(IBotSetup.DefaultStorytellerRoleFormat, desc.TownName);
            if (desc.VillagerRoleName == null)
                desc.VillagerRoleName = string.Format(IBotSetup.DefaultVillagerRoleFormat, desc.TownName);

            return desc;
        }

        public async Task CreateTown(TownDescription townDesc, IRole? guildStRole = null, IRole? guildPlayerRole = null)
        {
            // TODO: make sure this all works correctly if roles/channels/etc with these names already exist
            IGuild guild = townDesc.Guild;

            // Get bot role
            var botRole = guild.BotRole;
            var everyoneRole = guild.EveryoneRole;

            townDesc = FallbackToDefaults(townDesc);

            // First create the roles for this town
            var gameStRole = await guild.CreateRoleAsync(townDesc.StorytellerRoleName!, Color.Magenta);
            var gameVillagerRole = await guild.CreateRoleAsync(townDesc.VillagerRoleName!, Color.DarkMagenta);

            // Create Day Category and set up visibility
            var dayCat = await guild.CreateCategoryAsync(townDesc.DayCategoryName!);
            await dayCat.AddOverwriteAsync(gameVillagerRole, Permissions.AccessChannels);
            await dayCat.AddOverwriteAsync(botRole, Permissions.AccessChannels | Permissions.MoveMembers);

            var controlChan = await guild.CreateTextChannelAsync(townDesc.ControlChannelName!, dayCat);
            await controlChan.AddOverwriteAsync(botRole, Permissions.AccessChannels);
            await controlChan.AddOverwriteAsync(gameVillagerRole, allow: Permissions.None, deny: Permissions.AccessChannels);

            if(guildStRole != null)
            {
                await controlChan.AddOverwriteAsync(guildStRole, Permissions.AccessChannels);
                await controlChan.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
            }

            var townSquareChan = await guild.CreateVoiceChannelAsync(townDesc.TownSquareName!, dayCat);

            if (guildPlayerRole != null)
            {
                await dayCat.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
                await townSquareChan.AddOverwriteAsync(guildPlayerRole, Permissions.AccessChannels);
            }

            // Chat channel is optional
            if (townDesc.ChatChannelName != null)
            {
                var chatChan = await guild.CreateTextChannelAsync(townDesc.ChatChannelName, dayCat);
                await chatChan.AddOverwriteAsync(botRole, Permissions.AccessChannels);

                if (guildPlayerRole == null)
                    await chatChan.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
            }
            
            foreach(var chanName in DefaultExtraDayChannels)
            {
                var newChan = await guild.CreateVoiceChannelAsync(chanName, dayCat);
                if (guildPlayerRole == null)
                    await newChan.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);
            }

            // Night category is optional
            if(townDesc.NightCategoryName != null)
            {
                var nightCat = await guild.CreateCategoryAsync(townDesc.NightCategoryName);
                await nightCat.AddOverwriteAsync(gameStRole, Permissions.AccessChannels);
                await nightCat.AddOverwriteAsync(botRole, Permissions.AccessChannels | Permissions.MoveMembers);
                await nightCat.AddOverwriteAsync(everyoneRole, allow: Permissions.None, deny: Permissions.AccessChannels);

                for(int i=0; i < IBotSetup.NumCottages; i++)
                {
                    await guild.CreateVoiceChannelAsync(IBotSetup.DefaultCottageName, nightCat);
                }
            }

        }
    }
}
