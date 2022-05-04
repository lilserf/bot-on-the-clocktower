using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotGameplayInteractionHandler
    {
        Task CommandGameAsync(IBotInteractionContext context);
        Task CommandNightAsync(IBotInteractionContext context);
        Task CommandDayAsync(IBotInteractionContext context);
        Task CommandVoteAsync(IBotInteractionContext context);
        Task CommandEndGameAsync(IBotInteractionContext context);
        Task CommandSetStorytellersAsync(IBotInteractionContext context, IEnumerable<IMember> users);
        Task RunVoteTimerAsync(IBotInteractionContext context, string timeString);
        Task RunStopVoteTimerAsync(IBotInteractionContext dSharpInteractionContext);
    }
}
