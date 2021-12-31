using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameService : IBotGameService
    {
		public async Task PhaseNightAsync(IBotInteractionContext context)
		{
            await context.CreateDeferredResponseMessageAsync();

            var townLookup = context.Services.GetService<ITownLookup>();
            var town = townLookup.GetTown(context.Guild.Id, context.Channel.Id);

            // TODO: do something with the town
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
