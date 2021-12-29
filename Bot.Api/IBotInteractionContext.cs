using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotInteractionContext
    {
        IServiceProvider Services { get; }
        Task CreateDeferredResponseMessageAsync();
        Task EditResponseAsync(IBotWebhookBuilder webhookBuilder);
    }
}
