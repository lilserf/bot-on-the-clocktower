using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotInteractionContext
    {
        Task CreateDeferredResponseMessage(IBotInteractionResponseBuilder response);
    }
}
