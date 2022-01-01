using Bot.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameService : IBotGameService
    {
        // TODO: better name for this method, probably
        public async Task<IGame> CurrentGameAsync(IBotInteractionContext context, ITown town)
		{
            // TODO: check if there's already an active game in this town by adding some kind of ActiveGameService
            var game = new Game(town);

            // Assume the author of the command is the Storyteller
            var storyteller = context.Member;
            await storyteller.GrantRoleAsync(town.StoryTellerRole);
            game.StoryTellers.Add(storyteller);

            // Make everyone else a villager
            foreach(var v in town.TownSquare.Users.Where(x => x != storyteller))
			{
                await v.GrantRoleAsync(town.VillagerRole);
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

            // TODO: put storytellers in the first cottages
            var pairs = town.NightCategory.Channels.OrderBy(c=>c.Position).Zip(town.TownSquare.Users.OrderBy(u => u.DisplayName), (c, u) => Tuple.Create(c, u));

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
