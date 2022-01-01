using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotInteractionContext
    {
        IServiceProvider Services { get; }
        IGuild Guild { get; }
        IChannel Channel { get; }
        IMember Member { get; }
        Task CreateDeferredResponseMessageAsync();
        Task EditResponseAsync(IBotWebhookBuilder webhookBuilder);
    }
}
