using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public static class InteractionWrapper
    {
        [Obsolete("Please use IGuildInteractionErrorHandler.TryProcessReportingErrorsAsync instead")]
        public static Task<string> TryProcessReportingErrorsAsync(TownKey townKey, IMember requester, Func<IProcessLogger, Task<string>> process)
        {
            return TryProcessReportingErrorsAsync(townKey, requester, process, GetFriendlyStringForTownKey);
        }

        public static async Task<string> TryProcessReportingErrorsAsync<TKey>(TKey key, IMember requester, Func<IProcessLogger, Task<string>> process, Func<TKey, string> getFriendlyStringForKey)
        {
            var logger = new ProcessLogger();
            var msg = "An error occurred processing this command - please check your private messages for a detailed report.";
            try
            {
                msg = await process(logger);
            }
            catch (Exception e)
            {
                await TrySendExceptionToAuthorAsync(key, requester, e, getFriendlyStringForKey);
            }

            if(logger.HasMessages)
			{
                msg = string.Join("\n", logger.Messages) + "\n" + msg;
			}

            return msg;
        }

        public static string GetFriendlyStringForTownKey(TownKey townKey) => $"Guild: `{townKey.GuildId}`\nChannel: `{townKey.ControlChannelId}`";

        private static async Task TrySendExceptionToAuthorAsync<TKey>(TKey key, IMember requester, Exception e, Func<TKey, string> getFriendlyStringForKey)
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
