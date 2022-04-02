using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotClient
    {
        Task ConnectAsync(IServiceProvider serviceProvider);
        Task DisconnectAsync();

        Task<IGuild?> GetGuildAsync(ulong guildId);

        event EventHandler<EventArgs> Connected;
    }
}
