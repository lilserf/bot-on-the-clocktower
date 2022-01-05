using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameplay : IBotGameplay
    {
        private enum GameplayButton
		{
            Night,
            Day,
            Vote,
            More,
            EndGame,
		}

        private readonly IBotComponent m_nightButton;
        private readonly IBotComponent m_dayButton;
        private readonly IBotComponent m_voteButton;
        private readonly IBotComponent m_moreButton;
        private readonly IBotComponent m_endGameButton;

        private readonly IBotSystem m_system;
        private readonly IBotClient m_client;

        private readonly IComponentService m_componentService;
        private readonly IActiveGameService m_activeGameService;
        private readonly ITownLookup m_townLookup;
        private readonly IShuffleService m_shuffle;

		public BotGameplay(IServiceProvider services)
		{
            m_system = services.GetService<IBotSystem>();
            m_client = services.GetService<IBotClient>();
            m_componentService = services.GetService<IComponentService>();
            m_activeGameService = services.GetService<IActiveGameService>();
            m_townLookup = services.GetService<ITownLookup>();
            m_shuffle = services.GetService<IShuffleService>();

            m_nightButton = CreateButton(GameplayButton.Night, "Night");
            m_dayButton = CreateButton(GameplayButton.Day, "Day", IBotSystem.ButtonType.Success);
            m_voteButton = CreateButton(GameplayButton.Vote, "Vote", IBotSystem.ButtonType.Danger);
            m_moreButton = CreateButton(GameplayButton.More, "More", IBotSystem.ButtonType.Secondary);
            m_endGameButton = CreateButton(GameplayButton.EndGame, "End Game", IBotSystem.ButtonType.Danger);

            m_componentService.RegisterComponent(m_nightButton, NightButtonPressed);
            m_componentService.RegisterComponent(m_dayButton, DayButtonPressed);
            m_componentService.RegisterComponent(m_voteButton, VoteButtonPressed);
            m_componentService.RegisterComponent(m_moreButton, MoreButtonPressed);
            m_componentService.RegisterComponent(m_endGameButton, EndGameButtonPressed);
        }

        private IBotComponent CreateButton(GameplayButton id, string label, IBotSystem.ButtonType type = IBotSystem.ButtonType.Primary)
        {
            return m_system.CreateButton($"gameplay_{id}", label, type);
        }

        // Helper for editing the original interaction with a summarizing message when finished
        // TODO: move within IBotInteractionContext
        private async Task EditOriginalMessage(IBotInteractionContext context, string s)
        {
            try
            {
                var webhook = m_system.CreateWebhookBuilder().WithContent(s);
                await context.EditResponseAsync(webhook);
            }
            catch (Exception)
            { }
        }

        private const string InvalidTownMessage = "Couldn't find a registered town for this server and channel. Consider re-creating the town with /createTown or /addTown.";
        public bool CheckIsTownViable(ITown? town, IProcessLogger logger)
        {
            // Without these two, there's not much else to do
            if (town == null)
            {
                logger.LogMessage(InvalidTownMessage);
                return false;
            }
            if (town.TownRecord == null)
            {
                logger.LogMessage(InvalidTownMessage);
                return false;
            }

            // Run all these even if one fails
            bool success = true;
            if (town.DayCategory == null)
            {
                if (town.TownRecord.DayCategory != null)
                    logger.LogMessage($"Couldn't find Day Category '{town.TownRecord.DayCategory}'");
                else
                    logger.LogMessage($"Couldn't find a registered Day Category for this town! Consider re-creating the town with /createTown or /addTown.");
                success = false;
            }
            if(town.TownSquare == null)
            {
                if(town.TownRecord.TownSquare != null)
                    logger.LogMessage($"Couldn't find Town Square channel '{town.TownRecord.TownSquare}'");
                else
                    logger.LogMessage($"Couldn't find a registered Town Square for this town! Consider re-creating the town with /createTown or /addTown.");
                success = false;
            }
            if(town.StoryTellerRole == null)
            {
                if(town.TownRecord.StoryTellerRole != null)
                    logger.LogMessage($"Couldn't find Storyteller role '{town.TownRecord.StoryTellerRole}'");
                else
                    logger.LogMessage($"Couldn't find a registered Storyteller role for this town! Consider re-creating the town with /createTown or /addTown.");
            }
            if (town.VillagerRole == null)
            {
                if (town.TownRecord.VillagerRole != null)
                    logger.LogMessage($"Couldn't find Storyteller role '{town.TownRecord.VillagerRole}'");
                else
                    logger.LogMessage($"Couldn't find a registered Villager role for this town! Consider re-creating the town with /createTown or /addTown.");
            }

            return success;
        }

        private async void TagStorytellers(IGame game, IProcessLogger logger)
        {
            foreach (var u in game.StoryTellers)
            {
                await MemberHelper.AddStorytellerTag(u, logger);
            }

            foreach(var u in game.Villagers)
            {
                await MemberHelper.RemoveStorytellerTag(u, logger);
            }
        }

        private async void GrantAndRevokeRoles(IGame game, IProcessLogger logger)
        {

        }

        // TODO: better name for this method, probably
        public async Task<IGame?> CurrentGameAsync(IBotInteractionContext context, IProcessLogger logger)
        {
            if (m_activeGameService.TryGetGame(context, out IGame? game))
            {
                if(!CheckIsTownViable(game.Town, logger))
                {
                    return null;
                }
                // TODO: resolve a change in Storytellers

                TagStorytellers(game, logger);

                var foundUsers = game.Town.TownSquare.Users.ToList();

                foreach (var c in game.Town.DayCategory.Channels.Where(c => c.IsVoice))
                {
                    foundUsers.AddRange(c.Users.ToList());
                }

                if (game.Town.NightCategory != null)
                {
                    foreach (var c in game.Town.NightCategory.Channels.Where(c => c.IsVoice))
                    {
                        foundUsers.AddRange(c.Users.ToList());
                    }
                }

                // Sanity check for bots
                foundUsers = foundUsers.Where(u => !u.IsBot).ToList();

                var newPlayers = foundUsers.Except(game.AllPlayers);
                var oldPlayers = game.AllPlayers.Except(foundUsers);

                foreach (var p in newPlayers)
                {
                    await MemberHelper.GrantRoleLoggingErrorsAsync(p, game.Town.VillagerRole, logger);
                    game.AddVillager(p);
                }
                foreach (var p in oldPlayers)
                {
                    await MemberHelper.RevokeRoleLoggingErrorsAsync(p, game.Town.VillagerRole, logger);
                    game.RemoveVillager(p);
                }
            }
            else
            {
                var townRec = await m_townLookup.GetTownRecord(context.Guild.Id, context.Channel.Id);
                if(townRec == null)
                {
                    logger.LogMessage(InvalidTownMessage);
                    return null;
                }

                var town = await m_client.ResolveTownAsync(townRec);
                if (!CheckIsTownViable(town, logger))
                {
                    return null;
                }

                // No record, so create one
                game = new Game(town!);

                // Assume the author of the command is the Storyteller
                var storyteller = context.Member;

                await MemberHelper.GrantRoleLoggingErrorsAsync(storyteller, town.StoryTellerRole, logger);
                game.AddStoryTeller(storyteller);

                var allUsers = new List<IMember>();

                foreach (var c in town.DayCategory.Channels.Where(c => c.IsVoice))
                {
                    allUsers.AddRange(c.Users);
                }

                if(town.NightCategory != null)
                {
                    foreach(var c in town.NightCategory.Channels.Where(c => c.IsVoice))
                    {
                        allUsers.AddRange(c.Users);
                    }
                }

                // Sanity check for bots
                allUsers = allUsers.Where(u => !u.IsBot).ToList();

                allUsers.Remove(storyteller);

                // Make everyone else a villager
                foreach (var v in allUsers)
                {
                    await MemberHelper.GrantRoleLoggingErrorsAsync(v, town.VillagerRole, logger);
                    game.AddVillager(v);
                }

                TagStorytellers(game, logger);

                m_activeGameService.RegisterGame(town, game);
            }

            return game;
        }

        public async Task PhaseNightAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await PhaseNightInternal(context);

            await EditOriginalMessage(context, message);
        }

        public async Task<string> PhaseNightInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, (processLog) => PhaseNightUnsafe(context, processLog) );
        }

        public async Task<string> PhaseNightUnsafe(IBotInteractionContext context, IProcessLogger processLog)
		{
            // TODO: games with no night category
            var game = await CurrentGameAsync(context, processLog);
            if (game == null)
            {
                // TODO: more error reporting here? Could make use of use processLog!
                return "Couldn't find an active game record for this town!";
            }

            // First. put storytellers into the top cottages
            var cottages = game.Town.NightCategory.Channels.OrderBy(c => c.Position).ToList();
            var stPairs = cottages.Take(game.StoryTellers.Count).Zip(game.StoryTellers.OrderBy(u => u.DisplayName), (c, u) => Tuple.Create(c, u)).ToList();

            foreach (var (c, st) in m_shuffle.Shuffle(stPairs))
            {
                await MemberHelper.MoveToChannelLoggingErrorsAsync(st, c, processLog);
            }

            // Now put everyone else in the remaining cottages
            var pairs = cottages.Skip(game.StoryTellers.Count).Zip(game.Villagers.OrderBy(u => u.DisplayName), (c, u) => Tuple.Create(c, u)).ToList();

            foreach (var (cottage, user) in m_shuffle.Shuffle(pairs))
            {
                await MemberHelper.MoveToChannelLoggingErrorsAsync(user, cottage, processLog);
            }

            // TODO: set permissions on the cottages for each user (hopefully in a batch)
            return "Moved all players from Town Square to Cottages!";
        }

        // TODO: should this be a method on Game itself? :thinking:
        // Helper for moving all players to Town Square (used by Day and Vote commands)
        private async Task MoveActivePlayersToTownSquare(IGame game, IProcessLogger logger)
        {
            // TODO: take away cottage permissions
            foreach (var member in m_shuffle.Shuffle(game.AllPlayers))
            {
                await MemberHelper.MoveToChannelLoggingErrorsAsync(member, game.Town.TownSquare, logger);
            }
        }

        public async Task PhaseDayAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();
            var message = await PhaseDayInternal(context);
            await EditOriginalMessage(context, message);
        }

        public async Task<string> PhaseDayInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, (processLog) => PhaseDayUnsafe(context, processLog));
        }

        public async Task<string> PhaseDayUnsafe(IBotInteractionContext context, IProcessLogger processLog)
		{
            var game = await CurrentGameAsync(context, processLog);
            if (game == null)
            {
                // TODO: more error reporting here?
                return "Couldn't find an active game record for this town!";
            }

            await MoveActivePlayersToTownSquare(game, processLog);
            return "Moved all players from Cottages back to Town Square!";
        }

        public async Task PhaseVoteAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();
            var message = await PhaseVoteInternal(context);
            await EditOriginalMessage(context, message);
        }

        public async Task<string> PhaseVoteInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, (processLog) => PhaseDayUnsafe(context, processLog));
        }

        public async Task<string> PhaseVoteUnsafe(IBotInteractionContext context, IProcessLogger processLog)
        { 
            var game = await CurrentGameAsync(context, processLog);
            if (game == null)
            {
                // TODO: more error reporting here?
                return "Couldn't find an active game record for this town!";
            }

            await MoveActivePlayersToTownSquare(game, processLog);
            return "Moved all players to Town Square for voting!";
        }

        public async Task RunGameAsync(IBotInteractionContext context)
        {
            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                await context.DeferInteractionResponse();

                var webhook = m_system.CreateWebhookBuilder().WithContent("Welcome to Blood on the Clocktower!");
                webhook = webhook.AddComponents(m_nightButton!, m_dayButton!, m_voteButton!);
                await context.EditResponseAsync(webhook);
                return "";
            });
        }


        public async Task EndGameAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();
            var message = await EndGameInternal(context);
            await EditOriginalMessage(context, message);
        }

        public async Task<string> EndGameInternal(IBotInteractionContext context)
        {
            return await InteractionWrapper.TryProcessReportingErrorsAsync(context, (processLog) => EndGameUnsafe(context, processLog));
        }

        private async Task<string> EndGameUnsafe(IBotInteractionContext context, IProcessLogger logger)
        {
            var game = await CurrentGameAsync(context, logger);
            if(game == null)
            {
                return "Couldn't find a current game to end!";
            }
            m_activeGameService.EndGame(game.Town);

            foreach (var user in game.StoryTellers)
            {
                await MemberHelper.RemoveStorytellerTag(user, logger);
                await user.RevokeRoleAsync(game.Town.StoryTellerRole);
                await user.RevokeRoleAsync(game.Town.VillagerRole);
            }

            foreach(var user in game.Villagers)
            {
                await user.RevokeRoleAsync(game.Town.VillagerRole);
            }

            return "Thank you for playing Blood on the Clocktower!";
        }

        public async Task NightButtonPressed(IBotInteractionContext context)
		{
            await context.DeferInteractionResponse();

            var message = await PhaseNightInternal(context);

            var builder = m_system.CreateWebhookBuilder().WithContent(message);
            builder = builder.AddComponents(m_dayButton!, m_moreButton!);
            await context.EditResponseAsync(builder);
		}

        public async Task DayButtonPressed(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await PhaseDayInternal(context);

            var builder = m_system.CreateWebhookBuilder().WithContent(message);
            builder = builder.AddComponents(m_voteButton!, m_moreButton!);
            await context.EditResponseAsync(builder);
        }

        public async Task VoteButtonPressed(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await PhaseVoteInternal(context);

            var builder = m_system.CreateWebhookBuilder().WithContent(message);
            builder = builder.AddComponents(m_nightButton!, m_moreButton!);
            await context.EditResponseAsync(builder);
        }

        public async Task MoreButtonPressed(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var builder = m_system.CreateWebhookBuilder().WithContent("Here are all the options again!");
            builder = builder.AddComponents(m_nightButton!, m_dayButton!, m_voteButton!, m_endGameButton!);
            await context.EditResponseAsync(builder);
        }

        public async Task EndGameButtonPressed(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            var message = await EndGameInternal(context);
            var builder = m_system.CreateWebhookBuilder().WithContent(message);
            await context.EditResponseAsync(builder);
        }
    }
}
