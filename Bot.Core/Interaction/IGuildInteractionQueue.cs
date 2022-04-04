using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public interface IGuildInteractionQueue
    {
        Task QueueInteractionAsync(string initialMessage, IBotInteractionContext context, Func<Task<InteractionResult>> queuedTask);
    }
}
