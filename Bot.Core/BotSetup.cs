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

            await CreateTown(tdesc, ctx.Member, guildStRole, guildPlayerRole);

            var builder = m_botSystem.CreateWebhookBuilder().WithContent($"Created new town **{townName}**!");
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

        public async Task DestroyTown(ulong guildId, ulong channelId)
        {
            var townRec = await m_townDb.GetTownRecordAsync(guildId, channelId);

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

            townDesc = FallbackToDefaults(townDesc);

            // First create the roles for this town
            newTown.StorytellerRole = await RoleHelper.GetOrCreateRole(guild, townDesc.StorytellerRoleName!, Color.Magenta);
            if (newTown.StorytellerRole == null)
                throw new CreateTownException($"Could not find or create Storyteller role '{townDesc.StorytellerRoleName}'");

            newTown.VillagerRole = await RoleHelper.GetOrCreateRole(guild, townDesc.VillagerRoleName!, Color.DarkMagenta);
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
                    await ChannelHelper.GetOrCreateVoiceChannel(guild, newTown.NightCategory, IBotSetup.DefaultCottageName);
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
