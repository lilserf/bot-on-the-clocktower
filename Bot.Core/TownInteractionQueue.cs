using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class TownInteractionQueue : ITownInteractionQueue
    {
        private readonly IBotSystem m_botSystem;
        private readonly IShutdownPreventionService m_shutdownPreventionService;

        private readonly Dictionary<TownKey, Queue<QueueItem>> m_townToCommandQueue = new();

        private readonly TaskCompletionSource m_readyToShutdown = new();
        private bool m_shutdownRequested = false;

        const string ShutdownRequestedMessage = "Bot on the Clocktower is restarting. Please wait a few moments, then try again.";

        public TownInteractionQueue(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_botSystem);
            serviceProvider.Inject(out m_shutdownPreventionService);

            m_shutdownPreventionService.RegisterShutdownPreventer(m_readyToShutdown.Task);
            m_shutdownPreventionService.ShutdownRequested += OnShutdownRequsted;
        }

        public async Task QueueInteractionAsync(string initialMessage, IBotInteractionContext context, Func<Task<QueuedInteractionResult>> queuedTask)
        {
            try
            {
                await context.DeferInteractionResponse();
                var webhook = m_botSystem.CreateWebhookBuilder().WithContent(m_shutdownRequested ? ShutdownRequestedMessage : initialMessage);
                await context.EditResponseAsync(webhook);

                if (m_shutdownRequested)
                    return;

                QueueItem newItem = new(context, queuedTask);

                var townKey = context.GetTownKey();
                if (m_townToCommandQueue.TryGetValue(townKey, out var queue))
                    queue.Enqueue(newItem); // If queue exists it should already be processing
                else
                {
                    queue = new();
                    m_townToCommandQueue.Add(townKey, queue);
                    queue.Enqueue(newItem);

                    // Process the queue, but don't wait for it to complete
                    ProcessQueue(townKey);
                }
            }
            catch (Exception)
            { }
        }

        private void ProcessQueue(TownKey townKey)
        {
            ProcessQueueAsync(townKey).ConfigureAwait(continueOnCapturedContext: true);
        }

        private async Task ProcessQueueAsync(TownKey townKey)
        {
            if (m_townToCommandQueue.TryGetValue(townKey, out var queue))
            {
                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();
                    try
                    {
                        var result = await item.CommandFunc();

                        var webhook = m_botSystem.CreateWebhookBuilder().WithContent(result.Message);
                        if (result.IncludeComponents)
                            foreach (var components in result.ComponentSets)
                                webhook.AddComponents(components);
                        await item.Context.EditResponseAsync(webhook);
                    }
                    catch (Exception)
                    { }
                }
                m_townToCommandQueue.Remove(townKey);
            }
            if (m_shutdownRequested && !m_townToCommandQueue.Any())
                m_readyToShutdown.TrySetResult();
        }

        private void OnShutdownRequsted(object? sender, EventArgs e)
        {
            m_shutdownRequested = true;

            if (!m_townToCommandQueue.Any())
                m_readyToShutdown.TrySetResult();

            m_shutdownPreventionService.ShutdownRequested -= OnShutdownRequsted;
        }

        private class QueueItem
        {
            public IBotInteractionContext Context { get; }
            public Func<Task<QueuedInteractionResult>> CommandFunc { get; }
            public QueueItem(IBotInteractionContext context, Func<Task<QueuedInteractionResult>> commandFunc)
            {
                Context = context;
                CommandFunc = commandFunc;
            }
        }
    }
}
