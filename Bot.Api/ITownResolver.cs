using Bot.Api.Database;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface ITownResolver
    {
        Task<ITown?> ResolveTownAsync(ITownRecord rec);
    }
}
