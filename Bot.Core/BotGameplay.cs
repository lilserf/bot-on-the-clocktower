using Bot.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameplay : IBotGameplay
    {
        // TODO: we probably need a more robust system that can queue up multiple lines of errors without returning, then report them all?
        // This could be what IProcessLogger does
        private static Task ReportExceptionAsync(IBotInteractionContext context, Exception ex, string goal)
        {
            string message = $"Couldn't {goal} due to unknown error!";
            if (ex is UnauthorizedException)
            {
                message = $"Couldn't {goal} due to lack of permissions.";
            }
            else if(ex is ServerErrorException) // teehee
            {
                message = $"Couldn't {goal} due to a server error.";
            }
            else if(ex is RequestSizeException) // grrr
            {
                message = $"Couldn't {goal} due to bad request size?";
            }
            else if(ex is RateLimitException) // nice watch
            {
                message = $"Couldn't {goal} due to rate limits.";
            }
            else if (ex is BadRequestException) // no more common market
            {
                message = $"Couldn't {goal} - somehow resulted in a bad request.";
            }
            else if (ex is NotFoundException) // can't think of something clever here
            {
                message = $"Couldn't {goal} - something was not found!";
            }

            var system = context.Services.GetService<IBotSystem>();
            var webhook = system.CreateWebhookBuilder().WithContent(message);
            return context.EditResponseAsync(webhook);
        }

        // Helper that tries to grant a role, and sends a message if that failed
        private async Task<bool> GrantRoleAsync(IBotInteractionContext context, IMember member, IRole role)
        {
            try
            {
                await member.GrantRoleAsync(role);
                return true;
            }
            catch (Exception ex)
            {
                await ReportExceptionAsync(context, ex, $"grant role '{role.Name}' to {member.DisplayName}");
                return false;
            }
        }

        private async Task<bool> RevokeRoleAsync(IBotInteractionContext context, IMember member, IRole role)
		{
			try
			{
                await member.RevokeRoleAsync(role);
                return true;
			}
            catch(Exception ex)
			{
                await ReportExceptionAsync(context, ex, $"revoke role '{role.Name}' from {member.DisplayName}");
                return false;
            }
        }

        // Helper for editing the original interaction with a summarizing message when finished
        private async Task EditOriginalMessage(IBotInteractionContext context, string s)
        {
            var system = context.Services.GetService<IBotSystem>();
            var webhook = system.CreateWebhookBuilder().WithContent(s);
            await context.EditResponseAsync(webhook);
        }

        // TODO: better name for this method, probably
        public async Task<IGame?> CurrentGameAsync(IBotInteractionContext context)
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
                    await GrantRoleAsync(context, p, game.Town.VillagerRole);
                    game.Villagers.Add(p);
				}
                foreach(var p in oldPlayers)
				{
                    await RevokeRoleAsync(context, p, game.Town.VillagerRole);
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

                await GrantRoleAsync(context, storyteller, town.StoryTellerRole);
                game.StoryTellers.Add(storyteller);

                var allUsers = town.TownSquare.Users.ToList();
                allUsers.Remove(storyteller);

                // Make everyone else a villager
                foreach (var v in allUsers)
                {
                    await GrantRoleAsync(context, v, town.VillagerRole);
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
                var game = await CurrentGameAsync(context);
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
                    try
                    {
                        await st.PlaceInAsync(c);
                    }
                    catch (UnauthorizedException)
                    { }
                    catch (NotFoundException)
                    { }
                }

                // Now put everyone else in the remaining cottages
                var pairs = cottages.Zip(game.Villagers.OrderBy(u => u.DisplayName), (c, u) => Tuple.Create(c, u));

                foreach (var (cottage, user) in pairs)
                {
                    try
                    {
                        await user.PlaceInAsync(cottage);
                    }
                    catch (UnauthorizedException)
                    { }
                    catch (NotFoundException)
                    { }
                }

                // TODO: set permissions on the cottages for each user (hopefully in a batch)

                await EditOriginalMessage(context, "Moved all players from Town Square to Cottages!");
            });
        }

        // TODO: should this be a method on Game itself? :thinking:
        // Helper for moving all players to Town Square (used by Day and Vote commands)
        private async Task MoveActivePlayersToTownSquare(IGame game)
        {
            foreach (var member in game.AllPlayers)
            {
                try
                {
                    await member.PlaceInAsync(game.Town.TownSquare);
                }
                catch (UnauthorizedException)
                { }
                catch (NotFoundException)
                { }
            }
        }

        public async Task PhaseDayAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var game = await CurrentGameAsync(context);
                if (game == null)
                {
                    // TODO: more error reporting here?
                    await EditOriginalMessage(context, "Couldn't find an active game record for this town!");
                    return;
                }

                await MoveActivePlayersToTownSquare(game);

                await EditOriginalMessage(context, "Moved all players from Cottages back to Town Square!");
            });
        }

        public async Task PhaseVoteAsync(IBotInteractionContext context)
        {
            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                await context.DeferInteractionResponse();

                var game = await CurrentGameAsync(context);
                if (game == null)
                {
                    // TODO: more error reporting here?
                    await EditOriginalMessage(context, "Couldn't find an active game record for this town!");
                    return;
                }

                await MoveActivePlayersToTownSquare(game);

                await EditOriginalMessage(context, "Moved all players to Town Square for voting!");
            });
        }

        public async Task RunGameAsync(IBotInteractionContext context)
        {
            await context.DeferInteractionResponse();

            await InteractionWrapper.TryProcessReportingErrorsAsync(context, async (processLog) =>
            {
                var system = context.Services.GetService<IBotSystem>();

                var webhook = system.CreateWebhookBuilder();

                webhook.WithContent("You just ran the Game command. Good for you!");
                await context.EditResponseAsync(webhook);
            });
        }
    }
}
