using Bot.Api;
using System.Threading.Tasks;

namespace Bot.Core
{
    public interface IVoteHandler
    {
        Task PerformVoteAsync(ITownRecord townRecord);
    }
}
