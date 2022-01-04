using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotClient
    {
        Task ConnectAsync(IServiceProvider serviceProvider);

        Task<ITown> ResolveTownAsync(ITownRecord rec);
    }
}
