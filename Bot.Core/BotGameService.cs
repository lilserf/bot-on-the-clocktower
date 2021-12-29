using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameService : IBotGameService
    {
        public Task RunGameAsync(IBotInteractionContext context)
        {
            var system = context.Services.GetService<IBotSystem>();
            var response = system.CreateInteractionResponseBuilder();
            response.WithContent("You just ran the Game command. Good for you!");
            return context.CreateDeferredResponseMessage(response);
        }
    }
}
