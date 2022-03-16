using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameplayInteractionHandler : IBotGameplayInteractionHandler
    {
        private enum GameplayButton
        {
            Night,
            Day,
            Vote,
            More,
            EndGame,
        }

        private readonly IBotSystem m_system;
        private readonly IComponentService m_componentService;
        private readonly ITownCommandQueue m_townCommandQueue;

        private readonly BotGameplay m_gameplay;
        private readonly BotVoteTimer m_voteTimer;

        private readonly IBotComponent m_nightButton;
        private readonly IBotComponent m_dayButton;
        private readonly IBotComponent m_voteButton;
        private readonly IBotComponent m_moreButton;
        private readonly IBotComponent m_endGameButton;

        private readonly IBotComponent m_voteTimerMenu;

        const string CommandLogMsg = "/{command} command on guild {@guild} by user {@user}";
        const string ButtonLogMsg = "[{button}] button pressed on guild {@guild} by user {@user}";

        public BotGameplayInteractionHandler(IServiceProvider serviceProvider, BotGameplay gameplay, BotVoteTimer voteTimer)
        {
            m_gameplay = gameplay;
            m_voteTimer = voteTimer;

            serviceProvider.Inject(out m_system);
            serviceProvider.Inject(out m_componentService);
            serviceProvider.Inject(out m_townCommandQueue);

            m_nightButton = CreateButton(GameplayButton.Night, "Night", pressMethod: NightButtonPressed, emoji: "🌙");
            m_dayButton = CreateButton(GameplayButton.Day, "Day", pressMethod: DayButtonPressed, IBotSystem.ButtonType.Success, emoji: "☀️");
            m_voteButton = CreateButton(GameplayButton.Vote, "Vote", pressMethod: VoteButtonPressed, IBotSystem.ButtonType.Danger, emoji: "💀");
            m_moreButton = CreateButton(GameplayButton.More, "More", pressMethod: MoreButtonPressed, IBotSystem.ButtonType.Secondary, emoji: "⚙️");
            m_endGameButton = CreateButton(GameplayButton.EndGame, "End Game", pressMethod: EndGameButtonPressed, IBotSystem.ButtonType.Secondary, emoji: "🛑");

            var options = new[]
            {
                new IBotSystem.SelectMenuOption("30 sec", "30s"),
                new IBotSystem.SelectMenuOption("1 min", "1m"),
                new IBotSystem.SelectMenuOption("1 min 30 sec", "1m30s"),
                new IBotSystem.SelectMenuOption("2 min", "2m"),
                new IBotSystem.SelectMenuOption("2 min 30 sec", "2m30s"),
                new IBotSystem.SelectMenuOption("3 min", "3m"),
                new IBotSystem.SelectMenuOption("3 min 30 sec", "3m30s"),
                new IBotSystem.SelectMenuOption("4 min", "4m"),
                new IBotSystem.SelectMenuOption("4 min 30 sec", "4m30s"),
                new IBotSystem.SelectMenuOption("5 min", "5m"),
                new IBotSystem.SelectMenuOption("6 min", "6m"),
                new IBotSystem.SelectMenuOption("7 min", "7m"),
                new IBotSystem.SelectMenuOption("8 min", "8m"),
                new IBotSystem.SelectMenuOption("9 min", "9m"),
                new IBotSystem.SelectMenuOption("10 min", "10m"),
            };
            m_voteTimerMenu = m_system.CreateSelectMenu($"gameplay_menu_votetimer", "Or instead, set a Vote Timer...", options);

            m_componentService.RegisterComponent(m_voteTimerMenu, VoteTimerMenuSelected);
        }

        private IBotComponent CreateButton(GameplayButton id, string label, Func<IBotInteractionContext, Task> pressMethod, IBotSystem.ButtonType type = IBotSystem.ButtonType.Primary, string? emoji = null)
        {
            var button = m_system.CreateButton($"gameplay_{id}", label, type, emoji: emoji);
            m_componentService.RegisterComponent(button, pressMethod);
            return button;
        }

        // Helper for editing the original interaction with a summarizing message when finished
        // TODO: move within IBotInteractionContext
        protected async Task EditOriginalMessage(IBotInteractionContext context, string s)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(s))
                {
                    var webhook = m_system.CreateWebhookBuilder().WithContent(s);
                    await context.EditResponseAsync(webhook);
                }
            }
            catch (Exception)
            { }
        }

        #region Callbacks from system with interaction context
        public async Task<string> PhaseNightInternal(TownKey townKey, IMember requester)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(townKey, requester, async (processLog) =>
            {
                var game = await m_gameplay.CurrentGameAsync(townKey, requester, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here? Could make use of use processLog!
                    return "Couldn't find an active game record for this town!";
                }
                return await m_gameplay.PhaseNightUnsafe(game, processLog);
            });
        }

        private async Task<string> PhaseDayInternal(TownKey townKey, IMember requester)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(townKey, requester, async (processLog) =>
            {
                var game = await m_gameplay.CurrentGameAsync(townKey, requester, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here? Could make use of use processLog!
                    return "Couldn't find an active game record for this town!";
                }
                return await m_gameplay.PhaseDayUnsafe(game, processLog);
            });
        }

        private async Task<string> PhaseVoteInternal(TownKey townKey, IMember requester)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(townKey, requester, async (processLog) =>
            {
                var game = await m_gameplay.CurrentGameAsync(townKey, requester, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here?
                    return "Couldn't find an active game record for this town!";
                }
                return await m_gameplay.PhaseVoteUnsafe(game, processLog);
            });
        }

        private async Task<(string, bool)> PerformGameInternal(TownKey townKey, IMember requester)
        {
            bool success = true;
            var message = await InteractionWrapper.TryProcessReportingErrorsAsync(townKey, requester, async (processLog) =>
            {
                var game = await m_gameplay.CurrentGameAsync(townKey, requester, processLog);
                success = game != null;
                return success ? "Welcome to Blood on the Clocktower!" : "Couldn't find an active game record for this town!";
            });
            return (message, success);
        }

        private async Task<string> EndGameInternal(TownKey townKey, IMember requester)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(townKey, requester, async (processLog) =>
            {
                var game = await m_gameplay.CurrentGameAsync(townKey, requester, processLog);
                if (game == null)
                {
                    return "Couldn't find a current game to end!";
                }
                return await m_gameplay.EndGameUnsafe(game.TownKey, processLog);
            });
        }

        public async Task<string> SetStorytellersInternal(TownKey townKey, IMember requester, IEnumerable<IMember> users)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(townKey, requester, (processLog) => m_gameplay.SetStorytellersUnsafe(townKey, requester, users, processLog));
        }

        public async Task<string> RunVoteTimerInternal(TownKey townKey, IMember requester, string timeString)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(townKey, requester, processLoggger => m_voteTimer.RunVoteTimerUnsafe(townKey, timeString, processLoggger));
        }

        public async Task<string> RunStopVoteTimerInternal(TownKey townKey, IMember requester)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(townKey, requester, processLoggger => m_voteTimer.RunStopVoteTimerUnsafe(townKey, processLoggger));
        }
        #endregion

        #region Command handlers
        public Task CommandGameAsync(IBotInteractionContext context)
        {
            return m_townCommandQueue.QueueCommandAsync("Starting game...", context, async () =>
            {
                Serilog.Log.Information(CommandLogMsg, "game", context.Guild, context.Member);
                (var message, bool success) = await PerformGameInternal(context.GetTownKey(), context.Member);
                return new QueuedCommandResult(message, success, new[] { m_nightButton, m_dayButton, m_voteButton, m_endGameButton }, new[] { m_voteTimerMenu });
            });
        }

        public Task CommandNightAsync(IBotInteractionContext context) => QueueNightCommandAsync(context, false);
        public Task NightButtonPressed(IBotInteractionContext context) => QueueNightCommandAsync(context, true);

        private Task QueueNightCommandAsync(IBotInteractionContext context, bool includeComponents)
        {
            return m_townCommandQueue.QueueCommandAsync("Sending players to nighttime...", context, async () =>
            {
                Serilog.Log.Information(ButtonLogMsg, "Night", context.Guild, context.Member);
                var message = await PhaseNightInternal(context.GetTownKey(), context.Member);
                return new QueuedCommandResult(message, includeComponents, new[] { m_dayButton, m_moreButton });
            });
        }

        public Task CommandDayAsync(IBotInteractionContext context) => QueueDayCommandAsync(context, false);
        public Task DayButtonPressed(IBotInteractionContext context) => QueueDayCommandAsync(context, true);
        private Task QueueDayCommandAsync(IBotInteractionContext context, bool includeComponents)
        {
            return m_townCommandQueue.QueueCommandAsync("Sending players to daytime...", context, async () =>
            {
                Serilog.Log.Information(ButtonLogMsg, "Day", context.Guild, context.Member);
                var message = await PhaseDayInternal(context.GetTownKey(), context.Member);
                return new QueuedCommandResult(message, includeComponents, new[] { m_voteButton, m_endGameButton, m_moreButton }, new[] { m_voteTimerMenu });
            });
        }

        public Task CommandVoteAsync(IBotInteractionContext context) => QueueVoteCommandAsync(context, false);
        public Task VoteButtonPressed(IBotInteractionContext context) => QueueVoteCommandAsync(context, true);
        private Task QueueVoteCommandAsync(IBotInteractionContext context, bool includeComponents)
        {
            return m_townCommandQueue.QueueCommandAsync("Calling players for a vote...", context, async () =>
            {
                Serilog.Log.Information(ButtonLogMsg, "Vote", context.Guild, context.Member);
                var message = await PhaseVoteInternal(context.GetTownKey(), context.Member);
                return new QueuedCommandResult(message, includeComponents, new[] { m_nightButton, m_endGameButton, m_moreButton });
            });
        }

        public Task MoreButtonPressed(IBotInteractionContext context)
        {
            return m_townCommandQueue.QueueCommandAsync("Expanding options...", context, () =>
            {
                Serilog.Log.Information(ButtonLogMsg, "More", context.Guild, context.Member);
                var message = "Here are all the options again!";
                return Task.FromResult(new QueuedCommandResult(message, true, new[] { m_nightButton, m_dayButton, m_voteButton, m_endGameButton }, new[] { m_voteTimerMenu  }));
            });
        }

        public Task CommandEndGameAsync(IBotInteractionContext context) => QueueEndGameCommandAsync(context);
        public Task EndGameButtonPressed(IBotInteractionContext context) => QueueEndGameCommandAsync(context);
        private Task QueueEndGameCommandAsync(IBotInteractionContext context)
        {
            return m_townCommandQueue.QueueCommandAsync("Ending the game...", context, async () =>
            {
                Serilog.Log.Information(ButtonLogMsg, "End Game", context.Guild, context.Member);
                var message = await EndGameInternal(context.GetTownKey(), context.Member);
                return new QueuedCommandResult(message);
            });
        }

        public Task RunVoteTimerAsync(IBotInteractionContext context, string timeString) => QueueVoteTimerCommandAsync(context, timeString, false);
        public Task VoteTimerMenuSelected(IBotInteractionContext context) => QueueVoteTimerCommandAsync(context, context.ComponentValues.First(), true);
        private Task QueueVoteTimerCommandAsync(IBotInteractionContext context, string timeString, bool includeComponents)
        {
            return m_townCommandQueue.QueueCommandAsync("Setting a timer before a vote happens...", context, async () =>
            {
                Serilog.Log.Information("[{button}] Menu selected on guild {@guild} by user {@user}: {value}", "Vote Timer", context.Guild, context.Member, timeString);
                var message = await RunVoteTimerInternal(context.GetTownKey(), context.Member, timeString);
                return new QueuedCommandResult(message, includeComponents, new[] { m_nightButton, m_moreButton }, new[] { m_voteTimerMenu });
            });
        }

        public Task CommandSetStorytellersAsync(IBotInteractionContext context, IEnumerable<IMember> users)
        {
            return m_townCommandQueue.QueueCommandAsync("Settings storytellers...", context, async () =>
            {
                Serilog.Log.Information(CommandLogMsg + ": {users}", "storytellers", context.Guild, context.Member, users);
                var message = await SetStorytellersInternal(context.GetTownKey(), context.Member, users);
                return new QueuedCommandResult(message);
            });
        }

        public Task RunStopVoteTimerAsync(IBotInteractionContext context)
        {
            return m_townCommandQueue.QueueCommandAsync("Stopping vote timer...", context, async () =>
            {
                Serilog.Log.Information(CommandLogMsg, "stopVoteTimer", context.Guild, context.Member);
                var message = await RunStopVoteTimerInternal(context.GetTownKey(), context.Member);
                return new QueuedCommandResult(message);
            });
        }
        #endregion
    }
}
