using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public abstract class BaseInteractionQueue<TKey> where TKey : notnull
    {
        private readonly IBotSystem m_botSystem;
        private readonly IShutdownPreventionService m_shutdownPreventionService;

        private readonly Dictionary<TKey, Queue<QueueItem>> m_keyToCommandQueue = new();

        private readonly TaskCompletionSource m_readyToShutdown = new();
        private bool m_shutdownRequested = false;

        const string ShutdownRequestedMessage = "Bot on the Clocktower is restarting. Please wait a few moments, then try again.";

        public BaseInteractionQueue(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_botSystem);
            serviceProvider.Inject(out m_shutdownPreventionService);

            m_shutdownPreventionService.RegisterShutdownPreventer(m_readyToShutdown.Task);
            m_shutdownPreventionService.ShutdownRequested += OnShutdownRequsted;
        }

        public async Task QueueInteractionAsync(string initialMessage, IBotInteractionContext context, Func<Task<InteractionResult>> queuedTask)
        {
            try
            {
                await context.DeferInteractionResponse();
                var webhook = m_botSystem.CreateWebhookBuilder().WithContent(m_shutdownRequested ? ShutdownRequestedMessage : initialMessage);
                await context.EditResponseAsync(webhook);

                if (m_shutdownRequested)
                    return;

                QueueItem newItem = new(context, queuedTask);

                var key = KeyFromContext(context);
                if (m_keyToCommandQueue.TryGetValue(key, out var queue))
                    queue.Enqueue(newItem); // If queue exists it should already be processing
                else
                {
                    queue = new();
                    m_keyToCommandQueue.Add(key, queue);
                    queue.Enqueue(newItem);

                    // Process the queue, but don't wait for it to complete
                    ProcessQueue(key);
                }
            }
            catch (Exception)
            { }
        }

        protected abstract TKey KeyFromContext(IBotInteractionContext context);

        private void ProcessQueue(TKey key)
        {
            ProcessQueueAsync(key).ConfigureAwait(continueOnCapturedContext: true);
        }

        private async Task ProcessQueueAsync(TKey key)
        {
            if (m_keyToCommandQueue.TryGetValue(key, out var queue))
            {
                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();
                    try
                    {
                        var result = await item.CommandFunc();

                        string messageWithLogs = result.LogMessages.Count > 0 ? $"{string.Join('\n', result.LogMessages)}\n{result.Message}" : result.Message;

                        var webhook = m_botSystem.CreateWebhookBuilder()
                            .WithContent(messageWithLogs)
                            .AddEmbeds(result.Embeds);

                        if (result.IncludeComponents)
                            foreach (var components in result.ComponentSets)
                                webhook.AddComponents(components);
                        await item.Context.EditResponseAsync(webhook);
                    }
                    catch (Exception)
                    { }
                }
                m_keyToCommandQueue.Remove(key);
            }
            if (m_shutdownRequested && !m_keyToCommandQueue.Any())
                m_readyToShutdown.TrySetResult();
        }

        private void OnShutdownRequsted(object? sender, EventArgs e)
        {
            m_shutdownRequested = true;

            if (!m_keyToCommandQueue.Any())
                m_readyToShutdown.TrySetResult();

            m_shutdownPreventionService.ShutdownRequested -= OnShutdownRequsted;
        }

        private class QueueItem
        {
            public IBotInteractionContext Context { get; }
            public Func<Task<InteractionResult>> CommandFunc { get; }
            public QueueItem(IBotInteractionContext context, Func<Task<InteractionResult>> commandFunc)
            {
                Context = context;
                CommandFunc = commandFunc;
            }
        }
    }
}
