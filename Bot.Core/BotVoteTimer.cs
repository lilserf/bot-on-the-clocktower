using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Callbacks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotVoteTimer : BotTownLookupHelper
    {
        private readonly VoteTimerController m_voteTimerController;

        public BotVoteTimer(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            m_voteTimerController = new(serviceProvider);
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

            var ret = await m_voteTimerController.AddTownAsync(TownKey.FromTown(town), span.Value);

            if (!string.IsNullOrWhiteSpace(ret))
                return ret;

            return $"Vote timer started for {GetTimeString(span.Value, false)}!";
        }

        public async Task<string> RunStopVoteTimerUnsafe(IBotInteractionContext context, IProcessLogger processLoggger)
        {
            var town = await GetValidTownOrLogErrorAsync(context, processLoggger);
            if (town == null)
                return "Failed to run command";

            return await m_voteTimerController.RemoveTownAsync(TownKey.FromTown(town));
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

            private readonly ICallbackScheduler<TownKey> m_callbackScheduler;
            private readonly IBotClient m_client;
            private readonly ITownDatabase m_townLookup;
            private readonly IVoteHandler m_voteHandler;

            private readonly Dictionary<TownKey, DateTime> m_townKeyToVoteTime = new();

            public VoteTimerController(IServiceProvider serviceProvider)
            {
                serviceProvider.Inject(out DateTime);

                serviceProvider.Inject(out m_client);
                serviceProvider.Inject(out m_townLookup);
                serviceProvider.Inject(out m_voteHandler);

                var callbackFactory = serviceProvider.GetService<ICallbackSchedulerFactory>();
                m_callbackScheduler = callbackFactory.CreateScheduler<TownKey>(ScheduledCallbackAsync, TimeSpan.FromSeconds(1));
            }

            public async Task<string> AddTownAsync(TownKey townKey, TimeSpan timeSpan)
            {
                var now = DateTime.Now;
                var endTime = now + timeSpan;
                m_townKeyToVoteTime[townKey] = endTime;

                ScheduleNextProcessTime(townKey, endTime, now);
                return await SendTimeRemainingMessageAsync(townKey, endTime, now);
            }

            public async Task<string> RemoveTownAsync(TownKey townKey)
            {
                bool hadTown = false;
                if (m_townKeyToVoteTime.ContainsKey(townKey))
                {
                    hadTown = true;
                    m_townKeyToVoteTime.Remove(townKey);
                    m_callbackScheduler.CancelCallback(townKey);
                }

                if (hadTown)
                {
                    var townRecord = await m_townLookup.GetTownRecord(townKey);
                    if (townRecord != null)
                    {
                        var town = await m_client.ResolveTownAsync(townRecord);
                        if (town != null && town.VillagerRole != null)
                        {
                            var message = $"{town.VillagerRole.Mention} - Vote countdown stopped!";
                            return await SendMessageAsync(town, message, "Vote countdown stopped");
                        }
                    }
                }
                return "Could not find town";
            }

            private static async Task<string> SendMessageAsync(ITown town, string message, string successfulReturn)
            {
                if (town.ChatChannel != null)
                {
                    try
                    {
                        await town.ChatChannel.SendMessageAsync(message);
                        return successfulReturn;
                    }
                    catch (Exception ex)
                    {
                        return $"Unable to send chat message. Do I have permission to send messages to chat channel `{town.ChatChannel.Name}`?\n\n{ex}";
                    }
                }
                return $"Unable to send chat message. Could not find a chat channel.";
            }


            private Task ScheduledCallbackAsync(TownKey townKey)
            {
                return ProcessTownLogicForTimeAsync(townKey, DateTime.Now);
            }

            private async Task ProcessTownLogicForTimeAsync(TownKey townKey, DateTime now)
            {
                if (m_townKeyToVoteTime.TryGetValue(townKey, out var endTime))
                {
                    if (endTime <= now)
                    {
                        m_townKeyToVoteTime.Remove(townKey);
                        await SendTimeToVoteMessageAsync(townKey);
                        await m_voteHandler.PerformVoteAsync(townKey);
                    }
                    else
                    {
                        ScheduleNextProcessTime(townKey, endTime, now);
                        await SendTimeRemainingMessageAsync(townKey, endTime, now);
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

            private async Task<string> SendTimeRemainingMessageAsync(TownKey townKey, DateTime endTime, DateTime now)
            {
                var townRecord = await m_townLookup.GetTownRecord(townKey);
                if (townRecord == null)
                    return "Could not find town";

                var town = await m_client.ResolveTownAsync(townRecord);
                if (town == null)
                    return "Could not find town";

                var message = ConstructMessage(town, endTime, now);
                return await SendMessageAsync(town, message, "Notified town of remaining time");
            }

            private static string GetVillagerRoleMention(IRole? villagerRole)
            {
                return villagerRole != null ? $"{villagerRole.Mention}" : "Players";
            }

            private async Task<string> SendTimeToVoteMessageAsync(TownKey townKey)
            {
                var townRecord = await m_townLookup.GetTownRecord(townKey);
                if (townRecord == null)
                    return "Could not find town";

                var town = await m_client.ResolveTownAsync(townRecord);
                if (town == null)
                    return "Could not find town";

                var townSquareName = town.TownSquare?.Name ?? "Town Square";
                return await SendMessageAsync(town, $"{GetVillagerRoleMention(town.VillagerRole)} - Returning to {townSquareName} to vote!", $"Notified town of returning to {townSquareName}");
            }

            private void ScheduleNextProcessTime(TownKey townKey, DateTime endTime, DateTime now)
            {
                var next_time = endTime;
                var delta = (endTime - now).TotalSeconds;
                if (delta >= 0)
                {
                    int[] advance_seconds = new[] { 240, 120, 60, 15, 0 };
                    foreach (var second in advance_seconds)
                    {
                        if (delta > second)
                        {
                            next_time = endTime - TimeSpan.FromSeconds(second);
                            break;
                        }
                    }
                }

                m_callbackScheduler.ScheduleCallback(townKey, next_time);
            }
        }
    }
}
