using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public static class ExceptionReportingHelper
    {
        public static async Task TrySendExceptionToMemberAsync(string identifier, IMember member, Exception e)
        {
            try
            {
                await member.SendMessageAsync($"Bot on the Clocktower encountered an error.\n\nPlease consider reporting the error at <https://github.com/lilserf/bot-on-the-clocktower/issues> and including all the information below:\n\n{identifier}\nException: `{e.GetType().Name}`\nMessage:   `{e.Message}`\nStack trace:\n```{e.StackTrace}```");
            }
            catch (Exception)
            {
                // Not a whole lot we can do about this. Maybe log it?
            }
        }
    }
}
