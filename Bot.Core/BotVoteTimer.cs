using Bot.Api;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotVoteTimer : BotCommandHandler, IBotVoteTimer
    {
        private readonly IBotGameplay m_gameplay;
        private readonly Dictionary<ITownRecord, DateTime> m_townToVoteTime = new();

        public BotVoteTimer(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            m_gameplay = serviceProvider.GetService<IBotGameplay>();
        }

        public async Task RunVoteTimerAsync(IBotInteractionContext context, string timeString)
        {
            await context.DeferInteractionResponse();

            var message = await RunVoteTimerInternal(context, timeString);

            await EditOriginalMessage(context, message); // TODO: Make use of process log... somehow.
        }

        public async Task<string> RunVoteTimerInternal(IBotInteractionContext context, string timeString)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, processLoggger => RunVoteTimerUnsafe(context, timeString, processLoggger));
        }

        public async Task<string> RunVoteTimerUnsafe(IBotInteractionContext context, string timeString, IProcessLogger processLoggger)
        {
            var town = await GetValidTownOrLogErrorAsync(context, processLoggger);
            if (town == null)
                return "";

            if (town.ChatChannel == null)
                return LogAndReturnEmptyString(processLoggger, "No chat channel found for this town. Please set the chat channel via the `/setChatChannel` command.");

            if (town.VillagerRole == null)
                return LogAndReturnEmptyString(processLoggger, "No villager role found for this town. Please set up the town properly via the `/addTown` command.");

            TimeSpan? span = TimeSpanParser.Parse(timeString);

            if (!span.HasValue)
                return LogAndReturnEmptyString(processLoggger, "Please enter a vote time in a format like \"5m30s\" or \"2 minutes\" or similar.");

            if (span.Value.TotalSeconds < 10 || span.Value.TotalMinutes > 20)
                return LogAndReturnEmptyString(processLoggger, $"Please choose a time between 10 seconds and 20 minutes. You requested: {GetTimeString(span.Value, false)}");

            throw new NotImplementedException();
        }

        private static string LogAndReturnEmptyString(IProcessLogger processLoggger, string logString)
        {
            processLoggger.LogMessage(logString);
            return "";
        }

        private static string GetTimeString(TimeSpan timeSpan, bool round)
        {
            var totalSeconds = timeSpan.TotalSeconds;
            var roundedSeconds = round ? Math.Round(totalSeconds / 5) * 5 : totalSeconds;

            StringBuilder message = new();

            var minutes = Math.Floor(roundedSeconds / 60);
            var seconds = roundedSeconds % 60;

            if (minutes > 0)
            {
                message.Append($"{minutes} minute");
                if (minutes > 1)
                    message.Append('s');
                if (seconds > 0)
                    message.Append(", ");
            }

            if (seconds > 0 || minutes == 0)
                message.Append($"{seconds} seconds");

            return message.ToString();
        }

        //IRole villagerRole
        //// string message = $"{villagerRole.Mention} - "
    }
}
