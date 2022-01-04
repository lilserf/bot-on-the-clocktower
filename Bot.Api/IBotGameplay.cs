using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotGameplay
    {
        Task RunGameAsync(IBotInteractionContext context);
        Task PhaseNightAsync(IBotInteractionContext context);
        Task PhaseDayAsync(IBotInteractionContext context);
        Task PhaseVoteAsync(IBotInteractionContext context);
    }
}
