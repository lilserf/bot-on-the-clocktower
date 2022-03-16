using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public enum BotChannelType
    {
        Text,
        Voice,
    }

    public interface IBotClient
    {
        Task ConnectAsync(IServiceProvider serviceProvider);

        Task<GetChannelResult> GetChannelAsync(ulong id, string? name, BotChannelType type);
        Task<GetChannelCategoryResult> GetChannelCategoryAsync(ulong id, string? name);
        Task<IGuild?> GetGuildAsync(ulong guildId);

        event EventHandler<EventArgs> Connected;
    }

    public enum ChannelUpdateRequired
    {
        None,
        Id,
        Name,
    }

    public class GetChannelResult : GetChannelResultBase<IChannel>
    {
        public GetChannelResult(IChannel? channel, ChannelUpdateRequired updateRequired)
            : base(channel, updateRequired)
        { }
    }
    public class GetChannelCategoryResult : GetChannelResultBase<IChannelCategory>
    {
        public GetChannelCategoryResult(IChannelCategory? channel, ChannelUpdateRequired updateRequired)
            : base(channel, updateRequired)
        { }
    }

    public class GetChannelResultBase<T> where T : class
    {
        public ChannelUpdateRequired UpdateRequired { get; }
        public T? Channel { get; }

        public GetChannelResultBase(T? channel, ChannelUpdateRequired updateRequired)
        {
            UpdateRequired = updateRequired;
            Channel = channel;
        }
    }
}
