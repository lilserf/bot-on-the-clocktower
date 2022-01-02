using Bot.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameplay : IBotGameplay
    {
        // TODO: we probably need a more robust system that can queue up multiple lines of errors without returning, then report them all?
        public async Task ReportException(IBotInteractionContext context, Exception ex, string goal)
        {
            string message = $"Couldn't {goal} due to unknown error!";
            if (ex is UnauthorizedException uex)
            {
                message = $"Couldn't {goal} due to lack of permissions.";
            }
            else if(ex is ServerErrorException sex) // teehee
            {
                message = $"Couldn't {goal} due to a server error.";
            }
            else if(ex is RequestSizeException rex) // grrr
            {
                message = $"Couldn't {goal} due to bad request size?";
            }
            else if(ex is RateLimitException rlex) // nice watch
            {
                message = $"Couldn't {goal} due to rate limits.";
            }
            else if (ex is BadRequestException brex) // no more common market
            {
                message = $"Couldn't {goal} - somehow resulted in a bad request.";
            }
            else if (ex is NotFoundException nfex) // can't think of something clever here
            {
                message = $"Couldn't {goal} - something was not found!";
            }

            var system = context.Services.GetService<IBotSystem>();
            var webhook = system.CreateWebhookBuilder().WithContent(message);
            await context.EditResponseAsync(webhook);
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
                await ReportException(context, ex, $"grant role '{role.Name}' to {member.DisplayName}");
                return false;
            }
        }

        // TODO: better name for this method, probably
        public async Task<IGame?> CurrentGameAsync(IBotInteractionContext context, ITown town)
        {
            // TODO: check if there's already an active game in this town by adding some kind of ActiveGameService
            var game = new Game(town);

            // Assume the author of the command is the Storyteller
            var storyteller = context.Member;

            await GrantRoleAsync(context, storyteller, town.StoryTellerRole);
            game.StoryTellers.Add(storyteller);

            // Make everyone else a villager
            foreach(var v in town.TownSquare.Users.Where(x => x != storyteller))
            {
                await GrantRoleAsync(context, v, town.VillagerRole);
                game.Villagers.Add(v);
            }

            return game;
        }

        public async Task PhaseNightAsync(IBotInteractionContext context)
        {
            await context.CreateDeferredResponseMessageAsync();

            var townLookup = context.Services.GetService<ITownLookup>();
            var townRec = await townLookup.GetTownRecord(context.Guild.Id, context.Channel.Id);

            var client = context.Services.GetService<IBotClient>();
            var town = await client.ResolveTownAsync(townRec);

            var game = await CurrentGameAsync(context, town);
            if(game == null)
            {
                // TODO: more error reporting here?
                return;
            }

            // First. put storytellers into the top cottages
            var cottages = town.NightCategory.Channels.OrderBy(c => c.Position).ToList();
            foreach(var st in game.StoryTellers)
            {
                var c = cottages.ElementAt(0);
                cottages.Remove(c);
                await st.PlaceInAsync(c);
            }

            // Now put everyone else in the remaining cottages
            var pairs = cottages.Zip(game.Villagers.OrderBy(u => u.DisplayName), (c, u) => Tuple.Create(c, u));

            foreach(var (cottage, user) in pairs)
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

            var system = context.Services.GetService<IBotSystem>();
            var webhook = system.CreateWebhookBuilder();
            webhook.WithContent("Moved all users from Town Square to Cottages!");
            await context.EditResponseAsync(webhook);
        }

        public async Task RunGameAsync(IBotInteractionContext context)
        {
            var system = context.Services.GetService<IBotSystem>();
            await context.CreateDeferredResponseMessageAsync();

            var webhook = system.CreateWebhookBuilder();
            webhook.WithContent("You just ran the Game command. Good for you!");
            await context.EditResponseAsync(webhook);
        }
    }
}
