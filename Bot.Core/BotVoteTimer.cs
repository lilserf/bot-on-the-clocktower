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

            return await m_voteTimerController.add_town(town.TownRecord, span.Value);
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
                return "";

            return await m_voteTimerController.remove_town(town.TownRecord);
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

        private class VoteTimerController
        {
            private readonly IBotGameplay m_gameplay;
            private readonly ICallbackScheduler<ITownRecord> callback_scheduler;
            private readonly IDateTime datetime_provider;
            private readonly IBotClient town_info_provider;
            private readonly IVoteHandler vote_handler;

            private readonly Dictionary<ITownRecord, DateTime> town_map = new();

            public VoteTimerController(IServiceProvider serviceProvider)
            {
                m_gameplay = serviceProvider.GetService<IBotGameplay>();
                town_info_provider = serviceProvider.GetService<IBotClient>();
                datetime_provider = serviceProvider.GetService<IDateTime>();
                vote_handler = serviceProvider.GetService<IVoteHandler>();

                var callbackFactory = serviceProvider.GetService<ICallbackSchedulerFactory>();
                callback_scheduler = callbackFactory.CreateScheduler<ITownRecord>(town_finished, TimeSpan.FromSeconds(1));
            }

            public async Task<string> add_town(ITownRecord town_id, TimeSpan timeSpan)
            {
                var now = datetime_provider.Now;
                var end_time = now + timeSpan;
                town_map[town_id] = end_time;

                var ret = await send_time_remaining_message(town_id, end_time, now);
                queue_next_time(town_id, end_time, now);

                return ret;
            }

            public async Task<string> remove_town(ITownRecord town_id)
            {
                bool had_town = false;
                if (town_map.ContainsKey(town_id))
                {
                    had_town = true;
                    town_map.Remove(town_id);
                    callback_scheduler.CancelCallback(town_id);
                }

                if (had_town)
                {
                    var town_info = await town_info_provider.ResolveTownAsync(town_id);
                    if (town_info != null && town_info.VillagerRole != null)
                    {
                        var message = $"{town_info.VillagerRole.Mention} - Vote countdown stopped!";
                        return await send_message(town_info, message);
                    }
                }
                return "Could not find town";
            }

            private async Task<string> send_message(ITown town_info, string message)
            {
                if (town_info.ChatChannel != null)
                {
                    try
                    {
                        await town_info.ChatChannel.SendMessageAsync(message);
                        return "";
                    }
                    catch (Exception ex)
                    {
                        return $"Unable to send chat message. Do I have permission to send messages to chat channel `{town_info.ChatChannel.Name}`?\n\n{ex}";
                    }
                }
                return $"Unable to send chat message. Could not find a chat channel.";
            }


            private async Task town_finished(ITownRecord town_id)
            {
                await advance_town(town_id, datetime_provider.Now);
            }

            private async Task advance_town(ITownRecord town_id, DateTime now)
            {
                if (town_map.TryGetValue(town_id, out var end_time))
                {
                    if (end_time <= now)
                    {
                        town_map.Remove(town_id);
                        await send_time_to_vote_message(town_id);
                        await vote_handler.PerformVoteAsync(town_id);
                    }
                    else
                    {
                        await send_time_remaining_message(town_id, end_time, now);
                        queue_next_time(town_id, end_time, now);
                    }
                }
            }

            private static string construct_message(ITown town_info, DateTime end_time, DateTime now)
            {
                var timeSpan = (end_time - now);

                var townSquareName = town_info.TownSquare.Name ?? "Town Square";
                var messagePrefix = $"{GetVillagerRoleMention(town_info.VillagerRole)} - ";
                var messageTime = (timeSpan.TotalSeconds < 3) ? $"Returning to {townSquareName} to vote!" : $"{GetTimeString(timeSpan, true)} remaining until vote!";

                return messagePrefix + messageTime;
            }

            private async Task<string> send_time_remaining_message(ITownRecord town_id, DateTime end_time, DateTime now)
            {
                var town_info = await town_info_provider.ResolveTownAsync(town_id);
                if (town_info == null)
                    return "Could not find town";

                var message = construct_message(town_info, end_time, now);
                return await send_message(town_info, message);
            }

            private static string GetVillagerRoleMention(IRole? villagerRole)
            {
                return villagerRole != null ? $"{villagerRole.Mention}" : "Players";
            }

            private async Task<string> send_time_to_vote_message(ITownRecord town_id)
            {
                var town_info = await town_info_provider.ResolveTownAsync(town_id);
                if (town_info != null)
                    return await send_message(town_info, $"{GetVillagerRoleMention(town_info.VillagerRole)} - Returning to {town_info.TownSquare?.Name ?? "Town Square"} to vote!");
                return "Could not find town";
            }

            private void queue_next_time(ITownRecord town_id, DateTime end_time, DateTime now)
            {
                var next_time = end_time;
                var delta = (end_time - now).TotalSeconds;
                if (delta >= 0)
                {
                    int[] advance_seconds = new[] { 300, 60, 15, 0 };
                    foreach (var second in advance_seconds)
                    {
                        if (delta > second)
                        {
                            next_time = end_time - TimeSpan.FromSeconds(second);
                            break;
                        }
                    }
                }

                callback_scheduler.ScheduleCallback(town_id, next_time);
            }
        }
    }
}
