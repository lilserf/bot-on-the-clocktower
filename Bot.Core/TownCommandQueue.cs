using Bot.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class TownCommandQueue : ITownCommandQueue
    {
        private readonly IBotSystem m_botSystem;

        private readonly Dictionary<TownKey, Queue<Func<Task<QueuedCommandResult>>>> m_townToCommandQueue = new();

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

                var townKey = context.GetTownKey();
                if (m_townToCommandQueue.TryGetValue(townKey, out var queue))
                    queue.Enqueue(queuedTask); // If queue exists it should already be processing
                else
                {
                    queue = new();
                    m_townToCommandQueue.Add(townKey, queue);
                    queue.Enqueue(queuedTask);

                    // Process the queue, but don't wait for it to complete
                    ProcessQueue(context, townKey);
                }
            }
            catch (Exception)
            { }
        }

        private void ProcessQueue(IBotInteractionContext context, TownKey townKey)
        {
            ProcessQueueAsync(context, townKey).ConfigureAwait(continueOnCapturedContext: true);
        }

        private async Task ProcessQueueAsync(IBotInteractionContext context, TownKey townKey)
        {
            if (m_townToCommandQueue.TryGetValue(townKey, out var queue))
            {
                while (queue.Count > 0)
                {
                    var func = queue.Dequeue();
                    try
                    {
                        var result = await func();

                        var webhook = m_botSystem.CreateWebhookBuilder().WithContent(result.Message);
                        foreach (var components in result.ComponentSets)
                            webhook.AddComponents(components);
                        await context.EditResponseAsync(webhook);
                    }
                    catch (Exception)
                    { }
                }
                m_townToCommandQueue.Remove(townKey);
            }
        }
    }
}
