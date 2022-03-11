using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public static class InteractionWrapper
    {
        public static async Task<string> TryProcessReportingErrorsAsync(TownKey townKey, IMember requester, Func<IProcessLogger, Task<string>> process)
        {
            var logger = new ProcessLogger();
            var msg = "An error occurred processing this command - please check your private messages for a detailed report.";
            try
            {
                msg = await process(logger);
            }
            catch (Exception e)
            {
                await TrySendExceptionToAuthorAsync(townKey, requester, e);
            }

            if(logger.HasMessages)
			{
                msg = string.Join("\n", logger.Messages) + "\n" + msg;
			}

            return msg;
        }

        private static async Task TrySendExceptionToAuthorAsync(TownKey townKey, IMember requester, Exception e)
        {
            try
            {
                await requester.SendMessageAsync($"Bot on the Clocktower encountered an error.\n\nPlease consider reporting the error at <https://github.com/lilserf/bot-on-the-clocktower/issues> and including all the information below:\n\nGuild: `{townKey.GuildId}`\nChannel: `{townKey.ControlChannelId}`\nException: `{e.GetType().Name}`\nMessage:   `{e.Message}`\nStack trace:\n```{e.StackTrace}```");
            }
            catch (Exception)
            { }
        }
    }
}
