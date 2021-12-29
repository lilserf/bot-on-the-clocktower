using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotGameService
    {
        Task RunGameAsync(IBotClient client, IBotInteractionContext context);
    }
}
