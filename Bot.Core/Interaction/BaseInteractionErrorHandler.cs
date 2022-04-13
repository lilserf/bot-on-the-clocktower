using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public abstract class BaseInteractionErrorHandler<TKey> where TKey : notnull
    {
        protected abstract string GetFriendlyStringForKey(TKey key);

        public async Task<InteractionResult> TryProcessReportingErrorsAsync(TKey key, IMember requester, Func<IProcessLogger, Task<InteractionResult>> process)
        {
            var logger = new ProcessLogger();
            InteractionResult result = "An error occurred processing this command - please check your private messages for a detailed report.";
            try
            {
                result = await process(logger);
            }
            catch (Exception e)
            {
                await TrySendExceptionToAuthorAsync(key, requester, e, GetFriendlyStringForKey);
            }

            result.AddLogMessages(logger.Messages);

            return result;
        }

        private static Task TrySendExceptionToAuthorAsync(TKey key, IMember requester, Exception e, Func<TKey, string> getFriendlyStringForKey)
        {
            return ExceptionReportingHelper.TrySendExceptionToMemberAsync(getFriendlyStringForKey(key), requester, e);
        }
    }
}