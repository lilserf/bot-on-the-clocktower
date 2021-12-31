using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotClient
    {
        Task ConnectAsync();

        Task<ITown> ResolveTownAsync(ITownRecord rec);

        Task<IGuild> GetGuildAsync(ulong id);
        Task<IChannel> GetChannelAsync(ulong id);
    }
}
