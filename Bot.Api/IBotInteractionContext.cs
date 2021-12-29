using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotInteractionContext
    {
        IServiceProvider Services { get; }
        Task CreateDeferredResponseMessage(IBotInteractionResponseBuilder response);
    }
}
