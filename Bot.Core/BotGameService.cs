using Bot.Api;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameService : IBotGameService
    {
        public Task RunGameAsync(IBotClient client, IBotInteractionContext context)
        {
            var response = client.CreateInteractionResponseBuilder();
            response.WithContent("You just ran the Game command. Good for you!");
            return context.CreateDeferredResponseMessage(response);
        }
    }
}
