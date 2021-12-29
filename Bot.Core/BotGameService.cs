using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameService : IBotGameService
    {
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
