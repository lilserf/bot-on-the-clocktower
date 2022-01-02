using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotGameplay
    {
        Task RunGameAsync(IBotInteractionContext context);
		Task PhaseNightAsync(IBotInteractionContext context);
	}
}
