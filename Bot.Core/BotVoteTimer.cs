using Bot.Api;
using Bot.Core.Callbacks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotVoteTimer : BotCommandHandler, IBotVoteTimer
    {
        private readonly VoteTimerController m_voteTimerController;

        public BotVoteTimer(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            m_voteTimerController = new(serviceProvider);
        }

        public async Task RunVoteTimerAsync(IBotInteractionContext context, string timeString)
        {
            await context.DeferInteractionResponse();

            var message = await RunVoteTimerInternal(context, timeString);

            await EditOriginalMessage(context, message);
        }

        public async Task<string> RunVoteTimerInternal(IBotInteractionContext context, string timeString)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, processLoggger => RunVoteTimerUnsafe(context, timeString, processLoggger));
        }

        public async Task<string> RunVoteTimerUnsafe(IBotInteractionContext context, string timeString, IProcessLogger processLoggger)
        {
            var town = await GetValidTownOrLogErrorAsync(context, processLoggger);
            if (town == null)
                return "Failed to run command";

            if (town.ChatChannel == null)
                return LogAndReturnEmptyString(processLoggger, "No chat channel found for this town. Please set the chat channel via the `/setChatChannel` command.");

            if (town.VillagerRole == null)
                return LogAndReturnEmptyString(processLoggger, "No villager role found for this town. Please set up the town properly via the `/addTown` command.");

            TimeSpan? span = TimeSpanParser.Parse(timeString);

            if (!span.HasValue)
                return LogAndReturnEmptyString(processLoggger, "Please enter a vote time in a format like \"5m30s\" or \"2 minutes\" or similar.");

            if (span.Value.TotalSeconds < 10 || span.Value.TotalMinutes > 20)
                return LogAndReturnEmptyString(processLoggger, $"Please choose a time between 10 seconds and 20 minutes. You requested: {GetTimeString(span.Value, false)}");

            var ret = await m_voteTimerController.AddTownAsync(town.TownRecord, span.Value);

            if (!string.IsNullOrWhiteSpace(ret))
                return ret;

            return $"Vote timer started for {GetTimeString(span.Value, false)}!";
        }

        public async Task RunStopVoteTimerAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await RunStopVoteTimerInternal(context);

            await EditOriginalMessage(context, message);
        }

        public async Task<string> RunStopVoteTimerInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, processLoggger => RunStopVoteTimerUnsafe(context, processLoggger));
        }

        public async Task<string> RunStopVoteTimerUnsafe(IBotInteractionContext context, IProcessLogger processLoggger)
        {
            var town = await GetValidTownOrLogErrorAsync(context, processLoggger);
            if (town == null)
                return "Failed to run command";

            return await m_voteTimerController.RemoveTownAsync(town.TownRecord);
        }

        private static string LogAndReturnEmptyString(IProcessLogger processLoggger, string logString)
        {
            processLoggger.LogMessage(logString);
            return "Failed to run command";
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

        private class VoteTimerController
        {
            private readonly IDateTime DateTime;

            private readonly ICallbackScheduler<ITownRecord> m_callbackScheduler;
            private readonly IBotClient m_client;
            private readonly IVoteHandler m_voteHandler;

            private readonly Dictionary<ITownRecord, DateTime> m_townToVoteTime = new();

            public VoteTimerController(IServiceProvider serviceProvider)
            {
                DateTime = serviceProvider.GetService<IDateTime>();

                m_client = serviceProvider.GetService<IBotClient>();
                m_voteHandler = serviceProvider.GetService<IVoteHandler>();

                var callbackFactory = serviceProvider.GetService<ICallbackSchedulerFactory>();
                m_callbackScheduler = callbackFactory.CreateScheduler<ITownRecord>(ScheduledCallbackAsync, TimeSpan.FromSeconds(1));
            }

            public async Task<string> AddTownAsync(ITownRecord townRecord, TimeSpan timeSpan)
            {
                var now = DateTime.Now;
                var endTime = now + timeSpan;
                m_townToVoteTime[townRecord] = endTime;

                ScheduleNextProcessTime(townRecord, endTime, now);
                return await SendTimeRemainingMessageAsync(townRecord, endTime, now);
            }

            public async Task<string> RemoveTownAsync(ITownRecord townRecord)
            {
                bool hadTown = false;
                if (m_townToVoteTime.ContainsKey(townRecord))
                {
                    hadTown = true;
                    m_townToVoteTime.Remove(townRecord);
                    m_callbackScheduler.CancelCallback(townRecord);
                }

                if (hadTown)
                {
                    var town = await m_client.ResolveTownAsync(townRecord);
                    if (town != null && town.VillagerRole != null)
                    {
                        var message = $"{town.VillagerRole.Mention} - Vote countdown stopped!";
                        return await SendMessageAsync(town, message);
                    }
                }
                return "Could not find town";
            }

            private static async Task<string> SendMessageAsync(ITown town, string message)
            {
                if (town.ChatChannel != null)
                {
                    try
                    {
                        await town.ChatChannel.SendMessageAsync(message);
                        return "";
                    }
                    catch (Exception ex)
                    {
                        return $"Unable to send chat message. Do I have permission to send messages to chat channel `{town.ChatChannel.Name}`?\n\n{ex}";
                    }
                }
                return $"Unable to send chat message. Could not find a chat channel.";
            }


            private Task ScheduledCallbackAsync(ITownRecord townRecord)
            {
                return ProcessTownLogicForTimeAsync(townRecord, DateTime.Now);
            }

            private async Task ProcessTownLogicForTimeAsync(ITownRecord townRecord, DateTime now)
            {
                if (m_townToVoteTime.TryGetValue(townRecord, out var endTime))
                {
                    if (endTime <= now)
                    {
                        m_townToVoteTime.Remove(townRecord);
                        await SendTimeToVoteMessageAsync(townRecord);
                        await m_voteHandler.PerformVoteAsync(townRecord);
                    }
                    else
                    {
                        ScheduleNextProcessTime(townRecord, endTime, now);
                        await SendTimeRemainingMessageAsync(townRecord, endTime, now);
                    }
                }
            }

            private static string ConstructMessage(ITown town, DateTime endTime, DateTime now)
            {
                var timeSpan = (endTime - now);

                var townSquareName = town.TownSquare?.Name ?? "Town Square";
                var messagePrefix = $"{GetVillagerRoleMention(town.VillagerRole)} - ";
                var messageTime = (timeSpan.TotalSeconds < 3) ? $"Returning to {townSquareName} to vote!" : $"{GetTimeString(timeSpan, true)} remaining until vote!";

                return messagePrefix + messageTime;
            }

            private async Task<string> SendTimeRemainingMessageAsync(ITownRecord townRecord, DateTime endTime, DateTime now)
            {
                var town = await m_client.ResolveTownAsync(townRecord);
                if (town == null)
                    return "Could not find town";

                var message = ConstructMessage(town, endTime, now);
                return await SendMessageAsync(town, message);
            }

            private static string GetVillagerRoleMention(IRole? villagerRole)
            {
                return villagerRole != null ? $"{villagerRole.Mention}" : "Players";
            }

            private async Task<string> SendTimeToVoteMessageAsync(ITownRecord townRecord)
            {
                var town = await m_client.ResolveTownAsync(townRecord);
                if (town != null)
                    return await SendMessageAsync(town, $"{GetVillagerRoleMention(town.VillagerRole)} - Returning to {town.TownSquare?.Name ?? "Town Square"} to vote!");
                return "Could not find town";
            }

            private void ScheduleNextProcessTime(ITownRecord townRecord, DateTime endTime, DateTime now)
            {
                var next_time = endTime;
                var delta = (endTime - now).TotalSeconds;
                if (delta >= 0)
                {
                    int[] advance_seconds = new[] { 300, 60, 15, 0 };
                    foreach (var second in advance_seconds)
                    {
                        if (delta > second)
                        {
                            next_time = endTime - TimeSpan.FromSeconds(second);
                            break;
                        }
                    }
                }

                m_callbackScheduler.ScheduleCallback(townRecord, next_time);
            }
        }
    }
}
