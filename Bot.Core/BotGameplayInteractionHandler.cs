﻿using Bot.Api;
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

        private readonly BotGameplay m_gameplay;
        private readonly BotVoteTimer m_voteTimer;

        private readonly IBotComponent m_nightButton;
        private readonly IBotComponent m_dayButton;
        private readonly IBotComponent m_voteButton;
        private readonly IBotComponent m_moreButton;
        private readonly IBotComponent m_endGameButton;

        private readonly IBotComponent m_voteTimerMenu;

        public BotGameplayInteractionHandler(IServiceProvider serviceProvider, BotGameplay gameplay, BotVoteTimer voteTimer)
        {
            m_gameplay = gameplay;
            m_voteTimer = voteTimer;

            m_system = serviceProvider.GetService<IBotSystem>();
            var componentService = serviceProvider.GetService<IComponentService>();

            m_nightButton = CreateButton(GameplayButton.Night, "Night", emoji: "🌙");
            m_dayButton = CreateButton(GameplayButton.Day, "Day", IBotSystem.ButtonType.Success, emoji: "☀️");
            m_voteButton = CreateButton(GameplayButton.Vote, "Vote", IBotSystem.ButtonType.Danger, emoji: "💀");
            m_moreButton = CreateButton(GameplayButton.More, "More", IBotSystem.ButtonType.Secondary, emoji: "⚙️");
            m_endGameButton = CreateButton(GameplayButton.EndGame, "End Game", IBotSystem.ButtonType.Secondary, emoji: "🛑");

            componentService.RegisterComponent(m_nightButton, NightButtonPressed);
            componentService.RegisterComponent(m_dayButton, DayButtonPressed);
            componentService.RegisterComponent(m_voteButton, VoteButtonPressed);
            componentService.RegisterComponent(m_moreButton, MoreButtonPressed);
            componentService.RegisterComponent(m_endGameButton, EndGameButtonPressed);

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
                new IBotSystem.SelectMenuOption("5 min 30 sec", "5m30s"),
                new IBotSystem.SelectMenuOption("6 min", "6m"),
                new IBotSystem.SelectMenuOption("6 min 30 sec", "6m30s"),
                new IBotSystem.SelectMenuOption("7 min", "7m"),
                new IBotSystem.SelectMenuOption("7 min 30 sec", "7m30s"),
                new IBotSystem.SelectMenuOption("8 min", "8m"),
                new IBotSystem.SelectMenuOption("8 min 30 sec", "8m30s"),
                new IBotSystem.SelectMenuOption("9 min", "9m"),
                new IBotSystem.SelectMenuOption("9 min 30 sec", "9m30s"),
                new IBotSystem.SelectMenuOption("10 min", "10m"),
            };
            m_voteTimerMenu = m_system.CreateSelectMenu($"gameplay_menu_votetimer", "Or instead, set a Vote Timer...", options);

            componentService.RegisterComponent(m_voteTimerMenu, VoteTimerMenuSelected);
        }

        private IBotComponent CreateButton(GameplayButton id, string label, IBotSystem.ButtonType type = IBotSystem.ButtonType.Primary, string? emoji = null)
        {
            return m_system.CreateButton($"gameplay_{id}", label, type, emoji: emoji);
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
        public async Task CommandNightAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await PhaseNightInternal(context);

            await EditOriginalMessage(context, message);
        }

        public async Task<string> PhaseNightInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var game = await m_gameplay.CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here? Could make use of use processLog!
                    return "Couldn't find an active game record for this town!";
                }
                return await m_gameplay.PhaseNightUnsafe(game, processLog);
            });
        }

        public async Task CommandDayAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();
            var message = await PhaseDayInternal(context);
            await EditOriginalMessage(context, message);
        }

        public async Task<string> PhaseDayInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var game = await m_gameplay.CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here? Could make use of use processLog!
                    return "Couldn't find an active game record for this town!";
                }
                return await m_gameplay.PhaseDayUnsafe(game, processLog);
            });
        }

        public async Task CommandVoteAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();
            var message = await PhaseVoteInternal(context);
            await EditOriginalMessage(context, message);
        }

        public async Task<string> PhaseVoteInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var game = await m_gameplay.CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here?
                    return "Couldn't find an active game record for this town!";
                }
                return await m_gameplay.PhaseVoteUnsafe(game, processLog);
            });
        }

        public async Task CommandGameAsync(IBotInteractionContext context)
        {
            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                await context.DeferInteractionResponse();

                var game = await m_gameplay.CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here?
                    return "Couldn't find an active game record for this town!";
                }

                var webhook = m_system.CreateWebhookBuilder().WithContent("Welcome to Blood on the Clocktower!");
                webhook = webhook.AddComponents(m_nightButton, m_dayButton, m_voteButton, m_endGameButton);
                webhook = webhook.AddComponents(m_voteTimerMenu);
                await context.EditResponseAsync(webhook);
                return "";
            });
        }

        public async Task CommandEndGameAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();
            var message = await EndGameInternal(context);
            await EditOriginalMessage(context, message);
        }

        public async Task<string> EndGameInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var game = await m_gameplay.CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    return "Couldn't find a current game to end!";
                }
                return await m_gameplay.EndGameUnsafe(game, processLog);
            });
        }

        public async Task CommandSetStorytellersAsync(IBotInteractionContext context, IEnumerable<IMember> users)
        {
            await context.DeferInteractionResponse();
            var message = await SetStorytellersInternal(context, users);
            await EditOriginalMessage(context, message);
        }

        public async Task<string> SetStorytellersInternal(IBotInteractionContext context, IEnumerable<IMember> users)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, (processLog) => m_gameplay.SetStorytellersUnsafe(context, users, processLog));
        }

        public async Task RunVoteTimerAsync(IBotInteractionContext context, string timeString)
        {
            await context.DeferInteractionResponse();

            var message = await RunVoteTimerInternal(context, timeString);

            await EditOriginalMessage(context, message);
        }

        public async Task<string> RunVoteTimerInternal(IBotInteractionContext context, string timeString)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, processLoggger => m_voteTimer.RunVoteTimerUnsafe(context, timeString, processLoggger));
        }

        public async Task RunStopVoteTimerAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await RunStopVoteTimerInternal(context);

            await EditOriginalMessage(context, message);
        }

        public async Task<string> RunStopVoteTimerInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, processLoggger => m_voteTimer.RunStopVoteTimerUnsafe(context, processLoggger));
        }
        #endregion

        #region Button handlers
        public async Task NightButtonPressed(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await PhaseNightInternal(context);

            var builder = m_system.CreateWebhookBuilder().WithContent(message);
            builder = builder.AddComponents(m_dayButton, m_moreButton);
            await context.EditResponseAsync(builder);
        }

        public async Task DayButtonPressed(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await PhaseDayInternal(context);

            var builder = m_system.CreateWebhookBuilder().WithContent(message);
            builder = builder.AddComponents(m_voteButton, m_endGameButton,  m_moreButton);
            builder = builder.AddComponents(m_voteTimerMenu);
            await context.EditResponseAsync(builder);
        }

        public async Task VoteButtonPressed(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await PhaseVoteInternal(context);

            var builder = m_system.CreateWebhookBuilder().WithContent(message);
            builder = builder.AddComponents(m_nightButton, m_endGameButton, m_moreButton);
            await context.EditResponseAsync(builder);
        }

        public async Task MoreButtonPressed(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var builder = m_system.CreateWebhookBuilder().WithContent("Here are all the options again!");
            builder = builder.AddComponents(m_nightButton, m_dayButton, m_voteButton, m_endGameButton);
            builder = builder.AddComponents(m_voteTimerMenu);
            await context.EditResponseAsync(builder);
        }

        public async Task EndGameButtonPressed(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await EndGameInternal(context);
            var builder = m_system.CreateWebhookBuilder().WithContent(message);
            await context.EditResponseAsync(builder);
        }

        public async Task VoteTimerMenuSelected(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var value = context.ComponentValues.First();

            var message = await RunVoteTimerInternal(context, value);
            var builder = m_system.CreateWebhookBuilder().WithContent(message);
            builder = builder.AddComponents(m_nightButton, m_moreButton);
            builder = builder.AddComponents(m_voteTimerMenu);
            await context.EditResponseAsync(builder);
        }
        #endregion
    }
}