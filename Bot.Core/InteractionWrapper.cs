using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public static class InteractionWrapper
    {
        public static async Task TryProcessReportingErrorsAsync(IBotInteractionContext context, Func<IProcessLogger, Task> process)
        {
            var logger = new ProcessLogger();
            try
            {
                await process(logger);
            }
            catch (Exception e)
            {
                await TrySendMessageToAuthorAsync(context, e);
            }
        }

        private static async Task TrySendMessageToAuthorAsync(IBotInteractionContext context, Exception e)
        {
            try
            {
                await context.Member.SendMessageAsync($"Bot on the Clocktower encountered an error.\n\nPlease consider reporting the error at https://github.com/lilserf/bot-on-the-clocktower/issues\n\nException: `{e.GetType().Name}`\nMessage:   `{e.Message}`\nStack trace:\n```{e.StackTrace}```");
            }
            catch (Exception)
            { }
        }
    }
}
