using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public interface ITownInteractionQueue
    {
        Task QueueInteractionAsync(string initialMessage, IBotInteractionContext context, Func<Task<QueuedInteractionResult>> queuedTask);
    }

    public class QueuedInteractionResult
    {
        public string Message { get; }
        public bool IncludeComponents { get; }
        public IBotComponent[][] ComponentSets { get; }
        public QueuedInteractionResult(string message, bool includeComponents=false, params IBotComponent[][] componentSets)
        {
            Message = message;
            IncludeComponents = includeComponents;
            ComponentSets = componentSets;
        }
    }
}
