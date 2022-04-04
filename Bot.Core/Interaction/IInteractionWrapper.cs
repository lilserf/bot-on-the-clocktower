using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public interface IInteractionWrapper<TKey> where TKey : notnull
    {
        Task WrapInteractionAsync(string initialMessage, IBotInteractionContext context, Func<IProcessLogger, Task<InteractionResult>> process);
    }
}
