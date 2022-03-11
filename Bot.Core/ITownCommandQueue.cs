using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public interface ITownCommandQueue
    {
        Task QueueCommandAsync(string initialMessage, IBotInteractionContext context, Func<Task<QueuedCommandResult>> queuedTask);
    }

    public class QueuedCommandResult
    {
        public string Message { get; }
        public IBotComponent[][] ComponentSets { get; }
        public QueuedCommandResult(string message, params IBotComponent[][] componentSets)
        {
            Message = message;
            ComponentSets = componentSets;
        }
    }
}
