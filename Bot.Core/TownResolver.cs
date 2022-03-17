using Bot.Api;
using Bot.Api.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class TownResolver : ITownResolver
    {
        private readonly IBotClient m_client;
        private readonly ITownDatabase m_townDb;

        public TownResolver(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_client);
            serviceProvider.Inject(out m_townDb);
        }

        public async Task<ITown?> ResolveTownAsync(ITownRecord rec)
        {
            var guild = await m_client.GetGuildAsync(rec.GuildId);
            if (guild != null)
            {
                var controlChannelResult = await GetChannelAsync(rec.ControlChannelId, rec.ControlChannel, false);
                var dayCategoryResult = await GetChannelCategoryAsync(rec.DayCategoryId, rec.DayCategory);
                var nightCategoryResult = await GetChannelCategoryAsync(rec.NightCategoryId, rec.NightCategory);
                var chatChannelResult = await GetChannelAsync(rec.ChatChannelId, rec.ChatChannel, false);
                var townSquareResult = await GetChannelAsync(rec.TownSquareId, rec.TownSquare, true);

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

                if (AnyUpdatesRequired(
                    controlChannelResult,
                    dayCategoryResult,
                    nightCategoryResult, 
                    chatChannelResult,
                    townSquareResult))
                    await m_townDb.UpdateTownAsync(town);

                return town;
            }
            return null;
        }

        private bool AnyUpdatesRequired(params GetChannelResultBase[] results) => results.Any(r => r.UpdateRequired != ChannelUpdateRequired.None);

        private async Task<GetChannelResult> GetChannelAsync(ulong channelId, string? channelName, bool isVoice)
        {
            ChannelUpdateRequired update = ChannelUpdateRequired.None;

            var channel = await m_client.GetChannelAsync(channelId);
            if (channel != null && channel.Name != channelName)
                update = ChannelUpdateRequired.Name;

            return new GetChannelResult(channel, update);
        }

        private async Task<GetChannelCategoryResult> GetChannelCategoryAsync(ulong channelId, string? channelName)
        {
            ChannelUpdateRequired update = ChannelUpdateRequired.None;

            var channelCategory = await m_client.GetChannelCategoryAsync(channelId);

            if (channelCategory != null && channelCategory.Name != channelName)
                update = ChannelUpdateRequired.Name;

            return new GetChannelCategoryResult(channelCategory, update);
        }

        private static IRole? GetRoleForGuild(IGuild guild, ulong roleId)
        {
            if (guild.Roles.TryGetValue(roleId, out var role))
                return role;
            return null;
        }

        private enum ChannelUpdateRequired
        {
            None,
            Id,
            Name,
        }

        private class GetChannelResult : GetChannelResultBase<IChannel>
        {
            public GetChannelResult(IChannel? channel, ChannelUpdateRequired updateRequired)
                : base(channel, updateRequired)
            { }
        }

        private class GetChannelCategoryResult : GetChannelResultBase<IChannelCategory>
        {
            public GetChannelCategoryResult(IChannelCategory? channel, ChannelUpdateRequired updateRequired)
                : base(channel, updateRequired)
            { }
        }

        private class GetChannelResultBase<T> : GetChannelResultBase where T : class
        {
            public T? Channel { get; }

            public GetChannelResultBase(T? channel, ChannelUpdateRequired updateRequired)
                :base(updateRequired)
            {
                Channel = channel;
            }
        }

        private class GetChannelResultBase
        {
            public ChannelUpdateRequired UpdateRequired { get; }
            public GetChannelResultBase(ChannelUpdateRequired updateRequired)
            {
                UpdateRequired = updateRequired;
            }
        }
    }
}
