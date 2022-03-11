using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class TownCommandQueue : ITownCommandQueue
    {
        private readonly IBotSystem m_botSystem;

        private readonly Dictionary<TownKey, Queue<QueueItem>> m_townToCommandQueue = new();

        public TownCommandQueue(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_botSystem);
        }

        public async Task QueueCommandAsync(string initialMessage, IBotInteractionContext context, Func<Task<QueuedCommandResult>> queuedTask)
        {
            try
            {
                await context.DeferInteractionResponse();
                var webhook = m_botSystem.CreateWebhookBuilder().WithContent(initialMessage);
                await context.EditResponseAsync(webhook);

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
        }

        private class QueueItem
        {
            public IBotInteractionContext Context { get; }
            public Func<Task<QueuedCommandResult>> CommandFunc { get; }
            public QueueItem(IBotInteractionContext context, Func<Task<QueuedCommandResult>> commandFunc)
            {
                Context = context;
                CommandFunc = commandFunc;
            }
        }
    }
}
