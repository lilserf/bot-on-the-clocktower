using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public abstract class BaseInteractionErrorHandler<TKey> where TKey : notnull
    {
        public Task<string> TryProcessReportingErrorsAsync(TKey key, IMember requester, Func<IProcessLogger, Task<string>> process)
        {
            return InteractionWrapper.TryProcessReportingErrorsAsync(key, requester, process, GetFriendlyStringForKey);
        }

        protected abstract string GetFriendlyStringForKey(TKey key);
    }
}