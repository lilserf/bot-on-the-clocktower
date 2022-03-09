using Bot.Api.Database;
using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotClient
    {
        Task ConnectAsync(IServiceProvider serviceProvider);

        Task<ITown?> ResolveTownAsync(ITownRecord rec);

        Task<IGuild?> GetGuild(ulong guildId);
    }
}
