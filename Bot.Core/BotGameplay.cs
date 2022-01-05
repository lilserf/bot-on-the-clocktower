using Bot.Api;
using System;
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
		}

        private readonly IBotComponent m_nightButton;
        private readonly IBotComponent m_dayButton;
        private readonly IBotComponent m_voteButton;
        private readonly IBotComponent m_moreButton;

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

            m_componentService.RegisterComponent(m_nightButton, NightButtonPressed);
            m_componentService.RegisterComponent(m_dayButton, DayButtonPressed);
            m_componentService.RegisterComponent(m_voteButton, VoteButtonPressed);
            m_componentService.RegisterComponent(m_moreButton, MoreButtonPressed);
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

        // TODO: better name for this method, probably
        public async Task<IGame> CurrentGameAsync(IBotInteractionContext context, IProcessLogger logger)
        {
            if (m_activeGameService.TryGetGame(context, out IGame? game))
            {
                // TODO: resolve a change in Storytellers

                var foundUsers = game.Town.TownSquare.Users.ToList();
                foreach (var c in game.Town.DayCategory.Channels.Where(c => c.IsVoice))
                {
                    foundUsers.AddRange(c.Users.ToList());
                }
                foreach (var c in game.Town.NightCategory.Channels.Where(c => c.IsVoice))
                {
                    foundUsers.AddRange(c.Users.ToList());
                }

                // Sanity check for bots
                foundUsers = foundUsers.Where(u => !u.IsBot).ToList();

                var newPlayers = foundUsers.Except(game.AllPlayers);
                var oldPlayers = game.AllPlayers.Except(foundUsers);

                foreach (var p in newPlayers)
                {
                    await MemberHelper.GrantRoleLoggingErrorsAsync(p, game.Town.VillagerRole, logger);
                    game.Villagers.Add(p);
                }
                foreach (var p in oldPlayers)
                {
                    await MemberHelper.RevokeRoleLoggingErrorsAsync(p, game.Town.VillagerRole, logger);
                    game.Villagers.Remove(p);
                }

            }
            else
            {
                var townRec = await m_townLookup.GetTownRecord(context.Guild.Id, context.Channel.Id);
                var town = await m_client.ResolveTownAsync(townRec);

                // No record, so create one
                game = new Game(town);

                // Assume the author of the command is the Storyteller
                var storyteller = context.Member;

                await MemberHelper.GrantRoleLoggingErrorsAsync(storyteller, town.StoryTellerRole, logger);
                game.StoryTellers.Add(storyteller);

                var allUsers = town.TownSquare.Users.ToList();
                allUsers.Remove(storyteller);

                // Make everyone else a villager
                foreach (var v in allUsers)
                {
                    await MemberHelper.GrantRoleLoggingErrorsAsync(v, town.VillagerRole, logger);
                    game.Villagers.Add(v);
                }

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
            // TODO: games with no night category

            string message = "Moved all players from Town Square to Cottages!";
            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var game = await CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here? Could make use of use processLog!
                    message = "Couldn't find an active game record for this town!";
                    return;
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
            });

            return message;
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
            string msg = "Moved all players from Cottages back to Town Square!";
            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var game = await CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here?
                    msg = "Couldn't find an active game record for this town!";
                    return;
                }

                await MoveActivePlayersToTownSquare(game, processLog);

            });

            return msg;
        }

        public async Task PhaseVoteAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();
            var message = await PhaseVoteInternal(context);
            await EditOriginalMessage(context, message);
        }

        public async Task<string> PhaseVoteInternal(IBotInteractionContext context)
        { 
            string msg = "Moved all players to Town Square for voting!";
            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {

                var game = await CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here?
                    msg = "Couldn't find an active game record for this town!";
                    return;
                }

                await MoveActivePlayersToTownSquare(game, processLog);
            });
            return msg;
        }

        public async Task RunGameAsync(IBotInteractionContext context)
        {
            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                await context.DeferInteractionResponse();

                var webhook = m_system.CreateWebhookBuilder().WithContent("Welcome to Blood on the Clocktower!");
                webhook = webhook.AddComponents(m_nightButton!, m_dayButton!, m_voteButton!);
                await context.EditResponseAsync(webhook);
            });
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
            builder = builder.AddComponents(m_nightButton!, m_dayButton!, m_voteButton!);
            await context.EditResponseAsync(builder);
        }
    }
}
