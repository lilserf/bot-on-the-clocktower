using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public abstract class BaseInteractionErrorHandler<TKey> where TKey : notnull
    {
        public Task<InteractionResult> TryProcessReportingErrorsAsync(TKey key, IMember requester, Func<IProcessLogger, Task<InteractionResult>> process)
        {
            return InteractionWrapper.TryProcessReportingErrorsAsync(key, requester, process, GetFriendlyStringForKey);
        }

        protected abstract string GetFriendlyStringForKey(TKey key);
    }
}