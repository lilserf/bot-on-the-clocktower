using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public interface IInteractionErrorHandler<TKey> where TKey : notnull
    {
        Task<InteractionResult> TryProcessReportingErrorsAsync(TKey key, IMember requester, Func<IProcessLogger, Task<InteractionResult>> process);
    }
}