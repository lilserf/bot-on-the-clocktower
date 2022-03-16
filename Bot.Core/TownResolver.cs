using Bot.Api;
using Bot.Api.Database;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class TownResolver : ITownResolver
    {
        private readonly IBotClient m_client;
        public TownResolver(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_client);
        }

        public async Task<ITown?> ResolveTownAsync(ITownRecord rec)
        {
            var guild = await m_client.GetGuildAsync(rec.GuildId);
            if (guild != null)
            {
                var controlChannelResult = await m_client.GetChannelAsync(rec.ControlChannelId, rec.ControlChannel, BotChannelType.Text);
                var dayCategoryResult = await m_client.GetChannelCategoryAsync(rec.DayCategoryId, rec.DayCategory);
                var nightCategoryResult = await m_client.GetChannelCategoryAsync(rec.NightCategoryId, rec.NightCategory);
                var chatChannelResult = await m_client.GetChannelAsync(rec.ChatChannelId, rec.ChatChannel, BotChannelType.Text);
                var townSquareResult = await m_client.GetChannelAsync(rec.TownSquareId, rec.TownSquare, BotChannelType.Voice);

                var town = new Town(rec)
                {
                    Guild = guild,
                    ControlChannel = controlChannelResult.Channel,
                    DayCategory = dayCategoryResult.Channel,
                    NightCategory = nightCategoryResult.Channel,
                    ChatChannel = chatChannelResult.Channel,
                    TownSquare = townSquareResult.Channel,
                    StorytellerRole = GetRoleForGuild(guild, rec.StorytellerRoleId),
                    VillagerRole = GetRoleForGuild(guild, rec.VillagerRoleId),
                };
                return town;
            }
            return null;
        }

        private static IRole? GetRoleForGuild(IGuild guild, ulong roleId)
        {
            if (guild.Roles.TryGetValue(roleId, out var role))
                return role;
            return null;
        }
    }
}
