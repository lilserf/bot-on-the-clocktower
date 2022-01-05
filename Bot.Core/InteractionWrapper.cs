using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Core
{
    public static class InteractionWrapper
    {
        public static async Task<string> TryProcessReportingErrorsAsync(IBotInteractionContext context, Func<IProcessLogger, Task<string>> process)
        {
            var logger = new ProcessLogger();
            var msg = "Unknown error occurred.";
            try
            {
                msg = await process(logger);
            }
            catch (Exception e)
            {
                await TrySendExceptionToAuthorAsync(context, e);
            }

            if(logger.HasMessages)
			{
                //await TrySendMessagesToChannelAsync(context, logger.Messages);
                msg = string.Join("\n", logger.Messages) + "\n" + msg;
			}

            return msg;
        }

        // TODO: edit the interaction response instead?
        private static async Task TrySendMessagesToChannelAsync(IBotInteractionContext context, IReadOnlyCollection<string> messages)
		{
            try
			{
                string fullMsg = string.Join("\n", messages);
                await context.Channel.SendMessageAsync(fullMsg);
			}
            catch(Exception)
            { }
		}

        private static async Task TrySendExceptionToAuthorAsync(IBotInteractionContext context, Exception e)
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
