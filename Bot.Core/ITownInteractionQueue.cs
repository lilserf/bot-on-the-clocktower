using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public interface ITownInteractionQueue
    {
        Task QueueInteractionAsync(string initialMessage, IBotInteractionContext context, Func<Task<InteractionResult>> queuedTask);
    }
}
