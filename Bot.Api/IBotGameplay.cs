using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotGameplay
    {
        void CreateComponents(IServiceProvider services);
        Task RunGameAsync(IBotInteractionContext context);
        Task PhaseNightAsync(IBotInteractionContext context);
        Task PhaseDayAsync(IBotInteractionContext context);
        Task PhaseVoteAsync(IBotInteractionContext context);
    }
}
