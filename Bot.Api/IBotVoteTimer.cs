using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBotVoteTimer
    {
        Task RunVoteTimerAsync(IBotInteractionContext context, string timeString);
        Task RunStopVoteTimerAsync(IBotInteractionContext dSharpInteractionContext);
    }
}
