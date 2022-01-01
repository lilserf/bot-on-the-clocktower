using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotClient
    {
        Task ConnectAsync();

        Task<ITown> ResolveTownAsync(ITownRecord rec);

        // Do we actually need these in the interface?
        Task<IGuild> GetGuildAsync(ulong id);
        Task<IChannel> GetChannelAsync(ulong id);
    }
}
