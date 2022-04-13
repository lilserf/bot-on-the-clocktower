using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public static class ExceptionReportingHelper
    {
        private const int MaximumMessageLength = 2000; // Hardcoded in DSharpPlus
        public static async Task TrySendExceptionToMemberAsync(string identifier, IMember member, Exception e)
        {
            try
            {
                string messageMinusStackTrace = $"Bot on the Clocktower encountered an error.\n\nPlease consider reporting the error at <https://github.com/lilserf/bot-on-the-clocktower/issues> and including all the information below:\n\n{identifier}\nException: `{e.GetType().Name}`\nMessage:   `{e.Message}`\nStack trace:\n";
                int availableForStackTrace = MaximumMessageLength - messageMinusStackTrace.Length - 6;
                string fullMessage = $"{messageMinusStackTrace}{(e.StackTrace == null ? "n/a" : availableForStackTrace > 0 ? $"```{GetRangeOrFull(e.StackTrace!, availableForStackTrace)}```" : "(2long)")}";
                await member.SendMessageAsync(GetRangeOrFull(fullMessage, MaximumMessageLength));
            }
            catch (Exception)
            {
                // Not a whole lot we can do about this. Maybe log it?
            }

            string GetRangeOrFull(string str, int max)
            {
                return max < str.Length ? str[..max] : str;
            }
        }
    }
}
