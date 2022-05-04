using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public abstract class BaseInteractionWrapper<TKey, TQueue, TErrorHandler> : IInteractionWrapper<TKey>
        where TKey : notnull
        where TQueue : class, IInteractionQueue
        where TErrorHandler : class, IInteractionErrorHandler<TKey>
    {
        private readonly TQueue m_queue;
        private readonly TErrorHandler m_errorHandler;

        public BaseInteractionWrapper(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_queue);
            serviceProvider.Inject(out m_errorHandler);
        }

        public Task WrapInteractionAsync(string initialMessage, IBotInteractionContext context, Func<IProcessLogger, Task<InteractionResult>> process)
        {
            return m_queue.QueueInteractionAsync(initialMessage, context,
                () => m_errorHandler.TryProcessReportingErrorsAsync(KeyFromContext(context), context.Member, process));
        }

        protected abstract TKey KeyFromContext(IBotInteractionContext context);
    }
}
