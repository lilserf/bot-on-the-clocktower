using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotInteractionContext
    {
        IGuild Guild { get; }
        IChannel Channel { get; }
        IMember Member { get; }
        string? ComponentCustomId { get; }
        Task DeferInteractionResponse();
        Task EditResponseAsync(IBotWebhookBuilder webhookBuilder);
        Task UpdateOriginalMessageAsync(IInteractionResponseBuilder builder);

    }
}
