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

        private static async Task TrySendExceptionToAuthorAsync(TKey key, IMember requester, Exception e, Func<TKey, string> getFriendlyStringForKey)
        {
            try
            {
                await requester.SendMessageAsync($"Bot on the Clocktower encountered an error.\n\nPlease consider reporting the error at <https://github.com/lilserf/bot-on-the-clocktower/issues> and including all the information below:\n\n{getFriendlyStringForKey(key)}\nException: `{e.GetType().Name}`\nMessage:   `{e.Message}`\nStack trace:\n```{e.StackTrace}```");
            }
            catch (Exception)
            { }
        }
    }
}