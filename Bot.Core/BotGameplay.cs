using Bot.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameplay : IBotGameplay
    {
        private IBotComponent? m_testButton;
		public BotGameplay()
		{
        }

        public void CreateComponents(IServiceProvider services)
		{
            var system = services.GetService<IBotSystem>();
            m_testButton = system.CreateButton("test_id", "Test Button!");

            var compService = services.GetService<IComponentService>();
            compService.RegisterComponent(m_testButton, TestButtonPressed);
        }

        // Helper for editing the original interaction with a summarizing message when finished
        // TODO: move within IBotInteractionContext
        private async Task EditOriginalMessage(IBotInteractionContext context, string s)
        {
            var system = context.Services.GetService<IBotSystem>();
            var webhook = system.CreateWebhookBuilder().WithContent(s);
            await context.EditResponseAsync(webhook);
        }

        // TODO: better name for this method, probably
        public async Task<IGame?> CurrentGameAsync(IBotInteractionContext context, IProcessLogger logger)
        {
            var ags = context.Services.GetService<IActiveGameService>();

            IGame? game = null;
            if (ags.TryGetGame(context, out game))
            {
                // TODO: resolve a change in Storytellers

                var foundUsers = game!.Town.TownSquare.Users.ToList();
                foreach(var c in game!.Town.DayCategory.Channels.Where(c => c.IsVoice))
                {
                    foundUsers.AddRange(c.Users.ToList());
                }
                foreach(var c in game!.Town.NightCategory.Channels.Where(c => c.IsVoice))
                {
                    foundUsers.AddRange(c.Users.ToList());
                }

                // Sanity check for bots
                foundUsers = foundUsers.Where(u => !u.IsBot).ToList();

                var newPlayers = foundUsers.Except(game.AllPlayers);
                var oldPlayers = game.AllPlayers.Except(foundUsers);

                foreach(var p in newPlayers)
				{
                    await MemberHelper.GrantRoleLoggingErrorsAsync(p, game.Town.VillagerRole, logger);
                    game.Villagers.Add(p);
				}
                foreach(var p in oldPlayers)
				{
                    await MemberHelper.RevokeRoleLoggingErrorsAsync(p, game.Town.VillagerRole, logger);
                    game.Villagers.Remove(p);
                }

            }
            else
            {
                var townLookup = context.Services.GetService<ITownLookup>();
                var townRec = await townLookup.GetTownRecord(context.Guild.Id, context.Channel.Id);

                var client = context.Services.GetService<IBotClient>();
                var town = await client.ResolveTownAsync(townRec);

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

                ags.RegisterGame(town, game);
            }

            return game;
        }

        public async Task PhaseNightAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var game = await CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here? Could make use of use processLog!
                    await EditOriginalMessage(context, "Couldn't find an active game record for this town!");
                    return;
                }

                // First. put storytellers into the top cottages
                var cottages = game.Town.NightCategory.Channels.OrderBy(c => c.Position).ToList();
                foreach (var st in game.StoryTellers)
                {
                    var c = cottages.ElementAt(0);
                    cottages.Remove(c);
                    await MemberHelper.MoveToChannelLoggingErrorsAsync(st, c, processLog);
                }

                // Now put everyone else in the remaining cottages
                var pairs = cottages.Zip(game.Villagers.OrderBy(u => u.DisplayName), (c, u) => Tuple.Create(c, u));

                foreach (var (cottage, user) in pairs)
                {
                    await MemberHelper.MoveToChannelLoggingErrorsAsync(user, cottage, processLog);
                }

                // TODO: set permissions on the cottages for each user (hopefully in a batch)

                await EditOriginalMessage(context, "Moved all players from Town Square to Cottages!");
            });
        }

        // TODO: should this be a method on Game itself? :thinking:
        // Helper for moving all players to Town Square (used by Day and Vote commands)
        private async Task MoveActivePlayersToTownSquare(IGame game, IProcessLogger logger)
        {
            foreach (var member in game.AllPlayers)
            {
                await MemberHelper.MoveToChannelLoggingErrorsAsync(member, game.Town.TownSquare, logger);
            }
        }

        public async Task PhaseDayAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var game = await CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here?
                    await EditOriginalMessage(context, "Couldn't find an active game record for this town!");
                    return;
                }

                await MoveActivePlayersToTownSquare(game, processLog);

                await EditOriginalMessage(context, "Moved all players from Cottages back to Town Square!");
            });
        }

        public async Task PhaseVoteAsync(IBotInteractionContext context)
        {
            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                await context.DeferInteractionResponse();

                var game = await CurrentGameAsync(context, processLog);
                if (game == null)
                {
                    // TODO: more error reporting here?
                    await EditOriginalMessage(context, "Couldn't find an active game record for this town!");
                    return;
                }

                await MoveActivePlayersToTownSquare(game, processLog);

                await EditOriginalMessage(context, "Moved all players to Town Square for voting!");
            });
        }

        public async Task RunGameAsync(IBotInteractionContext context)
        {
            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                await context.DeferInteractionResponse();

                var system = context.Services.GetService<IBotSystem>();
                var webhook = system.CreateWebhookBuilder().WithContent("You just ran the Game command. Good for you!");
                webhook = webhook.AddComponents(m_testButton!);
                await context.EditResponseAsync(webhook);
            });
        }

        public async Task TestButtonPressed(IBotComponentContext context, IServiceProvider services)
		{
            var system = services.GetService<IBotSystem>();
            var builder = system.CreateInteractionResponseBuilder().WithContent("You clicked on my button. Congratulations!");
			await context.UpdateOriginalMessageAsync(builder);
		}
    }
}
