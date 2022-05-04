using Bot.Api;
using Bot.Api.Database;
using System;
using System.Collections.Generic;
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
                var dayCategoryResult = GetChannelCategory(guild, rec.DayCategoryId, rec.DayCategory);
                var nightCategoryResult = GetChannelCategory(guild, rec.NightCategoryId, rec.NightCategory);
                var controlChannelResult = GetChannel(guild, dayCategoryResult.Channel, rec.ControlChannelId, rec.ControlChannel, false);
                var chatChannelResult = GetChannel(guild, dayCategoryResult.Channel, rec.ChatChannelId, rec.ChatChannel, false);
                var townSquareResult = GetChannel(guild, dayCategoryResult.Channel, rec.TownSquareId, rec.TownSquare, true);
                var storytellerRoleResult = GetRoleForGuild(guild, rec.StorytellerRoleId, rec.StorytellerRole);
                var villagerRoleResult = GetRoleForGuild(guild, rec.VillagerRoleId, rec.VillagerRole);

                var town = new Town(rec)
                {
                    Guild = guild,
                    ControlChannel = controlChannelResult.Channel,
                    DayCategory = dayCategoryResult.Channel,
                    NightCategory = nightCategoryResult.Channel,
                    ChatChannel = chatChannelResult.Channel,
                    TownSquare = townSquareResult.Channel,
                    StorytellerRole = storytellerRoleResult.Item1,
                    VillagerRole = villagerRoleResult.Item1,
                };

                bool updatesRequired = AnyUpdatesRequired(
                    SelectUpdatesRequired(
                        controlChannelResult,
                        dayCategoryResult,
                        nightCategoryResult,
                        chatChannelResult,
                        townSquareResult)
                    .Concat(new[] {
                        storytellerRoleResult.Item2,
                        villagerRoleResult.Item2 }));

                if (updatesRequired)
                    await m_townDb.UpdateTownAsync(town);

                return town;
            }
            return null;
        }

        private static IEnumerable<UpdateRequired> SelectUpdatesRequired(params GetChannelResultBase[] results) => results.Select(r => r.UpdateRequired);
        private static bool AnyUpdatesRequired(IEnumerable<UpdateRequired> updates) => updates.Any(u => u != UpdateRequired.None);

        private static GetChannelResult GetChannel(IGuild guild, IChannelCategory? parentCategory, ulong channelId, string? channelName, bool expectedIsVoice)
        {
            UpdateRequired update = UpdateRequired.None;

            var channel = guild.GetChannel(channelId);
            if (channel == null && parentCategory != null)
                channel = parentCategory!.Channels.FirstOrDefault(c => c.Name == channelName);

            if (channel != null)
                if (channel.IsVoice != expectedIsVoice)
                    channel = null;
                else if (channel.Name != channelName)
                    update = UpdateRequired.Name;
                else if (channel.Id != channelId)
                    update = UpdateRequired.Id;

            return new GetChannelResult(channel, update);
        }

        private static GetChannelCategoryResult GetChannelCategory(IGuild guild, ulong channelId, string? channelName)
        {
            UpdateRequired update = UpdateRequired.None;

            var channelCategory = guild.GetChannelCategory(channelId);
            if (channelCategory == null)
                channelCategory = guild.ChannelCategories.FirstOrDefault(c => c.Name == channelName);

            if (channelCategory != null)
                if (channelCategory.Name != channelName)
                    update = UpdateRequired.Name;
                else if (channelCategory.Id != channelId)
                    update = UpdateRequired.Id;

            return new GetChannelCategoryResult(channelCategory, update);
        }

        private static (IRole?, UpdateRequired) GetRoleForGuild(IGuild guild, ulong roleId, string? roleName)
        {
            UpdateRequired update = UpdateRequired.None;

            if (!guild.Roles.TryGetValue(roleId, out var role))
                role = guild.Roles.Values.FirstOrDefault(r => r.Name== roleName);

            if (role != null)
                if (role.Name != roleName)
                    update = UpdateRequired.Name;
                else if (role.Id != roleId)
                    update = UpdateRequired.Id;

            return (role, update);
        }

        private enum UpdateRequired
        {
            None,
            Id,
            Name,
        }

        private class GetChannelResult : GetChannelResultBase<IChannel>
        {
            public GetChannelResult(IChannel? channel, UpdateRequired updateRequired)
                : base(channel, updateRequired)
            { }
        }

        private class GetChannelCategoryResult : GetChannelResultBase<IChannelCategory>
        {
            public GetChannelCategoryResult(IChannelCategory? channel, UpdateRequired updateRequired)
                : base(channel, updateRequired)
            { }
        }

        private class GetChannelResultBase<T> : GetChannelResultBase where T : class
        {
            public T? Channel { get; }

            public GetChannelResultBase(T? channel, UpdateRequired updateRequired)
                :base(updateRequired)
            {
                Channel = channel;
            }
        }

        private class GetChannelResultBase
        {
            public UpdateRequired UpdateRequired { get; }
            public GetChannelResultBase(UpdateRequired updateRequired)
            {
                UpdateRequired = updateRequired;
            }
        }
    }
}
