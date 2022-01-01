using Bot.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameService : IBotGameService
    {
		public async Task PhaseNightAsync(IBotInteractionContext context)
		{
            // TODO: "current game" role assignment

            await context.CreateDeferredResponseMessageAsync();

            var townLookup = context.Services.GetService<ITownLookup>();
            var townRec = await townLookup.GetTownRecord(context.Guild.Id, context.Channel.Id);

            var client = context.Services.GetService<IBotClient>();
            var town = await client.ResolveTownAsync(townRec);

            // TODO: order users by display name
            // TODO: put storytellers in the first cottages
            var pairs = town.NightCategory.Channels.Zip(town.TownSquare.Users, (c, u) => Tuple.Create(c, u));

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
