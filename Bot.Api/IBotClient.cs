using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotClient
    {
        Task ConnectAsync(IServiceProvider serviceProvider);

        Task<IChannel?> GetChannelAsync(ulong id);
        Task<IChannelCategory?> GetChannelCategoryAsync(ulong id);
        Task<IGuild?> GetGuildAsync(ulong guildId);

        event EventHandler<EventArgs> Connected;
    }
}
