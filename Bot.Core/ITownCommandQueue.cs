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
        public bool IncludeComponents { get; }
        public IBotComponent[][] ComponentSets { get; }
        public QueuedCommandResult(string message, bool includeComponents=false, params IBotComponent[][] componentSets)
        {
            Message = message;
            IncludeComponents = includeComponents;
            ComponentSets = componentSets;
        }
    }
}
